using Core.Domain; 
namespace Core.Abstractions;

public interface ILibraryStore //оголошення інтерфейсу
{
    IReadOnlyList<BookCopy> List(); //повернення записів зі сховища (перегляд без зміни)
    BookCopy? GetById(string id); //пошук примірника за ідентифікатором 
    void Add(BookCopy item);//дод. об'єкт BookCopy у сховище і перевіряє id на унікальність
    void Update(BookCopy item); // зберігає новий стан існуючого об'єкта 
    bool Remove(string id); //видалення за id, успіх- true, ні-false

    IReadOnlyList<BookCopy> Find(Func<BookCopy, bool> predicate); //пошук за делегатом (умова перед. як ф-ція)
}