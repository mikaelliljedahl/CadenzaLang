using System;
using System.Collections.Generic;
using System.Linq;
using Cadenza.Core;

namespace Cadenza.Analysis;

/// <summary>
/// Analyzer for Cadenza performance optimization suggestions
/// </summary>
public class PerformanceAnalyzer
{
    /// <summary>
    /// Rule: Suggest StringBuilder for multiple string concatenations
    /// </summary>
    public class StringConcatenationRule : LintRule
    {
        public override string RuleId => "string-concatenation";
        public override string Description => "Suggest StringBuilder for multiple string concatenations";
        public override string Category => AnalysisCategories.Performance;

        public override IEnumerable<AnalysisDiagnostic> Analyze(ProgramNode ast, string filePath, string sourceText)
        {
            var visitor = new StringConcatenationVisitor(filePath, sourceText);
            visitor.Visit(ast);
            return visitor.GetDiagnostics(this);
        }

        private class StringConcatenationVisitor : ASTVisitor
        {
            private readonly List<(ASTNode node, int concatenationCount, SourceLocation location)> _violations = new();

            public StringConcatenationVisitor(string filePath, string sourceText) : base(filePath, sourceText) { }

            public override void VisitFunctionDeclaration(FunctionDeclaration func)
            {
                CheckFunctionForStringConcatenation(func);
                base.VisitFunctionDeclaration(func);
            }

            private void CheckFunctionForStringConcatenation(FunctionDeclaration func)
            {
                foreach (var stmt in func.Body)
                {
                    var concatenationCount = CountStringConcatenations(stmt);
                    if (concatenationCount >= 3)
                    {
                        var location = GetLocationFromStatement(stmt);
                        _violations.Add((stmt, concatenationCount, location));
                    }
                }
            }

            private int CountStringConcatenations(ASTNode node)
            {
                switch (node)
                {
                    case BinaryExpression binary when binary.Operator == "+":
                        // Check if this looks like string concatenation
                        if (IsLikelyStringConcatenation(binary))
                        {
                            return 1 + CountStringConcatenations(binary.Left) + CountStringConcatenations(binary.Right);
                        }
                        break;

                    case LetStatement let:
                        return CountStringConcatenations(let.Expression);

                    case ReturnStatement ret:
                        return CountStringConcatenations(ret.Expression);
                }

                return 0;
            }

            private bool IsLikelyStringConcatenation(BinaryExpression binary)
            {
                // Simple heuristics for string concatenation
                return ContainsStringLiteral(binary.Left) || ContainsStringLiteral(binary.Right) ||
                       ContainsStringInterpolation(binary.Left) || ContainsStringInterpolation(binary.Right);
            }

            private bool ContainsStringLiteral(ASTNode node)
            {
                return node is StringLiteral || 
                       (node is BinaryExpression binary && 
                        (ContainsStringLiteral(binary.Left) || ContainsStringLiteral(binary.Right)));
            }

            private bool ContainsStringInterpolation(ASTNode node)
            {
                return node is StringInterpolation ||
                       (node is BinaryExpression binary && 
                        (ContainsStringInterpolation(binary.Left) || ContainsStringInterpolation(binary.Right)));
            }

            private SourceLocation GetLocationFromStatement(ASTNode stmt)
            {
                string searchText = stmt switch
                {
                    LetStatement let => $"let {let.Name}",
                    ReturnStatement => "return",
                    _ => "+"
                };
                return GetLocation(searchText);
            }

            public IEnumerable<AnalysisDiagnostic> GetDiagnostics(LintRule rule)
            {
                return _violations.Select(v => rule.CreateDiagnostic(
                    $"Multiple string concatenations detected ({v.concatenationCount} operations). Consider using string interpolation or StringBuilder for better performance",
                    v.location,
                    "Use string interpolation $\"text {var} more text\" or StringBuilder for multiple concatenations"
                ));
            }
        }
    }

