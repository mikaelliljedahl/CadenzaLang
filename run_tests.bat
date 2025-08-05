@echo off
echo === Cadenza Refactoring Test Runner ===
echo Running tests to verify 100%% pass rate...
echo.

echo 1. Building Cadenza.Core project...
dotnet build src/Cadenza.Core/cadenzac-core.csproj
if %errorlevel% neq 0 (
    echo ❌ Build failed!
    exit /b 1
)
echo ✅ Build successful
echo.

echo 2. Building test project...
dotnet build tests/Cadenza.Tests.csproj
if %errorlevel% neq 0 (
    echo ❌ Test build failed!
    exit /b 1
)
echo ✅ Test build successful
echo.

echo 3. Running all tests...
dotnet test tests/Cadenza.Tests.csproj --verbosity normal --logger "console;verbosity=detailed"
if %errorlevel% neq 0 (
    echo ❌ Tests failed!
    exit /b 1
)
echo ✅ All tests passed
echo.

echo 4. Building comprehensive test...
dotnet build comprehensive_test.csproj
if %errorlevel% neq 0 (
    echo ❌ Comprehensive test build failed!
    exit /b 1
)
echo ✅ Comprehensive test build successful
echo.

echo 5. Running comprehensive test...
dotnet run --project comprehensive_test.csproj
if %errorlevel% neq 0 (
    echo ❌ Comprehensive test failed!
    exit /b 1
)
echo ✅ Comprehensive test passed
echo.

echo 6. Building Cadenza.Core.Tests project...
dotnet build tests/Cadenza.Core.Tests/Cadenza.Core.Tests.csproj
if %errorlevel% neq 0 (
    echo ❌ Cadenza.Core.Tests build failed!
    exit /b 1
)
echo ✅ Cadenza.Core.Tests build successful
echo.

echo 7. Running Cadenza.Core.Tests...
dotnet test tests/Cadenza.Core.Tests/Cadenza.Core.Tests.csproj --verbosity normal --logger "console;verbosity=detailed"
if %errorlevel% neq 0 (
    echo ❌ Cadenza.Core.Tests failed!
    exit /b 1
)
echo ✅ Cadenza.Core.Tests passed
echo.

echo === ALL TESTS PASSED! ===
echo ✅ Refactoring verification complete
echo ✅ 100%% test pass rate achieved