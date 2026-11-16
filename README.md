# CrossApp

Наскрізний проєкт із дисципліни «Крос-платформне програмування».

- **Студентка**: Дуб Вікторія, група ФЕІ-32с
- **Предметна область**: Бібліотека (видання, примірники, читачі, видачі)

---

## Лабораторна робота №3: Базові типи домену (Records, Pattern Matching, CSV/JSON Importers)

У межах лабораторної роботи реалізовано:

1. **Доменні моделі**: незмінні записи (`record`) `BookDto` та універсальний контейнер результатів `ImportResult<T>`.
2. **Стійкі імпортери даних**:
   - `BookCsvImporter`: розбір CSV-файлів із роздільником `;`, захистом через `CultureInfo.InvariantCulture`, пропуском заголовків і рядковою обробкою помилок.
   - `BookJsonImporter`: аналіз масивів об'єктів у форматі JSON за допомогою `JsonDocument` з поелементним виловлюванням аномалій без зупинки програми.
3. **Кросплатформний CLI-інтерфейс**: динамічний вибір імпортера за розширенням файлу (`.csv` / `.json`) через `switch expression`, захист від відсутності файлу (`File.Exists` + `Path.GetFullPath`) та розширений підрахунок статистики імпорту.

## Лабораторна робота №4: Доменна модель (сутності, інваріанти, інкапсуляція)

### Перелік інваріантів

**BookCopy**

- Ідентифікатор примірника не порожній -`ArgumentException` - `BookCopy.Create`
- ISBN не порожній - `ArgumentException` - `BookCopy.Create`
- Назва книги не порожня - `ArgumentException` - `BookCopy.Create`
- Не можна видати вже виданий примірник - `InvalidOperationException` - `BookCopy.IssueCopy`
- Не можна повернути примірник, який уже в бібліотеці - `InvalidOperationException` - `BookCopy.ReturnCopy`

**Loan**

- Ідентифікатори видачі, примірника та читача не порожні - `ArgumentException` - `Loan.Open`
- Дата видачі не може бути в майбутньому - `ArgumentOutOfRangeException` - `Loan.Open`
- Дата повернення не раніша за дату видачі - `ArgumentOutOfRangeException` - `Loan.Close`
- Допустимі переходи стану лише `Open → Closed` та `Open → Cancelled`; закриту чи скасовану видачу змінити не можна — `InvalidOperationException` - `Loan.EnsureCanMoveTo` (викликається з `Close` і `Cancel`)
- Стан і дата повернення в DTO узгоджені (закрита видача має дату, відкрита й скасована не мають) - `ArgumentException` - `Loan.FromDto`

**LoanService (правила між сутностями)**

- Читач має не більше 5 відкритих видач - `InvalidOperationException` - `LoanService.IssueBook`
- Повернути чи скасувати можна лише існуючу видачу, що стосується цього примірника, і лише коли примірник виданий - `InvalidOperationException` - `LoanService.ReturnBook` / `CancelLoan`

**BookCopyConverter**

- Ідентифікатор книги у файлі не порожній - `ArgumentException` - `BookCopyConverter.Convert`

### Додаткові завдання

1. **Імпорт → сутності + помилки**: `BookCopyConverter.Convert`.
2. **Інваріант на дві сутності** (ліміт 5 відкритих видач): `LoanService.IssueBook`. Правило винесено в сервіс, бо `Loan` не знає про інші видачі читача, а ліміт є політикою бібліотеки, а не властивістю однієї видачі.
3. **Стан-перелічування й переходи**: `LoanStatus` та перевірка переходів через `switch` expression в `Loan.EnsureCanMoveTo`.

---

## Лабораторна робота №5: Сервісний шар (інтерфейси, два сховища, ручний DI)

### Ідея

