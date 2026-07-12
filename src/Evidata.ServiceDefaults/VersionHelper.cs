using System.Reflection;

namespace Evidata.ServiceDefaults;

/// <summary>
/// Helper para obtener información de versión del assembly ejecutándose.
/// Reutilizable entre la API y las Azure Functions Isolated.
/// </summary>
public static class VersionHelper
{
    public static VersionInfo GetVersionInfo()
    {
        var asm = Assembly.GetEntryAssembly()!;
        var infoVersion = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                          ?? asm.GetName().Version?.ToString() ?? "unknown";
        var fileVersion = asm.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "unknown";
        var buildTime = new FileInfo(asm.Location).LastWriteTimeUtc.ToString("o");
        var buildTimeUtc = new FileInfo(asm.Location).LastWriteTimeUtc;

        return new VersionInfo
        {
            Version = infoVersion,
            FileVersion = fileVersion,
            AssemblyName = asm.GetName().Name ?? "unknown",
            BuildTime = buildTime,
            BuildTimeUtc = buildTimeUtc
        };
    }
}

public class VersionInfo
{
    public required string Version { get; set; }
    public required string FileVersion { get; set; }
    public required string AssemblyName { get; set; }
    public required string BuildTime { get; set; }
    public required DateTime BuildTimeUtc { get; set; }

    public object ToResponse(string? environment = null) => new
    {
        version = Version,
        fileVersion = FileVersion,
        assemblyName = AssemblyName,
        buildTime = BuildTime,
        environment = environment
    };
}

