// Cadenza Core Compiler - Semantic Styling System
// CSS-in-Cadenza with semantic tokens and automatic responsive behavior

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cadenza.Core;

// =============================================================================
// SEMANTIC STYLING SYSTEM
// =============================================================================

/// <summary>
/// Semantic styling system that converts Cadenza styling expressions to CSS
/// </summary>
public class CadenzaSemanticStyling
{
    private readonly DesignSystem _designSystem;
    private readonly Dictionary<string, SemanticClass> _generatedClasses;
    private readonly StringBuilder _cssOutput;
    
    public CadenzaSemanticStyling()
    {
        _designSystem = new DesignSystem();
        _generatedClasses = new Dictionary<string, SemanticClass>();
        _cssOutput = new StringBuilder();
    }
    
    /// <summary>
    /// Processes a component's styling and generates optimized CSS
    /// </summary>
    public string ProcessComponentStyling(ComponentDeclaration component)
    {
        _cssOutput.Clear();
        _generatedClasses.Clear();
        
        // Add base design system CSS
        GenerateDesignSystemCSS();
        
        // Process component-specific styling
        ProcessComponentStyleRules(component);
        
        // Generate component-scoped styles
        GenerateComponentStyles(component);
        
        return _cssOutput.ToString();
    }
    
    /// <summary>
    /// Converts a Cadenza style expression to CSS class name
    /// </summary>
    public string ConvertStyleExpression(string styleExpression, string componentName)
    {
        if (string.IsNullOrEmpty(styleExpression))
            return "";
            
        // Handle semantic style expressions
        if (IsSemanticExpression(styleExpression))
        {
            return ProcessSemanticExpression(styleExpression, componentName);
        }
        
        // Handle direct CSS class names
        return styleExpression;
    }
    
    private void GenerateDesignSystemCSS()
    {
        _cssOutput.AppendLine("/* Cadenza Design System */");
        _cssOutput.AppendLine(":root {");
        
        // Generate CSS custom properties for design tokens
        foreach (var token in _designSystem.GetAllTokens())
        {
            _cssOutput.AppendLine($"  --{token.Name}: {token.Value};");
        }
        
        _cssOutput.AppendLine("}");
        _cssOutput.AppendLine();
        
        // Generate utility classes
        GenerateUtilityClasses();
        
        // Generate responsive utilities
        GenerateResponsiveUtilities();
    }
    
    private void GenerateUtilityClasses()
    {
        _cssOutput.AppendLine("/* Utility Classes */");
        
        // Spacing utilities
        foreach (var spacing in _designSystem.Spacing)
        {
            _cssOutput.AppendLine($".m-{spacing.Key} {{ margin: var(--spacing-{spacing.Key}); }}");
            _cssOutput.AppendLine($".mt-{spacing.Key} {{ margin-top: var(--spacing-{spacing.Key}); }}");
            _cssOutput.AppendLine($".mr-{spacing.Key} {{ margin-right: var(--spacing-{spacing.Key}); }}");
            _cssOutput.AppendLine($".mb-{spacing.Key} {{ margin-bottom: var(--spacing-{spacing.Key}); }}");
            _cssOutput.AppendLine($".ml-{spacing.Key} {{ margin-left: var(--spacing-{spacing.Key}); }}");
            _cssOutput.AppendLine($".mx-{spacing.Key} {{ margin-left: var(--spacing-{spacing.Key}); margin-right: var(--spacing-{spacing.Key}); }}");
            _cssOutput.AppendLine($".my-{spacing.Key} {{ margin-top: var(--spacing-{spacing.Key}); margin-bottom: var(--spacing-{spacing.Key}); }}");
            
            _cssOutput.AppendLine($".p-{spacing.Key} {{ padding: var(--spacing-{spacing.Key}); }}");
            _cssOutput.AppendLine($".pt-{spacing.Key} {{ padding-top: var(--spacing-{spacing.Key}); }}");
            _cssOutput.AppendLine($".pr-{spacing.Key} {{ padding-right: var(--spacing-{spacing.Key}); }}");
            _cssOutput.AppendLine($".pb-{spacing.Key} {{ padding-bottom: var(--spacing-{spacing.Key}); }}");
            _cssOutput.AppendLine($".pl-{spacing.Key} {{ padding-left: var(--spacing-{spacing.Key}); }}");
            _cssOutput.AppendLine($".px-{spacing.Key} {{ padding-left: var(--spacing-{spacing.Key}); padding-right: var(--spacing-{spacing.Key}); }}");
            _cssOutput.AppendLine($".py-{spacing.Key} {{ padding-top: var(--spacing-{spacing.Key}); padding-bottom: var(--spacing-{spacing.Key}); }}");
        }
        
        // Color utilities
        foreach (var color in _designSystem.Colors)
        {
            _cssOutput.AppendLine($".text-{color.Key} {{ color: var(--color-{color.Key}); }}");
            _cssOutput.AppendLine($".bg-{color.Key} {{ background-color: var(--color-{color.Key}); }}");
            _cssOutput.AppendLine($".border-{color.Key} {{ border-color: var(--color-{color.Key}); }}");
        }
        
        // Typography utilities
        foreach (var fontSize in _designSystem.FontSizes)
        {
            _cssOutput.AppendLine($".text-{fontSize.Key} {{ font-size: var(--font-size-{fontSize.Key}); }}");
        }
        
        // Layout utilities
        _cssOutput.AppendLine(".flex { display: flex; }");
        _cssOutput.AppendLine(".flex-col { flex-direction: column; }");
        _cssOutput.AppendLine(".flex-row { flex-direction: row; }");
        _cssOutput.AppendLine(".items-center { align-items: center; }");
        _cssOutput.AppendLine(".justify-center { justify-content: center; }");
        _cssOutput.AppendLine(".justify-between { justify-content: space-between; }");
        _cssOutput.AppendLine(".gap-sm { gap: var(--spacing-sm); }");
        _cssOutput.AppendLine(".gap-md { gap: var(--spacing-md); }");
        _cssOutput.AppendLine(".gap-lg { gap: var(--spacing-lg); }");
        
        // Button utilities
        _cssOutput.AppendLine(".btn { padding: var(--spacing-sm) var(--spacing-md); border: 1px solid var(--color-border); border-radius: var(--border-radius-md); cursor: pointer; font-family: inherit; }");
        _cssOutput.AppendLine(".btn-primary { background-color: var(--color-primary); color: var(--color-primary-text); border-color: var(--color-primary); }");
        _cssOutput.AppendLine(".btn-secondary { background-color: var(--color-secondary); color: var(--color-secondary-text); border-color: var(--color-secondary); }");
        _cssOutput.AppendLine(".btn-danger { background-color: var(--color-danger); color: var(--color-danger-text); border-color: var(--color-danger); }");
        
        _cssOutput.AppendLine();
    }
    
