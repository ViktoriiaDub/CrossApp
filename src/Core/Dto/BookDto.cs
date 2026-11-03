using Core.Domain;

namespace Core.Dto;

public record BookDto(
    string Id,
    string Isbn,
    string Title,
    int Year);

// для BookCopy.ToDto / FromDto
public record BookCopyDto(
    string Id,
    string Isbn,
    string Title,
    bool IsIssued
);

// для Loan.ToDto / FromDto
public record LoanDto(
    string Id,
    string CopyId,
    string ReaderId,
    DateTime IssuedOn,
    DateTime? ReturnedOn,
    LoanStatus Status=LoanStatus.Open
);  