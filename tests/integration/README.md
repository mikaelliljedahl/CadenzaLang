# Integration Tests

This directory contains integration tests for the Cadenza compiler and web server functionality.

## Counter Functionality Test

**File**: `counter-functionality-test.js`

Tests the complete counter application workflow:
- Server connectivity and response
- Counter page loading
- Presence of increment/decrement buttons
- Blazor framework integration
- CSS/styling presence
- No serialization errors
- Proper HTTP headers and response size

### Usage

1. **Start the Cadenza web server**:
   ```bash
   dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- --serve examples/counter.cdz --port 5000 --no-open
   ```

2. **Run the test** (in another terminal):
   ```bash
   node tests/integration/counter-functionality-test.js 5000
   ```

3. **Default port** (if no port specified, defaults to 5000):
   ```bash
   node tests/integration/counter-functionality-test.js
   ```

### Requirements

- Node.js (uses only built-in modules)
- Cadenza web server running on the specified port
- `examples/counter.cdz` file present

### Test Output

The test provides detailed output showing:
- ✅/❌ Individual test results
- Response size and content analysis
- Response preview (first 200 characters)
- Summary with success rate
- Exit code 0 for success, 1 for failure

### Example Output

```
🧪 Cadenza Counter Functionality Test
=====================================
Testing server at: http://localhost:5000
Timeout: 10000ms

🔗 Testing server connectivity...
✅ Server responds to root path

📄 Testing counter page...
✅ Counter page loads (HTTP 200)
✅ Page contains counter content
✅ Page contains increment button
✅ Page contains decrement button
✅ Page includes Blazor framework
✅ Page includes CSS/styling
✅ No serialization errors in HTML
✅ Proper Content-Type header
✅ Response has reasonable size

📈 Test Results Summary
=======================
Total tests: 9
Passed: 9 ✅
Failed: 0 ❌
Success rate: 100%

🎉 All tests passed! The counter functionality appears to be working.
```

## Adding More Tests

To add additional integration tests:

1. Create new `.js` files in this directory
2. Follow the same pattern as `counter-functionality-test.js`
3. Use Node.js built-in modules for maximum compatibility
4. Include clear usage instructions and examples
5. Update this README with documentation for new tests

## Best Practices

- Tests should be self-contained with no external dependencies
- Include timeout handling for network requests
- Provide clear error messages and debugging information
- Use exit codes (0 for success, 1 for failure) for CI/CD integration
- Test both positive and negative scenarios when applicable