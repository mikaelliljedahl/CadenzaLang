#!/bin/bash

echo "=== Cadenza Refactoring Test Runner ==="
echo "Running tests to verify 100% pass rate..."
echo ""

# First build the core project
echo "1. Building Cadenza.Core project..."
dotnet build src/Cadenza.Core/cadenzac-core.csproj
if [ $? -ne 0 ]; then
    echo "❌ Build failed!"
    exit 1
fi
echo "✅ Build successful"
echo ""

# Build the test project
echo "2. Building test project..."
dotnet build tests/Cadenza.Tests.csproj
if [ $? -ne 0 ]; then
    echo "❌ Test build failed!"
    exit 1
fi
echo "✅ Test build successful"
echo ""

# Run all tests
echo "3. Running all tests..."
dotnet test tests/Cadenza.Tests.csproj --verbosity normal --logger "console;verbosity=detailed"
if [ $? -ne 0 ]; then
    echo "❌ Tests failed!"
    exit 1
fi
echo "✅ All tests passed"
echo ""

# Build and run our comprehensive test
echo "4. Building comprehensive test..."
dotnet build comprehensive_test.csproj
if [ $? -ne 0 ]; then
    echo "❌ Comprehensive test build failed!"
    exit 1
fi
echo "✅ Comprehensive test build successful"
echo ""

echo "5. Running comprehensive test..."
dotnet run --project comprehensive_test.csproj
if [ $? -ne 0 ]; then
    echo "❌ Comprehensive test failed!"
    exit 1
fi
echo "✅ Comprehensive test passed"
echo ""

# Test the new Cadenza.Core.Tests project
echo "6. Building Cadenza.Core.Tests project..."
dotnet build tests/Cadenza.Core.Tests/Cadenza.Core.Tests.csproj
if [ $? -ne 0 ]; then
    echo "❌ Cadenza.Core.Tests build failed!"
    exit 1
fi
echo "✅ Cadenza.Core.Tests build successful"
echo ""

echo "7. Running Cadenza.Core.Tests..."
dotnet test tests/Cadenza.Core.Tests/Cadenza.Core.Tests.csproj --verbosity normal --logger "console;verbosity=detailed"
if [ $? -ne 0 ]; then
    echo "❌ Cadenza.Core.Tests failed!"
    exit 1
fi
echo "✅ Cadenza.Core.Tests passed"
echo ""

echo "=== ALL TESTS PASSED! ==="
echo "✅ Refactoring verification complete"
echo "✅ 100% test pass rate achieved"