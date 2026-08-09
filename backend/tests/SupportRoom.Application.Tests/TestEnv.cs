using System.Runtime.CompilerServices;
using SupportRoom.Domain.Configuration;

namespace SupportRoom.Application.Tests;

/// <summary>
/// Tests construct Real providers directly now that Mock has been removed, so they need the
/// same credentials `dotnet run` picks up from the gitignored .env - dotnet test never runs
/// Program.cs, so nothing loads it otherwise. Runs once, automatically, before any test in this
/// assembly (ModuleInitializer) - walks up from the test binary's output folder to the repo root
/// (marked by SupportRoom.slnx) rather than hardcoding a relative path that breaks the moment the
/// build configuration or target framework folder name changes.
/// </summary>
internal static class TestEnv
{
    [ModuleInitializer]
    public static void Load()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "SupportRoom.slnx")))
        {
            dir = dir.Parent;
        }
        if (dir is not null)
        {
            DotEnv.Load(Path.Combine(dir.FullName, "src", "SupportRoom.Api", ".env"));
        }
    }
}
