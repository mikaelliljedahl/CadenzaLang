using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Cadenza.Core;

namespace Cadenza.Analysis;

/// <summary>
/// Analyzer for Cadenza security best practices and vulnerability detection
/// </summary>
public class SecurityAnalyzer
{
    /// <summary>
    /// Rule: Functions handling external input should validate it
    /// </summary>
    public class InputValidationRule : LintRule
    {
        public override string RuleId => "input-validation";
        public override string Description => "Functions handling external input should validate it";
        public override string Category => AnalysisCategories.Security;

        public override IEnumerable<AnalysisDiagnostic> Analyze(ProgramNode ast, string filePath, string sourceText)
        {
            var visitor = new InputValidationVisitor(filePath, sourceText);
            visitor.Visit(ast);
            return visitor.GetDiagnostics(this);
        }

        private class InputValidationVisitor : ASTVisitor
        {
            private readonly List<(FunctionDeclaration func, Parameter param, SourceLocation location)> _violations = new();

            public InputValidationVisitor(string filePath, string sourceText) : base(filePath, sourceText) { }

            public override void VisitFunctionDeclaration(FunctionDeclaration func)
            {
                if (IsExternalInputFunction(func))
                {
                    CheckInputValidation(func);
                }
                base.VisitFunctionDeclaration(func);
            }

            private bool IsExternalInputFunction(FunctionDeclaration func)
            {
                // Check if function has effects that suggest external input
                var effects = func.Effects ?? new List<string>();
                return effects.Contains("Network") || effects.Contains("IO") || effects.Contains("FileSystem") ||
                       func.Name.Contains("api") || func.Name.Contains("input") || func.Name.Contains("user") ||
                       func.Name.Contains("request") || func.Name.Contains("external");
            }

            private void CheckInputValidation(FunctionDeclaration func)
            {
                var stringParams = func.Parameters.Where(p => p.Type == "string").ToList();
                
                foreach (var param in stringParams)
                {
                    if (!HasValidationForParameter(func, param))
                    {
                        var location = GetLocation(param.Name);
                        _violations.Add((func, param, location));
                    }
                }
            }

            private bool HasValidationForParameter(FunctionDeclaration func, Parameter param)
            {
                // Look for validation patterns in function body
                foreach (var stmt in func.Body)
                {
                    if (ContainsValidationFor(stmt, param.Name))
                        return true;
                }
                return false;
            }

            private bool ContainsValidationFor(ASTNode node, string paramName)
            {
                switch (node)
                {
                    case IfStatement ifStmt:
                        return ContainsParameterCheck(ifStmt.Condition, paramName);

                    case GuardStatement guard:
                        return ContainsParameterCheck(guard.Condition, paramName);

                    case LetStatement let when let.Expression is CallExpression call:
                        return call.Name.Contains("validate") && 
                               call.Arguments.Any(arg => arg is Identifier id && id.Name == paramName);

                    default:
                        return false;
                }
            }

            private bool ContainsParameterCheck(ASTNode condition, string paramName)
            {
                switch (condition)
                {
                    case BinaryExpression binary:
                        return ContainsIdentifier(binary.Left, paramName) || ContainsIdentifier(binary.Right, paramName);

                    case CallExpression call:
                        return call.Arguments.Any(arg => arg is Identifier id && id.Name == paramName);

                    default:
                        return false;
                }
            }

            private bool ContainsIdentifier(ASTNode node, string paramName)
            {
                return node is Identifier id && id.Name == paramName;
            }

            public IEnumerable<AnalysisDiagnostic> GetDiagnostics(LintRule rule)
            {
                return _violations.Select(v => rule.CreateDiagnostic(
                    $"Parameter '{v.param.Name}' in function '{v.func.Name}' should be validated before use",
                    v.location,
                    "Add input validation: guard param != \"\" else { return Error(\"Invalid input\") }"
                ));
            }
        }
    }

