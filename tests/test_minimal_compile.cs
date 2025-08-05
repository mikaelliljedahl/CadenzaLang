using System;
using System.IO;

class MinimalTest 
{
    static void Main(string[] args)
    {
        Console.WriteLine("Testing minimal Cadenza compilation...");
        
        // Check if a file was provided as argument
        string flowCode;
        if (args.Length > 0 && File.Exists(args[0]))
        {
            flowCode = File.ReadAllText(args[0]);
            Console.WriteLine($"Reading Cadenza code from: {args[0]}");
        }
        else
        {
            // Simple Cadenza code
            flowCode = @"
function test(a: int) -> int {
    return a
}";
            Console.WriteLine("Using default Cadenza code");
        }

        try 
        {
            // Test lexer
            var lexer = new CadenzaLexer(flowCode);
            var tokens = lexer.Tokenize();
            Console.WriteLine($"Lexer: Generated {tokens.Count} tokens");
            
            // Test parser  
            var parser = new CadenzaParser(tokens);
            var ast = parser.Parse();
            Console.WriteLine($"Parser: Generated AST with {ast.Statements.Count} statements");
            
            // Test code generator
            var generator = new CSharpGenerator();
            var syntaxTree = generator.GenerateFromAST(ast);
            var csharpCode = syntaxTree.ToString();
            Console.WriteLine($"CodeGen: Generated {csharpCode.Length} characters of C# code");
            Console.WriteLine("\n=== Generated C# code ===");
            Console.WriteLine(csharpCode);
            
            // Save to file if input was from file
            if (args.Length > 0 && File.Exists(args[0]))
            {
                var outputFile = Path.ChangeExtension(args[0], ".cs");
                File.WriteAllText(outputFile, csharpCode);
                Console.WriteLine($"\n✅ C# code saved to: {outputFile}");
            }
            
            Console.WriteLine("\n✅ Minimal compilation test passed!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Compilation test failed: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}