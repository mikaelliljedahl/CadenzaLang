// Cadenza Core Compiler - Embedded Web Server Infrastructure
// Self-contained web runtime for Cadenza components

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Components.Endpoints;

namespace Cadenza.Core;

// =============================================================================
// EMBEDDED WEB SERVER
// =============================================================================

/// <summary>
/// Embedded Kestrel web server for serving Cadenza components as Blazor applications
/// </summary>
public class CadenzaWebServer
{
    private readonly CadenzaWebServerOptions _options;
    private readonly BlazorProjectGenerator _projectGenerator;
    private string? _currentProjectDir;
    
    public CadenzaWebServer(CadenzaWebServerOptions options)
    {
        _options = options;
        _projectGenerator = new BlazorProjectGenerator();
        
        // Setup cleanup on process termination
        Console.CancelKeyPress += (sender, e) => {
            e.Cancel = true; // Prevent immediate termination
            CleanupTempDirectory();
            Environment.Exit(0);
        };
    }

    /// <summary>
    /// Determines if the input file is a project configuration or single component
    /// </summary>
    private bool IsProjectConfig(string inputFile)
    {
        return inputFile.EndsWith("cadenzac.json");
    }

    /// <summary>
    /// Parses a cadenzac.json project configuration file
    /// </summary>
    private async Task<UIProjectConfig> ParseProjectConfigAsync(string configPath)
    {
        var json = await File.ReadAllTextAsync(configPath);
        var config = JsonSerializer.Deserialize<UIProjectConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        return config ?? throw new InvalidOperationException("Failed to deserialize project configuration");
    }

    /// <summary>
    /// Discovers component files in the project source directory
    /// </summary>
    private Task<List<string>> DiscoverComponentFilesAsync(string projectDir, string sourceDir)
    {
        var fullSourcePath = Path.Combine(projectDir, sourceDir);
        
        if (!Directory.Exists(fullSourcePath))
        {
            throw new DirectoryNotFoundException($"Source directory not found: {fullSourcePath}");
        }

        var componentFiles = Directory.GetFiles(fullSourcePath, "*.cdz", SearchOption.AllDirectories)
                                     .OrderBy(Path.GetFileName)
                                     .ToList();

        Console.WriteLine($"📁 Discovered {componentFiles.Count} component files in {sourceDir}");
        foreach (var file in componentFiles)
        {
            Console.WriteLine($"   - {Path.GetRelativePath(projectDir, file)}");
        }

        return Task.FromResult(componentFiles);
    }

    /// <summary>
    /// Starts the Blazor web server by launching the generated project as a subprocess
    /// </summary>
    public async Task StartAsync()
    {
        Console.WriteLine($"🌐 Starting Cadenza web server on port {_options.Port}...");
        
        // Create a debug directory for easier inspection of generated files
        _currentProjectDir = Path.Combine(Directory.GetCurrentDirectory(), "debug", $"cadenza-web-{DateTime.Now:yyyyMMdd-HHmmss}");
        Directory.CreateDirectory(_currentProjectDir);
        var tempProjectDir = _currentProjectDir;
        
        try
        {
            // Check if input is a project configuration or single file
            if (IsProjectConfig(_options.InputFile))
            {
                // Handle project-based serving
                await HandleProjectServing(tempProjectDir);
            }
            else
            {
                // Handle single file serving (backward compatibility)
                await _projectGenerator.GenerateBlazorProjectAsync(_options.InputFile, tempProjectDir);
            }
            
            // Build the generated project
            await BuildGeneratedProjectAsync(tempProjectDir);
            
            // Launch the generated project as a subprocess
            await LaunchBlazorProjectAsync(tempProjectDir);
        }
        finally
        {
            // Clean up temporary directory
            CleanupTempDirectory();
        }
    }

    /// <summary>
    /// Handles project-based serving with cadenzac.json configuration
    /// </summary>
    private async Task HandleProjectServing(string tempProjectDir)
    {
        var configPath = _options.InputFile;
        var projectDir = Path.GetDirectoryName(configPath) ?? throw new InvalidOperationException("Invalid config path");
        
        Console.WriteLine($"📋 Loading project configuration: {Path.GetFileName(configPath)}");
        
        // Parse project configuration
        var config = await ParseProjectConfigAsync(configPath);
        
        Console.WriteLine($"📦 Project: {config.Name} v{config.Version}");
        if (!string.IsNullOrEmpty(config.Description))
        {
            Console.WriteLine($"   {config.Description}");
        }
        
        // Discover component files
        var componentFiles = await DiscoverComponentFilesAsync(projectDir, config.Build.Source);
        
        if (componentFiles.Count == 0)
        {
            throw new InvalidOperationException($"No component files found in {config.Build.Source}");
        }
        
        // Generate Blazor project from multiple components
        await _projectGenerator.GenerateBlazorProjectFromFilesAsync(componentFiles, tempProjectDir, config);
    }

