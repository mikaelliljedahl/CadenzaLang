// Cadenza Core Component Parser - Component-specific parsing methods
// Handles component declarations, UI elements, render blocks, state and event handlers, conditional and iterative rendering

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

public partial class CadenzaParser
{
    private ComponentDeclaration ParseComponentDeclaration()
    {
        var name = Consume(TokenType.Identifier, "Expected component name").Lexeme;
        
        Consume(TokenType.LeftParen, "Expected '(' after component name");
        var parameters = new List<Parameter>();
        
        if (!Check(TokenType.RightParen))
        {
            do
            {
                var paramName = Consume(TokenType.Identifier, "Expected parameter name").Lexeme;
                Consume(TokenType.Colon, "Expected ':' after parameter name");
                var paramType = ConsumeType("Expected parameter type").Lexeme;
                parameters.Add(new Parameter(paramName, paramType));
            } while (Match(TokenType.Comma));
        }
        
        Consume(TokenType.RightParen, "Expected ')' after parameters");
        
        List<string>? effects = null;
        if (Match(TokenType.Uses))
        {
            effects = ParseEffectsList();
        }
        
        List<StateDeclaration>? state = null;
        List<EventHandler>? events = null;
        ASTNode? onMount = null;
        
        // Parse state declarations if present
        if (Match(TokenType.State))
        {
            state = ParseStateDeclarationsList();
        }
        
        // Parse event handlers if present
        if (Match(TokenType.Events))
        {
            events = ParseEventHandlersList();
        }
        
        Consume(TokenType.Arrow, "Expected '->' after component signature");
        var returnType = Consume(TokenType.Identifier, "Expected return type").Lexeme; // UIComponent, etc.
        Consume(TokenType.LeftBrace, "Expected '{' to start component body");
        
        // Parse component body sections
        while (!Check(TokenType.Render) && !Check(TokenType.RightBrace) && !IsAtEnd())
        {
            if (Match(TokenType.State))
            {
                state = ParseStateDeclarations();
            }
            else if (Match(TokenType.Events))
            {
                events = ParseEventHandlers();
            }
            else if (Match(TokenType.OnMount))
            {
                onMount = ParseOnMount();
            }
            else if (Match(TokenType.DeclareState))
            {
                // Parse declare_state statements and store them in the state list
                if (state == null) state = new List<StateDeclaration>();
                var stateDeclaration = ParseDeclareStateStatement();
                state.Add(stateDeclaration);
            }
            else if (Match(TokenType.EventHandler))
            {
                // Parse and store event handler
                if (events == null) events = new List<EventHandler>();
                var eventHandler = ParseEventHandlerDeclaration();
                events.Add(eventHandler);
            }
            else
            {
                // Skip unknown tokens or parse other statements
                Advance();
            }
        }
        
        // Parse render block
        ASTNode renderBlock;
        if (Match(TokenType.Render))
        {
            renderBlock = ParseRenderBlock();
        }
        else
        {
            throw new Exception("Expected render block in component");
        }
        
        Consume(TokenType.RightBrace, "Expected '}' after component body");
        
        return new ComponentDeclaration(name, parameters, effects, returnType, state, events, onMount, renderBlock);
    }

    private List<StateDeclaration> ParseStateDeclarations()
    {
        var declarations = new List<StateDeclaration>();
        
        Consume(TokenType.LeftBracket, "Expected '[' after state keyword");
        
        do
        {
            var name = Consume(TokenType.Identifier, "Expected state variable name").Lexeme;
            Consume(TokenType.Colon, "Expected ':' after state variable name");
            var type = Consume(TokenType.Identifier, "Expected state variable type").Lexeme;
            
            ASTNode? initialValue = null;
            if (Match(TokenType.Assign))
            {
                initialValue = ParseExpression();
            }
            
            declarations.Add(new StateDeclaration(name, type, initialValue));
        } while (Match(TokenType.Comma));
        
        Consume(TokenType.RightBracket, "Expected ']' after state declarations");
        
        return declarations;
    }

