using System;
using System.Collections.Generic;

namespace Cadenza.Core;

// AST Node definitions
public abstract record ASTNode;
public record ProgramNode(List<ASTNode> Statements) : ASTNode;

// Module system AST nodes
public record ModuleDeclaration(string Name, List<ASTNode> Body, List<string>? Exports = null, SpecificationBlock? Specification = null) : ASTNode;
public record ImportStatement(string ModuleName, List<string>? SpecificImports = null, bool IsWildcard = false) : ASTNode;
public record ExportStatement(List<string> ExportedNames) : ASTNode;

// Specification block AST node
public record SpecificationBlock(
    string Intent,
    List<string>? Rules = null,
    List<string>? Postconditions = null,
    string? SourceDoc = null
) : ASTNode;

// Type definition AST nodes
public record TypeDeclaration(string Name, List<TypeField> Fields, bool IsExported = false) : ASTNode;
public record TypeField(string Name, string Type);

// Function AST nodes
public record FunctionDeclaration(
    string Name, 
    List<Parameter> Parameters, 
    string? ReturnType, 
    List<ASTNode> Body, 
    bool IsPure = false,
    List<string>? Effects = null,
    bool IsExported = false,
    SpecificationBlock? Specification = null
) : ASTNode;

public record Parameter(string Name, string Type);
public record EffectAnnotation(List<string> Effects) : ASTNode;

// Statement AST nodes
public record ReturnStatement(ASTNode? Expression) : ASTNode;
public record IfStatement(ASTNode Condition, List<ASTNode> ThenBody, List<ASTNode>? ElseBody = null) : ASTNode;
public record LetStatement(string Name, string? Type, ASTNode Expression) : ASTNode;
public record GuardStatement(ASTNode Condition, List<ASTNode>? ElseBody = null) : ASTNode;

// Expression AST nodes
public record BinaryExpression(ASTNode Left, string Operator, ASTNode Right) : ASTNode;
public record UnaryExpression(string Operator, ASTNode Operand) : ASTNode;
public record CallExpression(string Name, List<ASTNode> Arguments) : ASTNode;
public record MemberAccessExpression(ASTNode Object, string Member) : ASTNode;
public record MethodCallExpression(ASTNode Object, string Method, List<ASTNode> Arguments) : ASTNode;
public record Identifier(string Name) : ASTNode;
public record NumberLiteral(int Value) : ASTNode;
public record StringLiteral(string Value) : ASTNode;
public record StringInterpolation(List<ASTNode> Parts) : ASTNode;
public record BooleanLiteral(bool Value) : ASTNode;
public record ResultExpression(string Type, ASTNode Value) : ASTNode; // Ok(...) or Error(...)
public record ErrorPropagation(ASTNode Expression) : ASTNode; // expr?
public record TernaryExpression(ASTNode Condition, ASTNode ThenExpr, ASTNode ElseExpr) : ASTNode;
public record LogicalExpression(ASTNode Left, string Operator, ASTNode Right) : ASTNode;
public record ComparisonExpression(ASTNode Left, string Operator, ASTNode Right) : ASTNode;
public record ArithmeticExpression(ASTNode Left, string Operator, ASTNode Right) : ASTNode;
public record ListExpression(List<ASTNode> Elements) : ASTNode;
public record ListAccessExpression(ASTNode List, ASTNode Index) : ASTNode;
public record GenericType(string BaseType, List<string> TypeArguments) : ASTNode;
public record OptionExpression(string Type, ASTNode? Value) : ASTNode; // Some(value) or None
public record MatchExpression(ASTNode Value, List<MatchCase> Cases) : ASTNode;
public record MatchCase(string Pattern, string? Variable, List<ASTNode> Body) : ASTNode;

// UI Component AST nodes (Phase 4)
public record ComponentDeclaration(
    string Name,
    List<Parameter> Parameters,
    List<string>? Effects,
    string ReturnType,
    List<StateDeclaration>? State,
    List<EventHandler>? Events,
    ASTNode? OnMount,
    ASTNode RenderBlock
) : ASTNode;

public record StateDeclaration(string Name, string Type, ASTNode? InitialValue = null) : ASTNode;
public record EventHandler(string Name, List<Parameter> Parameters, List<string>? Effects, List<ASTNode> Body) : ASTNode;
public record UIElement(string Tag, List<UIAttribute> Attributes, List<ASTNode> Children) : ASTNode;
public record UIAttribute(string Name, ASTNode Value) : ASTNode;
public record ComponentInstance(string Name, List<UIAttribute> Props, List<ASTNode>? Children = null) : ASTNode;

public record ConditionalRender(ASTNode Condition, List<ASTNode> ThenBody, List<ASTNode>? ElseBody = null) : ASTNode;
public record IterativeRender(string Variable, ASTNode Collection, ASTNode? Condition, List<ASTNode> Body) : ASTNode;
public record RenderBlock(List<ASTNode> Items) : ASTNode;

public record AppStateDeclaration(
    string Name,
    List<StateDeclaration> StateVariables,
    List<StateAction> Actions,
    List<string>? Effects
) : ASTNode;

public record StateAction(string Name, List<Parameter> Parameters, List<string>? Effects, List<ASTNode> Body) : ASTNode;

public record ApiClientDeclaration(
    string Name,
    string BaseUrl,
    List<ApiMethod> Methods
) : ASTNode;

public record ApiMethod(string Name, List<Parameter> Parameters, string ReturnType, List<string>? Effects) : ASTNode;