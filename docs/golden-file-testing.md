# Golden File Testing System

The Cadenza project uses a golden file testing system to verify that the transpiler generates correct C# code from Cadenza source files. This document explains how to use the improved golden file system.

## Overview

Golden file tests work by:
1. Taking a `.cdz` input file containing Cadenza source code
2. Transpiling it to C# using the current transpiler
3. Comparing the generated output with an expected `.cs` golden file
4. Reporting any differences with detailed diff information

## Directory Structure

```
tests/golden/
├── inputs/           # Cadenza source files (.cdz)
│   ├── basic_functions.cdz
│   ├── control_flow.cdz
│   └── ...
├── expected/         # Expected C# output files (.cs)
│   ├── basic_functions.cs
│   ├── control_flow.cs
│   └── ...
└── GoldenFileTests.cs
```

## Running Golden File Tests

### Normal Mode (Comparison)

Run golden file tests in normal mode to verify that the transpiler output matches the expected files:

```bash
# Run all golden file tests
dotnet test tests/Cadenza.Tests.csproj --filter "GoldenFile"

# Run a specific golden file test
dotnet test tests/Cadenza.Tests.csproj --filter "GoldenFile_BasicFunctions_ShouldMatch"
```

If a test fails, you'll see detailed diff information showing exactly what changed:

```
Golden file mismatch for basic_functions.

To update the golden file, run: REGENERATE_GOLDEN_FILES=true dotnet test

Diff for basic_functions:
==========================================
  0058: /// <param name="a">Parameter of type int</param>
  0059: /// <param name="b">Parameter of type int</param>
  0060: /// <returns>Returns int</returns>
- 0061: public static int add(int a, int b)
+ 0061: public static int Add(int a, int b)
  0062: {
  0063:     return a + b;
  0064: }
...
==========================================
Legend: - = expected, + = actual, (line numbers) = context
```

### Regeneration Mode

When you've made changes to the transpiler and need to update the golden files, use the environment variable:

```bash
# Regenerate all golden files
REGENERATE_GOLDEN_FILES=true dotnet test tests/Cadenza.Tests.csproj --filter "GoldenFile"

# Regenerate a specific golden file
REGENERATE_GOLDEN_FILES=true dotnet test tests/Cadenza.Tests.csproj --filter "GoldenFile_BasicFunctions_ShouldMatch"
```

In regeneration mode:
- Tests will automatically update the expected output files with the current transpiler output
- All tests will pass (assuming the generated code compiles)
- You'll see messages like "✅ Updated golden file: basic_functions.cs"

## Adding New Golden File Tests

1. **Create the input file**: Add a new `.cdz` file to `tests/golden/inputs/` with your Cadenza source code
2. **Generate the expected output**: Run the test in regeneration mode to create the initial golden file
3. **Add the test method**: Add a new test method in `GoldenFileTests.cs`:

```csharp
[Test]
public void GoldenFile_YourNewFeature_ShouldMatch()
{
    ExecuteGoldenFileTest("your_new_feature");
}
```

## Features

### Environment Variable Control
- **Normal mode**: Standard test execution with comparison
- **Regeneration mode**: `REGENERATE_GOLDEN_FILES=true` automatically updates golden files

### Detailed Diff Reporting
- Shows line-by-line differences with context
- Highlights exactly what changed between expected and actual output
- Includes helpful legends and instructions

### Automatic Detection
- Detects missing golden files and provides clear instructions
- Validates that generated code compiles successfully
- Provides suggestions for resolving mismatches

### Integration with Normal Test Flow
- No separate regeneration utility needed
- Works seamlessly with existing test infrastructure
- Environment variable approach is CI/CD friendly

## Best Practices

1. **Review changes carefully**: When regenerating golden files, always review the diff to ensure the changes are intentional
2. **Commit golden files**: Golden files should be committed to version control
3. **Use meaningful test names**: Name your input files and test methods descriptively
4. **Test compilation**: The system automatically verifies that generated code compiles
5. **Regular maintenance**: Periodically regenerate golden files to keep them current with transpiler improvements

## Deprecated Methods

The old manual regeneration test `GoldenFile_ExpectedOutputs_ShouldRegenerate` is deprecated in favor of the environment variable approach. The new system provides better integration and more precise control over which golden files to regenerate.

## Troubleshooting

### Test fails with compilation errors
The generated code has syntax errors. Check the transpiler logic and fix the code generation.

### Test fails with diff output
The transpiler output has changed. Review the diff to determine if:
- The change is intentional (regenerate the golden file)
- The change indicates a bug (fix the transpiler)

### Missing golden file
Run the test with `REGENERATE_GOLDEN_FILES=true` to create the initial golden file.

### Environment variable not working
Ensure you're setting the environment variable correctly:
- Linux/macOS: `REGENERATE_GOLDEN_FILES=true dotnet test`
- Windows: `set REGENERATE_GOLDEN_FILES=true && dotnet test`