using System.Text.Json;
using Core.Dto;

namespace Core.Import;

public static class BookJsonImporter
{
    public static ImportResult<BookDto> Load(string path)
    {
        try
        {
            string json = File.ReadAllText(path, System.Text.Encoding.UTF8);
            using JsonDocument doc = JsonDocument.Parse(json);

            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return new ImportResult<BookDto>([], ["JSON-файл має містити масив об'єктів"]);
            }

            List<BookDto> items = [];
            List<string> errors = [];
            int index = 0;

            foreach (JsonElement element in doc.RootElement.EnumerateArray())
            {
                index++;
                try
                {
                    // Безпечно зчитуємо поля
                    string id = element.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "";
                    string isbn = element.TryGetProperty("isbn", out var isbnProp) ? isbnProp.GetString() ?? "" : "";
                    string title = element.TryGetProperty("title", out var titleProp) ? titleProp.GetString() ?? "" : "";

                    // Перевірки на порожні поля
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        errors.Add($"об'єкт {index}: ID порожній");
                        continue;
                    }
                    if (string.IsNullOrWhiteSpace(isbn))
                    {
                        errors.Add($"об'єкт {index}: ISBN порожній");
                        continue;
                    }
                    if (string.IsNullOrWhiteSpace(title))
                    {
                        errors.Add($"об'єкт {index}: Назва книги порожня");
                        continue;
                    }

                    // Перевірка року
                    if (element.TryGetProperty("year", out var yearProp) && yearProp.TryGetInt32(out int year))
                    {
                        items.Add(new BookDto(id, isbn, title, year));
                    }
                    else
                    {
                        string wrongYear = yearProp.ValueKind != JsonValueKind.Undefined ? yearProp.ToString() : "відсутній";
                        errors.Add($"об'єкт {index}: рік '{wrongYear}' має бути коректним числом");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"об'єкт {index}: помилка структури ({ex.Message})");
                }
            }

            return new ImportResult<BookDto>(items, errors);
        }
        catch (Exception ex)
        {
            return new ImportResult<BookDto>([], [$"Помилка читання файлу JSON: {ex.Message}"]);
        }
    }
}