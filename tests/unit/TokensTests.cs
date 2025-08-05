using NUnit.Framework;
using Cadenza.Core;

namespace Cadenza.Tests.Unit
{

[TestFixture]
public class TokensTests
{
    [Test]
    public void Tokens_Token_ShouldSetAllProperties()
    {
        // Arrange
        var type = TokenType.Identifier;
        var lexeme = "testVariable";
        var literal = "literal_value";
        var line = 5;
        var column = 10;

        // Act
        var token = new Token(type, lexeme, literal, line, column);

        // Assert
        Assert.That(token.Type, Is.EqualTo(type));
        Assert.That(token.Lexeme, Is.EqualTo(lexeme));
        Assert.That(token.Literal, Is.EqualTo(literal));
        Assert.That(token.Line, Is.EqualTo(line));
        Assert.That(token.Column, Is.EqualTo(column));
    }

    [Test]
    public void Tokens_Token_ShouldAcceptNull_WhenNullLiteral()
    {
        // Arrange & Act
        var token = new Token(TokenType.Identifier, "test", null, 1, 1);

        // Assert
        Assert.That(token.Type, Is.EqualTo(TokenType.Identifier));
        Assert.That(token.Lexeme, Is.EqualTo("test"));
        Assert.That(token.Literal, Is.Null);
        Assert.That(token.Line, Is.EqualTo(1));
        Assert.That(token.Column, Is.EqualTo(1));
    }

    [Test]
    public void Tokens_TokenType_ShouldContainExpectedValues()
    {
        // Test that key token types exist
        Assert.That(System.Enum.IsDefined(typeof(TokenType), TokenType.Function));
        Assert.That(System.Enum.IsDefined(typeof(TokenType), TokenType.Return));
        Assert.That(System.Enum.IsDefined(typeof(TokenType), TokenType.If));
        Assert.That(System.Enum.IsDefined(typeof(TokenType), TokenType.Match));
        Assert.That(System.Enum.IsDefined(typeof(TokenType), TokenType.Ok));
        Assert.That(System.Enum.IsDefined(typeof(TokenType), TokenType.Error));
        Assert.That(System.Enum.IsDefined(typeof(TokenType), TokenType.EOF));
    }

    [Test]
    public void Tokens_TokenType_ShouldHaveOperatorTokens()
    {
        // Test that operator tokens exist
        Assert.That(System.Enum.IsDefined(typeof(TokenType), TokenType.Plus));
        Assert.That(System.Enum.IsDefined(typeof(TokenType), TokenType.Minus));
        Assert.That(System.Enum.IsDefined(typeof(TokenType), TokenType.Equal));
        Assert.That(System.Enum.IsDefined(typeof(TokenType), TokenType.NotEqual));
        Assert.That(System.Enum.IsDefined(typeof(TokenType), TokenType.And));
        Assert.That(System.Enum.IsDefined(typeof(TokenType), TokenType.Or));
        Assert.That(System.Enum.IsDefined(typeof(TokenType), TokenType.Question));
    }

    [Test]
    public void Tokens_TokenType_ShouldHaveEffectTokens()
    {
        // Test that effect tokens exist
        Assert.That(System.Enum.IsDefined(typeof(TokenType), TokenType.Database));
        Assert.That(System.Enum.IsDefined(typeof(TokenType), TokenType.Network));
        Assert.That(System.Enum.IsDefined(typeof(TokenType), TokenType.Logging));
        Assert.That(System.Enum.IsDefined(typeof(TokenType), TokenType.FileSystem));
        Assert.That(System.Enum.IsDefined(typeof(TokenType), TokenType.Memory));
        Assert.That(System.Enum.IsDefined(typeof(TokenType), TokenType.IO));
    }

    [Test]
    public void Tokens_TokenType_ShouldHaveModuleTokens()
    {
        // Test that module system tokens exist
        Assert.That(System.Enum.IsDefined(typeof(TokenType), TokenType.Module));
        Assert.That(System.Enum.IsDefined(typeof(TokenType), TokenType.Import));
        Assert.That(System.Enum.IsDefined(typeof(TokenType), TokenType.Export));
        Assert.That(System.Enum.IsDefined(typeof(TokenType), TokenType.From));
    }

    [Test]
    public void Tokens_TokenType_ShouldHaveUITokens()
    {
        // Test that UI component tokens exist
        Assert.That(System.Enum.IsDefined(typeof(TokenType), TokenType.Component));
        Assert.That(System.Enum.IsDefined(typeof(TokenType), TokenType.State));
        Assert.That(System.Enum.IsDefined(typeof(TokenType), TokenType.Render));
        Assert.That(System.Enum.IsDefined(typeof(TokenType), TokenType.EventHandler));
    }
}
}