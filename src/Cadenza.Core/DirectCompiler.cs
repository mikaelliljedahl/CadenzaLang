// Cadenza Core Compiler - Direct Compilation Classes
// Extracted from Compiler.cs

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
// COMPILATION RECORDS
// =============================================================================

/// <summary>
/// Result of a direct compilation operation
/// </summary>
public record CompilationResult(
    bool Success,
    IEnumerable<Diagnostic> Diagnostics,
    string? AssemblyPath = null,
    TimeSpan? CompilationTime = null
);

/// <summary>
/// Options for direct compilation
/// </summary>
public record CompilationOptions(
    string SourceFile,
    string OutputPath,
    OutputKind OutputKind = OutputKind.ConsoleApplication,
    bool OptimizeCode = true,
    bool IncludeDebugSymbols = false,
    string[]? AdditionalReferences = null
);

// =============================================================================
// DIRECT COMPILER
// =============================================================================

/// <summary>
/// Direct compiler that generates assemblies from Cadenza source using Roslyn
/// </summary>
public class DirectCompiler
{
    private readonly CadenzaTranspiler _transpiler;
    private readonly CompilationCache _cache;

    public DirectCompiler()
    {
        _transpiler = new CadenzaTranspiler();
        _cache = new CompilationCache();
    }

    /// <summary>
    /// Compiles Cadenza source directly to an assembly
    /// </summary>
    public async Task<CompilationResult> CompileToAssemblyAsync(CompilationOptions options)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            // Step 1: Parse Cadenza source to AST
            var source = await File.ReadAllTextAsync(options.SourceFile);
            var lexer = new CadenzaLexer(source);
            var tokens = lexer.ScanTokens();
            var parser = new CadenzaParser(tokens);
            var ast = parser.Parse();

            // Step 2: Generate C# syntax tree
            var generator = new CSharpGenerator();
            var syntaxTree = generator.GenerateFromAST(ast);

            // Step 3: Create compilation with references
            var compilation = CreateCompilation(syntaxTree, options);

            // Step 4: Emit assembly
            var emitResult = compilation.Emit(options.OutputPath);

            var compilationTime = DateTime.UtcNow - startTime;

            return new CompilationResult(
                Success: emitResult.Success,
                Diagnostics: emitResult.Diagnostics,
                AssemblyPath: emitResult.Success ? options.OutputPath : null,
                CompilationTime: compilationTime
            );
        }
        catch (Exception ex)
        {
            var compilationTime = DateTime.UtcNow - startTime;
            var diagnostic = Diagnostic.Create(
                new DiagnosticDescriptor(
                    "FL0001",
                    "Compilation Error",
                    ex.Message,
                    "Compiler",
                    DiagnosticSeverity.Error,
                    true
                ),
                Location.None
            );

            return new CompilationResult(
                Success: false,
                Diagnostics: new[] { diagnostic },
                CompilationTime: compilationTime
            );
        }
    }

    /// <summary>
    /// Compiles and immediately executes the Cadenza program
    /// </summary>
    public async Task<(CompilationResult CompilationResult, int? ExitCode)> CompileAndRunAsync(CompilationOptions options)
    {
        var compilationResult = await CompileToAssemblyAsync(options);
        
        if (!compilationResult.Success || compilationResult.AssemblyPath == null)
        {
            return (compilationResult, null);
        }

        try
        {
            // Load and execute the assembly
            var assembly = Assembly.LoadFrom(compilationResult.AssemblyPath);
            var entryPoint = assembly.EntryPoint;

            if (entryPoint == null)
            {
                return (compilationResult, null);
            }

            var result = entryPoint.Invoke(null, new object[] { Array.Empty<string>() });
            var exitCode = result is int code ? code : 0;

            return (compilationResult, exitCode);
        }
        catch (Exception ex)
        {
            var diagnostic = Diagnostic.Create(
                new DiagnosticDescriptor(
                    "FL0002",
                    "Execution Error",
                    ex.Message,
                    "Runtime",
                    DiagnosticSeverity.Error,
                    true
                ),
                Location.None
            );

            var updatedResult = compilationResult with
            {
                Success = false,
                Diagnostics = compilationResult.Diagnostics.Append(diagnostic)
            };

            return (updatedResult, null);
        }
    }

    /// <summary>
    /// Creates a CSharpCompilation with proper references and options
    /// </summary>
    private CSharpCompilation CreateCompilation(SyntaxTree syntaxTree, CompilationOptions options)
    {
        var references = GetDefaultReferences();
        
        if (options.AdditionalReferences != null)
        {
            var additionalRefs = options.AdditionalReferences
                .Select(path => MetadataReference.CreateFromFile(path));
            references = references.Concat(additionalRefs).ToArray();
        }

        var compilationOptions = new CSharpCompilationOptions(
            outputKind: options.OutputKind,
            optimizationLevel: options.OptimizeCode ? OptimizationLevel.Release : OptimizationLevel.Debug,
            allowUnsafe: false,
            platform: Platform.AnyCpu
        );

        var assemblyName = Path.GetFileNameWithoutExtension(options.OutputPath);
        
        return CSharpCompilation.Create(
            assemblyName: assemblyName,
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: compilationOptions
        );
    }

    /// <summary>
    /// Gets the default .NET references needed for Cadenza programs
    /// </summary>
    private MetadataReference[] GetDefaultReferences()
    {
        var references = new List<MetadataReference>
        {
            // Core .NET references
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Runtime.CompilerServices.RuntimeHelpers).Assembly.Location),
            
            // System.Runtime reference (needed for modern .NET)
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            
            // System.Collections reference
            MetadataReference.CreateFromFile(Assembly.Load("System.Collections").Location),
            
            // System.Text reference (for string operations)
            MetadataReference.CreateFromFile(Assembly.Load("System.Text.RegularExpressions").Location)
        };
        
        // Add Cadenza.Runtime reference if it exists in the same assembly
        try
        {
            var currentAssembly = Assembly.GetExecutingAssembly();
            references.Add(MetadataReference.CreateFromFile(currentAssembly.Location));
        }
        catch (Exception)
        {
            // If we can't load the runtime, continue without it
        }
        
        return references.ToArray();
    }
}