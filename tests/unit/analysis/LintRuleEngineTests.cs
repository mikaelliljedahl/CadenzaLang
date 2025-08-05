using Cadenza.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Cadenza.Analysis;
using NUnit.Framework;

namespace Cadenza.Tests.Unit.Analysis;

[TestFixture]
public class LintRuleEngineTests
{
    [Test]
    public void Analysis_CreateDefaultConfiguration_ShouldHaveAllStandardRules()
    {
        // Arrange & Act
        var config = LintConfiguration.CreateDefaultConfiguration();

        // Assert
        Assert.That(config.Rules, Is.Not.Null);
        
        // Check for key rule categories
        Assert.That(config.Rules.ContainsKey("effect-completeness"), Is.True);
        Assert.That(config.Rules.ContainsKey("unused-results"), Is.True);
        Assert.That(config.Rules.ContainsKey("dead-code"), Is.True);
        Assert.That(config.Rules.ContainsKey("string-concatenation"), Is.True);
        Assert.That(config.Rules.ContainsKey("input-validation"), Is.True);
    }

    [Test]
    public void Analysis_LintConfiguration_ShouldRespectSeverityThreshold()
    {
        // Arrange
        var config = new LintConfiguration
        {
            SeverityThreshold = DiagnosticSeverity.Warning,
            Rules = new Dictionary<string, LintRuleConfig>
            {
                ["test-error"] = new(DiagnosticSeverity.Error),
                ["test-warning"] = new(DiagnosticSeverity.Warning),
                ["test-info"] = new(DiagnosticSeverity.Info)
            }
        };

        // Act & Assert
        Assert.That(config.IsRuleEnabled("test-error"), Is.True);
        Assert.That(config.IsRuleEnabled("test-warning"), Is.True);
        Assert.That(config.IsRuleEnabled("test-info"), Is.False);
    }

    [Test]
    public void Analysis_LintConfiguration_ShouldExcludeFilesByPattern()
    {
        // Arrange
        var config = new LintConfiguration
        {
            Exclude = new List<string> { "generated/*", "*.test.cdz", "temp_*" }
        };

        // Act & Assert
        Assert.That(config.ShouldExcludeFile("generated/auto.cdz"), Is.True);
        Assert.That(config.ShouldExcludeFile("example.test.cdz"), Is.True);
        Assert.That(config.ShouldExcludeFile("temp_output.cdz"), Is.True);
        Assert.That(config.ShouldExcludeFile("src/main.cdz"), Is.False);
    }

    [Test]
    public void Analysis_LintRule_ShouldGetParameterValues()
    {
        // Arrange
        var config = new LintConfiguration
        {
            Rules = new Dictionary<string, LintRuleConfig>
            {
                ["test-rule"] = new(DiagnosticSeverity.Warning, true, new Dictionary<string, object>
                {
                    ["maxLength"] = 100,
                    ["enabled"] = true,
                    ["pattern"] = "test-*"
                })
            }
        };

        var rule = new TestRule();
        rule.SetConfiguration(config);

        // Act & Assert
        Assert.That(rule.GetTestParameter("maxLength", 50), Is.EqualTo(100));
        Assert.That(rule.GetTestParameter("enabled", false), Is.True);
        Assert.That(rule.GetTestParameter("pattern", "default"), Is.EqualTo("test-*"));
        Assert.That(rule.GetTestParameter("missing", 42), Is.EqualTo(42));
    }

    [Test]
    public void Analysis_AnalysisReport_ShouldAggregateMetricsCorrectly()
    {
        // Arrange
        var report = new AnalysisReport();

        // Act
        report.AddDiagnostic(new AnalysisDiagnostic(
            "test-rule-1", "Test error", DiagnosticSeverity.Error,
            new SourceLocation("test.cdz", 1, 1), "test"));

        report.AddDiagnostic(new AnalysisDiagnostic(
            "test-rule-2", "Test warning", DiagnosticSeverity.Warning,
            new SourceLocation("test.cdz", 2, 1), "test"));

        report.AddDiagnostic(new AnalysisDiagnostic(
            "test-rule-1", "Another error", DiagnosticSeverity.Error,
            new SourceLocation("test.cdz", 3, 1), "test"));

        // Assert
        Assert.That(report.Metrics.TotalIssues, Is.EqualTo(3));
        Assert.That(report.Metrics.Errors, Is.EqualTo(2));
        Assert.That(report.Metrics.Warnings, Is.EqualTo(1));
        Assert.That(report.Metrics.Infos, Is.EqualTo(0));
        
        Assert.That(report.RuleCounts["test-rule-1"], Is.EqualTo(2));
        Assert.That(report.RuleCounts["test-rule-2"], Is.EqualTo(1));
    }

