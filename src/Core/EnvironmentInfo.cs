using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Core;

public sealed record EnvironmentReport(
    string Student,
    string OsDescription,
    string EnvironmentOS,
    string ProcessArchitecture,
    string DetectedRid,
    string ReportedRid,
    string ClrVersion,
    string RuntimeVersion,
    string AppDirectory,
    string CurrentDirectory,
    string BuildNote,
    string Domain);

public static class EnvironmentInfo
{
#if NET10_0_OR_GREATER
    private const string BuildNoteValue = "збірка під net10.0";
#else
    private const string BuildNoteValue = "збірка під net8.0";
#endif

    public static EnvironmentReport Collect()
    {
        return new EnvironmentReport(
            Student: "Вікторія Дуб",
            OsDescription: RuntimeInformation.OSDescription,
            EnvironmentOS: Environment.OSVersion.ToString(),
            ProcessArchitecture: RuntimeInformation.ProcessArchitecture.ToString(),
            DetectedRid: DetectRid(),
            ReportedRid: RuntimeInformation.RuntimeIdentifier,
            ClrVersion: Environment.Version.ToString(),
            RuntimeVersion: RuntimeInformation.FrameworkDescription,
            AppDirectory: AppContext.BaseDirectory,
            CurrentDirectory: Directory.GetCurrentDirectory(),
            BuildNote: BuildNoteValue,
            Domain: "Кросплатформні застосунки"
        );
    }

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