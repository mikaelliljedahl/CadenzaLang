// Cadenza Transpiler - Extracted from cadenzac-core.cs

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
// TRANSPILER
// =============================================================================

public class CadenzaTranspiler
{
    public string TranspileFromSource(string source)
    {
        // Lex
        var lexer = new CadenzaLexer(source);
        var tokens = lexer.ScanTokens();
        
        // Parse
        var parser = new CadenzaParser(tokens);
        var ast = parser.Parse();
        
        // Check if any components have UIComponent return type
        bool hasUIComponents = ast.Statements.OfType<ComponentDeclaration>()
            .Any(c => c.ReturnType.Equals("UIComponent", StringComparison.OrdinalIgnoreCase));
        
        if (hasUIComponents)
        {
            // Use Blazor generation for UI components
            return TranspileToBlazorFromAST(ast);
        }
        else
        {
            // Generate regular C#
            var generator = new CSharpGenerator();
            var syntaxTree = generator.GenerateFromAST(ast);
            
            return syntaxTree.GetRoot().NormalizeWhitespace().ToFullString();
        }
    }

    public async Task<string> TranspileAsync(string sourceFile, string? outputFile = null)
    {
        // Read source file
        var source = await File.ReadAllTextAsync(sourceFile);
        
        // Lex
        var lexer = new CadenzaLexer(source);
        var tokens = lexer.ScanTokens();
        
        // Parse
        var parser = new CadenzaParser(tokens);
        var ast = parser.Parse();
        
        // Check if any components have UIComponent return type
        bool hasUIComponents = ast.Statements.OfType<ComponentDeclaration>()
            .Any(c => c.ReturnType.Equals("UIComponent", StringComparison.OrdinalIgnoreCase));
        
        string csharpCode;
        if (hasUIComponents)
        {
            // Use Blazor generation for UI components
            csharpCode = TranspileToBlazorFromAST(ast);
        }
        else
        {
            // Generate regular C#
            var generator = new CSharpGenerator();
            var syntaxTree = generator.GenerateFromAST(ast);
            csharpCode = syntaxTree.GetRoot().NormalizeWhitespace().ToFullString();
        }
        
        // Write to output file if specified
        if (outputFile != null)
        {
            await File.WriteAllTextAsync(outputFile, csharpCode);
        }
        
        return csharpCode;
    }
    
    public async Task<string> TranspileToJavaScriptAsync(string sourceFile, string? outputFile = null)
    {
        // For now, return a placeholder - full JavaScript generation would be implemented here
        var jsCode = "// JavaScript generation not yet implemented in core compiler\n// Use cadenzac-dev tool for JavaScript compilation";
        
        if (outputFile != null)
        {
            await File.WriteAllTextAsync(outputFile, jsCode);
        }
        
        return jsCode;
    }
    
    public async Task<string> TranspileToBlazorAsync(string sourceFile, string? outputFile = null)
    {
        try
        {
            // Read source file
            var source = await File.ReadAllTextAsync(sourceFile);
            
            // Lex
            var lexer = new CadenzaLexer(source);
            var tokens = lexer.ScanTokens();
            
            // Parse
            var parser = new CadenzaParser(tokens);
            var ast = parser.Parse();
            
            // Generate Blazor using production BlazorGenerator
            var blazorCode = TranspileToBlazorFromAST(ast);
            
            // Write to output file if specified
            if (outputFile != null)
            {
                await File.WriteAllTextAsync(outputFile, blazorCode);
            }
            
            return blazorCode;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to transpile Blazor component from '{sourceFile}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Helper method to transpile AST to Blazor using the production BlazorGenerator
    /// </summary>
    private string TranspileToBlazorFromAST(ProgramNode ast)
    {
        try
        {
            var blazorGenerator = new EnhancedBlazorGenerator();
            var blazorContent = new System.Text.StringBuilder();
            
            // Generate Blazor components from AST
            foreach (var statement in ast.Statements)
            {
                if (statement is ComponentDeclaration component)
                {
                    blazorContent.AppendLine(blazorGenerator.GenerateBlazorComponent(component));
                    blazorContent.AppendLine(); // Add spacing between components
                }
                // Note: For mixed UI/non-UI scenarios, non-UI statements would need separate compilation
                // This integration focuses on pure UI component files
            }
            
            var result = blazorContent.ToString();
            
            // Validate that we generated some content
            if (string.IsNullOrWhiteSpace(result))
            {
                throw new InvalidOperationException("No Blazor components found or generated");
            }
            
            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to generate Blazor code from AST: {ex.Message}", ex);
        }
    }
}