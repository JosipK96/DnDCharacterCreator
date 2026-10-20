using System.Text.RegularExpressions;

using FluentAssertions;

namespace CharacterCreatorApp.Domain.UnitTests;

// Sessions look up the backticked names in the CLAUDE.md files with one Grep, so a rename that
// leaves a stale name behind sends them searching for code that does not exist. This reads the
// files as text and needs no Domain type; it sits here because Domain.UnitTests is the unit test
// project every change builds.
public partial class ClaudeMdReferencesTests
{
    // Names a CLAUDE.md uses on purpose that are not declared in this repository's C# code.
    private static readonly Dictionary<string, string> Allowlist = new(StringComparer.Ordinal)
    {
        ["CancellationToken"] = "BCL type",
        ["DateTime.Now"] = "BCL member, named as the thing not to use",
        ["Guid.CreateVersion7"] = "BCL member",
        ["TimeProvider"] = "BCL type",
        ["IQueryable"] = "BCL type, named as the thing a repository never returns",
        ["IRequest"] = "MediatR type",
        ["NotEmptyValidator"] = "FluentValidation type",
        ["MockBehavior.Strict"] = "Moq member",
        ["WebApplicationBuilder"] = "ASP.NET Core type",
        ["WebApplicationFactory"] = "ASP.NET Core testing type",
        ["MapFallbackToFile"] = "ASP.NET Core method",
        ["MapStaticAssets"] = "ASP.NET Core method",
        ["NoAction"] = "EF Core DeleteBehavior value",
        ["TreatWarningsAsErrors"] = "MSBuild property",
        ["EnforceCodeStyleInBuild"] = "MSBuild property",
        ["PackageReference"] = "MSBuild item",
        ["Async"] = "the method-name suffix convention, not a name",
    };

    [Fact]
    public void ClaudeMdFiles_NameOnlyDeclaredCode()
    {
        // Arrange
        var root = FindRepositoryRoot();
        var declared = DeclaredNames(root);

        // Act
        var stale = EnumerateRepositoryFiles(root, "CLAUDE.md")
            .SelectMany(file => CodeReferences(File.ReadAllText(file))
                .Where(reference => !Allowlist.ContainsKey(reference))
                .Where(reference => !reference.Split('.').All(declared.Contains))
                .Select(reference => $"{Path.GetRelativePath(root, file)}: `{reference}`"))
            .Distinct()
            .ToList();

        // Assert
        stale.Should().BeEmpty("every name a CLAUDE.md puts in backticks should be declared in the code, or be in the allowlist with a reason");
    }

    [Fact]
    public void Allowlist_HoldsOnlyNamesNotDeclaredHere()
    {
        // Arrange
        var declared = DeclaredNames(FindRepositoryRoot());

        // Act
        var redundant = Allowlist.Keys.Where(name => name.Split('.').All(declared.Contains)).ToList();

        // Assert
        redundant.Should().BeEmpty("an allowlisted name that is declared here would hide its own rename");
    }

    [Theory]
    [InlineData("`CharacterBuilder`", "CharacterBuilder")]
    [InlineData("`Race.SpeedWith`", "Race.SpeedWith")]
    [InlineData("`Entity<TId>`", "Entity")]
    [InlineData("`ICommand`", "ICommand")]
    public void CodeReferences_TypeOrMember_IsChecked(string markdown, string expected)
    {
        // Act
        var references = CodeReferences(markdown);

        // Assert
        references.Should().Equal(expected);
    }

    [Theory]
    [InlineData("`elf_2024`")]
    [InlineData("`common-phb`")]
    [InlineData("`Books.json`")]
    [InlineData("`docs/conventions/glossary.md`")]
    [InlineData("`<Area>Errors`")]
    [InlineData("`*ApiRequest`")]
    [InlineData("`â€¦WithDetails`")]
    [InlineData("`Error.Validation(\"Some.Code\", ...)`")]
    [InlineData("`.Result`")]
    [InlineData("`ConnectionStrings__DefaultConnection`")]
    [InlineData("`Method_Scenario_Expectation`")]
    [InlineData("`PHB`")]
    [InlineData("```\nCharacterBuilder\n```")]
    public void CodeReferences_PathIdPlaceholderOrSnippet_IsSkipped(string markdown)
    {
        // Act
        var references = CodeReferences(markdown);

        // Assert
        references.Should().BeEmpty();
    }

    private static List<string> CodeReferences(string markdown)
    {
        var prose = FencedBlock().Replace(markdown, string.Empty);
        return InlineCode().Matches(prose)
            .Select(match => match.Groups[1].Value)
            .Where(code => CodeName().IsMatch(code))

            // All capitals is a book or source abbreviation (PHB, XGE), not a C# name.
            .Where(code => code.Any(char.IsLower))
            .Select(code => GenericArguments().Replace(code, string.Empty))
            .ToList();
    }

