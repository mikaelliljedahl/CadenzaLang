// Cadenza Core Compiler - CLI Classes
// Extracted from Compiler.cs

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Cadenza.Core;

// =============================================================================
// DIRECT COMPILER CLI
// =============================================================================

/// <summary>
/// Enhanced CLI program with direct compilation support
/// </summary>
public class DirectCompilerCLI
{
    private readonly DirectCompiler _compiler;

    public DirectCompilerCLI()
    {
        _compiler = new DirectCompiler();
    }

    /// <summary>
    /// Enhanced main method with direct compilation support
    /// </summary>
    public async Task<int> RunAsync(string[] args)
    {
        try
        {
            var options = ParseArguments(args);
            
            if (options == null)
            {
                ShowHelp();
                return 1;
            }

            if (options.ShowHelp)
            {
                ShowHelp();
                return 0;
            }

            if (options.ShowVersion)
            {
                Console.WriteLine("Cadenza Direct Compiler v1.0.0");
                return 0;
            }

            if (options.ServeMode)
            {
                return await HandleServeMode(options);
            }
            else if (options.CompileMode)
            {
                return await HandleCompileMode(options);
            }
            else
            {
                return await HandleTranspileMode(options);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private async Task<int> HandleCompileMode(CLIOptions options)
    {
        // Handle project compilation mode
        if (options.ProjectMode)
        {
            return await HandleProjectMode(options);
        }

        var compilationOptions = new CompilationOptions(
            SourceFile: options.InputFile!,
            OutputPath: options.OutputFile ?? GetDefaultOutputPath(options.InputFile!, options.Library),
            OutputKind: options.Library ? OutputKind.DynamicallyLinkedLibrary : OutputKind.ConsoleApplication,
            OptimizeCode: !options.Debug,
            IncludeDebugSymbols: options.Debug
        );

        if (options.Run)
        {
            var (result, exitCode) = await _compiler.CompileAndRunAsync(compilationOptions);
            
            if (!result.Success)
            {
                DisplayCompilationErrors(result.Diagnostics);
                return 1;
            }

            Console.WriteLine($"Compilation successful: {compilationOptions.OutputPath}");
            Console.WriteLine($"Compilation time: {result.CompilationTime?.TotalMilliseconds:F2}ms");
            
            if (exitCode.HasValue)
            {
                Console.WriteLine($"Program exited with code: {exitCode.Value}");
                return exitCode.Value;
            }
            
            return 0;
        }
        else
        {
            var result = await _compiler.CompileToAssemblyAsync(compilationOptions);
            
            if (!result.Success)
            {
                DisplayCompilationErrors(result.Diagnostics);
                return 1;
            }

            Console.WriteLine($"Compilation successful: {compilationOptions.OutputPath}");
            Console.WriteLine($"Compilation time: {result.CompilationTime?.TotalMilliseconds:F2}ms");
            return 0;
        }
    }


    private async Task<int> HandleServeMode(CLIOptions options)
    {
        if (string.IsNullOrEmpty(options.InputFile))
        {
            Console.Error.WriteLine("Error: Input file or project directory required for --serve mode");
            return 1;
        }
        
        // Detect if the input is a project directory, cadenzac.json file, or single .cdz file
        string resolvedPath = ResolveServeInput(options.InputFile);
        
        if (resolvedPath == null)
        {
            Console.Error.WriteLine($"Error: Could not find valid input - checked for:");
            Console.Error.WriteLine($"  - Single .cdz file: {options.InputFile}");
            Console.Error.WriteLine($"  - Project directory: {options.InputFile}/cadenzac.json");
            Console.Error.WriteLine($"  - Direct cadenzac.json: {options.InputFile}");
            return 1;
        }
        
        try
        {
            var serverOptions = new CadenzaWebServerOptions
            {
                InputFile = resolvedPath,
                Port = options.Port,
                OpenBrowser = options.OpenBrowser,
                HotReload = options.HotReload
            };
            
            var webServer = new CadenzaWebServer(serverOptions);
            await webServer.StartAsync();
            
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error starting web server: {ex.Message}");
            if (options.Verbose)
            {
                Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            return 1;
        }
    }

    /// <summary>
    /// Resolves the serve input to determine if it's a single file or project configuration
    /// </summary>
    private string? ResolveServeInput(string input)
    {
        // Check if it's a direct .cdz file (backward compatibility)
        if (File.Exists(input) && input.EndsWith(".cdz"))
        {
            return input;
        }
        
        // Check if it's a direct cadenzac.json file
        if (File.Exists(input) && input.EndsWith("cadenzac.json"))
        {
            return input;
        }
        
        // Check if it's a project directory containing cadenzac.json
        if (Directory.Exists(input))
        {
            var configPath = Path.Combine(input, "cadenzac.json");
            if (File.Exists(configPath))
            {
                return configPath;
            }
        }
        
        return null;
    }

    private async Task<int> HandleTranspileMode(CLIOptions options)
    {
        // Use transpiler with automatic UI component detection
        var transpiler = new CadenzaTranspiler();
        
        await transpiler.TranspileAsync(options.InputFile!, options.OutputFile);
        Console.WriteLine($"Successfully transpiled {options.InputFile} -> {options.OutputFile}");
        
        return 0;
    }

    private void DisplayCompilationErrors(IEnumerable<Diagnostic> diagnostics)
    {
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);
        
        foreach (var error in errors)
        {
            Console.Error.WriteLine($"Error: {error.GetMessage()}");
            
            if (error.Location != Location.None)
            {
                var lineSpan = error.Location.GetLineSpan();
                Console.Error.WriteLine($"  at line {lineSpan.StartLinePosition.Line + 1}, column {lineSpan.StartLinePosition.Character + 1}");
            }
        }
    }

    private async Task<int> HandleProjectMode(CLIOptions options)
    {
        var projectCompiler = new ProjectCompiler();
        
        try
        {
            if (options.Verbose)
            {
                Console.WriteLine("Starting project compilation...");
            }
            
            var result = await projectCompiler.CompileProjectAsync(options);
            
            if (result.Success)
            {
                Console.WriteLine($"✅ Project compiled successfully");
                Console.WriteLine($"   Output: {result.OutputPath}");
                Console.WriteLine($"   Files: {result.FilesCompiled}");
                Console.WriteLine($"   Time: {result.CompilationTime.TotalMilliseconds:F0}ms");
                
                if (result.Warnings.Count > 0)
                {
                    Console.WriteLine($"   Warnings: {result.Warnings.Count}");
                    if (options.Verbose)
                    {
                        foreach (var warning in result.Warnings)
                        {
                            Console.WriteLine($"     Warning: {warning}");
                        }
                    }
                }
                
                return 0;
            }
            else
            {
                Console.Error.WriteLine("❌ Project compilation failed");
                foreach (var error in result.Errors)
                {
                    Console.Error.WriteLine($"   Error: {error}");
                }
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"❌ Project compilation failed: {ex.Message}");
            if (options.Verbose)
            {
                Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            return 1;
        }
    }

    private string GetDefaultOutputPath(string inputFile, bool isLibrary)
    {
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(inputFile);
        var extension = isLibrary ? ".dll" : ".exe";
        return nameWithoutExtension + extension;
    }

    private CLIOptions? ParseArguments(string[] args)
    {
        if (args.Length == 0)
            return new CLIOptions { ShowHelp = true };

        var options = new CLIOptions();
        
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--help":
                case "-h":
                    options.ShowHelp = true;
                    return options;
                
                case "--version":
                case "-v":
                    options.ShowVersion = true;
                    return options;
                
                case "--compile":
                case "-c":
                    options.CompileMode = true;
                    break;
                
                case "--run":
                case "-r":
                    options.Run = true;
                    options.CompileMode = true; // --run implies --compile
                    break;
                
                case "--library":
                case "-l":
                    options.Library = true;
                    break;
                
                case "--debug":
                case "-d":
                    options.Debug = true;
                    break;
                
                case "--project":
                case "-p":
                    options.ProjectMode = true;
                    options.CompileMode = true; // --project implies --compile
                    break;
                
                case "--config":
                    if (i + 1 < args.Length)
                    {
                        options.ConfigFile = args[++i];
                    }
                    break;
                
                case "--incremental":
                    options.Incremental = true;
                    break;
                
                case "--clean":
                    options.Clean = true;
                    break;
                
                case "--verbose":
                    options.Verbose = true;
                    break;
                
                case "--framework":
                    if (i + 1 < args.Length)
                    {
                        options.Framework = args[++i];
                    }
                    break;
                
                case "--output":
                case "-o":
                    if (i + 1 < args.Length)
                    {
                        options.OutputFile = args[++i];
                    }
                    break;
                
                case "--serve":
                case "-s":
                    options.ServeMode = true;
                    break;
                
                case "--port":
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out int port))
                    {
                        options.Port = port;
                        i++;
                    }
                    break;
                
                case "--no-open":
                    options.OpenBrowser = false;
                    break;
                
                case "--no-hot-reload":
                    options.HotReload = false;
                    break;
                
                
                default:
                    if (!args[i].StartsWith('-'))
                    {
                        if (options.InputFile == null)
                        {
                            options.InputFile = args[i];
                        }
                        else if (options.OutputFile == null && !options.CompileMode)
                        {
                            options.OutputFile = args[i];
                        }
                    }
                    break;
            }
        }

        // Validation
        if (options.InputFile == null && !options.ShowHelp && !options.ShowVersion && !options.ProjectMode)
        {
            Console.Error.WriteLine("Error: Input file required");
            return null;
        }

        // Set default output file for transpile mode
        if (!options.CompileMode && options.OutputFile == null && options.InputFile != null)
        {
            options.OutputFile = Path.ChangeExtension(options.InputFile, ".cs");
        }

        return options;
    }

