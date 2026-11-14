using System.Text.Json;
using FluentAssertions;

namespace eQuantic.Core.Persistence.Tests;

/// <summary>
/// Holds eQuantic.Core.Persistence and eQuantic.Core.Application to what a project on a native
/// eQuantic.Core.Data engine takes with them: no Entity Framework Core, no Humanizer, no Lamar. Those
/// belong to eQuantic.Core.Persistence.EntityFramework and to whoever chooses Lamar.
/// </summary>
/// <remarks>
/// It reads this test project's deps.json, the dependency closure the SDK resolved for it. The project
/// references nothing else that could bring those packages, so the closure is the packages' own. A package
/// reference that compiles to nothing (as Lamar did in the Application package) never shows in the
/// assembly's references, but it does show here.
/// </remarks>
public sealed class PersistenceDependencyTests
{
    private static readonly string[] Forbidden =
    [
        "Microsoft.EntityFrameworkCore",
        "eQuantic.Core.Data.EntityFramework",
        "Humanizer",
        "Lamar",
        "JasperFx",
    ];

    [Test]
    public void The_seam_and_the_application_layer_bring_no_entity_framework_humanizer_or_lamar()
    {
        var libraries = ResolvedLibraries();

        // The closure was really read: the seam and what it stands on are in it.
        libraries.Should().Contain(l => l.StartsWith("eQuantic.Core.Persistence/", StringComparison.Ordinal));
        libraries.Should().Contain(l => l.StartsWith("eQuantic.Core.Application/", StringComparison.Ordinal));
        libraries.Should().Contain(l => l.StartsWith("eQuantic.Core.Data/", StringComparison.Ordinal));

        libraries.Where(IsForbidden).Should().BeEmpty(
            "eQuantic.Core.Persistence and eQuantic.Core.Application must stay usable over a native engine without Entity Framework Core, Humanizer or Lamar");
    }

    private static bool IsForbidden(string library) =>
        Forbidden.Any(name =>
            library.StartsWith(name + "/", StringComparison.OrdinalIgnoreCase)
            || library.StartsWith(name + ".", StringComparison.OrdinalIgnoreCase));

    private static IReadOnlyList<string> ResolvedLibraries()
    {
        var assembly = typeof(PersistenceDependencyTests).Assembly.GetName().Name;
        var path = Path.Combine(AppContext.BaseDirectory, $"{assembly}.deps.json");
        File.Exists(path).Should().BeTrue($"the SDK writes {path} beside the test assembly");

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.GetProperty("libraries").EnumerateObject().Select(p => p.Name).ToList();
    }
}
