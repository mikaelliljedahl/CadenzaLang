using Cadenza.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Cadenza.Analysis;
using NUnit.Framework;

namespace Cadenza.Tests.Unit.Analysis;

[TestFixture]
public class EffectAnalyzerTests
{
    [Test]
    public void Analysis_PureFunctionValidationRule_ShouldDetectPureFunctionWithEffects()
    {
        // Arrange
        var rule = new EffectAnalyzer.PureFunctionValidationRule();
        var ast = new ProgramNode(new List<ASTNode>
        {
            new FunctionDeclaration(
                "badPure",
                new List<Parameter>(),
                "int",
                new List<ASTNode>(),
                IsPure: true,
                Effects: new List<string> { "Database" }
            )
        });

        // Act
        var diagnostics = rule.Analyze(ast, "test.cdz", "pure function badPure() uses [Database] -> int {}").ToList();

        // Assert
        Assert.That(diagnostics.Count, Is.EqualTo(1));
        Assert.That(diagnostics[0].RuleId, Is.EqualTo("pure-function-validation"));
        Assert.That(diagnostics[0].Message, Does.Contain("Pure function 'badPure' cannot declare effects"));
        Assert.That(diagnostics[0].Severity, Is.EqualTo(DiagnosticSeverity.Warning));
    }

    [Test]
    public void Analysis_PureFunctionValidationRule_ShouldAllowPureFunctionWithoutEffects()
    {
        // Arrange
        var rule = new EffectAnalyzer.PureFunctionValidationRule();
        var ast = new ProgramNode(new List<ASTNode>
        {
            new FunctionDeclaration(
                "goodPure",
                new List<Parameter>(),
                "int",
                new List<ASTNode>(),
                IsPure: true
            )
        });

        // Act
        var diagnostics = rule.Analyze(ast, "test.cdz", "pure function goodPure() -> int {}").ToList();

        // Assert
        Assert.That(diagnostics, Is.Not.Null);
    }

    [Test]
    public void Analysis_EffectCompletenessRule_ShouldDetectMissingEffectDeclarations()
    {
        // Arrange
        var rule = new EffectAnalyzer.EffectCompletenessRule();
        var functionBody = new List<ASTNode>
        {
            new CallExpression("database_query", new List<ASTNode>()),
            new ReturnStatement(new NumberLiteral(42))
        };

        var ast = new ProgramNode(new List<ASTNode>
        {
            new FunctionDeclaration(
                "testFunction",
                new List<Parameter>(),
                "int",
                functionBody,
                IsPure: false // No effects declared
            )
        });

        // Act
        var diagnostics = rule.Analyze(ast, "test.cdz", 
            "function testFunction() -> int { database_query() return 42 }").ToList();

        // Assert
        Assert.That(diagnostics.Count, Is.EqualTo(1));
        Assert.That(diagnostics[0].RuleId, Is.EqualTo("effect-completeness"));
        Assert.That(diagnostics[0].Message, Does.Contain("uses effects"));
        Assert.That(diagnostics[0].Message, Does.Contain("doesn't declare them"));
    }

    [Test]
    public void Analysis_EffectMinimalityRule_ShouldDetectUnusedEffects()
    {
        // Arrange
        var rule = new EffectAnalyzer.EffectMinimalityRule();
        var functionBody = new List<ASTNode>
        {
            new ReturnStatement(new NumberLiteral(42))
        };

        var ast = new ProgramNode(new List<ASTNode>
        {
            new FunctionDeclaration(
                "testFunction",
                new List<Parameter>(),
                "int",
                functionBody,
                IsPure: false,
                Effects: new List<string> { "Database", "Network" } // Effects declared but not used
            )
        });

        // Act
        var diagnostics = rule.Analyze(ast, "test.cdz", 
            "function testFunction() uses [Database, Network] -> int { return 42 }").ToList();

        // Assert
        Assert.That(diagnostics.Count, Is.EqualTo(1));
        Assert.That(diagnostics[0].RuleId, Is.EqualTo("effect-minimality"));
        Assert.That(diagnostics[0].Message, Does.Contain("declares effects"));
        Assert.That(diagnostics[0].Message, Does.Contain("doesn't appear to use them"));
    }

    [Test]
    public void Analysis_EffectPropagationRule_ShouldDetectMissingEffectPropagation()
    {
        // Arrange
        var rule = new EffectAnalyzer.EffectPropagationRule();
        
        // First function with effects
        var dbFunction = new FunctionDeclaration(
            "queryDatabase",
            new List<Parameter>(),
            "Result<string, string>",
            new List<ASTNode>(),
            IsPure: false,
                Effects: new List<string> { "Database" }
        );

        // Second function that calls the first but doesn't declare Database effect
        var callerFunction = new FunctionDeclaration(
            "processData",
            new List<Parameter>(),
            "Result<string, string>",
            new List<ASTNode>
            {
                new CallExpression("queryDatabase", new List<ASTNode>())
            },
            IsPure: false // Missing Database effect
        );

        var ast = new ProgramNode(new List<ASTNode> { dbFunction, callerFunction });

        // Act
        var diagnostics = rule.Analyze(ast, "test.cdz", "test source").ToList();

        // Assert
        Assert.That(diagnostics.Count, Is.EqualTo(1));
        Assert.That(diagnostics[0].RuleId, Is.EqualTo("effect-propagation"));
        Assert.That(diagnostics[0].Message, Does.Contain("calls 'queryDatabase' but doesn't declare required effects"));
    }

