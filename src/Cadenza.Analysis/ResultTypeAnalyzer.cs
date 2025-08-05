using System;
using System.Collections.Generic;
using System.Linq;
using Cadenza.Core;

namespace Cadenza.Analysis;

/// <summary>
/// Analyzer for Cadenza Result type usage and error handling patterns
/// </summary>
public class ResultTypeAnalyzer
{
    /// <summary>
    /// Rule: All Result<T,E> values must be handled with ? or explicit match
    /// </summary>
    public class UnusedResultsRule : LintRule
    {
        public override string RuleId => "unused-results";
        public override string Description => "All Result values must be handled or propagated";
        public override string Category => AnalysisCategories.ResultTypes;

        public override IEnumerable<AnalysisDiagnostic> Analyze(ProgramNode ast, string filePath, string sourceText)
        {
            var visitor = new UnusedResultVisitor(filePath, sourceText);
            visitor.Visit(ast);
            return visitor.GetDiagnostics(this);
        }

        private class UnusedResultVisitor : ASTVisitor
        {
            private readonly List<(CallExpression call, SourceLocation location)> _violations = new();
            private readonly Dictionary<string, string> _functionReturnTypes = new();

            public UnusedResultVisitor(string filePath, string sourceText) : base(filePath, sourceText) { }

            public override void VisitProgram(ProgramNode program)
            {
                // First pass: collect function return types
                foreach (var stmt in program.Statements)
                {
                    if (stmt is FunctionDeclaration func)
                    {
                        _functionReturnTypes[func.Name] = func.ReturnType;
                    }
                    else if (stmt is ModuleDeclaration module)
                    {
                        foreach (var moduleStmt in module.Body)
                        {
                            if (moduleStmt is FunctionDeclaration moduleFunc)
                            {
                                _functionReturnTypes[$"{module.Name}.{moduleFunc.Name}"] = moduleFunc.ReturnType;
                            }
                        }
                    }
                }

                // Second pass: check for unused results
                base.VisitProgram(program);
            }

            public override void VisitFunctionDeclaration(FunctionDeclaration func)
            {
                CheckBodyForUnusedResults(func.Body);
                base.VisitFunctionDeclaration(func);
            }

            private void CheckBodyForUnusedResults(List<ASTNode> body)
            {
                foreach (var stmt in body)
                {
                    CheckStatementForUnusedResults(stmt);
                }
            }

            private void CheckStatementForUnusedResults(ASTNode node)
            {
                switch (node)
                {
                    case CallExpression call:
                        // Check if this function call returns a Result type and is not handled
                        if (IsResultReturningFunction(call) && !IsResultHandled(call))
                        {
                            var location = GetLocation(call.Name);
                            _violations.Add((call, location));
                        }
                        break;

                    case LetStatement let:
                        // Let statements automatically handle results, so this is OK
                        CheckExpressionForUnusedResults(let.Expression);
                        break;

                    case IfStatement ifStmt:
                        CheckExpressionForUnusedResults(ifStmt.Condition);
                        CheckBodyForUnusedResults(ifStmt.ThenBody);
                        if (ifStmt.ElseBody != null)
                            CheckBodyForUnusedResults(ifStmt.ElseBody);
                        break;

                    case ReturnStatement ret:
                        CheckExpressionForUnusedResults(ret.Expression);
                        break;

                    case GuardStatement guard:
                        CheckExpressionForUnusedResults(guard.Condition);
                        CheckBodyForUnusedResults(guard.ElseBody);
                        break;
                }
            }

            private void CheckExpressionForUnusedResults(ASTNode expression)
            {
                switch (expression)
                {
                    case CallExpression call:
                        if (IsResultReturningFunction(call))
                        {
                            var location = GetLocation(call.Name);
                            _violations.Add((call, location));
                        }
                        break;

                    case BinaryExpression binary:
                        CheckExpressionForUnusedResults(binary.Left);
                        CheckExpressionForUnusedResults(binary.Right);
                        break;

                    case ErrorPropagation:
                        // Error propagation properly handles results
                        break;
                }
            }

            private bool IsResultReturningFunction(CallExpression call)
            {
                return _functionReturnTypes.TryGetValue(call.Name, out var returnType) && 
                       returnType.StartsWith("Result<");
            }

            private bool IsResultHandled(CallExpression call)
            {
                // For now, assume all function calls in expressions are unhandled
                // In a complete implementation, we'd check the parent context
                return false;
            }

