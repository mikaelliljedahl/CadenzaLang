// Cadenza Core Expression Parser - All expression parsing methods with proper precedence
// Handles operator precedence, primary expressions, and complex expression parsing

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Cadenza.Core;

public partial class CadenzaParser
{
    private ASTNode ParseExpression()
    {
        return ParseComplexExpression();
    }

    private ASTNode ParseComplexExpression()
    {
        return ParseTernaryExpression();
    }

    private ASTNode ParseTernaryExpression()
    {
        var expr = ParseLogicalOrExpression();
        
        if (Match(TokenType.Question))
        {
            var thenExpr = ParseLogicalOrExpression();
            Consume(TokenType.Colon, "Expected ':' after ternary then expression");
            var elseExpr = ParseTernaryExpression();
            return new TernaryExpression(expr, thenExpr, elseExpr);
        }
        
        return expr;
    }

    private ASTNode ParseLogicalOrExpression()
    {
        var expr = ParseLogicalAndExpression();
        
        while (Match(TokenType.Or))
        {
            var op = Previous().Lexeme;
            var right = ParseLogicalAndExpression();
            expr = new LogicalExpression(expr, op, right);
        }
        
        return expr;
    }

    private ASTNode ParseLogicalAndExpression()
    {
        var expr = ParseEqualityExpression();
        
        while (Match(TokenType.And))
        {
            var op = Previous().Lexeme;
            var right = ParseEqualityExpression();
            expr = new LogicalExpression(expr, op, right);
        }
        
        return expr;
    }

    private ASTNode ParseEqualityExpression()
    {
        var expr = ParseComparisonExpression();
        
        while (Match(TokenType.Equal, TokenType.NotEqual))
        {
            var op = Previous().Lexeme;
            var right = ParseComparisonExpression();
            expr = new ComparisonExpression(expr, op, right);
        }
        
        return expr;
    }

    private ASTNode ParseComparisonExpression()
    {
        var expr = ParseArithmeticExpression();
        
        while (Match(TokenType.Greater, TokenType.GreaterEqual, TokenType.Less, TokenType.LessEqual))
        {
            var op = Previous().Lexeme;
            var right = ParseArithmeticExpression();
            expr = new ComparisonExpression(expr, op, right);
        }
        
        return expr;
    }

    private ASTNode ParseArithmeticExpression()
    {
        var expr = ParseTermExpression();
        
        while (Match(TokenType.Plus, TokenType.Minus))
        {
            var op = Previous().Lexeme;
            var right = ParseTermExpression();
            expr = new ArithmeticExpression(expr, op, right);
        }
        
        return expr;
    }

    private ASTNode ParseTermExpression()
    {
        var expr = ParseUnaryExpression();
        
        while (Match(TokenType.Multiply, TokenType.Divide, TokenType.Modulo))
        {
            var op = Previous().Lexeme;
            var right = ParseUnaryExpression();
            expr = new ArithmeticExpression(expr, op, right);
        }
        
        return expr;
    }

    private ASTNode ParseUnaryExpression()
    {
        if (Match(TokenType.Not, TokenType.Minus))
        {
            var op = Previous().Lexeme;
            var right = ParseUnaryExpression();
            return new UnaryExpression(op, right);
        }
        
        return ParseMemberAccessExpression();
    }

    private ASTNode ParseMemberAccessExpression()
    {
        var expr = ParsePrimaryExpression();
        
        while (true)
        {
            if (Match(TokenType.Dot))
            {
                var member = Consume(TokenType.Identifier, "Expected member name after '.'").Lexeme;
                
                if (Match(TokenType.LeftParen))
                {
                    // Method call
                    var args = new List<ASTNode>();
                    
                    if (!Check(TokenType.RightParen))
                    {
                        do
                        {
                            args.Add(ParseExpression());
                        } while (Match(TokenType.Comma));
                    }
                    
                    Consume(TokenType.RightParen, "Expected ')' after method arguments");
                    expr = new MethodCallExpression(expr, member, args);
                }
                else
                {
                    // Property access
                    expr = new MemberAccessExpression(expr, member);
                }
            }
            else if (Match(TokenType.Question))
            {
                // Error propagation
                expr = new ErrorPropagation(expr);
            }
            else if (Match(TokenType.LeftBracket))
            {
                // List access: list[index]
                var index = ParseExpression();
                Consume(TokenType.RightBracket, "Expected ']' after list index");
                expr = new ListAccessExpression(expr, index);
            }
            else
            {
                break;
            }
        }
        
        return expr;
    }

