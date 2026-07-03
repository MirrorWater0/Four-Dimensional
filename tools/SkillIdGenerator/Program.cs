using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

internal sealed class SkillRegistryFile
{
    public Dictionary<string, string> ClassToEnum { get; set; } = new(StringComparer.Ordinal);
    public List<SkillRegistryEntry> Entries { get; set; } = new();
}

internal sealed class SkillRegistryEntry
{
    public string EnumName { get; set; } = string.Empty;
    public int Id { get; set; }
    public string? Player { get; set; }
    public bool Colorless { get; set; }
    public string Managed { get; set; } = "legacy";
    public string? ImplementationClass { get; set; }
}

internal static class Program
{
    private static readonly string RepoRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..")
    );
    private static readonly string RegistryPath = Path.Combine(
        RepoRoot,
        "tools",
        "skill-id-registry.json"
    );
    private static readonly string SkillCsPath = Path.Combine(
        RepoRoot,
        "character",
        "SkillBase",
        "Skill.cs"
    );
    private static readonly string GeneratedPath = Path.Combine(
        RepoRoot,
        "character",
        "SkillBase",
        "SkillID.Generated.cs"
    );
    private static readonly string CharacterRoot = Path.Combine(RepoRoot, "character");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly Dictionary<string, string> DefaultClassToEnum = new(
        StringComparer.Ordinal
    )
    {
        ["Shelter"] = "ResonanceShelter",
        ["ArcTrack"] = "ConcordSlash",
        ["EternalDarkSkill"] = "EternalDark",
    };

    private static readonly Regex SkillClassRegex = new(
        @"public\s+(?:partial\s+)?class\s+(\w+)\s*:\s*(?:Colorless)?Skill\b",
        RegexOptions.Compiled
    );

    private static readonly Regex EnumMemberRegex = new(
        @"^\s*(?<name>[A-Za-z_][A-Za-z0-9_]*)(?:\s*=\s*(?<id>\d+))?\s*,?\s*(?://.*)?$",
        RegexOptions.Compiled
    );

    public static int Main(string[] args)
    {
        string command = args.FirstOrDefault() ?? "generate";
        try
        {
            return command switch
            {
                "import" => ImportLegacyEnum(),
                "generate" => Generate(),
                "check" => CheckOnly(),
                _ => PrintUsage(),
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static int PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project tools/SkillIdGenerator -- import");
        Console.WriteLine("  dotnet run --project tools/SkillIdGenerator -- generate");
        Console.WriteLine("  dotnet run --project tools/SkillIdGenerator -- check");
        return 1;
    }

    private static int ImportLegacyEnum()
    {
        if (!File.Exists(SkillCsPath))
            throw new FileNotFoundException("Skill.cs not found", SkillCsPath);

        string skillCs = File.ReadAllText(SkillCsPath);
        int enumStart = skillCs.IndexOf("public enum SkillID", StringComparison.Ordinal);
        if (enumStart < 0)
            enumStart = skillCs.IndexOf("public partial enum SkillID", StringComparison.Ordinal);
        if (enumStart < 0)
            throw new InvalidOperationException("SkillID enum not found in Skill.cs");

        int braceStart = skillCs.IndexOf('{', enumStart);
        int braceEnd = FindMatchingBrace(skillCs, braceStart);
        string enumBody = skillCs[(braceStart + 1)..braceEnd];

        var registry = new SkillRegistryFile { ClassToEnum = new(DefaultClassToEnum) };
        string? currentPlayer = null;
        bool currentColorless = false;
        int lastId = -1;

        foreach (string rawLine in enumBody.Split('\n'))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#region", StringComparison.Ordinal) || line.StartsWith("#endregion", StringComparison.Ordinal))
                continue;

            if (line.StartsWith("[PlayerSkill(", StringComparison.Ordinal))
            {
                var playerMatch = Regex.Match(line, @"PlayerCharacterKey\.(\w+)");
                currentPlayer = playerMatch.Success ? playerMatch.Groups[1].Value : null;
                currentColorless = false;
                continue;
            }

            if (line.StartsWith("[ColorlessSkill]", StringComparison.Ordinal))
            {
                currentColorless = true;
                currentPlayer = null;
                continue;
            }

            if (!line.Contains(','))
                continue;

            var memberMatch = EnumMemberRegex.Match(line);
            if (!memberMatch.Success)
                continue;

            string enumName = memberMatch.Groups["name"].Value;
            if (memberMatch.Groups["id"].Success)
                lastId = int.Parse(memberMatch.Groups["id"].Value);
            else
                lastId++;

            string? implementationClass = ResolveImplementationClass(enumName, registry.ClassToEnum);
            registry.Entries.Add(
                new SkillRegistryEntry
                {
                    EnumName = enumName,
                    Id = lastId,
                    Player = currentPlayer,
                    Colorless = currentColorless,
                    Managed = "legacy",
                    ImplementationClass = implementationClass,
                }
            );
            currentPlayer = null;
            currentColorless = false;
        }

        registry.Entries = registry
            .Entries.GroupBy(entry => entry.EnumName, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(entry => entry.Id)
            .ToList();

        WriteRegistry(registry);
        Console.WriteLine($"Imported {registry.Entries.Count} SkillID entries to {RegistryPath}");
        return 0;
    }

    private static int Generate()
    {
        var registry = File.Exists(RegistryPath) ? ReadRegistry() : new SkillRegistryFile();
        if (registry.ClassToEnum.Count == 0)
            registry.ClassToEnum = new Dictionary<string, string>(DefaultClassToEnum, StringComparer.Ordinal);
        else
        {
            foreach (var pair in DefaultClassToEnum)
                registry.ClassToEnum.TryAdd(pair.Key, pair.Value);
        }

        var discovered = DiscoverSkillClasses();
        var entriesByEnum = registry.Entries.ToDictionary(
            entry => entry.EnumName,
            StringComparer.Ordinal
        );
        int nextId = registry.Entries.Count == 0 ? 0 : registry.Entries.Max(entry => entry.Id) + 1;
        int added = 0;

        foreach (var discoveredClass in discovered)
        {
            string enumName = ResolveEnumName(discoveredClass.ClassName, registry.ClassToEnum);
            if (entriesByEnum.ContainsKey(enumName))
            {
                var existing = entriesByEnum[enumName];
                existing.ImplementationClass ??= discoveredClass.ClassName;
                continue;
            }

            var entry = new SkillRegistryEntry
            {
                EnumName = enumName,
                Id = nextId++,
                Player = discoveredClass.Player,
                Colorless = discoveredClass.Colorless,
                Managed = "generated",
                ImplementationClass = discoveredClass.ClassName,
            };
            registry.Entries.Add(entry);
            entriesByEnum[enumName] = entry;
            added++;
            Console.WriteLine($"Added {enumName} = {entry.Id} ({discoveredClass.ClassName})");
        }

        registry.Entries = registry.Entries.OrderBy(entry => entry.Id).ToList();
        WriteRegistry(registry);
        WriteGeneratedEnum(registry);
        RemoveLegacyEnumFromSkillCs();

        Console.WriteLine(
            added == 0
                ? "SkillID registry is up to date."
                : $"Added {added} SkillID entries. Wrote {GeneratedPath}"
        );
        return 0;
    }

    private static int CheckOnly()
    {
        var registry = File.Exists(RegistryPath) ? ReadRegistry() : new SkillRegistryFile();
        var discovered = DiscoverSkillClasses();
        var missing = new List<string>();

        foreach (var discoveredClass in discovered)
        {
            string enumName = ResolveEnumName(discoveredClass.ClassName, registry.ClassToEnum);
            if (!registry.Entries.Any(entry => entry.EnumName == enumName))
                missing.Add($"{discoveredClass.ClassName} -> {enumName}");
        }

        if (missing.Count == 0)
        {
            Console.WriteLine("All skill classes are registered in skill-id-registry.json");
            return 0;
        }

        Console.WriteLine("Missing SkillID entries:");
        foreach (string line in missing)
            Console.WriteLine($"  - {line}");
        Console.WriteLine("Run: dotnet run --project tools/SkillIdGenerator -- generate");
        return 1;
    }

    private static List<DiscoveredSkillClass> DiscoverSkillClasses()
    {
        var results = new List<DiscoveredSkillClass>();
        foreach (string file in Directory.EnumerateFiles(CharacterRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}SkillIdGenerator{Path.DirectorySeparatorChar}"))
                continue;

            string text = File.ReadAllText(file);
            foreach (Match match in SkillClassRegex.Matches(text))
            {
                string className = match.Groups[1].Value;
                if (className is "Skill" or "ColorlessSkill" or "PlaceholderSkill")
                    continue;

                bool colorless = Regex.IsMatch(
                    match.Value,
                    @":\s*ColorlessSkill\b",
                    RegexOptions.CultureInvariant
                );
                results.Add(
                    new DiscoveredSkillClass(
                        className,
                        InferPlayer(file),
                        colorless || file.Contains($"{Path.DirectorySeparatorChar}Colorless{Path.DirectorySeparatorChar}")
                    )
                );
            }
        }

        return results
            .GroupBy(skill => skill.ClassName, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(skill => skill.ClassName, StringComparer.Ordinal)
            .ToList();
    }

    private static string? InferPlayer(string filePath)
    {
        string normalized = filePath.Replace('\\', '/');
        foreach (string player in new[] { "Kasiya", "Echo", "Mariya", "Nightingale" })
        {
            if (normalized.Contains($"/PlayerCharacter/{player}/", StringComparison.OrdinalIgnoreCase))
                return player;
        }

        return null;
    }

    private static string ResolveEnumName(string className, Dictionary<string, string> classToEnum) =>
        classToEnum.TryGetValue(className, out string? enumName) ? enumName : className;

    private static string? ResolveImplementationClass(
        string enumName,
        Dictionary<string, string> classToEnum
    )
    {
        foreach (var pair in classToEnum)
        {
            if (pair.Value == enumName)
                return pair.Key;
        }

        return enumName;
    }

    private static void WriteGeneratedEnum(SkillRegistryFile registry)
    {
        var entries = registry.Entries.OrderBy(entry => entry.Id).ToList();
        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated> Run: dotnet run --project tools/SkillIdGenerator -- generate");
        builder.AppendLine("// Source of truth: tools/skill-id-registry.json");
        builder.AppendLine();
        builder.AppendLine("public enum SkillID");
        builder.AppendLine("{");
        builder.AppendLine("    None = -1,");

        foreach (var entry in entries)
        {
            if (entry.Colorless)
                builder.AppendLine("    [ColorlessSkill]");
            else if (!string.IsNullOrWhiteSpace(entry.Player))
                builder.AppendLine($"    [PlayerSkill(PlayerCharacterKey.{entry.Player})]");

            builder.AppendLine($"    {entry.EnumName} = {entry.Id},");
        }

        builder.AppendLine("}");

        string content = builder.ToString().ReplaceLineEndings("\n");
        string? directory = Path.GetDirectoryName(GeneratedPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(GeneratedPath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static void RemoveLegacyEnumFromSkillCs()
    {
        if (!File.Exists(SkillCsPath))
            return;

        string skillCs = File.ReadAllText(SkillCsPath);
        int enumStart = skillCs.IndexOf("public partial enum SkillID", StringComparison.Ordinal);
        if (enumStart < 0)
            enumStart = skillCs.IndexOf("public enum SkillID", StringComparison.Ordinal);
        if (enumStart < 0)
            return;

        int braceStart = skillCs.IndexOf('{', enumStart);
        int braceEnd = FindMatchingBrace(skillCs, braceStart);
        int removeEnd = braceEnd + 1;
        while (removeEnd < skillCs.Length && (skillCs[removeEnd] == '\r' || skillCs[removeEnd] == '\n'))
            removeEnd++;

        skillCs = skillCs[..enumStart] + skillCs[removeEnd..];
        File.WriteAllText(SkillCsPath, skillCs, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static SkillRegistryFile ReadRegistry()
    {
        string json = File.ReadAllText(RegistryPath);
        return JsonSerializer.Deserialize<SkillRegistryFile>(json, JsonOptions)
            ?? new SkillRegistryFile();
    }

    private static void WriteRegistry(SkillRegistryFile registry)
    {
        string json = JsonSerializer.Serialize(registry, JsonOptions);
        string? directory = Path.GetDirectoryName(RegistryPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(RegistryPath, json.ReplaceLineEndings("\n"), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static int FindMatchingBrace(string text, int openIndex)
    {
        int depth = 0;
        for (int i = openIndex; i < text.Length; i++)
        {
            if (text[i] == '{')
                depth++;
            else if (text[i] == '}')
            {
                depth--;
                if (depth == 0)
                    return i;
            }
        }

        throw new InvalidOperationException("Unbalanced braces in SkillID enum");
    }

    private readonly record struct DiscoveredSkillClass(
        string ClassName,
        string? Player,
        bool Colorless
    );
}
