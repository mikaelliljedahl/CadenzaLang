using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Cadenza.Core;

namespace Cadenza.Analysis;

/// <summary>
/// Analyzer for general code quality rules in Cadenza
/// </summary>
public class CodeQualityAnalyzer
{
    /// <summary>
    /// Rule: Detect dead code (unused functions, variables, imports)
    /// </summary>
    public class DeadCodeRule : LintRule
    {
        public override string RuleId => "dead-code";
        public override string Description => "Detect unused functions, variables, and imports";
        public override string Category => AnalysisCategories.CodeQuality;

        public override IEnumerable<AnalysisDiagnostic> Analyze(ProgramNode ast, string filePath, string sourceText)
        {
            var visitor = new DeadCodeVisitor(filePath, sourceText);
            visitor.Visit(ast);
            return visitor.GetDiagnostics(this);
        }

        private class DeadCodeVisitor : ASTVisitor
        {
            private readonly Dictionary<string, FunctionDeclaration> _declaredFunctions = new();
            private readonly HashSet<string> _usedFunctions = new();
            private readonly Dictionary<string, ImportStatement> _imports = new();
            private readonly HashSet<string> _usedImports = new();
            private readonly List<(string name, string type, SourceLocation location)> _violations = new();

            public DeadCodeVisitor(string filePath, string sourceText) : base(filePath, sourceText) { }

            public override void VisitProgram(ProgramNode program)
            {
                // First pass: collect declarations and imports
                foreach (var stmt in program.Statements)
                {
                    if (stmt is FunctionDeclaration func)
                    {
                        _declaredFunctions[func.Name] = func;
                    }
                    else if (stmt is ImportStatement import)
                    {
                        _imports[import.ModuleName] = import;
                    }
                    else if (stmt is ModuleDeclaration module)
                    {
                        foreach (var moduleStmt in module.Body)
                        {
                            if (moduleStmt is FunctionDeclaration moduleFunc)
                            {
                                _declaredFunctions[$"{module.Name}.{moduleFunc.Name}"] = moduleFunc;
                            }
                        }
                    }
                }

                // Second pass: find usage
                base.VisitProgram(program);

                // Report unused items
                ReportUnusedItems();
            }

            public override void VisitFunctionDeclaration(FunctionDeclaration func)
            {
                AnalyzeFunctionBodyForUsage(func.Body);
                base.VisitFunctionDeclaration(func);
            }

            private void AnalyzeFunctionBodyForUsage(List<ASTNode> body)
            {
                foreach (var stmt in body)
                {
                    AnalyzeStatementForUsage(stmt);
                }
            }

            private void AnalyzeStatementForUsage(ASTNode node)
            {
                switch (node)
                {
                    case CallExpression call:
                        _usedFunctions.Add(call.Name);
                        break;

                    case MemberAccessExpression qualified:
                        if (qualified.Object is Identifier obj)
                        {
                            _usedImports.Add(obj.Name);
                            _usedFunctions.Add($"{obj.Name}.{qualified.Member}");
                        }
                        break;

                    case LetStatement let:
                        AnalyzeStatementForUsage(let.Expression);
                        break;

                    case IfStatement ifStmt:
                        AnalyzeStatementForUsage(ifStmt.Condition);
                        AnalyzeFunctionBodyForUsage(ifStmt.ThenBody);
                        if (ifStmt.ElseBody != null)
                            AnalyzeFunctionBodyForUsage(ifStmt.ElseBody);
                        break;

                    case ReturnStatement ret:
                        AnalyzeStatementForUsage(ret.Expression);
                        break;

                    case BinaryExpression binary:
                        AnalyzeStatementForUsage(binary.Left);
                        AnalyzeStatementForUsage(binary.Right);
                        break;

                    case ErrorPropagation prop:
                        AnalyzeStatementForUsage(prop.Expression);
                        break;
                }
            }

            private void ReportUnusedItems()
            {
                // Report unused functions (except main)
                foreach (var kvp in _declaredFunctions)
                {
                    if (kvp.Key != "main" && !_usedFunctions.Contains(kvp.Key))
                    {
                        var location = GetLocation(kvp.Value.Name);
                        _violations.Add((kvp.Key, "function", location));
                    }
                }

                // Report unused imports
                foreach (var kvp in _imports)
                {
                    if (!_usedImports.Contains(kvp.Key))
                    {
                        var location = GetLocation(kvp.Value.ModuleName);
                        _violations.Add((kvp.Key, "import", location));
                    }
                }
            }

            public IEnumerable<AnalysisDiagnostic> GetDiagnostics(LintRule rule)
            {
                return _violations.Select(v => rule.CreateDiagnostic(
                    $"Unused {v.type}: '{v.name}'",
                    v.location,
                    $"Remove unused {v.type} or mark with @unused annotation"
                ));
            }
        }
    }

