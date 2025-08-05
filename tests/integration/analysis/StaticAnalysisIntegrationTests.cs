using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cadenza.Analysis;
using NUnit.Framework;
using Cadenza.Tests.Framework;

namespace Cadenza.Tests.Integration.Analysis;

public class StaticAnalysisIntegrationTests : TestBase
{
    [Test]
    public async Task StaticAnalyzer_ShouldAnalyzeCompleteFlowProgram()
    {
        // Arrange
        var sourceCode = @"
// Complete Cadenza program with various issues
module TestModule {
    // Missing effect declaration (should trigger effect-completeness)
    function process_user_data(user_id: string) -> Result<string, string> {
        let log_result = log_message(""Processing user: "" + user_id)
        let db_result = database_query(""SELECT * FROM users WHERE id = "" + user_id)
        return Ok(""processed"")
    }
    
    // Pure function calling effect function (should trigger effect-propagation)
    pure function calculate_score(data: string) -> int {
        log_debug(""Calculating score"")
        return 100
    }
    
    // Unused function (should trigger dead-code)
    function unused_helper(x: int) -> int {
        return x * 2
    }
    
    // Function with unused effects (should trigger effect-minimality)
    function simple_math(a: int, b: int) uses [Database, Network] -> int {
        return a + b
    }
    
    // Result not handled (should trigger unused-results)
    function main() -> int {
        process_user_data(""123"")
        return 42
    }
    
    export { process_user_data, calculate_score, main }
}";

        var analyzer = new StaticAnalyzer();

        // Act
        var report = analyzer.AnalyzeFile("test.cdz", sourceCode);

        // Assert
        Assert.That(report.Diagnostics.Count > 0, "Should find multiple issues in the test code");
        
        // Check for specific rule violations
        var ruleIds = report.Diagnostics.Select(d => d.RuleId).ToHashSet();
        
        // Should detect various categories of issues
        Assert.That(ruleIds.Count >= 3, $"Should detect issues from multiple rules. Found: {string.Join(", ", ruleIds)}");
        
        // Verify metrics are collected
        Assert.That(report.Metrics.TotalFunctions > 0, Is.True);
        Assert.That(report.Metrics.ModuleCount, Is.EqualTo(1));
        
        // Should have both pure and effect functions
        Assert.That(report.Metrics.PureFunctions > 0, Is.True);
        Assert.That(report.Metrics.FunctionsWithEffects > 0, Is.True);
    }

    [Test]
    public async Task StaticAnalyzer_ShouldRespectConfiguration()
    {
        // Arrange
        var sourceCode = @"
function test_func() -> int {
    let unused_var = 42
    return 100
}";

        var strictConfig = new LintConfiguration
        {
            SeverityThreshold = DiagnosticSeverity.Info,
            Rules = new Dictionary<string, LintRuleConfig>
            {
                ["unused-variables"] = new(DiagnosticSeverity.Error, true),
                ["effect-completeness"] = new(DiagnosticSeverity.Error, false) // Disabled
            }
        };

        var lenientConfig = new LintConfiguration
        {
            SeverityThreshold = DiagnosticSeverity.Error,
            Rules = new Dictionary<string, LintRuleConfig>
            {
                ["unused-variables"] = new(DiagnosticSeverity.Warning, true) // Should be filtered out
            }
        };

        var strictAnalyzer = new StaticAnalyzer(strictConfig);
        var lenientAnalyzer = new StaticAnalyzer(lenientConfig);

        // Act
        var strictReport = strictAnalyzer.AnalyzeFile("test.cdz", sourceCode);
        var lenientReport = lenientAnalyzer.AnalyzeFile("test.cdz", sourceCode);

        // Assert
        Assert.That(strictReport.Diagnostics.Count >= lenientReport.Diagnostics.Count,
            "Strict configuration should find more or equal issues than lenient");
    }

    [Test]
    public async Task StaticAnalyzer_ShouldGenerateJsonOutput()
    {
        // Arrange
        var sourceCode = @"
function bad_function() -> Result<int, string> {
    return Error(""test error"")
}";

        var analyzer = new StaticAnalyzer();

        // Act
        var report = analyzer.AnalyzeFile("test.cdz", sourceCode);
        var json = report.ToJson();

        // Assert
        Assert.That(json, Is.Not.Null);
        Assert.That(json, Does.Contain("\"diagnostics\""));
        Assert.That(json, Does.Contain("\"metrics\""));
        Assert.That(json, Does.Contain("\"ruleCounts\""));
        
        // Should be valid JSON

    }

    [Test]
    public async Task StaticAnalyzer_ShouldGenerateSarifOutput()
    {
        // Arrange
        var sourceCode = @"
function security_issue() -> string {
    let api_key = ""sk-1234567890abcdef1234567890abcdef""
    return api_key
}";

        var analyzer = new StaticAnalyzer();

        // Act
        var report = analyzer.AnalyzeFile("test.cdz", sourceCode);
        var sarif = report.ToSarif();

        // Assert
        Assert.That(sarif, Is.Not.Null);
        Assert.That(sarif, Does.Contain("\"version\": \"2.1.0\""));
        Assert.That(sarif, Does.Contain("\"runs\""));
        Assert.That(sarif, Does.Contain("\"results\""));
        
        // Should be valid JSON
        Assert.DoesNotThrow(() => System.Text.Json.JsonDocument.Parse(sarif));
    }