    [Test]
    public void Analysis_AnalysisReport_ShouldFilterDiagnosticsByCategory()
    {
        // Arrange
        var report = new AnalysisReport();
        report.AddDiagnostic(new AnalysisDiagnostic(
            "rule1", "Message", DiagnosticSeverity.Error,
            new SourceLocation("test.cdz", 1, 1), "category1"));
        report.AddDiagnostic(new AnalysisDiagnostic(
            "rule2", "Message", DiagnosticSeverity.Warning,
            new SourceLocation("test.cdz", 2, 1), "category2"));
        report.AddDiagnostic(new AnalysisDiagnostic(
            "rule3", "Message", DiagnosticSeverity.Error,
            new SourceLocation("test.cdz", 3, 1), "category1"));

        // Act
        var category1Diagnostics = report.GetDiagnosticsByCategory("category1").ToList();
        var category2Diagnostics = report.GetDiagnosticsByCategory("category2").ToList();

        // Assert
        Assert.That(category1Diagnostics.Count, Is.EqualTo(2));
        Assert.That(category2Diagnostics.Count, Is.EqualTo(1));
        foreach (var d in category1Diagnostics)
            Assert.That(d.Category, Is.EqualTo("category1"));
        foreach (var d in category2Diagnostics)
            Assert.That(d.Category, Is.EqualTo("category2"));
    }

    [Test]
    public void Analysis_AnalysisReport_ShouldDeterminePassingResult()
    {
        // Arrange
        var report = new AnalysisReport();

        // Act & Assert - Empty report should pass
        Assert.That(report.HasPassingResult(DiagnosticSeverity.Error), Is.True);
        Assert.That(report.HasPassingResult(DiagnosticSeverity.Warning), Is.True);

        // Add warning
        report.AddDiagnostic(new AnalysisDiagnostic(
            "rule", "Warning", DiagnosticSeverity.Warning,
            new SourceLocation("test.cdz", 1, 1), "test"));

        Assert.That(report.HasPassingResult(DiagnosticSeverity.Error), Is.True);
        Assert.That(report.HasPassingResult(DiagnosticSeverity.Warning), Is.False);

        // Add error
        report.AddDiagnostic(new AnalysisDiagnostic(
            "rule", "Error", DiagnosticSeverity.Error,
            new SourceLocation("test.cdz", 2, 1), "test"));

        Assert.That(report.HasPassingResult(DiagnosticSeverity.Error), Is.False);
        Assert.That(report.HasPassingResult(DiagnosticSeverity.Warning), Is.False);
    }

    [Test]
    public void Analysis_LintRuleEngine_ShouldRegisterAndRunRules()
    {
        // Arrange
        var config = new LintConfiguration
        {
            Rules = new Dictionary<string, LintRuleConfig>
            {
                ["test-rule"] = new(DiagnosticSeverity.Error, true)
            }
        };
        var engine = new LintRuleEngine(config);
        var rule = new TestRule();
        engine.RegisterRule(rule);

        var ast = CreateTestProgram();

        // Act
        var report = engine.AnalyzeFile("test.cdz", "function test() -> int { return 42 }", ast);

        // Assert
        Assert.That(report.FilesAnalyzed, Is.EqualTo(1));
        Assert.That(report.Diagnostics.Count, Is.EqualTo(1));
        Assert.That(report.Diagnostics[0].RuleId, Is.EqualTo("test-rule"));
    }

    private ProgramNode CreateTestProgram()
    {
        return new ProgramNode(new List<ASTNode>
        {
            new FunctionDeclaration(
                "test",
                new List<Parameter>(),
                "int",
                new List<ASTNode>
                {
                    new ReturnStatement(new NumberLiteral(42))
                }
            )
        });
    }

    // Test rule implementation for testing
    private class TestRule : LintRule
    {
        public override string RuleId => "test-rule";
        public override string Description => "Test rule for unit tests";
        public override string Category => "test";

        public override IEnumerable<AnalysisDiagnostic> Analyze(ProgramNode ast, string filePath, string sourceText)
        {
            yield return CreateDiagnostic(
                "Test diagnostic",
                new SourceLocation(filePath, 1, 1, 0, "test line")
            );
        }

        public T? GetTestParameter<T>(string paramName, T? defaultValue = default)
        {
            return GetParameter(paramName, defaultValue);
        }
    }
}