    /// <summary>
    /// Cleans up the temporary project directory
    /// </summary>
    private void CleanupTempDirectory()
    {
        if (!string.IsNullOrEmpty(_currentProjectDir) && Directory.Exists(_currentProjectDir))
        {
            try
            {
                Console.WriteLine($"🧹 Cleaning up temporary directory: {_currentProjectDir}");
                Directory.Delete(_currentProjectDir, true);
                _currentProjectDir = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Warning: Could not clean up temporary directory: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Launches the generated Blazor project as a subprocess
    /// </summary>
    private async Task LaunchBlazorProjectAsync(string projectDir)
    {
        // Find an available port if the specified port is in use
        var availablePort = await FindAvailablePortAsync(_options.Port);
        if (availablePort != _options.Port)
        {
            Console.WriteLine($"⚠️  Port {_options.Port} is in use, using port {availablePort} instead");
        }
        
        Console.WriteLine($"🚀 Launching Blazor project on port {availablePort}...");
        
        var projectFile = Path.Combine(projectDir, "CadenzaWebApp.csproj");
        var arguments = $"run --project \"{projectFile}\" --urls http://localhost:{availablePort}";
        
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            WorkingDirectory = projectDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = false // Allow seeing the process for debugging
        };

        // Add environment variables for the subprocess
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        startInfo.Environment["ASPNETCORE_URLS"] = $"http://localhost:{availablePort}";
        
        Console.WriteLine($"🔧 Executing: dotnet {arguments}");
        Console.WriteLine($"🔧 Environment: ASPNETCORE_URLS=http://localhost:{availablePort}");

        using var process = new System.Diagnostics.Process { StartInfo = startInfo };
        
        // Handle process output
        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Console.WriteLine($"[Blazor] {e.Data}");
            }
        };
        
        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Console.WriteLine($"[Blazor Error] {e.Data}");
            }
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            Console.WriteLine($"✅ Blazor server started!");
            Console.WriteLine($"   URL: http://localhost:{availablePort}");
            Console.WriteLine($"   Component: {_options.InputFile}");
            Console.WriteLine($"   Process ID: {process.Id}");
            
            if (_options.OpenBrowser)
            {
                // Wait a moment for the server to start, then open browser
                await Task.Delay(2000);
                OpenBrowser($"http://localhost:{availablePort}");
            }
            
            Console.WriteLine();
            Console.WriteLine("Press Ctrl+C to stop the server.");
            
            // Wait for the process to exit or be cancelled
            await process.WaitForExitAsync();
            
            Console.WriteLine($"Blazor server stopped with exit code: {process.ExitCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error starting Blazor project: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Finds an available port starting from the specified port
    /// </summary>
    private async Task<int> FindAvailablePortAsync(int startPort)
    {
        for (int port = startPort; port < startPort + 100; port++)
        {
            if (await IsPortAvailableAsync(port))
            {
                return port;
            }
        }
        
        // If we can't find an available port in the range, return a random high port
        var random = new Random();
        return random.Next(8000, 9000);
    }
    
    /// <summary>
    /// Checks if a port is available for binding
    /// </summary>
    private async Task<bool> IsPortAvailableAsync(int port)
    {
        try
        {
            var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch (System.Net.Sockets.SocketException)
        {
            return false;
        }
    }
    
    private async Task BuildGeneratedProjectAsync(string projectDir)
    {
        Console.WriteLine($"🔧 Building generated project...");
        
        var buildProcess = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "build --configuration Release --no-restore --verbosity normal",
            WorkingDirectory = projectDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        using var process = System.Diagnostics.Process.Start(buildProcess);
        if (process != null)
        {
            await process.WaitForExitAsync();
            
            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                Console.WriteLine($"Build failed: {error}");
                // Continue anyway - the project might still work
            }
            else
            {
                Console.WriteLine($"   Project built successfully");
            }
        }
    }
    
    private void OpenBrowser(string url)
    {
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to open browser: {ex.Message}");
        }
    }
}

// =============================================================================
// WEB SERVER OPTIONS
// =============================================================================

/// <summary>
/// Configuration options for the Cadenza web server
/// </summary>
public class CadenzaWebServerOptions
{
    public required string InputFile { get; init; }
    public int Port { get; init; } = 5000;
    public bool OpenBrowser { get; init; } = true;
    public bool HotReload { get; init; } = true;
}

// =============================================================================
// BLAZOR PROJECT GENERATOR
// =============================================================================

/// <summary>
/// Generates a complete Blazor Server project from Cadenza components
/// </summary>
public class BlazorProjectGenerator
{
    private readonly EnhancedBlazorGenerator _blazorGenerator;
    
    public BlazorProjectGenerator()
    {
        _blazorGenerator = new EnhancedBlazorGenerator();
    }
    
    /// <summary>
    /// Generates a complete Blazor project structure from a Cadenza component
    /// </summary>
    public async Task GenerateBlazorProjectAsync(string cadenzaFile, string outputDir)
    {
        Console.WriteLine($"📦 Generating Blazor project...");
        
        // Parse the Cadenza component
        var source = await File.ReadAllTextAsync(cadenzaFile);
        var lexer = new CadenzaLexer(source);
        var tokens = lexer.ScanTokens();
        var parser = new CadenzaParser(tokens);
        var ast = parser.Parse();
        
        // Create project structure
        await CreateProjectStructureAsync(outputDir);
        
        // Generate Blazor components from Cadenza AST
        await GenerateBlazorComponentsAsync(ast, outputDir);
        
        // Generate project files
        await GenerateProjectFilesAsync(outputDir);
        
        Console.WriteLine($"   Generated project in: {outputDir}");
    }
    
