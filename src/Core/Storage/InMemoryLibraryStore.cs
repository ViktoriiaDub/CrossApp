using System;
using System.Collections.Generic;
using System.Linq;
using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;
 
 //оголошення класу +конструтор
public sealed class InMemoryLibraryStore(IEnumerable<BookCopy>? seed = null) : ILibraryStore
{
    //внутрішнє сховище
    private readonly Dictionary<string, BookCopy> _items =
        (seed ?? []).ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<BookCopy> List() => _items.Values.ToList();
    //читання даних
    public BookCopy? GetById(string id) => _items.GetValueOrDefault(id);
    //зміна даних
    public void Update(BookCopy item) => _items[item.Id] = item;

    public bool Remove(string id) => _items.Remove(id);

    public void Add(BookCopy item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (_items.ContainsKey(item.Id))
            throw new InvalidOperationException($"Запис з id={item.Id} уже існує.");
        _items.Add(item.Id, item);
    }

    public IReadOnlyList<BookCopy> Find(Func<BookCopy, bool> predicate)
{
    ArgumentNullException.ThrowIfNull(predicate);
    return _items.Values.Where(predicate).ToList();
}
}