            public IEnumerable<AnalysisDiagnostic> GetDiagnostics(LintRule rule)
            {
                return _violations.Select(v => rule.CreateDiagnostic(
                    $"Result value from '{v.call.Name}' is ignored and must be handled",
                    v.location,
                    "Use 'let result = function()?' for error propagation or handle the Result explicitly"
                ));
            }
        }
    }

    /// <summary>
    /// Rule: Functions returning Result must handle all error paths
    /// </summary>
    public class ErrorHandlingRule : LintRule
    {
        public override string RuleId => "error-handling";
        public override string Description => "Functions returning Result must handle all error paths properly";
        public override string Category => AnalysisCategories.ResultTypes;

        public override IEnumerable<AnalysisDiagnostic> Analyze(ProgramNode ast, string filePath, string sourceText)
        {
            var visitor = new ErrorHandlingVisitor(filePath, sourceText);
            visitor.Visit(ast);
            return visitor.GetDiagnostics(this);
        }

        private class ErrorHandlingVisitor : ASTVisitor
        {
            private readonly List<(FunctionDeclaration func, string issue, SourceLocation location)> _violations = new();

            public ErrorHandlingVisitor(string filePath, string sourceText) : base(filePath, sourceText) { }

            public override void VisitFunctionDeclaration(FunctionDeclaration func)
            {
                if (func.ReturnType.StartsWith("Result<"))
                {
                    CheckResultFunctionImplementation(func);
                }
                base.VisitFunctionDeclaration(func);
            }

            private void CheckResultFunctionImplementation(FunctionDeclaration func)
            {
                var hasOkReturn = false;
                var hasErrorReturn = false;
                var hasErrorPropagation = false;

                AnalyzeBodyForResultUsage(func.Body, ref hasOkReturn, ref hasErrorReturn, ref hasErrorPropagation);

                var location = GetLocation(func.Name);

                if (!hasOkReturn && !hasErrorReturn)
                {
                    _violations.Add((func, "Function returning Result must have at least one Ok() or Error() return", location));
                }

                if (hasErrorPropagation && !hasErrorReturn)
                {
                    _violations.Add((func, "Function using error propagation (?) should handle propagated errors", location));
                }
            }

            private void AnalyzeBodyForResultUsage(List<ASTNode> body, ref bool hasOkReturn, ref bool hasErrorReturn, ref bool hasErrorPropagation)
            {
                foreach (var stmt in body)
                {
                    AnalyzeStatementForResultUsage(stmt, ref hasOkReturn, ref hasErrorReturn, ref hasErrorPropagation);
                }
            }

            private void AnalyzeStatementForResultUsage(ASTNode node, ref bool hasOkReturn, ref bool hasErrorReturn, ref bool hasErrorPropagation)
            {
                switch (node)
                {
                    case ReturnStatement ret:
                        AnalyzeReturnForResultUsage(ret.Expression, ref hasOkReturn, ref hasErrorReturn);
                        AnalyzeExpressionForErrorPropagation(ret.Expression, ref hasErrorPropagation);
                        break;

                    case LetStatement let:
                        AnalyzeExpressionForErrorPropagation(let.Expression, ref hasErrorPropagation);
                        break;

                    case IfStatement ifStmt:
                        AnalyzeBodyForResultUsage(ifStmt.ThenBody, ref hasOkReturn, ref hasErrorReturn, ref hasErrorPropagation);
                        if (ifStmt.ElseBody != null)
                            AnalyzeBodyForResultUsage(ifStmt.ElseBody, ref hasOkReturn, ref hasErrorReturn, ref hasErrorPropagation);
                        break;

                    case GuardStatement guard:
                        AnalyzeBodyForResultUsage(guard.ElseBody, ref hasOkReturn, ref hasErrorReturn, ref hasErrorPropagation);
                        break;
                }
            }

            private void AnalyzeReturnForResultUsage(ASTNode expression, ref bool hasOkReturn, ref bool hasErrorReturn)
            {
                switch (expression)
                {
                    case ResultExpression ok when ok.Type == "Ok":
                        hasOkReturn = true;
                        break;
                    case ResultExpression error when error.Type == "Error":
                        hasErrorReturn = true;
                        break;
                }
            }

            private void AnalyzeExpressionForErrorPropagation(ASTNode expression, ref bool hasErrorPropagation)
            {
                switch (expression)
                {
                    case ErrorPropagation:
                        hasErrorPropagation = true;
                        break;
                    case BinaryExpression binary:
                        AnalyzeExpressionForErrorPropagation(binary.Left, ref hasErrorPropagation);
                        AnalyzeExpressionForErrorPropagation(binary.Right, ref hasErrorPropagation);
                        break;
                }
            }