    // shortcut: declarations are found with regexes over the source text, not a compiler, so a
    // name counts as declared if any type declares it, not necessarily the type the CLAUDE.md
    // puts before the dot; move to Roslyn if a stale `Type.Member` ever slips through.
    private static HashSet<string> DeclaredNames(string root)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in EnumerateRepositoryFiles(root, "*.cs"))
        {
            var source = File.ReadAllText(file);
            AddCaptures(names, TypeDeclaration().Matches(source));
            AddCaptures(names, ModifiedMemberDeclaration().Matches(source));
            AddCaptures(names, InterfaceMemberDeclaration().Matches(source));

            foreach (Match body in EnumBody().Matches(source))
            {
                AddCaptures(names, EnumMember().Matches(LineComment().Replace(body.Groups[1].Value, string.Empty)));
            }

            foreach (Match parameters in RecordParameters().Matches(source))
            {
                AddCaptures(names, RecordParameter().Matches(parameters.Groups[1].Value));
            }
        }

        return names;
    }

    private static void AddCaptures(HashSet<string> names, MatchCollection matches)
    {
        foreach (Match match in matches)
        {
            names.Add(match.Groups[1].Value);
        }
    }

    private static IEnumerable<string> EnumerateRepositoryFiles(string root, string pattern) =>
        Directory.EnumerateFiles(root, pattern, SearchOption.AllDirectories)
            .Where(file => !Path.GetRelativePath(root, file)
                .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(part => part is ".claude" or ".git" or "bin" or "obj" or "node_modules"));

    // Walks up from the test binaries, so a worktree finds its own copy rather than the main checkout.
    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "CharacterCreatorApp.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("CharacterCreatorApp.sln not found above the test binaries.");
    }

    [GeneratedRegex(@"^```.*?^```", RegexOptions.Multiline | RegexOptions.Singleline)]
    private static partial Regex FencedBlock();

    [GeneratedRegex(@"`([^`\r\n]+)`")]
    private static partial Regex InlineCode();

    // PascalCase segments joined by dots, with optional generic parameters: Race.SpeedWith, Entity<TId>.
    [GeneratedRegex(@"^[A-Z][A-Za-z0-9]*(<[A-Z][A-Za-z0-9, ]*>)?(\.[A-Z][A-Za-z0-9]*)*$")]
    private static partial Regex CodeName();

    [GeneratedRegex(@"<[^>]*>")]
    private static partial Regex GenericArguments();

    [GeneratedRegex(@"\b(?:class|record|struct|interface|enum|delegate\s+[\w<>\[\]?,. ]+?)\s+(?:class\s+|struct\s+)?([A-Za-z_]\w*)")]
    private static partial Regex TypeDeclaration();

    // An access modifier, then the type, then the name before (, {, =, =>, ; or a generic list. A type's
    // own declaration line is left to TypeDeclaration, or its base type would count as declared.
    [GeneratedRegex(@"\b(?:public|internal|protected|private)\b(?![^\r\n]*\b(?:class|record|struct|interface|enum)\s)[^;=(){}\r\n]*?\b([A-Za-z_]\w*)\s*(?:<[^>\r\n]*>)?\s*(?:\(|\{|=|;)")]
    private static partial Regex ModifiedMemberDeclaration();

    // Interface members have no modifier: a return type, then the name and its parameters.
    [GeneratedRegex(@"^\s*(?!return\b|await\b|throw\b|yield\b|new\b|else\b|var\b)[\w.]+(?:<[^>\r\n]*>)?\??\s+([A-Za-z_]\w*)\s*(?:<[^>\r\n]*>)?\s*\(", RegexOptions.Multiline)]
    private static partial Regex InterfaceMemberDeclaration();

    [GeneratedRegex(@"\benum\s+\w+[^{]*\{([^}]*)\}")]
    private static partial Regex EnumBody();

    [GeneratedRegex(@"(?:^|,)\s*(?:\[[^\]]*\]\s*)*([A-Za-z_]\w*)")]
    private static partial Regex EnumMember();

    [GeneratedRegex(@"//[^\r\n]*")]
    private static partial Regex LineComment();

    [GeneratedRegex(@"\brecord\s+(?:class\s+|struct\s+)?\w+(?:<[^>]*>)?\s*\(([^)]*)\)")]
    private static partial Regex RecordParameters();

    // The parameter name: the last identifier before a comma, a default value or the end.
    [GeneratedRegex(@"([A-Za-z_]\w*)\s*(?:=[^,]*)?(?:,|$)")]
    private static partial Regex RecordParameter();
}
