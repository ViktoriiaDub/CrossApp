using Core.Converters;
using Core.Services;
using Core.Domain;
using Core.Dto;
using Core.Import;

string path = args.Length > 0 ? args[0] : Path.Combine("data", "sample.csv");

if (!File.Exists(path))
{
    Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
    return 1;
}

string extension = Path.GetExtension(path).ToLowerInvariant();

ImportResult<BookDto> fileResult = extension switch
{
    ".csv" => BookCsvImporter.Load(path),
    ".json" => BookJsonImporter.Load(path),
    _ => new ImportResult<BookDto>([], [$"Непідтримуваний формат файлу: {extension}"])
};

ImportResult<BookCopy> copiesResult = BookCopyConverter.Convert(fileResult);

int fileErrors = fileResult.Errors.Count;
int domainErrors = copiesResult.Errors.Count - fileErrors;

Console.WriteLine($"=== Імпорт: {path} ===");
Console.WriteLine($"Прочитано з файлу:             {fileResult.Items.Count}");
Console.WriteLine($"Створено примірників:          {copiesResult.Items.Count}");
Console.WriteLine($"Відхилено на рівні файлу:      {fileErrors}");
Console.WriteLine($"Відхилено інваріантами домену: {domainErrors}");

if (copiesResult.Errors.Count > 0)
{
    Console.WriteLine("\nПомилки:");
    foreach (string err in copiesResult.Errors)
        Console.WriteLine($"  ! {err}");
}

Console.WriteLine("\nУсі примірники:");
foreach (BookCopy c in copiesResult.Items)
    Console.WriteLine($"  {c}");

BookCopy? copy = copiesResult.Items.FirstOrDefault();
if (copy is null)
{
    Console.WriteLine("\nУ файлі немає коректних книг для демонстрації.");
    return 0;
}

Console.WriteLine("\n=== Сценарій 1: успіх (примірник з файлу) ===");
Console.WriteLine($"Початок:          {copy}");

copy.IssueCopy();
Loan loan = Loan.Open("L-500", copy.Id, "Reader-Ivan", DateTime.Now.AddDays(-2));
Console.WriteLine($"Після видачі:     {copy}");
Console.WriteLine($"                  {loan}");

loan.Close(DateTime.Now);
copy.ReturnCopy();
Console.WriteLine($"Після повернення: {copy}");
Console.WriteLine($"                  {loan}");

BookCopy restored = BookCopy.FromDto(copy.ToDto());
Console.WriteLine($"FromDto(ToDto):   {restored}");

Console.WriteLine("\n=== Сценарій 2: порушення інваріантів (той самий примірник) ===");

// Підготовка: знову видаємо цей самий примірник і відкриваємо нову видачу
copy.IssueCopy();
Loan active = Loan.Open("L-501", copy.Id, "Reader-Petro", DateTime.Now.AddDays(-1));
Console.WriteLine($"Підготовка: {copy}");
Console.WriteLine($"            {active}\n");

TryDo("Повторна видача виданого примірника",
      () => copy.IssueCopy());
TryDo("Порожній ISBN при створенні примірника",
      () => BookCopy.Create(copy.Id, " ", copy.Title));
TryDo("Порожній ідентифікатор читача",
      () => Loan.Open("L-502", copy.Id, " ", DateTime.Now));
TryDo("Дата видачі в майбутньому",
      () => Loan.Open("L-503", copy.Id, "Reader-Petro", DateTime.Now.AddDays(3)));
TryDo("Повернення раніше дати видачі",
      () => active.Close(active.IssuedOn.AddDays(-1)));

// Коректно закриваємо, щоб перевірити два останні правила
active.Close(DateTime.Now);
copy.ReturnCopy();

TryDo("Повторне закриття вже закритої видачі",
      () => active.Close(DateTime.Now));
TryDo("Повернення примірника, який уже в бібліотеці",
      () => copy.ReturnCopy());

Console.WriteLine("\nСтан після всіх відмов (відмови нічого не змінили):");
Console.WriteLine($"  {copy}");
Console.WriteLine($"  {active}");


Console.WriteLine("\n=== Додаткове завдання 2: ліміт відкритих видач (сервіс) ===");

// Беремо примірники з імпортованого файлу (пропускаємо перший, він використаний вище)
var demo = copiesResult.Items.Skip(1).Take(6).ToList();

if (demo.Count < 6)
{
    Console.WriteLine("У файлі менше 7 коректних примірників — демонстрацію пропущено.");
}
else
{
    const string reader = "Reader-Anna";
    var service = new LoanService();

    for (int i = 0; i < 5; i++)
        service.IssueBook(demo[i], reader, DateTime.Now);

    Console.WriteLine($"Відкритих видач у {reader}: {service.CountOpenLoans(reader)}");

    TryDo("Шоста видача тому ж читачу",
          () => service.IssueBook(demo[5], reader, DateTime.Now));
    Console.WriteLine($"    Примірник після відмови: {demo[5]}");

    service.ReturnBook(service.Loans[0].Id, demo[0], DateTime.Now);
    service.IssueBook(demo[5], reader, DateTime.Now);
    Console.WriteLine($"Після повернення однієї книги шоста видача пройшла: {demo[5]}");
}

Console.WriteLine("\n=== Додаткове завдання 3: статуси видачі та переходи (LoanStatus) ===");

string statusCopyId = copiesResult.Items[0].Id;   // ID реального примірника з файлу

var cancelled = Loan.Open("L-900", statusCopyId, "Reader-Ivan", DateTime.Now);
Console.WriteLine($"Старт:       {cancelled}");
cancelled.Cancel();
Console.WriteLine($"Скасовано:   {cancelled}");

TryDo("Закрити скасовану видачу", () => cancelled.Close(DateTime.Now));
TryDo("Скасувати вже скасовану", () => cancelled.Cancel());
TryDo("Скасувати закриту видачу", () =>
{
    var l = Loan.Open("L-901", statusCopyId, "Reader-Ivan", DateTime.Now.AddDays(-1));
    l.Close(DateTime.Now);
    l.Cancel();
});

var returned = Loan.Open("L-902", statusCopyId, "Reader-Ivan", DateTime.Now.AddDays(-1));
Console.WriteLine($"Нова:        {returned}");
returned.Close(DateTime.Now);
Console.WriteLine($"Закрита:     {returned}  [Status = {returned.Status}]");

return 0;

static void TryDo(string rule, Action action)
{
    try
    {
        action();
        Console.WriteLine($"{rule}\n    ВИНЯТОК НЕ СПРАЦЮВАВ — інваріант відсутній!");
    }
    catch (Exception ex)
    {
        string message = ex.Message.Split('\n')[0].TrimEnd();
        int p = message.IndexOf(" (Parameter", StringComparison.Ordinal);
        if (p >= 0) message = message[..p];

        Console.WriteLine($"{rule}\n    {ex.GetType().Name}: {message}");
    }
}
