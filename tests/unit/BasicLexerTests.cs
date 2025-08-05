using Cadenza.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cadenza.Tests.Unit
{
    [TestFixture]
    public class BasicLexerTests
    {
        private CadenzaLexer _lexer = null!;

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
            var source = "123 456";
            _lexer = new CadenzaLexer(source);

            // Act
            var tokens = _lexer.ScanTokens();

            // Assert
            Assert.That(tokens.Count, Is.EqualTo(3)); // 123, 456, EOF
            Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Number));
            Assert.That(tokens[0].Literal, Is.EqualTo(123));
            Assert.That(tokens[1].Type, Is.EqualTo(TokenType.Number));
            Assert.That(tokens[1].Literal, Is.EqualTo(456));
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
    }
}