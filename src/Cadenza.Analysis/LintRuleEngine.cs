using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Cadenza.Core;

namespace Cadenza.Analysis;

/// <summary>
/// Rule configuration for individual lint rules
/// </summary>
public record LintRuleConfig(
    DiagnosticSeverity Level = DiagnosticSeverity.Warning,
    bool Enabled = true,
    Dictionary<string, object>? Parameters = null
);

/// <summary>
/// Configuration for the entire linting system
/// </summary>
public record LintConfiguration
{
    public Dictionary<string, LintRuleConfig> Rules { get; init; } = new();
    public List<string> Exclude { get; init; } = new();
    public DiagnosticSeverity SeverityThreshold { get; set; } = DiagnosticSeverity.Warning;
    public bool AutoFix { get; set; } = false;
    public string OutputFormat { get; set; } = "text";
    
    /// <summary>
    /// Load configuration from cadenzalint.json file
    /// </summary>
    public static LintConfiguration LoadFromFile(string configPath = "cadenzalint.json")
    {
        if (!File.Exists(configPath))
        {
            return CreateDefaultConfiguration();
        }
        
        try
        {
            var json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<LintConfiguration>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReadCommentHandling = JsonCommentHandling.Skip
            });
            
            return config ?? CreateDefaultConfiguration();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to load configuration from {configPath}: {ex.Message}");
            return CreateDefaultConfiguration();
        }
    }
    
    /// <summary>
    /// Create default configuration with all rules enabled
    /// </summary>
    public static LintConfiguration CreateDefaultConfiguration()
    {
        return new LintConfiguration
        {
            Rules = new Dictionary<string, LintRuleConfig>
            {
                // Effect System Rules
                ["effect-completeness"] = new(DiagnosticSeverity.Error),
                ["effect-minimality"] = new(DiagnosticSeverity.Warning),
                ["effect-propagation"] = new(DiagnosticSeverity.Error),
                ["pure-function-validation"] = new(DiagnosticSeverity.Error),
                ["effect-consistency"] = new(DiagnosticSeverity.Warning),
                
                // Result Type Rules
                ["unused-results"] = new(DiagnosticSeverity.Error),
                ["error-handling"] = new(DiagnosticSeverity.Error),
                ["error-propagation-validation"] = new(DiagnosticSeverity.Error),
                ["dead-error-paths"] = new(DiagnosticSeverity.Warning),
                ["result-type-consistency"] = new(DiagnosticSeverity.Warning),
                
                // Code Quality Rules
                ["dead-code"] = new(DiagnosticSeverity.Warning),
                ["unreachable-code"] = new(DiagnosticSeverity.Warning),
                ["unused-variables"] = new(DiagnosticSeverity.Warning),
                ["unused-imports"] = new(DiagnosticSeverity.Info),
                ["naming-convention"] = new(DiagnosticSeverity.Info),
                ["function-complexity"] = new(DiagnosticSeverity.Warning, true, new Dictionary<string, object> { ["maxLines"] = 50, ["maxParams"] = 5 }),
                ["duplicate-code"] = new(DiagnosticSeverity.Info),
                
                // Performance Rules
                ["string-concatenation"] = new(DiagnosticSeverity.Info),
                ["inefficient-effect-patterns"] = new(DiagnosticSeverity.Warning),
                ["module-import-optimization"] = new(DiagnosticSeverity.Info),
                ["unnecessary-error-propagation"] = new(DiagnosticSeverity.Info),
                
                // Security Rules
                ["input-validation"] = new(DiagnosticSeverity.Warning),
                ["effect-leakage"] = new(DiagnosticSeverity.Error),
                ["secret-detection"] = new(DiagnosticSeverity.Error),
                ["unsafe-string-interpolation"] = new(DiagnosticSeverity.Warning),
                
                // Style Rules
                ["consistent-spacing"] = new(DiagnosticSeverity.Info),
                ["line-length"] = new(DiagnosticSeverity.Info, true, new Dictionary<string, object> { ["maxLength"] = 120 }),
                ["prefer-expression-body"] = new(DiagnosticSeverity.Info),
                ["consistent-bracing"] = new(DiagnosticSeverity.Info)
            }
        };
    }
    
    /// <summary>
    /// Get rule configuration by ID
    /// </summary>
    public LintRuleConfig GetRuleConfig(string ruleId)
    {
        return Rules.TryGetValue(ruleId, out var config) ? config : new LintRuleConfig(Enabled: false);
    }
    
    /// <summary>
    /// Check if a rule is enabled and should run
    /// </summary>
    public bool IsRuleEnabled(string ruleId)
    {
        var config = GetRuleConfig(ruleId);
        return config.Enabled && config.Level >= SeverityThreshold;
    }
    
    /// <summary>
    /// Check if a file should be excluded from analysis
    /// </summary>
    public bool ShouldExcludeFile(string filePath)
    {
        return Exclude.Any(pattern => IsFileMatchingPattern(filePath, pattern));
    }
    
    private static bool IsFileMatchingPattern(string filePath, string pattern)
    {
        // Convert glob pattern to regex
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace(@"\*", ".*")
            .Replace(@"\?", ".") + "$";
        
        return Regex.IsMatch(filePath, regexPattern, RegexOptions.IgnoreCase);
    }
    
    /// <summary>
    /// Save configuration to file
    /// </summary>
    public void SaveToFile(string configPath = "cadenzalint.json")
    {
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        File.WriteAllText(configPath, json);
    }
}