    /// <summary>
    /// Generates a complete Blazor project structure from multiple Cadenza component files
    /// </summary>
    public async Task GenerateBlazorProjectFromFilesAsync(List<string> componentFiles, string outputDir, UIProjectConfig config)
    {
        Console.WriteLine($"📦 Generating Blazor project from {componentFiles.Count} components...");
        
        // Create project structure
        await CreateProjectStructureAsync(outputDir);
        
        // Parse all component files and combine into a single AST
        var allComponents = new List<ComponentDeclaration>();
        
        foreach (var componentFile in componentFiles)
        {
            var source = await File.ReadAllTextAsync(componentFile);
            var lexer = new CadenzaLexer(source);
            var tokens = lexer.ScanTokens();
            var parser = new CadenzaParser(tokens);
            var ast = parser.Parse();
            
            var components = ast.Statements.OfType<ComponentDeclaration>().ToList();
            allComponents.AddRange(components);
            
            Console.WriteLine($"   Parsed {components.Count} component(s) from {Path.GetFileName(componentFile)}");
        }
        
        // Create a combined AST
        var combinedAst = new ProgramNode(allComponents.Cast<ASTNode>().ToList());
        
        // Generate Blazor components with project configuration
        await GenerateBlazorComponentsWithConfigAsync(combinedAst, outputDir, config);
        
        // Generate project files
        await GenerateProjectFilesAsync(outputDir);
        
        Console.WriteLine($"   Generated project in: {outputDir}");
        Console.WriteLine($"   Project: {config.Name} ({allComponents.Count} components)");
    }
    
    private async Task CreateProjectStructureAsync(string outputDir)
    {
        // Create necessary directories matching standard Blazor structure
        Directory.CreateDirectory(Path.Combine(outputDir, "Components"));
        Directory.CreateDirectory(Path.Combine(outputDir, "Components", "Pages"));
        Directory.CreateDirectory(Path.Combine(outputDir, "Components", "Layout"));
        Directory.CreateDirectory(Path.Combine(outputDir, "wwwroot"));
        Directory.CreateDirectory(Path.Combine(outputDir, "wwwroot", "css"));
        Directory.CreateDirectory(Path.Combine(outputDir, "wwwroot", "js"));
    }
    
    private async Task GenerateBlazorComponentsAsync(ProgramNode ast, string outputDir)
    {
        var allComponentCSS = new StringBuilder();
        var components = ast.Statements.OfType<ComponentDeclaration>().ToList();

        // Generate components
        foreach (var component in components)
        {
            // Generate direct ComponentBase class (explicit over implicit)
            var blazorCode = _blazorGenerator.GenerateBlazorComponent(component);

            // Write to Components/Pages directory to match namespace
            var componentPath = Path.Combine(outputDir, "Components", "Pages", $"{component.Name}.cs");
            await File.WriteAllTextAsync(componentPath, blazorCode);

            // Generate semantic CSS for this component
            var componentCSS = _blazorGenerator.GenerateComponentCSS(component);
            Console.WriteLine($"   Generated CSS for {component.Name}: {componentCSS.Length} characters");
            allComponentCSS.AppendLine($"/* Component: {component.Name} */");
            allComponentCSS.AppendLine(componentCSS);
            allComponentCSS.AppendLine();
        }

        // Write the combined CSS file
        var cssPath = Path.Combine(outputDir, "wwwroot", "css", "components.css");
        await File.WriteAllTextAsync(cssPath, allComponentCSS.ToString());

        // Generate the main App.razor and Routes.razor (multi-component support)
        await GenerateAppRazorAsync(outputDir, components);

        // Copy demo files to wwwroot for serving
        await CopyDemoFilesAsync(outputDir);

        // Create a static HTML showcase for the first component (optional)
        await CreateBlazorComponentRouteAsync(outputDir, ast);
    }
    
    /// <summary>
    /// Generates Blazor components with project configuration support
    /// </summary>
    private async Task GenerateBlazorComponentsWithConfigAsync(ProgramNode ast, string outputDir, UIProjectConfig config)
    {
        var allComponentCSS = new StringBuilder();
        var components = ast.Statements.OfType<ComponentDeclaration>().ToList();

        // Generate components
        foreach (var component in components)
        {
            // Generate direct ComponentBase class (explicit over implicit)
            var blazorCode = _blazorGenerator.GenerateBlazorComponent(component);

            // Write to Components/Pages directory to match namespace
            var componentPath = Path.Combine(outputDir, "Components", "Pages", $"{component.Name}.cs");
            await File.WriteAllTextAsync(componentPath, blazorCode);

            // Generate semantic CSS for this component
            var componentCSS = _blazorGenerator.GenerateComponentCSS(component);
            Console.WriteLine($"   Generated CSS for {component.Name}: {componentCSS.Length} characters");
            allComponentCSS.AppendLine($"/* Component: {component.Name} */");
            allComponentCSS.AppendLine(componentCSS);
            allComponentCSS.AppendLine();
        }

        // Write the combined CSS file
        var cssPath = Path.Combine(outputDir, "wwwroot", "css", "components.css");
        await File.WriteAllTextAsync(cssPath, allComponentCSS.ToString());

        // Generate the main App.razor and Routes.razor with project configuration
        await GenerateAppRazorWithConfigAsync(outputDir, components, config);

        // Copy demo files to wwwroot for serving
        await CopyDemoFilesAsync(outputDir);

        // Create a static HTML showcase for the first component (optional)
        await CreateBlazorComponentRouteAsync(outputDir, ast);
    }
    
