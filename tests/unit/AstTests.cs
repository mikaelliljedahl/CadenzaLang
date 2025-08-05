using NUnit.Framework;
using System.Collections.Generic;
using Cadenza.Core;

namespace Cadenza.Tests.Unit
{

[TestFixture]
public class AstTests
{
    [Test]
    public void Ast_Program_ShouldStoreStatements()
    {
        // Arrange
        var statements = new List<ASTNode>
        {
            new FunctionDeclaration("test", new List<Parameter>(), "int", new List<ASTNode>()),
            new ReturnStatement(new NumberLiteral(42))
        };

        // Act
        var program = new ProgramNode(statements);

        // Assert
        Assert.That(program.Statements.Count, Is.EqualTo(2));
        Assert.That(program.Statements[0], Is.InstanceOf<FunctionDeclaration>());
        Assert.That(program.Statements[1], Is.InstanceOf<ReturnStatement>());
    }

    [Test]
    public void Ast_FunctionDeclaration_ShouldStoreAllProperties()
    {
        // Arrange
        var name = "testFunction";
        var parameters = new List<Parameter> { new Parameter("x", "int"), new Parameter("y", "string") };
        var returnType = "int";
        var body = new List<ASTNode> { new ReturnStatement(new NumberLiteral(42)) };
        var isPure = true;
        var effects = new List<string> { "Database", "Network" };
        var isExported = true;

        // Act
        var func = new FunctionDeclaration(name, parameters, returnType, body, isPure, effects, isExported);

        // Assert
        Assert.That(func.Name, Is.EqualTo(name));
        Assert.That(func.Parameters, Is.EqualTo(parameters));
        Assert.That(func.ReturnType, Is.EqualTo(returnType));
        Assert.That(func.Body, Is.EqualTo(body));
        Assert.That(func.IsPure, Is.EqualTo(isPure));
        Assert.That(func.Effects, Is.EqualTo(effects));
        Assert.That(func.IsExported, Is.EqualTo(isExported));
    }

    [Test]
    public void Ast_Parameter_ShouldStoreNameAndType()
    {
        // Arrange
        var name = "testParam";
        var type = "string";

        // Act
        var parameter = new Parameter(name, type);

        // Assert
        Assert.That(parameter.Name, Is.EqualTo(name));
        Assert.That(parameter.Type, Is.EqualTo(type));
    }

    [Test]
    public void Ast_LetStatement_ShouldStoreAllProperties()
    {
        // Arrange
        var name = "testVar";
        var type = "int";
        var expression = new NumberLiteral(42);

        // Act
        var letStmt = new LetStatement(name, type, expression);

        // Assert
        Assert.That(letStmt.Name, Is.EqualTo(name));
        Assert.That(letStmt.Type, Is.EqualTo(type));
        Assert.That(letStmt.Expression, Is.EqualTo(expression));
    }

    [Test]
    public void Ast_IfStatement_ShouldStoreAllProperties()
    {
        // Arrange
        var condition = new BooleanLiteral(true);
        var thenBody = new List<ASTNode> { new ReturnStatement(new NumberLiteral(1)) };
        var elseBody = new List<ASTNode> { new ReturnStatement(new NumberLiteral(0)) };

        // Act
        var ifStmt = new IfStatement(condition, thenBody, elseBody);

        // Assert
        Assert.That(ifStmt.Condition, Is.EqualTo(condition));
        Assert.That(ifStmt.ThenBody, Is.EqualTo(thenBody));
        Assert.That(ifStmt.ElseBody, Is.EqualTo(elseBody));
    }

    [Test]
    public void Ast_BinaryExpression_ShouldStoreAllProperties()
    {
        // Arrange
        var left = new NumberLiteral(5);
        var operator_ = "+";
        var right = new NumberLiteral(3);

        // Act
        var binaryExpr = new BinaryExpression(left, operator_, right);

        // Assert
        Assert.That(binaryExpr.Left, Is.EqualTo(left));
        Assert.That(binaryExpr.Operator, Is.EqualTo(operator_));
        Assert.That(binaryExpr.Right, Is.EqualTo(right));
    }

