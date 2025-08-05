using System;
using System.Collections.Generic;
using System.Text;

namespace Cadenza.Core;

public class CadenzaLexer
{
    private readonly string _source;
    private readonly List<Token> _tokens = new();
    private int _start = 0;
    private int _current = 0;
    private int _line = 1;
    private int _column = 1;

    private static readonly Dictionary<string, TokenType> Keywords = new()
    {
        {"function", TokenType.Function},
        {"return", TokenType.Return},
        {"if", TokenType.If},
        {"else", TokenType.Else},
        {"effects", TokenType.Effects},
        {"pure", TokenType.Pure},
        {"true", TokenType.Bool},
        {"false", TokenType.Bool},
        {"null", TokenType.Identifier}, // For now, handle null as an identifier
        {"uses", TokenType.Uses},
        {"Result", TokenType.Result},
        {"Ok", TokenType.Ok},
        {"Error", TokenType.Error},
        {"let", TokenType.Let},
        {"Some", TokenType.Some},
        {"None", TokenType.None},
        {"match", TokenType.Match},
        {"guard", TokenType.Guard},
        {"module", TokenType.Module},
        {"import", TokenType.Import},
        {"export", TokenType.Export},
        {"from", TokenType.From},
        {"type", TokenType.Type},
        {"component", TokenType.Component},
        {"state", TokenType.State},
        {"events", TokenType.Events},
        {"render", TokenType.Render},
        {"on_mount", TokenType.OnMount},
        {"event_handler", TokenType.EventHandler},
        {"declare_state", TokenType.DeclareState},
        {"app_state", TokenType.AppState},
        {"action", TokenType.Action},
        {"updates", TokenType.Updates},
        {"api_client", TokenType.ApiClient},
        {"endpoint", TokenType.Endpoint},
        {"for", TokenType.For},
        {"in", TokenType.In},
        {"where", TokenType.Where},
        {"Database", TokenType.Database},
        {"Network", TokenType.Network},
        {"Logging", TokenType.Logging},
        {"FileSystem", TokenType.FileSystem},
        {"Memory", TokenType.Memory},
        {"IO", TokenType.IO},
        {"int", TokenType.Int},
        {"string", TokenType.String_Type},
        {"bool", TokenType.Bool},
        {"List", TokenType.List},
        {"Option", TokenType.Option}
    };

    public CadenzaLexer(string source)
    {
        _source = source;
    }

    public List<Token> ScanTokens()
    {
        while (!IsAtEnd())
        {
            _start = _current;
            ScanToken();
        }

        _tokens.Add(new Token(TokenType.EOF, "", null, _line, _column));
        return _tokens;
    }

    private bool IsAtEnd() => _current >= _source.Length;

