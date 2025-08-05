// Cadenza Core Compiler - Blazor Project Generator
// Complete Blazor project generation from Cadenza components

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Cadenza.Core;

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
        await ProjectTemplates.GenerateProjectFilesAsync(outputDir);
        
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
        await ProjectTemplates.GenerateProjectFilesAsync(outputDir);
        
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
            // For files with multiple components, don't include RouteAttribute - let Routes.razor handle routing
            var includeRouteAttribute = components.Count == 1;  // Only use RouteAttribute for single components
            var blazorCode = _blazorGenerator.GenerateBlazorComponent(component, includeRouteAttribute);

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
        await ProjectTemplates.CopyDemoFilesAsync(outputDir);
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
            // For multi-component projects, include RouteAttribute for proper Blazor routing
            var blazorCode = _blazorGenerator.GenerateBlazorComponent(component, includeRouteAttribute: true);

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

        // Generate Home component for navigation if we have multiple components
        if (components.Count > 1)
        {
            await GenerateHomeComponentAsync(outputDir, components, config);
        }

        // Write the combined CSS file
        var cssPath = Path.Combine(outputDir, "wwwroot", "css", "components.css");
        await File.WriteAllTextAsync(cssPath, allComponentCSS.ToString());

        // Generate the main App.razor and Routes.razor with project configuration
        await GenerateAppRazorWithConfigAsync(outputDir, components, config);

        // Copy demo files to wwwroot for serving
        await ProjectTemplates.CopyDemoFilesAsync(outputDir);
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

        // Routes.razor – manual routing with auto-generated component routes
        var routeConditions = new StringBuilder();
        
        // Generate route for Home component (root)
        routeConditions.AppendLine("            @if (NavigationManager.ToBaseRelativePath(NavigationManager.Uri) == \"\")");
        routeConditions.AppendLine("            {");
        routeConditions.AppendLine("                <Home @rendermode=\"InteractiveServer\" />");
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
    
    /// <summary>
    /// Generates a Home component for multi-component navigation
    /// </summary>
    private async Task GenerateHomeComponentAsync(string outputDir, List<ComponentDeclaration> components, UIProjectConfig config)
    {
        var projectTitle = !string.IsNullOrEmpty(config.Name) ? config.Name : "Cadenza Components";
        var projectDescription = config.Description;
        
        var homeComponentCode = $@"// <auto-generated>
// This file was generated by the Cadenza compiler. Do not edit manually.
// Home component for multi-component navigation

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace CadenzaWebApp.Components.Pages
{{
    [Microsoft.AspNetCore.Components.RouteAttribute(""/"")]
    public class Home : ComponentBase
    {{
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {{
            builder.OpenElement(0, ""div"");
            builder.AddAttribute(1, ""class"", ""component-navigation"");
            
            builder.OpenElement(2, ""h1"");
            builder.AddContent(3, ""{projectTitle}"");
            builder.CloseElement();
            
            {(string.IsNullOrEmpty(projectDescription) ? "" : $@"builder.OpenElement(4, ""p"");
            builder.AddContent(5, ""{projectDescription}"");
            builder.CloseElement();
            ")}
            
            builder.OpenElement(6, ""p"");
            builder.AddContent(7, ""Select a component to view:"");
            builder.CloseElement();
            
            builder.OpenElement(8, ""ul"");
            builder.AddAttribute(9, ""class"", ""component-list"");";

        // Generate navigation links for each component
        int elementIndex = 10;
        foreach (var component in components)
        {
            var componentRoute = component.Name.ToLowerInvariant();
            homeComponentCode += $@"
            
            builder.OpenElement({elementIndex++}, ""li"");
            builder.OpenElement({elementIndex++}, ""a"");
            builder.AddAttribute({elementIndex++}, ""href"", ""/{componentRoute}"");
            builder.AddContent({elementIndex++}, ""{component.Name}"");
            builder.CloseElement();
            builder.CloseElement();";
        }
        
        homeComponentCode += $@"
            
            builder.CloseElement(); // ul
            builder.CloseElement(); // div
        }}
    }}
}}";
        
        var homePath = Path.Combine(outputDir, "Components", "Pages", "Home.cs");
        await File.WriteAllTextAsync(homePath, homeComponentCode);
        
        Console.WriteLine($"   Generated Home component for navigation");
    }
}