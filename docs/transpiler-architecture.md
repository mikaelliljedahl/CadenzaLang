# Cadenza Transpiler Architecture

## Overview

The Cadenza transpiler uses **Microsoft.CodeAnalysis.CSharp** (Roslyn) to transform Cadenza source code into executable C# code through a sophisticated multi-stage process. This document explains how the transpiler works internally and how it leverages Roslyn for code generation.

## Pipeline Architecture

```
Cadenza Source → Lexer → Parser → AST → Import Resolution → Roslyn Generator → C# Syntax Tree → C# Code
```

### Multi-Module Compilation Flow (December 2024)

Cadenza now supports multi-module compilation with proper import resolution:

```
1. Parse all Cadenza files into AST nodes
2. Process import statements to build symbol mapping
3. Generate qualified C# calls for imported functions
4. Combine all modules into single C# compilation unit
```

The transpiler follows this flow in `CadenzaTranspiler.TranspileAsync()` (`src/Cadenza.Core/cadenzac-core.cs:2841+`):

```csharp
// Read source file
var source = await File.ReadAllTextAsync(sourceFile);

// Lex: Convert text to tokens
var lexer = new CadenzaLexer(source);
var tokens = lexer.ScanTokens();

// Parse: Convert tokens to AST
var parser = new CadenzaParser(tokens);
var ast = parser.Parse();

// Generate: Convert AST to C# using Roslyn
var generator = new CSharpGenerator();
var syntaxTree = generator.GenerateFromAST(ast);

// Output: Convert syntax tree to readable C# code
var csharpCode = syntaxTree.GetRoot().NormalizeWhitespace().ToFullString();
```

## Multi-Module Import Resolution System

### Import Statement Processing

**Location**: `src/Cadenza.Core/cadenzac-core.cs:2845+`

The transpiler implements a two-pass system for handling imports:

```csharp
// First pass: Process imports to build symbol mapping
foreach (var statement in program.Statements)
{
    if (statement is ImportStatement import)
    {
        ProcessImportStatement(import);
    }
}

// Second pass: Generate actual C# code with qualified calls
foreach (var statement in program.Statements)
{
    var member = GenerateStatement(statement);
    // ...
}
```

### Symbol Mapping Implementation

```csharp
private readonly Dictionary<string, string> _importedSymbols = new();

private void ProcessImportStatement(ImportStatement import)
{
    // Handle specific imports like: import Math.{add, multiply}
    if (import.SpecificImports != null)
    {
        var moduleNamespace = $"Cadenza.Modules.{import.ModuleName}.{import.ModuleName}";
        
        foreach (var symbol in import.SpecificImports)
        {
            // Map imported symbol to fully qualified C# name
            _importedSymbols[symbol] = $"{moduleNamespace}.{symbol}";
        }
    }
}
```

### Qualified Call Generation

**Location**: `src/Cadenza.Core/cadenzac-core.cs:2525+`

```csharp
// In GenerateCallExpression:
if (_importedSymbols.ContainsKey(call.Name))
{
    // Generate qualified call: Cadenza.Modules.Math.Math.add
    var qualifiedName = _importedSymbols[call.Name];
    var parts = qualifiedName.Split('.');
    expression = IdentifierName(parts[0]);
    for (int i = 1; i < parts.Length; i++)
    {
        expression = MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            expression,
            IdentifierName(parts[i])
        );
    }
}
```

### Import Resolution Example

**Cadenza Input**:
```cadenza
import Math.{add, multiply}

function main() -> int {
    let result = add(5, 3)
    let product = multiply(result, 2)
    return product
}
```

**Generated C# Output**:
```csharp
public static int main()
{
    var result = Cadenza.Modules.Math.Math.add(5, 3);
    var product = Cadenza.Modules.Math.Math.multiply(result, 2);
    return product;
}
```

## Roslyn Syntax Tree Construction

The `CSharpGenerator` class builds C# syntax trees using Roslyn's factory methods from `SyntaxFactory`:

