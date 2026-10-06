using Core;

Console.OutputEncoding = System.Text.Encoding.UTF8;

EnvironmentReport report = EnvironmentInfo.Collect();

Console.WriteLine("CrossApp - практикум з крос-платформного програмування");
Console.WriteLine($"Студентка           : {report.Student}");
Console.WriteLine("----------------------------------------------------");
Console.WriteLine($"ОС (OSDescription)  : {report.OsDescription}");
Console.WriteLine($"ОС (Environment)    : {report.EnvironmentOS}");
Console.WriteLine($"Архітектура процесу : {report.ProcessArchitecture}");
Console.WriteLine($"RID (визначено)     : {report.DetectedRid}");
Console.WriteLine($"RID (від .NET)      : {report.ReportedRid}");
Console.WriteLine($"Версія .NET (CLR)   : {report.ClrVersion}");
Console.WriteLine($"Runtime             : {report.RuntimeVersion}");
Console.WriteLine($"Каталог застосунку  : {report.AppDirectory}");
Console.WriteLine($"Поточний каталог    : {report.CurrentDirectory}");
Console.WriteLine("----------------------------------------------------");
Console.WriteLine($"Примітка збірки     : {report.BuildNote}");
Console.WriteLine($"Предметна область   : {report.Domain}");