    private List<EventHandler> ParseEventHandlers()
    {
        var handlers = new List<EventHandler>();
        
        Consume(TokenType.LeftBracket, "Expected '[' after events keyword");
        
        do
        {
            var name = Consume(TokenType.Identifier, "Expected event handler name").Lexeme;
            
            Consume(TokenType.LeftParen, "Expected '(' after event handler name");
            var parameters = new List<Parameter>();
            
            if (!Check(TokenType.RightParen))
            {
                do
                {
                    var paramName = Consume(TokenType.Identifier, "Expected parameter name").Lexeme;
                    Consume(TokenType.Colon, "Expected ':' after parameter name");
                    var paramType = Consume(TokenType.Identifier, "Expected parameter type").Lexeme;
                    parameters.Add(new Parameter(paramName, paramType));
                } while (Match(TokenType.Comma));
            }
            
            Consume(TokenType.RightParen, "Expected ')' after parameters");
            
            List<string>? effects = null;
            if (Match(TokenType.Uses))
            {
                effects = ParseEffectsList();
            }
            
            Consume(TokenType.LeftBrace, "Expected '{' to start event handler body");
            var body = ParseStatements();
            Consume(TokenType.RightBrace, "Expected '}' after event handler body");
            
            handlers.Add(new EventHandler(name, parameters, effects, body));
        } while (Match(TokenType.Comma));
        
        Consume(TokenType.RightBracket, "Expected ']' after event handlers");
        
        return handlers;
    }

    private ASTNode ParseOnMount()
    {
        Consume(TokenType.LeftBrace, "Expected '{' after on_mount");
        var statements = ParseStatements();
        Consume(TokenType.RightBrace, "Expected '}' after on_mount body");
        
        // Return a synthetic block expression
        return new CallExpression("on_mount_block", statements);
    }

    private ASTNode ParseRenderBlock()
    {
        Consume(TokenType.LeftBrace, "Expected '{' after render");
        
        var renderItems = new List<ASTNode>();
        
        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            var item = ParseRenderItem();
            if (item != null)
            {
                renderItems.Add(item);
            }
        }
        
        Consume(TokenType.RightBrace, "Expected '}' after render block");
        