    // Restored: single-component overload; emits Razor files strictly as verbatim strings
    private async Task GenerateAppRazorAsync(string outputDir, List<ComponentDeclaration> components)
    {
        // Minimal App.razor for modern Blazor Web App
        var appContent = @"@using Microsoft.AspNetCore.Components.Web
@using CadenzaWebApp.Components

<!DOCTYPE html>
<html lang=""en"">

<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>Cadenza App</title>
    <base href=""/"" />
    <link rel=""stylesheet"" href=""css/components.css"" />
    <style>
        #blazor-error-ui {
            background: lightyellow;
            bottom: 0;
            box-shadow: 0 -1px 2px rgba(0, 0, 0, 0.2);
            display: none;
            left: 0;
            padding: 0.6rem 1.25rem 0.7rem 1.25rem;
            position: fixed;
            width: 100%;
            z-index: 1000;
        }
        #blazor-error-ui .dismiss {
            cursor: pointer;
            position: absolute;
            right: 0.75rem;
            top: 0.5rem;
        }
        .component-navigation {
            padding: 2rem;
            text-align: center;
        }
        .component-list {
            list-style: none;
            padding: 0;
        }
        .component-list li {
            margin: 1rem 0;
        }
        .component-list a {
            color: #0066cc;
            text-decoration: none;
            font-size: 1.2rem;
        }
        .component-list a:hover {
            text-decoration: underline;
        }
    </style>
    <HeadOutlet />
</head>

<body>
    <Routes />

    <div id=""blazor-error-ui"">
        An unhandled error has occurred.
        <a href="""" class=""reload"">Reload</a>
        <a class=""dismiss"">🗙</a>
    </div>

    <script src=""_framework/blazor.web.js""></script>
</body>

</html>";
        var appPath = Path.Combine(outputDir, "App.razor");
        await File.WriteAllTextAsync(appPath, appContent);

        // Routes.razor – dynamic router based on all discovered components
        var routeConditions = new StringBuilder();
        var defaultComponent = components.FirstOrDefault()?.Name ?? "Counter";
        
        // Generate routing logic
        // Handle root route first
        routeConditions.AppendLine("            @if (NavigationManager.ToBaseRelativePath(NavigationManager.Uri) == \"\")");
        routeConditions.AppendLine("            {");
        
        if (components.Count > 1)
        {
            // Multiple components - show navigation
            routeConditions.AppendLine("                <div class=\"component-navigation\">");
            routeConditions.AppendLine("                    <h1>Cadenza Components</h1>");
            routeConditions.AppendLine("                    <p>Select a component to view:</p>");
            routeConditions.AppendLine("                    <ul class=\"component-list\">");
            
            foreach (var component in components)
            {
                var componentRoute = component.Name.ToLowerInvariant();
                routeConditions.AppendLine($"                        <li><a href=\"/{componentRoute}\">{component.Name}</a></li>");
            }
            
            routeConditions.AppendLine("                    </ul>");
            routeConditions.AppendLine("                </div>");
        }
        else if (components.Count == 1)
        {
            // Single component - show on root route
            var singleComponent = components.First();
            routeConditions.AppendLine($"                <{singleComponent.Name} @rendermode=\"InteractiveServer\" />");
        }
        else
        {
            routeConditions.AppendLine("                <div>No components found</div>");
        }
        
        routeConditions.AppendLine("            }");
        
        // Generate route conditions for each component
        foreach (var component in components)
        {
            var componentRoute = component.Name.ToLowerInvariant();
            routeConditions.AppendLine($"            else if (NavigationManager.ToBaseRelativePath(NavigationManager.Uri) == \"{componentRoute}\")");
            routeConditions.AppendLine("            {");
            routeConditions.AppendLine($"                <{component.Name} @rendermode=\"InteractiveServer\" />");
            routeConditions.AppendLine("            }");
        }
        
        // Handle unknown routes
        routeConditions.AppendLine("            else");
        routeConditions.AppendLine("            {");
        routeConditions.AppendLine("                <div class=\"component-navigation\">");
        routeConditions.AppendLine("                    <h1>404 - Component Not Found</h1>");
        routeConditions.AppendLine("                    <p><a href=\"/\">Go back to home</a></p>");
        routeConditions.AppendLine("                </div>");
        routeConditions.AppendLine("            }");
        
        var routesContent = $@"@using CadenzaWebApp.Components.Pages
@using CadenzaWebApp.Components.Layout
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Components.Web.Virtualization
@inject NavigationManager NavigationManager

<Router AppAssembly=""typeof(CadenzaWebApp.App).Assembly"">
    <Found Context=""routeData"">
        <LayoutView Layout=""typeof(CadenzaWebApp.Components.Layout.MainLayout)"">
{routeConditions}
        </LayoutView>
    </Found>
    <NotFound>
        <PageTitle>Not found</PageTitle>
        <LayoutView Layout=""typeof(CadenzaWebApp.Components.Layout.MainLayout)"">
            <div class=""page"" role=""main"">
                <div style=""padding: 2rem; text-align: center;"">
                    <h1>404 - Page Not Found</h1>
                    <p>The requested page could not be found.</p>
                    <a href=""/"" style=""color: #0066cc;"">Go Home</a>
                </div>
            </div>
        </LayoutView>
    </NotFound>
</Router>";
        var routesPath = Path.Combine(outputDir, "Components", "Routes.razor");
        await File.WriteAllTextAsync(routesPath, routesContent);

        Console.WriteLine($"🔧 Generated App.razor and Routes.razor (supporting {components.Count} component(s))");
    }
    
    /// <summary>
    /// Generates App.razor and Routes.razor with project configuration support
    /// </summary>
    private async Task GenerateAppRazorWithConfigAsync(string outputDir, List<ComponentDeclaration> components, UIProjectConfig config)
    {
        // Use project name and description in the title
        var projectTitle = !string.IsNullOrEmpty(config.Name) ? config.Name : "Cadenza App";
        var projectDescription = config.Description;
        
        // Minimal App.razor for modern Blazor Web App with project info
        var appContent = $@"@using Microsoft.AspNetCore.Components.Web
@using CadenzaWebApp.Components

<!DOCTYPE html>
<html lang=""en"">

<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>{projectTitle}</title>
    <base href=""/"" />
    <link rel=""stylesheet"" href=""css/components.css"" />
    <style>
        #blazor-error-ui {{
            background: lightyellow;
            bottom: 0;
            box-shadow: 0 -1px 2px rgba(0, 0, 0, 0.2);
            display: none;
            left: 0;
            padding: 0.6rem 1.25rem 0.7rem 1.25rem;
            position: fixed;
            width: 100%;
            z-index: 1000;
        }}
        #blazor-error-ui .dismiss {{
            cursor: pointer;
            position: absolute;
            right: 0.75rem;
            top: 0.5rem;
        }}
        .component-navigation {{
            padding: 2rem;
            text-align: center;
        }}
        .component-list {{
            list-style: none;
            padding: 0;
        }}
        .component-list li {{
            margin: 1rem 0;
        }}
        .component-list a {{
            color: #0066cc;
            text-decoration: none;
            font-size: 1.2rem;
        }}
        .component-list a:hover {{
            text-decoration: underline;
        }}
    </style>
    <HeadOutlet />
</head>

<body>
    <Routes />

    <div id=""blazor-error-ui"">
        An unhandled error has occurred.
        <a href="""" class=""reload"">Reload</a>
        <a class=""dismiss"">🗙</a>
    </div>

    <script src=""_framework/blazor.web.js""></script>
