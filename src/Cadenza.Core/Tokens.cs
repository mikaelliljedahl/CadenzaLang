using System;

namespace Cadenza.Core;

public enum TokenType
{
    // Literals
    Identifier,
    Number,
    String,
    StringInterpolation,
    
    // Keywords
    Function,
    Return,
    If,
    Else,
    Effects,
    Pure,
    Uses,
    Result,
    Ok,
    Error,
    Let,
    Some,
    None,
    Match,
    
    // Effect names
    Database,
    Network,
    Logging,
    FileSystem,
    Memory,
    IO,
    
    // Types
    Int,
    String_Type,
    Bool,
    List,
    Option,
    
    // Symbols
    LeftParen,
    RightParen,
    LeftBrace,
    RightBrace,
    LeftBracket,
    RightBracket,
    Comma,
    Semicolon,
    Colon,
    Arrow,        // -> (function return type)
    FatArrow,     // => (match case)
    Assign,
    
    // Operators
    Plus,
    Minus,
    Multiply,
    Divide,
    Greater,
    Less,
    GreaterEqual,
    LessEqual,
    Equal,
    NotEqual,
    Question, // Error propagation operator ?
    
    // Logical operators
    And,      // &&
    Or,       // ||
    Not,      // !
    
    // Control flow keywords
    Guard,    // guard keyword
    
    // Module system keywords
    Module,   // module keyword
    Import,   // import keyword
    Export,   // export keyword
    From,     // from keyword
    Type,     // type keyword for type definitions
    Dot,      // . for qualified names and imports
    LeftBraceCurly,  // { for import lists
    RightBraceCurly, // } for import lists
    
    // UI Component Keywords (Phase 4)
    Component,
    State,
    Events,
    Render,
    OnMount,
    EventHandler,
    DeclareState,
    AppState,
    Action,
    Updates,
    ApiClient,
    Endpoint,
    For,
    In,
    Where,
    
    // Modulus operator
    Modulo,   // %
    
    // Specification block tokens
    SpecStart,  // /*spec
    SpecEnd,    // spec*/
    
    EOF
}

public record Token(TokenType Type, string Lexeme, object? Literal, int Line, int Column);