        // If multiple items, wrap in a fragment
        if (renderItems.Count == 1)
        {
            return renderItems[0];
        }
        else
        {
            return new UIElement("fragment", new List<UIAttribute>(), renderItems);
        }
    }

    private ASTNode? ParseRenderItem()
    {
        if (Match(TokenType.If))
        {
            return ParseConditionalRender();
        }
        else if (Match(TokenType.For))
        {
            return ParseLoopRender();
        }
        else if (Check(TokenType.Identifier))
        {
            // Could be a UI element or component instance
            var name = Advance().Lexeme;
            
            // Check if this is a known HTML element
            if (IsHtmlElement(name))
            {
                // Always treat HTML elements as UIElement, regardless of syntax
                var attributes = new List<UIAttribute>();
                List<ASTNode> children = new List<ASTNode>();
                
                if (Match(TokenType.LeftParen))
                {
                    attributes = ParseUIAttributes();
                    Consume(TokenType.RightParen, "Expected ')' after attributes");
                }
                
                if (Match(TokenType.LeftBrace))
                {
                    while (!Check(TokenType.RightBrace) && !IsAtEnd())
                    {
                        var child = ParseRenderItem();
                        if (child != null) children.Add(child);
                    }
                    Consume(TokenType.RightBrace, "Expected '}' after element children");
                }
                
                return new UIElement(name, attributes, children);
            }
            else if (Match(TokenType.LeftParen))
            {
                // Component instance with props
                var props = ParseUIAttributes();
                Consume(TokenType.RightParen, "Expected ')' after component props");
                
                List<ASTNode>? children = null;
                if (Match(TokenType.LeftBrace))
                {
                    children = new List<ASTNode>();
                    while (!Check(TokenType.RightBrace) && !IsAtEnd())
                    {
                        var child = ParseRenderItem();
                        if (child != null) children.Add(child);
                    }
                    Consume(TokenType.RightBrace, "Expected '}' after component children");
                }
                
                return new ComponentInstance(name, props, children);
            }
            else
            {
                // Simple UI element
                var attributes = new List<UIAttribute>();
                List<ASTNode> children = new List<ASTNode>();
                
                if (Match(TokenType.LeftParen))
                {
                    attributes = ParseUIAttributes();
                    Consume(TokenType.RightParen, "Expected ')' after attributes");
                }
                
                if (Match(TokenType.LeftBrace))
                {
                    while (!Check(TokenType.RightBrace) && !IsAtEnd())
                    {
                        var child = ParseRenderItem();
                        if (child != null) children.Add(child);
                    }
                    Consume(TokenType.RightBrace, "Expected '}' after element children");
                }
                
                return new UIElement(name, attributes, children);
            }
        }
        else
        {
            // Expression (like text content)
            return ParseExpression();
        }
    }

    private ConditionalRender ParseConditionalRender()
    {
        var condition = ParseExpression();
        
        Consume(TokenType.LeftBrace, "Expected '{' after if condition");
        var thenBody = new List<ASTNode>();
        
        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            var item = ParseRenderItem();
            if (item != null) thenBody.Add(item);
        }
        
        Consume(TokenType.RightBrace, "Expected '}' after if body");
        
        List<ASTNode>? elseBody = null;
        if (Match(TokenType.Else))
        {
            Consume(TokenType.LeftBrace, "Expected '{' after else");
            elseBody = new List<ASTNode>();
            
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                var item = ParseRenderItem();
                if (item != null) elseBody.Add(item);
            }
            
            Consume(TokenType.RightBrace, "Expected '}' after else body");
        }
        
        return new ConditionalRender(condition, thenBody, elseBody);
    }

    private IterativeRender ParseLoopRender()
    {
        var variable = Consume(TokenType.Identifier, "Expected variable name after 'for'").Lexeme;
        Consume(TokenType.In, "Expected 'in' after loop variable");
        var collection = ParseExpression();
        
        ASTNode? condition = null;
        if (Match(TokenType.Where))
        {
            condition = ParseExpression();
        }
        
        Consume(TokenType.LeftBrace, "Expected '{' after for statement");
        var body = new List<ASTNode>();
        
        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            var item = ParseRenderItem();
            if (item != null) body.Add(item);
        }
        
        Consume(TokenType.RightBrace, "Expected '}' after for body");
        
        return new IterativeRender(variable, collection, condition, body);
    }

    private List<UIAttribute> ParseUIAttributes()
    {
        var attributes = new List<UIAttribute>();
        
        if (!Check(TokenType.RightParen))
        {
            do
            {
                var name = Consume(TokenType.Identifier, "Expected attribute name").Lexeme;
                Consume(TokenType.Colon, "Expected ':' after attribute name");
                var value = ParseComplexExpression();
                attributes.Add(new UIAttribute(name, value));
            } while (Match(TokenType.Comma));
        }
        
        return attributes;
    }
    
    private List<StateDeclaration> ParseStateDeclarationsList()
    {
        var declarations = new List<StateDeclaration>();
        
        Consume(TokenType.LeftBracket, "Expected '[' after state keyword");
        
        do
        {
            var name = Consume(TokenType.Identifier, "Expected state variable name").Lexeme;
            // For now, assume all state variables are strings (could be enhanced later)
            declarations.Add(new StateDeclaration(name, "string"));
        } while (Match(TokenType.Comma));
        
        Consume(TokenType.RightBracket, "Expected ']' after state declarations");
        
        return declarations;
    }
    
    private List<EventHandler> ParseEventHandlersList()
    {
        // The events [...] declaration is for metadata only - actual handlers are parsed separately
        Consume(TokenType.LeftBracket, "Expected '[' after events keyword");
        
        // Skip the event names - they're just declarations, not implementations
        do
        {
            Consume(TokenType.Identifier, "Expected event handler name");
        } while (Match(TokenType.Comma));
        
        Consume(TokenType.RightBracket, "Expected ']' after event handlers");
        
        // Return empty list - actual handlers will be added when event_handler statements are parsed
        return new List<EventHandler>();
    }
    
    private StateDeclaration ParseDeclareStateStatement()
    {
        // Parse: declare_state message: string = "Hello"
        var name = Consume(TokenType.Identifier, "Expected state variable name after 'declare_state'").Lexeme;
        Consume(TokenType.Colon, "Expected ':' after state variable name");
        var type = ParseType();
        
        ASTNode? initialValue = null;
        if (Match(TokenType.Assign))
        {
            initialValue = ParseExpression();
        }
        
        return new StateDeclaration(name, type, initialValue);
    }
    
    private EventHandler ParseEventHandlerDeclaration()
    {
        // Parse: event_handler handle_click() uses [DOM] { ... }
        var name = Consume(TokenType.Identifier, "Expected event handler name after 'event_handler'").Lexeme;
        
        Consume(TokenType.LeftParen, "Expected '(' after event handler name");
        var parameters = new List<Parameter>();
        
        if (!Check(TokenType.RightParen))
        {
            do
            {
                var paramName = Consume(TokenType.Identifier, "Expected parameter name").Lexeme;
                Consume(TokenType.Colon, "Expected ':' after parameter name");
                var paramType = ParseType();
                parameters.Add(new Parameter(paramName, paramType));
            } while (Match(TokenType.Comma));
        }
        
        Consume(TokenType.RightParen, "Expected ')' after parameters");
        
        List<string>? effects = null;
        if (Match(TokenType.Uses))
        {
            effects = ParseEffectsList();
        }
        
        Consume(TokenType.LeftBrace, "Expected '{' to start event handler body");
        var body = ParseStatements();
        Consume(TokenType.RightBrace, "Expected '}' after event handler body");
        
        return new EventHandler(name, parameters, effects, body);
    }
}