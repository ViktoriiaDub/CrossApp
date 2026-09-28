using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Core;

public sealed record EnvironmentReport(
    string Student,
    string OsDescription,
    string EnvironmentOS,
    string ProcessArchitecture,
    string ClrVersion,
    string RuntimeVersion,
    string AppDirectory,
    string CurrentDirectory,
    string DetectedRid,
    string ReportedRid,
    string Domain);

public static class EnvironmentInfo
{
    public static EnvironmentReport Collect() => new(
        Student: "Дуб Вікторія, група ФЕІ-32",
        OsDescription: RuntimeInformation.OSDescription,
        EnvironmentOS: Environment.OSVersion.ToString(),
        ProcessArchitecture: RuntimeInformation.ProcessArchitecture.ToString(),
        ClrVersion: Environment.Version.ToString(),
        RuntimeVersion: RuntimeInformation.FrameworkDescription,
        AppDirectory: AppContext.BaseDirectory,
        CurrentDirectory: Directory.GetCurrentDirectory(),
        DetectedRid: DetectRid(),
        ReportedRid: RuntimeInformation.RuntimeIdentifier,
        Domain: "Бібліотека (видання, примірники, читачі, видачі)"
    );

    private static string DetectRid()
    {
        string os = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win" :
                   RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux" :
                   RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx" : "unknown";

        string arch = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            Architecture.Arm64 => "arm64",
            Architecture.Arm => "arm",
            _ => "unknown"
        };

        return $"{os}-{arch}";
    }
}