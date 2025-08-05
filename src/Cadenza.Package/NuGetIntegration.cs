using System.Net.Http;
using System.IO.Compression;
using System.Text.Json;

namespace Cadenza.Package;

/// <summary>
/// Interface for NuGet package operations
/// </summary>
public interface INuGetClient
{
    Task<NuGetPackage?> GetPackageAsync(string packageId, string versionSpec);
    Task<List<NuGetPackage>> SearchPackagesAsync(string query, int skip = 0, int take = 20);
    Task<List<string>> GetVersionsAsync(string packageId);
    Task<Stream> DownloadPackageAsync(string packageId, string version);
    Task<bool> PackageExistsAsync(string packageId, string version);
}

/// <summary>
/// NuGet package representation
/// </summary>
public record NuGetPackage(
    string Id,
    string Version,
    string Description,
    List<string> Authors,
    string ProjectUrl,
    string LicenseUrl,
    Dictionary<string, string> Dependencies,
    string DownloadUrl,
    string Hash,
    DateTime Published,
    long DownloadCount
);

/// <summary>
/// NuGet client implementation using NuGet APIs
/// </summary>
public class NuGetClient : INuGetClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly List<string> _packageSources;
    private readonly Dictionary<string, NuGetServiceIndex> _serviceIndexCache = new();

    public NuGetClient(List<string> packageSources)
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Cadenza Package Manager 1.0.0");
        _packageSources = packageSources;
    }

    public async Task<NuGetPackage?> GetPackageAsync(string packageId, string versionSpec)
    {
        foreach (var source in _packageSources)
        {
            try
            {
                var serviceIndex = await GetServiceIndexAsync(source);
                var registrationUrl = serviceIndex.GetRegistrationBaseUrl();
                
                if (registrationUrl == null) continue;

                var registrationResponse = await _httpClient.GetStringAsync($"{registrationUrl}{packageId.ToLowerInvariant()}/index.json");
                var registration = JsonSerializer.Deserialize<NuGetRegistration>(registrationResponse);

                if (registration?.Items == null) continue;

                // Find the best matching version
                foreach (var page in registration.Items)
                {
                    if (page.Items != null)
                    {
                        foreach (var item in page.Items)
                        {
                            if (item.CatalogEntry != null && IsVersionMatch(item.CatalogEntry.Version, versionSpec))
                            {
                                return new NuGetPackage(
                                    Id: item.CatalogEntry.Id,
                                    Version: item.CatalogEntry.Version,
                                    Description: item.CatalogEntry.Description ?? "",
                                    Authors: item.CatalogEntry.Authors?.Split(',').Select(a => a.Trim()).ToList() ?? new(),
                                    ProjectUrl: item.CatalogEntry.ProjectUrl ?? "",
                                    LicenseUrl: item.CatalogEntry.LicenseUrl ?? "",
                                    Dependencies: ExtractDependencies(item.CatalogEntry.DependencyGroups),
                                    DownloadUrl: item.PackageContent ?? "",
                                    Hash: "", // Would need to compute from package content
                                    Published: item.CatalogEntry.Published,
                                    DownloadCount: 0 // Not available in registration API
                                );
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to query NuGet source {source}: {ex.Message}");
                continue;
            }
        }

        return null;
    }

    public async Task<List<NuGetPackage>> SearchPackagesAsync(string query, int skip = 0, int take = 20)
    {
        var results = new List<NuGetPackage>();

        foreach (var source in _packageSources)
        {
            try
            {
                var serviceIndex = await GetServiceIndexAsync(source);
                var searchUrl = serviceIndex.GetSearchQueryServiceUrl();
                
                if (searchUrl == null) continue;

                var searchResponse = await _httpClient.GetStringAsync(
                    $"{searchUrl}?q={Uri.EscapeDataString(query)}&skip={skip}&take={take}&prerelease=true");
                var searchResult = JsonSerializer.Deserialize<NuGetSearchResult>(searchResponse);

                if (searchResult?.Data != null)
                {
                    foreach (var package in searchResult.Data)
                    {
                        results.Add(new NuGetPackage(
                            Id: package.Id,
                            Version: package.Version,
                            Description: package.Description ?? "",
                            Authors: package.Authors?.ToList() ?? new(),
                            ProjectUrl: package.ProjectUrl ?? "",
                            LicenseUrl: package.LicenseUrl ?? "",
                            Dependencies: new(), // Not available in search results
                            DownloadUrl: "",
                            Hash: "",
                            Published: DateTime.MinValue,
                            DownloadCount: package.TotalDownloads
                        ));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to search NuGet source {source}: {ex.Message}");
                continue;
            }
        }

        return results.DistinctBy(p => p.Id).ToList();
    }

    public async Task<List<string>> GetVersionsAsync(string packageId)
    {
        foreach (var source in _packageSources)
        {
            try
            {
                var serviceIndex = await GetServiceIndexAsync(source);
                var registrationUrl = serviceIndex.GetRegistrationBaseUrl();
                
                if (registrationUrl == null) continue;

                var registrationResponse = await _httpClient.GetStringAsync($"{registrationUrl}{packageId.ToLowerInvariant()}/index.json");
                var registration = JsonSerializer.Deserialize<NuGetRegistration>(registrationResponse);

                if (registration?.Items == null) continue;

                var versions = new List<string>();
                foreach (var page in registration.Items)
                {
                    if (page.Items != null)
                    {
                        foreach (var item in page.Items)
                        {
                            if (item.CatalogEntry?.Version != null)
                            {
                                versions.Add(item.CatalogEntry.Version);
                            }
                        }
                    }
                }

                return versions.OrderByDescending(v => SemanticVersion.Parse(v)).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to get versions from {source}: {ex.Message}");
                continue;
            }
        }

        return new List<string>();
    }

    public async Task<Stream> DownloadPackageAsync(string packageId, string version)
    {
        foreach (var source in _packageSources)
        {
            try
            {
                var serviceIndex = await GetServiceIndexAsync(source);
                var packageBaseUrl = serviceIndex.GetPackageBaseAddressUrl();
                
                if (packageBaseUrl == null) continue;

                var downloadUrl = $"{packageBaseUrl}{packageId.ToLowerInvariant()}/{version.ToLowerInvariant()}/{packageId.ToLowerInvariant()}.{version.ToLowerInvariant()}.nupkg";
                var response = await _httpClient.GetAsync(downloadUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStreamAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to download from {source}: {ex.Message}");
                continue;
            }
        }

        throw new PackageNotFoundException($"Package {packageId}@{version} not found in any configured source");
    }

    public async Task<bool> PackageExistsAsync(string packageId, string version)
    {
        try
        {
            using var stream = await DownloadPackageAsync(packageId, version);
            return true;
        }
        catch (PackageNotFoundException)
        {
            return false;
        }
    }

    private async Task<NuGetServiceIndex> GetServiceIndexAsync(string source)
    {
        if (_serviceIndexCache.TryGetValue(source, out var cached))
            return cached;

        var indexUrl = source.EndsWith("/index.json") ? source : $"{source.TrimEnd('/')}/index.json";
        var response = await _httpClient.GetStringAsync(indexUrl);
        var serviceIndex = JsonSerializer.Deserialize<NuGetServiceIndex>(response) ?? new NuGetServiceIndex();
        
        _serviceIndexCache[source] = serviceIndex;
        return serviceIndex;
    }

    private bool IsVersionMatch(string availableVersion, string versionSpec)
    {
        try
        {
            var resolver = new DependencyResolver(null!, this);
            return resolver.IsVersionCompatible(availableVersion, versionSpec);
        }
        catch
        {
            return false;
        }
    }

    private Dictionary<string, string> ExtractDependencies(List<DependencyGroup>? dependencyGroups)
    {
        var dependencies = new Dictionary<string, string>();
        
        if (dependencyGroups == null) return dependencies;

        foreach (var group in dependencyGroups)
        {
            if (group.Dependencies != null)
            {
                foreach (var dep in group.Dependencies)
                {
                    if (!string.IsNullOrEmpty(dep.Id) && !string.IsNullOrEmpty(dep.Range))
                    {
                        dependencies[dep.Id] = dep.Range;
                    }
                }
            }
        }

        return dependencies;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

/// <summary>
/// Automatic Cadenza binding generator for .NET libraries
/// </summary>
public class BindingGenerator
{
    private readonly Dictionary<string, List<string>> _effectMappings;

    public BindingGenerator(Dictionary<string, List<string>> effectMappings)
    {
        _effectMappings = effectMappings;
    }

    /// <summary>
    /// Generate Cadenza bindings for a NuGet package
    /// </summary>
    public async Task<string> GenerateBindingsAsync(NuGetPackage package, Stream packageStream)
    {
        using var archive = new ZipArchive(packageStream, ZipArchiveMode.Read);
        
        // Find .dll files in the package
        var dllEntries = archive.Entries
            .Where(e => e.FullName.EndsWith(".dll") && !e.FullName.Contains("/ref/"))
            .ToList();

        if (!dllEntries.Any())
        {
            return $"// No assemblies found in package {package.Id}";
        }

        var bindings = new List<string>
        {
            $"// Generated Cadenza bindings for {package.Id} v{package.Version}",
            $"// Description: {package.Description}",
            "",
            $"module {SanitizeModuleName(package.Id)} {{"
        };

        // For each assembly, generate bindings (simplified approach)
        foreach (var dllEntry in dllEntries)
        {
            try
            {
                // In a real implementation, we would use Reflection to analyze the assembly
                // For now, generate placeholder bindings based on common patterns
                bindings.AddRange(GenerateCommonBindings(package));
            }
            catch (Exception ex)
            {
                bindings.Add($"    // Warning: Could not analyze {dllEntry.Name}: {ex.Message}");
            }
        }

        bindings.Add("}");
        
        return string.Join(Environment.NewLine, bindings);
    }

    private List<string> GenerateCommonBindings(NuGetPackage package)
    {
        var bindings = new List<string>();
        var effects = InferEffects(package.Id);
        var effectsStr = effects.Any() ? $" uses [{string.Join(", ", effects)}]" : "";

        // Generate common method patterns based on package type
        if (package.Id.Contains("Http"))
        {
            bindings.AddRange(new[]
            {
                "",
                $"    function get(url: string){effectsStr} -> Result<string, HttpError> {{",
                "        // Implementation bridges to .NET HttpClient",
                "        return Error(\"Not implemented\")",
                "    }",
                "",
                $"    function post(url: string, body: string){effectsStr} -> Result<string, HttpError> {{",
                "        // Implementation bridges to .NET HttpClient",
                "        return Error(\"Not implemented\")",
                "    }"
            });
        }
        else if (package.Id.Contains("Sql") || package.Id.Contains("Database"))
        {
            bindings.AddRange(new[]
            {
                "",
                $"    function execute_query(sql: string){effectsStr} -> Result<List<Row>, DatabaseError> {{",
                "        // Implementation bridges to .NET database provider",
                "        return Error(\"Not implemented\")",
                "    }",
                "",
                $"    function execute_command(sql: string){effectsStr} -> Result<int, DatabaseError> {{",
                "        // Implementation bridges to .NET database provider", 
                "        return Error(\"Not implemented\")",
                "    }"
            });
        }
        else
        {
            bindings.AddRange(new[]
            {
                "",
                $"    function initialize(){effectsStr} -> Result<Unit, Error> {{",
                $"        // Initialize {package.Id}",
                "        return Error(\"Not implemented\")",
                "    }"
            });
        }

        return bindings;
    }

    private List<string> InferEffects(string packageId)
    {
        foreach (var (pattern, mappedEffects) in _effectMappings)
        {
            if (packageId.StartsWith(pattern.Replace("*", "")))
                return mappedEffects;
        }

        // Default inference
        var effects = new List<string>();
        var lower = packageId.ToLowerInvariant();

        if (lower.Contains("http") || lower.Contains("client")) effects.Add("Network");
        if (lower.Contains("sql") || lower.Contains("database")) effects.Add("Database");
        if (lower.Contains("log")) effects.Add("Logging");
        if (lower.Contains("file") || lower.Contains("io")) effects.Add("FileSystem");
        if (lower.Contains("cache") || lower.Contains("memory")) effects.Add("Memory");

        return effects.Any() ? effects : new List<string> { "IO" };
    }

    private string SanitizeModuleName(string packageId)
    {
        return packageId.Replace(".", "_").Replace("-", "_");
    }
}

// NuGet API data models
public class NuGetServiceIndex
{
    public List<NuGetService> Resources { get; set; } = new();

    public string? GetRegistrationBaseUrl() => 
        Resources.FirstOrDefault(r => r.Type == "RegistrationsBaseUrl/3.6.0")?.Id;

    public string? GetSearchQueryServiceUrl() => 
        Resources.FirstOrDefault(r => r.Type == "SearchQueryService/3.5.0")?.Id;

    public string? GetPackageBaseAddressUrl() => 
        Resources.FirstOrDefault(r => r.Type == "PackageBaseAddress/3.0.0")?.Id;
}

public class NuGetService
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
}

public class NuGetRegistration
{
    public List<RegistrationPage> Items { get; set; } = new();
}

public class RegistrationPage
{
    public List<RegistrationLeaf>? Items { get; set; }
}

public class RegistrationLeaf
{
    public CatalogEntry? CatalogEntry { get; set; }
    public string? PackageContent { get; set; }
}

public class CatalogEntry
{
    public string Id { get; set; } = "";
    public string Version { get; set; } = "";
    public string? Description { get; set; }
    public string? Authors { get; set; }
    public string? ProjectUrl { get; set; }
    public string? LicenseUrl { get; set; }
    public DateTime Published { get; set; }
    public List<DependencyGroup>? DependencyGroups { get; set; }
}

public class DependencyGroup
{
    public string? TargetFramework { get; set; }
    public List<Dependency>? Dependencies { get; set; }
}

public class Dependency
{
    public string Id { get; set; } = "";
    public string Range { get; set; } = "";
}

public class NuGetSearchResult
{
    public List<SearchPackage> Data { get; set; } = new();
}

public class SearchPackage
{
    public string Id { get; set; } = "";
    public string Version { get; set; } = "";
    public string? Description { get; set; }
    public string[]? Authors { get; set; }
    public string? ProjectUrl { get; set; }
    public string? LicenseUrl { get; set; }
    public long TotalDownloads { get; set; }
}

public class PackageNotFoundException : Exception
{
    public PackageNotFoundException(string message) : base(message) { }
}