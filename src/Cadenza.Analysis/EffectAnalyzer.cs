using System;
using System.Collections.Generic;
using System.Linq;
using Cadenza.Core;

namespace Cadenza.Analysis;

/// <summary>
/// Analyzer for Cadenza effect system validation and optimization
/// </summary>
public class EffectAnalyzer
{
    /// <summary>
    /// Rule: Pure functions cannot declare or use any effects
    /// </summary>
    public class PureFunctionValidationRule : LintRule
    {
        public override string RuleId => "pure-function-validation";
        public override string Description => "Pure functions cannot declare or use any effects";
        public override string Category => AnalysisCategories.EffectSystem;

        public override IEnumerable<AnalysisDiagnostic> Analyze(ProgramNode ast, string filePath, string sourceText)
        {
            var visitor = new PureFunctionVisitor(filePath, sourceText);
            visitor.Visit(ast);
            return visitor.GetDiagnostics(this);
        }

        private class PureFunctionVisitor : ASTVisitor
        {
            private readonly List<(FunctionDeclaration func, SourceLocation location)> _violations = new();

            public PureFunctionVisitor(string filePath, string sourceText) : base(filePath, sourceText) { }

            public override void VisitFunctionDeclaration(FunctionDeclaration func)
            {
                if (func.IsPure && func.Effects != null && func.Effects.Count > 0)
                {
                    var location = GetLocation(func.Name);
                    _violations.Add((func, location));
                }
                
                base.VisitFunctionDeclaration(func);
            }

            public IEnumerable<AnalysisDiagnostic> GetDiagnostics(LintRule rule)
            {
                return _violations.Select(v => rule.CreateDiagnostic(
                    $"Pure function '{v.func.Name}' cannot declare effects: {string.Join(", ", v.func.Effects ?? new List<string>())}",
                    v.location,
                    $"Remove 'uses [{string.Join(", ", v.func.Effects ?? new List<string>())}]' or remove 'pure' modifier"
                ));
            }
        }
    }

    /// <summary>
    /// Rule: Functions must declare all effects they use
    /// </summary>
    public class EffectCompletenessRule : LintRule
    {
        public override string RuleId => "effect-completeness";
        public override string Description => "Functions must declare all effects they actually use";
        public override string Category => AnalysisCategories.EffectSystem;

        public override IEnumerable<AnalysisDiagnostic> Analyze(ProgramNode ast, string filePath, string sourceText)
        {
            var visitor = new EffectUsageVisitor(filePath, sourceText);
            visitor.Visit(ast);
            return visitor.GetDiagnostics(this);
        }

        private class EffectUsageVisitor : ASTVisitor
        {
            private readonly List<(FunctionDeclaration func, List<string> missingEffects, SourceLocation location)> _violations = new();
            private readonly Dictionary<string, HashSet<string>> _functionEffects = new();

            public EffectUsageVisitor(string filePath, string sourceText) : base(filePath, sourceText) { }

            public override void VisitFunctionDeclaration(FunctionDeclaration func)
            {
                if (func.IsPure)
                {
                    base.VisitFunctionDeclaration(func);
                    return;
                }

                var declaredEffects = func.Effects?.ToHashSet() ?? new HashSet<string>();
                var usedEffects = new HashSet<string>();

                // Analyze function body for effect usage
                AnalyzeEffectUsage(func.Body, usedEffects);

                var missingEffects = usedEffects.Except(declaredEffects).ToList();
                if (missingEffects.Count > 0)
                {
                    var location = GetLocation(func.Name);
                    _violations.Add((func, missingEffects, location));
                }

                _functionEffects[func.Name] = usedEffects;
                base.VisitFunctionDeclaration(func);
            }

            private void AnalyzeEffectUsage(List<ASTNode> body, HashSet<string> usedEffects)
            {
                foreach (var stmt in body)
                {
                    AnalyzeStatementForEffects(stmt, usedEffects);
                }
            }

