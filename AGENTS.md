# LLM-Friendly Backend Language (Cadenza) - Development Plan

## Project Overview
Cadenza is a backend programming language designed specifically for LLM-assisted development. It prioritizes explicitness, predictability, and safety while maintaining compatibility with existing ecosystems.

## Core Philosophy
- **Explicit over implicit**: Every operation, side effect, and dependency must be clearly declared
- **One way to do things**: Minimize choices to reduce LLM confusion and increase code consistency
- **Safety by default**: Null safety, effect tracking, and comprehensive error handling built-in
- **Self-documenting**: Code structure serves as documentation
- **Specification preservation**: Intent and reasoning are atomically linked with implementation to prevent context loss

## Development Memories

### Project Development Guidelines
- If you create a folder for testing transpiler or compiler output, remove it afterward. Don't create transpiled files in the the root directory. Always follow existing directory structure, e.g. code in the src directory, documentation in the docs folder etc
- Keep the root folder clean from test files or temporary documents. Finished docuyments should be in the docs folder.
- You will not include estimated time in the sprint plan docs because it is irrelevant.
- A feature is not completed or tested until you successfully run a test end to end (compiled code works). Transpilation might still generate invalid code.
- If you encounter a bug or a feature that is not complete in the compiler, make sure it is documented in the src\Cadenza.Core\TODO.md file