    /// <summary>
    /// Rule: Ensure sensitive effects are properly contained
    /// </summary>
    public class EffectLeakageRule : LintRule
    {
        public override string RuleId => "effect-leakage";
        public override string Description => "Ensure sensitive effects are properly contained";
        public override string Category => AnalysisCategories.Security;

        public override IEnumerable<AnalysisDiagnostic> Analyze(ProgramNode ast, string filePath, string sourceText)
        {
            var visitor = new EffectLeakageVisitor(filePath, sourceText);
            visitor.Visit(ast);
            return visitor.GetDiagnostics(this);
        }

        private class EffectLeakageVisitor : ASTVisitor
        {
            private readonly Dictionary<string, HashSet<string>> _functionEffects = new();
            private readonly List<(string issue, SourceLocation location)> _violations = new();

            public EffectLeakageVisitor(string filePath, string sourceText) : base(filePath, sourceText) { }

            public override void VisitProgram(ProgramNode program)
            {
                // First pass: collect function effects
                foreach (var stmt in program.Statements)
                {
                    if (stmt is FunctionDeclaration func)
                    {
                        var effects = func.Effects?.ToHashSet() ?? new HashSet<string>();
                        _functionEffects[func.Name] = effects;
                    }
                }

                // Second pass: analyze effect propagation
                base.VisitProgram(program);
            }

            public override void VisitFunctionDeclaration(FunctionDeclaration func)
            {
                CheckForEffectLeakage(func);
                base.VisitFunctionDeclaration(func);
            }

            private void CheckForEffectLeakage(FunctionDeclaration func)
            {
                var sensitiveEffects = GetSensitiveEffects(func);
                if (sensitiveEffects.Count == 0)
                    return;

                // Check if sensitive effects are properly contained
                CheckEffectContainment(func, sensitiveEffects);
                
                // Check if function exposes sensitive operations
                CheckSensitiveOperationExposure(func, sensitiveEffects);
            }

            private HashSet<string> GetSensitiveEffects(FunctionDeclaration func)
            {
                var effects = func.Effects ?? new List<string>();
                var sensitive = new HashSet<string>();

                foreach (var effect in effects)
                {
                    if (IsSensitiveEffect(effect))
                    {
                        sensitive.Add(effect);
                    }
                }

                return sensitive;
            }

            private bool IsSensitiveEffect(string effect)
            {
                return effect == "Database" || effect == "Network" || effect == "FileSystem";
            }

            private void CheckEffectContainment(FunctionDeclaration func, HashSet<string> sensitiveEffects)
            {
                // Check if function with sensitive effects is exported/public without proper access control
                if (sensitiveEffects.Count > 0 && IsPublicFunction(func))
                {
                    var location = GetLocation(func.Name);
                    _violations.Add(($"Function '{func.Name}' with sensitive effects ({string.Join(", ", sensitiveEffects)}) should have access control", location));
                }
            }

            private void CheckSensitiveOperationExposure(FunctionDeclaration func, HashSet<string> sensitiveEffects)
            {
                if (func.Name.Contains("admin") || func.Name.Contains("delete") || func.Name.Contains("remove"))
                {
                    if (!HasAuthenticationCheck(func))
                    {
                        var location = GetLocation(func.Name);
                        _violations.Add(($"Sensitive function '{func.Name}' should include authentication checks", location));
                    }
                }
            }

            private bool IsPublicFunction(FunctionDeclaration func)
            {
                // In Cadenza, functions are public by default unless in a module with explicit exports
                return !func.Name.StartsWith("_") && !func.Name.Contains("internal");
            }

            private bool HasAuthenticationCheck(FunctionDeclaration func)
            {
                // Look for authentication patterns in function body
                return func.Body.Any(stmt => ContainsAuthenticationCheck(stmt));
            }