</body>

</html>";
        var appPath = Path.Combine(outputDir, "App.razor");
        await File.WriteAllTextAsync(appPath, appContent);

        // Routes.razor – dynamic router based on project configuration
        var routeConditions = new StringBuilder();
        var homeComponent = !string.IsNullOrEmpty(config.UI.Navigation.HomeComponent) 
            ? config.UI.Navigation.HomeComponent 
            : components.FirstOrDefault()?.Name ?? "Counter";
        
        // Generate routing logic
        // Handle root route first
        routeConditions.AppendLine("            @if (NavigationManager.ToBaseRelativePath(NavigationManager.Uri) == \"\")");
        routeConditions.AppendLine("            {");
        
        if (config.UI.Navigation.ShowNavigation && components.Count > 1)
        {
            // Multiple components with navigation enabled - show navigation
            routeConditions.AppendLine("                <div class=\"component-navigation\">");
            routeConditions.AppendLine($"                    <h1>{projectTitle}</h1>");
            if (!string.IsNullOrEmpty(projectDescription))
            {
                routeConditions.AppendLine($"                    <p>{projectDescription}</p>");
            }
            routeConditions.AppendLine("                    <p>Select a component to view:</p>");
            routeConditions.AppendLine("                    <ul class=\"component-list\">");
            
            foreach (var component in components)
            {
                var componentRoute = component.Name.ToLowerInvariant();
                routeConditions.AppendLine($"                        <li><a href=\"/{componentRoute}\">{component.Name}</a></li>");
            }
            
            routeConditions.AppendLine("                    </ul>");
            routeConditions.AppendLine("                </div>");
        }
        else if (components.Count == 1 || !config.UI.Navigation.ShowNavigation)
        {
            // Single component or navigation disabled - show home component directly
            var targetComponent = components.FirstOrDefault(c => c.Name == homeComponent) ?? components.First();
            routeConditions.AppendLine($"                <{targetComponent.Name} @rendermode=\"InteractiveServer\" />");
        }
        else
        {
            routeConditions.AppendLine("                <div>No components found</div>");
        }
        
        routeConditions.AppendLine("            }");
        
        // Generate route conditions for each component
        foreach (var component in components)
        {
            var componentRoute = component.Name.ToLowerInvariant();
            routeConditions.AppendLine($"            else if (NavigationManager.ToBaseRelativePath(NavigationManager.Uri) == \"{componentRoute}\")");
            routeConditions.AppendLine("            {");
            routeConditions.AppendLine($"                <{component.Name} @rendermode=\"InteractiveServer\" />");
            routeConditions.AppendLine("            }");
        }
        
        // Handle unknown routes
        routeConditions.AppendLine("            else");
        routeConditions.AppendLine("            {");
        routeConditions.AppendLine("                <div class=\"component-navigation\">");
        routeConditions.AppendLine("                    <h1>404 - Component Not Found</h1>");
        routeConditions.AppendLine("                    <p><a href=\"/\">Go back to home</a></p>");
        routeConditions.AppendLine("                </div>");
        routeConditions.AppendLine("            }");
        
        var routesContent = $@"@using CadenzaWebApp.Components.Pages
