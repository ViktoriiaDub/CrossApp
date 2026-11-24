using System;
using System.Collections.Generic;
using System.Linq;
using Core.Abstractions;
using Core.Domain;
using Core.Events;

namespace Core.Services;

// Dependency Injection
public sealed class LoanService(ILibraryStore store)
{
    private readonly ILibraryStore _store = store ?? throw new ArgumentNullException(nameof(store));

    public const int MaxOpenLoansPerReader = 5; // 1 читач — максимум 5 книг
    public const int DefaultLoanDays = 14;      // стандартний термін видачі (для DueDate)

    private readonly List<Loan> _loans = [];
    private int _nextLoanNumber = 1;

    // Подія сповіщення про зміни в бібліотеці
    public event EventHandler<LibraryChangedEventArgs>? Changed;

    private void OnChanged(LibraryChangeKind kind, string copyId, string? readerId = null, DateTime? dueDate = null) =>
        Changed?.Invoke(this, new LibraryChangedEventArgs(kind, copyId, readerId, dueDate));

    public IReadOnlyList<Loan> Loans => _loans.AsReadOnly();

    public int CountOpenLoans(string readerId) =>
        _loans.Count(l => l.Status == LoanStatus.Open &&
                          string.Equals(l.ReaderId, readerId?.Trim(), StringComparison.Ordinal));

    // Додавання нового примірника до сховища
    public BookCopy AddBookCopy(string isbn, string title)
    {
        var copy = BookCopy.Create(Guid.NewGuid().ToString("N")[..8], isbn, title, false);

        _store.Add(copy);

        // Піднімаємо подію ПІСЛЯ успішного збереження
        OnChanged(LibraryChangeKind.BookAdded, copy.Id);
        return copy;
    }

    public Loan IssueBook(string copyId, string readerId, DateTime issuedOn)
    {
        var copy = _store.GetById(copyId)
            ?? throw new InvalidOperationException($"Примірник з id={copyId} не знайдено у сховищі.");

        int open = CountOpenLoans(readerId);
        if (open >= MaxOpenLoansPerReader)
            throw new InvalidOperationException(
                $"Читач '{readerId}' уже має {open} відкритих видач (ліміт {MaxOpenLoansPerReader}): " +
                $"примірник {copyId} видати не можна");

        // Перевірка інваріантів у Loan.Open та BookCopy.IssueCopy
        var loan = Loan.Open($"L-{_nextLoanNumber:D4}", copy.Id, readerId, issuedOn);
        copy.IssueCopy();

        _store.Update(copy);

        _loans.Add(loan);
        _nextLoanNumber++;

        // Розраховуємо планову дату повернення (DueDate) і піднімаємо подію ПІСЛЯ всіх змін
        DateTime dueDate = issuedOn.AddDays(DefaultLoanDays);
        OnChanged(LibraryChangeKind.CopyIssued, copy.Id, loan.ReaderId, dueDate);

        return loan;
    }

    public void ReturnBook(string loanId, string copyId, DateTime returnedOn)
    {
        var copy = _store.GetById(copyId)
            ?? throw new InvalidOperationException($"Примірник з id={copyId} не знайдено.");

        var loan = GetLoan(loanId, copy);
        EnsureCopyIssued(copy);

        loan.Close(returnedOn); // якщо кине виняток — копія не змінена і події не буде
        copy.ReturnCopy();

        _store.Update(copy);

        // Піднімаємо подію ПІСЛЯ успішного повернення
        OnChanged(LibraryChangeKind.CopyReturned, copy.Id, loan.ReaderId);
    }

    public void CancelLoan(string loanId, string copyId)
    {
        var copy = _store.GetById(copyId)
            ?? throw new InvalidOperationException($"Примірник з id={copyId} не знайдено.");

        var loan = GetLoan(loanId, copy);
        EnsureCopyIssued(copy);

        loan.Cancel();
        copy.ReturnCopy();

        _store.Update(copy);

        OnChanged(LibraryChangeKind.LoanCanceled, copy.Id, loan.ReaderId);
    }

    public IReadOnlyList<BookCopy> AllBooks() => _store.List();

    public BookCopy? FindBook(string id) => _store.GetById(id);

    // Пошук за довільною умовою через Func<BookCopy, bool>
    public IReadOnlyList<BookCopy> SearchBooks(Func<BookCopy, bool> predicate) =>
        _store.Find(predicate);

    private Loan GetLoan(string loanId, BookCopy copy)
    {
        ArgumentNullException.ThrowIfNull(copy);

        var loan = _loans.FirstOrDefault(l => l.Id == loanId?.Trim())
            ?? throw new InvalidOperationException($"Видачу '{loanId}' не знайдено");

        if (loan.CopyId != copy.Id)
            throw new InvalidOperationException(
                $"Видача '{loan.Id}' стосується примірника {loan.CopyId}, а не {copy.Id}");

        return loan;
    }

    private static void EnsureCopyIssued(BookCopy copy)
    {
        if (!copy.IsIssued)
            throw new InvalidOperationException(
                $"Примірник {copy.Id} уже знаходиться у бібліотеці");
    }
}