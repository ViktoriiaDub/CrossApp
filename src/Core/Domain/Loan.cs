using Core.Dto;
namespace Core.Domain;

public sealed class Loan
{
    public string Id { get; }
    public string CopyId { get; }
    public string ReaderId { get; }
    public DateTime IssuedOn { get; }
    public DateTime? ReturnedOn { get; private set; }
    public LoanStatus Status { get; private set; }

    private Loan(string id, string copyId, string readerId, DateTime issuedOn)
    {
        Id = id;
        CopyId = copyId;
        ReaderId = readerId;
        IssuedOn = issuedOn;
        ReturnedOn = null;
        Status = LoanStatus.Open;
    }

    //фабричний метод
    public static Loan Open(string id, string copyId, string readerId, DateTime issuedOn)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор видачі не може бути порожнім", nameof(id));

        if (string.IsNullOrWhiteSpace(copyId))
            throw new ArgumentException("Ідентифікатор примірника не може бути порожнім", nameof(copyId));

        if (string.IsNullOrWhiteSpace(readerId))
            throw new ArgumentException("Ідентифікатор читача не може бути порожнім", nameof(readerId));

        if (issuedOn > DateTime.Now.AddMinutes(5))
            throw new ArgumentOutOfRangeException(nameof(issuedOn), issuedOn, "Дата видачі не може бути в майбутньому.");

        return new Loan(id.Trim(), copyId.Trim(), readerId.Trim(), issuedOn);
    }

    //повернення книги (з open у closed)
    public void Close(DateTime returnedOn)
    {
        EnsureCanMoveTo(LoanStatus.Closed);

        if (returnedOn < IssuedOn)
            throw new ArgumentOutOfRangeException(nameof(returnedOn), returnedOn,
                $"Дата повернення {returnedOn:dd-MM-yyyy} раніша за дату видачі {IssuedOn:dd-MM-yyyy}");

        //зміна стану після перевірок
        ReturnedOn = returnedOn;
        Status = LoanStatus.Closed;
    }

    // Скасування помилкової видачі
    public void Cancel()
    {
        EnsureCanMoveTo(LoanStatus.Cancelled);
        Status = LoanStatus.Cancelled;
    }

    //перевірка допустимості переходу
    private void EnsureCanMoveTo(LoanStatus target)
    {
        string? error = (Status, target) switch
        {
            (LoanStatus.Open, LoanStatus.Closed) or
            (LoanStatus.Open, LoanStatus.Cancelled)
                => null, // дозволені переходи

            (LoanStatus.Closed, LoanStatus.Closed)
                => $"Видачу '{Id}' уже закрито {ReturnedOn:yyyy-MM-dd}, повторно закрити не можна",

            (LoanStatus.Cancelled, _)
                => $"Видачу '{Id}' скасовано, подальші зміни неможливі",

            (LoanStatus.Closed, _)
                => $"Видачу '{Id}' закрито, перехід у стан {target} неможливий",

            _ => $"Недопустимий перехід видачі '{Id}': {Status} → {target}"
        };

        if (error is not null)
            throw new InvalidOperationException(error);
    }

    public LoanDto ToDto() => new(Id, CopyId, ReaderId, IssuedOn, ReturnedOn, Status);

    // FromDto відновлює об'єкт ЛИШЕ через ті самі методи, що й звичайна робота
    public static Loan FromDto(LoanDto dto)
    {
        var loan = Open(dto.Id, dto.CopyId, dto.ReaderId, dto.IssuedOn);

        switch (dto.Status)
        {
            case LoanStatus.Open when dto.ReturnedOn.HasValue:
                throw new ArgumentException($"Видача '{dto.Id}' відкрита, але має дату повернення", nameof(dto));

            case LoanStatus.Open:
                break;

            case LoanStatus.Closed when !dto.ReturnedOn.HasValue:
                throw new ArgumentException($"Видача '{dto.Id}' закрита, але без дати повернення", nameof(dto));

            case LoanStatus.Closed:
                loan.Close(dto.ReturnedOn!.Value);
                break;

            case LoanStatus.Cancelled when dto.ReturnedOn.HasValue:
                throw new ArgumentException($"Видача '{dto.Id}' скасована, але має дату повернення", nameof(dto));

            case LoanStatus.Cancelled:
                loan.Cancel();
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(dto), dto.Status, "Невідомий статус видачі");
        }

        return loan;
    }

    public override string ToString() =>
        $"Видача {Id}: примірник {CopyId}, читач {ReaderId}, з {IssuedOn:dd-MM-yyyy} " +
        Status switch
        {
            LoanStatus.Open => "(відкрита)",
            LoanStatus.Closed => $"по {ReturnedOn:dd-MM-yyyy} (закрита)",
            LoanStatus.Cancelled => "(скасована)",
            _ => ""
        };
}