// Cadenza Core Compiler - Type Mapper
// Type mapping between Cadenza and C#/Blazor types and elements

using System;
using System.Collections.Generic;
using System.Linq;

namespace Cadenza.Core;

// =============================================================================
// TYPE MAPPER
// =============================================================================

/// <summary>
/// Provides type mapping between Cadenza and C#/Blazor types, elements, and services
/// </summary>
public static class TypeMapper
{
    /// <summary>
    /// Maps Cadenza element names to HTML tags, considering semantic attributes
    /// </summary>
    public static string MapCadenzaElementToHtml(string cadenzaElement, List<UIAttribute>? attributes = null)
    {
        return cadenzaElement switch
        {
            "container" => "div",
            "heading" => GetHeadingTag(attributes),
            "button" => "button",
            "text_input" => "input",
            "text" => "span",
            "image" => "img",
            "list" => "ul",
            "list_item" => "li",
            "table" => "table",
            "table_row" => "tr",
            "table_header" => "th",
            "table_cell" => "td",
            "main_content" => "main",
            "card" => "div",
            "form_header" => "div",
            "form_fields" => "div",
            "form_field" => "div",
            "form_actions" => "div",
            _ => cadenzaElement
        };
    }

    /// <summary>
    /// Gets the appropriate heading tag based on level attribute
    /// </summary>
    public static string GetHeadingTag(List<UIAttribute>? attributes)
    {
        if (attributes == null) return "h1";
        
        var levelAttr = attributes.FirstOrDefault(a => a.Name == "level");
        if (levelAttr?.Value is NumberLiteral numLiteral)
        {
            var level = Math.Max(1, Math.Min(6, numLiteral.Value)); // Clamp between h1 and h6
            return $"h{level}";
        }
        
        return "h1"; // Default
    }

    /// <summary>
    /// Determines if an attribute is semantic (not a real HTML attribute)
    /// </summary>
    public static bool IsSemanticAttribute(string elementTag, string attributeName)
    {
        return elementTag switch
        {
            "heading" => attributeName == "level",
            _ => false
        };
    }

    /// <summary>
    /// Maps Cadenza attributes to Blazor attributes
    /// </summary>
    public static string MapCadenzaAttributeToBlazor(string cadenzaAttribute)
    {
        return cadenzaAttribute switch
        {
            "on_click" => "onclick",
            "on_change" => "onchange",
            "on_input" => "oninput",
            "on_submit" => "onsubmit",
            "on_mouse_enter" => "onmouseenter",
            "on_mouse_leave" => "onmouseleave",
            "on_key_press" => "onkeypress",
            "class" => "class",
            "id" => "id",
            "text" => "value",
            "placeholder" => "placeholder",
            "disabled" => "disabled",
            "src" => "src",
            "alt" => "alt",
            "role" => "role",
            "aria_label" => "aria-label",
            "type" => "type",
            "for" => "for",
            _ => cadenzaAttribute
        };
    }

    /// <summary>
    /// Maps Cadenza types to Blazor/C# types
    /// </summary>
    public static string MapCadenzaTypeToBlazor(string cadenzaType)
    {
        return cadenzaType switch
        {
            "string" => "string",
            "int" => "int",
            "bool" => "bool",
            "float" => "float",
            "double" => "double",
            "DateTime" => "DateTime",
            "List<string>" => "List<string>",
            "List<int>" => "List<int>",
            "Option<string>" => "string?",
            "Option<int>" => "int?",
            "UIComponent" => "ComponentBase",
            _ => cadenzaType
        };
    }

    /// <summary>
    /// Maps Cadenza effects to Blazor services
    /// </summary>
    public static string MapEffectToBlazorService(string effect)
    {
        return effect switch
        {
            "Network" => "HttpClient",
            "LocalStorage" => "IJSRuntime",
            "Database" => "IDbContext",
            "Logging" => "ILogger",
            "DOM" => "IJSRuntime",
            _ => $"I{effect}Service"
        };
    }

    /// <summary>
    /// Gets the required namespace for an effect
    /// </summary>
    public static string? GetEffectNamespace(string effect)
    {
        return effect switch
        {
            "Network" => "System.Net.Http",
            "LocalStorage" => "Microsoft.JSInterop",
            "DOM" => "Microsoft.JSInterop",
            "Database" => "Microsoft.EntityFrameworkCore",
            "Logging" => "Microsoft.Extensions.Logging",
            _ => "Cadenza.Services"
        };
    }
}