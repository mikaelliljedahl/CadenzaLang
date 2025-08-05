using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System;
using System.IO;
using System.Collections.Generic;
using Cadenza.Core;

namespace Cadenza.Tests.Performance
{
    [MemoryDiagnoser]
    [SimpleJob]
    public class TranspilerBenchmarks
    {
        private CadenzaTranspiler _transpiler;
        private string _simpleFunction;
        private string _complexFunction;
        private string _moduleCode;
        private string _largeProgram;

        [GlobalSetup]
        public void Setup()
        {
            _transpiler = new CadenzaTranspiler();

            _simpleFunction = "function add(a: int, b: int) -> int { return a + b }";

            _complexFunction = @"
                function complexCalculation(x: int, y: int, z: int) -> Result<int, string> {
                    if x < 0 {
                        return Error(""x must be positive"")
                    }
                    
                    guard y > 0 else {
                        return Error(""y must be greater than zero"")
                    }
                    
                    let intermediate = x + y * z
                    let result = processValue(intermediate)?
                    
                    if result > 1000 {
                        return Ok(result)
                    } else {
                        return Error(""result too small"")
                    }
                }";

            _moduleCode = @"
                module Math {
                    pure function add(a: int, b: int) -> int {
                        return a + b
                    }
                    
                    pure function multiply(a: int, b: int) -> int {
                        return a * b
                    }
                    
                    function divide(a: int, b: int) -> Result<int, string> {
                        if b == 0 {
                            return Error(""Division by zero"")
                        } else {
                            return Ok(a / b)
                        }
                    }
                    
                    export {add, multiply, divide}
                }
                
                import Math.{add, multiply}
                
                function calculate(x: int, y: int) -> int {
                    let sum = Math.add(x, y)
                    return Math.multiply(sum, 2)
                }";

            _largeProgram = GenerateLargeProgram(100); // 100 functions
        }

        [Benchmark]
        public string TranspileSimpleFunction()
        {
            return _transpiler.TranspileFromSource(_simpleFunction);
        }

        [Benchmark]
        public string TranspileComplexFunction()
        {
            return _transpiler.TranspileFromSource(_complexFunction);
        }

        [Benchmark]
        public string TranspileModuleCode()
        {
            return _transpiler.TranspileFromSource(_moduleCode);
        }

        [Benchmark]
        public string TranspileLargeProgram()
        {
            return _transpiler.TranspileFromSource(_largeProgram);
        }

        [Benchmark]
        public List<Token> LexSimpleFunction()
        {
            var lexer = new CadenzaLexer(_simpleFunction);
            return lexer.ScanTokens();
        }

        [Benchmark]
        public List<Token> LexComplexFunction()
        {
            var lexer = new CadenzaLexer(_complexFunction);
            return lexer.ScanTokens();
        }

        [Benchmark]
        public ProgramNode ParseSimpleFunction()
        {
            var lexer = new CadenzaLexer(_simpleFunction);
            var tokens = lexer.ScanTokens();
            var parser = new CadenzaParser(tokens);
            return parser.Parse();
        }

        [Benchmark]
        public ProgramNode ParseComplexFunction()
        {
            var lexer = new CadenzaLexer(_complexFunction);
            var tokens = lexer.ScanTokens();
            var parser = new CadenzaParser(tokens);
            return parser.Parse();
        }

        private string GenerateLargeProgram(int functionCount)
        {
            var functions = new List<string>();
            
            for (int i = 0; i < functionCount; i++)
            {
                var func = $@"
                    function func{i}(a: int, b: int) -> int {{
                        if a > b {{
                            let result = a + b
                            return result * 2
                        }} else {{
                            return a - b
                        }}
                    }}";
                functions.Add(func);
            }

            return string.Join("\n\n", functions);
        }
    }

    public class BenchmarkRunner
    {
        public static void RunBenchmarks()
        {
            BenchmarkDotNet.Running.BenchmarkRunner.Run<TranspilerBenchmarks>();
        }
    }
}