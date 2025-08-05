// Cadenza Blazor Generator Helpers - Utility classes for advanced Blazor component generation

using System;
using System.Collections.Generic;
using System.Text;
using Cadenza.Core;

namespace Cadenza.Core
{
    /// <summary>
    /// Provides helper methods for advanced Blazor component generation scenarios
    /// </summary>
    public static class BlazorGeneratorHelpers
    {
        /// <summary>
        /// Validates that a ComponentDeclaration is suitable for Blazor compilation
        /// </summary>
        public static ValidationResult ValidateComponentForBlazor(ComponentDeclaration component)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            
            // Check component name
            if (string.IsNullOrWhiteSpace(component.Name))
            {
                errors.Add("Component name cannot be empty");
            }
            else if (!IsValidCSharpIdentifier(component.Name))
            {
                errors.Add($"Component name '{component.Name}' is not a valid C# identifier");
            }
            
            // Check parameters
            if (component.Parameters != null)
            {
                foreach (var param in component.Parameters)
                {
                    if (!IsValidCSharpIdentifier(param.Name))
                    {
                        errors.Add($"Parameter name '{param.Name}' is not a valid C# identifier");
                    }
                    
                    if (!IsSupportedBlazorType(param.Type))
                    {
                        warnings.Add($"Parameter type '{param.Type}' may not be supported in Blazor");
                    }
                }
            }
            
            // Check state variables
            if (component.State != null)
            {
                foreach (var state in component.State)
                {
                    if (!IsValidCSharpIdentifier(state.Name))
                    {
                        errors.Add($"State variable name '{state.Name}' is not a valid C# identifier");
                    }
                    
                    if (!IsSupportedBlazorType(state.Type))
                    {
                        warnings.Add($"State variable type '{state.Type}' may not be supported in Blazor");
                    }
                }
            }
            
            // Check event handlers
            if (component.Events != null)
            {
                foreach (var handler in component.Events)
                {
                    if (!IsValidCSharpIdentifier(handler.Name))
                    {
                        errors.Add($"Event handler name '{handler.Name}' is not a valid C# identifier");
                    }
                }
            }
            
            return new ValidationResult(errors, warnings);
        }
        
        /// <summary>
        /// Generates CSS class names for Blazor components based on Cadenza naming conventions
        /// </summary>
        public static string GenerateCssClassName(string componentName, string? modifier = null)
        {
            var baseName = ConvertToKebabCase(componentName);
            return modifier != null ? $"{baseName}--{ConvertToKebabCase(modifier)}" : baseName;
        }
        
        /// <summary>
        /// Converts PascalCase to kebab-case for CSS class names
        /// </summary>
        public static string ConvertToKebabCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;
                
            var result = new StringBuilder();
            
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                
                if (char.IsUpper(c))
                {
                    if (i > 0)
                        result.Append('-');
                    result.Append(char.ToLower(c));
                }
                else
                {
                    result.Append(c);
                }
            }
            
            return result.ToString();
        }
        
        /// <summary>
        /// Generates unique keys for list items in Blazor components
        /// </summary>
        public static string GenerateListItemKey(string variableName, string? keyProperty = null)
        {
            return keyProperty != null ? $"{variableName}.{keyProperty}" : variableName;
        }
        
        /// <summary>
        /// Determines if an effect requires async handling in Blazor
        /// </summary>
        public static bool RequiresAsyncHandling(string effect)
        {
            return effect switch
            {
                "Network" => true,
                "Database" => true,
                "LocalStorage" => true,
                "FileSystem" => true,
                _ => false
            };
        }
        
        /// <summary>
        /// Gets the appropriate Blazor lifecycle method for a Cadenza lifecycle event
        /// </summary>
        public static string GetBlazorLifecycleMethod(string cadenzaLifecycleEvent)
        {
            return cadenzaLifecycleEvent switch
            {
                "on_mount" => "OnInitializedAsync",
                "on_update" => "OnParametersSetAsync",
                "on_unmount" => "Dispose",
                _ => "OnInitializedAsync"
            };
        }
        
        private static bool IsValidCSharpIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return false;
                
            // First character must be letter or underscore
            if (!char.IsLetter(identifier[0]) && identifier[0] != '_')
                return false;
                
            // Remaining characters must be letters, digits, or underscores
            for (int i = 1; i < identifier.Length; i++)
            {
                char c = identifier[i];
                if (!char.IsLetterOrDigit(c) && c != '_')
                    return false;
            }
            
            // Check if it's a C# keyword
            return !IsCSharpKeyword(identifier);
        }
        
        private static bool IsCSharpKeyword(string identifier)
        {
            var keywords = new HashSet<string>
            {
                "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
                "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
                "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
                "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
                "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
                "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
                "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this",
                "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
                "using", "virtual", "void", "volatile", "while"
            };
            
            return keywords.Contains(identifier);
        }
        
        private static bool IsSupportedBlazorType(string type)
        {
            var supportedTypes = new HashSet<string>
            {
                "string", "int", "bool", "float", "double", "decimal", "DateTime",
                "List<string>", "List<int>", "List<bool>", "string?", "int?", "bool?",
                "RenderFragment", "EventCallback", "ComponentBase"
            };
            
            return supportedTypes.Contains(type) || type.StartsWith("List<") || type.EndsWith("?");
        }
    }
    
    /// <summary>
    /// Represents the result of component validation
    /// </summary>
    public class ValidationResult
    {
        public List<string> Errors { get; }
        public List<string> Warnings { get; }
        public bool IsValid => Errors.Count == 0;
        
        public ValidationResult(List<string> errors, List<string> warnings)
        {
            Errors = errors ?? new List<string>();
            Warnings = warnings ?? new List<string>();
        }
    }
    
    /// <summary>
    /// Options for configuring Blazor component generation
    /// </summary>
    public class BlazorGenerationOptions
    {
        /// <summary>
        /// Whether to generate CSS class names automatically
        /// </summary>
        public bool GenerateCssClasses { get; set; } = true;
        
        /// <summary>
        /// Whether to use async methods for all event handlers
        /// </summary>
        public bool UseAsyncEventHandlers { get; set; } = false;
        
        /// <summary>
        /// Whether to generate XML documentation comments
        /// </summary>
        public bool GenerateDocumentationComments { get; set; } = true;
        
        /// <summary>
        /// Prefix to add to generated CSS class names
        /// </summary>
        public string? CssClassPrefix { get; set; }
        
        /// <summary>
        /// Whether to generate parameter validation
        /// </summary>
        public bool GenerateParameterValidation { get; set; } = false;
        
        /// <summary>
        /// Whether to use builder regions for loop items (improves performance)
        /// </summary>
        public bool UseBuilderRegions { get; set; } = true;
    }
}