    private void ScanToken()
    {
        char c = Advance();
        
        switch (c)
        {
            case ' ':
            case '\r':
            case '\t':
                _column++;
                break;
            case '\n':
                _line++;
                _column = 1;
                break;
            case '(':
                _column++;
                AddToken(TokenType.LeftParen);
                break;
            case ')':
                _column++;
                AddToken(TokenType.RightParen);
                break;
            case '{':
                _column++;
                AddToken(TokenType.LeftBrace);
                break;
            case '}':
                _column++;
                AddToken(TokenType.RightBrace);
                break;
            case '[':
                _column++;
                AddToken(TokenType.LeftBracket);
                break;
            case ']':
                _column++;
                AddToken(TokenType.RightBracket);
                break;
            case ',':
                _column++;
                AddToken(TokenType.Comma);
                break;
            case ';':
                _column++;
                AddToken(TokenType.Semicolon);
                break;
            case ':':
                _column++;
                AddToken(TokenType.Colon);
                break;
            case '+':
                _column++;
                AddToken(TokenType.Plus);
                break;
            case '*':
                _column++;
                AddToken(TokenType.Multiply);
                break;
            case '/':
                if (Match('/'))
                {
                    _column += 2; // For the //
                    // Line comment
                    while (Peek() != '\n' && !IsAtEnd()) 
                    {
                        _column++;
                        Advance();
                    }
                }
                else if (Match('*'))
                {
                    _column += 2; // For the /*
                    // Check for specification block comment
                    ScanSpecificationOrComment();
                }
                else
                {
                    _column++;
                    AddToken(TokenType.Divide);
                }
                break;
            case '%':
                _column++;
                AddToken(TokenType.Modulo);
                break;
            case '?':
                _column++;
                AddToken(TokenType.Question);
                break;
            case '.':
                _column++;
                AddToken(TokenType.Dot);
                break;
            case '-':
                if (Match('>'))
                {
                    _column += 2; // For the ->
                    AddToken(TokenType.Arrow);
                }
                else
                {
                    _column++;
                    AddToken(TokenType.Minus);
                }
                break;
            case '=':
                if (Match('='))
                {
                    _column += 2; // For the ==
                    AddToken(TokenType.Equal);
                }
                else if (Match('>'))
                {
                    _column += 2; // For the =>
                    AddToken(TokenType.FatArrow);
                }
                else
                {
                    _column++;
                    AddToken(TokenType.Assign);
                }
                break;
            case '!':
                if (Match('='))
                {
                    _column += 2; // For the !=
                    AddToken(TokenType.NotEqual);
                }
                else
                {
                    _column++;
                    AddToken(TokenType.Not);
                }
                break;
            case '<':
                if (Match('='))
                {
                    _column += 2; // For the <=
                    AddToken(TokenType.LessEqual);
                }
                else
                {
                    _column++;
                    AddToken(TokenType.Less);
                }
                break;
            case '>':
                if (Match('='))
                {
                    _column += 2; // For the >=
                    AddToken(TokenType.GreaterEqual);
                }
                else
                {
                    _column++;
                    AddToken(TokenType.Greater);
                }
                break;
            case '&':
                if (Match('&'))
                {
                    _column += 2; // For the &&
                    AddToken(TokenType.And);
                }
                else
                {
                    _column++;
                    throw new Exception($"Unexpected character '&' at line {_line}, column {_column}");
                }
                break;
            case '|':
                if (Match('|'))
                {
                    _column += 2; // For the ||
                    AddToken(TokenType.Or);
                }
                else
                {
                    _column++;
                    throw new Exception($"Unexpected character '|' at line {_line}, column {_column}");
                }
                break;
            case '"':
                StringLiteral();
                break;
            case '$':
                if (Peek() == '"')
                {
                    _column++; // for the $
                    Advance(); // consume the "
                    InterpolatedString();
                }
                else
                {
                    _column++;
                    throw new Exception($"Unexpected character '$' at line {_line}, column {_column}");
                }
                break;
            default:
                if (IsDigit(c))
                {
                    Number();
                }
                else if (IsAlpha(c))
                {
                    Identifier();
                }
                else
                {
                    throw new Exception($"Unexpected character '{c}' at line {_line}, column {_column}");
                }
                break;
        }
    }

    private char Advance()
    {
        return _source[_current++];
    }

    private bool Match(char expected)
    {
        if (IsAtEnd()) return false;
        if (_source[_current] != expected) return false;

        _current++;
        return true;
    }

    private char Peek() => IsAtEnd() ? '\0' : _source[_current];

    private char PeekNext() => (_current + 1 >= _source.Length) ? '\0' : _source[_current + 1];

    private bool MatchSequence(string sequence)
    {
        if (_current + sequence.Length > _source.Length) return false;
        
        for (int i = 0; i < sequence.Length; i++)
        {
            if (_source[_current + i] != sequence[i]) return false;
        }
        
        // Consume the sequence
        _current += sequence.Length;
        _column += sequence.Length;
        return true;
    }

    private void ScanSpecificationOrComment()
    {
        // We're already past /*
        // Check if this is a specification block
        if (MatchSequence("spec"))
        {
            // Scan the specification content
            var specContent = new StringBuilder();
            
            // Scan until we find spec*/
            while (!IsAtEnd())
            {
                if (Peek() == 's')
                {
                    // Check if this is the end marker
                    var saved_current = _current;
                    var saved_column = _column;
                    
                    if (MatchSequence("spec*/"))
                    {
                        // Found the end - create a token with the content
                        AddToken(TokenType.SpecStart, specContent.ToString().Trim());
                        return;
                    }
                    
                    // Not the end marker, restore position and continue
                    _current = saved_current;
                    _column = saved_column;
                }
                
                if (Peek() == '\n')
                {
                    _line++;
                    _column = 1;
                    specContent.AppendLine();
                }
                else
                {
                    _column++;
                    specContent.Append(Peek());
                }
                Advance();
            }
            
            throw new Exception($"Unterminated specification block starting at line {_line}");
        }
        else
        {
            // Regular block comment - skip it
            SkipBlockComment();
        }
    }

    private void SkipBlockComment()
    {
        // We're already past /*
        while (!IsAtEnd())
        {
            if (Peek() == '*' && PeekNext() == '/')
            {
                Advance(); // consume '*'
                Advance(); // consume '/'
                break;
            }
            if (Peek() == '\n')
            {
                _line++;
                _column = 1;
            }
            else
            {
                _column++;
            }
            Advance();
        }
    }

    private void AddToken(TokenType type, object? literal = null)
    {
        string text = _source.Substring(_start, _current - _start);
        _tokens.Add(new Token(type, text, literal, _line, _column - text.Length));
    }

