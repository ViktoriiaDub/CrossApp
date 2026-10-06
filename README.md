# CrossApp

Наскрізний проєкт із дисципліни «Крос-платформне програмування».

- **Студентка**: Дуб Вікторія, група ФЕІ-32
- **Предметна область**: Бібліотека (видання, примірники, читачі, видачі)

---

## Структура Solution

```text
CrossApp/
├── CrossApp.sln
├── README.md
├── .gitignore
└── src/
    ├── Core/                    # Бібліотека класів (Class Library)
    │   ├── Core.csproj
    │   ├── EnvironmentInfo.cs   # Логіка збору даних про середовище
    │   ├── Dto/                 # Record-типи формату даних (тиждень 3)
    │   ├── Domain/              # Доменні сутності (тиждень 4)
    │   └── Storage/             # Сховища даних (тиждень 5)
    └── Cli/                     # Консольний застосунок (Console App)
        ├── Cli.csproj           # ProjectReference на Core
        └── Program.cs           # Форматування та вивід даних
```

---

## Команди Build / Run / Publish

### Збірка (Build)

```bash
dotnet build
```

### Запуск (Run)

```bash
dotnet run --project src/Cli
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
| `win-x64` | `self-contained` + `SingleFile` + `Trimmed` | ~12.57 МБ | **ні** |
| `linux-x64` | `self-contained` | ~78.70 МБ | **ні** |
