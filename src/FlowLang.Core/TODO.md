# FlowLang Core TODO - Development Priorities

## ✅ TRANSPILER STATUS: MULTI-MODULE COMPILATION WORKING!

**MAJOR BREAKTHROUGH: Multi-module import resolution is now fully functional!**

### 🎉 DECEMBER 2024 MILESTONE - MULTI-MODULE COMPILATION ACHIEVED

**What Changed:**
- **Import Resolution**: `import Math.{add, multiply}` now generates correct qualified C# calls
- **Export Syntax**: Both `export add, multiply` and `export { add, multiply }` work
- **End-to-End Tested**: Full FlowLang → C# → execution pipeline proven working

**Proof of Success:**
```
Testing FlowLang import resolution...
add(5, 3) = 8
multiply(8, 2) = 16
Final result: 16
✅ SUCCESS: FlowLang import resolution works!
```

---

## 🚀 NEXT MAJOR FEATURE: DIRECT COMPILATION WITH ROSLYN

### Overview
Add native compilation support to FlowLang using Roslyn's compilation capabilities instead of transpiling to C# source files. This will provide a more efficient compilation pipeline and better user experience.

### Current Architecture Analysis
- **Existing Infrastructure**: FlowLang already uses Microsoft.CodeAnalysis.CSharp v4.5.0 for syntax tree generation
- **CSharpGenerator**: Produces `SyntaxTree` objects via `GenerateFromAST()` method
- **Validation**: Test suite uses `CSharpCompilation.Create()` to validate generated code
- **Ready Foundation**: All necessary components for direct compilation already exist

### Implementation Plan

#### Phase 1: Core Compilation Infrastructure
**Location**: `src/FlowLang.Core/flowc-core.cs`

**1.1 Extend CSharpGenerator Class**
- Add `CompileToAssembly(Program program, string outputPath)` method
- Leverage existing `GenerateFromAST()` for syntax tree generation
- Use `CSharpCompilation.Create()` pattern from test validation code
- Support both `OutputKind.ConsoleApplication` and `OutputKind.DynamicallyLinkedLibrary`

**1.2 Reference Resolution System**
```csharp
private MetadataReference[] GetDefaultReferences()
{
    return new[]
    {
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location)
    };
}
```

**1.3 Compilation Method Implementation**
```csharp
public CompilationResult CompileToAssembly(Program program, string outputPath)
{
    // 1. Generate syntax tree (existing functionality)
    var syntaxTree = GenerateFromAST(program);
    
    // 2. Create compilation
    var compilation = CSharpCompilation.Create(
        Path.GetFileNameWithoutExtension(outputPath),
        new[] { syntaxTree },
        GetDefaultReferences(),
        new CSharpCompilationOptions(OutputKind.ConsoleApplication)
    );
    
    // 3. Emit assembly
    var result = compilation.Emit(outputPath);
    
    // 4. Return diagnostics and success status
    return new CompilationResult(result.Success, result.Diagnostics);
}
```

#### Phase 2: CLI Integration
**Location**: `src/FlowLang.Core/flowc-core.cs` (FlowCoreProgram class)

**2.1 Command Line Options**
- Add `--compile` flag: Compile to assembly instead of transpiling
- Add `--output` flag: Specify output assembly path (default: `{filename}.exe`)
- Add `--run` flag: Compile and execute in one step
- Add `--library` flag: Generate DLL instead of executable

**2.2 Enhanced Main Method**
```csharp
public static async Task<int> Main(string[] args)
{
    var options = ParseArguments(args);
    
    if (options.Compile)
    {
        return await CompileFlow(options);
    }
    else
    {
        return await TranspileFlow(options); // Existing behavior
    }
}
```

#### Phase 3: Error Reporting Enhancement
- Map Roslyn `Diagnostic` objects back to FlowLang source locations
- Enhance error messages to reference FlowLang syntax instead of generated C#
- Implement `FlowLangDiagnostic` wrapper class

#### Phase 4: Performance Optimizations
- Cache `CSharpCompilation` objects for incremental builds
- Track file dependencies and modification times
- Implement `CompilationCache` class

### Benefits

**Immediate Benefits**
- **Performance**: 30-50% faster compilation (eliminates intermediate C# file I/O)
- **User Experience**: Single command to compile and run FlowLang programs
- **Memory Efficiency**: No string manipulation of C# source code
- **Cleaner Pipeline**: Direct FlowLang → Assembly compilation

**Long-term Benefits**
- **Incremental Compilation**: Foundation for faster rebuilds
- **Better Debugging**: Direct source mapping from FlowLang to IL
- **Tool Integration**: Easier IDE integration with direct compilation
- **Multi-target Support**: Foundation for JavaScript, WASM, and native targets

### Implementation Priority

**High Priority (Sprint 1)**
1. Phase 1: Core compilation infrastructure
2. Phase 2: Basic CLI integration (--compile flag)
3. Basic testing infrastructure

**Medium Priority (Sprint 2)**
1. Phase 2: Advanced CLI options (--run, --library)
2. Phase 3: Enhanced error reporting
3. Performance benchmarks

**Low Priority (Sprint 3)**
1. Phase 4: Performance optimizations
2. Advanced source location mapping
3. Parallel compilation support

### Technical Considerations

**Backward Compatibility**
- Maintain existing transpilation workflow as default
- Add compilation as opt-in feature via command line flags
- Preserve all existing test cases and examples

**Success Metrics**
- [ ] Can compile simple FlowLang programs to executables
- [ ] Generated executables run correctly
- [ ] Compilation time < 80% of transpile + dotnet build time
- [ ] Clear error messages referencing FlowLang syntax
- [ ] Backward compatibility with existing workflows

---

## Current Known Issues & Improvements Needed

### Parser Enhancement Needs

#### Complex Nested Expressions
- **Issue**: Parser fails on complex nested if-else expressions
- **Priority**: Medium
- **Impact**: Limits expressiveness of conditional logic

#### Error Reporting
- **Issue**: Parser errors could be more descriptive
- **Priority**: Low  
- **Impact**: Developer experience

### Transpiler Code Generation Gaps

#### Non-Standard Type Casing (Partial Issue)
- **Status**: PARTIALLY RESOLVED - Most types now use correct C# keywords
- **Implementation**: MapFlowLangTypeToCSharp function converts most FlowLang types to proper C# types
- **Remaining Issue**: Some parameter types in XML docs still show as `String` instead of `string`
- **Priority**: Low
- **Impact**: Code consistency and professional appearance

#### Complex Expressions Support
- **Issue**: Limited nested expression support
- **Priority**: Medium
- **Impact**: Reduces language expressiveness for complex calculations

### Future Enhancements

#### Additional Type System Features
- **Pattern Matching**: Extend beyond basic Result<T,E> matching
- **Advanced Generics**: Support for more complex generic constraints
- **Union Types**: Consider adding union type support

#### Performance Optimizations
- **Incremental Compilation**: Cache parsed AST for faster rebuilds
- **Parallel Processing**: Multi-file compilation optimization
- **Memory Management**: Reduce memory footprint during compilation