            private void AnalyzeStatementForEffects(ASTNode node, HashSet<string> usedEffects)
            {
                switch (node)
                {
                    case CallExpression call:
                        // Check if called function has effects
                        if (_functionEffects.TryGetValue(call.Name, out var calleeEffects))
                        {
                            foreach (var effect in calleeEffects)
                            {
                                usedEffects.Add(effect);
                            }
                        }
                        // Analyze known effect-inducing function patterns
                        AnalyzeCallExpressionForEffects(call, usedEffects);
                        break;

                    case LetStatement let:
                        AnalyzeStatementForEffects(let.Expression, usedEffects);
                        break;

                    case IfStatement ifStmt:
                        AnalyzeStatementForEffects(ifStmt.Condition, usedEffects);
                        foreach (var stmt in ifStmt.ThenBody)
                            AnalyzeStatementForEffects(stmt, usedEffects);
                        if (ifStmt.ElseBody != null)
                        {
                            foreach (var stmt in ifStmt.ElseBody)
                                AnalyzeStatementForEffects(stmt, usedEffects);
                        }
                        break;

                    case ReturnStatement ret:
                        AnalyzeStatementForEffects(ret.Expression, usedEffects);
                        break;

                    case ErrorPropagation prop:
                        AnalyzeStatementForEffects(prop.Expression, usedEffects);
                        break;
                }
            }

            private void AnalyzeCallExpressionForEffects(CallExpression call, HashSet<string> usedEffects)
            {
                // Pattern matching for common effect-inducing operations
                var functionName = call.Name.ToLower();
                
                if (functionName.Contains("database") || functionName.Contains("query") || 
                    functionName.Contains("save") || functionName.Contains("load"))
                {
                    usedEffects.Add("Database");
                }
                
                if (functionName.Contains("http") || functionName.Contains("fetch") || 
                    functionName.Contains("request") || functionName.Contains("api"))
                {
                    usedEffects.Add("Network");
                }
                
                if (functionName.Contains("log") || functionName.Contains("print") || 
                    functionName.Contains("debug"))
                {
                    usedEffects.Add("Logging");
                }
                
                if (functionName.Contains("file") || functionName.Contains("read") || 
                    functionName.Contains("write") || functionName.Contains("path"))
                {
                    usedEffects.Add("FileSystem");
                }
                
                if (functionName.Contains("cache") || functionName.Contains("memory") || 
                    functionName.Contains("buffer"))
                {
                    usedEffects.Add("Memory");
                }
                
                if (functionName.Contains("io") || functionName.Contains("stream") || 
                    functionName.Contains("input") || functionName.Contains("output"))
                {
                    usedEffects.Add("IO");
                }
            }

            public IEnumerable<AnalysisDiagnostic> GetDiagnostics(LintRule rule)
            {
                return _violations.Select(v => rule.CreateDiagnostic(
                    $"Function '{v.func.Name}' uses effects {string.Join(", ", v.missingEffects)} but doesn't declare them",
                    v.location,
                    $"Add 'uses [{string.Join(", ", v.missingEffects)}]' to function declaration"
                ));
            }
        }
    }

    /// <summary>
    /// Rule: Warn about declared but unused effects
    /// </summary>
    public class EffectMinimalityRule : LintRule
    {
        public override string RuleId => "effect-minimality";
        public override string Description => "Functions should not declare effects they don't use";
        public override string Category => AnalysisCategories.EffectSystem;

        public override IEnumerable<AnalysisDiagnostic> Analyze(ProgramNode ast, string filePath, string sourceText)
        {
            var visitor = new UnusedEffectVisitor(filePath, sourceText);
            visitor.Visit(ast);
            return visitor.GetDiagnostics(this);
        }

        private class UnusedEffectVisitor : ASTVisitor
        {
            private readonly List<(FunctionDeclaration func, List<string> unusedEffects, SourceLocation location)> _violations = new();

            public UnusedEffectVisitor(string filePath, string sourceText) : base(filePath, sourceText) { }

            public override void VisitFunctionDeclaration(FunctionDeclaration func)
            {
                if (func.Effects == null || func.Effects.Count == 0)
                {
                    base.VisitFunctionDeclaration(func);
                    return;
                }

                var declaredEffects = func.Effects.ToHashSet();
                var usedEffects = new HashSet<string>();

                // Simple analysis - check for obvious effect usage patterns
                AnalyzeBodyForEffectUsage(func.Body, usedEffects);

                var unusedEffects = declaredEffects.Except(usedEffects).ToList();
                if (unusedEffects.Count > 0)
                {
                    var location = GetLocation(func.Name);
                    _violations.Add((func, unusedEffects, location));
                }

                base.VisitFunctionDeclaration(func);
            }

            private void AnalyzeBodyForEffectUsage(List<ASTNode> body, HashSet<string> usedEffects)
            {
                foreach (var stmt in body)
                {
                    AnalyzeNodeForEffectUsage(stmt, usedEffects);
                }
            }

