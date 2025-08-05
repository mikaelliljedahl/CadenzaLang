using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cadenza.Core;

namespace Cadenza.Analysis;

/// <summary>
/// Main static analyzer that coordinates all lint rules and analyzers
/// </summary>
public class StaticAnalyzer
{
    private readonly LintRuleEngine _ruleEngine;
    private readonly LintConfiguration _configuration;

    public StaticAnalyzer(LintConfiguration? configuration = null)
    {
        _configuration = configuration ?? LintConfiguration.CreateDefaultConfiguration();
        _ruleEngine = new LintRuleEngine(_configuration);

        // Register all analyzer rules
        RegisterAllRules();
    }

    private void RegisterAllRules()
    {
        // Effect System Rules
        _ruleEngine.RegisterRules(EffectAnalyzer.GetRules());

        // Result Type Rules
        _ruleEngine.RegisterRules(ResultTypeAnalyzer.GetRules());

        // Code Quality Rules
        _ruleEngine.RegisterRules(CodeQualityAnalyzer.GetRules());

        // Performance Rules
        _ruleEngine.RegisterRules(PerformanceAnalyzer.GetRules());

        // Security Rules
        _ruleEngine.RegisterRules(SecurityAnalyzer.GetRules());
    }

    /// <summary>
    /// Analyze a single Cadenza file
    /// </summary>
    public async Task<AnalysisReport> AnalyzeFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        var sourceText = await File.ReadAllTextAsync(filePath);
        return AnalyzeFile(filePath, sourceText);
    }

    /// <summary>
    /// Analyze Cadenza source code from string
    /// </summary>
    public AnalysisReport AnalyzeFile(string filePath, string sourceText)
    {
        try
        {
            // Parse the Cadenza source
            var lexer = new CadenzaLexer(sourceText);
            var tokens = lexer.ScanTokens();

            var parser = new CadenzaParser(tokens);
            var ast = parser.Parse();

            // Run static analysis
            var report = _ruleEngine.AnalyzeFile(filePath, sourceText, ast);

            // Collect metrics
            CollectMetrics(ast, report.Metrics);

            return report;
        }
        catch (Exception ex)
        {
            var report = new AnalysisReport { FilesAnalyzed = 1 };
            report.AddDiagnostic(new AnalysisDiagnostic(
                "parse-error",
                $"Failed to parse file: {ex.Message}",
                DiagnosticSeverity.Error,
                new SourceLocation(filePath, 1, 1, 0, ""),
                "parser",
                "Fix syntax errors in the source file"
            ));
            return report;
        }
    }

    /// <summary>
    /// Analyze multiple files or directories
    /// </summary>
    public async Task<AnalysisReport> AnalyzeAsync(IEnumerable<string> paths)
    {
        var files = new List<(string filePath, string sourceText, ProgramNode ast)>();

        foreach (var path in paths)
        {
            if (File.Exists(path))
            {
                await ProcessFile(path, files);
            }
            else if (Directory.Exists(path))
            {
                await ProcessDirectory(path, files);
            }
            else
            {
                Console.WriteLine($"Warning: Path not found: {path}");
            }
        }

        if (files.Count == 0)
        {
            return new AnalysisReport();
        }

        var report = _ruleEngine.AnalyzeFiles(files);

        // Collect combined metrics
        var combinedMetrics = new AnalysisMetrics();
        foreach (var (_, sourceText, ast) in files)
        {
            CollectMetrics(ast, combinedMetrics);
            combinedMetrics.LinesOfCode += sourceText.Split('\n').Length;
        }

        return report with { Metrics = combinedMetrics };
    }

    private async Task ProcessFile(string filePath, List<(string, string, ProgramNode)> files)
    {
        if (!filePath.EndsWith(".cdz") || _configuration.ShouldExcludeFile(filePath))
        {
            return;
        }

        try
        {
            var sourceText = await File.ReadAllTextAsync(filePath);
            var lexer = new CadenzaLexer(sourceText);
            var tokens = lexer.ScanTokens();
            var parser = new CadenzaParser(tokens);
            var ast = parser.Parse();

            files.Add((filePath, sourceText, ast));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to parse {filePath}: {ex.Message}");
        }
    }

    private async Task ProcessDirectory(string directoryPath, List<(string, string, ProgramNode)> files)
    {
        var flowFiles = Directory.GetFiles(directoryPath, "*.cdz", SearchOption.AllDirectories);
        
        foreach (var filePath in flowFiles)
        {
            await ProcessFile(filePath, files);
        }
    }

    private void CollectMetrics(ProgramNode ast, AnalysisMetrics metrics)
    {
        foreach (var stmt in ast.Statements)
        {
            CollectStatementMetrics(stmt, metrics);
        }
    }

    private void CollectStatementMetrics(ASTNode node, AnalysisMetrics metrics)
    {
        switch (node)
        {
            case FunctionDeclaration func:
                metrics.TotalFunctions++;
                if (func.IsPure)
                    metrics.PureFunctions++;
                if (func.Effects != null && func.Effects.Count > 0)
                {
                    metrics.FunctionsWithEffects++;
                    foreach (var effect in func.Effects)
                    {
                        metrics.EffectUsage[effect] = metrics.EffectUsage.GetValueOrDefault(effect, 0) + 1;
                    }
                }
                if (func.ReturnType.StartsWith("Result<"))
                    metrics.ResultTypeUsage++;

                // Count error propagation in function body
                CountErrorPropagation(func.Body, metrics);
                break;

            case ModuleDeclaration module:
                metrics.ModuleCount++;
                foreach (var moduleStmt in module.Body)
                {
                    CollectStatementMetrics(moduleStmt, metrics);
                }
                break;

            case StringInterpolation:
                metrics.StringInterpolationCount++;
                break;
                
            case ExportStatement:
                // Export statements don't contribute to metrics but are valid
                break;
        }
    }

    private void CountErrorPropagation(List<ASTNode> statements, AnalysisMetrics metrics)
    {
        foreach (var stmt in statements)
        {
            CountErrorPropagationInNode(stmt, metrics);
        }
    }

    private void CountErrorPropagationInNode(ASTNode node, AnalysisMetrics metrics)
    {
        switch (node)
        {
            case ErrorPropagation:
                metrics.ErrorPropagationCount++;
                break;

            case LetStatement let:
                CountErrorPropagationInNode(let.Expression, metrics);
                break;

            case IfStatement ifStmt:
                CountErrorPropagationInNode(ifStmt.Condition, metrics);
                CountErrorPropagation(ifStmt.ThenBody, metrics);
                if (ifStmt.ElseBody != null)
                    CountErrorPropagation(ifStmt.ElseBody, metrics);
                break;

            case ReturnStatement ret:
                CountErrorPropagationInNode(ret.Expression, metrics);
                break;

            case GuardStatement guard:
                CountErrorPropagationInNode(guard.Condition, metrics);
                CountErrorPropagation(guard.ElseBody, metrics);
                break;
        }
    }

    /// <summary>
    /// Get all available rules grouped by category
    /// </summary>
    public Dictionary<string, List<LintRule>> GetRulesByCategory()
    {
        var rulesByCategory = new Dictionary<string, List<LintRule>>();
        
        foreach (var rule in _ruleEngine.GetRules())
        {
            if (!rulesByCategory.ContainsKey(rule.Category))
            {
                rulesByCategory[rule.Category] = new List<LintRule>();
            }
            rulesByCategory[rule.Category].Add(rule);
        }

        return rulesByCategory;
    }

    /// <summary>
    /// Get enabled rules count by category
    /// </summary>
    public Dictionary<string, int> GetEnabledRulesCountByCategory()
    {
        var countsByCategory = new Dictionary<string, int>();
        
        foreach (var rule in _ruleEngine.GetEnabledRules())
        {
            countsByCategory[rule.Category] = countsByCategory.GetValueOrDefault(rule.Category, 0) + 1;
        }

        return countsByCategory;
    }

    /// <summary>
    /// Print analysis summary to console
    /// </summary>
    public void PrintSummary(AnalysisReport report)
    {
        Console.WriteLine();
        Console.WriteLine("=== Cadenza Static Analysis Report ===");
        Console.WriteLine();
        Console.WriteLine(report.GetSummary());
        
        if (report.Diagnostics.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Issues by Category:");
            
            var issuesByCategory = report.Diagnostics
                .GroupBy(d => d.Category)
                .OrderByDescending(g => g.Count())
                .ToList();

            foreach (var group in issuesByCategory)
            {
                var errorCount = group.Count(d => d.Severity == DiagnosticSeverity.Error);
                var warningCount = group.Count(d => d.Severity == DiagnosticSeverity.Warning);
                var infoCount = group.Count(d => d.Severity == DiagnosticSeverity.Info);
                
                Console.WriteLine($"  {group.Key}: {group.Count()} ({errorCount} errors, {warningCount} warnings, {infoCount} info)");
            }

            Console.WriteLine();
            Console.WriteLine("Top Issues:");
            
            var topIssues = report.RuleCounts
                .OrderByDescending(kvp => kvp.Value)
                .Take(5)
                .ToList();

            foreach (var issue in topIssues)
            {
                Console.WriteLine($"  {issue.Key}: {issue.Value} occurrences");
            }
        }

        // Print metrics if available
        if (report.Metrics.TotalFunctions > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Code Metrics:");
            Console.WriteLine($"  Functions: {report.Metrics.TotalFunctions} ({report.Metrics.PureFunctions} pure, {report.Metrics.FunctionsWithEffects} with effects)");
            Console.WriteLine($"  Modules: {report.Metrics.ModuleCount}");
            Console.WriteLine($"  Result Types: {report.Metrics.ResultTypeUsage}");
            Console.WriteLine($"  Error Propagations: {report.Metrics.ErrorPropagationCount}");
            Console.WriteLine($"  String Interpolations: {report.Metrics.StringInterpolationCount}");
            
            if (report.Metrics.EffectUsage.Count > 0)
            {
                Console.WriteLine("  Effect Usage:");
                foreach (var effect in report.Metrics.EffectUsage.OrderByDescending(kvp => kvp.Value))
                {
                    Console.WriteLine($"    {effect.Key}: {effect.Value}");
                }
            }
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Print detailed diagnostics to console
    /// </summary>
    public void PrintDiagnostics(AnalysisReport report, bool includeInfo = false)
    {
        var diagnosticsToShow = report.Diagnostics
            .Where(d => includeInfo || d.Severity != DiagnosticSeverity.Info)
            .OrderBy(d => d.Location.FilePath)
            .ThenBy(d => d.Location.Line)
            .ThenBy(d => d.Location.Column)
            .ToList();

        if (diagnosticsToShow.Count == 0)
        {
            Console.WriteLine("No issues found!");
            return;
        }

        string? currentFile = null;
        foreach (var diagnostic in diagnosticsToShow)
        {
            if (diagnostic.Location.FilePath != currentFile)
            {
                currentFile = diagnostic.Location.FilePath;
                Console.WriteLine();
                Console.WriteLine($"=== {currentFile} ===");
            }

            var severityIcon = diagnostic.Severity switch
            {
                DiagnosticSeverity.Error => "✗",
                DiagnosticSeverity.Warning => "⚠",
                DiagnosticSeverity.Info => "ℹ",
                _ => "?"
            };

            var severityColor = diagnostic.Severity switch
            {
                DiagnosticSeverity.Error => ConsoleColor.Red,
                DiagnosticSeverity.Warning => ConsoleColor.Yellow,
                DiagnosticSeverity.Info => ConsoleColor.Cyan,
                _ => ConsoleColor.White
            };

            Console.ForegroundColor = severityColor;
            Console.Write($"{severityIcon} ");
            Console.ResetColor();

            Console.WriteLine($"{diagnostic.Location.Line}:{diagnostic.Location.Column} [{diagnostic.RuleId}] {diagnostic.Message}");

            if (!string.IsNullOrEmpty(diagnostic.Location.SourceText))
            {
                Console.WriteLine($"    {diagnostic.Location.SourceText.Trim()}");
                if (diagnostic.Location.Column > 0 && diagnostic.Location.Length > 0)
                {
                    var pointer = new string(' ', diagnostic.Location.Column + 3) + new string('^', Math.Max(1, diagnostic.Location.Length));
                    Console.ForegroundColor = severityColor;
                    Console.WriteLine(pointer);
                    Console.ResetColor();
                }
            }

            if (!string.IsNullOrEmpty(diagnostic.FixSuggestion))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"    Fix: {diagnostic.FixSuggestion}");
                Console.ResetColor();
            }

            Console.WriteLine();
        }
    }
}