The test suite for the Cadenza project is currently failing with a large number of compilation errors. Your task is to fix all the compilation errors in the `tests/Cadenza.Tests.csproj` project and ensure that all tests pass.

**Instructions:**

1.  Run the tests using the following command to see the full list of errors:
    ```bash
    dotnet test tests/Cadenza.Tests.csproj
    ```
2.  Systematically go through the errors and fix them. The errors are primarily due to the tests being out of sync with the production code. You will need to:
    *   Update method calls with the correct arguments.
    *   Remove or update references to obsolete methods and properties.
    *   Resolve ambiguous references by fully qualifying types or using aliases.
    *   Add any missing `using` directives.
3.  After fixing a set of errors, run the tests again to ensure that you are making progress and not introducing new issues.
4.  Continue this process until all compilation errors are resolved and all tests pass.

**Acceptance Criteria:**

*   The `dotnet test tests/Cadenza.Tests.csproj` command completes successfully.
*   All tests in the test suite pass.
*   No new errors are introduced into the build.
