using Core.Dto;
using Core.Import;

string path = args.Length > 0 ? args[0] : Path.Combine("data", "sample.csv");

if (!File.Exists(path))
{
    Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
    return 1;
}

string extension = Path.GetExtension(path).ToLowerInvariant();

ImportResult<BookDto> result = extension switch
{
    ".csv" => BookCsvImporter.Load(path),
    ".json" => BookJsonImporter.Load(path),
    _ => new ImportResult<BookDto>([], [$"Непідтримуваний формат файлу: {extension}"])
};

Console.WriteLine($"Завантажено записів: {result.Items.Count}");
foreach (BookDto b in result.Items.Take(5))
{
    Console.WriteLine($" {b.Id,-6} {b.Isbn,-18} {b.Title,-35} {b.Year,5}");
}

if (result.Errors.Count > 0)
{
    Console.WriteLine($"Пропущено рядків: {result.Errors.Count}");
    foreach (string e in result.Errors)
    {
        Console.WriteLine($" ! {e}");
    }
}

int totalAttempted = result.Items.Count + result.Errors.Count;
double errorRate = totalAttempted > 0 ? (double)result.Errors.Count / totalAttempted * 100 : 0;

Console.WriteLine($"\n[Статистика імпорту]: Усього: {totalAttempted} | Успішно: {result.Items.Count} | Пропущено: {result.Errors.Count} | Помилки: {errorRate:F1}%");

return 0;