@using CadenzaWebApp.Components.Layout
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Components.Web.Virtualization
@inject NavigationManager NavigationManager

<Router AppAssembly=""typeof(CadenzaWebApp.App).Assembly"">
    <Found Context=""routeData"">
        <LayoutView Layout=""typeof(CadenzaWebApp.Components.Layout.MainLayout)"">
{routeConditions}
        </LayoutView>
    </Found>
    <NotFound>
        <PageTitle>Not found</PageTitle>
        <LayoutView Layout=""typeof(CadenzaWebApp.Components.Layout.MainLayout)"">
            <div class=""page"" role=""main"">
                <div style=""padding: 2rem; text-align: center;"">
                    <h1>404 - Page Not Found</h1>
                    <p>The requested page could not be found.</p>
                    <a href=""/"" style=""color: #0066cc;"">Go Home</a>
                </div>
            </div>
        </LayoutView>
    </NotFound>
</Router>";
        var routesPath = Path.Combine(outputDir, "Components", "Routes.razor");
        await File.WriteAllTextAsync(routesPath, routesContent);

        Console.WriteLine($"🔧 Generated App.razor and Routes.razor for project '{projectTitle}' (supporting {components.Count} component(s))");
    }
    
    // Removed hardcoded Home component generation. Root behavior is handled by Routes.razor.
    
    private async Task GenerateProjectFilesAsync(string outputDir)
    {
        // Generate MainLayout.razor for modern Blazor Web App
        var layoutContent = @"@inherits LayoutComponentBase

<div class=""page"">
    <main>
        @Body
    </main>
</div>";
        
        var layoutPath = Path.Combine(outputDir, "Components", "Layout", "MainLayout.razor");
        await File.WriteAllTextAsync(layoutPath, layoutContent);
        
        // Generate Program.cs with interactive server rendering
        var programContent = @"using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using CadenzaWebApp;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(""/Error"");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<CadenzaWebApp.App>()
    .AddInteractiveServerRenderMode();

app.Run();";
        
        var programPath = Path.Combine(outputDir, "Program.cs");
        await File.WriteAllTextAsync(programPath, programContent);
        
        // Generate project file for modern Blazor Web App
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk.Web"">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

</Project>";
        
        var projectPath = Path.Combine(outputDir, "CadenzaWebApp.csproj");
        await File.WriteAllTextAsync(projectPath, projectContent);
        
        // Generate enhanced base CSS with semantic design system
        var cssContent = @"/* Cadenza Base Styles */
html, body {
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
    line-height: 1.6;
    margin: 0;
    padding: 0;
    background-color: var(--color-background, #ffffff);
    color: var(--color-text, #1f2937);
}

*, *::before, *::after {
    box-sizing: border-box;
}

.page {
    position: relative;
    display: flex;
    flex-direction: column;
    min-height: 100vh;
}

main {
    flex: 1;
    padding: var(--spacing-md, 1rem);
}

/* Typography */
h1, h2, h3, h4, h5, h6 {
    margin: 0 0 var(--spacing-md, 1rem) 0;
    font-weight: 600;
    line-height: 1.25;
    color: var(--color-text, #1f2937);
}

h1 { font-size: var(--font-size-3xl, 1.875rem); }
h2 { font-size: var(--font-size-2xl, 1.5rem); }
h3 { font-size: var(--font-size-xl, 1.25rem); }
h4 { font-size: var(--font-size-lg, 1.125rem); }
h5 { font-size: var(--font-size-base, 1rem); }
h6 { font-size: var(--font-size-sm, 0.875rem); }

p {
    margin: 0 0 var(--spacing-md, 1rem) 0;
}

/* Interactive elements */
button {
    font-family: inherit;
    cursor: pointer;
    border: none;
    outline: none;
    transition: all 0.2s ease-in-out;
}

button:focus-visible {
    outline: 2px solid var(--color-focus, #3b82f6);
    outline-offset: 2px;
}

button:disabled {
    cursor: not-allowed;
    opacity: 0.6;
}

/* Utility classes for immediate use */
.container {
    max-width: 1200px;
    margin: 0 auto;
    padding: 0 var(--spacing-md, 1rem);
}

.sr-only {
    position: absolute;
    width: 1px;
    height: 1px;
    padding: 0;
    margin: -1px;
    overflow: hidden;
    clip: rect(0, 0, 0, 0);
    white-space: nowrap;
    border: 0;
}";
        
        var cssPath = Path.Combine(outputDir, "wwwroot", "css", "site.css");
        await File.WriteAllTextAsync(cssPath, cssContent);

        var importsContent = @"@using System.Net.Http
@using System.Net.Http.Json
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using static Microsoft.AspNetCore.Components.Web.RenderMode
@using Microsoft.AspNetCore.Components.Web.Virtualization
@using Microsoft.JSInterop
@using CadenzaWebApp
@using CadenzaWebApp.Components";
        var importsPath = Path.Combine(outputDir, "Components", "_Imports.razor");
        await File.WriteAllTextAsync(importsPath, importsContent);
    }
    
    private async Task CopyDemoFilesAsync(string outputDir)
    {
        // Create working counter app HTML file
        var counterAppContent = GenerateWorkingCounterApp();
        var counterPath = Path.Combine(outputDir, "wwwroot", "working_counter_app.html");
        await File.WriteAllTextAsync(counterPath, counterAppContent);
        
        // Create demo page
        var demoContent = GenerateDemoPage();
        var demoPath = Path.Combine(outputDir, "wwwroot", "cadenza_demo.html");
        await File.WriteAllTextAsync(demoPath, demoContent);
        
        Console.WriteLine($"   Added demo files: working_counter_app.html, cadenza_demo.html");
    }
    
    private string GenerateWorkingCounterApp()
    {
        return @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Cadenza Counter App</title>
    <link href=""css/site.css"" rel=""stylesheet"" />
    <link href=""css/components.css"" rel=""stylesheet"" />
    <style>
        .cadenza-counter-container {
            padding: var(--spacing-xl, 2rem);
            background-color: var(--color-background-alt, #f9fafb);
            display: flex;
            align-items: center;
            justify-content: center;
            flex-direction: column;
            min-height: 100vh;
            border-radius: 0.5rem;
            border: 2px solid var(--color-border, #d1d5db);
            max-width: 600px;
            margin: var(--spacing-lg, 1.5rem) auto;
        }
        .cadenza-counter-title {
            color: var(--color-primary, #3b82f6);
            margin: var(--spacing-lg, 1.5rem);
            font-size: 2.5rem;
            font-weight: 700;
            text-align: center;
        }
        .cadenza-counter-buttons {
            display: flex;
            gap: var(--spacing-md, 1rem);
            margin: var(--spacing-lg, 1.5rem) 0;
        }
        .btn-primary, .btn-secondary {
            padding: var(--spacing-sm, 0.5rem) var(--spacing-lg, 1.5rem);
            border: none;
            border-radius: 0.375rem;
            cursor: pointer;
            font-weight: 500;
            font-size: 1rem;
            transition: all 0.2s ease-in-out;
            min-width: 120px;
        }
        .btn-primary {
            background-color: var(--color-primary, #3b82f6);
            color: var(--color-primary-text, #ffffff);
        }
        .btn-primary:hover {
            background-color: #2563eb;
            transform: translateY(-1px);
        }
        .btn-secondary {
            background-color: var(--color-secondary, #6b7280);
            color: var(--color-secondary-text, #ffffff);
        }
        .btn-secondary:hover {
            background-color: #4b5563;
        }
        .text-success { color: var(--color-success, #10b981); font-weight: 600; }
        .text-warning { color: var(--color-warning, #f59e0b); font-weight: 600; }
        .text-danger { color: var(--color-danger, #ef4444); font-weight: 600; }
    </style>
</head>
<body>
    <div class=""cadenza-counter-container"">
        <h1 class=""cadenza-counter-title"" id=""counter-display"">Counter: 0</h1>
        <div class=""cadenza-counter-buttons"">
            <button class=""btn-primary"" onclick=""increment()"">Increment</button>
            <button class=""btn-secondary"" onclick=""decrement()"">Decrement</button>
        </div>
        <div id=""status"" class=""text-success"">Status: Normal</div>
    </div>
    <script>
        let count = 0;
        function increment() { count++; update(); }
        function decrement() { count--; update(); }
        function update() {
            document.getElementById('counter-display').textContent = `Counter: ${count}`;
            const status = document.getElementById('status');
            if (count > 10) {
                status.className = 'text-warning';
                status.textContent = 'Status: High';
            } else if (count < 0) {
                status.className = 'text-danger';
                status.textContent = 'Status: Negative';
            } else {
                status.className = 'text-success';
                status.textContent = 'Status: Normal';
            }
        }
        console.log('✅ Cadenza Counter App loaded successfully!');
    </script>
</body>
</html>";
    }
    
    private async Task CreateBlazorComponentRouteAsync(string outputDir, ProgramNode ast)
    {
        var components = ast.Statements.OfType<ComponentDeclaration>().ToList();
        
        if (components.Any())
        {
            var firstComponent = components.First();
            
            // Create a static HTML version that shows the generated Blazor component code
            var blazorShowcaseContent = $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Generated Blazor Component - {firstComponent.Name}</title>
    <link href=""css/site.css"" rel=""stylesheet"" />
    <link href=""css/components.css"" rel=""stylesheet"" />
    <style>
        .code-container {{ 
            background: #f8f9fa; 
            padding: 1.5rem; 
            border-radius: 0.5rem; 
            border: 1px solid #e9ecef; 
            margin: 1rem 0; 
            font-family: 'Courier New', monospace; 
            font-size: 0.875rem; 
            white-space: pre-wrap;
            max-height: 400px;
            overflow-y: auto;
        }}
        .info-box {{ 
            background: var(--color-primary, #3b82f6); 
            color: white; 
            padding: 1rem; 
            border-radius: 0.375rem; 
            margin: 1rem 0; 
        }}
    </style>
</head>
<body style=""padding: 2rem; font-family: system-ui, -apple-system, sans-serif;"">
    <h1>🔧 Generated Blazor Component: {firstComponent.Name}</h1>
    
    <div class=""info-box"">
        <strong>✅ Component Generated Successfully!</strong><br>
        Generated from: <code>{Path.GetFileName(_currentComponentPathPlaceholder)}</code><br>
        Location: <code>Components/{firstComponent.Name}.cs</code>
    </div>
    
    <h2>📄 Generated C# Blazor Component</h2>
    <div class=""code-container"">
        The generated Blazor component is located at:<br>
        <code>Components/{firstComponent.Name}.cs</code><br><br>
        This component includes:<br>
        • State management for counter value<br>
        • Event handlers for increment/decrement<br>
        • Semantic styling classes applied<br>
        • Blazor render tree generation
    </div>
    
    <h2>🎨 Generated CSS</h2>
    <div class=""code-container"" style=""max-height: 200px;"" id=""css-code"">Loading CSS...</div>
    
    <h2>🚀 Working Demos</h2>
    <p>You can also use these demos:</p>
    <ul>
        <li><a href=""working_counter_app.html"">📱 Working Counter App (HTML/JS)</a></li>
        <li><a href=""cadenza_demo.html"">🎨 Semantic Styling Demo</a></li>
        <li><a href=""css/components.css"">📄 Generated CSS File</a></li>
    </ul>

    <script>            
        // Load and display the generated CSS
        fetch('css/components.css')
            .then(response => response.text())
            .then(css => {{
                document.getElementById('css-code').textContent = css.substring(0, 1000) + '\\n\\n... (truncated, ' + css.length + ' total characters)';
            }})
            .catch(() => {{
                document.getElementById('css-code').textContent = 'CSS loading failed';
            }});
            
        console.log('✅ Blazor component showcase loaded');
    </script>
</body>
</html>";

            var showcasePath = Path.Combine(outputDir, "wwwroot", "blazor_component.html");
            await File.WriteAllTextAsync(showcasePath, blazorShowcaseContent);
            
            Console.WriteLine($"   Added Blazor component showcase: blazor_component.html");
        }
    }
    
    private string _currentComponentPathPlaceholder => "examples/component.cdz";
    
    private string GenerateDemoPage()
    {
        return @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Cadenza Semantic Styling Demo</title>
    <link href=""css/site.css"" rel=""stylesheet"" />
    <link href=""css/components.css"" rel=""stylesheet"" />
    <style>
        .demo-section {
            margin-bottom: 2rem;
            padding: 1.5rem;
            border: 1px solid #e5e7eb;
            border-radius: 0.5rem;
        }
        .color-demo {
            display: flex;
            gap: 0.5rem;
            flex-wrap: wrap;
            margin-top: 1rem;
        }
        .color-swatch {
            width: 4rem;
            height: 4rem;
            border-radius: 0.375rem;
            display: flex;
            align-items: center;
            justify-content: center;
            color: white;
            font-size: 0.75rem;
            font-weight: 500;
            text-align: center;
        }
    </style>
</head>
<body>
    <div style=""padding: 2rem;"">
        <h1>Cadenza Semantic Styling System Demo</h1>
        <p>This demonstrates the generated CSS from the Cadenza semantic styling system.</p>
        
        <div class=""demo-section"">
            <h2>Design System Colors</h2>
            <div class=""color-demo"">
                <div class=""color-swatch"" style=""background: var(--color-primary, #3b82f6)"">Primary</div>
                <div class=""color-swatch"" style=""background: var(--color-secondary, #6b7280)"">Secondary</div>
                <div class=""color-swatch"" style=""background: var(--color-success, #10b981)"">Success</div>
                <div class=""color-swatch"" style=""background: var(--color-warning, #f59e0b); color: black;"">Warning</div>
                <div class=""color-swatch"" style=""background: var(--color-danger, #ef4444)"">Danger</div>
            </div>
        </div>
        
        <div class=""demo-section"">
            <h2>Generated CSS Status</h2>
            <div style=""padding: 1rem; background: var(--color-success, #10b981); color: white; border-radius: 0.375rem; margin-bottom: 1rem;"">
                ✅ CSS generated and served
            </div>
      </body>
      </html>";
    }
}

// =============================================================================
// HOT RELOAD SERVICE
// =============================================================================

/// <summary>
/// Service for handling hot reload functionality
/// </summary>
public class CadenzaHotReloadService
{
    private readonly List<Func<Task>> _reloadCallbacks = new();
    
    public void RegisterReloadCallback(Func<Task> callback)
    {
        _reloadCallbacks.Add(callback);
    }
    
    public async Task TriggerReloadAsync()
    {
        foreach (var callback in _reloadCallbacks)
        {
            try
            {
                await callback();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hot reload callback failed: {ex.Message}");
            }
        }
    }
}

// =============================================================================
// PROJECT CONFIGURATION CLASSES
// =============================================================================

/// <summary>
/// Project configuration for UI project serving
/// </summary>
public class UIProjectConfig
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "1.0.0";
    public string Description { get; set; } = "";
    public UIBuildConfig Build { get; set; } = new();
    public UINavigationConfig UI { get; set; } = new();
}

/// <summary>
/// Build configuration for UI projects
/// </summary>
public class UIBuildConfig
{
    public string Source { get; set; } = "components/";
    public string OutputType { get; set; } = "webapp";
    public string Target { get; set; } = "blazor";
}

/// <summary>
/// UI navigation configuration
/// </summary>
public class UINavigationConfig
{
    public NavigationConfig Navigation { get; set; } = new();
}

/// <summary>
/// Navigation settings
/// </summary>
public class NavigationConfig
{
    public bool ShowNavigation { get; set; } = true;
    public string HomeComponent { get; set; } = "";
}