            private bool ContainsAuthenticationCheck(ASTNode node)
            {
                switch (node)
                {
                    case CallExpression call:
                        return call.Name.Contains("auth") || call.Name.Contains("verify") || call.Name.Contains("check");

                    case IfStatement ifStmt:
                        return ContainsAuthenticationCheck(ifStmt.Condition) ||
                               ifStmt.ThenBody.Any(ContainsAuthenticationCheck) ||
                               (ifStmt.ElseBody?.Any(ContainsAuthenticationCheck) ?? false);

                    case GuardStatement guard:
                        return ContainsAuthenticationCheck(guard.Condition);

                    case LetStatement let:
                        return ContainsAuthenticationCheck(let.Expression);

                    default:
                        return false;
                }
            }

            public IEnumerable<AnalysisDiagnostic> GetDiagnostics(LintRule rule)
            {
                return _violations.Select(v => rule.CreateDiagnostic(
                    v.issue,
                    v.location,
                    "Add proper access control and authentication checks for sensitive operations"
                ));
            }
        }
    }

    /// <summary>
    /// Rule: Detect hardcoded secrets and sensitive data
    /// </summary>
    public class SecretDetectionRule : LintRule
    {
        public override string RuleId => "secret-detection";
        public override string Description => "Detect hardcoded secrets and sensitive data";
        public override string Category => AnalysisCategories.Security;

        private static readonly Dictionary<string, Regex> SecretPatterns = new()
        {
            ["API Key"] = new Regex(@"api[_-]?key[""']?\s*[:=]\s*[""']([a-zA-Z0-9_\-]{20,})[""']", RegexOptions.IgnoreCase),
            ["Password"] = new Regex(@"password[""']?\s*[:=]\s*[""']([^""']{8,})[""']", RegexOptions.IgnoreCase),
            ["Secret"] = new Regex(@"secret[""']?\s*[:=]\s*[""']([^""']{10,})[""']", RegexOptions.IgnoreCase),
            ["Token"] = new Regex(@"token[""']?\s*[:=]\s*[""']([a-zA-Z0-9_\-\.]{20,})[""']", RegexOptions.IgnoreCase),
            ["Connection String"] = new Regex(@"connection[_-]?string[""']?\s*[:=]\s*[""']([^""']*(?:password|pwd)[^""']*)[""']", RegexOptions.IgnoreCase),
            ["Private Key"] = new Regex(@"-----BEGIN\s+(RSA\s+)?PRIVATE\s+KEY-----", RegexOptions.IgnoreCase)
        };

        public override IEnumerable<AnalysisDiagnostic> Analyze(ProgramNode ast, string filePath, string sourceText)
        {
            var visitor = new SecretDetectionVisitor(filePath, sourceText);
            visitor.Visit(ast);
            return visitor.GetDiagnostics(this);
        }

        private class SecretDetectionVisitor : ASTVisitor
        {
            private readonly List<(string secretType, string value, SourceLocation location)> _violations = new();

            public SecretDetectionVisitor(string filePath, string sourceText) : base(filePath, sourceText) { }

            public override void VisitFunctionDeclaration(FunctionDeclaration func)
            {
                CheckFunctionForSecrets(func);
                base.VisitFunctionDeclaration(func);
            }

            private void CheckFunctionForSecrets(FunctionDeclaration func)
            {
                foreach (var stmt in func.Body)
                {
                    CheckStatementForSecrets(stmt);
                }
            }

            private void CheckStatementForSecrets(ASTNode node)
            {
                switch (node)
                {
                    case LetStatement let when let.Expression is StringLiteral str:
                        CheckStringForSecrets(str.Value, GetLocation(let.Name));
                        break;

                    case ReturnStatement ret when ret.Expression is StringLiteral str:
                        CheckStringForSecrets(str.Value, GetLocation("return"));
                        break;

                    case StringLiteral str:
                        CheckStringForSecrets(str.Value, GetLocation(str.Value.Substring(0, Math.Min(10, str.Value.Length))));
                        break;

                    case IfStatement ifStmt:
                        foreach (var stmt in ifStmt.ThenBody)
                            CheckStatementForSecrets(stmt);
                        if (ifStmt.ElseBody != null)
                        {
                            foreach (var stmt in ifStmt.ElseBody)
                                CheckStatementForSecrets(stmt);
                        }
                        break;

                    case GuardStatement guard:
                        foreach (var stmt in guard.ElseBody)
                            CheckStatementForSecrets(stmt);
                        break;
                }

                // Also check the raw source text for patterns
                CheckSourceTextForSecrets();
            }

