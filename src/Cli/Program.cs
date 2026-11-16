using Core.Abstractions;
using Core.Converters;
using Core.Services;
using Core.Domain;
using Core.Dto;
using Core.Import;
using Core.Storage;


bool useFile = args.Contains("--file");
string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "library.json");
//шлях до файлу
string path = args.FirstOrDefault(a => a != "--file") ?? Path.Combine("data", "sample.csv");

// початкові дані з імпорту sample.csv+конвертація у доменні об'єкти
List<BookCopy> initialCopies = [];
if (File.Exists(path))
{
    string extension = Path.GetExtension(path).ToLowerInvariant();
    ImportResult<BookDto> fileResult = extension switch
    {
        ".csv" => BookCsvImporter.Load(path),
        ".json" => BookJsonImporter.Load(path),
        _ => new ImportResult<BookDto>([], [$"непідтримуваний формат файлу '{extension}'"])
    };

    var copiesResult = BookCopyConverter.Convert(fileResult);
    initialCopies = copiesResult.Items.ToList();

    if (copiesResult.Errors.Count > 0)
    {
        Console.WriteLine($"[Помилки імпорту ({copiesResult.Errors.Count}):]");
        foreach (string error in copiesResult.Errors)
            Console.WriteLine($"   - {error}");
        Console.WriteLine();
    }
}
else
{
    Console.WriteLine($"[Файл імпорту '{path}' не знайдено]\n");
}

// чи існує файл сховища до його створення
bool fileExists = File.Exists(dataPath);

//ручний DI: вибір реалізації сховища
ILibraryStore store = useFile
    ? new FileLibraryStore(dataPath)
    : new InMemoryLibraryStore(initialCopies);

// Файловий режим, а library.json ще немає — заповнюємо його даними з імпорту
if (useFile && !fileExists && initialCopies.Count > 0)
{
    foreach (var initialCopy in initialCopies)
    {
        store.Add(initialCopy);
    }
    Console.WriteLine($"[Автоматично створено та заповнено сховище з імпорту: {initialCopies.Count} примірників]\n");
}

//сервіс отримує сховище через конструктор
var service = new LoanService(store);

Console.WriteLine($" Сховище: {store.GetType().Name} ");
Console.WriteLine($"Кількість книг у сховищі: {service.AllBooks().Count}");

//сценарій 1, операції через loanservice 
Console.WriteLine("\n=== Сценарій 1: додати → знайти за id → змінити → список ===");

//lодати
BookCopy added = service.AddBookCopy("978-966-03-1234-5", "Тестова книга для демонстрації");
Console.WriteLine($"1. Додано:            {added}");

// pнайти за id
BookCopy? found = service.FindBook(added.Id);
Console.WriteLine($"2. Знайдено за id:    {found?.ToString() ?? "не знайдено"}");

// pмінити (видача, потім повернення)
Loan loan = service.IssueBook(added.Id, "Reader-Ivan", DateTime.Now.AddDays(-2));
Console.WriteLine($"3. Після видачі:      {service.FindBook(added.Id)}");
Console.WriteLine($"                      {loan}");

service.ReturnBook(loan.Id, added.Id, DateTime.Now);
Console.WriteLine($"   Після повернення:  {service.FindBook(added.Id)}");
Console.WriteLine($"                      {loan}");

// показати список
Console.WriteLine("4. Усі примірники у сховищі:");
foreach (BookCopy item in service.AllBooks())
{
    Console.WriteLine($"   {item}");
}

//сценарій 2 лаб 4- порушення інваріантів
Console.WriteLine("\nСценарій 2: порушення інваріантів");

BookCopy sample = BookCopy.Create("DEMO-1", "978-0-00-000000-0", "Демонстраційний примірник");
sample.IssueCopy();
Loan active = Loan.Open("L-DEMO", sample.Id, "Reader-Petro", DateTime.Now.AddDays(-1));
Console.WriteLine($"Підготовка: {sample}");
Console.WriteLine($"            {active}\n");

