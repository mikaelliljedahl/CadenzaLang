using Cadenza.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cadenza.Tests.Unit
{
    [TestFixture]
    public class LexerTests
    {
        private CadenzaLexer _lexer;

        [Test]
        public void Lexer_ShouldTokenizeSimpleFunction()
        {
            // Arrange
            var source = "function test() -> int";
            _lexer = new CadenzaLexer(source);

            // Act
            var tokens = _lexer.ScanTokens();

            // Assert
            Assert.That(tokens.Count, Is.EqualTo(7)); // function, test, (, ), ->, int, EOF
            Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Function));
            Assert.That(tokens[1].Type, Is.EqualTo(TokenType.Identifier));
            Assert.That(tokens[1].Lexeme, Is.EqualTo("test"));
            Assert.That(tokens[2].Type, Is.EqualTo(TokenType.LeftParen));
            Assert.That(tokens[3].Type, Is.EqualTo(TokenType.RightParen));
            Assert.That(tokens[4].Type, Is.EqualTo(TokenType.Arrow));
            Assert.That(tokens[5].Type, Is.EqualTo(TokenType.Int));
            Assert.That(tokens[6].Type, Is.EqualTo(TokenType.EOF));
        }

        [Test]
        public void Lexer_ShouldTokenizeNumbers()
        {
            // Arrange
            var source = "123 456 789";
            _lexer = new CadenzaLexer(source);

            // Act
            var tokens = _lexer.ScanTokens();

            // Assert
            Assert.That(tokens.Count, Is.EqualTo(4)); // 123, 456, 789, EOF
            Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Number));
            Assert.That(tokens[0].Literal, Is.EqualTo(123));
            Assert.That(tokens[1].Type, Is.EqualTo(TokenType.Number));
            Assert.That(tokens[1].Literal, Is.EqualTo(456));
            Assert.That(tokens[2].Type, Is.EqualTo(TokenType.Number));
            Assert.That(tokens[2].Literal, Is.EqualTo(789));
        }

        [Test]
        public void Lexer_ShouldTokenizeStrings()
        {
            // Arrange
            var source = "\"hello\" \"world\"";
            _lexer = new CadenzaLexer(source);

            // Act
            var tokens = _lexer.ScanTokens();

            // Assert
            Assert.That(tokens.Count, Is.EqualTo(3)); // "hello", "world", EOF
            Assert.That(tokens[0].Type, Is.EqualTo(TokenType.String));
            Assert.That(tokens[0].Literal, Is.EqualTo("hello"));
            Assert.That(tokens[1].Type, Is.EqualTo(TokenType.String));
            Assert.That(tokens[1].Literal, Is.EqualTo("world"));
        }

        [Test]
        public void Lexer_ShouldTokenizeStringInterpolation()
        {
            // Arrange
            var source = "$\"Hello {name}!\"";
            _lexer = new CadenzaLexer(source);

            // Act
            var tokens = _lexer.ScanTokens();

            // Assert
            Assert.That(tokens.Count, Is.EqualTo(2)); // string interpolation, EOF
            Assert.That(tokens[0].Type, Is.EqualTo(TokenType.StringInterpolation));
            Assert.That(tokens[0].Literal, Is.Not.Null);
        }

        [Test]
        public void Lexer_ShouldTokenizeKeywords()
        {
            // Arrange
            var source = "function return if else pure uses Result Ok Error";
            _lexer = new CadenzaLexer(source);

            // Act
            var tokens = _lexer.ScanTokens();

            // Assert
            Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Function));
            Assert.That(tokens[1].Type, Is.EqualTo(TokenType.Return));
            Assert.That(tokens[2].Type, Is.EqualTo(TokenType.If));
            Assert.That(tokens[3].Type, Is.EqualTo(TokenType.Else));
            Assert.That(tokens[4].Type, Is.EqualTo(TokenType.Pure));
            Assert.That(tokens[5].Type, Is.EqualTo(TokenType.Uses));
            Assert.That(tokens[6].Type, Is.EqualTo(TokenType.Result));
            Assert.That(tokens[7].Type, Is.EqualTo(TokenType.Ok));
            Assert.That(tokens[8].Type, Is.EqualTo(TokenType.Error));
        }

        [Test]
        public void Lexer_ShouldTokenizeOperators()
        {
            // Arrange
            var source = "+ - * / > < >= <= == != && || !";
            _lexer = new CadenzaLexer(source);

            // Act
            var tokens = _lexer.ScanTokens();

            // Assert
            Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Plus));
            Assert.That(tokens[1].Type, Is.EqualTo(TokenType.Minus));
            Assert.That(tokens[2].Type, Is.EqualTo(TokenType.Multiply));
            Assert.That(tokens[3].Type, Is.EqualTo(TokenType.Divide));
            Assert.That(tokens[4].Type, Is.EqualTo(TokenType.Greater));
            Assert.That(tokens[5].Type, Is.EqualTo(TokenType.Less));
            Assert.That(tokens[6].Type, Is.EqualTo(TokenType.GreaterEqual));
            Assert.That(tokens[7].Type, Is.EqualTo(TokenType.LessEqual));
            Assert.That(tokens[8].Type, Is.EqualTo(TokenType.Equal));
            Assert.That(tokens[9].Type, Is.EqualTo(TokenType.NotEqual));
            Assert.That(tokens[10].Type, Is.EqualTo(TokenType.And));
            Assert.That(tokens[11].Type, Is.EqualTo(TokenType.Or));
            Assert.That(tokens[12].Type, Is.EqualTo(TokenType.Not));
        }

        [Test]
        public void Lexer_ShouldTokenizeEffectNames()
        {
            // Arrange
            var source = "Database Network Logging FileSystem Memory IO";
            _lexer = new CadenzaLexer(source);

            // Act
            var tokens = _lexer.ScanTokens();

            // Assert
            Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Database));
            Assert.That(tokens[1].Type, Is.EqualTo(TokenType.Network));
            Assert.That(tokens[2].Type, Is.EqualTo(TokenType.Logging));
            Assert.That(tokens[3].Type, Is.EqualTo(TokenType.FileSystem));
            Assert.That(tokens[4].Type, Is.EqualTo(TokenType.Memory));
            Assert.That(tokens[5].Type, Is.EqualTo(TokenType.IO));
        }

        [Test]
        public void Lexer_ShouldTokenizeModuleKeywords()
        {
            // Arrange
            var source = "module import export from . Math.add";
            _lexer = new CadenzaLexer(source);

            // Act
            var tokens = _lexer.ScanTokens();

            // Assert
            Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Module));
            Assert.That(tokens[1].Type, Is.EqualTo(TokenType.Import));
            Assert.That(tokens[2].Type, Is.EqualTo(TokenType.Export));
            Assert.That(tokens[3].Type, Is.EqualTo(TokenType.From));
            Assert.That(tokens[4].Type, Is.EqualTo(TokenType.Dot));
            Assert.That(tokens[5].Type, Is.EqualTo(TokenType.Identifier));
            Assert.That(tokens[5].Lexeme, Is.EqualTo("Math"));
            Assert.That(tokens[6].Type, Is.EqualTo(TokenType.Dot));
            Assert.That(tokens[7].Type, Is.EqualTo(TokenType.Identifier));
            Assert.That(tokens[7].Lexeme, Is.EqualTo("add"));
        }

        [Test]
        public void Lexer_ShouldSkipComments()
        {
            // Arrange
            var source = "function test() // this is a comment\n-> int";
            _lexer = new CadenzaLexer(source);

            // Act
            var tokens = _lexer.ScanTokens();

            // Assert
            Assert.That(tokens.Count, Is.EqualTo(7)); // function, test, (, ), ->, int, EOF
            Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Function));
            Assert.That(tokens[1].Type, Is.EqualTo(TokenType.Identifier));
            Assert.That(tokens[2].Type, Is.EqualTo(TokenType.LeftParen));
            Assert.That(tokens[3].Type, Is.EqualTo(TokenType.RightParen));
            Assert.That(tokens[4].Type, Is.EqualTo(TokenType.Arrow));
            Assert.That(tokens[5].Type, Is.EqualTo(TokenType.Int));
        }

        [Test]
        public void Lexer_ShouldHandleEscapeSequences()
        {
            // Arrange
            var source = "\"Hello\\nWorld\" \"Quote: \\\"Test\\\"\"";
            _lexer = new CadenzaLexer(source);

            // Act
            var tokens = _lexer.ScanTokens();

            // Assert
            Assert.That(tokens.Count, Is.EqualTo(3)); // two strings, EOF
            Assert.That(tokens[0].Type, Is.EqualTo(TokenType.String));
            Assert.That(tokens[0].Literal, Is.EqualTo("Hello\nWorld"));
            Assert.That(tokens[1].Type, Is.EqualTo(TokenType.String));
            Assert.That(tokens[1].Literal, Is.EqualTo("Quote: \"Test\""));
        }

        [Test]
        public void Lexer_ShouldReportLineAndColumnNumbers()
        {
            // Arrange
            var source = "function\ntest() ->\nint";
            _lexer = new CadenzaLexer(source);

            // Act
            var tokens = _lexer.ScanTokens();

            // Assert
            Assert.That(tokens[0].Line, Is.EqualTo(1));
            Assert.That(tokens[0].Column, Is.EqualTo(1));
            Assert.That(tokens[1].Line, Is.EqualTo(2));
            Assert.That(tokens[1].Column, Is.EqualTo(1));
            Assert.That(tokens[2].Line, Is.EqualTo(2));
            Assert.That(tokens[2].Column, Is.EqualTo(5));
        }

        [Test]
        public void Lexer_ShouldThrowOnUnterminatedString()
        {
            // Arrange
            var source = "\"unterminated string";
            _lexer = new CadenzaLexer(source);

            // Act & Assert
            Assert.Throws<Exception>(() => _lexer.ScanTokens());
        }

        [Test]
        public void Lexer_ShouldThrowOnInvalidCharacter()
        {
            // Arrange
            var source = "function test @ invalid";
            _lexer = new CadenzaLexer(source);

            // Act & Assert
            Assert.Throws<Exception>(() => _lexer.ScanTokens());
        }

        [Test]
        public void Lexer_ShouldTokenizeComplexExpression()
        {
            // Arrange
            var source = "a + b * (c - d) >= 42";
            _lexer = new CadenzaLexer(source);

            // Act
            var tokens = _lexer.ScanTokens();

            // Assert
            Assert.That(tokens.Count, Is.EqualTo(12)); // a, +, b, *, (, c, -, d, ), >=, 42, EOF
            Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Identifier));
            Assert.That(tokens[1].Type, Is.EqualTo(TokenType.Plus));
            Assert.That(tokens[2].Type, Is.EqualTo(TokenType.Identifier));
            Assert.That(tokens[3].Type, Is.EqualTo(TokenType.Multiply));
            Assert.That(tokens[4].Type, Is.EqualTo(TokenType.LeftParen));
            Assert.That(tokens[5].Type, Is.EqualTo(TokenType.Identifier));
            Assert.That(tokens[6].Type, Is.EqualTo(TokenType.Minus));
            Assert.That(tokens[7].Type, Is.EqualTo(TokenType.Identifier));
            Assert.That(tokens[8].Type, Is.EqualTo(TokenType.RightParen));
            Assert.That(tokens[9].Type, Is.EqualTo(TokenType.GreaterEqual));
            Assert.That(tokens[10].Type, Is.EqualTo(TokenType.Number));
            Assert.That(tokens[10].Literal, Is.EqualTo(42));
        }

        [Test]
        public void Lexer_ShouldTokenizeErrorPropagationOperator()
        {
            // Arrange
            var source = "result?";
            _lexer = new CadenzaLexer(source);

            // Act
            var tokens = _lexer.ScanTokens();

            // Assert
            Assert.That(tokens.Count, Is.EqualTo(3)); // result, ?, EOF
            Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Identifier));
            Assert.That(tokens[1].Type, Is.EqualTo(TokenType.Question));
        }

        [Test]
        public void Lexer_ShouldTokenizeGuardKeyword()
        {
            // Arrange
            var source = "guard condition else { return 0 }";
            _lexer = new CadenzaLexer(source);

            // Act
            var tokens = _lexer.ScanTokens();

            // Assert
            Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Guard));
            Assert.That(tokens[1].Type, Is.EqualTo(TokenType.Identifier));
            Assert.That(tokens[1].Lexeme, Is.EqualTo("condition"));
            Assert.That(tokens[2].Type, Is.EqualTo(TokenType.Else));
        }

        [Test]
        public void Lexer_ShouldTokenizeLetKeyword()
        {
            // Arrange
            var source = "let x = 42";
            _lexer = new CadenzaLexer(source);

            // Act
            var tokens = _lexer.ScanTokens();

            // Assert
            Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Let));
            Assert.That(tokens[1].Type, Is.EqualTo(TokenType.Identifier));
            Assert.That(tokens[1].Lexeme, Is.EqualTo("x"));
            Assert.That(tokens[2].Type, Is.EqualTo(TokenType.Assign));
            Assert.That(tokens[3].Type, Is.EqualTo(TokenType.Number));
            Assert.That(tokens[3].Literal, Is.EqualTo(42));
        }

        [Test]
        public void Lexer_ShouldTokenizeMultilineInput()
        {
            // Arrange
            var source = @"function test() -> int {
    let x = 42
    return x
}";
            _lexer = new CadenzaLexer(source);

            // Act
            var tokens = _lexer.ScanTokens();

            // Assert
            var nonEOFTokens = tokens.Where(t => t.Type != TokenType.EOF).ToList();
            Assert.That(nonEOFTokens.Count, Is.GreaterThan(12)); // function, test, (, ), ->, int, {, let, x, =, 42, return, x, }
            Assert.That(nonEOFTokens[0].Type, Is.EqualTo(TokenType.Function));
            Assert.That(nonEOFTokens.Any(t => t.Type == TokenType.Let));
            Assert.That(nonEOFTokens.Any(t => t.Type == TokenType.RightBrace));
        }

        [Test]
        public void Lexer_ShouldHandleWhitespaceCorrectly()
        {
            // Arrange
            var source = "  function   test  (  )  ->  int  ";
            _lexer = new CadenzaLexer(source);

            // Act
            var tokens = _lexer.ScanTokens();

            // Assert
            Assert.That(tokens.Count, Is.EqualTo(7)); // function, test, (, ), ->, int, EOF
            Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Function));
            Assert.That(tokens[1].Type, Is.EqualTo(TokenType.Identifier));
            Assert.That(tokens[1].Lexeme, Is.EqualTo("test"));
        }

        [Test]
        public void Lexer_ShouldTokenizeNestedBraces()
        {
            // Arrange
            var source = "{ { } }";
            _lexer = new CadenzaLexer(source);

            // Act
            var tokens = _lexer.ScanTokens();

            // Assert
            Assert.That(tokens.Count, Is.EqualTo(5)); // {, {, }, }, EOF
            Assert.That(tokens[0].Type, Is.EqualTo(TokenType.LeftBrace));
            Assert.That(tokens[1].Type, Is.EqualTo(TokenType.LeftBrace));
            Assert.That(tokens[2].Type, Is.EqualTo(TokenType.RightBrace));
            Assert.That(tokens[3].Type, Is.EqualTo(TokenType.RightBrace));
        }

        [Test]
        public void Lexer_ShouldTokenizeAllBinaryOperators()
        {
            // Arrange
            var source = "+ - * / > < >= <= == != && ||";
            _lexer = new CadenzaLexer(source);

            // Act
            var tokens = _lexer.ScanTokens();

            // Assert
            var expectedTypes = new[] 
            {
                TokenType.Plus, TokenType.Minus, TokenType.Multiply, TokenType.Divide,
                TokenType.Greater, TokenType.Less, TokenType.GreaterEqual, TokenType.LessEqual,
                TokenType.Equal, TokenType.NotEqual, TokenType.And, TokenType.Or
            };
            
            for (int i = 0; i < expectedTypes.Length; i++)
            {
                Assert.That(tokens[i].Type, Is.EqualTo(expectedTypes[i]), $"Token at index {i} should be {expectedTypes[i]}");
            }
        }

        [Test]
        public void Lexer_ShouldTokenizeComplexStringInterpolation()
        {
            // Arrange
            var source = "$\"Name: {user.name}, Age: {user.age}, Status: {getStatus()}\"";
            _lexer = new CadenzaLexer(source);

            // Act
            var tokens = _lexer.ScanTokens();

            // Assert
            Assert.That(tokens.Count, Is.EqualTo(2)); // string interpolation, EOF
            Assert.That(tokens[0].Type, Is.EqualTo(TokenType.StringInterpolation));
            Assert.That(tokens[0].Literal, Is.Not.Null);
        }

        [Test]
        public void Lexer_ShouldTokenizeAllResultKeywords()
        {
            // Arrange
            var source = "Result Ok Error";
            _lexer = new CadenzaLexer(source);

            // Act
            var tokens = _lexer.ScanTokens();

            // Assert
            Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Result));
            Assert.That(tokens[1].Type, Is.EqualTo(TokenType.Ok));
            Assert.That(tokens[2].Type, Is.EqualTo(TokenType.Error));
        }

        [Test]
        public void Lexer_ShouldPreserveSourceLocationAccurately()
        {
            // Arrange
            var source = "function\ntest\n()";
            _lexer = new CadenzaLexer(source);

            // Act
            var tokens = _lexer.ScanTokens();

            // Assert
            Assert.That(tokens[0].Line, Is.EqualTo(1));
            Assert.That(tokens[0].Column, Is.EqualTo(1));
            Assert.That(tokens[1].Line, Is.EqualTo(2));
            Assert.That(tokens[1].Column, Is.EqualTo(1));
            Assert.That(tokens[2].Line, Is.EqualTo(3));
            Assert.That(tokens[2].Column, Is.EqualTo(1));
        }

        [Test]
        public void Lexer_ShouldHandleLargeNumbers()
        {
            // Arrange
            var source = "1234567890";
            _lexer = new CadenzaLexer(source);

            // Act
            var tokens = _lexer.ScanTokens();

            // Assert
            Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Number));
            Assert.That(tokens[0].Literal, Is.EqualTo(1234567890));
        }

        [Test]
        public void Lexer_ShouldThrowOnUnterminatedStringInterpolation()
        {
            // Arrange
            var source = "$\"unterminated {name";
            _lexer = new CadenzaLexer(source);

            // Act & Assert
            Assert.Throws<Exception>(() => _lexer.ScanTokens());
        }

        [Test]
        public void Lexer_ShouldThrowOnInvalidEscapeSequence()
        {
            // Arrange
            var source = "\"invalid\\q escape\"";
            _lexer = new CadenzaLexer(source);

            // Act
            var tokens = _lexer.ScanTokens();

            // Assert - Invalid escape sequences are treated as literal characters
            Assert.That(tokens[0].Type, Is.EqualTo(TokenType.String));
            Assert.That(tokens[0].Literal, Is.EqualTo("invalidq escape"));
        }
    }
}