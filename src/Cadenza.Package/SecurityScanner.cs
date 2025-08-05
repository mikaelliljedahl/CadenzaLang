using System.Net.Http;
using System.Text.Json;

namespace Cadenza.Package;

/// <summary>
/// Security scanner for package vulnerabilities and compliance
/// </summary>
public class SecurityScanner
{
    private readonly HttpClient _httpClient;
    private readonly List<IVulnerabilityDatabase> _databases;

    public SecurityScanner()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Cadenza Package Manager Security Scanner 1.0.0");
        
        _databases = new List<IVulnerabilityDatabase>
        {
            new NuGetVulnerabilityDatabase(_httpClient),
            new OSVDatabase(_httpClient),
            new CadenzaSecurityDatabase(_httpClient)
        };
    }

    /// <summary>
    /// Audit all packages in the current project for security vulnerabilities
    /// </summary>
    public async Task<SecurityAuditReport> AuditAsync(bool includeDev = true, bool verbose = false)
    {
        try
        {
            var lockFile = await ConfigurationManager.LoadLockFileAsync();
            var report = new SecurityAuditReport();

            Console.WriteLine("Scanning packages for security vulnerabilities...");

            foreach (var (packageName, resolvedPackage) in lockFile.Resolved)
            {
                if (verbose)
                    Console.WriteLine($"Scanning {packageName}@{resolvedPackage.Version}...");

                var vulnerabilities = await ScanPackageAsync(packageName, resolvedPackage.Version, resolvedPackage.Type);
                
                if (vulnerabilities.Any())
                {
                    report.VulnerablePackages.Add(new VulnerablePackage(
                        packageName, resolvedPackage.Version, vulnerabilities));
                    
                    foreach (var vuln in vulnerabilities)
                    {
                        report.Vulnerabilities.Add(vuln);
                        
                        if (vuln.Severity == VulnerabilitySeverity.Critical)
                            report.CriticalCount++;
                        else if (vuln.Severity == VulnerabilitySeverity.High)
                            report.HighCount++;
                        else if (vuln.Severity == VulnerabilitySeverity.Medium)
                            report.MediumCount++;
                        else
                            report.LowCount++;
                    }
                }
            }

            // Check for license compliance
            await CheckLicenseComplianceAsync(report, lockFile);

            // Check for deprecated packages
            await CheckDeprecatedPackagesAsync(report, lockFile);

            report.TotalPackagesScanned = lockFile.Resolved.Count;
            report.ScanCompletedAt = DateTime.UtcNow;

            return report;
        }
        catch (Exception ex)
        {
            throw new SecurityScanException($"Security audit failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Scan a specific package for vulnerabilities
    /// </summary>
    public async Task<List<Vulnerability>> ScanPackageAsync(string packageName, string version, PackageType type)
    {
        var vulnerabilities = new List<Vulnerability>();

        foreach (var database in _databases)
        {
            try
            {
                if (database.SupportsPackageType(type))
                {
                    var vulns = await database.GetVulnerabilitiesAsync(packageName, version);
                    vulnerabilities.AddRange(vulns);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to scan {packageName} with {database.GetType().Name}: {ex.Message}");
            }
        }

        return vulnerabilities.DistinctBy(v => v.Id).ToList();
    }

    /// <summary>
    /// Attempt to fix security issues automatically
    /// </summary>
    public async Task<SecurityFixReport> FixSecurityIssuesAsync(bool dryRun = false)
    {
        var auditReport = await AuditAsync();
        var fixReport = new SecurityFixReport();

        if (!auditReport.HasVulnerabilities)
        {
            fixReport.Message = "No security vulnerabilities found to fix";
            return fixReport;
        }

        var config = await ConfigurationManager.LoadConfigAsync();
        var resolver = new DependencyResolver(null!, null!);

        foreach (var vulnerablePackage in auditReport.VulnerablePackages)
        {
            var fixes = await FindFixesAsync(vulnerablePackage);
            
            foreach (var fix in fixes)
            {
                if (fix.FixType == SecurityFixType.UpdateVersion)
                {
                    // Check if update is compatible
                    var currentVersionSpec = GetVersionSpec(vulnerablePackage.Name, config);
                    if (currentVersionSpec != null && resolver.IsVersionCompatible(fix.FixedVersion, currentVersionSpec))
                    {
                        fixReport.AutomaticallyFixable.Add(new SecurityFix(
                            vulnerablePackage.Name, vulnerablePackage.Version, fix.FixedVersion, 
                            SecurityFixType.UpdateVersion, $"Update to {fix.FixedVersion}"));
                        
                        if (!dryRun)
                        {
                            // Apply the fix
                            UpdatePackageVersion(config, vulnerablePackage.Name, fix.FixedVersion);
                        }
                    }
                    else
                    {
                        fixReport.ManuallyFixable.Add(new SecurityFix(
                            vulnerablePackage.Name, vulnerablePackage.Version, fix.FixedVersion,
                            SecurityFixType.ManualUpdate, $"Requires manual update to {fix.FixedVersion} (breaking change)"));
                    }
                }
                else if (fix.FixType == SecurityFixType.RemovePackage)
                {
                    fixReport.ManuallyFixable.Add(new SecurityFix(
                        vulnerablePackage.Name, vulnerablePackage.Version, "",
                        SecurityFixType.RemovePackage, "Package should be removed (no secure version available)"));
                }
            }
        }

        if (!dryRun && fixReport.AutomaticallyFixable.Any())
        {
            await ConfigurationManager.SaveConfigAsync(config);
            Console.WriteLine($"Applied {fixReport.AutomaticallyFixable.Count} automatic security fixes");
        }

        fixReport.Success = true;
        fixReport.Message = $"Security scan completed: {fixReport.AutomaticallyFixable.Count} auto-fixable, {fixReport.ManuallyFixable.Count} manual fixes required";
        
        return fixReport;
    }

    private async Task<List<PotentialFix>> FindFixesAsync(VulnerablePackage package)
    {
        var fixes = new List<PotentialFix>();

        // Look for patched versions
        foreach (var vuln in package.Vulnerabilities)
        {
            if (vuln.FixedIn != null)
            {
                fixes.Add(new PotentialFix(SecurityFixType.UpdateVersion, vuln.FixedIn));
            }
        }

        return fixes.DistinctBy(f => f.FixedVersion).ToList();
    }

    private string? GetVersionSpec(string packageName, EnhancedFlowcConfig config)
    {
        return config.Dependencies.GetValueOrDefault(packageName) ?? 
               config.DevDependencies.GetValueOrDefault(packageName);
    }

    private void UpdatePackageVersion(EnhancedFlowcConfig config, string packageName, string newVersion)
    {
        if (config.Dependencies.ContainsKey(packageName))
        {
            config.Dependencies[packageName] = $"^{newVersion}";
        }
        else if (config.DevDependencies.ContainsKey(packageName))
        {
            config.DevDependencies[packageName] = $"^{newVersion}";
        }
    }

    private async Task CheckLicenseComplianceAsync(SecurityAuditReport report, LockFile lockFile)
    {
        var incompatibleLicenses = new[]
        {
            "GPL-3.0", "AGPL-3.0", "GPL-2.0"  // Example restrictive licenses
        };

        foreach (var (packageName, package) in lockFile.Resolved)
        {
            try
            {
                // For NuGet packages, check license
                if (package.Type == PackageType.NuGet)
                {
                    var nugetClient = new NuGetClient(new List<string> { "https://api.nuget.org/v3/index.json" });
                    var nugetPackage = await nugetClient.GetPackageAsync(packageName, package.Version);
                    
                    if (nugetPackage?.LicenseUrl != null)
                    {
                        foreach (var restrictive in incompatibleLicenses)
                        {
                            if (nugetPackage.LicenseUrl.Contains(restrictive, StringComparison.OrdinalIgnoreCase))
                            {
                                report.LicenseIssues.Add(new LicenseIssue(
                                    packageName, package.Version, restrictive,
                                    $"Package uses restrictive license: {restrictive}"));
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignore license check failures
            }
        }
    }

    private async Task CheckDeprecatedPackagesAsync(SecurityAuditReport report, LockFile lockFile)
    {
        // Check for packages with known deprecation warnings
        var deprecatedPatterns = new[]
        {
            "Microsoft.AspNetCore.Http.Abstractions", // Example deprecated package
            "System.Web" // Legacy package
        };

        foreach (var (packageName, package) in lockFile.Resolved)
        {
            if (deprecatedPatterns.Any(pattern => packageName.StartsWith(pattern)))
            {
                report.DeprecatedPackages.Add(new DeprecatedPackage(
                    packageName, package.Version, 
                    $"Package {packageName} is deprecated and should be replaced"));
            }
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

/// <summary>
/// Interface for vulnerability databases
/// </summary>
public interface IVulnerabilityDatabase
{
    Task<List<Vulnerability>> GetVulnerabilitiesAsync(string packageName, string version);
    bool SupportsPackageType(PackageType type);
}

/// <summary>
/// NuGet vulnerability database implementation
/// </summary>
public class NuGetVulnerabilityDatabase : IVulnerabilityDatabase
{
    private readonly HttpClient _httpClient;

    public NuGetVulnerabilityDatabase(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Vulnerability>> GetVulnerabilitiesAsync(string packageName, string version)
    {
        try
        {
            // Use GitHub Advisory Database for NuGet vulnerabilities
            var url = $"https://api.github.com/advisories?ecosystem=nuget&package={packageName}";
            var response = await _httpClient.GetStringAsync(url);
            var advisories = JsonSerializer.Deserialize<GitHubAdvisory[]>(response) ?? Array.Empty<GitHubAdvisory>();

            var vulnerabilities = new List<Vulnerability>();
            
            foreach (var advisory in advisories)
            {
                if (IsVersionAffected(version, advisory.VulnerableVersions))
                {
                    vulnerabilities.Add(new Vulnerability(
                        Id: advisory.Id,
                        Title: advisory.Summary,
                        Description: advisory.Description,
                        Severity: MapSeverity(advisory.Severity),
                        CvssScore: advisory.CvssScore,
                        References: advisory.References?.ToList() ?? new(),
                        FixedIn: advisory.FirstPatchedVersion,
                        PublishedAt: advisory.PublishedAt
                    ));
                }
            }

            return vulnerabilities;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to check NuGet vulnerabilities for {packageName}: {ex.Message}");
            return new List<Vulnerability>();
        }
    }

    public bool SupportsPackageType(PackageType type) => type == PackageType.NuGet;

    private bool IsVersionAffected(string version, string[] vulnerableVersions)
    {
        // Simplified version checking - would need more sophisticated implementation
        return vulnerableVersions?.Any(v => v.Contains(version)) ?? false;
    }

    private VulnerabilitySeverity MapSeverity(string severity)
    {
        return severity?.ToLowerInvariant() switch
        {
            "critical" => VulnerabilitySeverity.Critical,
            "high" => VulnerabilitySeverity.High,
            "medium" or "moderate" => VulnerabilitySeverity.Medium,
            "low" => VulnerabilitySeverity.Low,
            _ => VulnerabilitySeverity.Medium
        };
    }
}

/// <summary>
/// OSV (Open Source Vulnerabilities) database implementation
/// </summary>
public class OSVDatabase : IVulnerabilityDatabase
{
    private readonly HttpClient _httpClient;

    public OSVDatabase(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Vulnerability>> GetVulnerabilitiesAsync(string packageName, string version)
    {
        try
        {
            var request = new
            {
                package = new { name = packageName, ecosystem = "NuGet" },
                version = version
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("https://api.osv.dev/v1/query", content);
            var responseText = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
                return new List<Vulnerability>();

            var osvResponse = JsonSerializer.Deserialize<OSVResponse>(responseText);
            
            return osvResponse?.Vulns?.Select(v => new Vulnerability(
                Id: v.Id,
                Title: v.Summary,
                Description: v.Details,
                Severity: VulnerabilitySeverity.Medium, // OSV doesn't always provide severity
                CvssScore: null,
                References: v.References?.Select(r => r.Url).ToList() ?? new(),
                FixedIn: null, // Would need to parse from affected ranges
                PublishedAt: v.Published
            )).ToList() ?? new List<Vulnerability>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to check OSV for {packageName}: {ex.Message}");
            return new List<Vulnerability>();
        }
    }

    public bool SupportsPackageType(PackageType type) => type == PackageType.NuGet;
}

/// <summary>
/// Cadenza-specific security database
/// </summary>
public class CadenzaSecurityDatabase : IVulnerabilityDatabase
{
    private readonly HttpClient _httpClient;

    public CadenzaSecurityDatabase(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Vulnerability>> GetVulnerabilitiesAsync(string packageName, string version)
    {
        try
        {
            var url = $"https://security.cadenza.org/api/vulnerabilities/{packageName}/{version}";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
                return new List<Vulnerability>();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Vulnerability>>(json) ?? new List<Vulnerability>();
        }
        catch
        {
            // Cadenza security database might not exist yet
            return new List<Vulnerability>();
        }
    }

    public bool SupportsPackageType(PackageType type) => type == PackageType.Cadenza;
}

// Data models for security scanning
public class SecurityAuditReport
{
    public List<VulnerablePackage> VulnerablePackages { get; } = new();
    public List<Vulnerability> Vulnerabilities { get; } = new();
    public List<LicenseIssue> LicenseIssues { get; } = new();
    public List<DeprecatedPackage> DeprecatedPackages { get; } = new();
    
    public int CriticalCount { get; set; }
    public int HighCount { get; set; }
    public int MediumCount { get; set; }
    public int LowCount { get; set; }
    public int TotalPackagesScanned { get; set; }
    public DateTime ScanCompletedAt { get; set; }

    public bool HasVulnerabilities => Vulnerabilities.Any();
    public bool HasCriticalVulnerabilities => CriticalCount > 0;
    public int TotalVulnerabilities => CriticalCount + HighCount + MediumCount + LowCount;
}

public record VulnerablePackage(string Name, string Version, List<Vulnerability> Vulnerabilities);

public record Vulnerability(
    string Id,
    string Title,
    string Description,
    VulnerabilitySeverity Severity,
    double? CvssScore,
    List<string> References,
    string? FixedIn,
    DateTime PublishedAt
);

public record LicenseIssue(string PackageName, string Version, string License, string Issue);

public record DeprecatedPackage(string Name, string Version, string Reason);

public enum VulnerabilitySeverity
{
    Low,
    Medium, 
    High,
    Critical
}

public class SecurityFixReport
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public List<SecurityFix> AutomaticallyFixable { get; } = new();
    public List<SecurityFix> ManuallyFixable { get; } = new();
}

public record SecurityFix(
    string PackageName,
    string CurrentVersion,
    string FixedVersion,
    SecurityFixType FixType,
    string Description
);

public enum SecurityFixType
{
    UpdateVersion,
    ManualUpdate,
    RemovePackage
}

public record PotentialFix(SecurityFixType FixType, string FixedVersion);

public class SecurityScanException : Exception
{
    public SecurityScanException(string message) : base(message) { }
    public SecurityScanException(string message, Exception innerException) : base(message, innerException) { }
}

// External API models
public class GitHubAdvisory
{
    public string Id { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Description { get; set; } = "";
    public string Severity { get; set; } = "";
    public double CvssScore { get; set; }
    public string[] References { get; set; } = Array.Empty<string>();
    public string[] VulnerableVersions { get; set; } = Array.Empty<string>();
    public string FirstPatchedVersion { get; set; } = "";
    public DateTime PublishedAt { get; set; }
}

public class OSVResponse
{
    public OSVVulnerability[] Vulns { get; set; } = Array.Empty<OSVVulnerability>();
}

public class OSVVulnerability
{
    public string Id { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Details { get; set; } = "";
    public OSVReference[] References { get; set; } = Array.Empty<OSVReference>();
    public DateTime Published { get; set; }
}

public class OSVReference
{
    public string Url { get; set; } = "";
}