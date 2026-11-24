using Core.Abstractions;
using Core.Converters;
using Core.Services;
using Core.Domain;
using Core.Dto;
using Core.Events;
using Core.Import;
using Core.Storage;

bool useFile = args.Contains("--file");
string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "library.json");
string path = args.FirstOrDefault(a => a != "--file") ?? Path.Combine("data", "sample.csv");

// 1. Початкові дані з імпорту
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

bool fileExists = File.Exists(dataPath);

// Ручний DI: вибір реалізації сховища
ILibraryStore store = useFile
    ? new FileLibraryStore(dataPath)
    : new InMemoryLibraryStore(initialCopies);

if (useFile && !fileExists && initialCopies.Count > 0)
{
    foreach (var initialCopy in initialCopies)
        store.Add(initialCopy);

    Console.WriteLine($"[Автоматично створено та заповнено сховище з імпорту: {initialCopies.Count} примірників]\n");
}

// Сервіс отримує сховище через конструктор
var service = new LoanService(store);

Console.WriteLine($"Сховище: {store.GetType().Name}");
Console.WriteLine($"Кількість книг у сховищі: {service.AllBooks().Count}\n");

// ЛАБОРАТОРНА РОБОТА 6: Події, делегати Action/Func, лямбди

// КРОК 3: Підписка локальним методом (вивід у консоль + запис у logs/app.log)
var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "app.log"); //dod7
Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

void OnLibraryChanged(object? sender, LibraryChangedEventArgs e)
{
    string details = e.Kind switch
    {
        LibraryChangeKind.BookAdded => "додано новий примірник до фонду",
        LibraryChangeKind.CopyIssued => $"видано читачу {e.ReaderId} до дати {e.DueDate:yyyy-MM-dd}",
        LibraryChangeKind.CopyReturned => $"повернуто читачем {e.ReaderId}",
        LibraryChangeKind.LoanCanceled => $"видачу скасовано (читач {e.ReaderId})",
        _ => string.Empty
    };

    var line = $"{e.At:HH:mm:ss} {e.Kind,-13} [{e.CopyId}] -> {details}";
    Console.WriteLine($"  [LOG] {line}");

    // Виняток у підписнику не повинен ламати бізнес-операцію в сервісі
    try
    {
        File.AppendAllText(logPath, line + Environment.NewLine);
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
        Console.WriteLine($"  [LOG] Не вдалося записати у файл: {ex.Message}");
    }
}

service.Changed += OnLibraryChanged; //dod4

// КРОК 4: Другий підписник- лямбда із замиканням над локальною змінною.
// Виводить [COUNT] після кожного [LOG], щоб було видно порядок підписки.
int changes = 0;
service.Changed += (_, _) => //dod4 
{
    changes++;
    Console.WriteLine($"  [COUNT] {changes}");
};

// КРОК 5: Сценарій роботи, перевірка лічильника та відписка (-=)
Console.WriteLine("=== Крок 5: Демонстрація подій та відписки ===");

BookCopy added1 = service.AddBookCopy("978-966-03-1234-5", "Кобзар");
Loan loan1 = service.IssueBook(added1.Id, "Reader-Ivan", DateTime.Now);
service.ReturnBook(loan1.Id, added1.Id, DateTime.Now);

Console.WriteLine($"Подій отримано (очікується 3): {changes}\n");

// Відписуємо логер консолі/файлу, залишаємо тільки лямбду-лічильник
service.Changed -= OnLibraryChanged; //dod5
Console.WriteLine("--- Виконано відписку логера (service.Changed -= OnLibraryChanged) ---");

// Тиха операція: запису в лог немає, але дані змінюються і лічильник росте
BookCopy added2 = service.AddBookCopy("978-966-03-9999-1", "Тіні забутих предків");
Loan loan2 = service.IssueBook(added2.Id, "Reader-Olena", DateTime.Now);

Console.WriteLine($"Подій отримано після тихих операцій (очікується 5): {changes}");
Console.WriteLine($"Стан книги '{added2.Title}' у сховищі: {(service.FindBook(added2.Id)?.IsIssued == true ? "видана" : "у бібліотеці")}\n");

// Повернемо підписку логера для демонстрації сценарію відмови (Крок 6)
service.Changed += OnLibraryChanged;

// КРОК 6: Сценарій відмови (порушення інваріантів- події НЕ повинно бути)
Console.WriteLine("=== Крок 6: Сценарій відмови (виняток є, події немає) ===");

TryDo("Спроба видати неіснуючий примірник",
      () => service.IssueBook("wrong-id-12345", "Reader-Oleh", DateTime.Now));

TryDo("Спроба повторно видати вже видану книгу",
      () => service.IssueBook(added2.Id, "Reader-Petro", DateTime.Now));

TryDo("Спроба додати книгу з порожнім ISBN",
      () => service.AddBookCopy("   ", "Книга без ISBN"));

TryDo("Спроба повернути книгу раніше за дату видачі",
      () => service.ReturnBook(loan2.Id, added2.Id, DateTime.Now.AddDays(-30)));

Console.WriteLine($"Лічильник подій після невдалих операцій не змінився (очікується 5): {changes}\n");

// КРОК 6.1: Скасування видачі (подія LoanCanceled)
Console.WriteLine("=== Крок 6.1: Скасування видачі ===");

BookCopy added3 = service.AddBookCopy("978-966-03-7777-7", "Беззоряне море");
Loan loan3 = service.IssueBook(added3.Id, "Reader-Anna", DateTime.Now);
service.CancelLoan(loan3.Id, added3.Id);

Console.WriteLine($"Подій отримано (очікується 8): {changes}");
Console.WriteLine($"Стан книги '{added3.Title}': {(service.FindBook(added3.Id)?.IsIssued == true ? "видана" : "у бібліотеці")}\n");

// КРОК 7: Делегат Func<BookCopy, bool> замість події (повернення значення)
Console.WriteLine("=== Крок 7: Пошук через Func<BookCopy, bool> ===");

var issuedBooks = service.SearchBooks(b => b.IsIssued);
Console.WriteLine($"Зараз видано книг читачам: {issuedBooks.Count}");
foreach (var book in issuedBooks)
{
    Console.WriteLine($"   - {book}");
}

var availableBooks = service.SearchBooks(b => !b.IsIssued);
Console.WriteLine($"Вільних книг у бібліотеці: {availableBooks.Count}");

Console.WriteLine($"\nФайл журналу збережено за шляхом: {logPath}");
Console.WriteLine("=== Кінець роботи програми ===");
return 0;

// Допоміжний метод для перевірки винятків
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

        Console.WriteLine($"[Очікувана помилка] {rule} -> {ex.GetType().Name}: {message}");
    }
}