/// <summary>
/// Base class for all lint rules
/// </summary>
public abstract class LintRule
{
    public abstract string RuleId { get; }
    public abstract string Description { get; }
    public abstract string Category { get; }
    
    protected LintConfiguration Configuration { get; private set; } = new();
    
    public void SetConfiguration(LintConfiguration configuration)
    {
        Configuration = configuration;
    }
    
    /// <summary>
    /// Check if this rule should run based on configuration
    /// </summary>
    public bool ShouldRun()
    {
        return Configuration.IsRuleEnabled(RuleId);
    }
    
    /// <summary>
    /// Get the configured severity for this rule
    /// </summary>
    public DiagnosticSeverity GetSeverity()
    {
        return Configuration.GetRuleConfig(RuleId).Level;
    }
    
    /// <summary>
    /// Get rule parameter value
    /// </summary>
    protected T? GetParameter<T>(string paramName, T? defaultValue = default)
    {
        var config = Configuration.GetRuleConfig(RuleId);
        if (config.Parameters?.TryGetValue(paramName, out var value) == true)
        {
            try
            {
                if (value is JsonElement element)
                {
                    return element.Deserialize<T>();
                }
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }
    
    /// <summary>
    /// Analyze a single file and return diagnostics
    /// </summary>
    public abstract IEnumerable<AnalysisDiagnostic> Analyze(ProgramNode ast, string filePath, string sourceText);
    
    /// <summary>
    /// Create a diagnostic for this rule
    /// </summary>
    public AnalysisDiagnostic CreateDiagnostic(
        string message,
        SourceLocation location,
        string? fixSuggestion = null,
        Dictionary<string, object>? properties = null)
    {
        return new AnalysisDiagnostic(
            RuleId,
            message,
            GetSeverity(),
            location,
            Category,
            fixSuggestion,
            properties
        );
    }
}

/// <summary>
/// Engine for running lint rules and collecting results
/// </summary>
public class LintRuleEngine
{
    private readonly List<LintRule> _rules = new();
    private readonly LintConfiguration _configuration;
    
    public LintRuleEngine(LintConfiguration? configuration = null)
    {
        _configuration = configuration ?? LintConfiguration.CreateDefaultConfiguration();
        
        // Configure all rules
        foreach (var rule in _rules)
        {
            rule.SetConfiguration(_configuration);
        }
    }
    
    /// <summary>
    /// Register a lint rule
    /// </summary>
    public void RegisterRule(LintRule rule)
    {
        rule.SetConfiguration(_configuration);
        _rules.Add(rule);
    }
    
    /// <summary>
    /// Register multiple lint rules
    /// </summary>
    public void RegisterRules(IEnumerable<LintRule> rules)
    {
        foreach (var rule in rules)
        {
            RegisterRule(rule);
        }
    }
    
    /// <summary>
    /// Analyze a single file
    /// </summary>
    public AnalysisReport AnalyzeFile(string filePath, string sourceText, ProgramNode ast)
    {
        var report = new AnalysisReport { FilesAnalyzed = 1 };
        
        if (_configuration.ShouldExcludeFile(filePath))
        {
            return report;
        }
        
        var startTime = DateTime.UtcNow;
        
        // Run all enabled rules
        foreach (var rule in _rules.Where(r => r.ShouldRun()))
        {
            try
            {
                var diagnostics = rule.Analyze(ast, filePath, sourceText);
                foreach (var diagnostic in diagnostics)
                {
                    report.AddDiagnostic(diagnostic);
                }
            }
            catch (Exception ex)
            {
                // Log rule execution error but continue with other rules
                Console.Error.WriteLine($"Warning: Rule {rule.RuleId} failed: {ex.Message}");
            }
        }
        
        report.AnalysisTime = DateTime.UtcNow - startTime;
        return report;
    }
    
    /// <summary>
    /// Analyze multiple files
    /// </summary>
    public AnalysisReport AnalyzeFiles(IEnumerable<(string filePath, string sourceText, ProgramNode ast)> files)
    {
        var combinedReport = new AnalysisReport();
        var startTime = DateTime.UtcNow;
        
        foreach (var (filePath, sourceText, ast) in files)
        {
            var fileReport = AnalyzeFile(filePath, sourceText, ast);
            
            // Merge diagnostics
            foreach (var diagnostic in fileReport.Diagnostics)
            {
                combinedReport.AddDiagnostic(diagnostic);
            }
            
            combinedReport.FilesAnalyzed++;
        }
        
        combinedReport.AnalysisTime = DateTime.UtcNow - startTime;
        return combinedReport;
    }
    
    /// <summary>
    /// Get all registered rules
    /// </summary>
    public IReadOnlyList<LintRule> GetRules() => _rules.AsReadOnly();
    
    /// <summary>
    /// Get enabled rules only
    /// </summary>
    public IEnumerable<LintRule> GetEnabledRules() => _rules.Where(r => r.ShouldRun());
}