    /// <summary>
    /// Rule: Detect inefficient effect patterns
    /// </summary>
    public class InefficientEffectPatternsRule : LintRule
    {
        public override string RuleId => "inefficient-effect-patterns";
        public override string Description => "Detect inefficient effect usage patterns";
        public override string Category => AnalysisCategories.Performance;

        public override IEnumerable<AnalysisDiagnostic> Analyze(ProgramNode ast, string filePath, string sourceText)
        {
            var visitor = new InefficientEffectVisitor(filePath, sourceText);
            visitor.Visit(ast);
            return visitor.GetDiagnostics(this);
        }

        private class InefficientEffectVisitor : ASTVisitor
        {
            private readonly List<(string issue, SourceLocation location)> _violations = new();

            public InefficientEffectVisitor(string filePath, string sourceText) : base(filePath, sourceText) { }

            public override void VisitFunctionDeclaration(FunctionDeclaration func)
            {
                if (func.Effects != null)
                {
                    CheckForRedundantEffects(func);
                    CheckForEffectOrdering(func);
                }
                base.VisitFunctionDeclaration(func);
            }

            private void CheckForRedundantEffects(FunctionDeclaration func)
            {
                var effects = func.Effects ?? new List<string>();
                
                // Check for redundant effect combinations
                if (effects.Contains("Database") && effects.Contains("Network") && effects.Contains("IO"))
                {
                    var location = GetLocation(func.Name);
                    _violations.Add(("Function uses Database, Network, and IO effects. Consider batching operations or using a single comprehensive effect", location));
                }

                // Check for Memory + FileSystem combination (potential inefficiency)
                if (effects.Contains("Memory") && effects.Contains("FileSystem"))
                {
                    var location = GetLocation(func.Name);
                    _violations.Add(("Function uses both Memory and FileSystem effects. Consider caching strategies", location));
                }
            }

            private void CheckForEffectOrdering(FunctionDeclaration func)
            {
                // Analyze function body for effect usage order
                var effectUsageOrder = AnalyzeEffectUsageOrder(func.Body);
                
                if (effectUsageOrder.Count > 1)
                {
                    CheckForSuboptimalOrdering(func, effectUsageOrder);
                }
            }

            private List<string> AnalyzeEffectUsageOrder(List<ASTNode> body)
            {
                var order = new List<string>();
                foreach (var stmt in body)
                {
                    var effects = GetStatementEffects(stmt);
                    order.AddRange(effects);
                }
                return order;
            }

            private List<string> GetStatementEffects(ASTNode node)
            {
                var effects = new List<string>();
                
                if (node is CallExpression call)
                {
                    // Simple pattern matching for effect types
                    var name = call.Name.ToLower();
                    if (name.Contains("log")) effects.Add("Logging");
                    if (name.Contains("database") || name.Contains("query")) effects.Add("Database");
                    if (name.Contains("http") || name.Contains("network")) effects.Add("Network");
                    if (name.Contains("file")) effects.Add("FileSystem");
                }
                else if (node is LetStatement let)
                {
                    effects.AddRange(GetStatementEffects(let.Expression));
                }
                else if (node is ReturnStatement ret)
                {
                    effects.AddRange(GetStatementEffects(ret.Expression));
                }

                return effects;
            }

            private void CheckForSuboptimalOrdering(FunctionDeclaration func, List<string> effectOrder)
            {
                // Check for common anti-patterns
                for (int i = 0; i < effectOrder.Count - 1; i++)
                {
                    var current = effectOrder[i];
                    var next = effectOrder[i + 1];

                    // Database operations followed by logging (should log first for better error tracking)
                    if (current == "Database" && next == "Logging")
                    {
                        var location = GetLocation(func.Name);
                        _violations.Add(("Consider logging before database operations for better error tracking", location));
                    }

                    // Network operations followed by file operations (potential resource contention)
                    if (current == "Network" && next == "FileSystem")
                    {
                        var location = GetLocation(func.Name);
                        _violations.Add(("Network operations followed by file operations may cause resource contention", location));
                    }
                }
            }

