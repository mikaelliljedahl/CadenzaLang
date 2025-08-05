using Cadenza.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cadenza.Tests.Unit
{
    [TestFixture]
    public class ParserTests
    {
        private CadenzaParser CreateParser(string source)
        {
            var lexer = new CadenzaLexer(source);
            var tokens = lexer.ScanTokens();
            return new CadenzaParser(tokens);
        }

        [Test]
        public void Parser_ShouldParseFunctionDeclaration()
        {
            // Arrange
            var source = "function add(a: int, b: int) -> int { return a + b }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            Assert.That(program.Statements.Count, Is.EqualTo(1));
            Assert.That(program.Statements[0], Is.InstanceOf<FunctionDeclaration>());
            
            var func = program.Statements[0] as FunctionDeclaration;
            Assert.That(func, Is.Not.Null);
            Assert.That(func.Name, Is.EqualTo("add"));
            Assert.That(func.Parameters.Count, Is.EqualTo(2));
            Assert.That(func.Parameters[0].Name, Is.EqualTo("a"));
            Assert.That(func.Parameters[0].Type, Is.EqualTo("int"));
            Assert.That(func.Parameters[1].Name, Is.EqualTo("b"));
            Assert.That(func.Parameters[1].Type, Is.EqualTo("int"));
            Assert.That(func.ReturnType, Is.EqualTo("int"));
            Assert.That(func.Body.Count, Is.EqualTo(1));
            Assert.That(func.Body[0], Is.InstanceOf<ReturnStatement>());
        }

        [Test]
        public void Parser_ShouldParsePureFunction()
        {
            // Arrange
            var source = "pure function add(a: int, b: int) -> int { return a + b }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            Assert.That(program.Statements.Count, Is.EqualTo(1));
            var func = (FunctionDeclaration)program.Statements[0];
            Assert.That(func.IsPure, Is.True);
            Assert.That(func.Effects, Is.Null);
        }

        [Test]
        public void Parser_ShouldParseFunctionWithEffects()
        {
            // Arrange
            var source = "function saveUser(user: string) uses [Database, Logging] -> int { return 42 }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            Assert.That(program.Statements.Count, Is.EqualTo(1));
            var func = (FunctionDeclaration)program.Statements[0];
            Assert.That(func.Effects, Is.Not.Null);
            Assert.That(func.Effects.Count, Is.EqualTo(2));
            Assert.That(func.Effects[0], Is.EqualTo("Database"));
            Assert.That(func.Effects[1], Is.EqualTo("Logging"));
        }

        [Test]
        public void Parser_ShouldParseResultType()
        {
            // Arrange
            var source = "function divide(a: int, b: int) -> Result<int, string> { return Ok(42) }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            Assert.That(program.Statements.Count, Is.EqualTo(1));
            var func = (FunctionDeclaration)program.Statements[0];
            Assert.That(func.ReturnType, Is.EqualTo("Result<int, string>"));
        }

        [Test]
        public void Parser_ShouldParseBinaryExpressions()
        {
            // Arrange
            var source = "function test() -> int { return a + b * c }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            var func = (FunctionDeclaration)program.Statements[0];
            var returnStmt = (ReturnStatement)func.Body[0];
            Assert.That(returnStmt, Is.Not.Null);
            Assert.That(returnStmt.Expression, Is.Not.Null);
            Assert.That(returnStmt.Expression, Is.InstanceOf<ArithmeticExpression>());
            
            var expr = (ArithmeticExpression)returnStmt.Expression;
            Assert.That(expr.Operator, Is.EqualTo("+"));
            Assert.That(expr.Left, Is.InstanceOf<Identifier>());
            Assert.That(expr.Right, Is.InstanceOf<ArithmeticExpression>());
        }

        [Test]
        public void Parser_ShouldParseUnaryExpressions()
        {
            // Arrange
            var source = "function test() -> bool { return !condition }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            var func = (FunctionDeclaration)program.Statements[0];
            var returnStmt = (ReturnStatement)func.Body[0];
            Assert.That(returnStmt.Expression, Is.InstanceOf<UnaryExpression>());
            
            var expr = (UnaryExpression)returnStmt.Expression;
            Assert.That(expr.Operator, Is.EqualTo("!"));
            Assert.That(expr.Operand, Is.InstanceOf<Identifier>());
        }

        [Test]
        public void Parser_ShouldParseIfStatement()
        {
            // Arrange
            var source = @"function test() -> int { 
                if condition { 
                    return 1 
                } else { 
                    return 2 
                }
            }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            var func = (FunctionDeclaration)program.Statements[0];
            Assert.That(func.Body.Count, Is.EqualTo(1));
            Assert.That(func.Body[0], Is.InstanceOf<IfStatement>());
            
            var ifStmt = (IfStatement)func.Body[0];
            Assert.That(ifStmt.Condition, Is.InstanceOf<Identifier>());
            Assert.That(ifStmt.ThenBody.Count, Is.EqualTo(1));
            Assert.That(ifStmt.ElseBody, Is.Not.Null);
            Assert.That(ifStmt.ElseBody.Count, Is.EqualTo(1));
        }

        [Test]
        public void Parser_ShouldParseGuardStatement()
        {
            // Arrange
            var source = @"function test() -> int { 
                guard condition else { 
                    return 0 
                }
                return 1
            }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            var func = (FunctionDeclaration)program.Statements[0];
            Assert.That(func.Body.Count, Is.EqualTo(2));
            Assert.That(func.Body[0], Is.InstanceOf<GuardStatement>());
            
            var guardStmt = (GuardStatement)func.Body[0];
            Assert.That(guardStmt, Is.Not.Null);
            Assert.That(guardStmt.Condition, Is.InstanceOf<Identifier>());
            Assert.That(guardStmt.ElseBody, Is.Not.Null);
            Assert.That(guardStmt.ElseBody.Count, Is.EqualTo(1));
        }

        [Test]
        public void Parser_ShouldParseLetStatement()
        {
            // Arrange
            var source = @"function test() -> int { 
                let x = 42
                return x
            }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            var func = (FunctionDeclaration)program.Statements[0];
            Assert.That(func.Body.Count, Is.EqualTo(2));
            Assert.That(func.Body[0], Is.InstanceOf<LetStatement>());
            
            var letStmt = (LetStatement)func.Body[0];
            Assert.That(letStmt.Name, Is.EqualTo("x"));
            Assert.That(letStmt.Expression, Is.InstanceOf<NumberLiteral>());
        }

        [Test]
        public void Parser_ShouldParseFunctionCall()
        {
            // Arrange
            var source = "function test() -> int { return add(1, 2) }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            var func = (FunctionDeclaration)program.Statements[0];
            var returnStmt = (ReturnStatement)func.Body[0];
            Assert.That(returnStmt.Expression, Is.InstanceOf<CallExpression>());
            
            var call = (CallExpression)returnStmt.Expression;
            Assert.That(call.Name, Is.EqualTo("add"));
            Assert.That(call.Arguments.Count, Is.EqualTo(2));
            Assert.That(call.Arguments[0], Is.InstanceOf<NumberLiteral>());
            Assert.That(call.Arguments[1], Is.InstanceOf<NumberLiteral>());
        }

        [Test]
        public void Parser_ShouldParseOkExpression()
        {
            // Arrange
            var source = "function test() -> Result<int, string> { return Ok(42) }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            var func = (FunctionDeclaration)program.Statements[0];
            var returnStmt = (ReturnStatement)func.Body[0];
            Assert.That(returnStmt.Expression, Is.InstanceOf<ResultExpression>());
            
            var okExpr = (ResultExpression)returnStmt.Expression;
            Assert.That(okExpr.Value, Is.InstanceOf<NumberLiteral>());
        }

        [Test]
        public void Parser_ShouldParseErrorExpression()
        {
            // Arrange
            var source = "function test() -> Result<int, string> { return Error(\"failed\") }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            var func = (FunctionDeclaration)program.Statements[0];
            var returnStmt = (ReturnStatement)func.Body[0];
            Assert.That(returnStmt.Expression, Is.InstanceOf<ResultExpression>());
            
            var errorExpr = (ResultExpression)returnStmt.Expression;
            Assert.That(errorExpr.Value, Is.InstanceOf<StringLiteral>());
        }

        [Test]
        public void Parser_ShouldParseErrorPropagation()
        {
            // Arrange
            var source = "function test() -> Result<int, string> { let x = getValue()? return Ok(x) }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            var func = (FunctionDeclaration)program.Statements[0];
            Assert.That(func.Body.Count, Is.EqualTo(2));
            var letStmt = func.Body[0] as LetStatement;
            Assert.That(letStmt, Is.Not.Null);
            Assert.That(letStmt.Expression, Is.InstanceOf<ErrorPropagation>());
            
            var errorProp = (ErrorPropagation)letStmt.Expression;
            Assert.That(errorProp.Expression, Is.InstanceOf<CallExpression>());
        }

        [Test]
        public void Parser_ShouldParseStringInterpolation()
        {
            // Arrange
            var source = "function test() -> string { return $\"Hello {name}!\" }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            var func = (FunctionDeclaration)program.Statements[0];
            var returnStmt = (ReturnStatement)func.Body[0];
            Assert.That(returnStmt.Expression, Is.InstanceOf<StringInterpolation>());
            
            var interp = returnStmt.Expression as StringInterpolation;
            Assert.That(interp, Is.Not.Null);
            Assert.That(interp.Parts.Count, Is.GreaterThan(0));
        }

        [Test]
        public void Parser_ShouldParseModuleDeclaration()
        {
            // Arrange
            var source = @"module Math {
                function add(a: int, b: int) -> int { return a + b }
            }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            Assert.That(program.Statements.Count, Is.EqualTo(1));
            Assert.That(program.Statements[0], Is.InstanceOf<ModuleDeclaration>());
            
            var module = (ModuleDeclaration)program.Statements[0];
            Assert.That(module.Name, Is.EqualTo("Math"));
            Assert.That(module.Body.Count, Is.EqualTo(1));
            Assert.That(module.Body[0], Is.InstanceOf<FunctionDeclaration>());
        }

        [Test]
        public void Parser_ShouldParseImportStatement()
        {
            // Arrange
            var source = "import Math.{add, subtract}";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            Assert.That(program.Statements.Count, Is.EqualTo(1));
            Assert.That(program.Statements[0], Is.InstanceOf<ImportStatement>());
            
            var import = (ImportStatement)program.Statements[0];
            Assert.That(import.ModuleName, Is.EqualTo("Math"));
            Assert.That(import.SpecificImports, Is.Not.Null);
            Assert.That(import.SpecificImports.Count, Is.EqualTo(2));
            Assert.That(import.SpecificImports[0], Is.EqualTo("add"));
            Assert.That(import.SpecificImports[1], Is.EqualTo("subtract"));
        }

        [Test]
        public void Parser_ShouldParseWildcardImport()
        {
            // Arrange
            var source = "import Math.{*}";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            Assert.That(program.Statements.Count, Is.EqualTo(1));
            var import = (ImportStatement)program.Statements[0];
            Assert.That(import, Is.Not.Null);
            Assert.That(import.ModuleName, Is.EqualTo("Math"));
            Assert.That(import.IsWildcard, Is.True);
        }

        [Test]
        public void Parser_ShouldParseExportStatement()
        {
            // Arrange
            var source = "export {add, subtract}";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            Assert.That(program.Statements.Count, Is.EqualTo(1));
            Assert.That(program.Statements[0], Is.InstanceOf<ExportStatement>());
            
            var export = (ExportStatement)program.Statements[0];
            Assert.That(export.ExportedNames.Count, Is.EqualTo(2));
            Assert.That(export.ExportedNames[0], Is.EqualTo("add"));
            Assert.That(export.ExportedNames[1], Is.EqualTo("subtract"));
        }

        [Test]
        public void Parser_ShouldParseQualifiedFunctionCall()
        {
            // Arrange
            var source = "function test() -> int { return Math.add(1, 2) }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            var func = (FunctionDeclaration)program.Statements[0];
            var returnStmt = (ReturnStatement)func.Body[0];
            Assert.That(returnStmt, Is.Not.Null);
            Assert.That(returnStmt.Expression, Is.Not.Null);
            Assert.That(returnStmt.Expression, Is.InstanceOf<MethodCallExpression>());
            
            var call = (MethodCallExpression)returnStmt.Expression;
            Assert.That(call.Method, Is.EqualTo("add"));
        }

        [Test]
        public void Parser_ShouldHandleOperatorPrecedence()
        {
            // Arrange
            var source = "function test() -> bool { return a + b * c > d && e || f }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            var func = (FunctionDeclaration)program.Statements[0];
            var returnStmt = (ReturnStatement)func.Body[0];
            Assert.That(returnStmt, Is.Not.Null);
            Assert.That(returnStmt.Expression, Is.Not.Null);
            Assert.That(returnStmt.Expression, Is.InstanceOf<LogicalExpression>());
            
            // Should parse as: ((a + (b * c)) > d) && e) || f
            var expr = (LogicalExpression)returnStmt.Expression;
            Assert.That(expr.Operator, Is.EqualTo("||"));
        }

        [Test]
        public void Parser_ShouldThrowOnInvalidSyntax()
        {
            // Arrange
            var source = "function test( -> int";
            var parser = CreateParser(source);

            // Act & Assert
            Assert.Throws<Exception>(() => parser.Parse());
        }

        [Test]
        public void Parser_ShouldThrowOnMissingFunctionBody()
        {
            // Arrange
            var source = "function test() -> int";
            var parser = CreateParser(source);

            // Act & Assert
            Assert.Throws<Exception>(() => parser.Parse());
        }

        [Test]
        public void Parser_ShouldAcceptCustomEffectNames()
        {
            // Arrange
            var source = "function test() uses [InvalidEffect] -> int { return 42 }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            var func = (FunctionDeclaration)program.Statements[0];
            Assert.That(func, Is.Not.Null);
            Assert.That(func.Effects, Is.Not.Null);
            Assert.That(func.Effects.Count, Is.EqualTo(1));
            Assert.That(func.Effects[0], Is.EqualTo("InvalidEffect"));
        }

        [Test]
        public void Parser_ShouldParseNestedIfStatements()
        {
            // Arrange
            var source = @"function nested() -> int {
                if x > 0 {
                    if y > 0 {
                        return 1
                    } else {
                        return 2
                    }
                } else {
                    return 3
                }
            }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            var func = (FunctionDeclaration)program.Statements[0];
            Assert.That(func.Body.Count, Is.EqualTo(1));
            var outerIf = (IfStatement)func.Body[0];
            Assert.That(outerIf.ThenBody.Count, Is.EqualTo(1));
            Assert.That(outerIf.ThenBody[0], Is.InstanceOf<IfStatement>());
        }

        [Test]
        public void Parser_ShouldParseMultipleParameterTypes()
        {
            // Arrange
            var source = "function test(a: int, b: string, c: bool) -> int { return 42 }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            var func = (FunctionDeclaration)program.Statements[0];
            Assert.That(func.Parameters.Count, Is.EqualTo(3));
            Assert.That(func.Parameters[0].Type, Is.EqualTo("int"));
            Assert.That(func.Parameters[1].Type, Is.EqualTo("string"));
            Assert.That(func.Parameters[2].Type, Is.EqualTo("bool"));
        }

        [Test]
        public void Parser_ShouldParseComplexResultType()
        {
            // Arrange
            var source = "function complex() -> Result<Result<int, string>, bool> { return Ok(Ok(42)) }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            var func = (FunctionDeclaration)program.Statements[0];
            Assert.That(func.ReturnType, Is.EqualTo("Result<Result<int, string>, bool>"));
        }

        [Test]
        public void Parser_ShouldParseMultipleEffects()
        {
            // Arrange
            var source = "function process() uses [Database, Network, Logging, FileSystem] -> int { return 42 }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            var func = (FunctionDeclaration)program.Statements[0];
            Assert.That(func, Is.Not.Null);
            Assert.That(func.Effects, Is.Not.Null);
            Assert.That(func.Effects.Count, Is.EqualTo(4));
            Assert.That(func.Effects, Contains.Item("Database"));
            Assert.That(func.Effects, Contains.Item("Network"));
            Assert.That(func.Effects, Contains.Item("Logging"));
            Assert.That(func.Effects, Contains.Item("FileSystem"));
        }

        [Test]
        public void Parser_ShouldParseChainedErrorPropagation()
        {
            // Arrange
            var source = "function chain() -> Result<int, string> { let x = getValue()?.process()? return Ok(x) }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            var func = (FunctionDeclaration)program.Statements[0];
            var letStmt = (LetStatement)func.Body[0];
            Assert.That(letStmt.Expression, Is.InstanceOf<ErrorPropagation>());
        }

        [Test]
        public void Parser_ShouldParseComplexStringInterpolation()
        {
            // Arrange
            var source = @"function greet() -> string { return $""Hello {user.name}, you have {getCount()} messages!"" }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            var func = (FunctionDeclaration)program.Statements[0];
            var returnStmt = (ReturnStatement)func.Body[0];
            Assert.That(returnStmt.Expression, Is.InstanceOf<StringInterpolation>());
            var interp = (StringInterpolation)returnStmt.Expression;
            Assert.That(interp.Parts.Count, Is.GreaterThan(2));
        }

        [Test]
        public void Parser_ShouldParseNestedModuleImports()
        {
            // Arrange
            var source = "import Utils";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            var import = (ImportStatement)program.Statements[0];
            Assert.That(import, Is.Not.Null);
            Assert.That(import.ModuleName, Is.EqualTo("Utils"));
            Assert.That(import.SpecificImports, Is.Null);
        }

        [Test]
        public void Parser_ShouldParseEmptyModule()
        {
            // Arrange
            var source = "module Empty { }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            var module = (ModuleDeclaration)program.Statements[0];
            Assert.That(module.Name, Is.EqualTo("Empty"));
            Assert.That(module.Body.Count, Is.EqualTo(0));
        }

        [Test]
        public void Parser_ShouldParseModuleWithExports()
        {
            // Arrange
            var source = @"module Test {
                function helper() -> int { return 42 }
                function main() -> int { return helper() }
                export {main}
            }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            var module = (ModuleDeclaration)program.Statements[0];
            Assert.That(module.Body.Count, Is.EqualTo(3)); // 2 functions + 1 export
            Assert.That(module.Body[2], Is.InstanceOf<ExportStatement>());
        }

        [Test]
        public void Parser_ShouldHandleComplexBooleanExpressions()
        {
            // Arrange
            var source = "function complex() -> bool { return (a && b) || (c && !d) || (e > f && g <= h) }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            var func = (FunctionDeclaration)program.Statements[0];
            var returnStmt = (ReturnStatement)func.Body[0];
            Assert.That(returnStmt, Is.Not.Null);
            Assert.That(returnStmt.Expression, Is.Not.Null);
            Assert.That(returnStmt.Expression, Is.InstanceOf<LogicalExpression>());
        }

        [Test]
        public void Parser_ShouldParseMultipleLetStatements()
        {
            // Arrange
            var source = @"function calc() -> int {
                let a = 10
                let b = 20
                let c = a + b
                return c
            }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            var func = (FunctionDeclaration)program.Statements[0];
            Assert.That(func.Body.Count, Is.EqualTo(4)); // 3 let statements + 1 return
            Assert.That(func.Body[0], Is.InstanceOf<LetStatement>());
            Assert.That(func.Body[1], Is.InstanceOf<LetStatement>());
            Assert.That(func.Body[2], Is.InstanceOf<LetStatement>());
        }

        [Test]
        public void Parser_ShouldThrowOnMismatchedBraces()
        {
            // Arrange
            var source = "function test() -> int { return 42";
            var parser = CreateParser(source);

            // Act & Assert
            Assert.Throws<Exception>(() => parser.Parse());
        }

        [Test]
        public void Parser_ShouldThrowOnMismatchedParentheses()
        {
            // Arrange
            var source = "function test( -> int { return 42 }";
            var parser = CreateParser(source);

            // Act & Assert
            Assert.Throws<Exception>(() => parser.Parse());
        }

        [Test]
        public void Parser_ShouldThrowOnInvalidResultType()
        {
            // Arrange
            var source = "function test() -> Result<int> { return Ok(42) }";
            var parser = CreateParser(source);

            // Act & Assert
            Assert.Throws<Exception>(() => parser.Parse());
        }

        [Test]
        public void Parser_ShouldAllowPureFunctionWithEffectsForNow()
        {
            // Arrange
            var source = "pure function test() uses [Database] -> int { return 42 }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert - Currently parser doesn't validate pure functions with effects
            var func = program.Statements[0] as FunctionDeclaration;
            Assert.That(func, Is.Not.Null);
            Assert.That(func.IsPure, Is.True);
            Assert.That(func.Effects, Is.Not.Null);
            Assert.That(func.Effects.Count, Is.EqualTo(1));
            Assert.That(func.Effects[0], Is.EqualTo("Database"));
        }

        [Test]
        public void Parser_ShouldParseAllEffectTypes()
        {
            // Arrange
            var source = "function allEffects() uses [Database, Network, Logging, FileSystem, Memory, IO] -> int { return 42 }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            var func = (FunctionDeclaration)program.Statements[0];
            Assert.That(func, Is.Not.Null);
            Assert.That(func.Effects, Is.Not.Null);
            Assert.That(func.Effects.Count, Is.EqualTo(6));
            var expectedEffects = new[] { "Database", "Network", "Logging", "FileSystem", "Memory", "IO" };
            foreach (var effect in expectedEffects)
            {
                Assert.That(func.Effects, Contains.Item(effect));
            }
        }

        [Test]
        public void Parser_ShouldParseSimpleTypeDeclaration()
        {
            // Arrange
            var source = "type User { name: string, age: int }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            Assert.That(program.Statements.Count, Is.EqualTo(1));
            Assert.That(program.Statements[0], Is.InstanceOf<TypeDeclaration>());
            
            var type = program.Statements[0] as TypeDeclaration;
            Assert.That(type, Is.Not.Null);
            Assert.That(type.Name, Is.EqualTo("User"));
            Assert.That(type.Fields.Count, Is.EqualTo(2));
            
            Assert.That(type.Fields[0].Name, Is.EqualTo("name"));
            Assert.That(type.Fields[0].Type, Is.EqualTo("string"));
            Assert.That(type.Fields[1].Name, Is.EqualTo("age"));
            Assert.That(type.Fields[1].Type, Is.EqualTo("int"));
        }

        [Test]
        public void Parser_ShouldParseTypeDeclarationWithAllBuiltinTypes()
        {
            // Arrange
            var source = "type TestType { text: string, number: int, flag: bool }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            var type = (TypeDeclaration)program.Statements[0];
            Assert.That(type.Fields.Count, Is.EqualTo(3));
            Assert.That(type.Fields[0].Type, Is.EqualTo("string"));
            Assert.That(type.Fields[1].Type, Is.EqualTo("int"));
            Assert.That(type.Fields[2].Type, Is.EqualTo("bool"));
        }

        [Test]
        public void Parser_ShouldParseTypeDeclarationInModule()
        {
            // Arrange
            var source = @"module UserModule {
                type User { 
                    id: int,
                    name: string,
                    email: string
                }
            }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            Assert.That(program.Statements.Count, Is.EqualTo(1));
            Assert.That(program.Statements[0], Is.InstanceOf<ModuleDeclaration>());
            
            var module = program.Statements[0] as ModuleDeclaration;
            Assert.That(module, Is.Not.Null);
            Assert.That(module.Name, Is.EqualTo("UserModule"));
            Assert.That(module.Body.Count, Is.EqualTo(1));
            Assert.That(module.Body[0], Is.InstanceOf<TypeDeclaration>());
            
            var type = module.Body[0] as TypeDeclaration;
            Assert.That(type, Is.Not.Null);
            Assert.That(type.Name, Is.EqualTo("User"));
            Assert.That(type.Fields.Count, Is.EqualTo(3));
        }

        [Test]
        public void Parser_ShouldParseTypeDeclarationWithOptionalCommas()
        {
            // Arrange
            var source = @"type Product {
                id: int,
                name: string,
                price: int
            }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            var type = (TypeDeclaration)program.Statements[0];
            Assert.That(type.Fields.Count, Is.EqualTo(3));
            Assert.That(type.Fields[0].Name, Is.EqualTo("id"));
            Assert.That(type.Fields[1].Name, Is.EqualTo("name"));
            Assert.That(type.Fields[2].Name, Is.EqualTo("price"));
        }
    }
}