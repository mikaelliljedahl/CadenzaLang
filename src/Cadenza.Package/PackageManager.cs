
using System.IO.Compression;

namespace Cadenza.Package;

/// <summary>
/// Main package manager coordinator that orchestrates all package operations
/// </summary>
public class PackageManager
{
    private readonly IPackageRegistry _registry;
    private readonly INuGetClient _nugetClient;
    private readonly DependencyResolver _resolver;
    private readonly BindingGenerator _bindingGenerator;
    private readonly LockFileManager _lockFileManager;
    private readonly WorkspaceManager _workspaceManager;

    public PackageManager(EnhancedFlowcConfig? config = null)
    {
        config ??= new EnhancedFlowcConfig();
        
        _registry = new CadenzaRegistryClient(config.CadenzaRegistry);
        _nugetClient = new NuGetClient(config.NugetSources);
        _resolver = new DependencyResolver(_registry, _nugetClient);
        _bindingGenerator = new BindingGenerator(config.EffectMappings);
        _lockFileManager = new LockFileManager();
        _workspaceManager = new WorkspaceManager();
    }

    /// <summary>
    /// Add a package dependency to the project
    /// </summary>
    public async Task<AddResult> AddPackageAsync(string packageSpec, bool isDev = false, bool save = true)
    {
        var (packageName, versionSpec) = ParsePackageSpec(packageSpec);
        
        try
        {
            // Resolve the package to get exact version
            var resolved = await ResolvePackageSpec(packageName, versionSpec);
            if (resolved == null)
            {
                return new AddResult(false, $"Package not found: {packageSpec}");
            }

            // Load current config
            var config = await ConfigurationManager.LoadConfigAsync();
            
            // Add to appropriate dependencies section
            if (isDev)
            {
                config.DevDependencies[packageName] = versionSpec;
            }
            else
            {
                config.Dependencies[packageName] = versionSpec;
            }

            // Save config if requested
            if (save)
            {
                await ConfigurationManager.SaveConfigAsync(config);
            }

            // Install the package
            var installResult = await InstallPackagesAsync();
            
            if (installResult.Success)
            {
                Console.WriteLine($"Added {packageName}@{resolved.Version}");
                return new AddResult(true, $"Successfully added {packageName}@{resolved.Version}");
            }
            else
            {
                return new AddResult(false, $"Failed to install {packageName}: {installResult.Message}");
            }
        }
        catch (Exception ex)
        {
            return new AddResult(false, $"Error adding package: {ex.Message}");
        }
    }

    /// <summary>
    /// Remove a package dependency from the project
    /// </summary>
    public async Task<RemoveResult> RemovePackageAsync(string packageName, bool save = true)
    {
        try
        {
            var config = await ConfigurationManager.LoadConfigAsync();
            
            bool removed = false;
            if (config.Dependencies.Remove(packageName))
                removed = true;
            if (config.DevDependencies.Remove(packageName))
                removed = true;

            if (!removed)
            {
                return new RemoveResult(false, $"Package {packageName} not found in dependencies");
            }

            if (save)
            {
                await ConfigurationManager.SaveConfigAsync(config);
            }

            // Clean up lock file
            await _lockFileManager.RemovePackageAsync(packageName);

            Console.WriteLine($"Removed {packageName}");
            return new RemoveResult(true, $"Successfully removed {packageName}");
        }
        catch (Exception ex)
        {
            return new RemoveResult(false, $"Error removing package: {ex.Message}");
        }
    }

