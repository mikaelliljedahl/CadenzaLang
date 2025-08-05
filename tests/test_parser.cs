using System;
using System.IO;
using Cadenza.Core;

namespace Cadenza.Tests.Manual;

class TestParserProgram
{
    static void Main(string[] args)
    {
        try
        {
            var sourceCode = File.ReadAllText("test_minimal.cdz");
            
            // Test lexer
            var lexer = new CadenzaLexer(sourceCode);
            var tokens = lexer.Tokenize();
            
            Console.WriteLine($"Lexed {tokens.Count} tokens successfully");
            
            // Print first few tokens for debugging
            for (int i = 0; i < Math.Min(10, tokens.Count); i++)
            {
                Console.WriteLine($"Token {i}: {tokens[i].Type} = '{tokens[i].Value}'");
            }
            
            // Test parser
            Console.WriteLine("Starting parser...");
            var parser = new CadenzaParser(tokens);
            Console.WriteLine("Created parser, calling Parse()...");
            var ast = parser.Parse();
            Console.WriteLine("Parse completed");
            
            Console.WriteLine($"Parsed AST with {ast.Statements.Count} top-level statements");
            
            foreach (var statement in ast.Statements)
            {
                Console.WriteLine($"- Statement type: {statement.GetType().Name}");
                
                if (statement is ComponentDeclaration component)
                {
                    Console.WriteLine($"  Component: {component.Name}");
                    Console.WriteLine($"  Parameters: {component.Parameters.Count}");
                    Console.WriteLine($"  Effects: {string.Join(", ", component.Effects)}");
                    Console.WriteLine($"  State variables: {string.Join(", ", component.StateVariables)}");
                    Console.WriteLine($"  Render tree: {component.RenderTree.TagName}");
                }
            }
            
            Console.WriteLine("Parser test completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}