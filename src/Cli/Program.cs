using System.Runtime.InteropServices;

Console.WriteLine("CrossApp- практикум з крос-платформного програмування");
Console.WriteLine("Студентка: Дуб Вікторія, Феі-32");
Console.WriteLine(new string('-',52));

Console.WriteLine($"OC (OSDescription): {RuntimeInformation.OSDescription}");
Console.WriteLine($"OC (Environment) : {Environment.OSVersion}");
Console.WriteLine($"Архітектура процесу: {RuntimeInformation.ProcessArchitecture}");
Console.WriteLine($"Версія .NET (CLR): {Environment.Version}");
Console.WriteLine($"Runtime          : {RuntimeInformation.FrameworkDescription}");
Console.WriteLine($"Каталог застосунку: {AppContext.BaseDirectory}");
Console.WriteLine($"Поточний каталог : {Environment.CurrentDirectory}");
Console.WriteLine(new string('-', 52));
Console.WriteLine("Предметна область: Бібліотека (видання, примірники, читачі, видачі)");

