using System;
using System.Text.Encodings.Web;
using System.Text.Json;
using Core;

Console.OutputEncoding = System.Text.Encoding.UTF8;

EnvironmentReport report = EnvironmentInfo.Collect();

bool isJsonMode = args.Length > 0 && args[0].Equals("--json", StringComparison.OrdinalIgnoreCase);

if (isJsonMode)
{
    var options = new JsonSerializerOptions 
    { 
        WriteIndented = true, 
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping 
    };

    string jsonString = JsonSerializer.Serialize(report, options);
    Console.WriteLine(jsonString);
}
else
{
    Console.WriteLine("CrossApp - практикум з крос-платформного програмування");
    Console.WriteLine($"Студентка: {report.Student}");
    Console.WriteLine("----------------------------------------------------");
    Console.WriteLine($"ОС (OSDescription)  : {report.OsDescription}");
    Console.WriteLine($"ОС (Environment)    : {report.EnvironmentOS}");
    Console.WriteLine($"Архітектура процесу : {report.ProcessArchitecture}");
    Console.WriteLine($"RID (визначено)    : {report.DetectedRid}");
    Console.WriteLine($"RID (від .NET)     : {report.ReportedRid}");
    Console.WriteLine($"Версія .NET (CLR)   : {report.ClrVersion}");
    Console.WriteLine($"Runtime             : {report.RuntimeVersion}");
    Console.WriteLine($"Каталог застосунку  : {report.AppDirectory}");
    Console.WriteLine($"Поточний каталог    : {report.CurrentDirectory}");
    Console.WriteLine("----------------------------------------------------");
    Console.WriteLine($"Предметна область   : {report.Domain}");
}