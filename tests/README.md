# Cadenza Testing Framework

This directory contains the comprehensive testing framework for the Cadenza transpiler, implementing all the requirements specified in the roadmap.

## Overview

The testing framework provides complete coverage of the Cadenza transpiler with multiple types of tests:

- **Unit Tests**: Individual component testing for lexer, parser, and code generator
- **Integration Tests**: End-to-end transpilation testing
- **Golden File Tests**: Code generation verification with input/expected output pairs
- **Performance Tests**: Transpilation speed benchmarks and performance monitoring
- **Regression Tests**: Prevention of breaking changes with historical test cases
- **Framework Tests**: Test discovery, reporting, and coverage analysis

## Directory Structure

```
tests/
├── unit/                    # Unit tests for individual components
│   ├── LexerTests.cs       # Comprehensive lexer testing
│   ├── ParserTests.cs      # Comprehensive parser testing
│   └── CodeGeneratorTests.cs # Comprehensive code generator testing
├── integration/             # Integration tests
│   └── TranspilationTests.cs # End-to-end transpilation tests
├── golden/                  # Golden file tests
│   ├── inputs/             # Cadenza input files
│   │   ├── basic_functions.cdz
│   │   ├── control_flow.cdz
│   │   ├── result_types.cdz
│   │   ├── modules.cdz
│   │   ├── string_interpolation.cdz
│   │   └── effect_system.cdz
│   ├── expected/           # Expected C# output files
│   │   ├── basic_functions.cs
│   │   ├── control_flow.cs
│   │   ├── result_types.cs
│   │   ├── modules.cs
│   │   ├── string_interpolation.cs
│   │   └── effect_system.cs
│   └── GoldenFileTests.cs  # Golden file test execution
├── performance/             # Performance benchmarks
│   └── TranspilerBenchmarks.cs # BenchmarkDotNet performance tests
├── regression/              # Regression tests
│   ├── RegressionTests.cs  # Regression test suite
│   └── data/               # Saved regression test cases
├── framework/               # Test framework utilities
│   └── TestDiscovery.cs    # Test discovery and organization
├── reporting/               # Test reporting and coverage
│   └── TestReporting.cs    # Comprehensive test reporting
└── README.md               # This documentation
```

## Running Tests

### Prerequisites

- .NET 8.0 SDK or later
- All NuGet packages restored (`dotnet restore`)

### Running All Tests

```bash
# Run all tests
dotnet test

# Run with verbose output
dotnet test --verbosity normal

# Run with coverage (requires coverage tool)
dotnet test --collect:"XPlat Code Coverage"
```

### Running Specific Test Categories

```bash
# Unit tests only
dotnet test --filter "FullyQualifiedName~Cadenza.Tests.Unit"

# Integration tests only
dotnet test --filter "FullyQualifiedName~Cadenza.Tests.Integration"

# Golden file tests only
dotnet test --filter "FullyQualifiedName~Cadenza.Tests.Golden"

# Performance tests only
dotnet test --filter "FullyQualifiedName~Cadenza.Tests.Performance"

# Regression tests only
dotnet test --filter "FullyQualifiedName~Cadenza.Tests.Regression"
```

### Running Performance Benchmarks

Performance benchmarks use BenchmarkDotNet and should be run separately in Release mode:

```bash
dotnet run --project tests/performance/TranspilerBenchmarks.cs --configuration Release
```

## Test Categories

### Unit Tests

**Location**: `tests/unit/`

Unit tests provide comprehensive coverage of individual components:

- **LexerTests**: Token generation, error handling, source location tracking
- **ParserTests**: AST generation, syntax validation, error recovery
- **CodeGeneratorTests**: C# code generation, syntax tree creation, output validation

**Coverage Goals**: >90% code coverage for each component

### Integration Tests

**Location**: `tests/integration/`

Integration tests verify end-to-end functionality:

- Complete transpilation pipeline testing
- Generated code compilation verification
- Cross-component interaction testing
- Real-world scenario validation

### Golden File Tests

**Location**: `tests/golden/`

Golden file tests ensure consistent code generation:

- **Input Files**: Cadenza source code covering all language features
- **Expected Files**: Hand-verified C# output that should be generated
- **Validation**: Exact string matching with normalization for whitespace
- **Coverage**: All major language constructs and edge cases

**Adding New Golden Tests**:
1. Create input file in `tests/golden/inputs/`
2. Generate expected output using the transpiler
3. Manually verify the generated C# code
4. Save verified output to `tests/golden/expected/`
5. Add test case to `GoldenFileTests.cs`

### Performance Tests

**Location**: `tests/performance/`

Performance tests track transpilation speed and memory usage:

- **Benchmarks**: Individual operation timing (lexing, parsing, code generation)
- **Scalability**: Performance with varying input sizes
- **Regression Detection**: Automated alerts for performance degradation
- **Memory Profiling**: Memory allocation and garbage collection analysis

**Performance Thresholds**:
- Simple function transpilation: <10ms
- Complex function transpilation: <25ms
- Large program (100 functions): <500ms

### Regression Tests

**Location**: `tests/regression/`

Regression tests prevent breaking changes:

- **Historical Cases**: Known good transpilation examples
- **Error Preservation**: Verification that known errors still fail appropriately
- **Performance Baselines**: Detection of significant performance regressions
- **Backwards Compatibility**: Ensuring existing code still works

## Test Data Management

The testing framework includes automated test data management:

- **Automatic Generation**: Sample Cadenza files for testing
- **Result Caching**: Saving and loading test results for comparison
- **Data Validation**: Ensuring test data integrity
- **Cleanup**: Automatic cleanup of temporary test files

