// Simple test for UI component compilation
using System;
using System.IO;
using System.Threading.Tasks;
using Cadenza.Core;

namespace Cadenza.Tests.Manual;

class TestUICompilation
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Testing Cadenza UI Component Compilation...");
        
        // Simple UI component source
        var flowLangSource = @"
component HelloWorld(name: string) 
    uses [DOM] 
    state [message]
    events [on_click]
    -> UIComponent 
{
    declare_state message: string = ""Hello""
    
    event_handler handle_click() uses [DOM] {
        set_state(message, ""Clicked!"")
    }
    
    render {
        container(class: ""hello-world"") {
            heading(level: 1, text: message)
            button(
                text: ""Click me"",
                on_click: handle_click
            )
        }
    }
}";

        try
        {
            // Test lexing
            Console.WriteLine("1. Testing Lexer...");
            var lexer = new CadenzaLexer(flowLangSource);
            var tokens = lexer.Tokenize();
            Console.WriteLine($"   ✅ Generated {tokens.Count} tokens");
            
            // Test parsing
            Console.WriteLine("2. Testing Parser...");
            var parser = new CadenzaParser(tokens);
            var ast = parser.Parse();
            Console.WriteLine($"   ✅ Generated AST with {ast.Statements.Count} statements");
            
            // Check for component declarations
            var components = ast.Statements.OfType<ComponentDeclaration>().ToList();
            Console.WriteLine($"   ✅ Found {components.Count} UI components");
            
            if (components.Count > 0)
            {
                var component = components[0];
                Console.WriteLine($"   📝 Component: {component.Name}");
                Console.WriteLine($"   📝 Effects: [{string.Join(", ", component.Effects)}]");
                Console.WriteLine($"   📝 State: [{string.Join(", ", component.StateVariables)}]");
                Console.WriteLine($"   📝 Events: [{string.Join(", ", component.Events)}]");
            }
            
            // Test JavaScript generation
            Console.WriteLine("3. Testing JavaScript Generation...");
            var config = new TargetConfiguration();
            var jsGenerator = new JavaScriptGenerator();
            var result = await jsGenerator.GenerateAsync(ast, config);
            
            Console.WriteLine("   ✅ JavaScript generation successful!");
            Console.WriteLine($"   📄 Generated {result.SourceCode.Length} characters of JavaScript code");
            Console.WriteLine($"   📁 Generated {result.AdditionalFiles.Count} additional files");
            
            // Save generated code
            await File.WriteAllTextAsync("test_output.js", result.SourceCode);
            Console.WriteLine("   💾 Saved generated code to test_output.js");
            
            // Display first few lines of generated code
            var lines = result.SourceCode.Split('\n');
            Console.WriteLine("   📄 Generated JavaScript (first 10 lines):");
            for (int i = 0; i < Math.Min(10, lines.Length); i++)
            {
                Console.WriteLine($"      {i + 1:D2}: {lines[i]}");
            }
            
            Console.WriteLine();
            Console.WriteLine("🎉 UI Component compilation test PASSED!");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test FAILED: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}