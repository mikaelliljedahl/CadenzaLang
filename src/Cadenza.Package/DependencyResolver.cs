using System.Text.RegularExpressions;

namespace Cadenza.Package;

/// <summary>
/// Handles semantic versioning and dependency resolution with conflict management
/// </summary>
public class DependencyResolver
{
    private readonly IPackageRegistry _flowLangRegistry;
    private readonly INuGetClient _nugetClient;

    public DependencyResolver(IPackageRegistry flowLangRegistry, INuGetClient nugetClient)
    {
        _flowLangRegistry = flowLangRegistry;
        _nugetClient = nugetClient;
    }

    /// <summary>
    /// Resolve all dependencies for a project, handling version conflicts
    /// </summary>
    public async Task<ResolutionResult> ResolveAsync(EnhancedFlowcConfig config, bool includeDev = false)
    {
        var result = new ResolutionResult();
        var visited = new HashSet<string>();
        var resolving = new HashSet<string>();

        // Combine runtime and dev dependencies
        var allDependencies = new Dictionary<string, string>(config.Dependencies);
        if (includeDev)
        {
            foreach (var (name, version) in config.DevDependencies)
            {
                allDependencies[name] = version;
            }
        }

        // Resolve each top-level dependency
        foreach (var (packageName, versionSpec) in allDependencies)
        {
            await ResolvePackageRecursive(packageName, versionSpec, result, visited, resolving);
        }

        // Validate no conflicts remain
        ValidateResolution(result);

        return result;
    }

    private async Task ResolvePackageRecursive(
        string packageName, 
        string versionSpec, 
        ResolutionResult result,
        HashSet<string> visited,
        HashSet<string> resolving)
    {
        if (visited.Contains(packageName))
            return;

        if (resolving.Contains(packageName))
            throw new CircularDependencyException($"Circular dependency detected: {packageName}");

        resolving.Add(packageName);

        try
        {
            // Check if this package is already resolved with a compatible version
            if (result.ResolvedPackages.TryGetValue(packageName, out var existing))
            {
                if (!IsVersionCompatible(existing.Version, versionSpec))
                {
                    result.Conflicts.Add(new VersionConflict(
                        packageName, 
                        existing.Version, 
                        versionSpec, 
                        $"Version conflict for {packageName}: {existing.Version} vs {versionSpec}"
                    ));
                    return;
                }
                // Compatible version already resolved
                return;
            }

            // Resolve the package
            var package = await ResolvePackage(packageName, versionSpec);
            if (package == null)
            {
                result.Errors.Add($"Package not found: {packageName}@{versionSpec}");
                return;
            }

            result.ResolvedPackages[packageName] = package;

            // Recursively resolve dependencies
            foreach (var (depName, depVersion) in package.Dependencies)
            {
                await ResolvePackageRecursive(depName, depVersion, result, visited, resolving);
            }

            visited.Add(packageName);
        }
        finally
        {
            resolving.Remove(packageName);
        }
    }