            public IEnumerable<AnalysisDiagnostic> GetDiagnostics(LintRule rule)
            {
                return _violations.Select(v => rule.CreateDiagnostic(
                    v.issue,
                    v.location,
                    "Ensure all code paths return either Ok() or Error(), and handle error propagation properly"
                ));
            }
        }
    }

    /// <summary>
    /// Rule: Validate error propagation usage
    /// </summary>
    public class ErrorPropagationValidationRule : LintRule
    {
        public override string RuleId => "error-propagation-validation";
        public override string Description => "Error propagation operator (?) must be used correctly";
        public override string Category => AnalysisCategories.ResultTypes;

        public override IEnumerable<AnalysisDiagnostic> Analyze(ProgramNode ast, string filePath, string sourceText)
        {
            var visitor = new ErrorPropagationValidator(filePath, sourceText);
            visitor.Visit(ast);
            return visitor.GetDiagnostics(this);
        }

        private class ErrorPropagationValidator : ASTVisitor
        {
            private readonly Dictionary<string, string> _functionReturnTypes = new();
            private readonly List<(string issue, SourceLocation location)> _violations = new();
            private FunctionDeclaration? _currentFunction;

            public ErrorPropagationValidator(string filePath, string sourceText) : base(filePath, sourceText) { }

            public override void VisitProgram(ProgramNode program)
            {
                // Collect function return types
                foreach (var stmt in program.Statements)
                {
                    if (stmt is FunctionDeclaration func)
                    {
                        _functionReturnTypes[func.Name] = func.ReturnType;
                    }
                }
                base.VisitProgram(program);
            }

            public override void VisitFunctionDeclaration(FunctionDeclaration func)
            {
                _currentFunction = func;
                CheckErrorPropagationUsage(func.Body);
                base.VisitFunctionDeclaration(func);
                _currentFunction = null;
            }

            private void CheckErrorPropagationUsage(List<ASTNode> body)
            {
                foreach (var stmt in body)
                {
                    CheckStatementForErrorPropagation(stmt);
                }
            }

            private void CheckStatementForErrorPropagation(ASTNode node)
            {
                switch (node)
                {
                    case LetStatement let when let.Expression is ErrorPropagation prop:
                        ValidateErrorPropagation(prop);
                        break;

                    case ReturnStatement ret when ret.Expression is ErrorPropagation prop:
                        ValidateErrorPropagation(prop);
                        break;

                    case IfStatement ifStmt:
                        CheckStatementForErrorPropagation(ifStmt.Condition);
                        CheckErrorPropagationUsage(ifStmt.ThenBody);
                        if (ifStmt.ElseBody != null)
                            CheckErrorPropagationUsage(ifStmt.ElseBody);
                        break;

                    case GuardStatement guard:
                        CheckStatementForErrorPropagation(guard.Condition);
                        CheckErrorPropagationUsage(guard.ElseBody);
                        break;
                }
            }

            private void ValidateErrorPropagation(ErrorPropagation prop)
            {
                // Check if the current function can propagate errors
                if (_currentFunction == null)
                    return;

                if (!_currentFunction.ReturnType.StartsWith("Result<"))
                {
                    var location = GetLocation("?");
                    _violations.Add(($"Cannot use error propagation (?) in function '{_currentFunction.Name}' that doesn't return Result type", location));
                }

                // Check if the expression being propagated actually returns a Result
                if (prop.Expression is CallExpression call)
                {
                    if (_functionReturnTypes.TryGetValue(call.Name, out var returnType) && !returnType.StartsWith("Result<"))
                    {
                        var location = GetLocation(call.Name);
                        _violations.Add(($"Cannot use error propagation (?) on '{call.Name}' which doesn't return Result type", location));
                    }
                }
            }

            public IEnumerable<AnalysisDiagnostic> GetDiagnostics(LintRule rule)
            {
                return _violations.Select(v => rule.CreateDiagnostic(
                    v.issue,
                    v.location,
                    "Use error propagation only with Result-returning functions in Result-returning contexts"
                ));
            }
        }
    }

    /// <summary>
    /// Rule: Detect unreachable error conditions
    /// </summary>
    public class DeadErrorPathsRule : LintRule
    {
        public override string RuleId => "dead-error-paths";
        public override string Description => "Detect unreachable error conditions in Result handling";
        public override string Category => AnalysisCategories.ResultTypes;

