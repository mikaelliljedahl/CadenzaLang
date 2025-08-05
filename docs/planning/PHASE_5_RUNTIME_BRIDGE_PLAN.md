# Phase 5 - Runtime Bridge Fixes Plan

## Overview
Fix the Cadenza.Runtime bridge to enable .cdz tools compilation. This is the critical path blocker for self-hosting migration.

## Current Issues
1. **Namespace Access**: Generated C# code can't access `Cadenza.Runtime` namespace
2. **Missing Methods**: Runtime lacks methods that .cdz tools expect
3. **Syntax Incompatibilities**: Cadenza syntax doesn't transpile correctly to C#
4. **Result Type Integration**: Match expressions and Result patterns broken

## Tasks

### Task 1: Fix Namespace Integration
**Problem**: `Cadenza.Runtime` not accessible from generated C# code
**Solution**: 
- Add proper `using Cadenza.Runtime;` to generated C# files
- Ensure runtime namespace is included in compilation references
- Test with simple runtime call

### Task 2: Add Missing Runtime Methods
**Missing from analysis of linter.cdz compilation errors**:
- `Cadenza.Runtime.StringRuntime.ToString(int)` ✅ ADDED
- `Cadenza.Runtime.FileSystemRuntime.FindFiles()` ✅ ADDED  
- `Cadenza.Runtime.ProcessRuntime.GetExitCode()` ✅ ADDED
- `Cadenza.Runtime.ProcessRuntime.GetErrorOutput()` ✅ ADDED
- `Cadenza.Runtime.ConsoleRuntime.WriteLine()` ✅ ADDED

### Task 3: Fix Cadenza Transpiler Issues
**Problems identified**:
- `List<T>.length` → should be `List<T>.Count` in C#
- `true`/`false` literals not recognized
- `match` expressions don't generate C# code
- `Result<T,E>` pattern matching broken
- Variable scoping in match blocks

### Task 4: Test Runtime Bridge
**Test with existing .cdz tools**:
```bash
# Target: Get these to compile
./bin/release/cadenzac-core --compile src/Cadenza.Tools/linter.cdz -o bin/linter.exe
./bin/release/cadenzac-core --compile src/Cadenza.Tools/dev-server.cdz -o bin/dev-server.exe
./bin/release/cadenzac-core --compile src/Cadenza.Tools/simple-dev-server.cdz -o bin/simple-dev-server.exe
```

### Task 5: Runtime Bridge Testing
**Create test .cdz file**:
```cadenza
function test_runtime() uses [Logging, FileSystem] -> Result<string, string> {
    Cadenza.Runtime.LoggingRuntime.LogInfo("Testing runtime bridge")
    let files = Cadenza.Runtime.FileSystemRuntime.FindFiles(".", "*.cdz", true)
    Cadenza.Runtime.ConsoleRuntime.WriteLine("Found files: " + files.length.ToString())
    return Ok("Runtime bridge working")
}
```

## Expected Outcomes
- All .cdz tools compile successfully
- Runtime bridge methods work correctly
- Foundation for self-hosting migration complete
- Can proceed with tooling migration

## Success Criteria
- [ ] `linter.cdz` compiles to working executable
- [ ] `dev-server.cdz` compiles to working executable  
- [ ] `simple-dev-server.cdz` compiles to working executable
- [ ] Runtime bridge test passes
- [ ] All Cadenza.Runtime methods accessible from .cdz code

## Priority
**HIGH** - This is the critical path for Phase 5 self-hosting migration.