    private void ShowHelp()
    {
        Console.WriteLine("Cadenza Direct Compiler - Transpilation and Direct Compilation");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  Transpile (default):");
        Console.WriteLine("    cadenzac-core <input.cdz> [<output.cs>]");
        Console.WriteLine();
        Console.WriteLine("  Direct compilation:");
        Console.WriteLine("    cadenzac-core --compile <input.cdz> [--output <output.exe>]");
        Console.WriteLine("    cadenzac-core --run <input.cdz>");
        Console.WriteLine("    cadenzac-core --library <input.cdz> [--output <output.dll>]");
        Console.WriteLine();
        Console.WriteLine("  Project compilation:");
        Console.WriteLine("    cadenzac-core --project [--config <cadenzac.json>] [--output <output.exe>]");
        Console.WriteLine();
        Console.WriteLine("  Web server (self-contained web runtime):");
        Console.WriteLine("    cadenzac-core --serve <component.cdz|project-dir|cadenzac.json> [--port <port>]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --compile, -c    Compile directly to assembly (default: transpile)");
        Console.WriteLine("  --run, -r        Compile and run immediately");
        Console.WriteLine("  --library, -l    Generate library (.dll) instead of executable");
        Console.WriteLine("  --debug, -d      Include debug symbols and disable optimizations");
        Console.WriteLine("  --output, -o     Specify output file path");
        Console.WriteLine("  --project, -p    Compile multi-file project");
        Console.WriteLine("  --config         Path to cadenzac.json configuration file");
        Console.WriteLine("  --verbose        Show detailed compilation output");
        Console.WriteLine("  --serve, -s      Start embedded web server for self-contained web runtime");
        Console.WriteLine("  --port           Port for web server (default: 5000)");
        Console.WriteLine("  --no-open        Don't automatically open browser");
        Console.WriteLine("  --no-hot-reload  Disable hot reload functionality");
        Console.WriteLine("  --help, -h       Show this help message");
        Console.WriteLine("  --version, -v    Show version information");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  # Transpile to C#");
        Console.WriteLine("  cadenzac-core hello.cdz hello.cs");
        Console.WriteLine();
        Console.WriteLine("  # Direct compilation");
        Console.WriteLine("  cadenzac-core --compile hello.cdz");
        Console.WriteLine("  cadenzac-core --compile hello.cdz --output hello.exe");
        Console.WriteLine();
        Console.WriteLine("  # Compile and run");
        Console.WriteLine("  cadenzac-core --run hello.cdz");
        Console.WriteLine();
        Console.WriteLine("  # Generate library");
        Console.WriteLine("  cadenzac-core --library math.cdz --output math.dll");
        Console.WriteLine();
        Console.WriteLine("  # Project compilation");
        Console.WriteLine("  cadenzac-core --project");
        Console.WriteLine("  cadenzac-core --project --verbose --output MyApp.exe");
        Console.WriteLine();
        Console.WriteLine("  # Web server (self-contained web runtime)");
        Console.WriteLine("  cadenzac-core --serve counter.cdz");
        Console.WriteLine("  cadenzac-core --serve multi-component-project/");
        Console.WriteLine("  cadenzac-core --serve project/cadenzac.json --port 8080 --no-open");
    }
}

// =============================================================================
// CLI OPTIONS
// =============================================================================

/// <summary>
/// CLI options parsed from command line arguments
/// </summary>
public class CLIOptions
{
    public string? InputFile { get; set; }
    public string? OutputFile { get; set; }
    public bool CompileMode { get; set; }
    public bool Run { get; set; }
    public bool Library { get; set; }
    public bool Debug { get; set; }
    public bool ShowHelp { get; set; }
    public bool ShowVersion { get; set; }
    
    // Project compilation options
    public bool ProjectMode { get; set; }
    public string? ConfigFile { get; set; }
    public bool Incremental { get; set; }
    public bool Clean { get; set; }
    public bool Verbose { get; set; }
    public string? Framework { get; set; }
    
    // Web server options (will be added for --serve command)
    public bool ServeMode { get; set; }
    public int Port { get; set; } = 5000;
    public bool OpenBrowser { get; set; } = true;
    public bool HotReload { get; set; } = true;
}

// =============================================================================
// FLOW CORE PROGRAM
// =============================================================================

public class FlowCoreProgram
{
    public static async Task<int> RunAsync(string[] args)
    {
        var cli = new DirectCompilerCLI();
        return await cli.RunAsync(args);
    }
}