    private async Task<ResolvedPackage?> ResolvePackage(string packageName, string versionSpec)
    {
        // Try Cadenza registry first
        try
        {
            var metadata = await _flowLangRegistry.GetPackageMetadataAsync(packageName);
            if (metadata != null)
            {
                var version = ResolveBestVersion(metadata.Version, versionSpec);
                if (version != null)
                {
                    return new ResolvedPackage(
                        Version: version,
                        Resolved: $"https://api.nuget.org/v3-flatcontainer/{packageName.ToLowerInvariant()}/{version}/{packageName.ToLowerInvariant()}.{version}.nupkg",
                        Integrity: $"sha512-{GenerateIntegrity(packageName, version)}",
                        Dependencies: metadata.Dependencies,
                        Effects: metadata.Effects,
                        Type: PackageType.Cadenza
                    );
                }
            }
        }
        catch (Exception ex)
        {
            // Log warning and continue to NuGet
            Console.WriteLine($"Warning: Failed to resolve {packageName} from Cadenza registry: {ex.Message}");
        }

        // Try NuGet registry
        try
        {
            var nugetPackage = await _nugetClient.GetPackageAsync(packageName, versionSpec);
            if (nugetPackage != null)
            {
                return new ResolvedPackage(
                    Version: nugetPackage.Version,
                    Resolved: nugetPackage.DownloadUrl,
                    Integrity: nugetPackage.Hash,
                    Dependencies: nugetPackage.Dependencies,
                    Effects: InferEffectsFromNuGetPackage(packageName),
                    Type: PackageType.NuGet
                );
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to resolve {packageName} from NuGet: {ex.Message}");
        }

        return null;
    }

    private List<string> InferEffectsFromNuGetPackage(string packageName)
    {
        // Built-in effect mappings for common NuGet packages
        var effectMappings = new Dictionary<string, List<string>>
        {
            { "System.Data.SqlClient", new() { "Database" } },
            { "Microsoft.Data.SqlClient", new() { "Database" } },
            { "System.Net.Http", new() { "Network" } },
            { "Microsoft.Extensions.Logging", new() { "Logging" } },
            { "Azure.Storage.Blobs", new() { "Network", "FileSystem" } },
            { "StackExchange.Redis", new() { "Network", "Memory" } },
            { "Npgsql", new() { "Database" } },
            { "MongoDB.Driver", new() { "Database", "Network" } },
            { "Elasticsearch.Net", new() { "Network" } },
            { "RabbitMQ.Client", new() { "Network" } },
            { "Microsoft.Extensions.Caching", new() { "Memory" } },
            { "System.IO", new() { "FileSystem", "IO" } },
            { "System.Diagnostics", new() { "Logging" } }
        };

        // Check exact matches first
        if (effectMappings.ContainsKey(packageName))
            return effectMappings[packageName];

        // Pattern matching for package families
        foreach (var (pattern, effects) in effectMappings)
        {
            if (packageName.StartsWith(pattern.Replace("*", "")))
                return effects;
        }

        // Default inference based on package name patterns
        var inferredEffects = new List<string>();
        var lowerName = packageName.ToLowerInvariant();

        if (lowerName.Contains("database") || lowerName.Contains("sql") || 
            lowerName.Contains("mongo") || lowerName.Contains("redis") ||
            lowerName.Contains("cosmos") || lowerName.Contains("dynamo"))
            inferredEffects.Add("Database");

        if (lowerName.Contains("http") || lowerName.Contains("client") ||
            lowerName.Contains("api") || lowerName.Contains("rest") ||
            lowerName.Contains("graphql") || lowerName.Contains("grpc"))
            inferredEffects.Add("Network");

        if (lowerName.Contains("log") || lowerName.Contains("trace") ||
            lowerName.Contains("serilog") || lowerName.Contains("nlog"))
            inferredEffects.Add("Logging");

        if (lowerName.Contains("file") || lowerName.Contains("stream") ||
            lowerName.Contains("directory") || lowerName.Contains("path"))
            inferredEffects.Add("FileSystem");

        if (lowerName.Contains("cache") || lowerName.Contains("memory"))
            inferredEffects.Add("Memory");

        return inferredEffects.Any() ? inferredEffects : new List<string> { "IO" }; // Default fallback
    }

    public bool IsVersionCompatible(string resolvedVersion, string versionSpec)
    {
        if (string.IsNullOrEmpty(versionSpec) || versionSpec == "*")
            return true;

        var resolved = SemanticVersion.Parse(resolvedVersion);
        
        // Handle different version specifiers
        if (versionSpec.StartsWith("^"))
        {
            var target = SemanticVersion.Parse(versionSpec[1..]);
            return resolved.Major == target.Major && resolved >= target;
        }
        
        if (versionSpec.StartsWith("~"))
        {
            var target = SemanticVersion.Parse(versionSpec[1..]);
            return resolved.Major == target.Major && resolved.Minor == target.Minor && resolved >= target;
        }
        
        if (versionSpec.StartsWith(">="))
        {
            var target = SemanticVersion.Parse(versionSpec[2..]);
            return resolved >= target;
        }
        
        if (versionSpec.StartsWith(">"))
        {
            var target = SemanticVersion.Parse(versionSpec[1..]);
            return resolved > target;
        }
        
        if (versionSpec.StartsWith("<="))
        {
            var target = SemanticVersion.Parse(versionSpec[2..]);
            return resolved <= target;
        }
        
        if (versionSpec.StartsWith("<"))
        {
            var target = SemanticVersion.Parse(versionSpec[1..]);
            return resolved < target;
        }

        // Exact version match
        var exactTarget = SemanticVersion.Parse(versionSpec);
        return resolved == exactTarget;
    }

    private string? ResolveBestVersion(string availableVersion, string versionSpec)
    {
        // For now, simple implementation - would need package registry integration
        // to get all available versions and pick the best match
        if (IsVersionCompatible(availableVersion, versionSpec))
            return availableVersion;
        
        return null;
    }

    private void ValidateResolution(ResolutionResult result)
    {
        // Check for any unresolvable conflicts
        if (result.Conflicts.Any())
        {
            var conflictMessages = result.Conflicts.Select(c => c.Message);
            throw new DependencyResolutionException(
                $"Unable to resolve dependencies due to version conflicts:\n{string.Join("\n", conflictMessages)}"
            );
        }
    }

    private string GenerateIntegrity(string packageName, string version)
    {
        // Simplified integrity hash - in production would use actual package content
        var content = $"{packageName}@{version}";
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(content));
        return Convert.ToBase64String(hash)[..16];
    }
}