Сервіс не залежить від конкретного способу зберігання даних: він працює з абстракцією сховища. Конкретна реалізація (пам'ять або файл) обирається в одному місці - `Program.cs` (composition root) - за допомогою ручного впровадження залежностей через конструктор, без DI-контейнера.

### Назви компонентів

| Компонент | Назва | Розташування |
| --- | --- | --- |
| Інтерфейс сховища | `ILibraryStore` | `Core/Abstractions/ILibraryStore.cs` |
| Реалізація в пам'яті | `InMemoryLibraryStore` | `Core/Storage/InMemoryLibraryStore.cs` |
| Файлова реалізація | `FileLibraryStore` | `Core/Storage/FileLibraryStore.cs` |
| Сервіс | `LoanService` | `Core/Services/LoanService.cs` |
| Composition root | `Program.cs` | `Cli/Program.cs` |

### Контракт `ILibraryStore`

| Метод | Призначення |
| --- | --- |
| `IReadOnlyList<BookCopy> List()` | усі примірники; `IReadOnlyList`, щоб зовнішній код не змінював сховище в обхід `Add`/`Update`/`Remove` |
| `BookCopy? GetById(string id)` | пошук за id; `null`, якщо запису немає |
| `void Add(BookCopy item)` | додати примірник; дубль id → `InvalidOperationException` |
| `void Update(BookCopy item)` | зберегти змінений стан примірника |
| `bool Remove(string id)` | видалити примірник; `false`, якщо його не було |
| `IReadOnlyList<BookCopy> Find(Func<BookCopy, bool> predicate)` | пошук за довільною умовою (додаткове завдання 2) |

Інтерфейс містить лише доменні операції; файлової специфіки (`Save`, `Flush`) у контракті немає.

### Дві реалізації

- **`InMemoryLibraryStore`** - словник `Dictionary<string, BookCopy>` (ключі без урахування регістру) з початковими даними через конструктор. Після завершення програми дані зникають.
- **`FileLibraryStore`** - кеш у пам'яті, дозавантаження з файлу при першому зверненні та запис на диск після кожної зміни (`Add`/`Update`/`Remove`). На диск записуються DTO (`BookCopyDto`), а не сутності: мапінг робить `BookCopy.ToDto` / `BookCopy.FromDto`, тому дані з файлу проходять ті самі інваріанти (`BookCopy.Create`).
  - **Файл**: `library.json` у каталозі `data` поруч зі збіркою (`AppContext.BaseDirectory/data/library.json`), формат - JSON-масив `BookCopyDto` з відступами.

### Сервіс `LoanService`

Конструктор приймає `ILibraryStore` (інтерфейс, а не клас). Бізнес-операції:

| Операція | Опис |
| --- | --- |
| `AddBookCopy(isbn, title)` | створює примірник (`BookCopy.Create`) і додає його у сховище |
| `IssueBook(copyId, readerId, issuedOn)` | видача примірника: перевірка ліміту, створення `Loan`, зміна стану примірника, `Update` у сховищі |
| `ReturnBook(loanId, copyId, returnedOn)` | повернення примірника |
| `CancelLoan(loanId, copyId)` | скасування помилкової видачі |
| `AllBooks()` / `FindBook(id)` | список і пошук за id |
| `SearchBooks(predicate)` | пошук за `Func<BookCopy, bool>` (додаткове завдання 2) |

Список видач (`Loans`) зберігається лише в пам'яті сервісу й між запусками не персистентний; сховище містить лише примірники. Персистентність видач передбачено на наступних етапах.

### Схема залежностей

```text
Cli (Program.cs) → LoanService → ILibraryStore → InMemoryLibraryStore | FileLibraryStore
```

Конкретні класи сховищ створюються тільки в `Program.cs`:

```csharp
bool useFile = args.Contains("--file");
string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "library.json");

ILibraryStore store = useFile
    ? new FileLibraryStore(dataPath)
    : new InMemoryLibraryStore(initialCopies);

var service = new LoanService(store);
```

Пакет `Microsoft.Extensions.DependencyInjection`  не підключено: контейнер з'явиться разом з Api на тижні 10.

### Режими роботи

| Запуск | Сховище | Поведінка |
| --- | --- | --- |
| `dotnet run --project src/Cli` | `InMemoryLibraryStore` | початкові дані з імпорту (`data/sample.csv`); усі зміни зникають після виходу, повторний запуск стартує з тих самих даних |
| `dotnet run --project src/Cli -- --file` | `FileLibraryStore` | під час першого запуску створюється `library.json`, заповнений даними з імпорту; зміни записуються у файл, повторний запуск бачить попередні записи (кількість примірників зростає) |

### Сценарії в `Program.cs`

1. **Додати → знайти за id → змінити → показати список** (усе через `LoanService`).
2. **Порушення інваріантів** сутностей на окремому демонстраційному примірнику.
3. **Ліміт відкритих видач** (5 на читача); наприкінці всі видачі читача повертаються, щоб не лишати виданих примірників у файлі.
4. **Сценарій відмови** (`try/catch`): додавання дубля id у сховище та видача примірника з неіснуючим id — виводяться зрозумілі повідомлення.
5. **Додаткове завдання 2**: пошук через `SearchBooks(Func<BookCopy, bool>)` — вільні примірники та пошук за частиною назви.

### Що зміниться на тижні 11 (база даних)

`LoanService` і доменна модель не змінюються: достатньо додати нову реалізацію `ILibraryStore` (на Entity Framework Core) і змінити один рядок вибору реалізації в `Program.cs`. Єдине, що потребуватиме уваги, - метод `Find`: параметр `Func<BookCopy, bool>` доведеться замінити на `Expression<Func<BookCopy, bool>>`, щоб запит міг перетворюватися на SQL, а також винести список видач у сховище.

### Додаткові завдання

2. **Пошук з параметром `Func<BookCopy, bool>`**: `ILibraryStore.Find`, реалізації в обох сховищах і `LoanService.SearchBooks`.

---

## Структура Solution

```text
CrossApp/
├── CrossApp.sln
├── data                            # sample.csv, sample.json
├── README.md
├── .gitignore
└── src/
    ├── Core/                       # Бібліотека класів (Class Library)
    │   ├── Core.csproj
    │   ├── EnvironmentInfo.cs      # Логіка збору даних про середовище
    │   ├── Abstractions/           # ILibraryStore (контракт сховища, тиждень 5)
    │   ├── Dto/                    # Record-типи даних (BookDto, BookCopyDto, LoanDto, ImportResult)
    │   ├── Import/                 # Сервіси імпорту (BookCsvImporter, BookJsonImporter)
    │   ├── Converters/             # BookCopyConverter (DTO → сутності)
    │   ├── Domain/                 # Сутності (BookCopy, Loan, LoanStatus)
    │   ├── Services/               # LoanService (правила між сутностями, тиждень 5)
    │   └── Storage/                # InMemoryLibraryStore, FileLibraryStore (тиждень 5)
    └── Cli/                        # Консольний застосунок (Console App)
        ├── Cli.csproj              # ProjectReference на Core
        └── Program.cs              # Composition root, сценарії успіху, порушень, відмови та пошуку
```

---

## Формат вхідних даних (Data Formats)

У проєкті використовуються такі формати файлів (розміщуються у папці `data/`):

### 1. CSV-формат (`sample.csv`)

- **Роздільник полів**: зафіксована константа - крапка з комою (`;`).
- **Кодування**: UTF-8.
- **Структура рядка**: `id;isbn;title;year` (рядок заголовка автоматично пропускається парсером).
- **Обробка помилок**: рядки з порушенням структури, порожніми обов'язковими полями або некоректними числовими даними не зупиняють процес імпорту, а фіксуються у списку помилок із зазначенням номера рядка.

### 2. JSON-формат (`sample.json`)

- **Структура**: масив JSON-об'єктів із ключами `"id"`, `"isbn"`, `"title"`, `"year"`.
- **Кодування**: UTF-8.
- **Обробка помилок**: здійснюється поелементний перебір через `JsonDocument`, що дозволяє безпечно відсіювати та логувати пошкоджені об'єкти без фатального падіння програми.

### 3. Файл сховища (`library.json`, лабораторна №5)

- **Створюється** під час запуску з `--file` у каталозі `data` поруч зі збіркою (`bin/.../data/library.json`); у репозиторій не комітиться.
- **Структура**: JSON-масив об'єктів `BookCopyDto` (`Id`, `Isbn`, `Title`, `IsIssued`).
- Якщо формат файлу застарів і десеріалізація падає з `JsonException`, достатньо видалити `library.json` — його буде створено заново з імпорту.

---

## Команди Build / Run / Publish

### Збірка (Build)

```bash
dotnet build
```

### Запуск (Run)

За замовчуванням використовується `data/sample.csv`, сховище — в пам'яті:

```bash
dotnet run --project src/Cli
```

Запуск із файловим сховищем (дані зберігаються між запусками):

```bash
dotnet run --project src/Cli -- --file
```

Запуск із явним зазначенням файлу імпорту (CSV або JSON), можна поєднувати з `--file`:

```bash
dotnet run --project src/Cli -- data/sample.csv
dotnet run --project src/Cli -- data/sample.json
dotnet run --project src/Cli -- data/sample.json --file
```

### Запуск під конкретні TFM (Multi-Targeting)

```bash
# Запуск під .NET 10.0
dotnet run --project src/Cli -f net10.0

# Запуск під .NET 8.0
dotnet run --project src/Cli -f net8.0
```

### Публікація (Publish)

```bash
# Framework-dependent
dotnet publish src/Cli -c Release -r win-x64 --self-contained false -f net10.0

# Self-contained
dotnet publish src/Cli -c Release -r win-x64 --self-contained true -f net10.0

# Single-file Self-contained
dotnet publish src/Cli -c Release -r win-x64 --self-contained true -f net10.0 -p:PublishSingleFile=true

# Single-file + Trimmed
dotnet publish src/Cli -c Release -r win-x64 --self-contained true -f net10.0 -p:PublishSingleFile=true -p:PublishTrimmed=true
```

### Запуск із папки publish

```cmd
.\src\Cli\bin\Release\net10.0\win-x64\publish\Cli.exe
```

---

## Порівняння режимів публікації

| RID | Режим / Параметри | Розмір publish | Встановлення runtime |
| --- | --- | --- | --- |
| `win-x64` | `framework-dependent` | ~0.19 МБ | **так** (.NET 10) |
| `win-x64` | `self-contained` | ~76.85 МБ | **ні** |
| `win-x64` | `self-contained` + `PublishSingleFile` | ~70.15 МБ | **ні** |