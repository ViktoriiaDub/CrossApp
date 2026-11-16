using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto; // Простір імен, де лежить BookDto

namespace Core.Storage;

public sealed class FileLibraryStore(string path) : ILibraryStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true }; //форматування джисон
    private readonly Dictionary<string, BookCopy> _cache = new(StringComparer.OrdinalIgnoreCase); //словник у пам'яті де зберігаються завантажені книги
    private readonly string _path = Path.GetFullPath(path); //повний шлях до файлу json
    private bool _loaded; //прапорець стану зчитування файлів із диска в _cache

  private void EnsureLoaded()
{
    if (_loaded) return; //якщо зчитувався файл метод завершується 
    if (File.Exists(_path)) 
    {
        // Змінили BookDto на BookCopyDto:
        var dtos = JsonSerializer.Deserialize<List<BookCopyDto>>(File.ReadAllText(_path)) ?? [];
        foreach (var dto in dtos) //кожен dto на сутність bookcopy  і у словнник _cache 
        { 
            var p = BookCopy.FromDto(dto); 
            _cache[p.Id] = p; 
        }
    }
    _loaded = true;
}
    //збереження на диск
    private void Flush()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path,
            JsonSerializer.Serialize(_cache.Values.Select(p => p.ToDto()).ToList(), Options)); // Мапінг у DTO для збереження на диск
    }
//публічні методи контракту ILibraryStore
    public IReadOnlyList<BookCopy> List() //викликає ensure і бере дані з _cache
    {
        EnsureLoaded();
        return _cache.Values.ToList();
    }

    public BookCopy? GetById(string id) //те саме
    {
        EnsureLoaded();
        return _cache.GetValueOrDefault(id);
    }
    //спочатку викликають ensure тоді змінюють _cache і викликають flash щоб зберегти зміни 
    public void Add(BookCopy item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureLoaded();
        if (_cache.ContainsKey(item.Id))
            throw new InvalidOperationException($"Запис з id={item.Id} уже існує.");
        _cache.Add(item.Id, item);
        Flush();
    }

    public void Update(BookCopy item)
    {
        EnsureLoaded();
        _cache[item.Id] = item;
        Flush();
    }

    public bool Remove(string id)
    {
        EnsureLoaded();
        if (!_cache.Remove(id)) return false;
        Flush();
        return true;
    }
    // ще раз використовує List і фільтрує результат
    public IReadOnlyList<BookCopy> Find(Func<BookCopy, bool> predicate)
{
    ArgumentNullException.ThrowIfNull(predicate);
    return List().Where(predicate).ToList();
}
}