/// <summary>
/// Semantic version implementation following semver.org specification
/// </summary>
public record SemanticVersion(int Major, int Minor, int Patch, string? PreRelease = null, string? Build = null) 
    : IComparable<SemanticVersion>
{
    private static readonly Regex VersionRegex = new(
        @"^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)(?:-(?<prerelease>(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+(?<build>[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$",
        RegexOptions.Compiled);

    public static SemanticVersion Parse(string version)
    {
        var match = VersionRegex.Match(version);
        if (!match.Success)
            throw new ArgumentException($"Invalid semantic version: {version}");

        return new SemanticVersion(
            int.Parse(match.Groups["major"].Value),
            int.Parse(match.Groups["minor"].Value),
            int.Parse(match.Groups["patch"].Value),
            match.Groups["prerelease"].Value.NullIfEmpty(),
            match.Groups["build"].Value.NullIfEmpty()
        );
    }

    public int CompareTo(SemanticVersion? other)
    {
        if (other == null) return 1;

        var majorComparison = Major.CompareTo(other.Major);
        if (majorComparison != 0) return majorComparison;

        var minorComparison = Minor.CompareTo(other.Minor);
        if (minorComparison != 0) return minorComparison;

        var patchComparison = Patch.CompareTo(other.Patch);
        if (patchComparison != 0) return patchComparison;

        // Handle pre-release versions (pre-release < normal version)
        if (PreRelease == null && other.PreRelease != null) return 1;
        if (PreRelease != null && other.PreRelease == null) return -1;
        if (PreRelease != null && other.PreRelease != null)
            return string.Compare(PreRelease, other.PreRelease, StringComparison.OrdinalIgnoreCase);

        return 0;
    }

    public static bool operator >(SemanticVersion left, SemanticVersion right) => left.CompareTo(right) > 0;
    public static bool operator <(SemanticVersion left, SemanticVersion right) => left.CompareTo(right) < 0;
    public static bool operator >=(SemanticVersion left, SemanticVersion right) => left.CompareTo(right) >= 0;
    public static bool operator <=(SemanticVersion left, SemanticVersion right) => left.CompareTo(right) <= 0;

    public override string ToString() => $"{Major}.{Minor}.{Patch}" +
                                        (PreRelease != null ? $"-{PreRelease}" : "") +
                                        (Build != null ? $"+{Build}" : "");
}

public class ResolutionResult
{
    public Dictionary<string, ResolvedPackage> ResolvedPackages { get; } = new();
    public List<VersionConflict> Conflicts { get; } = new();
    public List<string> Errors { get; } = new();

    public bool HasConflicts => Conflicts.Any();
    public bool HasErrors => Errors.Any();
    public bool IsSuccessful => !HasConflicts && !HasErrors;
}

public record VersionConflict(string PackageName, string ResolvedVersion, string RequestedVersion, string Message);

public class CircularDependencyException : Exception
{
    public CircularDependencyException(string message) : base(message) { }
}

public class DependencyResolutionException : Exception
{
    public DependencyResolutionException(string message) : base(message) { }
}

public static class StringExtensions
{
    public static string? NullIfEmpty(this string? str) => string.IsNullOrEmpty(str) ? null : str;
}