// Cadenza Core Compiler - Render Tree Generator
// RenderTreeBuilder API code generation for Blazor components

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cadenza.Core;

// =============================================================================
// RENDER TREE GENERATOR
// =============================================================================

/// <summary>
/// Generates RenderTreeBuilder API code for Blazor components
/// </summary>
public class RenderTreeGenerator
{
    private readonly StringBuilder _classContent;
    private readonly ExpressionGenerator _expressionGenerator;
    private int _sequenceNumber = 0;

    public RenderTreeGenerator(StringBuilder classContent, ExpressionGenerator expressionGenerator)
    {
        _classContent = classContent;
        _expressionGenerator = expressionGenerator;
    }

    public void ResetSequence()
    {
        _sequenceNumber = 0;
    }

    /// <summary>
    /// Generates the main BuildRenderTree method
    /// </summary>
    public void GenerateBuildRenderTreeMethod(ASTNode renderBlock)
    {
        _classContent.AppendLine("    protected override void BuildRenderTree(RenderTreeBuilder builder)");
        _classContent.AppendLine("    {");
        
        GenerateRenderTreeNodes(renderBlock, "        ");
        
        _classContent.AppendLine("    }");
    }

    /// <summary>
    /// Generates RenderTreeBuilder calls for render tree nodes
    /// </summary>
    public void GenerateRenderTreeNodes(ASTNode node, string indent)
    {
        switch (node)
        {
            case UIElement element:
                GenerateElementRenderTree(element, indent);
                break;
            case ConditionalRender conditional:
                GenerateConditionalRenderTree(conditional, indent);
                break;
            case IterativeRender iterative:
                GenerateIterativeRenderTree(iterative, indent);
                break;
            case ComponentInstance componentInstance:
                GenerateComponentInstanceRenderTree(componentInstance, indent);
                break;
            case RenderBlock block:
                foreach (var item in block.Items)
                {
                    GenerateRenderTreeNodes(item, indent);
                }
                break;
            default:
                // Handle other node types like text content
                if (node is StringLiteral textNode)
                {
                    _classContent.AppendLine($"{indent}builder.AddContent({_sequenceNumber++}, \"{textNode.Value}\");");
                }
                break;
        }
    }

    /// <summary>
    /// Generates RenderTreeBuilder calls for HTML elements
    /// </summary>
    public void GenerateElementRenderTree(UIElement element, string indent)
    {
        var tag = TypeMapper.MapCadenzaElementToHtml(element.Tag, element.Attributes);
        var elementSeq = _sequenceNumber++;
        
        _classContent.AppendLine($"{indent}builder.OpenElement({elementSeq}, \"{tag}\");");
        
        // Generate attributes FIRST (must come immediately after OpenElement)
        foreach (var attr in element.Attributes)
        {
            if (attr.Name != "text" && !TypeMapper.IsSemanticAttribute(element.Tag, attr.Name)) // Skip text and semantic attributes
            {
                GenerateAttributeRenderTree(attr, indent);
            }
        }
        
        // Generate text content AFTER all attributes
        foreach (var attr in element.Attributes)
        {
            if (attr.Name == "text")
            {
                GenerateTextContentRenderTree(attr, indent);
            }
        }
        
        // Generate children
        if (element.Children != null && element.Children.Count > 0)
        {
            foreach (var child in element.Children)
            {
                GenerateRenderTreeNodes(child, indent);
            }
        }
        
        _classContent.AppendLine($"{indent}builder.CloseElement();");
    }

    /// <summary>
    /// Generates RenderTreeBuilder calls for attributes (excludes text content)
    /// </summary>
    public void GenerateAttributeRenderTree(UIAttribute attr, string indent)
    {
        var attrSeq = _sequenceNumber++;
        var attributeName = TypeMapper.MapCadenzaAttributeToBlazor(attr.Name);
        
        if (attributeName.StartsWith("on") && !attributeName.StartsWith("on_"))
        {
            // This is an event handler
            var eventHandlerName = _expressionGenerator.GenerateExpression(attr.Value);
            _classContent.AppendLine($"{indent}builder.AddAttribute({attrSeq}, \"{attributeName}\", EventCallback.Factory.Create(this, {eventHandlerName}));");
        }
        else if (attr.Value is StringLiteral stringLiteral)
        {
            _classContent.AppendLine($"{indent}builder.AddAttribute({attrSeq}, \"{attributeName}\", \"{stringLiteral.Value}\");");
        }
        else if (attr.Value is Identifier identifier)
        {
            _classContent.AppendLine($"{indent}builder.AddAttribute({attrSeq}, \"{attributeName}\", {_expressionGenerator.MapStateVariableName(identifier.Name)});");
        }
        else if (attr.Value is BooleanLiteral boolLiteral)
        {
            _classContent.AppendLine($"{indent}builder.AddAttribute({attrSeq}, \"{attributeName}\", {boolLiteral.Value.ToString().ToLower()});");
        }
        else
        {
            var expression = _expressionGenerator.GenerateExpression(attr.Value);
            _classContent.AppendLine($"{indent}builder.AddAttribute({attrSeq}, \"{attributeName}\", {expression});");
        }
    }
    
