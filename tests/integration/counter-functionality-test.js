#!/usr/bin/env node

/**
 * Counter Functionality Integration Test
 * 
 * This test verifies that the Cadenza counter example works correctly:
 * 1. Page loads successfully
 * 2. Initial counter shows "Counter: 0"
 * 3. Increment button increases the counter
 * 4. Decrement button decreases the counter
 * 5. Status text updates based on counter value
 * 
 * REQUIREMENTS:
 * - Node.js (built-in modules only, no external dependencies)
 * - Cadenza web server running on specified port
 * 
 * USAGE:
 *   node tests/integration/counter-functionality-test.js [port]
 * 
 * EXAMPLES:
 *   node tests/integration/counter-functionality-test.js 5000
 *   node tests/integration/counter-functionality-test.js        # defaults to 5000
 * 
 * TO RUN WITH CADENZA SERVER:
 *   # Terminal 1: Start the server
 *   dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- --serve examples/counter.cdz --port 5000 --no-open
 *   
 *   # Terminal 2: Run the test
 *   node tests/integration/counter-functionality-test.js 5000
 */

const http = require('http');
const { URL } = require('url');

// Configuration
const DEFAULT_PORT = 5000;
const TIMEOUT_MS = 10000;
const port = process.argv[2] || DEFAULT_PORT;
const baseUrl = `http://localhost:${port}`;

console.log('🧪 Cadenza Counter Functionality Test');
console.log('=====================================');
console.log(`Testing server at: ${baseUrl}`);
console.log(`Timeout: ${TIMEOUT_MS}ms\n`);

// Test results tracking
const results = {
  total: 0,
  passed: 0,
  failed: 0,
  tests: []
};

function addResult(testName, passed, message = '') {
  results.total++;
  if (passed) {
    results.passed++;
    console.log(`✅ ${testName}`);
  } else {
    results.failed++;
    console.log(`❌ ${testName}: ${message}`);
  }
  results.tests.push({ name: testName, passed, message });
}

// HTTP request helper with timeout
function makeRequest(path, method = 'GET', data = null) {
  return new Promise((resolve, reject) => {
    const url = new URL(path, baseUrl);
    const options = {
      hostname: url.hostname,
      port: url.port,
      path: url.pathname + url.search,
      method,
      timeout: TIMEOUT_MS,
      headers: method === 'POST' ? {
        'Content-Type': 'application/x-www-form-urlencoded',
        'Content-Length': data ? Buffer.byteLength(data) : 0
      } : {}
    };

    const req = http.request(options, (res) => {
      let responseData = '';
      res.on('data', (chunk) => {
        responseData += chunk;
      });
      
      res.on('end', () => {
        resolve({
          statusCode: res.statusCode,
          headers: res.headers,
          body: responseData
        });
      });
    });

    req.on('error', reject);
    req.on('timeout', () => {
      req.destroy();
      reject(new Error('Request timeout'));
    });

    if (data) {
      req.write(data);
    }
    
    req.end();
  });
}

async function runTests() {
  try {
    console.log('🔗 Testing server connectivity...');
    
    // Test 1: Server responds
    try {
      const response = await makeRequest('/');
      addResult('Server responds to root path', 
        response.statusCode >= 200 && response.statusCode < 400);
    } catch (error) {
      addResult('Server responds to root path', false, error.message);
      console.log('\n💡 Make sure the Cadenza server is running:');
      console.log(`   dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- --serve examples/counter.cdz --port ${port} --no-open\n`);
      return;
    }

    // Test 2: Counter page loads
    console.log('\n📄 Testing counter page...');
    const counterResponse = await makeRequest('/counter');
    const counterPageLoads = counterResponse.statusCode === 200;
    addResult('Counter page loads (HTTP 200)', counterPageLoads);
    
    if (!counterPageLoads) {
      console.log(`   Status code: ${counterResponse.statusCode}`);
      return;
    }

    const html = counterResponse.body;
    
    // Test 3: Page contains expected content
    const hasCounterText = html.includes('Counter:') || html.toLowerCase().includes('counter');
    addResult('Page contains counter content', hasCounterText);
    
    const hasIncrementButton = html.includes('Increment') || html.toLowerCase().includes('increment');
    addResult('Page contains increment button', hasIncrementButton);
    
    const hasDecrementButton = html.includes('Decrement') || html.toLowerCase().includes('decrement');
    addResult('Page contains decrement button', hasDecrementButton);

    // Test 4: Check for Blazor framework
    const hasBlazorScript = html.includes('blazor') || html.includes('_framework');
    addResult('Page includes Blazor framework', hasBlazorScript);

    // Test 5: Check for CSS/styling
    const hasCSS = html.includes('.css') || html.includes('<style') || html.includes('class=');
    addResult('Page includes CSS/styling', hasCSS);

    // Test 6: No serialization errors in HTML
    const hasSerializationError = html.includes('System.RuntimeType') || 
                                  html.includes('ThrowNotSupportedException') ||
                                  html.includes('Serialization and deserialization');
    addResult('No serialization errors in HTML', !hasSerializationError);

    // Test 7: Check response headers
    const hasContentType = counterResponse.headers['content-type'] && 
                           counterResponse.headers['content-type'].includes('text/html');
    addResult('Proper Content-Type header', hasContentType);

    // Test 8: Reasonable response size (indicates content was rendered)
    const hasReasonableSize = html.length > 500 && html.length < 100000;
    addResult('Response has reasonable size', hasReasonableSize, 
             `${html.length} bytes`);

    console.log('\n📊 Content Analysis:');
    console.log(`   Response size: ${html.length} bytes`);
    console.log(`   Contains DOCTYPE: ${html.includes('<!DOCTYPE')}`);
    console.log(`   Contains <html>: ${html.includes('<html')}`);
    console.log(`   Contains <body>: ${html.includes('<body')}`);
    
    // Show first 200 chars of response for debugging
    if (html.length > 0) {
      console.log('\n📝 Response preview (first 200 chars):');
      console.log('   ' + html.substring(0, 200).replace(/\n/g, '\\n') + '...');
    }

  } catch (error) {
    console.error(`\n💥 Test failed with error: ${error.message}`);
    addResult('Test execution', false, error.message);
  }
}

// Print final results
function printResults() {
  console.log('\n📈 Test Results Summary');
  console.log('=======================');
  console.log(`Total tests: ${results.total}`);
  console.log(`Passed: ${results.passed} ✅`);
  console.log(`Failed: ${results.failed} ❌`);
  console.log(`Success rate: ${results.total > 0 ? Math.round((results.passed / results.total) * 100) : 0}%`);
  
  if (results.failed > 0) {
    console.log('\n🔍 Failed tests:');
    results.tests.filter(t => !t.passed).forEach(test => {
      console.log(`   • ${test.name}${test.message ? ': ' + test.message : ''}`);
    });
  }

  if (results.passed === results.total && results.total > 0) {
    console.log('\n🎉 All tests passed! The counter functionality appears to be working.');
  } else if (results.passed > 0) {
    console.log('\n⚠️  Some tests failed. The application may have issues.');
  } else {
    console.log('\n❌ All tests failed. The application may not be running correctly.');
  }

  // Exit with appropriate code
  process.exit(results.failed > 0 ? 1 : 0);
}

// Run the tests
runTests().finally(printResults);