TryDo("Повторна видача виданого примірника",
      () => sample.IssueCopy());
TryDo("Порожній ISBN при створенні примірника",
      () => BookCopy.Create(sample.Id, " ", sample.Title, false));
TryDo("Порожній ідентифікатор читача",
      () => Loan.Open("L-DEMO-2", sample.Id, " ", DateTime.Now));
TryDo("Дата видачі в майбутньому",
      () => Loan.Open("L-DEMO-3", sample.Id, "Reader-Petro", DateTime.Now.AddDays(3)));
TryDo("Повернення раніше дати видачі",
      () => active.Close(active.IssuedOn.AddDays(-1)));

//ліміт видач
Console.WriteLine("\nЛіміт відкритих видач (сервіс) ");

var free = service.AllBooks()
                  .Where(b => !b.IsIssued)
                  .Take(6)
                  .ToList();

if (free.Count < 6)
{
    Console.WriteLine("У сховищі менше 6 вільних примірників - демонстрацію ліміту пропущено.");
}
else
{
    const string reader = "Reader-Anna";
    var annLoans = new List<Loan>();

    try
    {
        for (int i = 0; i < MaxLoans(); i++)
        {
            annLoans.Add(service.IssueBook(free[i].Id, reader, DateTime.Now));
        }

        Console.WriteLine($"Відкритих видач у {reader}: {service.CountOpenLoans(reader)}");

        TryDo("Шоста видача тому ж читачу",
              () => service.IssueBook(free[5].Id, reader, DateTime.Now));

        // Повертаємо першу книгу -ліміт звільняється
        service.ReturnBook(annLoans[0].Id, free[0].Id, DateTime.Now);
        annLoans.Add(service.IssueBook(free[5].Id, reader, DateTime.Now));
        Console.WriteLine($"Після повернення однієї книги шоста видача пройшла успішно для примірника: {free[5].Id}");
    }
    finally
    {
        //повернення  відкритих видач Reader-Anna, щоб не лишалось прим. без видач
        foreach (Loan open in annLoans.Where(x => x.Status == LoanStatus.Open).ToList())
        {
            service.ReturnBook(open.Id, open.CopyId, DateTime.Now);
        }
        Console.WriteLine($"Прибирання: відкритих видач у {reader}: {service.CountOpenLoans(reader)}");
    }
}

//сценарій відмови 
Console.WriteLine("\n КРОК 6: Сценарій відмови (try/catch)");

// додавання у сховище об'єкта із дублюючим ID
try
{
    BookCopy? existingBook = service.AllBooks().FirstOrDefault();
    if (existingBook != null)
    {
        var duplicateBook = BookCopy.Create(existingBook.Id, "978-966-00-0000-0", "Книга-Дублікат", false);
        store.Add(duplicateBook);
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[Успішно перехоплено помилку сховища]: {ex.Message}");
}

// операція з неіснуючим ID
try
{
    service.IssueBook("wrong-id-12345", "Reader-Oleh", DateTime.Now);
}
catch (Exception ex)
{
    Console.WriteLine($"[Успішно перехоплено помилку сервісу]: {ex.Message}");
}

//додаткове 2
Console.WriteLine("\n=== Додаткове завдання 2: пошук з Func<BookCopy, bool> ===");

//всі вільні примірники
var availableBooks = service.SearchBooks(b => !b.IsIssued);
Console.WriteLine($"Вільних книг у бібліотеці: {availableBooks.Count}");

//за частиною назви 
var specificBooks = service.SearchBooks(b => b.Title.Contains("Вавилон", StringComparison.OrdinalIgnoreCase));
if (specificBooks.Count == 0)
{
    Console.WriteLine("За назвою 'Вавилон' нічого не знайдено.");
}
foreach (BookCopy book in specificBooks)
{
    Console.WriteLine($"Знайдено за назвою: {book}");
}

Console.WriteLine("\n=== Кінець роботи програми ===");
return 0;
//допоміжні
static int MaxLoans() => LoanService.MaxOpenLoansPerReader;

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