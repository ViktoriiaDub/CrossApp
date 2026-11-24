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

### Сценарії в `Program.cs` (лабораторна №5)

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

## Лабораторна робота №6: Доменні події, делегати Action/Func, лямбди

### Ідея

Сервіс має сповіщати зовнішній код про виконані зміни, але не знати, як саме ці сповіщення оброблятимуться. `LoanService` лише піднімає подію `Changed` («щось сталося»), а Cli самостійно вирішує, що з цим робити: вивести в консоль, записати в `logs/app.log`, порахувати. У `Core` немає `Console.WriteLine` і запису в лог-файл, тому підключення нового споживача сповіщень не вимагає змін у `Core`.

### Які зміни стану є подіями

Подією вважається кожна **успішна** операція, що змінює дані бібліотеки:

| Вид (`LibraryChangeKind`) | Коли виникає | Операція |
| --- | --- | --- |
| `BookAdded` | новий примірник додано до сховища | `LoanService.AddBookCopy` |
| `CopyIssued` | примірник видано читачу | `LoanService.IssueBook` |
| `CopyReturned` | примірник повернуто | `LoanService.ReturnBook` |
| `LoanCanceled` | помилкову видачу скасовано | `LoanService.CancelLoan` |

Якщо інваріант кинув виняток (`BookCopy`, `Loan` або правило в `LoanService`), стан не змінився, а отже події немає. Подію піднімають лише після `_store.Add` / `_store.Update`, ніколи не до них.

### Компоненти

| Компонент | Розташування | Призначення |
| --- | --- | --- |
| `LibraryChangeKind` | `Core/Events/LibraryChangeKind.cs` | enum з видами змін |
| `LibraryChangedEventArgs` | `Core/Events/LibraryChangedEventArgs.cs` | дані події: `Kind`, `CopyId`, `ReaderId`, `DueDate`, `At` (усі лише для читання) |
| `LoanService` | `Core/Services/LoanService.cs` | `event EventHandler<LibraryChangedEventArgs>? Changed`, приватний `OnChanged`, пошук з `Func` |
| `Program.cs` | `Cli/Program.cs` | підписники, відписка, сценарії |
| `app.log` | `logs/app.log` поруч зі збіркою | журнал, який пише підписник у Cli |

Подія оголошена як `event`, а не як публічне поле-делегат: зовнішній код може лише підписатися (`+=`) чи відписатися (`-=`), а викликати її може тільки `LoanService`.

```csharp
public event EventHandler<LibraryChangedEventArgs>? Changed;

private void OnChanged(LibraryChangeKind kind, string copyId, string? readerId = null, DateTime? dueDate = null) =>
    Changed?.Invoke(this, new LibraryChangedEventArgs(kind, copyId, readerId, dueDate));
```

`?.Invoke` потрібен, бо без підписників `Changed` дорівнює `null`. Це нормальний стан сервісу. `DueDate` (дата видачі + `DefaultLoanDays` = 14 днів) обчислюється в `IssueBook` і передається лише в події `CopyIssued`.

### Підписники

| Підписник | Де | Що робить |
| --- | --- | --- |
| `OnLibraryChanged` (локальний метод) | `Cli/Program.cs` | друкує `[LOG]` у консоль і дописує рядок у `logs/app.log`; запис у файл обгорнуто в `try/catch`, щоб помилка логування не ламала бізнес-операцію |
| Лямбда `(_, _) => { changes++; ... }` | `Cli/Program.cs` | замикання над локальною змінною `changes`; рахує події й друкує `[COUNT]` |

Підписники викликаються синхронно, по черзі, у порядку підписки: у консолі після кожного рядка `[LOG]` іде рядок `[COUNT]`. Відписка працює для `OnLibraryChanged`, бо це іменований локальний метод: `-=` знімає той самий делегат. Лямбду так само можна було б відписати, лише якщо зберегти її у змінній.

**Хто стане підписником далі:**

