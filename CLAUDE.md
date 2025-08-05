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


### How to Perform Search and Replace in Multiple Files on Linux/WSL

An issue we have encountered is the use of outdated NUnit assertion methods. For example, `Assert.Single()` and `Assert.Contains()` are no longer the preferred way to write these assertions. You can fix these across the entire project efficiently using command-line tools.

Here is a guide on how to use `grep` to find the files containing the patterns and `sed` to replace them.

**1. Finding the Files**

First, it's a good idea to see which files will be affected. You can use `grep` to recursively search for a pattern in the `tests` directory.

*   To find all occurrences of `Assert.Single`:
    ```bash
    grep -r "Assert.Single" tests/
    ```
*   To find all occurrences of `Assert.Contains`:
    ```bash
    grep -r "Assert.Contains" tests/
    ```

**2. Performing the Replacement with `sed`**

Once you have an idea of the files that will be changed, you can use a combination of `find`, `xargs`, and `sed` to perform the replacement. The `sed -i` command modifies the files in place.

**Example 1: Replacing `Assert.Single(collection)`**

This is a bit complex because you need to capture the collection name. A simple search and replace won't work. You often have to do this manually, but for simple cases, you can use regular expressions.

Let's say you have code like `Assert.Single(myCollection);`. You want to change it to `Assert.That(myCollection.Count, Is.EqualTo(1));`.

A `sed` command to do this would look like:

```bash
find tests -type f -name "*.cs" -print0 | xargs -0 sed -i 's/Assert\.Single(\([^)]*\));/Assert.That(\1.Count, Is.EqualTo(1));/g'
```

**Breakdown of the `sed` command:**
*   `s/.../.../g`: This is the substitute command.
*   `Assert\.Single\(`: Matches the literal text `Assert.Single(`. We escape the `.` and `(`.
*   `\([^)]*\)`: This is a capture group. It matches any characters that are not a closing parenthesis `)` and captures them. This is how we get the `myCollection` part.
*   `\);`: Matches the closing `);`.
*   `Assert.That(\1.Count, Is.EqualTo(1));`: This is the replacement string. `\1` refers to the first captured group (the collection name).

**Example 2: Replacing `Assert.Contains("substring", text)`**

The old `Assert.Contains` is often used in a way that is not a direct replacement. The modern equivalent is `Assert.That(text, Does.Contain("substring"))`.

If you have `Assert.Contains("some string", myVariable);`, you want to change it to `Assert.That(myVariable, Does.Contain("some string"));`.

```bash
find tests -type f -name "*.cs" -print0 | xargs -0 sed -i 's/Assert\.Contains(\([^,]*\), \([^)]*\));/Assert.That(\2, Does.Contain(\1));/g'
```

**Breakdown of the `sed` command:**
*   `Assert\.Contains\(`: Matches the literal `Assert.Contains(`.
*   `\([^,]*\)`: Captures the first argument (the substring).
*   `, `: Matches the comma and space.
*   `\([^)]*\)`: Captures the second argument (the text to search in).
*   `\);`: Matches the closing `);`.
*   `Assert.That(\2, Does.Contain(\1));`: The replacement string. `\2` is the second capture group (the text) and `\1` is the first (the substring).

**Important:** Always make sure you have a clean git status before running these commands, so you can easily review the changes and revert them if something goes wrong.

### How to Run Specific Tests

A helper script `test_specific.sh` has been created to simplify running and debugging specific tests. This script formats the output to show key information like test results, assertions, and error messages.

**Usage:**
```bash
# Run a specific test by name
bash test_specific.sh "TestName"

# Run the default test (Transpiler_ShouldTranspileResultTypes)
bash test_specific.sh

# Examples:
bash test_specific.sh "MatchExpression"
bash test_specific.sh "Transpiler_ShouldTranspileResultTypes"
bash test_specific.sh "Parser_MatchExpression_ShouldParseCorrectly"
```

**What the script does:**
- Runs `dotnet test` with the specified filter
- Extracts and displays key information: test results, assertions, expected vs actual values
- Shows up to 25 lines of context after key matches
- Helps avoid repeating long command lines when debugging specific test failures

**Note:** The script may show line ending warnings (`$'\r': command not found`) but these don't affect the test results.

**Alternative - Run all tests:**
```bash
# Run all tests and see summary
dotnet test --logger "console;verbosity=minimal" 2>&1 | grep -E "Failed:.*Passed:" | tail -1
```