```csharp
public SyntaxTree GenerateFromAST(Program program)
{
    // Create compilation unit (top-level container)
    var compilationUnit = CompilationUnit();
    
    // Add using statements
    compilationUnit = compilationUnit.AddUsings(
        UsingDirective(ParseName("System")),
        UsingDirective(ParseName("System.Collections.Generic"))
    );
    
    // Generate namespace declarations
    var namespaceDecl = NamespaceDeclaration(ParseName(namespaceName))
        .AddMembers(members.ToArray());
    
    // Create final syntax tree
    return CSharpSyntaxTree.Create(compilationUnit);
}
```

## AST Node Translation

Each Cadenza AST node type is systematically translated to equivalent C# syntax:

### Function Declarations

**Location**: `src/Cadenza.Core/cadenzac-core.cs:2250+`

```csharp
private MethodDeclarationSyntax GenerateFunctionDeclaration(FunctionDeclaration func)
{
    // Convert Cadenza function to C# static method
    var method = MethodDeclaration(returnType, func.Name)
        .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
        .AddParameterListParameters(parameters.ToArray())
        .WithBody(Block(statements));
}
```

### Method Calls

**Location**: `src/Cadenza.Core/cadenzac-core.cs:2476+`

```csharp
private InvocationExpressionSyntax GenerateMethodCallExpression(MethodCallExpression methodCall)
{
    // Generate: object.method(args)
    var objectExpression = GenerateExpression(methodCall.Object);
    var memberAccess = MemberAccessExpression(
        SyntaxKind.SimpleMemberAccessExpression,
        objectExpression,
        IdentifierName(methodCall.Method)
    );
    var args = methodCall.Arguments.Select(arg => Argument(GenerateExpression(arg))).ToArray();
    return InvocationExpression(memberAccess).AddArgumentListArguments(args);
}
```

### Expression Handling

The transpiler handles complex nested expressions through recursive Roslyn generation:

```csharp
private ExpressionSyntax GenerateExpression(ASTNode expression)
{
    return expression switch
    {
        BinaryExpression binary => GenerateBinaryExpression(binary),
        CallExpression call => GenerateCallExpression(call),
        MethodCallExpression methodCall => GenerateMethodCallExpression(methodCall),
        StringInterpolation interpolation => GenerateStringInterpolation(interpolation),
        ListExpression list => GenerateListExpression(list),
        OptionExpression option => GenerateOptionExpression(option),
        ResultExpression result => GenerateResultExpression(result),
        MatchExpression match => GenerateMatchExpression(match),
        // ... 15+ other expression types
    };
}
```

## Type System Generation

Cadenza's advanced types are generated as C# structs and helper classes:

### Result&lt;T,E&gt; Types

**Location**: `src/Cadenza.Core/cadenzac-core.cs:2100+`

```csharp
private MemberDeclarationSyntax[] GenerateResultTypes()
{
    // Generate Result<T,E> struct
    var resultStruct = StructDeclaration("Result")
        .AddModifiers(Token(SyntaxKind.PublicKeyword))
        .AddTypeParameterListParameters(TypeParameter("T"), TypeParameter("E"))
        .AddMembers(
            // readonly bool IsSuccess;
            // readonly T Value;
            // readonly E Error;
            // Constructor
        );
    
    // Generate Result helper class with Ok/Error methods
    var resultClass = ClassDeclaration("Result")
        .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
        .AddMembers(
            // public static Result<T,E> Ok<T,E>(T value)
            // public static Result<T,E> Error<T,E>(E error)
        );
    
    return new MemberDeclarationSyntax[] { resultStruct, resultClass };
}
```

### Option&lt;T&gt; Types

Similar pattern for Option types with `Some(value)` and `None()` constructors.

## Modern C# Features Integration

### Top-Level Statements

**Location**: `src/Cadenza.Core/cadenzac-core.cs:2076+`

