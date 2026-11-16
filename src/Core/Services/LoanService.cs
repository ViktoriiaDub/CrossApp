using System;
using System.Collections.Generic;
using System.Linq;
using Core.Abstractions;
using Core.Domain;

namespace Core.Services;
//Dependency Injection
public sealed class LoanService(ILibraryStore store)
{
    //збереження переданого сховища у приватнре поле, якщо перпедано null-> помилка
    private readonly ILibraryStore _store = store ?? throw new ArgumentNullException(nameof(store));

    public const int MaxOpenLoansPerReader = 5;//константа, 1 читач- максимум 5 книг

    private readonly List<Loan> _loans = []; //тимчасови список усіх видач у пам"яті сервісу+лічильник
    private int _nextLoanNumber = 1;

    public IReadOnlyList<Loan> Loans => _loans.AsReadOnly(); //ззовні можна подивитись список видач без змін напряму 

    public int CountOpenLoans(string readerId) =>
        _loans.Count(l => l.Status == LoanStatus.Open &&
                          string.Equals(l.ReaderId, readerId?.Trim(), StringComparison.Ordinal));

    // Додавання нового примірника до сховища (через сервіс)
    public BookCopy AddBookCopy(string isbn, string title)
    {
        // IsIssued = false, бо примірник новий і ще не виданий
        var copy = BookCopy.Create(Guid.NewGuid().ToString("N")[..8], isbn, title, false);

        _store.Add(copy);
        return copy;
    }

    public Loan IssueBook(string copyId, string readerId, DateTime issuedOn)
    {
        // примірник зі сховища за його ID
        var copy = _store.GetById(copyId)
            ?? throw new InvalidOperationException($"Примірник з id={copyId} не знайдено у сховищі.");

        // перевірка ліміту відкритих видач на читача
        int open = CountOpenLoans(readerId);
        if (open >= MaxOpenLoansPerReader)
            throw new InvalidOperationException(
                $"Читач '{readerId}' уже має {open} відкритих видач (ліміт {MaxOpenLoansPerReader}): " +
                $"примірник {copyId} видати не можна");

        // створення видачі- Loan.Open перевіряє інваріанти (читач, дата).
        // якщо кине виняток- примірник  не змінений і нічого не збережено.
        var loan = Loan.Open($"L-{_nextLoanNumber:D4}", copy.Id, readerId, issuedOn);

        //зміна стану після успішної валідації
        copy.IssueCopy();

        //збереження оновленого стану примірника у сховище
        _store.Update(copy);

        // реєстрація видачі
        _loans.Add(loan);
        _nextLoanNumber++;
        return loan;
    }

    public void ReturnBook(string loanId, string copyId, DateTime returnedOn)
    {
        var copy = _store.GetById(copyId)
            ?? throw new InvalidOperationException($"Примірник з id={copyId} не знайдено.");

        var loan = GetLoan(loanId, copy);
        EnsureCopyIssued(copy);

        loan.Close(returnedOn);   // якщо кине виняток- копія не змінена
        copy.ReturnCopy();

        _store.Update(copy);
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
    }

    // Допоміжні методи для отримання списку або пошуку через сховище
    public IReadOnlyList<BookCopy> AllBooks() => _store.List();

    public BookCopy? FindBook(string id) => _store.GetById(id);

    // Додаткове завдання 2: пошук за довільною умовою
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