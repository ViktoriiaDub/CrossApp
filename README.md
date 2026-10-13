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

---

## Структура Solution

```text
CrossApp/
├── CrossApp.sln
├── data
├── README.md
├── .gitignore
└── src/
    ├── Core/                       # Бібліотека класів (Class Library)
    │   ├── Core.csproj
    │   ├── EnvironmentInfo.cs      # Логіка збору даних про середовище
    │   ├── Dto/                    # Record-типи даних (BookDto, ImportResult)
    │   ├── Import/                 # Сервіси імпорту (BookCsvImporter, BookJsonImporter)
    │   ├── Domain/                 # Доменні сутності (тиждень 4)
    │   └── Storage/                # Сховища даних (тиждень 5)
    └── Cli/                        # Консольний застосунок (Console App)
        ├── Cli.csproj              # ProjectReference на Core
        └── Program.cs              # Інтерфейс командного рядка та вивід статистики
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

---

## Команди Build / Run / Publish

### Збірка (Build)

```bash
dotnet build
```

### Запуск (Run)

За замовчуванням використовується `data/sample.csv`:

```bash
dotnet run --project src/Cli
```

Запуск із явним зазначенням файлу (CSV або JSON):

```bash
dotnet run --project src/Cli -- data/sample.csv
dotnet run --project src/Cli -- data/sample.json
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
| `win-x64` |