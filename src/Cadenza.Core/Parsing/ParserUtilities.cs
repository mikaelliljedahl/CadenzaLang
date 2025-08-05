// Cadenza Core Parser Utilities - Common parsing utilities and helpers
// Handles token management, navigation, error handling and validation

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
    // Utility methods
    private bool Match(params TokenType[] types)
    {
        foreach (var type in types)
        {
            if (Check(type))
            {
                Advance();
                return true;
            }
        }
        return false;
    }

    private bool Check(TokenType type) => !IsAtEnd() && Peek().Type == type;

    private Token Advance() => IsAtEnd() ? Previous() : _tokens[_current++];

    private bool IsAtEnd() => Peek().Type == TokenType.EOF;

    private Token Peek() => _tokens[_current];

    private Token Previous() => _tokens[_current - 1];

    private Token Consume(TokenType type, string message)
    {
        if (Check(type)) return Advance();
        throw new Exception($"{message}. Got '{Peek().Lexeme}' at line {Peek().Line}");
    }
    
    private Token ConsumeType(string message)
    {
        // Accept both identifiers and type keywords
        if (Check(TokenType.Identifier) || Check(TokenType.String_Type) || Check(TokenType.Int) || 
            Check(TokenType.Bool) || Check(TokenType.List) || Check(TokenType.Option))
        {
            return Advance();
        }
        throw new Exception($"{message}. Got '{Peek().Lexeme}' at line {Peek().Line}");
    }

    private string ParseType()
    {
        if (Match(TokenType.Result))
        {
            Consume(TokenType.Less, "Expected '<' after Result");
            var okType = ParseType();
            Consume(TokenType.Comma, "Expected ',' in Result type");
            var errorType = ParseType();
            Consume(TokenType.Greater, "Expected '>' after Result type");
            return $"Result<{okType}, {errorType}>";
        }
        
        if (Match(TokenType.List))
        {
            Consume(TokenType.Less, "Expected '<' after List");
            var elementType = ParseType();
            Consume(TokenType.Greater, "Expected '>' after List type");
            return $"List<{elementType}>";
        }
        
        if (Match(TokenType.Option))
        {
            Consume(TokenType.Less, "Expected '<' after Option");
            var valueType = ParseType();
            Consume(TokenType.Greater, "Expected '>' after Option type");
            return $"Option<{valueType}>";
        }
        
        var token = Advance();
        return token.Lexeme;
    }

    private string ParseTypeName()
    {
        if (Check(TokenType.Identifier))
        {
            return Advance().Lexeme;
        }
        else if (Check(TokenType.String_Type))
        {
            Advance();
            return "string";
        }
        else if (Check(TokenType.Int))
        {
            Advance();
            return "int";
        }
        else if (Check(TokenType.Bool))
        {
            Advance();
            return "bool";
        }
        else
        {
            throw new Exception($"Expected type name. Got '{Peek().Lexeme}' at line {Peek().Line}");
        }
    }

    private List<string> ParseEffectsList()
    {
        var effects = new List<string>();
        
        Consume(TokenType.LeftBracket, "Expected '[' after 'uses'");
        
        do
        {
            // Effect names can be specific token types or identifiers
            var token = Advance();
            string effectName = token.Type switch
            {
                TokenType.Database => "Database",
                TokenType.Network => "Network", 
                TokenType.Logging => "Logging",
                TokenType.FileSystem => "FileSystem",
                TokenType.Memory => "Memory",
                TokenType.IO => "IO",
                TokenType.Identifier => token.Lexeme,
                _ => throw new Exception($"Expected effect name. Got '{token.Lexeme}' at line {token.Line}")
            };
            effects.Add(effectName);
        } while (Match(TokenType.Comma));
        
        Consume(TokenType.RightBracket, "Expected ']' after effects list");
        
        return effects;
    }

    /// <summary>
    /// Checks if a name is a known HTML element that should be treated as UIElement
    /// </summary>
    private bool IsHtmlElement(string name)
    {
        return name switch
        {
            // Standard HTML elements
            "div" or "span" or "p" or "a" or "img" or "h1" or "h2" or "h3" or "h4" or "h5" or "h6" or
            "button" or "input" or "textarea" or "select" or "option" or "label" or "form" or
            "ul" or "ol" or "li" or "table" or "tr" or "td" or "th" or "thead" or "tbody" or "tfoot" or
            "header" or "footer" or "main" or "section" or "article" or "aside" or "nav" or
            "details" or "summary" or "dialog" or "menu" or "menuitem" or
            // Cadenza semantic elements (that map to HTML)
            "container" or "heading" or "text_input" or "text" or "image" or "list" or "list_item" or
            "table_row" or "table_header" or "table_cell" or "main_content" or "card" or
            "form_header" or "form_fields" or "form_field" or "form_actions" => true,
            _ => false
        };
    }
}