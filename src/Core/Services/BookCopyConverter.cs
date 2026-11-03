using Core.Domain;
using Core.Dto;

namespace Core.Converters;

public static class BookCopyConverter
{
    public static ImportResult<BookCopy> Convert(ImportResult<BookDto> bookDtoResult)
    {
        var validCopies = new List<BookCopy>();
        // перенесення вже знайдених помилок з файлу 
        var errors = new List<string>(bookDtoResult.Errors);

        int counter = 1;
        foreach (var dto in bookDtoResult.Items)
        {
            try
            {
                // Перевірка до формування ID примірника
                if (string.IsNullOrWhiteSpace(dto.Id))
                    throw new ArgumentException("Ідентифікатор книги в файлі порожній", nameof(dto.Id));

                // Формуємо унікальний ID для копії на основі ID книги з файлу
                string copyId = $"COPY-{dto.Id}-{counter++}";

                // створення доменної сутності через фабричний метод (спрацьовують інваріанти)
                var copy = BookCopy.Create(copyId, dto.Isbn, dto.Title);

                validCopies.Add(copy);
            }
            catch (Exception ex)
            {
                // ловить виняток і додє до загального звіту помилок, не зупиняючи процес
                errors.Add($"Рядок з ID '{dto.Id}': {ex.Message}");
            }
        }

        return new ImportResult<BookCopy>(validCopies, errors);
    }
}