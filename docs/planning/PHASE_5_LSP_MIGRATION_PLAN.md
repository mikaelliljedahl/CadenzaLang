# Phase 5 - LSP Server Migration Plan

## Overview
Migrate Cadenza LSP (Language Server Protocol) implementation from incomplete C# to working .cdz implementation for IDE integration.

## Current State
**Existing C# files** (incomplete, no .csproj, untested):
- `src/Cadenza.LSP/CadenzaLanguageServer.cs`
- `src/Cadenza.LSP/CompletionProvider.cs`
- `src/Cadenza.LSP/DiagnosticsProvider.cs`
- `src/Cadenza.LSP/HoverProvider.cs`
- `src/Cadenza.LSP/DefinitionProvider.cs`
- `src/Cadenza.LSP/DocumentManager.cs`

## LSP Features to Implement
- **Diagnostics**: Real-time error detection and reporting
- **Completion**: Auto-completion for keywords, types, functions
- **Hover**: Type information and documentation on hover
- **Go-to-Definition**: Navigation to function/type definitions
- **Document Symbols**: Outline view of functions and types
- **Workspace Symbols**: Project-wide symbol search

## Tasks

### Task 1: LSP Foundation
**Main Language Server**:
- File: `src/Cadenza.Tools/lsp/language-server.cdz`
- Replace: `CadenzaLanguageServer.cs`
- Functionality:
  - LSP protocol message handling
  - Client capability negotiation
  - Request/response routing
  - Error handling and logging

### Task 2: Document Management
**Document Lifecycle**:
- File: `src/Cadenza.Tools/lsp/document-manager.cdz`
- Replace: `DocumentManager.cs`
- Functionality:
  - Track open documents
  - Handle document changes
  - Maintain document state
  - Parse and cache ASTs

### Task 3: Diagnostics Provider
**Real-time Error Detection**:
- File: `src/Cadenza.Tools/lsp/diagnostics-provider.cdz`
- Replace: `DiagnosticsProvider.cs`
- Functionality:
  - Syntax error detection
  - Semantic error detection
  - Integration with analysis tools
  - Real-time error reporting

### Task 4: Completion Provider
**Auto-completion**:
- File: `src/Cadenza.Tools/lsp/completion-provider.cdz`
- Replace: `CompletionProvider.cs`
- Functionality:
  - Keyword completion (`function`, `pure`, `uses`, etc.)
  - Type completion (`int`, `string`, `Result`, etc.)
  - Function completion (available functions in scope)
  - Module completion (imported modules)
  - Context-aware suggestions

### Task 5: Hover Provider
**Type Information**:
- File: `src/Cadenza.Tools/lsp/hover-provider.cdz`
- Replace: `HoverProvider.cs`
- Functionality:
  - Type information display
  - Function signature display
  - Effect information display
  - Documentation from spec blocks
  - Parameter information

### Task 6: Definition Provider
**Go-to-Definition**:
- File: `src/Cadenza.Tools/lsp/definition-provider.cdz`
- Replace: `DefinitionProvider.cs`
- Functionality:
  - Function definition lookup
  - Type definition lookup
  - Module definition lookup
  - Cross-file navigation
  - Symbol resolution

### Task 7: Symbol Providers
**Symbol Information**:
- File: `src/Cadenza.Tools/lsp/symbol-provider.cdz`
- Functionality:
  - Document symbols (functions, types in current file)
  - Workspace symbols (project-wide symbol search)
  - Symbol hierarchy display
  - Symbol filtering and search

### Task 8: Protocol Integration
**LSP Protocol Support**:
- JSON-RPC message handling
- Standard LSP methods implementation
- Error response handling
- Capability negotiation
- Initialize/shutdown lifecycle

## Runtime Bridge Requirements
**Network Operations**:
- JSON-RPC over stdin/stdout
- TCP socket support (optional)
- Message serialization/deserialization

**File Operations**:
- Multi-file parsing
- File watching for changes
- Workspace directory scanning

## IDE Integration Testing
**VS Code Extension**:
- Create VS Code extension configuration
- Test real-time diagnostics
- Test auto-completion
- Test hover information
- Test go-to-definition

**Other IDEs**:
- JetBrains IDEs support
- Neovim LSP integration
- Emacs LSP integration

## Expected Outcomes
- Full LSP server implementation in Cadenza
- Real-time IDE integration
- Comprehensive developer experience
- Multi-IDE support

## Success Criteria
- [ ] LSP server starts and accepts connections
- [ ] Real-time diagnostics work in VS Code
- [ ] Auto-completion provides relevant suggestions
- [ ] Hover shows type and effect information
- [ ] Go-to-definition navigation works
- [ ] Document symbols show file outline
- [ ] Workspace symbols enable project-wide search
- [ ] Multiple IDEs supported

## Dependencies
- **Runtime Bridge Fixes**: Need working .cdz tools for JSON-RPC
- **Analysis Tools**: Need working analysis for diagnostics

## Priority
**MEDIUM** - Important for developer experience but not critical for self-hosting

## File Structure
```
src/Cadenza.Tools/lsp/
├── language-server.cdz      # Main LSP server
├── document-manager.cdz     # Document lifecycle
├── diagnostics-provider.cdz # Error detection
├── completion-provider.cdz  # Auto-completion
├── hover-provider.cdz       # Type information
├── definition-provider.cdz  # Go-to-definition
├── symbol-provider.cdz      # Symbol information
└── protocol-handler.cdz     # LSP protocol support
```

## VS Code Extension Structure
```
extensions/vscode/
├── package.json             # Extension manifest
├── src/
│   └── extension.ts        # Extension entry point
├── syntaxes/
│   └── cadenza.tmGrammar.json # Syntax highlighting
└── README.md               # Extension documentation
```