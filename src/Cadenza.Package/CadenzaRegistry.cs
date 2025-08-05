using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Cadenza.Package;

/// <summary>
/// Interface for Cadenza package registry operations
/// </summary>
public interface IPackageRegistry
{
    Task<PackageMetadata?> GetPackageMetadataAsync(string packageName, string? version = null);
    Task<List<PackageMetadata>> SearchPackagesAsync(string query, int skip = 0, int take = 20);
    Task<Stream> DownloadPackageAsync(string packageName, string version);
    Task<PublishResult> PublishPackageAsync(string packagePath, PublishOptions options);
    Task<List<string>> GetVersionsAsync(string packageName);
    Task<bool> PackageExistsAsync(string packageName, string version);
    Task<RegistryInfo> GetRegistryInfoAsync();
}

/// <summary>
/// Cadenza package registry client implementation
/// </summary>
public class CadenzaRegistryClient : IPackageRegistry, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _registryUrl;
    private readonly string? _authToken;

    public CadenzaRegistryClient(string registryUrl, string? authToken = null)
    {
        _registryUrl = registryUrl.TrimEnd('/');
        _authToken = authToken;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Cadenza Package Manager 1.0.0");
        
        if (!string.IsNullOrEmpty(_authToken))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");
        }
    }

    public async Task<PackageMetadata?> GetPackageMetadataAsync(string packageName, string? version = null)
    {
        // If using the default non-existent registry, return null immediately
        if (_registryUrl.Contains("packages.cadenza.org") || _registryUrl.Contains("api.nuget.org"))
        {
            return null;
        }
        
        try
        {
            var url = version != null 
                ? $"{_registryUrl}/api/packages/{packageName}/{version}"
                : $"{_registryUrl}/api/packages/{packageName}";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;
                    
                throw new RegistryException($"Failed to get package metadata: {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PackageMetadata>(json, JsonOptions);
        }
        catch (HttpRequestException ex)
        {
            throw new RegistryException($"Network error accessing registry: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            throw new RegistryException($"Invalid package metadata format: {ex.Message}", ex);
        }
    }

    public async Task<List<PackageMetadata>> SearchPackagesAsync(string query, int skip = 0, int take = 20)
    {
        // If using the default non-existent registry, return empty list immediately
        if (_registryUrl.Contains("packages.cadenza.org") || _registryUrl.Contains("api.nuget.org"))
        {
            return new List<PackageMetadata>();
        }
        
        try
        {
            var url = $"{_registryUrl}/api/search?q={Uri.EscapeDataString(query)}&skip={skip}&take={take}";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                throw new RegistryException($"Search failed: {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var searchResult = JsonSerializer.Deserialize<SearchResult>(json, JsonOptions);
            
            return searchResult?.Packages ?? new List<PackageMetadata>();
        }
        catch (HttpRequestException ex)
        {
            throw new RegistryException($"Network error during search: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            throw new RegistryException($"Invalid search result format: {ex.Message}", ex);
        }
    }

    public async Task<Stream> DownloadPackageAsync(string packageName, string version)
    {
        try
        {
            var url = $"{_registryUrl}/api/packages/{packageName}/{version}/download";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    throw new PackageNotFoundException($"Package {packageName}@{version} not found");
                    
                throw new RegistryException($"Download failed: {response.StatusCode}");
            }

            return await response.Content.ReadAsStreamAsync();
        }
        catch (HttpRequestException ex)
        {
            throw new RegistryException($"Network error during download: {ex.Message}", ex);
        }
    }

    public async Task<PublishResult> PublishPackageAsync(string packagePath, PublishOptions options)
    {
        if (!File.Exists(packagePath))
            throw new FileNotFoundException($"Package file not found: {packagePath}");

        try
        {
            using var form = new MultipartFormDataContent();
            
            // Add package file
            var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(packagePath));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            form.Add(fileContent, "package", Path.GetFileName(packagePath));

            // Add metadata
            if (options.Access != null)
                form.Add(new StringContent(options.Access), "access");
            
            if (options.Tags?.Any() == true)
                form.Add(new StringContent(string.Join(",", options.Tags)), "tags");

            var url = $"{_registryUrl}/api/publish";
            var response = await _httpClient.PostAsync(url, form);

            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                var errorResult = JsonSerializer.Deserialize<ErrorResult>(responseContent, JsonOptions);
                throw new PublishException($"Publish failed: {errorResult?.Message ?? response.StatusCode.ToString()}");
            }

            return JsonSerializer.Deserialize<PublishResult>(responseContent, JsonOptions) ?? 
                   new PublishResult(false, "Unknown error");
        }
        catch (HttpRequestException ex)
        {
            throw new RegistryException($"Network error during publish: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            throw new RegistryException($"Invalid publish response format: {ex.Message}", ex);
        }
    }

    public async Task<List<string>> GetVersionsAsync(string packageName)
    {
        try
        {
            var url = $"{_registryUrl}/api/packages/{packageName}/versions";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return new List<string>();
                    
                throw new RegistryException($"Failed to get versions: {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<VersionsResult>(json, JsonOptions);
            
            return result?.Versions?.OrderByDescending(v => SemanticVersion.Parse(v)).ToList() ?? new List<string>();
        }
        catch (HttpRequestException ex)
        {
            throw new RegistryException($"Network error getting versions: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            throw new RegistryException($"Invalid versions format: {ex.Message}", ex);
        }
    }

    public async Task<bool> PackageExistsAsync(string packageName, string version)
    {
        var metadata = await GetPackageMetadataAsync(packageName, version);
        return metadata != null;
    }

    public async Task<RegistryInfo> GetRegistryInfoAsync()
    {
        try
        {
            var url = $"{_registryUrl}/api/info";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
                throw new RegistryException($"Failed to get registry info: {response.StatusCode}");

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<RegistryInfo>(json, JsonOptions) ?? new RegistryInfo();
        }
        catch (HttpRequestException ex)
        {
            throw new RegistryException($"Network error getting registry info: {ex.Message}", ex);
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

/// <summary>
/// Package creator for Cadenza packages
/// </summary>
public class PackageCreator
{
    public async Task<string> CreatePackageAsync(string projectPath, string outputPath)
    {
        var config = await ConfigurationManager.LoadConfigAsync(Path.Combine(projectPath, "cadenzac.json"));
        
        // Validate package
        ValidatePackage(config, projectPath);

        // Create package archive
        var packageFileName = $"{config.Name}-{config.Version}.zip";
        var fullOutputPath = Path.Combine(outputPath, packageFileName);

        using (var fileStream = File.Create(fullOutputPath))
        using (var archive = new System.IO.Compression.ZipArchive(fileStream, System.IO.Compression.ZipArchiveMode.Create))
        {
            await AddFilesToArchive(archive, projectPath, config);
        }

        Console.WriteLine($"Created package: {fullOutputPath}");
        return fullOutputPath;
    }

    private void ValidatePackage(EnhancedFlowcConfig config, string projectPath)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(config.Name))
            errors.Add("Package name is required");

        if (string.IsNullOrWhiteSpace(config.Version))
            errors.Add("Package version is required");

        try
        {
            SemanticVersion.Parse(config.Version);
        }
        catch
        {
            errors.Add($"Invalid semantic version: {config.Version}");
        }

        // Check for main entry point
        var srcPath = Path.Combine(projectPath, config.Build.Source);
        if (!Directory.Exists(srcPath))
            errors.Add($"Source directory not found: {srcPath}");

        var flowFiles = Directory.GetFiles(srcPath, "*.cdz", SearchOption.AllDirectories);
        if (!flowFiles.Any())
            errors.Add("No Cadenza source files found");

        if (errors.Any())
            throw new PackageValidationException($"Package validation failed:\n{string.Join("\n", errors)}");
    }

    private async Task AddFilesToArchive(System.IO.Compression.ZipArchive archive, string projectPath, EnhancedFlowcConfig config)
    {
        // Add package.json (cadenzac.json)
        await AddFileToArchive(archive, Path.Combine(projectPath, "cadenzac.json"), "cadenzac.json");

        // Add source files
        var srcPath = Path.Combine(projectPath, config.Build.Source);
        if (Directory.Exists(srcPath))
        {
            await AddDirectoryToArchive(archive, srcPath, "src/");
        }

        // Add README if exists
        var readmePath = Path.Combine(projectPath, "README.md");
        if (File.Exists(readmePath))
        {
            await AddFileToArchive(archive, readmePath, "README.md");
        }

        // Add LICENSE if exists
        var licensePath = Path.Combine(projectPath, "LICENSE");
        if (File.Exists(licensePath))
        {
            await AddFileToArchive(archive, licensePath, "LICENSE");
        }
    }

    private async Task AddFileToArchive(System.IO.Compression.ZipArchive archive, string filePath, string archivePath)
    {
        var entry = archive.CreateEntry(archivePath);
        
        using var entryStream = entry.Open();
        using var fileStream = File.OpenRead(filePath);
        await fileStream.CopyToAsync(entryStream);
    }

    private async Task AddDirectoryToArchive(System.IO.Compression.ZipArchive archive, string dirPath, string archivePrefix)
    {
        foreach (var file in Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(dirPath, file);
            var archivePath = archivePrefix + relativePath.Replace('\\', '/');
            await AddFileToArchive(archive, file, archivePath);
        }
    }
}

// Data models for registry operations
public record SearchResult(List<PackageMetadata> Packages, int Total);

public record VersionsResult(List<string> Versions);

public record PublishResult(bool Success, string Message, string? PackageUrl = null);

public record PublishOptions(
    string? Access = "public",
    List<string>? Tags = null,
    bool DryRun = false
);

public record ErrorResult(string Message, string? Code = null);

public record RegistryInfo(
    string Name = "Cadenza Package Registry",
    string Version = "1.0.0",
    string Description = "Cadenza package registry",
    int PackageCount = 0,
    DateTime LastUpdated = default
);

// Exceptions
public class RegistryException : Exception
{
    public RegistryException(string message) : base(message) { }
    public RegistryException(string message, Exception innerException) : base(message, innerException) { }
}

public class PublishException : Exception
{
    public PublishException(string message) : base(message) { }
}

public class PackageValidationException : Exception
{
    public PackageValidationException(string message) : base(message) { }
}