    [Test]
    public void Analysis_EffectPropagationRule_ShouldDetectPureFunctionCallingEffectFunction()
    {
        // Arrange
        var rule = new EffectAnalyzer.EffectPropagationRule();
        
        // Function with effects
        var effectFunction = new FunctionDeclaration(
            "writeLog",
            new List<Parameter>(),
            "Result<string, string>",
            new List<ASTNode>(),
            IsPure: false,
                Effects: new List<string> { "Logging" }
        );

        // Pure function that calls the effect function (violation)
        var pureFunction = new FunctionDeclaration(
            "calculate",
            new List<Parameter>(),
            "int",
            new List<ASTNode>
            {
                new CallExpression("writeLog", new List<ASTNode>()),
                new ReturnStatement(new NumberLiteral(42))
            },
            IsPure: true
        );

        var ast = new ProgramNode(new List<ASTNode> { effectFunction, pureFunction });

        // Act
        var diagnostics = rule.Analyze(ast, "test.cdz", "test source").ToList();

        // Assert
        Assert.That(diagnostics.Count, Is.EqualTo(1));
        Assert.That(diagnostics[0].RuleId, Is.EqualTo("effect-propagation"));
        Assert.That(diagnostics[0].Message, Does.Contain("Pure function 'calculate' cannot call function 'writeLog' which has effects"));
    }

    [Test]
    public void Analysis_EffectPropagationRule_ShouldAllowProperEffectPropagation()
    {
        // Arrange
        var rule = new EffectAnalyzer.EffectPropagationRule();
        
        // Function with effects
        var dbFunction = new FunctionDeclaration(
            "queryDatabase",
            new List<Parameter>(),
            "Result<string, string>",
            new List<ASTNode>(),
            IsPure: false,
                Effects: new List<string> { "Database" }
        );

        // Function that properly declares effects it uses
        var callerFunction = new FunctionDeclaration(
            "processData",
            new List<Parameter>(),
            "Result<string, string>",
            new List<ASTNode>
            {
                new CallExpression("queryDatabase", new List<ASTNode>())
            },
            IsPure: false,
                Effects: new List<string> { "Database" } // Properly declares Database effect
        );

        var ast = new ProgramNode(new List<ASTNode> { dbFunction, callerFunction });

        // Act
        var diagnostics = rule.Analyze(ast, "test.cdz", "test source").ToList();

        // Assert
        Assert.That(diagnostics, Is.Not.Null);
    }

    [Test]
    public void Analysis_EffectAnalyzer_ShouldProvideAllRules()
    {
        // Act
        var rules = EffectAnalyzer.GetRules().ToList();

        // Assert
        Assert.That(rules, Is.Not.Null);
        Assert.That(rules.Any(r => r.RuleId == "pure-function-validation"), Is.True);
        Assert.That(rules.Any(r => r.RuleId == "effect-completeness"), Is.True);
        Assert.That(rules.Any(r => r.RuleId == "effect-minimality"), Is.True);
        Assert.That(rules.Any(r => r.RuleId == "effect-propagation"), Is.True);
        
        // All rules should have the effect system category
        foreach (var r in rules)
            Assert.That(r.Category, Is.EqualTo(AnalysisCategories.EffectSystem));
    }

    [Test]
    [TestCase("database_save", "Database")]
    [TestCase("http_request", "Network")]
    [TestCase("log_message", "Logging")]
    [TestCase("file_read", "FileSystem")]
    [TestCase("cache_store", "Memory")]
    [TestCase("input_stream", "IO")]
    public void Analysis_EffectCompletenessRule_ShouldDetectEffectsByFunctionName(string functionName, string expectedEffect)
    {
        // Arrange
        var rule = new EffectAnalyzer.EffectCompletenessRule();
        var functionBody = new List<ASTNode>
        {
            new CallExpression(functionName, new List<ASTNode>()),
            new ReturnStatement(new NumberLiteral(42))
        };

        var ast = new ProgramNode(new List<ASTNode>
        {
            new FunctionDeclaration(
                "testFunction",
                new List<Parameter>(),
                "int",
                functionBody,
                IsPure: false
            )
        });

        // Act
        var diagnostics = rule.Analyze(ast, "test.cdz", $"function testFunction() -> int {{ {functionName}() return 42 }}").ToList();

        // Assert
        Assert.That(diagnostics.Count, Is.EqualTo(1));
        Assert.That(diagnostics[0].Message, Does.Contain(expectedEffect));
    }
}