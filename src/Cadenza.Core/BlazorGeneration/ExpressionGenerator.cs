// Cadenza Core Compiler - Expression Generator
// C# expression generation from Cadenza AST

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cadenza.Core;

// =============================================================================
// EXPRESSION GENERATOR
// =============================================================================

/// <summary>
/// Generates C# expressions and statements from Cadenza AST nodes
/// </summary>
public class ExpressionGenerator
{
    private readonly Dictionary<string, string> _stateVariables;
    private readonly StringBuilder _classContent;

    public ExpressionGenerator(Dictionary<string, string> stateVariables, StringBuilder classContent)
    {
        _stateVariables = stateVariables;
        _classContent = classContent;
    }

    /// <summary>
    /// Generates C# expression code from Cadenza AST
    /// </summary>
    public string GenerateExpression(ASTNode expression)
    {
        return expression switch
        {
            Identifier id => MapStateVariableName(id.Name),
            StringLiteral str => $"\"{str.Value}\"",
            NumberLiteral num => num.Value.ToString(),
            BooleanLiteral boolean => boolean.Value.ToString().ToLower(),
            BinaryExpression binary => $"({GenerateExpression(binary.Left)} {GetOperatorSymbol(binary.Operator)} {GenerateExpression(binary.Right)})",
            ArithmeticExpression arithmetic => $"({GenerateExpression(arithmetic.Left)} {GetOperatorSymbol(arithmetic.Operator)} {GenerateExpression(arithmetic.Right)})",
            UnaryExpression unary => $"{GetUnaryOperatorSymbol(unary.Operator)}{GenerateExpression(unary.Operand)}",
            CallExpression call => $"{call.Name}({string.Join(", ", call.Arguments.Select(GenerateExpression))})",
            MethodCallExpression methodCall => $"{GenerateExpression(methodCall.Object)}.{FixMethodCasing(methodCall.Method)}({string.Join(", ", methodCall.Arguments.Select(GenerateExpression))})",
            MemberAccessExpression member => $"{GenerateExpression(member.Object)}.{member.Member}",
            TernaryExpression ternary => $"({GenerateExpression(ternary.Condition)} ? {GenerateExpression(ternary.ThenExpr)} : {GenerateExpression(ternary.ElseExpr)})",
            StringInterpolation interpolation => GenerateStringInterpolation(interpolation),
            _ => expression.ToString() ?? ""
        };
    }

    /// <summary>
    /// Maps state variable names to their private field names
    /// </summary>
    public string MapStateVariableName(string name)
    {
        return _stateVariables.ContainsKey(name) ? $"_{name}" : name;
    }

    /// <summary>
    /// Fixes method name casing for C# conventions
    /// </summary>
    public string FixMethodCasing(string methodName)
    {
        return methodName switch
        {
            "toString" => "ToString",
            "valueOf" => "ValueOf",
            "equals" => "Equals",
            "hashCode" => "GetHashCode",
            _ => methodName
        };
    }

    /// <summary>
    /// Generates string interpolation
    /// </summary>
    public string GenerateStringInterpolation(StringInterpolation interpolation)
    {
        var parts = interpolation.Parts.Select(part =>
            part is StringLiteral str ? str.Value : $"{{{GenerateExpression(part)}}}"
        );
        return $"$\"{string.Join("", parts)}\"";
    }

    /// <summary>
    /// Generates statements for code blocks
    /// </summary>
    public void GenerateStatements(ASTNode statement, string indent)
    {
        switch (statement)
        {
            case CallExpression call when call.Name == "set_state":
                // Handle Cadenza set_state calls
                if (call.Arguments.Count == 2)
                {
                    var stateVar = GenerateExpression(call.Arguments[0]);
                    var newValue = GenerateExpression(call.Arguments[1]);
                    _classContent.AppendLine($"{indent}{stateVar} = {newValue};");
                    _classContent.AppendLine($"{indent}StateHasChanged();");
                }
                break;
                
            case CallExpression call:
                var callExpr = GenerateExpression(call);
                _classContent.AppendLine($"{indent}{callExpr};");
                break;
                
            case IfStatement ifStmt:
                _classContent.AppendLine($"{indent}if ({GenerateExpression(ifStmt.Condition)})");
                _classContent.AppendLine($"{indent}{{");
                foreach (var stmt in ifStmt.ThenBody)
                {
                    GenerateStatements(stmt, indent + "    ");
                }
                _classContent.AppendLine($"{indent}}}");
                
                if (ifStmt.ElseBody != null)
                {
                    _classContent.AppendLine($"{indent}else");
                    _classContent.AppendLine($"{indent}{{");
                    foreach (var stmt in ifStmt.ElseBody)
                    {
                        GenerateStatements(stmt, indent + "    ");
                    }
                    _classContent.AppendLine($"{indent}}}");
                }
                break;
                
            case LetStatement letStmt:
                var letType = letStmt.Type ?? "var";
                var letValue = GenerateExpression(letStmt.Expression);
                _classContent.AppendLine($"{indent}{letType} {letStmt.Name} = {letValue};");
                break;
                
            default:
                // Handle other statement types
                var exprCode = GenerateExpression(statement);
                if (!string.IsNullOrEmpty(exprCode))
                {
                    _classContent.AppendLine($"{indent}{exprCode};");
                }
                break;
        }
    }

    /// <summary>
    /// Gets C# operator symbol from Cadenza operator
    /// </summary>
    public string GetOperatorSymbol(string cadenzaOperator)
    {
        return cadenzaOperator switch
        {
            "+" => "+",
            "-" => "-",
            "*" => "*",
            "/" => "/",
            "==" => "==",
            "!=" => "!=",
            ">" => ">",
            "<" => "<",
            ">=" => ">=",
            "<=" => "<=",
            "&&" => "&&",
            "||" => "||",
            _ => cadenzaOperator
        };
    }

    /// <summary>
    /// Gets C# unary operator symbol
    /// </summary>
    public string GetUnaryOperatorSymbol(string cadenzaOperator)
    {
        return cadenzaOperator switch
        {
            "!" => "!",
            "-" => "-",
            "+" => "+",
            _ => cadenzaOperator
        };
    }
}