    /// <summary>
    /// Rule: Detect unreachable code after return statements
    /// </summary>
    public class UnreachableCodeRule : LintRule
    {
        public override string RuleId => "unreachable-code";
        public override string Description => "Detect code that can never be executed";
        public override string Category => AnalysisCategories.CodeQuality;

        public override IEnumerable<AnalysisDiagnostic> Analyze(ProgramNode ast, string filePath, string sourceText)
        {
            var visitor = new UnreachableCodeVisitor(filePath, sourceText);
            visitor.Visit(ast);
            return visitor.GetDiagnostics(this);
        }

        private class UnreachableCodeVisitor : ASTVisitor
        {
            private readonly List<(ASTNode statement, SourceLocation location)> _violations = new();

            public UnreachableCodeVisitor(string filePath, string sourceText) : base(filePath, sourceText) { }

            public override void VisitFunctionDeclaration(FunctionDeclaration func)
            {
                CheckForUnreachableCode(func.Body);
                base.VisitFunctionDeclaration(func);
            }

            private void CheckForUnreachableCode(List<ASTNode> statements)
            {
                bool foundReturn = false;
                for (int i = 0; i < statements.Count; i++)
                {
                    var stmt = statements[i];

                    if (foundReturn)
                    {
                        var location = GetLocationFromStatement(stmt);
                        _violations.Add((stmt, location));
                        continue;
                    }

                    if (stmt is ReturnStatement)
                    {
                        foundReturn = true;
                    }
                    else if (stmt is IfStatement ifStmt)
                    {
                        CheckForUnreachableCode(ifStmt.ThenBody);
                        if (ifStmt.ElseBody != null)
                            CheckForUnreachableCode(ifStmt.ElseBody);
                    }
                    else if (stmt is GuardStatement guard)
                    {
                        CheckForUnreachableCode(guard.ElseBody);
                    }
                }
            }

            private SourceLocation GetLocationFromStatement(ASTNode stmt)
            {
                // Simple heuristic to find statement location
                string searchText = stmt switch
                {
                    ReturnStatement => "return",
                    LetStatement let => $"let {((LetStatement)stmt).Name}",
                    IfStatement => "if",
                    _ => stmt.GetType().Name
                };
                
                return GetLocation(searchText);
            }

            public IEnumerable<AnalysisDiagnostic> GetDiagnostics(LintRule rule)
            {
                return _violations.Select(v => rule.CreateDiagnostic(
                    "Unreachable code detected after return statement",
                    v.location,
                    "Remove unreachable code or restructure control flow"
                ));
            }
        }
    }

    /// <summary>
    /// Rule: Check naming conventions
    /// </summary>
    public class NamingConventionRule : LintRule
    {
        public override string RuleId => "naming-convention";
        public override string Description => "Enforce consistent naming conventions";
        public override string Category => AnalysisCategories.CodeQuality;

        public override IEnumerable<AnalysisDiagnostic> Analyze(ProgramNode ast, string filePath, string sourceText)
        {
            var visitor = new NamingConventionVisitor(filePath, sourceText);
            visitor.Visit(ast);
            return visitor.GetDiagnostics(this);
        }

        private class NamingConventionVisitor : ASTVisitor
        {
            private readonly List<(string name, string type, string issue, SourceLocation location)> _violations = new();

            public NamingConventionVisitor(string filePath, string sourceText) : base(filePath, sourceText) { }

            public override void VisitFunctionDeclaration(FunctionDeclaration func)
            {
                CheckFunctionNaming(func);
                CheckParameterNaming(func);
                base.VisitFunctionDeclaration(func);
            }

            public override void VisitModuleDeclaration(ModuleDeclaration module)
            {
                CheckModuleNaming(module);
                base.VisitModuleDeclaration(module);
            }

            private void CheckFunctionNaming(FunctionDeclaration func)
            {
                if (!IsValidFunctionName(func.Name))
                {
                    var location = GetLocation(func.Name);
                    var suggestion = SuggestFunctionName(func.Name);
                    _violations.Add((func.Name, "function", $"Function names should use camelCase. Consider: {suggestion}", location));
                }
            }

            private void CheckParameterNaming(FunctionDeclaration func)
            {
                foreach (var param in func.Parameters)
                {
                    if (!IsValidParameterName(param.Name))
                    {
                        var location = GetLocation(param.Name);
                        var suggestion = SuggestParameterName(param.Name);
                        _violations.Add((param.Name, "parameter", $"Parameter names should use camelCase. Consider: {suggestion}", location));
                    }
                }
            }

            private void CheckModuleNaming(ModuleDeclaration module)
            {
                if (!IsValidModuleName(module.Name))
                {
                    var location = GetLocation(module.Name);
                    var suggestion = SuggestModuleName(module.Name);
                    _violations.Add((module.Name, "module", $"Module names should use PascalCase. Consider: {suggestion}", location));
                }
            }