    [Test]
    public void Ast_CallExpression_ShouldStoreAllProperties()
    {
        // Arrange
        var name = "testFunction";
        var arguments = new List<ASTNode> { new NumberLiteral(42), new StringLiteral("test") };

        // Act
        var callExpr = new CallExpression(name, arguments);

        // Assert
        Assert.That(callExpr.Name, Is.EqualTo(name));
        Assert.That(callExpr.Arguments, Is.EqualTo(arguments));
    }

    [Test]
    public void Ast_NumberLiteral_ShouldStoreValue()
    {
        // Arrange
        var value = 42;

        // Act
        var numberLiteral = new NumberLiteral(value);

        // Assert
        Assert.That(numberLiteral.Value, Is.EqualTo(value));
    }

    [Test]
    public void Ast_StringLiteral_ShouldStoreValue()
    {
        // Arrange
        var value = "test string";

        // Act
        var stringLiteral = new StringLiteral(value);

        // Assert
        Assert.That(stringLiteral.Value, Is.EqualTo(value));
    }

    [Test]
    public void Ast_BooleanLiteral_ShouldStoreValue()
    {
        // Arrange
        var value = true;

        // Act
        var booleanLiteral = new BooleanLiteral(value);

        // Assert
        Assert.That(booleanLiteral.Value, Is.EqualTo(value));
    }

    [Test]
    public void Ast_ResultExpression_ShouldStoreAllProperties()
    {
        // Arrange
        var type = "Ok";
        var value = new NumberLiteral(42);

        // Act
        var resultExpr = new ResultExpression(type, value);

        // Assert
        Assert.That(resultExpr.Type, Is.EqualTo(type));
        Assert.That(resultExpr.Value, Is.EqualTo(value));
    }

    [Test]
    public void Ast_ErrorPropagation_ShouldStoreExpression()
    {
        // Arrange
        var expression = new CallExpression("riskyFunction", new List<ASTNode>());

        // Act
        var errorProp = new ErrorPropagation(expression);

        // Assert
        Assert.That(errorProp.Expression, Is.EqualTo(expression));
    }

    [Test]
    public void Ast_MatchExpression_ShouldStoreAllProperties()
    {
        // Arrange
        var value = new Identifier("result");
        var cases = new List<MatchCase>
        {
            new MatchCase("Ok", "val", new List<ASTNode> { new Identifier("val") }),
            new MatchCase("Error", "err", new List<ASTNode> { new NumberLiteral(0) })
        };

        // Act
        var matchExpr = new MatchExpression(value, cases);

        // Assert
        Assert.That(matchExpr.Value, Is.EqualTo(value));
        Assert.That(matchExpr.Cases, Is.EqualTo(cases));
    }

    [Test]
    public void Ast_MatchCase_ShouldStoreAllProperties()
    {
        // Arrange
        var pattern = "Ok";
        var variable = "value";
        var body = new List<ASTNode> { new Identifier("value") };

        // Act
        var matchCase = new MatchCase(pattern, variable, body);

        // Assert
        Assert.That(matchCase.Pattern, Is.EqualTo(pattern));
        Assert.That(matchCase.Variable, Is.EqualTo(variable));
        Assert.That(matchCase.Body, Is.EqualTo(body));
    }

    [Test]
    public void Ast_ModuleDeclaration_ShouldStoreAllProperties()
    {
        // Arrange
        var name = "TestModule";
        var body = new List<ASTNode> { new FunctionDeclaration("test", new List<Parameter>(), "int", new List<ASTNode>()) };
        var exports = new List<string> { "test" };

        // Act
        var module = new ModuleDeclaration(name, body, exports);

        // Assert
        Assert.That(module.Name, Is.EqualTo(name));
        Assert.That(module.Body, Is.EqualTo(body));
        Assert.That(module.Exports, Is.EqualTo(exports));
    }

    [Test]
    public void Ast_ImportStatement_ShouldStoreAllProperties()
    {
        // Arrange
        var moduleName = "TestModule";
        var specificImports = new List<string> { "function1", "function2" };
        var isWildcard = false;

        // Act
        var import = new ImportStatement(moduleName, specificImports, isWildcard);

        // Assert
        Assert.That(import.ModuleName, Is.EqualTo(moduleName));
        Assert.That(import.SpecificImports, Is.EqualTo(specificImports));
        Assert.That(import.IsWildcard, Is.EqualTo(isWildcard));
    }

