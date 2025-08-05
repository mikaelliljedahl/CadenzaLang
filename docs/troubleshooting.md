# Cadenza Troubleshooting Guide

This guide helps you diagnose and fix common issues when using Cadenza. It covers installation problems, compilation errors, runtime issues, and performance concerns.

## Table of Contents

1. [Installation Issues](#installation-issues)
2. [Compilation Errors](#compilation-errors)
3. [Language-Specific Issues](#language-specific-issues)
4. [CLI Problems](#cli-problems)
5. [Performance Issues](#performance-issues)
6. [Generated C# Issues](#generated-c-issues)
7. [Module System Problems](#module-system-problems)
8. [Development Environment](#development-environment)
9. [FAQ](#faq)

## Installation Issues

### .NET SDK Not Found

**Problem**: Error message "dotnet command not found" or "SDK not found"

**Solution**:
1. Install .NET 10.0 SDK or later from [Microsoft's website](https://dotnet.microsoft.com/download)
2. Verify installation:
   ```bash
   dotnet --version
   ```
3. Add .NET to your PATH if necessary
4. Restart your terminal/command prompt

### Cadenza Build Fails

**Problem**: Compilation errors when building the transpiler

**Symptoms**:
```bash
dotnet build
# Error: CS0246: The type or namespace name 'Microsoft' could not be found
```

**Solution**:
1. Ensure you're in the correct directory:
   ```bash
   cd src
   dotnet build
   ```
2. Restore NuGet packages:
   ```bash
   dotnet restore
   dotnet build
   ```
3. Check .NET version compatibility:
   ```bash
   dotnet --list-sdks
   ```

### Permission Issues

**Problem**: Access denied errors when creating projects or files

**Solution**:
1. **Windows**: Run as Administrator
2. **Linux/macOS**: Check file permissions:
   ```bash
   chmod +x cadenzac
   sudo chown -R $USER:$USER /path/to/cadenza
   ```
3. Avoid running in system directories

## Compilation Errors

### Lexical Errors

**Problem**: "Unexpected character" errors

**Common Issues**:
```cadenza
// ❌ Invalid: Single quotes not supported
let message = 'Hello'

// ✅ Correct: Use double quotes
let message = "Hello"
```

```cadenza
// ❌ Invalid: Underscore in numbers not supported
let big_number = 1_000_000

// ✅ Correct: No separators
let bigNumber = 1000000
```

**Solution**: Check Cadenza syntax rules in the [Language Reference](language-reference.md)

### Syntax Errors

**Problem**: Parser errors due to incorrect syntax

**Common Issues**:

1. **Missing type annotations**:
   ```cadenza
   // ❌ Invalid: Missing parameter types
   function add(a, b) -> int {
       return a + b
   }
   
   // ✅ Correct: Include parameter types
   function add(a: int, b: int) -> int {
       return a + b
   }
   ```

2. **Missing return type**:
   ```cadenza
   // ❌ Invalid: Missing return type
   function greet(name: string) {
       return "Hello, " + name
   }
   
   // ✅ Correct: Include return type
   function greet(name: string) -> string {
       return "Hello, " + name
   }
   ```

3. **Incorrect function syntax**:
   ```cadenza
   // ❌ Invalid: Missing function keyword
   greet(name: string) -> string {
       return "Hello"
   }
   
   // ✅ Correct: Include function keyword
   function greet(name: string) -> string {
       return "Hello"
   }
   ```

### Type Errors

**Problem**: Type mismatches or invalid type usage

**Common Issues**:

1. **Result type syntax**:
   ```cadenza
   // ❌ Invalid: Incorrect Result syntax
   function divide(a: int, b: int) -> Result(int, string) {
       return Ok(a / b)
   }
   
   // ✅ Correct: Use angle brackets
   function divide(a: int, b: int) -> Result<int, string> {
       return Ok(a / b)
   }
   ```

2. **Effect system violations**:
   ```cadenza
   // ❌ Invalid: Pure function cannot have effects
   pure function saveData(data: string) uses [Database] -> int {
       return 42
   }
   
   // ✅ Correct: Remove pure or remove effects
   function saveData(data: string) uses [Database] -> int {
       return 42
   }
   ```

## Language-Specific Issues

### Result Type Problems

**Problem**: Confusion about Result type usage

**Common Issues**:

1. **Not using error propagation**:
   ```cadenza
   // ❌ Inefficient: Manual error checking
   function complexOperation() -> Result<int, string> {
       let result1 = step1()
       if result1.IsError {
           return result1
       }
       let result2 = step2(result1.Value)
       if result2.IsError {
           return result2
       }
       return Ok(result2.Value)
   }
   
   // ✅ Better: Use error propagation
   function complexOperation() -> Result<int, string> {
       let result1 = step1()?
       let result2 = step2(result1)?
       return Ok(result2)
   }
   ```

2. **Mixing error handling paradigms**:
   ```cadenza
   // ❌ Don't mix exceptions with Results
   function riskyOperation() -> Result<int, string> {
       try {
           // Exception-based code doesn't work in Cadenza
           return Ok(42)
       } catch {
           return Error("Failed")
       }
   }
   
   // ✅ Use Result types consistently
   function riskyOperation() -> Result<int, string> {
       if someCondition {
           return Error("Failed")
       }
       return Ok(42)
   }
   ```

### Effect System Issues

**Problem**: Effect declarations not working correctly

**Common Issues**:

1. **Undeclared effects**:
   ```cadenza
   // ❌ Error: Function uses Database effect without declaring it
   function saveUser(name: string) -> Result<int, string> {
       return DatabaseService.save(name)  // DatabaseService.save uses [Database]
   }
   
   // ✅ Correct: Declare all used effects
   function saveUser(name: string) uses [Database] -> Result<int, string> {
       return DatabaseService.save(name)
   }
   ```

2. **Invalid effect names**:
   ```cadenza
   // ❌ Invalid: CustomEffect is not a known effect
   function customOperation() uses [CustomEffect] -> int {
       return 42
   }
   
   // ✅ Correct: Use valid effects
   function customOperation() uses [Database] -> int {
       return 42
   }
   ```

3. **Pure functions calling effectful functions**:
   ```cadenza
   // ❌ Invalid: Pure function cannot call effectful function
   pure function calculate() -> int {
       return effectfulOperation()  // effectfulOperation uses effects
   }
   
   // ✅ Correct: Remove pure or make function effectful
   function calculate() uses [Database] -> int {
       return effectfulOperation()
   }
   ```

### String Interpolation Issues

**Problem**: String interpolation not working as expected

**Common Issues**:

1. **Missing $ prefix**:
   ```cadenza
   // ❌ Invalid: Missing $ for interpolation
   let message = "Hello, {name}!"
   
   // ✅ Correct: Include $ prefix
   let message = $"Hello, {name}!"
   ```

2. **Unmatched braces**:
   ```cadenza
   // ❌ Invalid: Unmatched brace
   let message = $"Value: {calculate(x}"
   
   // ✅ Correct: Match braces properly
   let message = $"Value: {calculate(x)}"
   ```

## CLI Problems

### Command Not Found

**Problem**: `cadenzac` command not recognized

**Solution**:
1. Use the full dotnet command:
   ```bash
   dotnet run --project src/cadenzac.csproj -- --version
   ```
2. Create an alias for convenience:
   ```bash
   # Linux/macOS
   alias cadenzac="dotnet run --project /path/to/cadenza/src/cadenzac.csproj --"
   
   # Windows PowerShell
   function cadenzac { dotnet run --project C:\path\to\cadenza\src\cadenzac.csproj -- $args }
   ```

### Project Creation Fails

**Problem**: `cadenzac new` command fails

**Common Issues**:
1. **Directory already exists**:
   ```bash
   cadenzac new my-project
   # Error: Directory 'my-project' already exists
   
   # Solution: Use different name or remove existing directory
   rm -rf my-project
   cadenzac new my-project
   ```

2. **Permission issues**:
   ```bash
   # Solution: Check directory permissions
   ls -la
   chmod 755 .
   ```

### Build Command Issues

**Problem**: `cadenzac build` fails

**Common Issues**:
1. **No cadenzac.json found**:
   ```bash
   # Solution: Create project first or add cadenzac.json
   cadenzac new my-project
   cd my-project
   cadenzac build
   ```

2. **No .cdz files found**:
   ```bash
   # Solution: Add Cadenza files to src/ directory
   echo 'function main() -> string { return "Hello" }' > src/main.cdz
   cadenzac build
   ```

### Test Command Problems

**Problem**: `cadenzac test` doesn't find tests

**Solution**:
1. Create tests directory:
   ```bash
   mkdir tests
   echo 'function test_example() -> bool { return true }' > tests/example_test.cdz
   ```
2. Ensure test files have `.cdz` extension
3. Check test file syntax

## Performance Issues

### Slow Compilation

**Problem**: Cadenza compilation is taking too long

**Diagnosis**:
```bash
# Time the compilation
time dotnet run --project src/cadenzac.csproj -- build
```

**Solutions**:
1. **Reduce file size**: Break large files into smaller modules
2. **Check for infinite loops**: Review complex logic
3. **Use incremental compilation**: Only recompile changed files

### Memory Usage

**Problem**: High memory usage during compilation

**Diagnosis**:
```bash
# Monitor memory usage
top -p $(pgrep dotnet)
```

**Solutions**:
1. **Process files individually**:
   ```bash
   # Instead of building everything at once
   cadenzac run src/file1.cdz
   cadenzac run src/file2.cdz
   ```
2. **Increase available memory**
3. **Check for memory leaks** in complex expressions

### Generated Code Performance

**Problem**: Generated C# code runs slowly

**Solutions**:
1. **Profile the generated code**:
   ```bash
   dotnet run --configuration Release
   ```
2. **Optimize Cadenza algorithms**
3. **Check for unnecessary string concatenations**
4. **Use appropriate data structures**

## Generated C# Issues

### Compilation Errors in Generated Code

**Problem**: Generated C# code doesn't compile

**Diagnosis**:
1. **Examine generated code**:
   ```bash
   cadenzac run myfile.cdz
   # Check the generated C# output for issues
   ```

2. **Common issues**:
   - Missing using statements
   - Invalid C# syntax
   - Type mismatches

**Solutions**:
1. **Report transpiler bugs** if generated code is invalid
2. **Workaround**: Modify Cadenza source to avoid problematic patterns
3. **Check Cadenza syntax** for compliance with language rules

### Runtime Errors in Generated Code

**Problem**: Generated C# throws exceptions at runtime

**Common Issues**:
1. **Null reference exceptions**: Usually indicates transpiler bugs
2. **Type cast exceptions**: Check Cadenza type usage
3. **Index out of range**: Review array/string operations

**Solutions**:
1. **Debug the Cadenza source** rather than generated C#
2. **Add validation** in Cadenza code
3. **Use Result types** for error handling

## Module System Problems

### Import Resolution Failures

**Problem**: Cannot find imported modules

**Common Issues**:
1. **Module not found**:
   ```cadenza
   // ❌ Error: Module doesn't exist
   import NonExistentModule.{function1}
   
   // ✅ Solution: Check module name and availability
   import MathUtils.{add}
   ```

2. **Function not exported**:
   ```cadenza
   // ❌ Error: Function not in export list
   import MyModule.{privateFunction}
   
   // ✅ Solution: Import only exported functions
   import MyModule.{publicFunction}
   ```

### Circular Dependencies

**Problem**: Modules importing each other

**Example**:
```cadenza
// Module A imports Module B
// Module B imports Module A
// This creates a circular dependency
```

**Solution**:
1. **Refactor dependencies**: Extract common functionality
2. **Use dependency injection**: Pass dependencies as parameters
3. **Reorganize modules**: Create a clearer hierarchy

### Name Conflicts

**Problem**: Multiple modules export functions with the same name

**Solution**:
1. **Use qualified imports**:
   ```cadenza
   import ModuleA
   import ModuleB
   
   function example() -> int {
       return ModuleA.calculate() + ModuleB.calculate()
   }
   ```

2. **Use selective imports**:
   ```cadenza
   import ModuleA.{calculate as calculateA}
   import ModuleB.{calculate as calculateB}
   ```

## Development Environment

### Editor Issues

**Problem**: No syntax highlighting or IntelliSense

**Solutions**:
1. **Use generic syntax highlighting**: Set file type to "JavaScript" or "TypeScript" for basic highlighting
2. **Create custom highlighting**: Use your editor's syntax definition features
3. **Wait for official extension**: Cadenza language server is planned

### Debugging Generated Code

**Problem**: Difficult to debug transpiled C# code

**Solutions**:
1. **Debug at Cadenza level**: Add debugging statements in Cadenza
2. **Use cadenzac run**: Examine generated code structure
3. **Add logging**: Use effect system with Logging effect
4. **Step through transpiler**: Debug the transpilation process

### Version Control

**Problem**: What to commit to Git

**Recommendations**:
```gitignore
# Include in Git
*.cdz
cadenzac.json
README.md
docs/

# Exclude from Git  
*.cs
build/
bin/
obj/
```

## FAQ

### General Questions

**Q: Is Cadenza production-ready?**
A: Cadenza is in active development. It's suitable for experimental projects and learning, but not recommended for production systems yet.

**Q: How does Cadenza compare to other functional languages?**
A: Cadenza focuses specifically on LLM-assisted development with explicit effects and safety. It transpiles to C# for ecosystem compatibility.

**Q: Can I use existing .NET libraries?**
A: Future versions will support .NET interop. Currently, Cadenza is self-contained.

### Language Features

**Q: Why use Result types instead of exceptions?**
A: Result types make error handling explicit and predictable, which is better for LLM understanding and code safety.

**Q: Can I disable the effect system?**
A: No, the effect system is core to Cadenza's design philosophy of making side effects explicit.

**Q: Will Cadenza support generics?**
A: Generics are planned for future versions, but the current focus is on core language stability.

### Performance

**Q: How fast is Cadenza compared to C#?**
A: Since Cadenza transpiles to C#, runtime performance should be similar. Compilation time is currently slower.

**Q: Can I optimize the generated C# code?**
A: The transpiler generates standard C# that can be optimized by the .NET compiler. Specific optimizations are planned.

### Development

**Q: How can I contribute to Cadenza?**
A: See the [Contributing Guide](contributing.md) for detailed information on how to help.

**Q: Where can I report bugs?**
A: Report bugs through GitHub issues with detailed reproduction steps and example code.

**Q: Is there a roadmap for Cadenza?**
A: Yes, check the `roadmap/` directory for detailed development plans.

### Troubleshooting Checklist

When encountering issues, work through this checklist:

1. **Environment Check**:
   - [ ] .NET 10.0+ SDK installed
   - [ ] Cadenza transpiler builds successfully
   - [ ] Path and permissions configured correctly

2. **Syntax Check**:
   - [ ] All function parameters have type annotations
   - [ ] All functions have return type annotations
   - [ ] Effect declarations match function usage
   - [ ] String interpolation uses $ prefix

3. **Project Structure**:
   - [ ] cadenzac.json exists (for projects)
   - [ ] Source files in correct directories
   - [ ] Module imports/exports are correct

4. **Generated Code**:
   - [ ] Generated C# compiles without errors
   - [ ] No runtime exceptions in generated code
   - [ ] Performance is acceptable

5. **Documentation**:
   - [ ] Check language reference for correct syntax
   - [ ] Review examples for similar use cases
   - [ ] Consult API reference for advanced usage

If issues persist after checking these items, consider:
- Creating a minimal reproduction case
- Checking GitHub issues for similar problems
- Contributing a bug report with detailed information

## Getting Additional Help

1. **Documentation**: Start with the [Getting Started Guide](getting-started.md)
2. **Examples**: Review working code in the `examples/` directory
3. **Community**: Engage with other developers through GitHub discussions
4. **Issues**: Report bugs and request features through GitHub issues
5. **Contributing**: Help improve Cadenza by contributing code or documentation

Remember that Cadenza is actively developed, so issues you encounter may be fixed in newer versions. Always check for the latest release before troubleshooting.