    private void GenerateResponsiveUtilities()
    {
        _cssOutput.AppendLine("/* Responsive Utilities */");
        
        var breakpoints = _designSystem.Breakpoints;
        
        foreach (var breakpoint in breakpoints)
        {
            _cssOutput.AppendLine($"@media (min-width: {breakpoint.Value}) {{");
            
            // Generate responsive spacing
            foreach (var spacing in _designSystem.Spacing)
            {
                _cssOutput.AppendLine($"  .{breakpoint.Key}\\:m-{spacing.Key} {{ margin: var(--spacing-{spacing.Key}); }}");
                _cssOutput.AppendLine($"  .{breakpoint.Key}\\:p-{spacing.Key} {{ padding: var(--spacing-{spacing.Key}); }}");
            }
            
            // Generate responsive layout
            _cssOutput.AppendLine($"  .{breakpoint.Key}\\:flex {{ display: flex; }}");
            _cssOutput.AppendLine($"  .{breakpoint.Key}\\:flex-col {{ flex-direction: column; }}");
            _cssOutput.AppendLine($"  .{breakpoint.Key}\\:flex-row {{ flex-direction: row; }}");
            _cssOutput.AppendLine($"  .{breakpoint.Key}\\:hidden {{ display: none; }}");
            _cssOutput.AppendLine($"  .{breakpoint.Key}\\:block {{ display: block; }}");
            
            _cssOutput.AppendLine("}");
        }
        
        _cssOutput.AppendLine();
    }
    
    private bool IsSemanticExpression(string expression)
    {
        // Check if expression uses semantic tokens
        return expression.Contains("spacing(") ||
               expression.Contains("color(") ||
               expression.Contains("font(") ||
               expression.Contains("responsive(") ||
               expression.StartsWith("semantic:");
    }
    
    private string ProcessSemanticExpression(string expression, string componentName)
    {
        var className = $"cadenza-{componentName.ToLower()}-{_generatedClasses.Count}";
        
        // Parse semantic expression and generate CSS
        var cssRules = ParseSemanticExpression(expression);
        
        var semanticClass = new SemanticClass
        {
            Name = className,
            Expression = expression,
            CssRules = cssRules
        };
        
        _generatedClasses[className] = semanticClass;
        
        return className;
    }
    
    private List<string> ParseSemanticExpression(string expression)
    {
        var rules = new List<string>();
        
        // Handle semantic: prefix for complex styling
        if (expression.StartsWith("semantic:"))
        {
            var semanticPart = expression.Substring(9).Trim();
            rules.AddRange(ParseSemanticDirectives(semanticPart));
        }
        
        // Handle function-style semantic expressions
        rules.AddRange(ParseSemanticFunctions(expression));
        
        return rules;
    }
    