            private void CheckStringForSecrets(string text, SourceLocation location)
            {
                foreach (var pattern in SecretPatterns)
                {
                    var matches = pattern.Value.Matches(text);
                    foreach (Match match in matches)
                    {
                        if (match.Success && !IsLikelyExample(match.Groups[1].Value))
                        {
                            _violations.Add((pattern.Key, match.Groups[1].Value, location));
                        }
                    }
                }

                // Check for suspicious long strings that might be secrets
                if (text.Length > 30 && IsLikelySecret(text))
                {
                    _violations.Add(("Suspicious long string", text, location));
                }
            }

            private void CheckSourceTextForSecrets()
            {
                foreach (var pattern in SecretPatterns)
                {
                    var matches = pattern.Value.Matches(SourceText);
                    foreach (Match match in matches)
                    {
                        if (match.Success && !IsLikelyExample(match.Groups[1].Value))
                        {
                            var line = GetLineNumber(match.Index);
                            var location = new SourceLocation(FilePath, line, match.Index, match.Length, GetLineText(line));
                            _violations.Add((pattern.Key, match.Groups[1].Value, location));
                        }
                    }
                }
            }

            private bool IsLikelyExample(string value)
            {
                var lowerValue = value.ToLower();
                return lowerValue.Contains("example") || lowerValue.Contains("test") || 
                       lowerValue.Contains("dummy") || lowerValue.Contains("placeholder") ||
                       lowerValue == "your_api_key_here" || lowerValue == "changeme" ||
                       value.All(c => c == 'x' || c == '*' || c == '?');
            }

            private bool IsLikelySecret(string text)
            {
                // Heuristics for detecting secrets
                var hasUpperCase = text.Any(char.IsUpper);
                var hasLowerCase = text.Any(char.IsLower);
                var hasDigits = text.Any(char.IsDigit);
                var hasSpecialChars = text.Any(c => !char.IsLetterOrDigit(c));
                
                // Mix of character types suggests encoded data
                var charTypeCount = new[] { hasUpperCase, hasLowerCase, hasDigits, hasSpecialChars }.Count(x => x);
                
                return charTypeCount >= 3 && text.Length > 20;
            }

            private int GetLineNumber(int charIndex)
            {
                return SourceText.Take(charIndex).Count(c => c == '\n') + 1;
            }

            private string GetLineText(int lineNumber)
            {
                var lines = SourceText.Split('\n');
                return lineNumber <= lines.Length ? lines[lineNumber - 1] : "";
            }

            public IEnumerable<AnalysisDiagnostic> GetDiagnostics(LintRule rule)
            {
                return _violations.Select(v => rule.CreateDiagnostic(
                    $"Potential {v.secretType} detected in source code",
                    v.location,
                    "Move sensitive data to environment variables or secure configuration"
                ));
            }
        }
    }

    /// <summary>
    /// Rule: Check for unsafe string interpolation
    /// </summary>
    public class UnsafeStringInterpolationRule : LintRule
    {
        public override string RuleId => "unsafe-string-interpolation";
        public override string Description => "Detect potentially unsafe string interpolation patterns";
        public override string Category => AnalysisCategories.Security;

        public override IEnumerable<AnalysisDiagnostic> Analyze(ProgramNode ast, string filePath, string sourceText)
        {
            var visitor = new UnsafeStringInterpolationVisitor(filePath, sourceText);
            visitor.Visit(ast);
            return visitor.GetDiagnostics(this);
        }