            private bool IsValidFunctionName(string name)
            {
                return Regex.IsMatch(name, "^[a-z][a-zA-Z0-9_]*$");
            }

            private bool IsValidParameterName(string name)
            {
                return Regex.IsMatch(name, "^[a-z][a-zA-Z0-9_]*$");
            }

            private bool IsValidModuleName(string name)
            {
                return Regex.IsMatch(name, "^[A-Z][a-zA-Z0-9]*$");
            }

            private string SuggestFunctionName(string name)
            {
                if (char.IsUpper(name[0]))
                    return char.ToLower(name[0]) + name.Substring(1);
                return name.Replace("_", "").Replace("-", "");
            }

            private string SuggestParameterName(string name)
            {
                return SuggestFunctionName(name);
            }

            private string SuggestModuleName(string name)
            {
                if (char.IsLower(name[0]))
                    return char.ToUpper(name[0]) + name.Substring(1);
                return name.Replace("_", "").Replace("-", "");
            }

            public IEnumerable<AnalysisDiagnostic> GetDiagnostics(LintRule rule)
            {
                return _violations.Select(v => rule.CreateDiagnostic(
                    v.issue,
                    v.location,
                    $"Rename {v.type} to follow naming conventions"
                ));
            }
        }
    }

    /// <summary>
    /// Rule: Check function complexity (length, parameter count)
    /// </summary>
    public class FunctionComplexityRule : LintRule
    {
        public override string RuleId => "function-complexity";
        public override string Description => "Ensure functions are not too complex";
        public override string Category => AnalysisCategories.CodeQuality;

        public override IEnumerable<AnalysisDiagnostic> Analyze(ProgramNode ast, string filePath, string sourceText)
        {
            var visitor = new FunctionComplexityVisitor(filePath, sourceText, this);
            visitor.Visit(ast);
            return visitor.GetDiagnostics(this);
        }

        private class FunctionComplexityVisitor : ASTVisitor
        {
            private readonly List<(string issue, SourceLocation location)> _violations = new();
            private readonly LintRule _rule;

            public FunctionComplexityVisitor(string filePath, string sourceText, LintRule rule) : base(filePath, sourceText)
            {
                _rule = rule;
            }

            public override void VisitFunctionDeclaration(FunctionDeclaration func)
            {
                CheckFunctionComplexity(func);
                base.VisitFunctionDeclaration(func);
            }

            private void CheckFunctionComplexity(FunctionDeclaration func)
            {
                var maxLines = 50;
                var maxParams = 5;

                // Check parameter count
                if (func.Parameters.Count > maxParams)
                {
                    var location = GetLocation(func.Name);
                    _violations.Add(($"Function '{func.Name}' has {func.Parameters.Count} parameters (max: {maxParams})", location));
                }

                // Check function length (approximate)
                var functionLines = CountFunctionLines(func);
                if (functionLines > maxLines)
                {
                    var location = GetLocation(func.Name);
                    _violations.Add(($"Function '{func.Name}' has {functionLines} statements (max: {maxLines})", location));
                }

                // Check cyclomatic complexity
                var complexity = CalculateCyclomaticComplexity(func.Body);
                if (complexity > 10)
                {
                    var location = GetLocation(func.Name);
                    _violations.Add(($"Function '{func.Name}' has high cyclomatic complexity: {complexity}", location));
                }
            }

            private int CountFunctionLines(FunctionDeclaration func)
            {
                return CountStatements(func.Body);
            }

            private int CountStatements(List<ASTNode> statements)
            {
                int count = 0;
                foreach (var stmt in statements)
                {
                    count++;
                    if (stmt is IfStatement ifStmt)
                    {
                        count += CountStatements(ifStmt.ThenBody);
                        if (ifStmt.ElseBody != null)
                            count += CountStatements(ifStmt.ElseBody);
                    }
                    else if (stmt is GuardStatement guard)
                    {
                        count += CountStatements(guard.ElseBody);
                    }
                }
                return count;
            }

            private int CalculateCyclomaticComplexity(List<ASTNode> statements)
            {
                int complexity = 1; // Base complexity
                foreach (var stmt in statements)
                {
                    complexity += CountDecisionPoints(stmt);
                }
                return complexity;
            }

            private int CountDecisionPoints(ASTNode node)
            {
                switch (node)
                {
                    case IfStatement ifStmt:
                        int count = 1; // The if itself
                        count += CountDecisionPoints(ifStmt.Condition);
                        foreach (var stmt in ifStmt.ThenBody)
                            count += CountDecisionPoints(stmt);
                        if (ifStmt.ElseBody != null)
                        {
                            foreach (var stmt in ifStmt.ElseBody)
                                count += CountDecisionPoints(stmt);
                        }
                        return count;

                    case GuardStatement guard:
                        return 1 + CountDecisionPoints(guard.Condition) + 
                               guard.ElseBody.Sum(CountDecisionPoints);

                    case BinaryExpression binary when binary.Operator == "&&" || binary.Operator == "||":
                        return 1;

                    default:
                        return 0;
                }
            }

