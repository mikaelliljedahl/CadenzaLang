// Cadenza Core Declaration Parser - Top-level declaration parsing
// Handles function/module declarations, type system, import/export statements, API clients and app state

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
    private ModuleDeclaration ParseModuleDeclaration(SpecificationBlock? specification = null)
    {
        var name = Consume(TokenType.Identifier, "Expected module name").Lexeme;
        Consume(TokenType.LeftBrace, "Expected '{' after module name");
        
        var body = new List<ASTNode>();
        var exports = new List<string>();
        
        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            var stmt = ParseStatement();
            if (stmt != null)
            {
                body.Add(stmt);
                
                // Check if this is an exported function
                if (stmt is FunctionDeclaration func && func.IsExported)
                {
                    exports.Add(func.Name);
                }
            }
        }
        
        Consume(TokenType.RightBrace, "Expected '}' after module body");
        
        return new ModuleDeclaration(name, body, exports.Any() ? exports : null, specification);
    }

    private TypeDeclaration ParseTypeDeclaration()
    {
        var name = Consume(TokenType.Identifier, "Expected type name").Lexeme;
        Consume(TokenType.LeftBrace, "Expected '{' after type name");
        
        var fields = new List<TypeField>();
        
        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            var fieldName = Consume(TokenType.Identifier, "Expected field name").Lexeme;
            Consume(TokenType.Colon, "Expected ':' after field name");
            var fieldType = ParseTypeName();
            
            fields.Add(new TypeField(fieldName, fieldType));
            
            // Optional comma
            if (Check(TokenType.Comma))
            {
                Advance();
            }
        }
        
        Consume(TokenType.RightBrace, "Expected '}' after type body");
        
        return new TypeDeclaration(name, fields);
    }

    private ImportStatement ParseImportStatement()
    {
        var moduleName = "";
        List<string>? specificImports = null;
        bool isWildcard = false;
        
        if (Check(TokenType.Identifier))
        {
            moduleName = Advance().Lexeme;
            
            if (Match(TokenType.Dot))
            {
                // Handle wildcard imports: import Utils.*
                if (Check(TokenType.Multiply))
                {
                    Advance();
                    isWildcard = true;
                }
                else
                {
                    // Handle specific imports: import Utils.{add, subtract}
                    Consume(TokenType.LeftBrace, "Expected '{' for specific imports");
                    specificImports = new List<string>();
                    
                    if (Check(TokenType.Multiply))
                    {
                        Advance();
                        isWildcard = true;
                    }
                    else
                    {
                        do
                        {
                            specificImports.Add(Consume(TokenType.Identifier, "Expected import name").Lexeme);
                        } while (Match(TokenType.Comma));
                    }
                    
                    Consume(TokenType.RightBrace, "Expected '}' after imports");
                }
            }
        }
        
        return new ImportStatement(moduleName, specificImports, isWildcard);
    }

    private ASTNode ParseExportStatement()
    {
        if (Match(TokenType.Function) || Match(TokenType.Pure))
        {
            // This is an export function declaration - mark it as exported
            Previous(); // Go back
            return ParseFunctionDeclaration(null, true); // Mark as exported
        }
        else
        {
            // Export list - handle both syntax: export add, multiply AND export { add, multiply }
            var exports = new List<string>();
            
            // Check if using curly brace syntax: export { ... }
            if (Match(TokenType.LeftBrace))
            {
                // Parse: export { add, multiply }
                do
                {
                    exports.Add(Consume(TokenType.Identifier, "Expected export name").Lexeme);
                } while (Match(TokenType.Comma));
                
                Consume(TokenType.RightBrace, "Expected '}' after export list");
            }
            else
            {
                // Parse: export add, multiply
                do
                {
                    exports.Add(Consume(TokenType.Identifier, "Expected export name").Lexeme);
                } while (Match(TokenType.Comma));
            }
            
            return new ExportStatement(exports);
        }
    }

    private FunctionDeclaration ParseFunctionDeclaration(SpecificationBlock? specification = null, bool isExported = false)
    {
        bool isPure = Previous().Type == TokenType.Pure;
        if (isPure && !Match(TokenType.Function))
        {
            throw new Exception("Expected 'function' after 'pure'");
        }

        var name = Consume(TokenType.Identifier, "Expected function name").Lexeme;
        
        Consume(TokenType.LeftParen, "Expected '(' after function name");
        var parameters = new List<Parameter>();
        
        if (!Check(TokenType.RightParen))
        {
            do
            {
                var paramName = Consume(TokenType.Identifier, "Expected parameter name").Lexeme;
                Consume(TokenType.Colon, "Expected ':' after parameter name");
                var paramType = ParseType();
                parameters.Add(new Parameter(paramName, paramType));
            } while (Match(TokenType.Comma));
        }
        
        Consume(TokenType.RightParen, "Expected ')' after parameters");
        
        List<string>? effects = null;
        if (Match(TokenType.Uses))
        {
            effects = ParseEffectsList();
        }
        
        string? returnType = null;
        if (Match(TokenType.Arrow))
        {
            returnType = ParseType();
        }
        
        Consume(TokenType.LeftBrace, "Expected '{' before function body");
        var body = ParseStatements();
        Consume(TokenType.RightBrace, "Expected '}' after function body");
        
        return new FunctionDeclaration(name, parameters, returnType, body, isPure, effects, isExported, specification);
    }

    private AppStateDeclaration ParseAppStateDeclaration()
    {
        var name = Consume(TokenType.Identifier, "Expected app state name").Lexeme;
        
        List<string>? effects = null;
        if (Match(TokenType.Uses))
        {
            effects = ParseEffectsList();
        }
        
        Consume(TokenType.LeftBrace, "Expected '{' after app state declaration");
        
        var stateVariables = new List<StateDeclaration>();
        var actions = new List<StateAction>();
        
        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            if (Check(TokenType.Identifier))
            {
                // State variable declaration
                var varName = Advance().Lexeme;
                Consume(TokenType.Colon, "Expected ':' after state variable name");
                var varType = Consume(TokenType.Identifier, "Expected state variable type").Lexeme;
                
                ASTNode? initialValue = null;
                if (Match(TokenType.Assign))
                {
                    initialValue = ParseExpression();
                }
                
                stateVariables.Add(new StateDeclaration(varName, varType, initialValue));
            }
            else if (Match(TokenType.Action))
            {
                // Action declaration
                var actionName = Consume(TokenType.Identifier, "Expected action name").Lexeme;
                
                Consume(TokenType.LeftParen, "Expected '(' after action name");
                var parameters = new List<Parameter>();
                
                if (!Check(TokenType.RightParen))
                {
                    do
                    {
                        var paramName = Consume(TokenType.Identifier, "Expected parameter name").Lexeme;
                        Consume(TokenType.Colon, "Expected ':' after parameter name");
                        var paramType = Consume(TokenType.Identifier, "Expected parameter type").Lexeme;
                        parameters.Add(new Parameter(paramName, paramType));
                    } while (Match(TokenType.Comma));
                }
                
                Consume(TokenType.RightParen, "Expected ')' after parameters");
                
                List<string>? actionEffects = null;
                if (Match(TokenType.Uses))
                {
                    actionEffects = ParseEffectsList();
                }
                
                Consume(TokenType.LeftBrace, "Expected '{' to start action body");
                var body = ParseStatements();
                Consume(TokenType.RightBrace, "Expected '}' after action body");
                
                actions.Add(new StateAction(actionName, parameters, actionEffects, body));
            }
            else
            {
                Advance(); // Skip unknown tokens
            }
        }
        
        Consume(TokenType.RightBrace, "Expected '}' after app state body");
        
        return new AppStateDeclaration(name, stateVariables, actions, effects);
    }

    private ApiClientDeclaration ParseApiClientDeclaration()
    {
        var name = Consume(TokenType.Identifier, "Expected API client name").Lexeme;
        Consume(TokenType.From, "Expected 'from' after API client name");
        var baseUrl = Consume(TokenType.String, "Expected base URL string").Literal?.ToString() ?? "";
        
        Consume(TokenType.LeftBrace, "Expected '{' after API client declaration");
        
        var methods = new List<ApiMethod>();
        
        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            var methodName = Consume(TokenType.Identifier, "Expected method name").Lexeme;
            
            Consume(TokenType.LeftParen, "Expected '(' after method name");
            var parameters = new List<Parameter>();
            
            if (!Check(TokenType.RightParen))
            {
                do
                {
                    var paramName = Consume(TokenType.Identifier, "Expected parameter name").Lexeme;
                    Consume(TokenType.Colon, "Expected ':' after parameter name");
                    var paramType = Consume(TokenType.Identifier, "Expected parameter type").Lexeme;
                    parameters.Add(new Parameter(paramName, paramType));
                } while (Match(TokenType.Comma));
            }
            
            Consume(TokenType.RightParen, "Expected ')' after parameters");
            Consume(TokenType.Arrow, "Expected '->' after method parameters");
            var returnType = Consume(TokenType.Identifier, "Expected return type").Lexeme;
            
            List<string>? effects = null;
            if (Match(TokenType.Uses))
            {
                effects = ParseEffectsList();
            }
            
            methods.Add(new ApiMethod(methodName, parameters, returnType, effects));
        }
        
        Consume(TokenType.RightBrace, "Expected '}' after API client body");
        
        return new ApiClientDeclaration(name, baseUrl, methods);
    }

    private SpecificationBlock? ParseSpecificationBlock()
    {
        if (!Check(TokenType.SpecStart)) return null;
        
        var specToken = Advance(); // Consume SpecStart token
        var content = specToken.Literal?.ToString() ?? "";
        
        // Parse the YAML-like content
        var intent = "";
        var rules = new List<string>();
        var postconditions = new List<string>();
        string? sourceDoc = null;
        
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        string? currentSection = null;
        
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;
            
            if (trimmed.StartsWith("intent:"))
            {
                intent = trimmed.Substring(7).Trim().Trim('"');
                currentSection = "intent";
            }
            else if (trimmed.StartsWith("rules:"))
            {
                currentSection = "rules";
            }
            else if (trimmed.StartsWith("postconditions:"))
            {
                currentSection = "postconditions";
            }
            else if (trimmed.StartsWith("source_doc:"))
            {
                sourceDoc = trimmed.Substring(11).Trim().Trim('"');
                currentSection = "source_doc";
            }
            else if (trimmed.StartsWith("- "))
            {
                var item = trimmed.Substring(2).Trim().Trim('"');
                if (currentSection == "rules")
                {
                    rules.Add(item);
                }
                else if (currentSection == "postconditions")
                {
                    postconditions.Add(item);
                }
            }
        }
        
        if (string.IsNullOrEmpty(intent))
        {
            throw new Exception($"Specification block missing required 'intent' field at line {specToken.Line}");
        }
        
        return new SpecificationBlock(
            intent,
            rules.Count > 0 ? rules : null,
            postconditions.Count > 0 ? postconditions : null,
            sourceDoc
        );
    }
}