# Phase 5 - Analysis Tools Migration Plan

## Overview
Migrate Cadenza analysis tools from incomplete C# implementations to working .cdz implementations. This will provide comprehensive static analysis capabilities.

## Current State
**Existing C# files** (incomplete, no .csproj, untested):
- `src/Cadenza.Analysis/StaticAnalyzer.cs`
- `src/Cadenza.Analysis/LintRuleEngine.cs`
- `src/Cadenza.Analysis/EffectAnalyzer.cs`
- `src/Cadenza.Analysis/ResultTypeAnalyzer.cs`
- `src/Cadenza.Analysis/CodeQualityAnalyzer.cs`
- `src/Cadenza.Analysis/PerformanceAnalyzer.cs`
- `src/Cadenza.Analysis/SecurityAnalyzer.cs`
- `src/Cadenza.Analysis/AnalysisReport.cs`

**Existing .cdz tool** (partial implementation):
- `src/Cadenza.Tools/linter.cdz` (298 lines) - Basic analysis patterns

## Tasks

### Task 1: Extend Existing Linter
**Enhance `src/Cadenza.Tools/linter.cdz`**:
- Add comprehensive rule engine
- Support multiple analysis types
- Improve error reporting
- Add configuration support

### Task 2: Create Specialized Analyzers
**Effect System Analyzer**:
- File: `src/Cadenza.Tools/analysis/effect-analyzer.cdz`
- Replace: `EffectAnalyzer.cs`
- Rules:
  - Effect completeness (all side effects declared)
  - Effect minimality (no unused effects)
  - Effect propagation (effects properly bubbled up)
  - Effect consistency (consistent effect usage)

**Result Type Analyzer**:
- File: `src/Cadenza.Tools/analysis/result-analyzer.cdz`
- Replace: `ResultTypeAnalyzer.cs`
- Rules:
  - Result type usage (functions with effects should return Result)
  - Error propagation (proper ? operator usage)
  - Match completeness (all Result branches handled)
  - Error message quality (descriptive error messages)

**Code Quality Analyzer**:
- File: `src/Cadenza.Tools/analysis/quality-analyzer.cdz`
- Replace: `CodeQualityAnalyzer.cs`
- Rules:
  - Function complexity (cyclomatic complexity)
  - Naming conventions (consistent naming)
  - Documentation coverage (spec blocks present)
  - Code duplication detection

**Security Analyzer**:
- File: `src/Cadenza.Tools/analysis/security-analyzer.cdz`
- Replace: `SecurityAnalyzer.cs`
- Rules:
  - SQL injection detection
  - XSS vulnerability detection
  - Insecure random number generation
  - Hardcoded secrets detection

**Performance Analyzer**:
- File: `src/Cadenza.Tools/analysis/performance-analyzer.cdz`
- Replace: `PerformanceAnalyzer.cs`
- Rules:
  - Inefficient string concatenation
  - Unnecessary allocations
  - Loop optimization opportunities
  - Database query optimization

### Task 3: Create Analysis Engine
**Main Analysis Coordinator**:
- File: `src/Cadenza.Tools/analysis/analysis-engine.cdz`
- Replace: `StaticAnalyzer.cs`
- Functionality:
  - Coordinate all analyzers
  - Collect and merge results
  - Generate comprehensive reports
  - Support configuration files

### Task 4: Analysis Reporting
**Report Generator**:
- File: `src/Cadenza.Tools/analysis/report-generator.cdz`
- Replace: `AnalysisReport.cs`
- Formats:
  - Human-readable text output
  - JSON for CI/CD integration
  - SARIF for security tools
  - HTML for detailed browsing

### Task 5: Configuration System
**Analysis Configuration**:
- File: `src/Cadenza.Tools/analysis/config-manager.cdz`
- Functionality:
  - Load `cadenzalint.json` configuration
  - Rule enable/disable
  - Severity level configuration
  - Custom rule parameters

### Task 6: Integration with CLI
**CLI Integration**:
- Extend main CLI with analysis commands
- Support `cadenzac-core --lint` command
- Integration with existing linter tool
- Batch analysis of multiple files

## Expected Outcomes
- Comprehensive static analysis for Cadenza code
- Multiple analysis types (effects, results, quality, security, performance)
- Configurable rule engine
- Multiple output formats
- Integration with development workflow

## Success Criteria
- [ ] Effect analyzer detects all major effect system issues
- [ ] Result analyzer validates proper Result type usage
- [ ] Quality analyzer provides actionable code improvements
- [ ] Security analyzer detects common vulnerabilities
- [ ] Performance analyzer suggests optimizations
- [ ] All analyzers work together in analysis engine
- [ ] Configuration system allows rule customization
- [ ] CLI integration provides seamless analysis workflow

## Dependencies
- **Runtime Bridge Fixes**: Need working .cdz tools for file operations
- **Existing Linter**: Build on existing `linter.cdz` implementation

## Priority
**MEDIUM** - Important for code quality but not critical path for self-hosting

## File Structure
```
src/Cadenza.Tools/analysis/
├── analysis-engine.cdz         # Main coordinator
├── effect-analyzer.cdz         # Effect system analysis
├── result-analyzer.cdz         # Result type analysis
├── quality-analyzer.cdz        # Code quality analysis
├── security-analyzer.cdz       # Security analysis
├── performance-analyzer.cdz    # Performance analysis
├── report-generator.cdz        # Report generation
└── config-manager.cdz          # Configuration management
```