            public IEnumerable<AnalysisDiagnostic> GetDiagnostics(LintRule rule)
            {
                return _violations.Select(v => rule.CreateDiagnostic(
                    v.issue,
                    v.location,
                    "Consider breaking down complex functions into smaller, more focused functions"
                ));
            }
        }
    }

    /// <summary>
    /// Rule: Detect unused variables
    /// </summary>
    public class UnusedVariablesRule : LintRule
    {
        public override string RuleId => "unused-variables";
        public override string Description => "Detect variables that are declared but never used";
        public override string Category => AnalysisCategories.CodeQuality;

        public override IEnumerable<AnalysisDiagnostic> Analyze(ProgramNode ast, string filePath, string sourceText)
        {
            var visitor = new UnusedVariablesVisitor(filePath, sourceText);
            visitor.Visit(ast);
            return visitor.GetDiagnostics(this);
        }

        private class UnusedVariablesVisitor : ASTVisitor
        {
            private readonly Dictionary<string, (LetStatement stmt, SourceLocation location)> _declaredVariables = new();
            private readonly HashSet<string> _usedVariables = new();
            private readonly List<(string name, SourceLocation location)> _violations = new();

            public UnusedVariablesVisitor(string filePath, string sourceText) : base(filePath, sourceText) { }

            public override void VisitFunctionDeclaration(FunctionDeclaration func)
            {
                _declaredVariables.Clear();
                _usedVariables.Clear();
                
                AnalyzeFunctionForVariableUsage(func);
                
                // Report unused variables
                foreach (var kvp in _declaredVariables)
                {
                    if (!_usedVariables.Contains(kvp.Key))
                    {
                        _violations.Add((kvp.Key, kvp.Value.location));
                    }
                }
                
                base.VisitFunctionDeclaration(func);
            }

            private void AnalyzeFunctionForVariableUsage(FunctionDeclaration func)
            {
                AnalyzeStatementsForVariables(func.Body);
            }

            private void AnalyzeStatementsForVariables(List<ASTNode> statements)
            {
                foreach (var stmt in statements)
                {
                    AnalyzeStatementForVariables(stmt);
                }
            }

            private void AnalyzeStatementForVariables(ASTNode node)
            {
                switch (node)
                {
                    case LetStatement let:
                        var location = GetLocation(let.Name);
                        _declaredVariables[let.Name] = (let, location);
                        AnalyzeExpressionForVariables(let.Expression);
                        break;

                    case ReturnStatement ret:
                        AnalyzeExpressionForVariables(ret.Expression);
                        break;

                    case IfStatement ifStmt:
                        AnalyzeExpressionForVariables(ifStmt.Condition);
                        AnalyzeStatementsForVariables(ifStmt.ThenBody);
                        if (ifStmt.ElseBody != null)
                            AnalyzeStatementsForVariables(ifStmt.ElseBody);
                        break;

                    case GuardStatement guard:
                        AnalyzeExpressionForVariables(guard.Condition);
                        AnalyzeStatementsForVariables(guard.ElseBody);
                        break;
                }
            }

            private void AnalyzeExpressionForVariables(ASTNode expression)
            {
                switch (expression)
                {
                    case Identifier id:
                        _usedVariables.Add(id.Name);
                        break;

                    case BinaryExpression binary:
                        AnalyzeExpressionForVariables(binary.Left);
                        AnalyzeExpressionForVariables(binary.Right);
                        break;

                    case CallExpression call:
                        foreach (var arg in call.Arguments)
                        {
                            AnalyzeExpressionForVariables(arg);
                        }
                        break;

                    case ErrorPropagation prop:
                        AnalyzeExpressionForVariables(prop.Expression);
                        break;

                    case StringInterpolation interp:
                        foreach (var part in interp.Parts)
                        {
                            AnalyzeExpressionForVariables(part);
                        }
                        break;
                }
            }

            public IEnumerable<AnalysisDiagnostic> GetDiagnostics(LintRule rule)
            {
                return _violations.Select(v => rule.CreateDiagnostic(
                    $"Variable '{v.name}' is declared but never used",
                    v.location,
                    "Remove unused variable or use it in the function"
                ));
            }
        }
    }

    /// <summary>
    /// Get all code quality analyzer rules
    /// </summary>
    public static IEnumerable<LintRule> GetRules()
    {
        yield return new DeadCodeRule();
        yield return new UnreachableCodeRule();
        yield return new NamingConventionRule();
        yield return new FunctionComplexityRule();
        yield return new UnusedVariablesRule();
    }
}