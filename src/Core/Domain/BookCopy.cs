using Core.Dto;

namespace Core.Domain;

public sealed class BookCopy
{
    public string Id { get; }
    public string Isbn { get; }
    public string Title { get; }
    public bool IsIssued { get; private set; }

    // Приватний конструктор 
    private BookCopy(string id, string isbn, string title, bool isIssued)
    {
        Id = id;
        Isbn = isbn;
        Title = title;
        IsIssued = isIssued;
    }

    // Статичний фабричний метод з перевіркою інваріантів
    public static BookCopy Create(string id, string isbn, string title, bool isIssued = false)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор примірника не може бути порожнім", nameof(id));

        if (string.IsNullOrWhiteSpace(isbn))
            throw new ArgumentException("ISBN не може бути порожнім", nameof(isbn));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Назва книги не може бути порожньою", nameof(title));

        return new BookCopy(
            id.Trim(), 
            isbn.Trim().ToUpperInvariant(), 
            title.Trim(), 
            isIssued
        );
    }

    // Метод зміни стану: видача примірника
    public void IssueCopy()
    {
        if (IsIssued)
            throw new InvalidOperationException($"Примірник книги із ID '{Id}' вже виданий читачеві.");

        IsIssued = true;
    }

    // Метод зміни стану: повернення примірника
    public void ReturnCopy()
    {
        if (!IsIssued)
            throw new InvalidOperationException($"Примірник книги із ID '{Id}' вже знаходиться у бібліотеці.");

        IsIssued = false;
    }

    //перетворення сутності у DTO
    public BookCopyDto ToDto() => new(Id, Isbn, Title, IsIssued);
    public static BookCopy FromDto(BookCopyDto dto) =>
        Create(dto.Id, dto.Isbn, dto.Title, dto.IsIssued);
    public override string ToString() =>
    $"{Id} [{Isbn}] {Title} — {(IsIssued ? "видана" : "у бібліотеці")}";

}