            private void AnalyzeNodeForEffectUsage(ASTNode node, HashSet<string> usedEffects)
            {
                switch (node)
                {
                    case CallExpression call:
                        AnalyzeCallExpressionForEffectUsage(call, usedEffects);
                        break;
                    case LetStatement let:
                        AnalyzeNodeForEffectUsage(let.Expression, usedEffects);
                        break;
                    case IfStatement ifStmt:
                        AnalyzeNodeForEffectUsage(ifStmt.Condition, usedEffects);
                        AnalyzeBodyForEffectUsage(ifStmt.ThenBody, usedEffects);
                        if (ifStmt.ElseBody != null)
                            AnalyzeBodyForEffectUsage(ifStmt.ElseBody, usedEffects);
                        break;
                    case ReturnStatement ret:
                        AnalyzeNodeForEffectUsage(ret.Expression, usedEffects);
                        break;
                }
            }

            private void AnalyzeCallExpressionForEffectUsage(CallExpression call, HashSet<string> usedEffects)
            {
                var name = call.Name.ToLower();
                
                // Pattern matching for effect usage
                if (name.Contains("database") || name.Contains("db") || name.Contains("query")) 
                    usedEffects.Add("Database");
                if (name.Contains("http") || name.Contains("network") || name.Contains("fetch")) 
                    usedEffects.Add("Network");
                if (name.Contains("log") || name.Contains("print")) 
                    usedEffects.Add("Logging");
                if (name.Contains("file") || name.Contains("read") || name.Contains("write")) 
                    usedEffects.Add("FileSystem");
                if (name.Contains("memory") || name.Contains("cache")) 
                    usedEffects.Add("Memory");
                if (name.Contains("io") || name.Contains("input") || name.Contains("output")) 
                    usedEffects.Add("IO");
            }

            public IEnumerable<AnalysisDiagnostic> GetDiagnostics(LintRule rule)
            {
                return _violations.Select(v => rule.CreateDiagnostic(
                    $"Function '{v.func.Name}' declares effects {string.Join(", ", v.unusedEffects)} but doesn't appear to use them",
                    v.location,
                    $"Remove unused effects: {string.Join(", ", v.unusedEffects)}"
                ));
            }
        }
    }

    /// <summary>
    /// Rule: Callers must handle or propagate callee effects
    /// </summary>
    public class EffectPropagationRule : LintRule
    {
        public override string RuleId => "effect-propagation";
        public override string Description => "Callers must declare effects of functions they call";
        public override string Category => AnalysisCategories.EffectSystem;

        public override IEnumerable<AnalysisDiagnostic> Analyze(ProgramNode ast, string filePath, string sourceText)
        {
            var visitor = new EffectPropagationVisitor(filePath, sourceText);
            visitor.Visit(ast);
            return visitor.GetDiagnostics(this);
        }

        private class EffectPropagationVisitor : ASTVisitor
        {
            private readonly Dictionary<string, HashSet<string>> _functionEffects = new();
            private readonly List<(FunctionDeclaration caller, string callee, List<string> missingEffects, SourceLocation location)> _violations = new();

            public EffectPropagationVisitor(string filePath, string sourceText) : base(filePath, sourceText) { }

            public override void VisitProgram(ProgramNode program)
            {
                // First pass: collect all function effect declarations
                foreach (var stmt in program.Statements)
                {
                    if (stmt is FunctionDeclaration func)
                    {
                        var effects = func.Effects?.ToHashSet() ?? new HashSet<string>();
                        _functionEffects[func.Name] = effects;
                    }
                }

                // Second pass: validate effect propagation
                base.VisitProgram(program);
            }

            public override void VisitFunctionDeclaration(FunctionDeclaration func)
            {
                if (func.IsPure)
                {
                    // Pure functions cannot call functions with effects
                    CheckPureCallExpressions(func);
                }
                else
                {
                    // Non-pure functions must declare effects of called functions
                    CheckEffectPropagation(func);
                }

                base.VisitFunctionDeclaration(func);
            }

            private void CheckPureCallExpressions(FunctionDeclaration pureFunc)
            {
                var calls = ExtractCallExpressions(pureFunc.Body);
                foreach (var call in calls)
                {
                    if (_functionEffects.TryGetValue(call.Name, out var calleeEffects) && calleeEffects.Count > 0)
                    {
                        var location = GetLocation(call.Name);
                        _violations.Add((pureFunc, call.Name, calleeEffects.ToList(), location));
                    }
                }
            }