            public IEnumerable<AnalysisDiagnostic> GetDiagnostics(LintRule rule)
            {
                return _violations.Select(v => rule.CreateDiagnostic(
                    v.issue,
                    v.location,
                    "Consider optimizing effect usage patterns for better performance"
                ));
            }
        }
    }

    /// <summary>
    /// Rule: Optimize module imports
    /// </summary>
    public class ModuleImportOptimizationRule : LintRule
    {
        public override string RuleId => "module-import-optimization";
        public override string Description => "Optimize module imports for better performance";
        public override string Category => AnalysisCategories.Performance;

        public override IEnumerable<AnalysisDiagnostic> Analyze(ProgramNode ast, string filePath, string sourceText)
        {
            var visitor = new ModuleImportOptimizationVisitor(filePath, sourceText);
            visitor.Visit(ast);
            return visitor.GetDiagnostics(this);
        }

        private class ModuleImportOptimizationVisitor : ASTVisitor
        {
            private readonly Dictionary<string, ImportStatement> _imports = new();
            private readonly HashSet<string> _usedFunctions = new();
            private readonly List<(string issue, SourceLocation location)> _violations = new();

            public ModuleImportOptimizationVisitor(string filePath, string sourceText) : base(filePath, sourceText) { }

            public override void VisitProgram(ProgramNode program)
            {
                // First pass: collect imports
                foreach (var stmt in program.Statements)
                {
                    if (stmt is ImportStatement import)
                    {
                        _imports[import.ModuleName] = import;
                    }
                }

                // Second pass: collect usage
                base.VisitProgram(program);

                // Analyze import patterns
                AnalyzeImportPatterns();
            }

            public override void VisitFunctionDeclaration(FunctionDeclaration func)
            {
                CollectFunctionUsage(func.Body);
                base.VisitFunctionDeclaration(func);
            }

            private void CollectFunctionUsage(List<ASTNode> body)
            {
                foreach (var stmt in body)
                {
                    CollectUsageFromStatement(stmt);
                }
            }

            private void CollectUsageFromStatement(ASTNode node)
            {
                switch (node)
                {
                    case CallExpression call:
                        _usedFunctions.Add(call.Name);
                        break;

                    case MemberAccessExpression qualified:
                        if (qualified.Object is Identifier obj)
                        {
                            _usedFunctions.Add($"{obj.Name}.{qualified.Member}");
                        }
                        break;

                    case LetStatement let:
                        CollectUsageFromStatement(let.Expression);
                        break;

                    case IfStatement ifStmt:
                        CollectUsageFromStatement(ifStmt.Condition);
                        CollectFunctionUsage(ifStmt.ThenBody);
                        if (ifStmt.ElseBody != null)
                            CollectFunctionUsage(ifStmt.ElseBody);
                        break;

                    case ReturnStatement ret:
                        CollectUsageFromStatement(ret.Expression);
                        break;
                }
            }

            private void AnalyzeImportPatterns()
            {
                foreach (var kvp in _imports)
                {
                    var import = kvp.Value;

                    if (import.IsWildcard)
                    {
                        CheckWildcardImport(import);
                    }
                    else if (import.SpecificImports != null)
                    {
                        CheckSpecificImports(import);
                    }
                    else
                    {
                        CheckModuleImport(import);
                    }
                }
            }

            private void CheckWildcardImport(ImportStatement import)
            {
                var location = GetLocation($"import {import.ModuleName}.*");
                _violations.Add(("Wildcard imports can impact performance and readability. Consider specific imports", location));
            }

            private void CheckSpecificImports(ImportStatement import)
            {
                if (import.SpecificImports != null && import.SpecificImports.Count > 5)
                {
                    var location = GetLocation($"import {import.ModuleName}");
                    _violations.Add(($"Importing many functions ({import.SpecificImports.Count}) from module. Consider full module import", location));
                }
            }