    [Test]
    public void Ast_ExportStatement_ShouldStoreExportedNames()
    {
        // Arrange
        var exportedNames = new List<string> { "function1", "function2" };

        // Act
        var export = new ExportStatement(exportedNames);

        // Assert
        Assert.That(export.ExportedNames, Is.EqualTo(exportedNames));
    }

    [Test]
    public void Ast_SpecificationBlock_ShouldStoreAllProperties()
    {
        // Arrange
        var intent = "This function calculates the sum of two numbers";
        var rules = new List<string> { "Both inputs must be positive", "Result must be greater than either input" };
        var postconditions = new List<string> { "Returns sum of inputs", "Result is integer" };
        var sourceDoc = "API Documentation link";

        // Act
        var spec = new SpecificationBlock(intent, rules, postconditions, sourceDoc);

        // Assert
        Assert.That(spec.Intent, Is.EqualTo(intent));
        Assert.That(spec.Rules, Is.EqualTo(rules));
        Assert.That(spec.Postconditions, Is.EqualTo(postconditions));
        Assert.That(spec.SourceDoc, Is.EqualTo(sourceDoc));
    }

    [Test]
    public void Ast_GuardStatement_ShouldStoreAllProperties()
    {
        // Arrange
        var condition = new BooleanLiteral(true);
        var elseBody = new List<ASTNode> { new ReturnStatement(new NumberLiteral(0)) };

        // Act
        var guard = new GuardStatement(condition, elseBody);

        // Assert
        Assert.That(guard.Condition, Is.EqualTo(condition));
        Assert.That(guard.ElseBody, Is.EqualTo(elseBody));
    }

    [Test]
    public void Ast_TernaryExpression_ShouldStoreAllProperties()
    {
        // Arrange
        var condition = new BooleanLiteral(true);
        var thenExpr = new NumberLiteral(1);
        var elseExpr = new NumberLiteral(0);

        // Act
        var ternary = new TernaryExpression(condition, thenExpr, elseExpr);

        // Assert
        Assert.That(ternary.Condition, Is.EqualTo(condition));
        Assert.That(ternary.ThenExpr, Is.EqualTo(thenExpr));
        Assert.That(ternary.ElseExpr, Is.EqualTo(elseExpr));
    }

    [Test]
    public void Ast_AllASTNodes_ShouldInheritFromASTNode()
    {
        // Test that all AST node types properly inherit from ASTNode
        Assert.That(new ProgramNode(new List<ASTNode>()), Is.InstanceOf<ASTNode>());
        Assert.That(new FunctionDeclaration("test", new List<Parameter>(), "int", new List<ASTNode>()), Is.InstanceOf<ASTNode>());
        Assert.That(new ReturnStatement(null), Is.InstanceOf<ASTNode>());
        Assert.That(new IfStatement(new BooleanLiteral(true), new List<ASTNode>()), Is.InstanceOf<ASTNode>());
        Assert.That(new LetStatement("x", "int", new NumberLiteral(42)), Is.InstanceOf<ASTNode>());
        Assert.That(new BinaryExpression(new NumberLiteral(1), "+", new NumberLiteral(2)), Is.InstanceOf<ASTNode>());
        Assert.That(new CallExpression("test", new List<ASTNode>()), Is.InstanceOf<ASTNode>());
        Assert.That(new NumberLiteral(42), Is.InstanceOf<ASTNode>());
        Assert.That(new StringLiteral("test"), Is.InstanceOf<ASTNode>());
        Assert.That(new BooleanLiteral(true), Is.InstanceOf<ASTNode>());
        Assert.That(new ResultExpression("Ok", new NumberLiteral(42)), Is.InstanceOf<ASTNode>());
        Assert.That(new MatchExpression(new Identifier("x"), new List<MatchCase>()), Is.InstanceOf<ASTNode>());
        Assert.That(new ModuleDeclaration("test", new List<ASTNode>()), Is.InstanceOf<ASTNode>());
        Assert.That(new ImportStatement("test", null, false), Is.InstanceOf<ASTNode>());
        Assert.That(new ExportStatement(new List<string>()), Is.InstanceOf<ASTNode>());
    }
}
}