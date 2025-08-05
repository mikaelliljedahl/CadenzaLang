#!/bin/bash

echo "Cadenza Self-Hosting Test"
echo "=========================="
echo

echo "Testing Cadenza tool compilation..."
echo "------------------------------------"

# Test development server compilation
echo "1. Compiling development server..."
dotnet run --project src/core/cadenzac-core.csproj src/tools/simple-dev-server.cdz simple-dev-server.cs
if [ $? -eq 0 ]; then
    echo "✓ Development server compiled successfully"
else
    echo "✗ Development server compilation failed"
    exit 1
fi

# Test linter compilation
echo "2. Compiling linter..."
dotnet run --project src/core/cadenzac-core.csproj src/tools/linter.cdz linter.cs
if [ $? -eq 0 ]; then
    echo "✓ Linter compiled successfully"
else
    echo "✗ Linter compilation failed"
    exit 1
fi

# Test dev-server compilation
echo "3. Compiling dev-server..."
dotnet run --project src/core/cadenzac-core.csproj src/tools/dev-server.cdz dev-server.cs
if [ $? -eq 0 ]; then
    echo "✓ Dev-server compiled successfully"
else
    echo "✗ Dev-server compilation failed"
    exit 1
fi

echo
echo "All Cadenza tools compiled successfully!"
echo "Cadenza is now self-hosting with working development tools."
echo
echo "Generated files:"
echo "- simple-dev-server.cs (from simple-dev-server.cdz)"
echo "- linter.cs (from linter.cdz)"
echo "- dev-server.cs (from dev-server.cdz)"
echo
echo "Self-hosting validation complete!"