            private void CheckEffectPropagation(FunctionDeclaration func)
            {
                var declaredEffects = func.Effects?.ToHashSet() ?? new HashSet<string>();
                var calls = ExtractCallExpressions(func.Body);

                foreach (var call in calls)
                {
                    if (_functionEffects.TryGetValue(call.Name, out var calleeEffects))
                    {
                        var missingEffects = calleeEffects.Except(declaredEffects).ToList();
                        if (missingEffects.Count > 0)
                        {
                            var location = GetLocation(call.Name);
                            _violations.Add((func, call.Name, missingEffects, location));
                        }
                    }
                }
            }

            private List<CallExpression> ExtractCallExpressions(List<ASTNode> body)
            {
                var calls = new List<CallExpression>();
                foreach (var stmt in body)
                {
                    ExtractCallExpressionsFromNode(stmt, calls);
                }
                return calls;
            }

            private void ExtractCallExpressionsFromNode(ASTNode node, List<CallExpression> calls)
            {
                switch (node)
                {
                    case CallExpression call:
                        calls.Add(call);
                        break;
                    case LetStatement let:
                        ExtractCallExpressionsFromNode(let.Expression, calls);
                        break;
                    case IfStatement ifStmt:
                        ExtractCallExpressionsFromNode(ifStmt.Condition, calls);
                        foreach (var stmt in ifStmt.ThenBody)
                            ExtractCallExpressionsFromNode(stmt, calls);
                        if (ifStmt.ElseBody != null)
                        {
                            foreach (var stmt in ifStmt.ElseBody)
                                ExtractCallExpressionsFromNode(stmt, calls);
                        }
                        break;
                    case ReturnStatement ret:
                        ExtractCallExpressionsFromNode(ret.Expression, calls);
                        break;
                    case ErrorPropagation prop:
                        ExtractCallExpressionsFromNode(prop.Expression, calls);
                        break;
                }
            }

            public IEnumerable<AnalysisDiagnostic> GetDiagnostics(LintRule rule)
            {
                return _violations.Select(v => rule.CreateDiagnostic(
                    v.caller.IsPure 
                        ? $"Pure function '{v.caller.Name}' cannot call function '{v.callee}' which has effects: {string.Join(", ", v.missingEffects)}"
                        : $"Function '{v.caller.Name}' calls '{v.callee}' but doesn't declare required effects: {string.Join(", ", v.missingEffects)}",
                    v.location,
                    v.caller.IsPure 
                        ? $"Remove 'pure' modifier or avoid calling functions with effects"
                        : $"Add effects to function declaration: uses [{string.Join(", ", v.missingEffects)}]"
                ));
            }
        }
    }

    /// <summary>
    /// Get all effect analyzer rules
    /// </summary>
    public static IEnumerable<LintRule> GetRules()
    {
        yield return new PureFunctionValidationRule();
        yield return new EffectCompletenessRule();
        yield return new EffectMinimalityRule();
        yield return new EffectPropagationRule();
    }
}

/// <summary>
/// Base visitor class for AST traversal
/// </summary>
public abstract class ASTVisitor
{
    protected readonly string FilePath;
    protected readonly string SourceText;
    protected readonly string[] Lines;

    protected ASTVisitor(string filePath, string sourceText)
    {
        FilePath = filePath;
        SourceText = sourceText;
        Lines = sourceText.Split('\n');
    }

    public virtual void Visit(ASTNode node)
    {
        switch (node)
        {
            case ProgramNode program:
                VisitProgram(program);
                break;
            case FunctionDeclaration func:
                VisitFunctionDeclaration(func);
                break;
            case ModuleDeclaration module:
                VisitModuleDeclaration(module);
                break;
            default:
                VisitDefault(node);
                break;
        }
    }

    public virtual void VisitProgram(ProgramNode program)
    {
        foreach (var stmt in program.Statements)
        {
            Visit(stmt);
        }
    }

    public virtual void VisitFunctionDeclaration(FunctionDeclaration func)
    {
        foreach (var stmt in func.Body)
        {
            Visit(stmt);
        }
    }

    public virtual void VisitModuleDeclaration(ModuleDeclaration module)
    {
        foreach (var stmt in module.Body)
        {
            Visit(stmt);
        }
    }

    public virtual void VisitDefault(ASTNode node)
    {
        // Override in derived classes for specific node types
    }

    protected SourceLocation GetLocation(string searchText, int startLine = 1)
    {
        for (int i = startLine - 1; i < Lines.Length; i++)
        {
            var index = Lines[i].IndexOf(searchText, StringComparison.Ordinal);
            if (index >= 0)
            {
                return new SourceLocation(FilePath, i + 1, index + 1, searchText.Length, Lines[i]);
            }
        }
        
        // Fallback location
        return new SourceLocation(FilePath, 1, 1, 0, "");
    }
}