        public override IEnumerable<AnalysisDiagnostic> Analyze(ProgramNode ast, string filePath, string sourceText)
        {
            var visitor = new DeadErrorPathVisitor(filePath, sourceText);
            visitor.Visit(ast);
            return visitor.GetDiagnostics(this);
        }

        private class DeadErrorPathVisitor : ASTVisitor
        {
            private readonly List<(string issue, SourceLocation location)> _violations = new();

            public DeadErrorPathVisitor(string filePath, string sourceText) : base(filePath, sourceText) { }

            public override void VisitFunctionDeclaration(FunctionDeclaration func)
            {
                if (func.ReturnType.StartsWith("Result<"))
                {
                    AnalyzeForDeadErrorPaths(func);
                }
                base.VisitFunctionDeclaration(func);
            }

            private void AnalyzeForDeadErrorPaths(FunctionDeclaration func)
            {
                // Look for patterns like:
                // if (condition) return Ok(value);
                // return Error("unreachable");

                var statements = func.Body;
                for (int i = 0; i < statements.Count - 1; i++)
                {
                    var current = statements[i];
                    var next = statements[i + 1];

                    if (IsUnconditionalOkReturn(current) && IsErrorReturn(next))
                    {
                        var location = GetLocation("Error");
                        _violations.Add(($"Unreachable error return in function '{func.Name}' - previous statement always returns Ok", location));
                    }
                }

                // Check for impossible error conditions
                CheckForImpossibleErrorConditions(func);
            }

            private void CheckForImpossibleErrorConditions(FunctionDeclaration func)
            {
                foreach (var stmt in func.Body)
                {
                    if (stmt is IfStatement ifStmt)
                    {
                        AnalyzeIfStatementForImpossibleErrors(ifStmt);
                    }
                }
            }

            private void AnalyzeIfStatementForImpossibleErrors(IfStatement ifStmt)
            {
                // Look for patterns like: if (x > 0) { return Error("x is negative"); }
                if (ifStmt.Condition is BinaryExpression binary && 
                    ifStmt.ThenBody.Count == 1 && 
                    IsErrorReturn(ifStmt.ThenBody[0]))
                {
                    var errorStmt = ifStmt.ThenBody[0] as ReturnStatement;
                    if (errorStmt?.Expression is ResultExpression error && error.Type == "Error" &&
                        error.Value is StringLiteral errorMsg)
                    {
                        // Simple heuristic: check for contradictory conditions
                        if (IsContradictoryCondition(binary, errorMsg.Value))
                        {
                            var location = GetLocation("Error");
                            _violations.Add(("Contradictory error condition - the error message doesn't match the condition", location));
                        }
                    }
                }
            }

            private bool IsContradictoryCondition(BinaryExpression condition, string errorMessage)
            {
                // Simple pattern matching for common contradictions
                var conditionStr = $"{condition.Left} {condition.Operator} {condition.Right}";
                var message = errorMessage.ToLower();

                // Example: if (x > 0) return Error("negative value")
                if (condition.Operator == ">" && message.Contains("negative"))
                    return true;

                if (condition.Operator == "<" && message.Contains("positive"))
                    return true;

                if (condition.Operator == "==" && message.Contains("not equal"))
                    return true;

                return false;
            }

            private bool IsUnconditionalOkReturn(ASTNode statement)
            {
                return statement is ReturnStatement ret && ret.Expression is ResultExpression ok && ok.Type == "Ok";
            }

            private bool IsErrorReturn(ASTNode statement)
            {
                return statement is ReturnStatement ret && ret.Expression is ResultExpression error && error.Type == "Error";
            }

            public IEnumerable<AnalysisDiagnostic> GetDiagnostics(LintRule rule)
            {
                return _violations.Select(v => rule.CreateDiagnostic(
                    v.issue,
                    v.location,
                    "Review error handling logic and remove unreachable code"
                ));
            }
        }
    }

    /// <summary>
    /// Rule: Ensure Result type consistency in function signatures
    /// </summary>
    public class ResultTypeConsistencyRule : LintRule
    {
        public override string RuleId => "result-type-consistency";
        public override string Description => "Ensure consistent Result type usage across function signatures";
        public override string Category => AnalysisCategories.ResultTypes;

        public override IEnumerable<AnalysisDiagnostic> Analyze(ProgramNode ast, string filePath, string sourceText)
        {
            var visitor = new ResultTypeConsistencyVisitor(filePath, sourceText);
            visitor.Visit(ast);
            return visitor.GetDiagnostics(this);
        }

        private class ResultTypeConsistencyVisitor : ASTVisitor
        {
            private readonly List<(string issue, SourceLocation location)> _violations = new();

