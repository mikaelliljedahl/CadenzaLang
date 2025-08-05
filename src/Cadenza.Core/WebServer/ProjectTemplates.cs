// Cadenza Core Compiler - Project Templates
// Static content generation for Blazor projects

using System.Threading.Tasks;
using System.IO;

namespace Cadenza.Core;

// =============================================================================
// PROJECT TEMPLATES
// =============================================================================

/// <summary>
/// Provides static content generation for Blazor project templates
/// </summary>
public static class ProjectTemplates
{
    /// <summary>
    /// Generates the working counter app HTML demo
    /// </summary>
    public static string GenerateWorkingCounterApp()
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

    /// <summary>
    /// Generates the semantic styling demo page
    /// </summary>
    public static string GenerateDemoPage()
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
        </div>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Generates the MainLayout.razor content
    /// </summary>
    public static string GenerateMainLayoutRazor()
    {
        return @"@inherits LayoutComponentBase

<div class=""page"">
    <main>
        @Body
    </main>
</div>";
    }

    /// <summary>
    /// Generates the Program.cs content for modern Blazor Web App
    /// </summary>
    public static string GenerateProgramCs()
    {
        return @"using Microsoft.AspNetCore.Components;
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
    }

    /// <summary>
    /// Generates the .csproj content for modern Blazor Web App
    /// </summary>
    public static string GenerateProjectFile()
    {
        return @"<Project Sdk=""Microsoft.NET.Sdk.Web"">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

</Project>";
    }

    /// <summary>
    /// Generates the base CSS with semantic design system
    /// </summary>
    public static string GenerateBaseCss()
    {
        return @"/* Cadenza Base Styles */
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
    }

    /// <summary>
    /// Generates the _Imports.razor content
    /// </summary>
    public static string GenerateImportsRazor()
    {
        return @"@using System.Net.Http
@using System.Net.Http.Json
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using static Microsoft.AspNetCore.Components.Web.RenderMode
@using Microsoft.AspNetCore.Components.Web.Virtualization
@using Microsoft.JSInterop
@using CadenzaWebApp
@using CadenzaWebApp.Components";
    }

    /// <summary>
    /// Copies all demo files to the output directory
    /// </summary>
    public static async Task CopyDemoFilesAsync(string outputDir)
    {
        // Create working counter app HTML file
        var counterAppContent = GenerateWorkingCounterApp();
        var counterPath = Path.Combine(outputDir, "wwwroot", "working_counter_app.html");
        await File.WriteAllTextAsync(counterPath, counterAppContent);
        
        // Create demo page
        var demoContent = GenerateDemoPage();
        var demoPath = Path.Combine(outputDir, "wwwroot", "cadenza_demo.html");
        await File.WriteAllTextAsync(demoPath, demoContent);
    }

    /// <summary>
    /// Generates all project files for a Blazor Web App
    /// </summary>
    public static async Task GenerateProjectFilesAsync(string outputDir)
    {
        // Generate MainLayout.razor for modern Blazor Web App
        var layoutContent = GenerateMainLayoutRazor();
        var layoutPath = Path.Combine(outputDir, "Components", "Layout", "MainLayout.razor");
        await File.WriteAllTextAsync(layoutPath, layoutContent);
        
        // Generate Program.cs with interactive server rendering
        var programContent = GenerateProgramCs();
        var programPath = Path.Combine(outputDir, "Program.cs");
        await File.WriteAllTextAsync(programPath, programContent);
        
        // Generate project file for modern Blazor Web App
        var projectContent = GenerateProjectFile();
        var projectPath = Path.Combine(outputDir, "CadenzaWebApp.csproj");
        await File.WriteAllTextAsync(projectPath, projectContent);
        
        // Generate enhanced base CSS with semantic design system
        var cssContent = GenerateBaseCss();
        var cssPath = Path.Combine(outputDir, "wwwroot", "css", "site.css");
        await File.WriteAllTextAsync(cssPath, cssContent);

        // Generate _Imports.razor
        var importsContent = GenerateImportsRazor();
        var importsPath = Path.Combine(outputDir, "Components", "_Imports.razor");
        await File.WriteAllTextAsync(importsPath, importsContent);
    }
}