// Cadenza Core Parser - Main parser orchestration and coordination
// Handles parsing of Cadenza language tokens into AST nodes with high-level parsing flow control

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

// =============================================================================
// PARSER
// =============================================================================

public partial class CadenzaParser
{
    private readonly List<Token> _tokens;
    private int _current = 0;

    public CadenzaParser(List<Token> tokens)
    {
        _tokens = tokens;
    }

    public ProgramNode Parse()
    {
        var statements = new List<ASTNode>();
        
        while (!IsAtEnd())
        {
            var stmt = ParseStatement();
            if (stmt != null)
            {
                statements.Add(stmt);
            }
        }
        
        return new ProgramNode(statements);
    }

    private ASTNode? ParseStatement()
    {
        // Check for specification block first
        var specification = ParseSpecificationBlock();
        
        if (Match(TokenType.Module))
            return ParseModuleDeclaration(specification);
        if (Match(TokenType.Type))
            return ParseTypeDeclaration();
        if (Match(TokenType.Import))
            return ParseImportStatement();
        if (Match(TokenType.Export))
            return ParseExportStatement();
        if (Match(TokenType.Component))
            return ParseComponentDeclaration();
        if (Match(TokenType.AppState))
            return ParseAppStateDeclaration();
        if (Match(TokenType.ApiClient))
            return ParseApiClientDeclaration();
        if (Match(TokenType.Function) || Match(TokenType.Pure))
            return ParseFunctionDeclaration(specification);
        if (Match(TokenType.Return))
            return ParseReturnStatement();
        if (Match(TokenType.If))
            return ParseIfStatement();
        if (Match(TokenType.Let))
            return ParseLetStatement();
        if (Match(TokenType.Guard))
            return ParseGuardStatement();
        if (Match(TokenType.Match))
            return ParseMatchExpression();

        // If we have a specification but no matching declaration, that's an error
        if (specification != null)
        {
            throw new Exception($"Specification block found but no function or module declaration follows at line {Peek().Line}");
        }

        // Expression statement
        var expr = ParseExpression();
        if (Match(TokenType.Semicolon)) {} // Optional semicolon
        return expr;
    }

    private ReturnStatement ParseReturnStatement()
    {
        ASTNode? expression = null;
        if (!Check(TokenType.Semicolon) && !Check(TokenType.RightBrace))
        {
            expression = ParseExpression();
        }
        if (Match(TokenType.Semicolon)) {} // Optional semicolon
        return new ReturnStatement(expression);
    }

    private IfStatement ParseIfStatement()
    {
        var condition = ParseExpression();
        
        Consume(TokenType.LeftBrace, "Expected '{' after if condition");
        var thenBody = ParseStatements();
        Consume(TokenType.RightBrace, "Expected '}' after if body");
        
        List<ASTNode>? elseBody = null;
        if (Match(TokenType.Else))
        {
            // Handle else if
            if (Match(TokenType.If))
            {
                // Parse the else if as a nested if statement
                var elseIfCondition = ParseExpression();
                
                Consume(TokenType.LeftBrace, "Expected '{' after else if condition");
                var elseIfThenBody = ParseStatements();
                Consume(TokenType.RightBrace, "Expected '}' after else if body");
                
                List<ASTNode>? elseIfElseBody = null;
                if (Match(TokenType.Else))
                {
                    // Handle nested else if or else
                    if (Match(TokenType.If))
                    {
                        // Recursively parse more else if statements
                        var nestedElseIfCondition = ParseExpression();
                        
                        Consume(TokenType.LeftBrace, "Expected '{' after nested else if condition");
                        var nestedElseIfThenBody = ParseStatements();
                        Consume(TokenType.RightBrace, "Expected '}' after nested else if body");
                        
                        var nestedElseIf = new IfStatement(nestedElseIfCondition, nestedElseIfThenBody, null);
                        elseIfElseBody = new List<ASTNode> { nestedElseIf };
                    }
                    else
                    {
                        Consume(TokenType.LeftBrace, "Expected '{' after else");
                        elseIfElseBody = ParseStatements();
                        Consume(TokenType.RightBrace, "Expected '}' after else body");
                    }
                }
                
                var elseIfStatement = new IfStatement(elseIfCondition, elseIfThenBody, elseIfElseBody);
                elseBody = new List<ASTNode> { elseIfStatement };
            }
            else
            {
                Consume(TokenType.LeftBrace, "Expected '{' after else");
                elseBody = ParseStatements();
                Consume(TokenType.RightBrace, "Expected '}' after else body");
            }
        }
        
        return new IfStatement(condition, thenBody, elseBody);
    }

    private LetStatement ParseLetStatement()
    {
        var name = Consume(TokenType.Identifier, "Expected variable name").Lexeme;
        
        string? type = null;
        if (Match(TokenType.Colon))
        {
            type = ParseType();
        }
        
        Consume(TokenType.Assign, "Expected '=' after variable declaration");
        var expression = ParseExpression();
        
        if (Match(TokenType.Semicolon)) {} // Optional semicolon
        
        return new LetStatement(name, type, expression);
    }

    private GuardStatement ParseGuardStatement()
    {
        var condition = ParseExpression();
        
        List<ASTNode>? elseBody = null;
        if (Match(TokenType.Else))
        {
            Consume(TokenType.LeftBrace, "Expected '{' after 'else' in guard statement");
            elseBody = ParseStatements();
            Consume(TokenType.RightBrace, "Expected '}' to close guard else block");
        }
        
        if (Match(TokenType.Semicolon)) {} // Optional semicolon
        
        return new GuardStatement(condition, elseBody);
    }

    private List<ASTNode> ParseStatements()
    {
        var statements = new List<ASTNode>();
        
        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            var stmt = ParseStatement();
            if (stmt != null)
            {
                statements.Add(stmt);
            }
        }
        
        return statements;
    }
}