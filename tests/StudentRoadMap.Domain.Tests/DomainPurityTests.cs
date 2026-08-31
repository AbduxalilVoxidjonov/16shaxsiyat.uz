using System.Reflection;
using FluentAssertions;
using StudentRoadMap.Domain.Schools;

namespace StudentRoadMap.Domain.Tests;

/// <summary>
/// Domen qatlamining tozaligini tasdiqlaydi: `DateTime.Now`/`DateTime.UtcNow` ishlatilmaydi
/// (vaqt har doim parametr sifatida uzatiladi — `prompts/02-domain-qatlami.md` cheklovlari),
/// va `Domain` loyihasi hech qanday NuGet paketiga tegishli emas.
/// </summary>
public sealed class DomainPurityTests
{
    [Fact]
    public void DomainSourceFiles_DoNotUseSystemClockDirectly()
    {
        var domainSourceDirectory = FindDomainSourceDirectory();
        var sourceFiles = Directory.EnumerateFiles(domainSourceDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToList();

        sourceFiles.Should().NotBeEmpty("Domain manba fayllari topilishi kerak edi");

        var offendingFiles = new List<string>();
        foreach (var file in sourceFiles)
        {
            var content = File.ReadAllText(file);
            if (content.Contains("DateTime.Now", StringComparison.Ordinal) ||
                content.Contains("DateTime.UtcNow", StringComparison.Ordinal) ||
                content.Contains("DateTimeOffset.Now", StringComparison.Ordinal) ||
                content.Contains("DateTimeOffset.UtcNow", StringComparison.Ordinal))
            {
                offendingFiles.Add(file);
            }
        }

        offendingFiles.Should().BeEmpty("Domain qatlamida vaqt parametr sifatida uzatilishi kerak (System clock to'g'ridan-to'g'ri ishlatilmaydi)");
    }

    [Fact]
    public void DomainAssembly_HasNoExternalPackageDependencies()
    {
        // `School` — Domain assembly'sidan ixtiyoriy sinf, faqat sborka handle'ini olish uchun.
        var domainAssembly = typeof(SchoolSlug).Assembly;

        var referencedAssemblyNames = domainAssembly.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .ToList();

        // Faqat BCL/runtime sborkalariga ruxsat — uchinchi tomon NuGet paketlari bo'lmasligi kerak
        // (docs/06-arxitektura.md, Domain qatlami cheklovi).
        var allowedPrefixes = new[] { "System", "netstandard", "mscorlib" };

        var disallowed = referencedAssemblyNames
            .Where(name => !allowedPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
            .ToList();

        disallowed.Should().BeEmpty("Domain loyihasi hech qanday uchinchi tomon NuGet paketiga bog'liq bo'lmasligi kerak");
    }

    private static string FindDomainSourceDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "StudentRoadMap.sln")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new InvalidOperationException("Loyiha ildizi (StudentRoadMap.sln) topilmadi.");
        }

        var domainDirectory = Path.Combine(directory.FullName, "src", "StudentRoadMap.Domain");
        Directory.Exists(domainDirectory).Should().BeTrue($"'{domainDirectory}' mavjud bo'lishi kerak");

        return domainDirectory;
    }
}
