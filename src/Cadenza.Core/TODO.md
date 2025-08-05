# Cadenza Compiler TODO

This document tracks missing features and known issues in the Cadenza compiler.
## 🚨 CRITICAL: Missing Type Definition Support

### Overview
Multi-file project compilation fails due to missing support for `type` definitions in the parser and compiler.

### The Problem
Files using `type TypeName { ... }` syntax fail to parse with error "Unexpected token '{' at line X".

**Example failing syntax:**
```cadenza
type UserData {
    id: int,
    name: string,
    email: string
}
```

**Files affected:**
- All sample project modules in `examples/sample-project/src/`
- Many examples throughout the codebase
- Any multi-file project using structured data

### Root Cause Analysis
The type definition feature is completely missing from the compiler:

1. **Missing `Type` token** in `Tokens.cs` TokenType enum
2. **Missing keyword recognition** in `Lexer.cs` Keywords dictionary  
3. **Missing AST node** - No `TypeDeclaration` class in `Ast.cs`
4. **Missing parser logic** - No type parsing in `Parser.cs` ParseStatement()
5. **Missing code generation** - No type transpilation in `Compiler.cs` GenerateStatement()

### Implementation Requirements
Type definitions should transpile to C# classes or records:

```cadenza
type UserData {
    id: int,
    name: string,
    email: string
}
```

Should become:
```csharp
public class UserData 
{
    public int id { get; set; }
    public string name { get; set; }
    public string email { get; set; }
}
```

### Priority: **BLOCKING**
Multi-file compilation cannot work without type definitions since most real projects use structured data types.

## 🔧 CURRENT ISSUES

### High Priority Issues

#### Match Expression Transpilation Incomplete
- **Status**: Match expressions parse correctly but transpile to hardcoded values
- **Problem**: Parser generates proper AST but transpiler doesn't handle MatchExpression case
- **Impact**: Match statements don't actually work despite parsing successfully
- **Priority**: High

#### Complex Control Flow Parsing
- **Status**: Some complex nested if-else expressions still fail to parse  
- **Files affected**: `comprehensive_control_flow_demo.cdz` and similar complex examples
- **Impact**: Limits advanced conditional logic patterns
- **Priority**: Medium

### Medium Priority Issues  

#### Error Reporting Enhancement
- **Issue**: Parser errors could be more descriptive with better location information
- **Impact**: Developer experience when debugging syntax errors
- **Priority**: Medium

#### List Types and Array Access
- **Issue**: No support for List<T> types or array indexing syntax
- **Impact**: Limits data structure capabilities
- **Priority**: Medium (can work around with basic types)

### Low Priority Issues

#### LSP Infrastructure
- **Status**: Basic stub implementations exist but full LSP server not implemented
- **Files affected**: LSP test files use placeholder classes
- **Impact**: No IDE integration features
- **Priority**: Low (not core to compilation)

#### Performance Optimizations
- **Areas**: Incremental compilation, parallel processing, memory management
- **Impact**: Compilation speed for large projects
- **Priority**: Low (current performance acceptable for development)