    /// <summary>
    /// Generates RenderTreeBuilder calls for text content
    /// </summary>
    public void GenerateTextContentRenderTree(UIAttribute attr, string indent)
    {
        var contentSeq = _sequenceNumber++;
        
        if (attr.Value is StringLiteral stringLiteral)
        {
            _classContent.AppendLine($"{indent}builder.AddContent({contentSeq}, \"{stringLiteral.Value}\");");
        }
        else
        {
            var expression = _expressionGenerator.GenerateExpression(attr.Value);
            _classContent.AppendLine($"{indent}builder.AddContent({contentSeq}, {expression});");
        }
    }

    /// <summary>
    /// Generates conditional rendering using RenderTreeBuilder
    /// </summary>
    public void GenerateConditionalRenderTree(ConditionalRender conditional, string indent)
    {
        var condition = _expressionGenerator.GenerateExpression(conditional.Condition);
        
        _classContent.AppendLine($"{indent}if ({condition})");
        _classContent.AppendLine($"{indent}{{");
        
        foreach (var item in conditional.ThenBody)
        {
            GenerateRenderTreeNodes(item, indent + "    ");
        }
        
        _classContent.AppendLine($"{indent}}}");
        
        if (conditional.ElseBody != null && conditional.ElseBody.Count > 0)
        {
            _classContent.AppendLine($"{indent}else");
            _classContent.AppendLine($"{indent}{{");
            
            foreach (var item in conditional.ElseBody)
            {
                GenerateRenderTreeNodes(item, indent + "    ");
            }
            
            _classContent.AppendLine($"{indent}}}");
        }
    }

    /// <summary>
    /// Generates iterative rendering using RenderTreeBuilder
    /// </summary>
    public void GenerateIterativeRenderTree(IterativeRender iterative, string indent)
    {
        var collectionExpr = _expressionGenerator.GenerateExpression(iterative.Collection);
        
        if (iterative.Condition != null)
        {
            // Use LINQ Where for conditional iteration
            var conditionExpr = _expressionGenerator.GenerateExpression(iterative.Condition);
            _classContent.AppendLine($"{indent}foreach (var {iterative.Variable} in {collectionExpr}.Where({iterative.Variable} => {conditionExpr}))");
        }
        else
        {
            _classContent.AppendLine($"{indent}foreach (var {iterative.Variable} in {collectionExpr})");
        }
        
        _classContent.AppendLine($"{indent}{{");
        
        // Each iteration needs a unique sequence number for proper Blazor diffing
        _classContent.AppendLine($"{indent}    builder.OpenRegion({_sequenceNumber++});");
        
        foreach (var item in iterative.Body)
        {
            GenerateRenderTreeNodes(item, indent + "    ");
        }
        
        _classContent.AppendLine($"{indent}    builder.CloseRegion();");
        _classContent.AppendLine($"{indent}}}");
    }

    /// <summary>
    /// Generates component instance using RenderTreeBuilder
    /// </summary>
    public void GenerateComponentInstanceRenderTree(ComponentInstance instance, string indent)
    {
        var componentSeq = _sequenceNumber++;
        
        _classContent.AppendLine($"{indent}builder.OpenComponent<{instance.Name}>({componentSeq});");
        
        // Generate component parameters
        foreach (var prop in instance.Props)
        {
            var paramSeq = _sequenceNumber++;
            var paramName = prop.Name;
            var paramValue = _expressionGenerator.GenerateExpression(prop.Value);
            
            _classContent.AppendLine($"{indent}builder.AddAttribute({paramSeq}, \"{paramName}\", {paramValue});");
        }
        
        // Handle child content if present
        if (instance.Children != null && instance.Children.Count > 0)
        {
            var childContentSeq = _sequenceNumber++;
            _classContent.AppendLine($"{indent}builder.AddAttribute({childContentSeq}, \"ChildContent\", (RenderFragment)((builder2) => {{");
            
            var oldSequenceNumber = _sequenceNumber;
            _sequenceNumber = 0; // Reset for child content
            
            foreach (var child in instance.Children)
            {
                GenerateRenderTreeNodes(child, indent + "    ");
            }
            
            _sequenceNumber = oldSequenceNumber; // Restore sequence
            _classContent.AppendLine($"{indent}}}));");
        }
        
        _classContent.AppendLine($"{indent}builder.CloseComponent();");
    }
}