            public ResultTypeConsistencyVisitor(string filePath, string sourceText) : base(filePath, sourceText) { }

            public override void VisitFunctionDeclaration(FunctionDeclaration func)
            {
                if (func.ReturnType.StartsWith("Result<"))
                {
                    CheckResultTypeConsistency(func);
                }
                base.VisitFunctionDeclaration(func);
            }

            private void CheckResultTypeConsistency(FunctionDeclaration func)
            {
                // Check if function uses error propagation but doesn't handle all paths
                var hasErrorPropagation = HasErrorPropagation(func.Body);
                var hasExplicitErrorHandling = HasExplicitErrorHandling(func.Body);

                if (hasErrorPropagation && !hasExplicitErrorHandling)
                {
                    var location = GetLocation(func.Name);
                    _violations.Add(($"Function '{func.Name}' uses error propagation but lacks explicit error handling", location));
                }

                // Check for mixed error types
                CheckForMixedErrorTypes(func);
            }

            private void CheckForMixedErrorTypes(FunctionDeclaration func)
            {
                var errorTypes = new HashSet<string>();
                CollectErrorTypes(func.Body, errorTypes);

                if (errorTypes.Count > 1)
                {
                    var location = GetLocation(func.Name);
                    _violations.Add(($"Function '{func.Name}' returns multiple error types: {string.Join(", ", errorTypes)}. Consider using a common error type.", location));
                }
            }

            private void CollectErrorTypes(List<ASTNode> body, HashSet<string> errorTypes)
            {
                foreach (var stmt in body)
                {
                    CollectErrorTypesFromStatement(stmt, errorTypes);
                }
            }

            private void CollectErrorTypesFromStatement(ASTNode node, HashSet<string> errorTypes)
            {
                switch (node)
                {
                    case ReturnStatement ret when ret.Expression is ResultExpression error && error.Type == "Error":
                        if (error.Value is StringLiteral str)
                            errorTypes.Add("string");
                        else if (error.Value is NumberLiteral)
                            errorTypes.Add("int");
                        break;

                    case IfStatement ifStmt:
                        CollectErrorTypes(ifStmt.ThenBody, errorTypes);
                        if (ifStmt.ElseBody != null)
                            CollectErrorTypes(ifStmt.ElseBody, errorTypes);
                        break;

                    case GuardStatement guard:
                        CollectErrorTypes(guard.ElseBody, errorTypes);
                        break;
                }
            }

            private bool HasErrorPropagation(List<ASTNode> body)
            {
                return body.Any(stmt => ContainsErrorPropagation(stmt));
            }

            private bool ContainsErrorPropagation(ASTNode node)
            {
                switch (node)
                {
                    case LetStatement let:
                        return let.Expression is ErrorPropagation;
                    case IfStatement ifStmt:
                        return ContainsErrorPropagation(ifStmt.Condition) ||
                               ifStmt.ThenBody.Any(ContainsErrorPropagation) ||
                               (ifStmt.ElseBody?.Any(ContainsErrorPropagation) ?? false);
                    case ReturnStatement ret:
                        return ret.Expression is ErrorPropagation;
                    default:
                        return false;
                }
            }

            private bool HasExplicitErrorHandling(List<ASTNode> body)
            {
                return body.Any(stmt => ContainsExplicitErrorHandling(stmt));
            }

            private bool ContainsExplicitErrorHandling(ASTNode node)
            {
                switch (node)
                {
                    case ReturnStatement ret:
                        return ret.Expression is ResultExpression error && error.Type == "Error";
                    case IfStatement ifStmt:
                        return ifStmt.ThenBody.Any(ContainsExplicitErrorHandling) ||
                               (ifStmt.ElseBody?.Any(ContainsExplicitErrorHandling) ?? false);
                    case GuardStatement guard:
                        return guard.ElseBody.Any(ContainsExplicitErrorHandling);
                    default:
                        return false;
                }
            }

            public IEnumerable<AnalysisDiagnostic> GetDiagnostics(LintRule rule)
            {
                return _violations.Select(v => rule.CreateDiagnostic(
                    v.issue,
                    v.location,
                    "Ensure consistent Result type patterns and error handling"
                ));
            }
        }
    }

    /// <summary>
    /// Get all result type analyzer rules
    /// </summary>
    public static IEnumerable<LintRule> GetRules()
    {
        yield return new UnusedResultsRule();
        yield return new ErrorHandlingRule();
        yield return new ErrorPropagationValidationRule();
        yield return new DeadErrorPathsRule();
        yield return new ResultTypeConsistencyRule();
    }
}