        private class UnsafeStringInterpolationVisitor : ASTVisitor
        {
            private readonly List<(string issue, SourceLocation location)> _violations = new();

            public UnsafeStringInterpolationVisitor(string filePath, string sourceText) : base(filePath, sourceText) { }

            public override void VisitFunctionDeclaration(FunctionDeclaration func)
            {
                CheckFunctionForUnsafeInterpolation(func);
                base.VisitFunctionDeclaration(func);
            }

            private void CheckFunctionForUnsafeInterpolation(FunctionDeclaration func)
            {
                foreach (var stmt in func.Body)
                {
                    CheckStatementForUnsafeInterpolation(stmt);
                }
            }

            private void CheckStatementForUnsafeInterpolation(ASTNode node)
            {
                switch (node)
                {
                    case StringInterpolation interp:
                        CheckStringInterpolation(interp);
                        break;

                    case LetStatement let:
                        CheckStatementForUnsafeInterpolation(let.Expression);
                        break;

                    case ReturnStatement ret:
                        CheckStatementForUnsafeInterpolation(ret.Expression);
                        break;

                    case BinaryExpression binary:
                        CheckStatementForUnsafeInterpolation(binary.Left);
                        CheckStatementForUnsafeInterpolation(binary.Right);
                        break;

                    case IfStatement ifStmt:
                        CheckStatementForUnsafeInterpolation(ifStmt.Condition);
                        foreach (var stmt in ifStmt.ThenBody)
                            CheckStatementForUnsafeInterpolation(stmt);
                        if (ifStmt.ElseBody != null)
                        {
                            foreach (var stmt in ifStmt.ElseBody)
                                CheckStatementForUnsafeInterpolation(stmt);
                        }
                        break;
                }
            }

            private void CheckStringInterpolation(StringInterpolation interp)
            {
                foreach (var part in interp.Parts)
                {
                    if (part is Identifier id)
                    {
                        if (IsUserInput(id.Name))
                        {
                            var location = GetLocation(id.Name);
                            _violations.Add(($"String interpolation with user input '{id.Name}' may be unsafe", location));
                        }
                    }
                }

                // Check if interpolation is used in SQL-like context
                var stringParts = interp.Parts.OfType<StringLiteral>().ToList();
                foreach (var part in stringParts)
                {
                    if (LooksLikeSqlQuery(part.Value))
                    {
                        var location = GetLocation(part.Value.Substring(0, Math.Min(10, part.Value.Length)));
                        _violations.Add(("String interpolation in SQL context may lead to injection attacks", location));
                    }
                }
            }

            private bool IsUserInput(string variableName)
            {
                var name = variableName.ToLower();
                return name.Contains("user") || name.Contains("input") || name.Contains("request") ||
                       name.Contains("param") || name.Contains("query") || name.Contains("form") ||
                       name.Contains("external");
            }

            private bool LooksLikeSqlQuery(string text)
            {
                var lowerText = text.ToLower();
                return lowerText.Contains("select") || lowerText.Contains("insert") ||
                       lowerText.Contains("update") || lowerText.Contains("delete") ||
                       lowerText.Contains("where") || lowerText.Contains("from");
            }

            public IEnumerable<AnalysisDiagnostic> GetDiagnostics(LintRule rule)
            {
                return _violations.Select(v => rule.CreateDiagnostic(
                    v.issue,
                    v.location,
                    "Use parameterized queries or proper input sanitization for user data"
                ));
            }
        }
    }

    /// <summary>
    /// Get all security analyzer rules
    /// </summary>
    public static IEnumerable<LintRule> GetRules()
    {
        yield return new InputValidationRule();
        yield return new EffectLeakageRule();
        yield return new SecretDetectionRule();
        yield return new UnsafeStringInterpolationRule();
    }
}