            private void CheckModuleImport(ImportStatement import)
            {
                // Check if only one function is used from the module
                var moduleFunctions = _usedFunctions.Where(f => f.StartsWith(import.ModuleName + ".")).ToList();
                if (moduleFunctions.Count == 1)
                {
                    var location = GetLocation($"import {import.ModuleName}");
                    var functionName = moduleFunctions[0].Split('.')[1];
                    _violations.Add(($"Only using one function from module. Consider specific import: import {import.ModuleName}.{{{functionName}}}", location));
                }
            }

            public IEnumerable<AnalysisDiagnostic> GetDiagnostics(LintRule rule)
            {
                return _violations.Select(v => rule.CreateDiagnostic(
                    v.issue,
                    v.location,
                    "Optimize imports for better compilation and runtime performance"
                ));
            }
        }
    }

    /// <summary>
    /// Rule: Detect unnecessary error propagation
    /// </summary>
    public class UnnecessaryErrorPropagationRule : LintRule
    {
        public override string RuleId => "unnecessary-error-propagation";
        public override string Description => "Detect cases where error propagation could be optimized";
        public override string Category => AnalysisCategories.Performance;

        public override IEnumerable<AnalysisDiagnostic> Analyze(ProgramNode ast, string filePath, string sourceText)
        {
            var visitor = new UnnecessaryErrorPropagationVisitor(filePath, sourceText);
            visitor.Visit(ast);
            return visitor.GetDiagnostics(this);
        }

        private class UnnecessaryErrorPropagationVisitor : ASTVisitor
        {
            private readonly List<(string issue, SourceLocation location)> _violations = new();

            public UnnecessaryErrorPropagationVisitor(string filePath, string sourceText) : base(filePath, sourceText) { }

            public override void VisitFunctionDeclaration(FunctionDeclaration func)
            {
                CheckForUnnecessaryPropagation(func);
                base.VisitFunctionDeclaration(func);
            }

            private void CheckForUnnecessaryPropagation(FunctionDeclaration func)
            {
                for (int i = 0; i < func.Body.Count; i++)
                {
                    var stmt = func.Body[i];

                    // Check for immediately returned propagated results
                    if (stmt is LetStatement let && let.Expression is ErrorPropagation)
                    {
                        // Check if this is the last statement or followed by return of the variable
                        if (i == func.Body.Count - 2 && func.Body[i + 1] is ReturnStatement ret)
                        {
                            if (ret.Expression is Identifier id && id.Name == let.Name)
                            {
                                var location = GetLocation(let.Name);
                                _violations.Add(("Unnecessary let binding for error propagation. Consider direct return with ?", location));
                            }
                        }
                    }

                    // Check for redundant error propagation in simple cases
                    CheckForRedundantPropagation(stmt);
                }
            }

            private void CheckForRedundantPropagation(ASTNode node)
            {
                switch (node)
                {
                    case ReturnStatement ret when ret.Expression is ErrorPropagation prop:
                        if (prop.Expression is CallExpression call && IsSimpleFunction(call))
                        {
                            var location = GetLocation("return");
                            _violations.Add(("Consider handling the result explicitly instead of direct propagation for simple functions", location));
                        }
                        break;
                }
            }

            private bool IsSimpleFunction(CallExpression call)
            {
                // Heuristic: functions with no arguments or simple names might be simple
                return call.Arguments.Count == 0 || call.Name.Length < 10;
            }

            public IEnumerable<AnalysisDiagnostic> GetDiagnostics(LintRule rule)
            {
                return _violations.Select(v => rule.CreateDiagnostic(
                    v.issue,
                    v.location,
                    "Consider optimizing error propagation patterns for better performance"
                ));
            }
        }
    }

    /// <summary>
    /// Get all performance analyzer rules
    /// </summary>
    public static IEnumerable<LintRule> GetRules()
    {
        yield return new StringConcatenationRule();
        yield return new InefficientEffectPatternsRule();
        yield return new ModuleImportOptimizationRule();
        yield return new UnnecessaryErrorPropagationRule();
    }
}