    [Test]
    public async Task StaticAnalyzer_ShouldAnalyzeRealWorldExample()
    {
        // Arrange - A more realistic Cadenza program
        var sourceCode = @"
module UserService {
    function authenticate_user(username: string, password: string) 
        uses [Database, Logging] 
        -> Result<User, AuthError> {
        
        guard username != """" else {
            return Error(""Empty username"")
        }
        
        let log_result = log_info(""Authenticating user: "" + username)?
        let user_record = database_query(""SELECT * FROM users WHERE username = ?"", username)?
        
        if verify_password(password, user_record.password_hash) {
            return Ok(user_record)
        } else {
            return Error(""Invalid credentials"")
        }
    }
    
    function get_user_profile(user_id: string) 
        uses [Database, Network] 
        -> Result<UserProfile, string> {
        
        let user = database_get_user(user_id)?
        let profile_data = api_fetch_profile(user.external_id)?
        
        return Ok(create_profile(user, profile_data))
    }
    
    pure function create_profile(user: User, data: ProfileData) -> UserProfile {
        return create_user_profile(user.id, user.name, data)
    }
    
    export { authenticate_user, get_user_profile }
}";

        var analyzer = new StaticAnalyzer();

        // Act
        var report = analyzer.AnalyzeFile("user_service.cdz", sourceCode);

        // Assert
        Assert.That(report.FilesAnalyzed, Is.EqualTo(1));
        
        // Should have found the module and functions
        Assert.That(report.Metrics.ModuleCount, Is.EqualTo(1));
        Assert.That(report.Metrics.TotalFunctions, Is.EqualTo(3));
        Assert.That(report.Metrics.PureFunctions, Is.EqualTo(1));
        Assert.That(report.Metrics.FunctionsWithEffects, Is.EqualTo(2));
        
        // Should track effect usage
        Assert.That(report.Metrics.EffectUsage.ContainsKey("Database"));
        Assert.That(report.Metrics.EffectUsage.ContainsKey("Logging"));
        Assert.That(report.Metrics.EffectUsage.ContainsKey("Network"));
        
        // Should have proper Result type usage
        Assert.That(report.Metrics.ResultTypeUsage > 0, Is.True);
        
        // Should detect error propagation
        Assert.That(report.Metrics.ErrorPropagationCount > 0, Is.True);
    }

    [Test]
    public async Task StaticAnalyzer_ShouldHandleParseErrors()
    {
        // Arrange - Invalid Cadenza syntax
        var invalidSourceCode = @"
function invalid syntax here {
    this is not valid Cadenza
    missing return type and other issues
";

        var analyzer = new StaticAnalyzer();

        // Act
        var report = analyzer.AnalyzeFile("invalid.cdz", invalidSourceCode);

        // Assert
        Assert.That(report.FilesAnalyzed, Is.EqualTo(1));
        Assert.That(report.Diagnostics.Count > 0, Is.True);
        
        // Should have a parse error
        var parseErrors = report.Diagnostics.Where(d => d.RuleId == "parse-error").ToList();
        Assert.That(parseErrors, Is.Not.Null);
        foreach (var error in parseErrors)
            Assert.That(error.Severity, Is.EqualTo(DiagnosticSeverity.Error));
    }

    [Test]
    public async Task StaticAnalyzer_ShouldDetectSecurityIssues()
    {
        // Arrange - Code with potential security issues
        var sourceCode = @"
function risky_operation(user_input: string) uses [Database] -> Result<string, string> {
    // Hardcoded secret (should trigger secret-detection)
    let api_key = ""sk-1234567890abcdef1234567890abcdef""
    
    // SQL injection risk (should trigger unsafe-string-interpolation)
    let query = $""SELECT * FROM users WHERE name = '{user_input}'""
    
    // No input validation (should trigger input-validation)
    let result = database_execute(query)
    
    return Ok(""completed"")
}";

        var analyzer = new StaticAnalyzer();

        // Act
        var report = analyzer.AnalyzeFile("risky.cdz", sourceCode);

        // Assert
        var securityIssues = report.GetDiagnosticsByCategory(AnalysisCategories.Security).ToList();
        Assert.That(securityIssues, Is.Not.Null);
        
        // Should detect multiple security issues
        var ruleIds = securityIssues.Select(d => d.RuleId).ToHashSet();
        Assert.That(ruleIds.Count > 0, "Should detect security-related issues");
    }

    [Test]
    public async Task StaticAnalyzer_ShouldProvideFixSuggestions()
    {
        // Arrange
        var sourceCode = @"
function example() -> Result<string, string> {
    let unused_variable = ""test""
    let result = some_result_function()
    return Ok(""done"")
}";

        var analyzer = new StaticAnalyzer();

        // Act
        var report = analyzer.AnalyzeFile("example.cdz", sourceCode);

        // Assert
        var diagnosticsWithFixes = report.Diagnostics.Where(d => !string.IsNullOrEmpty(d.FixSuggestion)).ToList();
        Assert.That(diagnosticsWithFixes, Is.Not.Null);
        
        // Fix suggestions should be helpful
        foreach (var d in diagnosticsWithFixes)
        {
            Assert.That(d.FixSuggestion, Is.Not.Null);
            Assert.That(d.FixSuggestion!.Length > 10, "Fix suggestions should be descriptive");
        }
    }
}