## Test Reporting

### Automated Reports

The testing framework generates comprehensive reports:

- **JSON Reports**: Machine-readable test results and metrics
- **HTML Reports**: Human-readable test summaries with visualizations
- **Coverage Analysis**: Code coverage metrics by component
- **Performance Reports**: Benchmark results and trend analysis

### Metrics Tracked

- **Test Coverage**: Percentage of code covered by tests
- **Success Rate**: Percentage of tests passing
- **Performance Metrics**: Execution time trends and thresholds
- **Component Coverage**: Test distribution across components

### Generating Reports

```bash
# Generate comprehensive test report
dotnet test --filter "TestReporting_GenerateComprehensiveTestReport"

# Generate HTML report
dotnet test --filter "TestReporting_GenerateHtmlReport"

# Analyze code coverage
dotnet test --filter "TestReporting_GenerateCodeCoverageAnalysis"
```

## Adding New Tests

### Test Naming Conventions

Follow these naming patterns for consistency:

- **Unit Tests**: `{Component}_{ShouldBehavior}[_WhenCondition]`
  - Example: `Lexer_ShouldTokenizeNumbers`
  - Example: `Parser_ShouldThrowError_WhenMissingBrace`

- **Integration Tests**: `Transpiler_{ShouldBehavior}[_WhenCondition]`
  - Example: `Transpiler_ShouldTranspileSimpleFunction`

- **Golden File Tests**: `GoldenFile_{TestName}`
  - Example: `GoldenFile_BasicFunctions`

### Test Organization

1. **Group Related Tests**: Use nested classes or regions for related test methods
2. **Setup and Teardown**: Use `[SetUp]` and `[TearDown]` for test preparation
3. **Test Data**: Use `[TestCase]` or `[TestCaseSource]` for parameterized tests
4. **Categories**: Use `[Category]` attribute for test grouping

### Writing Effective Tests

1. **Arrange-Act-Assert**: Follow the AAA pattern consistently
2. **Single Responsibility**: Each test should verify one specific behavior
3. **Clear Assertions**: Use descriptive assertion messages
4. **Edge Cases**: Include boundary conditions and error scenarios
5. **Documentation**: Add comments for complex test scenarios

## Continuous Integration

### Build Pipeline Integration

The testing framework integrates with CI/CD pipelines:

```yaml
# Example CI configuration
- name: Run Tests
  run: |
    dotnet test --logger trx --results-directory TestResults
    dotnet test --collect:"XPlat Code Coverage" --results-directory TestResults

- name: Generate Reports
  run: |
    dotnet test --filter "TestReporting" --verbosity normal

- name: Upload Results
  uses: actions/upload-artifact@v2
  with:
    name: test-results
    path: TestResults
```

### Quality Gates

Automated quality checks ensure code quality:

- **Minimum Test Coverage**: 90% for unit tests, 80% overall
- **Performance Thresholds**: No regression >20% from baseline
- **Success Rate**: >95% test pass rate required
- **Breaking Changes**: Regression tests must pass

## Troubleshooting

### Common Issues

1. **Test Discovery Problems**:
   - Ensure proper namespace organization
   - Verify `[TestFixture]` and `[Test]` attributes
   - Check for compilation errors

2. **Golden File Test Failures**:
   - Verify input files exist and are valid Cadenza
   - Check that expected output files match current transpiler output
   - Use test output to identify differences

3. **Performance Test Instability**:
   - Run performance tests in Release mode
   - Ensure system is not under load during testing
   - Use multiple iterations for stable measurements

4. **Regression Test Failures**:
   - Check if changes are intentional improvements
   - Update regression data if behavior change is intended
   - Investigate if failure indicates a real regression

### Debugging Tests

1. **Use Test Explorer**: Visual Studio and VS Code provide test discovery and debugging
2. **Console Output**: Use `TestContext.WriteLine()` for test debugging output
3. **Conditional Compilation**: Use `#if DEBUG` for debug-only test code
4. **Test Isolation**: Ensure tests don't depend on external state or other tests

## Contributing

When contributing new tests:

1. **Follow Conventions**: Use established naming and organization patterns
2. **Add Documentation**: Update this README for new test categories or significant changes
3. **Verify Coverage**: Ensure new features have appropriate test coverage
4. **Update Golden Files**: Add golden file tests for new language features
5. **Performance Impact**: Consider performance implications of new tests

## Best Practices

1. **Test Early and Often**: Write tests alongside feature development
2. **Comprehensive Coverage**: Cover happy paths, edge cases, and error conditions
3. **Maintainable Tests**: Write tests that are easy to understand and modify
4. **Fast Execution**: Keep test execution time reasonable for developer productivity
5. **Reliable Results**: Ensure tests are deterministic and don't flake
6. **Clear Failures**: Provide clear error messages when tests fail

## Success Criteria

The testing framework meets the following success criteria:

✅ **Comprehensive Coverage**: >90% unit test coverage, >80% overall coverage
✅ **All Test Types**: Unit, integration, golden file, performance, and regression tests
✅ **Golden File System**: Input/expected output pairs with validation
✅ **Performance Benchmarks**: Automated speed and memory monitoring
✅ **Automated Reporting**: JSON and HTML reports with metrics
✅ **Test Discovery**: Automated test organization and validation
✅ **Regression Prevention**: Historical test cases prevent breaking changes
✅ **CI Integration**: Works with automated build pipelines
✅ **Clear Documentation**: Comprehensive testing guidelines and examples

This testing framework ensures the Cadenza transpiler maintains high quality, performance, and reliability throughout its development lifecycle.