using Core.Domain;

namespace Core.Services;

public sealed class LoanService
{
    public const int MaxOpenLoansPerReader = 5;

    private readonly List<Loan> _loans = [];
    private int _nextLoanNumber = 1;

    public IReadOnlyList<Loan> Loans => _loans.AsReadOnly();

    public int CountOpenLoans(string readerId) =>
        _loans.Count(l => l.Status == LoanStatus.Open &&
                          string.Equals(l.ReaderId, readerId?.Trim(), StringComparison.Ordinal));

    public Loan IssueBook(BookCopy copy, string readerId, DateTime issuedOn)
    {
        ArgumentNullException.ThrowIfNull(copy);

        // створення видачі: перевіряє аргументи без стану
        var loan = Loan.Open($"L-{_nextLoanNumber:D4}", copy.Id, readerId, issuedOn);

        // охоплення двох сутностей / набір видач
        int open = CountOpenLoans(loan.ReaderId);
        if (open >= MaxOpenLoansPerReader)
            throw new InvalidOperationException(
                $"Читач '{loan.ReaderId}' уже має {open} відкритих видач (ліміт {MaxOpenLoansPerReader}): " +
                $"примірник {copy.Id} видати не можна");

        //зміна стану: якщо копія вже видана то виняток, нічого не збережено
        copy.IssueCopy();

        _loans.Add(loan);
        _nextLoanNumber++;
        return loan;
    }

    public void ReturnBook(string loanId, BookCopy copy, DateTime returnedOn)
    {
        var loan = GetLoan(loanId, copy);
        EnsureCopyIssued(copy);

        loan.Close(returnedOn);   // якщо кине виняток то копія не змінена
        copy.ReturnCopy();
    }

    public void CancelLoan(string loanId, BookCopy copy)
    {
        var loan = GetLoan(loanId, copy);
        EnsureCopyIssued(copy);

        loan.Cancel();
        copy.ReturnCopy();
    }

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