- **Тиждень 10**: логер проєкту Api замість консолі й файлу Cli.
- **Тиждень 12**: компонент Blazor, який оновлюватиме таблицю.
- **Тиждень 8**: тести, які підписуються на `Changed` і перевіряють, що операція дала потрібне сповіщення.

У жодному з цих випадків `Core` змінювати не потрібно.

### Func замість події

Пошук `LoanService.SearchBooks(Func<BookCopy, bool> predicate)` повертає значення, тому тут делегат `Func`, а не подія: подія призначена для «щось сталося», а не для того, щоб отримати відповідь. Критерій задає той, хто викликає, наприклад `service.SearchBooks(b => b.IsIssued)`.

### Сценарій у `Program.cs` (лабораторна №6)

1. **Крок 5**: додати примірник → видати → повернути. Лічильник дорівнює 3. Далі `service.Changed -= OnLibraryChanged`, ще два тихі кроки (додати й видати) з лічильником 5: у консолі й у `app.log` нових рядків немає, але дані змінюються.
2. **Крок 6**: сценарій відмови. Неіснуючий id, повторна видача, порожній ISBN і повернення раніше за дату видачі. Виняток є, подій немає, лічильник лишається 5.
3. **Крок 6.1**: додати → видати → скасувати (`LoanCanceled`). Лічильник дорівнює 8.
4. **Крок 7**: пошук через `Func<BookCopy, bool>` (видані й вільні примірники).

Логер повертають на підписку перед кроком 6, тож усе, що відбувається в кроці 6.1, знову потрапляє в журнал.

### Журнал `logs/app.log`

Створюється під час запуску: шлях будується через `Path.Combine(AppContext.BaseDirectory, "logs", "app.log")`, а каталог створюється кодом (`Directory.CreateDirectory`). Це каталог збірки (`bin/.../logs`), тому в репозиторій файл не потрапляє. Журнал дописується (`File.AppendAllText`), тож під час кількох запусків рядки накопичуються.

Формат рядка: `час вид [id примірника] -> опис`.

```text
18:19:05 BookAdded     [6c58b412] -> додано новий примірник до фонду
18:19:05 CopyIssued    [6c58b412] -> видано читачу Reader-Ivan до дати 2026-12-07
18:19:05 CopyReturned  [6c58b412] -> повернуто читачем Reader-Ivan
```

### Самоперевірка

1. Пошук по `src/Core` слів `Console` і `logs` нічого не знаходить: `findstr /s /i /n "Console logs" src\Core\*.cs`.
2. Кожна успішна операція дає рівно одну подію.
3. Операція, що впала на інваріанті, події не дає.
4. Після `-=` обробник мовчить.
5. Подія оголошена як `event`, а не як публічне поле делегата.

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
    │   ├── Events/                 # LibraryChangeKind, LibraryChangedEventArgs (тиждень 6)
    │   ├── Services/               # LoanService (правила між сутностями, тиждень 5; подія Changed, тиждень 6)
    │   └── Storage/                # InMemoryLibraryStore, FileLibraryStore (тиждень 5)
    └── Cli/                        # Консольний застосунок (Console App)
        ├── Cli.csproj              # ProjectReference на Core
        └── Program.cs              # Composition root, підписники подій, сценарії успіху, відмови та пошуку
```

Під час запуску поруч зі збіркою (`src/Cli/bin/.../`) створюються `data/library.json` (режим `--file`) і `logs/app.log`; у репозиторій вони не комітяться.

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

### 4. Журнал подій (`app.log`, лабораторна №6)

- **Створюється** під час кожного запуску в каталозі `logs` поруч зі збіркою (`bin/.../logs/app.log`); у репозиторій не комітиться.
- **Формат рядка**: `HH:mm:ss вид [id примірника] -> опис`.
- **Вміст**: лише події, які обробив підписник `OnLibraryChanged`; операції після відписки та невдалі операції в журнал не потрапляють.

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

Журнал подій після запуску лежить за шляхом, який програма виводить наприкінці (`Файл журналу збережено за шляхом: ...`).

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