    private void StringLiteral()
    {
        _column++; // for the opening quote
        while (Peek() != '"' && !IsAtEnd())
        {
            if (Peek() == '\\' && PeekNext() == '"')
            {
                // Skip the escaped quote
                Advance(); // consume the backslash
                Advance(); // consume the quote
                _column += 2;
            }
            else if (Peek() == '\\' && PeekNext() != '\0')
            {
                // Skip the backslash and the next character
                Advance(); // consume the backslash
                Advance(); // consume the next character
                _column += 2;
            }
            else if (Peek() == '\n')
            {
                _line++;
                _column = 1;
                Advance();
            }
            else
            {
                _column++;
                Advance();
            }
        }

        if (IsAtEnd())
        {
            throw new Exception($"Unterminated string at line {_line}");
        }

        // The closing "
        _column++;
        Advance();

        // Trim the surrounding quotes and handle escape sequences
        string value = _source.Substring(_start + 1, _current - _start - 2);
        value = ProcessEscapeSequences(value);
        
        AddToken(TokenType.String, value);
    }

    private string ProcessEscapeSequences(string input)
    {
        var result = new StringBuilder();
        for (int i = 0; i < input.Length; i++)
        {
            if (input[i] == '\\' && i + 1 < input.Length)
            {
                char nextChar = input[i + 1];
                switch (nextChar)
                {
                    case 'n':
                        result.Append('\n');
                        i++; // Skip the next character
                        break;
                    case 't':
                        result.Append('\t');
                        i++; // Skip the next character
                        break;
                    case 'r':
                        result.Append('\r');
                        i++; // Skip the next character
                        break;
                    case '\\':
                        result.Append('\\');
                        i++; // Skip the next character
                        break;
                    case '"':
                        result.Append('"');
                        i++; // Skip the next character
                        break;
                    default:
                        // Invalid escape sequence - treat as literal characters
                        // Remove the backslash and keep the following character
                        result.Append(nextChar);
                        i++; // Skip the next character
                        break;
                }
            }
            else
            {
                result.Append(input[i]);
            }
        }
        return result.ToString();
    }

    private void InterpolatedString()
    {
        var parts = new List<object>();
        var currentPart = "";
        
        while (Peek() != '"' && !IsAtEnd())
        {
            if (Peek() == '{')
            {
                // Add the string part if not empty
                if (!string.IsNullOrEmpty(currentPart))
                {
                    parts.Add(currentPart);
                    currentPart = "";
                }
                
                Advance(); // consume '{'
                
                // Find the matching '}'
                var braceCount = 1;
                var expressionStart = _current;
                
                while (braceCount > 0 && !IsAtEnd())
                {
                    if (Peek() == '{') braceCount++;
                    else if (Peek() == '}') braceCount--;
                    Advance();
                }
                
                if (braceCount > 0)
                {
                    throw new Exception($"Unterminated interpolation expression at line {_line}");
                }
                
                // Extract the expression (excluding the closing '}')
                var expression = _source.Substring(expressionStart, _current - expressionStart - 1);
                parts.Add(new Dictionary<string, object> { ["IsExpression"] = true, ["Value"] = expression });
            }
            else
            {
                if (Peek() == '\n')
                {
                    _line++;
                    _column = 1;
                }
                currentPart += Advance();
            }
        }
        
        if (IsAtEnd())
        {
            throw new Exception($"Unterminated interpolated string at line {_line}");
        }
        
        // Add the final string part if not empty
        if (!string.IsNullOrEmpty(currentPart))
        {
            parts.Add(currentPart);
        }
        
        // The closing "
        Advance();
        
        AddToken(TokenType.StringInterpolation, parts);
    }

    private void Number()
    {
        _column++; // for the first digit
        while (IsDigit(Peek())) 
        {
            _column++;
            Advance();
        }

        // Look for a fractional part
        if (Peek() == '.' && IsDigit(PeekNext()))
        {
            // Consume the "."
            _column++;
            Advance();

            while (IsDigit(Peek()))
            {
                _column++;
                Advance();
            }
        }

        string numberStr = _source.Substring(_start, _current - _start);
        if (numberStr.Contains('.'))
        {
            AddToken(TokenType.Number, double.Parse(numberStr));
        }
        else
        {
            AddToken(TokenType.Number, int.Parse(numberStr));
        }
    }

    private void Identifier()
    {
        _column++; // for the first character
        while (IsAlphaNumeric(Peek()))
        {
            _column++;
            Advance();
        }

        string text = _source.Substring(_start, _current - _start);
        TokenType type = Keywords.ContainsKey(text) ? Keywords[text] : TokenType.Identifier;
        AddToken(type);
    }

    private bool IsDigit(char c) => c >= '0' && c <= '9';

    private bool IsAlpha(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_';

    private bool IsAlphaNumeric(char c) => IsAlpha(c) || IsDigit(c);
}