# Multi-File Compilation Design

## Overview

Cadenza supports multi-file compilation similar to C#'s .csproj system. The compiler can automatically discover and compile all `.cdz` files in a directory structure, with optional project configuration through `cadenzac.json`.

## Default Behavior (No Configuration File)

When using `cadenzac-core --project` without a `cadenzac.json` file:

1. **Auto-discovery**: Recursively finds all `.cdz` files in current directory and subdirectories
2. **Default output**: Generates an executable (`.exe` on Windows, binary on Linux/macOS)
3. **Entry point detection**: Looks for a `main()` function across all files
4. **Module resolution**: Automatically resolves imports between files based on file paths
5. **Dependency ordering**: Compiles files in dependency order based on import statements

### Directory Structure Example

```
my-project/
├── main.cdz                    # Entry point with main() function
├── utils/
│   ├── math.cdz               # Math utilities module
│   └── string.cdz             # String utilities module
├── services/
│   ├── user_service.cdz       # User service module
│   └── data_service.cdz       # Data service module
└── models/
    ├── user.cdz               # User model
    └── product.cdz            # Product model
```

### Usage

```bash
# Compile all .cdz files in current directory to executable
cadenzac-core --project

# Specify output name
cadenzac-core --project --output MyApp.exe

# Generate library instead of executable
cadenzac-core --project --library --output MyLibrary.dll
```

## Configuration File (cadenzac.json)

When `cadenzac.json` is present, it provides detailed control over the compilation process:

### Example Project Configuration

```json
{
  "name": "MyWebApp",
  "version": "1.2.0",
  "description": "A web application built with Cadenza",
  "build": {
    "source": "src/",
    "output": "bin/",
    "outputType": "exe",
    "entryPoint": "src/main.cdz",
    "target": "csharp",
    "framework": "net8.0"
  },
  "include": [
    "src/**/*.cdz",
    "shared/**/*.cdz"
  ],
  "exclude": [
    "tests/**/*.cdz",
    "examples/**/*.cdz",
    "**/*_temp.cdz"
  ],
  "dependencies": {
    "Newtonsoft.Json": "13.0.3",
    "Microsoft.Extensions.Logging": "^8.0.0"
  },
  "devDependencies": {
    "Cadenza.Testing": "^1.0.0"
  },
  "compiler": {
    "strictMode": true,
    "enableNullabilityChecks": true,
    "warningsAsErrors": false,
    "debugSymbols": true
  },
  "linting": {
    "enabled": true,
    "configFile": "cadenzalint.json"
  }
}
```

### Configuration Fields

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `name` | string | directory name | Project name |
| `version` | string | "1.0.0" | Project version |
| `description` | string | "" | Project description |
| `build.source` | string | "./" | Source directory root |
| `build.output` | string | "bin/" | Output directory |
| `build.outputType` | string | "exe" | Output type: "exe", "library", "winexe" |
| `build.entryPoint` | string | auto-detect | Main entry point file |
| `build.target` | string | "csharp" | Target platform |
| `build.framework` | string | "net8.0" | Target framework |
| `include` | string[] | ["**/*.cdz"] | File inclusion patterns |
| `exclude` | string[] | [] | File exclusion patterns |
| `dependencies` | object | {} | Runtime dependencies |
| `devDependencies` | object | {} | Development dependencies |
| `compiler.strictMode` | bool | true | Enable strict compilation |
| `compiler.enableNullabilityChecks` | bool | true | Enable nullability analysis |
| `compiler.warningsAsErrors` | bool | false | Treat warnings as errors |
| `compiler.debugSymbols` | bool | true | Include debug information |

## Module Resolution

### Import Syntax

```cadenza
// Relative imports
import "./utils/math" as Math
import "../shared/types" as Types

// Absolute imports (from project root)
import "services/user_service" as UserService
import "models/user" as User

// Selective imports
import Math.{add, multiply}
import UserService.{createUser, deleteUser}

// Wildcard imports
import Math.*
```

### File-to-Module Mapping

```
src/
├── main.cdz                   → main
├── utils/
│   ├── math.cdz              → utils.math
│   └── validation.cdz        → utils.validation
├── services/
│   └── user_service.cdz      → services.user_service
└── models/
    └── user.cdz              → models.user
```

### Module Declaration

Each `.cdz` file can optionally declare its module name:

```cadenza
module utils.math

export pure function add(a: int, b: int) -> int {
    return a + b
}

export pure function multiply(a: int, b: int) -> int {
    return a * b
}
```

## Compilation Process

### 1. Discovery Phase
- Scan directory structure for `.cdz` files
- Apply include/exclude patterns from configuration
- Build file dependency graph from import statements

### 2. Validation Phase
- Verify all imports can be resolved
- Check for circular dependencies
- Validate module declarations match file structure

### 3. Compilation Phase
- Sort files in topological dependency order
- Compile each file to C# AST
- Combine all ASTs into single assembly
- Generate executable or library output

### 4. Output Generation
- Create output directory if needed
- Generate assembly with all compiled modules
- Copy any referenced dependencies
- Generate debug symbols if enabled

## Advanced Features

### Conditional Compilation

```cadenza
// File: config.cdz
#if DEBUG
export const LOG_LEVEL = "debug"
#else
export const LOG_LEVEL = "info"
#endif
```

### Multi-Target Support

```json
{
  "build": {
    "targets": [
      {
        "name": "production",
        "outputType": "exe",
        "framework": "net8.0",
        "optimize": true
      },
      {
        "name": "development", 
        "outputType": "exe",
        "framework": "net8.0",
        "debugSymbols": true
      }
    ]
  }
}
```

### Incremental Compilation

The compiler tracks file modification times and only recompiles changed files and their dependents:

```bash
# Only recompiles changed files
cadenzac-core --project --incremental

# Force full rebuild
cadenzac-core --project --clean
```

## Examples

### Simple Console Application

**Directory Structure:**
```
my-console-app/
├── main.cdz
└── utils.cdz
```

**main.cdz:**
```cadenza
import "./utils" as Utils

function main() -> int {
    let result = Utils.add(5, 3)
    Console.WriteLine($"Result: {result}")
    return 0
}
```

**utils.cdz:**
```cadenza
module utils

export pure function add(a: int, b: int) -> int {
    return a + b
}
```

**Compilation:**
```bash
cd my-console-app
cadenzac-core --project --output MyApp.exe
./MyApp.exe  # Output: Result: 8
```

### Web API with Configuration

**Directory Structure:**
```
my-web-api/
├── cadenzac.json
├── src/
│   ├── main.cdz
│   ├── controllers/
│   │   └── user_controller.cdz
│   ├── services/
│   │   └── user_service.cdz
│   └── models/
│       └── user.cdz
└── tests/
    └── user_tests.cdz
```

**cadenzac.json:**
```json
{
  "name": "MyWebAPI",
  "version": "1.0.0",
  "build": {
    "source": "src/",
    "output": "bin/",
    "outputType": "exe",
    "entryPoint": "src/main.cdz"
  },
  "dependencies": {
    "Microsoft.AspNetCore": "8.0.0"
  }
}
```

**Compilation:**
```bash
cadenzac-core --project
# Outputs: bin/MyWebAPI.exe
```

### Library Project

**cadenzac.json:**
```json
{
  "name": "MathLibrary",
  "version": "2.1.0",
  "build": {
    "outputType": "library",
    "output": "dist/",
    "target": "csharp",
    "framework": "netstandard2.1"
  }
}
```

**Compilation:**
```bash
cadenzac-core --project
# Outputs: dist/MathLibrary.dll
```

## Error Handling

### Common Compilation Errors

1. **Unresolved Import:**
   ```
   Error: Cannot resolve import 'missing_module' in file 'main.cdz:3'
   ```

2. **Circular Dependency:**
   ```
   Error: Circular dependency detected: a.cdz -> b.cdz -> a.cdz
   ```

3. **Missing Entry Point:**
   ```
   Error: No main() function found in project for executable output
   ```

4. **Duplicate Module:**
   ```
   Error: Module 'utils.math' declared in multiple files:
     - src/utils/math.cdz
     - lib/math.cdz
   ```

### Build Warnings

```
Warning: Unused import 'unused_module' in file 'main.cdz:2'
Warning: File 'legacy.cdz' has no module declaration
```

## Integration with IDE

The multi-file system integrates with Cadenza's Language Server Protocol for:

- **Cross-file navigation**: Go to definition across modules
- **Auto-completion**: Import suggestions and symbol completion
- **Error highlighting**: Real-time compilation errors
- **Refactoring**: Rename symbols across files