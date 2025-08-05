using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cadenza.Analysis;

/// <summary>
/// Represents a diagnostic issue found during static analysis
/// </summary>
public record AnalysisDiagnostic(
    string RuleId,
    string Message,
    DiagnosticSeverity Severity,
    SourceLocation Location,
    string Category,
    string? FixSuggestion = null,
    Dictionary<string, object>? Properties = null
);

/// <summary>
/// Severity levels for diagnostics
/// </summary>
public enum DiagnosticSeverity
{
    Info,
    Warning,
    Error
}

/// <summary>
/// Represents a location in source code
/// </summary>
public record SourceLocation(
    string FilePath,
    int Line,
    int Column,
    int Length = 0,
    string? SourceText = null
);

/// <summary>
/// Analysis categories for organizing rules
/// </summary>
public static class AnalysisCategories
{
    public const string EffectSystem = "effect-system";
    public const string ResultTypes = "result-types";
    public const string CodeQuality = "code-quality";
    public const string Performance = "performance";
    public const string Security = "security";
    public const string Style = "style";
}

/// <summary>
/// Comprehensive analysis report containing all diagnostics and metrics
/// </summary>
public record AnalysisReport
{
    public List<AnalysisDiagnostic> Diagnostics { get; init; } = new();
    public AnalysisMetrics Metrics { get; init; } = new();
    public Dictionary<string, int> RuleCounts { get; init; } = new();
    public TimeSpan AnalysisTime { get; set; }
    public int FilesAnalyzed { get; set; }
    
    /// <summary>
    /// Add a diagnostic to the report
    /// </summary>
    public void AddDiagnostic(AnalysisDiagnostic diagnostic)
    {
        Diagnostics.Add(diagnostic);
        
        // Update rule counts
        if (RuleCounts.ContainsKey(diagnostic.RuleId))
            RuleCounts[diagnostic.RuleId]++;
        else
            RuleCounts[diagnostic.RuleId] = 1;
            
        // Update metrics
        Metrics.TotalIssues++;
        switch (diagnostic.Severity)
        {
            case DiagnosticSeverity.Error:
                Metrics.Errors++;
                break;
            case DiagnosticSeverity.Warning:
                Metrics.Warnings++;
                break;
            case DiagnosticSeverity.Info:
                Metrics.Infos++;
                break;
        }
    }
    
    /// <summary>
    /// Get diagnostics by severity level
    /// </summary>
    public IEnumerable<AnalysisDiagnostic> GetDiagnosticsBySeverity(DiagnosticSeverity severity)
    {
        return Diagnostics.Where(d => d.Severity == severity);
    }
    
    /// <summary>
    /// Get diagnostics by category
    /// </summary>
    public IEnumerable<AnalysisDiagnostic> GetDiagnosticsByCategory(string category)
    {
        return Diagnostics.Where(d => d.Category == category);
    }
    
    /// <summary>
    /// Check if analysis passed based on severity threshold
    /// </summary>
    public bool HasPassingResult(DiagnosticSeverity threshold = DiagnosticSeverity.Error)
    {
        return threshold switch
        {
            DiagnosticSeverity.Error => Metrics.Errors == 0,
            DiagnosticSeverity.Warning => Metrics.Errors == 0 && Metrics.Warnings == 0,
            DiagnosticSeverity.Info => Metrics.TotalIssues == 0,
            _ => true
        };
    }
    
    /// <summary>
    /// Generate a summary string of the analysis results
    /// </summary>
    public string GetSummary()
    {
        var summary = $"Analysis completed in {AnalysisTime.TotalMilliseconds:F0}ms\n";
        summary += $"Files analyzed: {FilesAnalyzed}\n";
        summary += $"Total issues: {Metrics.TotalIssues}";
        
        if (Metrics.TotalIssues > 0)
        {
            summary += $" ({Metrics.Errors} errors, {Metrics.Warnings} warnings, {Metrics.Infos} info)";
        }
        
        return summary;
    }
    
    /// <summary>
    /// Export report as JSON
    /// </summary>
    public string ToJson(bool indented = true)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = indented,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };
        
        return JsonSerializer.Serialize(this, options);
    }
    
    /// <summary>
    /// Export report in SARIF format for CI/CD integration
    /// </summary>
    public string ToSarif()
    {
        var sarif = new
        {
            version = "2.1.0",
            schema = "https://raw.githubusercontent.com/oasis-tcs/sarif-spec/master/Schemata/sarif-schema-2.1.0.json",
            runs = new[]
            {
                new
                {
                    tool = new
                    {
                        driver = new
                        {
                            name = "Cadenza Static Analyzer",
                            version = "1.0.0",
                            informationUri = "https://github.com/cadenza/cadenza",
                            rules = RuleCounts.Keys.Select(ruleId => new
                            {
                                id = ruleId,
                                shortDescription = new { text = $"Cadenza rule {ruleId}" }
                            }).ToArray()
                        }
                    },
                    results = Diagnostics.Select(d => new
                    {
                        ruleId = d.RuleId,
                        level = d.Severity.ToString().ToLower(),
                        message = new { text = d.Message },
                        locations = new[]
                        {
                            new
                            {
                                physicalLocation = new
                                {
                                    artifactLocation = new { uri = d.Location.FilePath },
                                    region = new
                                    {
                                        startLine = d.Location.Line,
                                        startColumn = d.Location.Column,
                                        charLength = d.Location.Length
                                    }
                                }
                            }
                        },
                        fixes = string.IsNullOrEmpty(d.FixSuggestion) ? null : new[]
                        {
                            new
                            {
                                description = new { text = d.FixSuggestion }
                            }
                        }
                    }).ToArray()
                }
            }
        };
        
        return JsonSerializer.Serialize(sarif, new JsonSerializerOptions { WriteIndented = true });
    }
}

/// <summary>
/// Analysis metrics and statistics
/// </summary>
public class AnalysisMetrics
{
    public int TotalIssues { get; set; }
    public int Errors { get; set; }
    public int Warnings { get; set; }
    public int Infos { get; set; }
    
    // Code metrics
    public int TotalFunctions { get; set; }
    public int PureFunctions { get; set; }
    public int FunctionsWithEffects { get; set; }
    public int ModuleCount { get; set; }
    public int LinesOfCode { get; set; }
    
    // Cadenza-specific metrics
    public Dictionary<string, int> EffectUsage { get; init; } = new();
    public int ResultTypeUsage { get; set; }
    public int ErrorPropagationCount { get; set; }
    public int StringInterpolationCount { get; set; }
}