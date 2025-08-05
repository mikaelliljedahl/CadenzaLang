// Cadenza Core Compiler - Modular Version
// This file imports the refactored modular components

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

// All classes now imported from their individual files:
// - TokenType and Token from Tokens.cs
// - AST classes from Ast.cs  
// - CadenzaLexer from Lexer.cs
// - CadenzaParser from Parser.cs
// - CadenzaTranspiler from Transpiler.cs
// - CSharpGenerator, CompilationResult, DirectCompiler, etc. from Compiler.cs

namespace Cadenza.Core
{
    // Entry point for the modular compiler
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            return await FlowCoreProgram.RunAsync(args);
        }
    }
}