    private ASTNode ParsePrimaryExpression()
    {
        if (Match(TokenType.Number))
        {
            var value = Previous().Literal;
            if (value is int intValue)
            {
                return new NumberLiteral(intValue);
            }
            else if (value is double doubleValue)
            {
                return new NumberLiteral((int)doubleValue); // For now, convert to int
            }
        }
        
        if (Match(TokenType.String))
        {
            return new StringLiteral(Previous().Literal?.ToString() ?? "");
        }
        
        if (Match(TokenType.Bool))
        {
            var value = Previous().Lexeme;
            return new BooleanLiteral(value == "true");
        }
        
        if (Match(TokenType.StringInterpolation))
        {
            var parts = Previous().Literal as List<object> ?? new List<object>();
            var interpolationParts = new List<ASTNode>();
            
            foreach (var part in parts)
            {
                if (part is string stringPart)
                {
                    interpolationParts.Add(new StringLiteral(stringPart));
                }
                else if (part is Dictionary<string, object> exprPart && exprPart.ContainsKey("IsExpression"))
                {
                    var exprString = exprPart["Value"]?.ToString() ?? "";
                    // Parse the expression string
                    var lexer = new CadenzaLexer(exprString);
                    var tokens = lexer.ScanTokens();
                    var parser = new CadenzaParser(tokens);
                    var expr = parser.ParseExpression();
                    interpolationParts.Add(expr);
                }
            }
            
            return new StringInterpolation(interpolationParts);
        }
        
        if (Match(TokenType.Identifier))
        {
            var name = Previous().Lexeme;
            
            if (Match(TokenType.LeftParen))
            {
                // Function call
                var args = new List<ASTNode>();
                
                if (!Check(TokenType.RightParen))
                {
                    do
                    {
                        args.Add(ParseExpression());
                    } while (Match(TokenType.Comma));
                }
                
                Consume(TokenType.RightParen, "Expected ')' after arguments");
                return new CallExpression(name, args);
            }
            
            return new Identifier(name);
        }
        
        if (Match(TokenType.Ok, TokenType.Error))
        {
            var type = Previous().Lexeme;
            Consume(TokenType.LeftParen, $"Expected '(' after '{type}'");
            var value = ParseExpression();
            Consume(TokenType.RightParen, $"Expected ')' after {type} value");
            return new ResultExpression(type, value);
        }
        
        if (Match(TokenType.Some))
        {
            Consume(TokenType.LeftParen, "Expected '(' after 'Some'");
            var value = ParseExpression();
            Consume(TokenType.RightParen, "Expected ')' after Some value");
            return new OptionExpression("Some", value);
        }
        
        if (Match(TokenType.None))
        {
            return new OptionExpression("None", null);
        }
        
        if (Match(TokenType.Match))
        {
            return ParseMatchExpression();
        }
        
        if (Match(TokenType.LeftBracket))
        {
            // List literal: [1, 2, 3]
            var elements = new List<ASTNode>();
            
            if (!Check(TokenType.RightBracket))
            {
                do
                {
                    elements.Add(ParseExpression());
                } while (Match(TokenType.Comma));
            }
            
            Consume(TokenType.RightBracket, "Expected ']' after list elements");
            return new ListExpression(elements);
        }
        
        if (Match(TokenType.LeftParen))
        {
            var expr = ParseExpression();
            Consume(TokenType.RightParen, "Expected ')' after expression");
            return expr;
        }
        
        throw new Exception($"Unexpected token '{Peek().Lexeme}' at line {Peek().Line}");
    }
    
    private MatchExpression ParseMatchExpression()
    {
        var value = ParseExpression();
        Consume(TokenType.LeftBrace, "Expected '{' after match expression");
        
        var cases = new List<MatchCase>();
        
        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            // Parse pattern like "Ok(x)" or "Error(e)" or "Some(val)" or "None"
            string pattern;
            string? variable = null;
            
            if (Check(TokenType.Ok) || Check(TokenType.Error) || Check(TokenType.Some) || Check(TokenType.None))
            {
                pattern = Advance().Lexeme;
                
                if (Match(TokenType.LeftParen))
                {
                    variable = Consume(TokenType.Identifier, "Expected variable name in pattern").Lexeme;
                    Consume(TokenType.RightParen, "Expected ')' after pattern variable");
                }
            }
            else if (Check(TokenType.Number) || Check(TokenType.String))
            {
                pattern = Advance().Literal?.ToString() ?? "";
            }
            else if (Check(TokenType.Identifier))
            {
                pattern = Advance().Lexeme;
                // Handle wildcard '_' or other identifiers
                if (pattern == "_")
                {
                    // Wildcard pattern
                }
                else
                {
                    // Could be a constructor pattern or variable binding
                    if (Match(TokenType.LeftParen))
                    {
                        variable = Consume(TokenType.Identifier, "Expected variable name in pattern").Lexeme;
                        Consume(TokenType.RightParen, "Expected ')' after pattern variable");
                    }
                }
            }
            else
            {
                throw new Exception($"Expected pattern in match case. Got '{Peek().Lexeme}' at line {Peek().Line}");
            }
            
            Consume(TokenType.Arrow, "Expected '->' after match pattern");
            
            // Parse the case body
            var caseBody = new List<ASTNode>();
            if (Match(TokenType.LeftBrace))
            {
                caseBody = ParseStatements();
                Consume(TokenType.RightBrace, "Expected '}' after match case body");
            }
            else
            {
                // Single expression
                caseBody.Add(ParseExpression());
            }
            
            cases.Add(new MatchCase(pattern, variable, caseBody));
            
            // Optional comma between cases
            Match(TokenType.Comma);
        }
        
        Consume(TokenType.RightBrace, "Expected '}' after match cases");
        return new MatchExpression(value, cases);
    }
}