    private List<string> ParseSemanticDirectives(string directives)
    {
        var rules = new List<string>();
        var parts = directives.Split(',', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            
            // Handle spacing directives
            if (trimmed.StartsWith("padding:"))
            {
                var value = trimmed.Substring(8).Trim();
                if (_designSystem.Spacing.ContainsKey(value))
                {
                    rules.Add($"padding: var(--spacing-{value})");
                }
            }
            else if (trimmed.StartsWith("margin:"))
            {
                var value = trimmed.Substring(7).Trim();
                if (_designSystem.Spacing.ContainsKey(value))
                {
                    rules.Add($"margin: var(--spacing-{value})");
                }
            }
            // Handle color directives
            else if (trimmed.StartsWith("background:"))
            {
                var value = trimmed.Substring(11).Trim();
                if (_designSystem.Colors.ContainsKey(value))
                {
                    rules.Add($"background-color: var(--color-{value})");
                }
            }
            else if (trimmed.StartsWith("text:"))
            {
                var value = trimmed.Substring(5).Trim();
                if (_designSystem.Colors.ContainsKey(value))
                {
                    rules.Add($"color: var(--color-{value})");
                }
            }
            // Handle layout directives
            else if (trimmed == "flex")
            {
                rules.Add("display: flex");
            }
            else if (trimmed == "flex-column")
            {
                rules.Add("display: flex");
                rules.Add("flex-direction: column");
            }
            else if (trimmed == "center")
            {
                rules.Add("display: flex");
                rules.Add("align-items: center");
                rules.Add("justify-content: center");
            }
        }
        
        return rules;
    }
    
    private List<string> ParseSemanticFunctions(string expression)
    {
        var rules = new List<string>();
        
        // Handle spacing() function
        if (expression.Contains("spacing("))
        {
            var match = System.Text.RegularExpressions.Regex.Match(expression, @"spacing\(([^)]+)\)");
            if (match.Success)
            {
                var spacing = match.Groups[1].Value;
                if (_designSystem.Spacing.ContainsKey(spacing))
                {
                    rules.Add($"padding: var(--spacing-{spacing})");
                }
            }
        }
        
        // Handle color() function
        if (expression.Contains("color("))
        {
            var match = System.Text.RegularExpressions.Regex.Match(expression, @"color\(([^)]+)\)");
            if (match.Success)
            {
                var color = match.Groups[1].Value;
                if (_designSystem.Colors.ContainsKey(color))
                {
                    rules.Add($"color: var(--color-{color})");
                }
            }
        }
        
        return rules;
    }
    
    private void ProcessComponentStyleRules(ComponentDeclaration component)
    {
        // Look for style blocks in component
        // This would be extended to parse style: {} blocks in Cadenza syntax
    }
    
    private void GenerateComponentStyles(ComponentDeclaration component)
    {
        if (_generatedClasses.Count == 0)
            return;
            
        _cssOutput.AppendLine($"/* Component: {component.Name} */");
        
        foreach (var semanticClass in _generatedClasses.Values)
        {
            _cssOutput.AppendLine($".{semanticClass.Name} {{");
            
            foreach (var rule in semanticClass.CssRules)
            {
                _cssOutput.AppendLine($"  {rule};");
            }
            
            _cssOutput.AppendLine("}");
        }
        
        _cssOutput.AppendLine();
    }
}

// =============================================================================
// DESIGN SYSTEM
// =============================================================================

/// <summary>
/// Built-in design system with semantic tokens
/// </summary>
public class DesignSystem
{
    public Dictionary<string, string> Spacing { get; }
    public Dictionary<string, string> Colors { get; }
    public Dictionary<string, string> FontSizes { get; }
    public Dictionary<string, string> Breakpoints { get; }
    