```csharp
private GlobalStatementSyntax GenerateTopLevelStatement(string mainNamespace)
{
    // Generate: Cadenza.Modules.ModuleName.ClassName.main();
    var moduleName = mainNamespace.Replace("Cadenza.Modules.", "");
    var statement = ExpressionStatement(
        InvocationExpression(
            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    ParseName(mainNamespace), IdentifierName(moduleName)),
                IdentifierName("main"))));
    return GlobalStatement(statement);
}
```

This generates clean modern C# like:
```csharp
Cadenza.Modules.GeminiExample.GeminiExample.main();
```

### Type Mapping

Cadenza types are mapped to appropriate C# types:

```csharp
private string MapCadenzaTypeToCSharp(string flowLangType)
{
    return flowLangType switch
    {
        "string" => "string",
        "int" => "int", 
        "bool" => "bool",
        "Unit" => "void",
        _ => flowLangType
    };
}
```

## Binary Generation Process

**Important**: Cadenza doesn't generate binaries directly. Instead:

1. **Transpiler Output**: Generates complete, compilable C# source code
2. **Runtime Compilation**: Uses `dotnet run` or `dotnet build` for actual binary creation
3. **Roslyn's Role**: Provides syntax tree construction and code formatting, not compilation

### Execution Workflow

```bash
# 1. Transpile Cadenza to C#
./cadenzac-core hello.cdz hello.cs

# 2. Execute with .NET runtime
dotnet run hello.cs
```

## Complete Example

### Cadenza Input
```cadenza
module GeminiExample {
    pure function createGreeting(name: string) -> string {
        return "Hello, " + name + " from a pure function!"
    }
    
    function main() : Console {
        let greeting = createGreeting("Gemini")
        Console.WriteLine(greeting)
    }
}
```

### Generated C# Output
```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

Cadenza.Modules.GeminiExample.GeminiExample.main();
namespace Cadenza.Modules.GeminiExample
{
    public static class GeminiExample
    {
        /// <param name="name">Parameter of type String</param>
        /// <returns>Returns String</returns>
        public static String createGreeting(String name)
        {
            return "Hello, " + name + " from a pure function!";
        }

        /// <summary>
        /// Effects: Console
        /// </summary>
        /// <param name="message">Parameter of type String</param>
        /// <returns>Returns Unit</returns>
        public static void main()
        {
            var greeting = createGreeting("Gemini");
            Console.WriteLine(greeting);
            return;
        }
    }
}

// Result<T,E> and Option<T> struct definitions...
```

### Execution Result
```
Hello, Gemini from a pure function!
```

## Key Advantages of Roslyn Approach

- **Correctness**: Roslyn ensures syntactically valid C# code
- **Formatting**: Automatic code formatting with `NormalizeWhitespace()`
- **Type Safety**: Compile-time validation of generated syntax trees
- **Modern Features**: Easy integration of latest C# language features
- **Performance**: Roslyn's optimized syntax tree manipulation
- **Debugging**: Generated code maintains Cadenza structure for debugging
- **Extensibility**: Easy to add new language features by extending AST and generators

## Architecture Benefits

The transpiler acts as a sophisticated **source-to-source compiler** that:

1. **Preserves Intent**: Cadenza's high-level abstractions map clearly to C# equivalents
2. **Maintains Performance**: Generated C# code is as fast as hand-written C#
3. **Enables Debugging**: Generated code structure allows debugging Cadenza through C#
4. **Leverages Ecosystem**: Full access to .NET libraries and tooling
5. **Future-Proof**: Easy to adapt to new C# language features

## Implementation Files

- **Core Transpiler**: `src/Cadenza.Core/cadenzac-core.cs`
- **Lexer**: `CadenzaLexer` class (lines 600+)
- **Parser**: `CadenzaParser` class (lines 900+)
- **Code Generator**: `CSharpGenerator` class (lines 1982+)
- **Entry Point**: `CadenzaTranspiler` class (lines 2839+)

The transpiler leverages Roslyn's powerful syntax manipulation capabilities to bridge Cadenza's high-level abstractions with C#'s execution model, providing a seamless development experience while maintaining the performance and ecosystem benefits of the .NET platform.