    /// <summary>
    /// Install all dependencies for the current project
    /// </summary>
    public async Task<InstallResult> InstallPackagesAsync(bool includeDev = true)
    {
        try
        {
            var config = await ConfigurationManager.LoadConfigAsync();
            
            Console.WriteLine("Resolving dependencies...");
            var resolution = await _resolver.ResolveAsync(config, includeDev);
            
            if (!resolution.IsSuccessful)
            {
                var errors = string.Join("\n", resolution.Errors);
                return new InstallResult(false, $"Dependency resolution failed:\n{errors}");
            }

            Console.WriteLine($"Installing {resolution.ResolvedPackages.Count} packages...");
            
            var packagesDir = Path.Combine(Directory.GetCurrentDirectory(), "packages");
            Directory.CreateDirectory(packagesDir);

            var lockFile = new LockFile();
            var installed = new List<string>();

            foreach (var (packageName, package) in resolution.ResolvedPackages)
            {
                try
                {
                    await InstallSinglePackage(packageName, package, packagesDir);
                    
                    lockFile.Resolved[packageName] = package;
                    installed.Add($"{packageName}@{package.Version}");
                    
                    Console.WriteLine($"✓ {packageName}@{package.Version}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Failed to install {packageName}: {ex.Message}");
                    return new InstallResult(false, $"Failed to install {packageName}: {ex.Message}");
                }
            }

            // Save lock file
            await ConfigurationManager.SaveLockFileAsync(lockFile);

            var message = $"Successfully installed {installed.Count} packages:\n{string.Join("\n", installed)}";
            return new InstallResult(true, message);
        }
        catch (Exception ex)
        {
            return new InstallResult(false, $"Installation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Update packages to their latest compatible versions
    /// </summary>
    public async Task<UpdateResult> UpdatePackagesAsync(string? specificPackage = null)
    {
        try
        {
            var config = await ConfigurationManager.LoadConfigAsync();
            var packagesToUpdate = specificPackage != null 
                ? new[] { specificPackage }
                : config.Dependencies.Keys.Concat(config.DevDependencies.Keys);

            var updated = new List<string>();
            var errors = new List<string>();

            foreach (var packageName in packagesToUpdate)
            {
                try
                {
                    var currentVersion = GetCurrentVersion(packageName, config);
                    if (currentVersion == null)
                    {
                        errors.Add($"Package {packageName} not found in dependencies");
                        continue;
                    }

                    var latestVersion = await GetLatestCompatibleVersion(packageName, currentVersion);
                    if (latestVersion != null && latestVersion != currentVersion)
                    {
                        // Update in config
                        if (config.Dependencies.ContainsKey(packageName))
                            config.Dependencies[packageName] = $"^{latestVersion}";
                        else if (config.DevDependencies.ContainsKey(packageName))
                            config.DevDependencies[packageName] = $"^{latestVersion}";

                        updated.Add($"{packageName}: {currentVersion} → {latestVersion}");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to update {packageName}: {ex.Message}");
                }
            }

            if (updated.Any())
            {
                await ConfigurationManager.SaveConfigAsync(config);
                var installResult = await InstallPackagesAsync();
                
                if (!installResult.Success)
                {
                    return new UpdateResult(false, $"Updates resolved but installation failed: {installResult.Message}");
                }
            }

            var message = updated.Any() 
                ? $"Updated packages:\n{string.Join("\n", updated)}"
                : "All packages are up to date";
                
            if (errors.Any())
            {
                message += $"\nErrors:\n{string.Join("\n", errors)}";
            }

            return new UpdateResult(updated.Any(), message);
        }
        catch (Exception ex)
        {
            return new UpdateResult(false, $"Update failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Search for packages in registries
    /// </summary>
    public async Task<List<PackageSearchResult>> SearchPackagesAsync(string query)
    {
        var results = new List<PackageSearchResult>();

        // Search Cadenza registry
        try
        {
            var flowLangPackages = await _registry.SearchPackagesAsync(query);
            results.AddRange(flowLangPackages.Select(p => new PackageSearchResult(
                p.Name, p.Version, p.Description, PackageType.Cadenza, p.DownloadCount)));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Cadenza registry search failed: {ex.Message}");
        }

        // Search NuGet registry
        try
        {
            var nugetPackages = await _nugetClient.SearchPackagesAsync(query);
            results.AddRange(nugetPackages.Select(p => new PackageSearchResult(
                p.Id, p.Version, p.Description, PackageType.NuGet, p.DownloadCount)));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: NuGet search failed: {ex.Message}");
        }

        return results.OrderByDescending(r => r.DownloadCount).ToList();
    }

    /// <summary>
    /// Get detailed information about a package
    /// </summary>
    public async Task<PackageInfo?> GetPackageInfoAsync(string packageName)
    {
        // Try Cadenza registry first
        try
        {
            var metadata = await _registry.GetPackageMetadataAsync(packageName);
            if (metadata != null)
            {
                var versions = await _registry.GetVersionsAsync(packageName);
                return new PackageInfo(
                    metadata.Name, metadata.Version, metadata.Description,
                    PackageType.Cadenza, metadata.Keywords, versions,
                    metadata.Homepage, metadata.Repository, metadata.License,
                    metadata.Author?.Name, metadata.Dependencies, metadata.Effects);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to get Cadenza package info: {ex.Message}");
        }

        // Try NuGet registry
        try
        {
            var nugetPackage = await _nugetClient.GetPackageAsync(packageName, "*");
            if (nugetPackage != null)
            {
                var versions = await _nugetClient.GetVersionsAsync(packageName);
                return new PackageInfo(
                    nugetPackage.Id, nugetPackage.Version, nugetPackage.Description,
                    PackageType.NuGet, new List<string>(), versions,
                    nugetPackage.ProjectUrl, "", nugetPackage.LicenseUrl,
                    string.Join(", ", nugetPackage.Authors), nugetPackage.Dependencies,
                    new List<string>());
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to get NuGet package info: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Publish a package to the Cadenza registry
    /// </summary>
    public async Task<PublishResult> PublishPackageAsync(string? projectPath = null, PublishOptions? options = null)
    {
        projectPath ??= Directory.GetCurrentDirectory();
        options ??= new PublishOptions();

        try
        {
            var packageCreator = new PackageCreator();
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                var packagePath = await packageCreator.CreatePackageAsync(projectPath, tempDir);
                return await _registry.PublishPackageAsync(packagePath, options);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }
        catch (Exception ex)
        {
            return new PublishResult(false, $"Publish failed: {ex.Message}");
        }
    }

    // Private helper methods
    private async Task InstallSinglePackage(string packageName, ResolvedPackage package, string packagesDir)
    {
        var packageDir = Path.Combine(packagesDir, packageName, package.Version);
        
        if (Directory.Exists(packageDir))
        {
            // Already installed
            return;
        }

        Directory.CreateDirectory(packageDir);

        if (package.Type == PackageType.Cadenza)
        {
            await InstallCadenzaPackage(packageName, package, packageDir);
        }
        else if (package.Type == PackageType.NuGet)
        {
            await InstallNuGetPackage(packageName, package, packageDir);
        }
    }

    private async Task InstallCadenzaPackage(string packageName, ResolvedPackage package, string packageDir)
    {
        using var packageStream = await _registry.DownloadPackageAsync(packageName, package.Version);
        using var archive = new ZipArchive(packageStream, ZipArchiveMode.Read);

        // Extract all entries manually since ExtractToDirectory might not be available
        foreach (var entry in archive.Entries)
        {
            var entryPath = Path.Combine(packageDir, entry.FullName);
            Directory.CreateDirectory(Path.GetDirectoryName(entryPath) ?? packageDir);
            
            if (!string.IsNullOrEmpty(entry.Name)) // Skip directories
            {
                using var entryStream = entry.Open();
                using var fileStream = File.Create(entryPath);
                await entryStream.CopyToAsync(fileStream);
            }
        }
    }

    private async Task InstallNuGetPackage(string packageName, ResolvedPackage package, string packageDir)
    {
        using var packageStream = await _nugetClient.DownloadPackageAsync(packageName, package.Version);
        using var archive = new ZipArchive(packageStream, ZipArchiveMode.Read);
        
        archive.ExtractToDirectory(packageDir);

        // Generate Cadenza bindings
        var bindings = await _bindingGenerator.GenerateBindingsAsync(
            new NuGetPackage(packageName, package.Version, "", new(), "", "", 
                            package.Dependencies, package.Resolved, "", DateTime.MinValue, 0),
            await _nugetClient.DownloadPackageAsync(packageName, package.Version));

        var bindingsPath = Path.Combine(packageDir, $"{packageName}.cdz");
        await File.WriteAllTextAsync(bindingsPath, bindings);
    }

    private async Task<ResolvedPackage?> ResolvePackageSpec(string packageName, string versionSpec)
    {
        // Try Cadenza registry first
        try
        {
            var metadata = await _registry.GetPackageMetadataAsync(packageName);
            if (metadata != null)
            {
                return new ResolvedPackage(
                    Version: metadata.Version,
                    Resolved: $"https://api.nuget.org/v3-flatcontainer/{packageName.ToLowerInvariant()}/{metadata.Version}/{packageName.ToLowerInvariant()}.{metadata.Version}.nupkg",
                    Integrity: "",
                    Dependencies: metadata.Dependencies,
                    Effects: metadata.Effects,
                    Type: PackageType.Cadenza
                );
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Cadenza registry lookup failed: {ex.Message}");
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
                    Effects: new List<string> { "IO" },
                    Type: PackageType.NuGet
                );
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: NuGet lookup failed: {ex.Message}");
        }

        return null;
    }

    private (string packageName, string versionSpec) ParsePackageSpec(string packageSpec)
    {
        var atIndex = packageSpec.LastIndexOf('@');
        if (atIndex > 0)
        {
            return (packageSpec[..atIndex], packageSpec[(atIndex + 1)..]);
        }
        return (packageSpec, "*");
    }

    private string? GetCurrentVersion(string packageName, EnhancedFlowcConfig config)
    {
        return config.Dependencies.GetValueOrDefault(packageName) ?? 
               config.DevDependencies.GetValueOrDefault(packageName);
    }

    private async Task<string?> GetLatestCompatibleVersion(string packageName, string currentVersionSpec)
    {
        try
        {
            var versions = await _registry.GetVersionsAsync(packageName);
            if (!versions.Any())
            {
                versions = await _nugetClient.GetVersionsAsync(packageName);
            }

            return versions.FirstOrDefault(v => _resolver.IsVersionCompatible(v, currentVersionSpec));
        }
        catch
        {
            return null;
        }
    }
}

// Result types for package operations
public record AddResult(bool Success, string Message);
public record RemoveResult(bool Success, string Message);
public record InstallResult(bool Success, string Message);
public record UpdateResult(bool Success, string Message);

public record PackageSearchResult(
    string Name, 
    string Version, 
    string Description, 
    PackageType Type, 
    long DownloadCount);

public record PackageInfo(
    string Name,
    string Version,
    string Description,
    PackageType Type,
    List<string> Keywords,
    List<string> Versions,
    string Homepage,
    string Repository,
    string License,
    string Author,
    Dictionary<string, string> Dependencies,
    List<string> Effects);

/// <summary>
/// Lock file manager for reproducible builds
/// </summary>
public class LockFileManager
{
    public async Task RemovePackageAsync(string packageName)
    {
        var lockFile = await ConfigurationManager.LoadLockFileAsync();
        lockFile.Resolved.Remove(packageName);
        await ConfigurationManager.SaveLockFileAsync(lockFile);
    }

    public async Task<bool> IsLockFileValidAsync()
    {
        try
        {
            var lockFile = await ConfigurationManager.LoadLockFileAsync();
            var config = await ConfigurationManager.LoadConfigAsync();

            // Check if all dependencies in config are in lock file
            foreach (var dep in config.Dependencies.Concat(config.DevDependencies))
            {
                if (!lockFile.Resolved.ContainsKey(dep.Key))
                    return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Workspace manager for multi-project scenarios
/// </summary>
public class WorkspaceManager
{
    public async Task<List<string>> GetWorkspaceProjectsAsync(string rootPath = ".")
    {
        return await ConfigurationManager.DiscoverWorkspaceProjectsAsync(rootPath);
    }

    public async Task<WorkspaceInstallResult> InstallWorkspaceAsync(string rootPath = ".")
    {
        var projects = await GetWorkspaceProjectsAsync(rootPath);
        var results = new List<InstallResult>();

        foreach (var project in projects)
        {
            var projectPath = Path.Combine(rootPath, project);
            var originalDir = Directory.GetCurrentDirectory();
            
            try
            {
                Directory.SetCurrentDirectory(projectPath);
                var packageManager = new PackageManager();
                var result = await packageManager.InstallPackagesAsync();
                results.Add(result);
                
                Console.WriteLine($"Project {project}: {(result.Success ? "✓" : "✗")}");
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
            }
        }

        var successCount = results.Count(r => r.Success);
        var message = $"Workspace install completed: {successCount}/{results.Count} projects successful";
        
        return new WorkspaceInstallResult(successCount == results.Count, message, results);
    }
}

public record WorkspaceInstallResult(bool Success, string Message, List<InstallResult> ProjectResults);