    public DesignSystem()
    {
        Spacing = new Dictionary<string, string>
        {
            ["xs"] = "0.25rem", // 4px
            ["sm"] = "0.5rem",  // 8px
            ["md"] = "1rem",    // 16px
            ["lg"] = "1.5rem",  // 24px
            ["xl"] = "2rem",    // 32px
            ["2xl"] = "3rem",   // 48px
            ["3xl"] = "4rem",   // 64px
        };
        
        Colors = new Dictionary<string, string>
        {
            // Primary colors
            ["primary"] = "#3b82f6",       // Blue
            ["primary-text"] = "#ffffff",
            ["secondary"] = "#6b7280",     // Gray
            ["secondary-text"] = "#ffffff",
            
            // Semantic colors
            ["success"] = "#10b981",       // Green
            ["success-text"] = "#ffffff",
            ["warning"] = "#f59e0b",       // Amber
            ["warning-text"] = "#000000",
            ["danger"] = "#ef4444",        // Red
            ["danger-text"] = "#ffffff",
            ["info"] = "#3b82f6",          // Blue
            ["info-text"] = "#ffffff",
            
            // Neutral colors
            ["text"] = "#1f2937",          // Dark gray
            ["text-muted"] = "#6b7280",    // Medium gray
            ["text-light"] = "#9ca3af",    // Light gray
            ["background"] = "#ffffff",     // White
            ["background-alt"] = "#f9fafb", // Light gray
            ["border"] = "#d1d5db",        // Light gray
            ["border-light"] = "#e5e7eb",  // Very light gray
            
            // Interactive states
            ["hover"] = "#f3f4f6",         // Light gray
            ["active"] = "#e5e7eb",        // Medium gray
            ["focus"] = "#3b82f6",         // Blue
            ["disabled"] = "#d1d5db",      // Light gray
        };
        
        FontSizes = new Dictionary<string, string>
        {
            ["xs"] = "0.75rem",   // 12px
            ["sm"] = "0.875rem",  // 14px
            ["base"] = "1rem",    // 16px
            ["lg"] = "1.125rem",  // 18px
            ["xl"] = "1.25rem",   // 20px
            ["2xl"] = "1.5rem",   // 24px
            ["3xl"] = "1.875rem", // 30px
            ["4xl"] = "2.25rem",  // 36px
            ["5xl"] = "3rem",     // 48px
        };
        
        Breakpoints = new Dictionary<string, string>
        {
            ["sm"] = "640px",
            ["md"] = "768px",
            ["lg"] = "1024px",
            ["xl"] = "1280px",
            ["2xl"] = "1536px",
        };
    }
    
    public List<DesignToken> GetAllTokens()
    {
        var tokens = new List<DesignToken>();
        
        foreach (var spacing in Spacing)
            tokens.Add(new DesignToken($"spacing-{spacing.Key}", spacing.Value));
            
        foreach (var color in Colors)
            tokens.Add(new DesignToken($"color-{color.Key}", color.Value));
            
        foreach (var fontSize in FontSizes)
            tokens.Add(new DesignToken($"font-size-{fontSize.Key}", fontSize.Value));
            
        // Add additional tokens
        tokens.Add(new DesignToken("border-radius-sm", "0.125rem"));
        tokens.Add(new DesignToken("border-radius-md", "0.375rem"));
        tokens.Add(new DesignToken("border-radius-lg", "0.5rem"));
        tokens.Add(new DesignToken("border-radius-xl", "0.75rem"));
        
        tokens.Add(new DesignToken("shadow-sm", "0 1px 2px 0 rgb(0 0 0 / 0.05)"));
        tokens.Add(new DesignToken("shadow-md", "0 4px 6px -1px rgb(0 0 0 / 0.1), 0 2px 4px -2px rgb(0 0 0 / 0.1)"));
        tokens.Add(new DesignToken("shadow-lg", "0 10px 15px -3px rgb(0 0 0 / 0.1), 0 4px 6px -4px rgb(0 0 0 / 0.1)"));
        
        return tokens;
    }
}

// =============================================================================
// HELPER CLASSES
// =============================================================================

public class DesignToken
{
    public string Name { get; }
    public string Value { get; }
    
    public DesignToken(string name, string value)
    {
        Name = name;
        Value = value;
    }
}

public class SemanticClass
{
    public required string Name { get; init; }
    public required string Expression { get; init; }
    public required List<string> CssRules { get; init; }
}

// =============================================================================
// ENHANCED BLAZOR GENERATOR INTEGRATION
// =============================================================================

/// <summary>
/// Enhanced Blazor generator with semantic styling support
/// </summary>
public class EnhancedBlazorGenerator : BlazorGenerator
{
    private readonly CadenzaSemanticStyling _semanticStyling;
    
    public EnhancedBlazorGenerator()
    {
        _semanticStyling = new CadenzaSemanticStyling();
    }
    
    /// <summary>
    /// Generates Blazor component with semantic styling support (CSS handled separately)
    /// </summary>
    public new string GenerateBlazorComponent(ComponentDeclaration component, bool includeRouteAttribute = true)
    {
        // Generate base component without CSS embedding
        var blazorCode = base.GenerateBlazorComponent(component, includeRouteAttribute);
        
        // CSS is generated separately via GenerateComponentCSS method
        return blazorCode;
    }
    
    /// <summary>
    /// Gets the generated CSS for a component
    /// </summary>
    public string GenerateComponentCSS(ComponentDeclaration component)
    {
        return _semanticStyling.ProcessComponentStyling(component);
    }
}