# Module System Examples

This document provides comprehensive examples of Cadenza's module system, demonstrating how to organize code into reusable modules with explicit imports and exports.

## Table of Contents

1. [Basic Module Structure](#basic-module-structure)
2. [Import Patterns](#import-patterns)
3. [Export Strategies](#export-strategies)
4. [Module Composition](#module-composition)
5. [Real-world Examples](#real-world-examples)
6. [Best Practices](#best-practices)

## Basic Module Structure

### Simple Module Declaration

```cadenza
// File: math_utils.cdz
module MathUtils {
    pure function add(a: int, b: int) -> int {
        return a + b
    }
    
    pure function subtract(a: int, b: int) -> int {
        return a - b
    }
    
    pure function multiply(a: int, b: int) -> int {
        return a * b
    }
    
    // Private function - not exported
    pure function helperFunction(x: int) -> int {
        return x * 2
    }
    
    // Export only public functions
    export {add, subtract, multiply}
}
```

### Using Modules

```cadenza
// File: calculator.cdz
import MathUtils.{add, multiply}

function calculate(x: int, y: int, z: int) -> int {
    let sum = add(x, y)        // From selective import
    let product = multiply(sum, z)  // From selective import
    return product
}
```

## Import Patterns

### Selective Import

Import specific functions from a module:

```cadenza
// Import only needed functions
import MathUtils.{add, multiply}
import StringUtils.{reverse, uppercase}

function processData(a: int, b: int, text: string) -> string {
    let sum = add(a, b)
    let product = multiply(sum, 2)
    let reversed = reverse(text)
    let upper = uppercase(reversed)
    return $"Result: {product}, Text: {upper}"
}
```

### Wildcard Import

Import all exported functions from a module:

```cadenza
// Import all functions from modules
import MathUtils.*
import StringUtils.*

function demonstrateWildcard(x: int, y: int, text: string) -> string {
    // Can use any exported function without qualification
    let sum = add(x, y)
    let difference = subtract(x, y)
    let product = multiply(x, y)
    
    let processed = uppercase(reverse(text))
    
    return $"Math: {sum}, {difference}, {product} | Text: {processed}"
}
```

### Simple Module Import

Import the entire module namespace:

```cadenza
// Import module for qualified access
import MathUtils
import StringUtils

function demonstrateQualified(x: int, y: int, text: string) -> string {
    // Must use qualified names
    let sum = MathUtils.add(x, y)
    let product = MathUtils.multiply(sum, 2)
    let processed = StringUtils.uppercase(text)
    
    return $"Qualified result: {product}, {processed}"
}
```

### Mixed Import Styles

Combine different import styles in the same file:

```cadenza
// Mixed import patterns
import MathUtils.{add}              // Selective import
import StringUtils.*                // Wildcard import  
import ValidationUtils              // Simple import

function mixedImportExample(a: int, b: string, email: string) -> string {
    let sum = add(a, 5)                           // From selective import
    let upper = uppercase(b)                      // From wildcard import
    let isValid = ValidationUtils.validateEmail(email)  // Qualified access
    
    return $"Sum: {sum}, Text: {upper}, Valid: {isValid}"
}
```

## Export Strategies

### Selective Export

Export only specific functions:

```cadenza
module UserService {
    // Public functions
    function createUser(name: string) uses [Database] -> Result<int, string> {
        let validated = validateUserName(name)?
        return saveUserToDatabase(validated)
    }
    
    function getUserById(id: int) uses [Database] -> Result<string, string> {
        return fetchUserFromDatabase(id)
    }
    
    // Private helper functions
    function validateUserName(name: string) -> Result<string, string> {
        if name == "" {
            return Error("Name cannot be empty")
        }
        return Ok(name)
    }
    
    function saveUserToDatabase(name: string) uses [Database] -> Result<int, string> {
        return Ok(42)  // User ID
    }
    
    function fetchUserFromDatabase(id: int) uses [Database] -> Result<string, string> {
        return Ok("User " + id)
    }
    
    // Export only public interface
    export {createUser, getUserById}
}
```

### Export All Functions

Export all functions (omit export statement):

```cadenza
module Utilities {
    pure function isEven(n: int) -> bool {
        return n % 2 == 0
    }
    
    pure function isOdd(n: int) -> bool {
        return n % 2 != 0
    }
    
    pure function clamp(value: int, min: int, max: int) -> int {
        if value < min {
            return min
        }
        if value > max {
            return max
        }
        return value
    }
    
    // No export statement means all functions are exported
}
```

### Conditional Export

Export different sets of functions based on module configuration:

```cadenza
module DatabaseUtils {
    // Core functions always available
    function connect(connectionString: string) uses [Database] -> Result<bool, string> {
        return Ok(true)
    }
    
    function disconnect() uses [Database] -> Result<bool, string> {
        return Ok(true)
    }
    
    // Standard CRUD operations
    function create(table: string, data: string) uses [Database] -> Result<int, string> {
        return Ok(42)
    }
    
    function read(table: string, id: int) uses [Database] -> Result<string, string> {
        return Ok("data")
    }
    
    function update(table: string, id: int, data: string) uses [Database] -> Result<bool, string> {
        return Ok(true)
    }
    
    function delete(table: string, id: int) uses [Database] -> Result<bool, string> {
        return Ok(true)
    }
    
    // Advanced functions
    function executeRawQuery(query: string) uses [Database] -> Result<string, string> {
        return Ok("query result")
    }
    
    function bulkInsert(table: string, records: string) uses [Database] -> Result<int, string> {
        return Ok(100)
    }
    
    // Export standard interface (advanced functions kept private)
    export {connect, disconnect, create, read, update, delete}
}
```

## Module Composition

### Layered Modules

Build higher-level modules from lower-level ones:

```cadenza
// File: string_utils.cdz
module StringUtils {
    pure function reverse(s: string) -> string {
        // Implementation would reverse the string
        return s + "_reversed"
    }
    
    pure function uppercase(s: string) -> string {
        // Implementation would convert to uppercase
        return s + "_UPPER"
    }
    
    pure function lowercase(s: string) -> string {
        // Implementation would convert to lowercase
        return s + "_lower"
    }
    
    export {reverse, uppercase, lowercase}
}

// File: validation_utils.cdz
module ValidationUtils {
    import StringUtils.{uppercase, lowercase}
    
    function validateEmail(email: string) -> Result<string, string> {
        if email == "" {
            return Error("Email cannot be empty")
        }
        let normalized = lowercase(email)
        // Email validation logic
        return Ok(normalized)
    }
    
    function validatePhoneNumber(phone: string) -> Result<string, string> {
        if phone == "" {
            return Error("Phone number cannot be empty")
        }
        // Phone validation logic
        return Ok(phone)
    }
    
    export {validateEmail, validatePhoneNumber}
}

// File: user_service.cdz
module UserService {
    import StringUtils.{uppercase}
    import ValidationUtils.{validateEmail}
    
    function createUser(name: string, email: string) 
        uses [Database, Logging] 
        -> Result<int, string> {
        
        // Validate input
        let validEmail = validateEmail(email)?
        let normalizedName = uppercase(name)
        
        // Create user logic
        return Ok(42)
    }
    
    export {createUser}
}
```

### Cross-Module Dependencies

Modules can depend on each other with proper import statements:

```cadenza
// File: config.cdz
module Config {
    pure function getDatabaseUrl() -> string {
        return "localhost:5432/mydb"
    }
    
    pure function getApiKey() -> string {
        return "api-key-12345"
    }
    
    export {getDatabaseUrl, getApiKey}
}

// File: database.cdz
module Database {
    import Config.{getDatabaseUrl}
    
    function connect() uses [Database, Logging] -> Result<bool, string> {
        let url = getDatabaseUrl()
        // Connection logic using url
        return Ok(true)
    }
    
    function query(sql: string) uses [Database] -> Result<string, string> {
        // Query execution logic
        return Ok("query result")
    }
    
    export {connect, query}
}

// File: api_client.cdz
module ApiClient {
    import Config.{getApiKey}
    
    function makeRequest(endpoint: string) uses [Network] -> Result<string, string> {
        let apiKey = getApiKey()
        // HTTP request logic using apiKey
        return Ok("response from " + endpoint)
    }
    
    export {makeRequest}
}

// File: application.cdz
module Application {
    import Database.{connect, query}
    import ApiClient.{makeRequest}
    
    function startup() uses [Database, Network, Logging] -> Result<bool, string> {
        let dbConnection = connect()?
        let apiResponse = makeRequest("/health")?
        return Ok(true)
    }
    
    export {startup}
}
```

## Real-world Examples

### E-commerce System

```cadenza
// File: models.cdz
module Models {
    // Pure functions for data modeling
    pure function createProduct(id: int, name: string, price: int) -> string {
        return $"Product(id={id}, name={name}, price={price})"
    }
    
    pure function createOrder(id: int, customerId: int, total: int) -> string {
        return $"Order(id={id}, customerId={customerId}, total={total})"
    }
    
    pure function createCustomer(id: int, name: string, email: string) -> string {
        return $"Customer(id={id}, name={name}, email={email})"
    }
    
    export {createProduct, createOrder, createCustomer}
}

// File: validation.cdz
module Validation {
    function validateProductName(name: string) -> Result<string, string> {
        if name == "" {
            return Error("Product name cannot be empty")
        }
        if name == "banned_product" {
            return Error("Product name not allowed")
        }
        return Ok(name)
    }
    
    function validatePrice(price: int) -> Result<int, string> {
        if price <= 0 {
            return Error("Price must be positive")
        }
        if price > 100000 {
            return Error("Price too high")
        }
        return Ok(price)
    }
    
    function validateEmail(email: string) -> Result<string, string> {
        if email == "" {
            return Error("Email cannot be empty")
        }
        if email == "invalid@" {
            return Error("Invalid email format")
        }
        return Ok(email)
    }
    
    export {validateProductName, validatePrice, validateEmail}
}

// File: product_service.cdz
module ProductService {
    import Models.{createProduct}
    import Validation.{validateProductName, validatePrice}
    
    function addProduct(name: string, price: int) 
        uses [Database, Logging] 
        -> Result<int, string> {
        
        let validName = validateProductName(name)?
        let validPrice = validatePrice(price)?
        
        // Create product model
        let product = createProduct(0, validName, validPrice)
        
        // Save to database (simulated)
        let productId = saveProduct(product)?
        
        return Ok(productId)
    }
    
    function getProduct(id: int) uses [Database] -> Result<string, string> {
        if id <= 0 {
            return Error("Invalid product ID")
        }
        return Ok("Product data for ID " + id)
    }
    
    function saveProduct(product: string) uses [Database] -> Result<int, string> {
        // Database save logic
        return Ok(123)  // Product ID
    }
    
    export {addProduct, getProduct}
}

// File: order_service.cdz
module OrderService {
    import Models.{createOrder}
    import ProductService.{getProduct}
    
    function createOrderWithItems(customerId: int, productId: int, quantity: int) 
        uses [Database, Logging] 
        -> Result<int, string> {
        
        // Validate product exists
        let product = getProduct(productId)?
        
        // Calculate total (simplified)
        let total = quantity * 100  // Assume $100 per item
        
        // Create order model
        let order = createOrder(0, customerId, total)
        
        // Save order
        let orderId = saveOrder(order)?
        
        return Ok(orderId)
    }
    
    function saveOrder(order: string) uses [Database] -> Result<int, string> {
        return Ok(456)  // Order ID
    }
    
    export {createOrderWithItems}
}

// File: main.cdz
import ProductService.{addProduct}
import OrderService.{createOrderWithItems}

function processEcommerceOrder(productName: string, price: int, customerId: int) 
    uses [Database, Logging] 
    -> Result<string, string> {
    
    // Add product to catalog
    let productId = addProduct(productName, price)?
    
    // Create order for customer
    let orderId = createOrderWithItems(customerId, productId, 2)?
    
    return Ok($"Order {orderId} created for product {productId}")
}
```

### Web API Framework

```cadenza
// File: http_utils.cdz
module HttpUtils {
    pure function createResponse(statusCode: int, body: string) -> string {
        return $"HTTP/{statusCode}: {body}"
    }
    
    pure function parseQueryParam(query: string, param: string) -> string {
        // Simplified query parsing
        return param + "_value"
    }
    
    export {createResponse, parseQueryParam}
}

// File: middleware.cdz
module Middleware {
    import HttpUtils.{createResponse}
    
    function authenticateRequest(token: string) uses [Database] -> Result<int, string> {
        if token == "" {
            return Error("No authentication token")
        }
        if token == "invalid_token" {
            return Error("Invalid token")
        }
        return Ok(123)  // User ID
    }
    
    function logRequest(method: string, path: string) uses [Logging] -> Result<bool, string> {
        let message = $"Request: {method} {path}"
        return Ok(true)
    }
    
    function validateRateLimit(userId: int) uses [Memory] -> Result<bool, string> {
        // Check rate limiting cache
        if userId == 999 {
            return Error("Rate limit exceeded")
        }
        return Ok(true)
    }
    
    export {authenticateRequest, logRequest, validateRateLimit}
}

// File: user_controller.cdz
module UserController {
    import HttpUtils.{createResponse, parseQueryParam}
    import Middleware.{authenticateRequest, logRequest, validateRateLimit}
    
    function getUser(token: string, query: string) 
        uses [Database, Logging, Memory] 
        -> Result<string, string> {
        
        // Log the request
        let logResult = logRequest("GET", "/api/users")?
        
        // Authenticate
        let userId = authenticateRequest(token)?
        
        // Check rate limiting
        let rateLimitOk = validateRateLimit(userId)?
        
        // Parse query parameters
        let userIdParam = parseQueryParam(query, "id")
        
        // Get user data (simulated)
        let userData = $"User data for {userIdParam}"
        
        // Create response
        let response = createResponse(200, userData)
        return Ok(response)
    }
    
    function createUser(token: string, userData: string) 
        uses [Database, Logging, Memory] 
        -> Result<string, string> {
        
        let logResult = logRequest("POST", "/api/users")?
        let userId = authenticateRequest(token)?
        let rateLimitOk = validateRateLimit(userId)?
        
        // Create user logic (simulated)
        let newUserId = 456
        let response = createResponse(201, $"User created with ID {newUserId}")
        
        return Ok(response)
    }
    
    export {getUser, createUser}
}

// File: api_server.cdz
module ApiServer {
    import UserController.{getUser, createUser}
    
    function handleRequest(method: string, path: string, token: string, body: string) 
        uses [Database, Logging, Memory, Network] 
        -> Result<string, string> {
        
        if method == "GET" && path == "/api/users" {
            return getUser(token, body)
        }
        
        if method == "POST" && path == "/api/users" {
            return createUser(token, body)
        }
        
        return Error("Route not found")
    }
    
    export {handleRequest}
}
```

### Data Processing Pipeline

```cadenza
// File: data_sources.cdz
module DataSources {
    function readCsvFile(filename: string) uses [FileSystem] -> Result<string, string> {
        if filename == "" {
            return Error("Filename cannot be empty")
        }
        return Ok("CSV data from " + filename)
    }
    
    function readJsonFile(filename: string) uses [FileSystem] -> Result<string, string> {
        if filename == "" {
            return Error("Filename cannot be empty")
        }
        return Ok("JSON data from " + filename)
    }
    
    function fetchApiData(url: string) uses [Network] -> Result<string, string> {
        if url == "" {
            return Error("URL cannot be empty")
        }
        return Ok("API data from " + url)
    }
    
    export {readCsvFile, readJsonFile, fetchApiData}
}

// File: data_transformers.cdz
module DataTransformers {
    pure function normalizeData(data: string) -> string {
        return "normalized_" + data
    }
    
    pure function filterData(data: string, criteria: string) -> string {
        return "filtered_" + data + "_by_" + criteria
    }
    
    pure function aggregateData(data: string, groupBy: string) -> string {
        return "aggregated_" + data + "_by_" + groupBy
    }
    
    function validateData(data: string) -> Result<string, string> {
        if data == "invalid_data" {
            return Error("Data validation failed")
        }
        return Ok("validated_" + data)
    }
    
    export {normalizeData, filterData, aggregateData, validateData}
}

// File: data_sinks.cdz
module DataSinks {
    function writeToDatabase(data: string, table: string) 
        uses [Database] 
        -> Result<int, string> {
        
        if table == "" {
            return Error("Table name cannot be empty")
        }
        return Ok(100)  // Records written
    }
    
    function writeToFile(data: string, filename: string) 
        uses [FileSystem] 
        -> Result<bool, string> {
        
        if filename == "" {
            return Error("Filename cannot be empty")
        }
        return Ok(true)
    }
    
    function sendToApi(data: string, endpoint: string) 
        uses [Network] 
        -> Result<bool, string> {
        
        if endpoint == "" {
            return Error("Endpoint cannot be empty")
        }
        return Ok(true)
    }
    
    export {writeToDatabase, writeToFile, sendToApi}
}

// File: pipeline.cdz
module Pipeline {
    import DataSources.{readCsvFile, fetchApiData}
    import DataTransformers.{normalizeData, filterData, validateData}
    import DataSinks.{writeToDatabase, writeToFile}
    
    function processDataPipeline(sourceFile: string, apiUrl: string, outputTable: string) 
        uses [FileSystem, Network, Database, Logging] 
        -> Result<string, string> {
        
        // Extract data from multiple sources
        let csvData = readCsvFile(sourceFile)?
        let apiData = fetchApiData(apiUrl)?
        
        // Combine data sources
        let combinedData = csvData + "|" + apiData
        
        // Transform data
        let normalized = normalizeData(combinedData)
        let filtered = filterData(normalized, "active_users")
        let validated = validateData(filtered)?
        
        // Load to destinations
        let recordsWritten = writeToDatabase(validated, outputTable)?
        let fileWritten = writeToFile(validated, "backup.txt")?
        
        return Ok($"Pipeline completed: {recordsWritten} records processed")
    }
    
    export {processDataPipeline}
}
```

## Best Practices

### 1. Organize by Domain

Group related functionality into domain-specific modules:

```cadenza
// Good: Domain-specific organization
module UserManagement { ... }
module OrderProcessing { ... }
module PaymentHandling { ... }

// Avoid: Technology-specific organization
module DatabaseFunctions { ... }
module HttpHandlers { ... }
module UtilityFunctions { ... }
```

### 2. Use Clear Import Patterns

Choose the right import style for your use case:

```cadenza
// Use selective imports for a few specific functions
import MathUtils.{add, multiply}

// Use wildcard imports when you need many functions
import StringUtils.*

// Use qualified imports to avoid naming conflicts
import Database
import Cache

function example() -> string {
    return Database.query("SELECT * FROM users") + Cache.get("key")
}
```

### 3. Design Clear Module Interfaces

Export only what consumers need:

```cadenza
module UserService {
    // Public API - exported
    function createUser(name: string) -> Result<int, string> { ... }
    function getUser(id: int) -> Result<string, string> { ... }
    
    // Private implementation details - not exported
    function validateUserName(name: string) -> Result<string, string> { ... }
    function hashPassword(password: string) -> string { ... }
    
    export {createUser, getUser}
}
```

### 4. Minimize Cross-Module Dependencies

Avoid circular dependencies and deep dependency chains:

```cadenza
// Good: Clear dependency hierarchy
// Models <- Validation <- Services <- Controllers

// Avoid: Circular dependencies
// ServiceA imports ServiceB, ServiceB imports ServiceA
```

### 5. Use Consistent Naming

Follow consistent naming conventions across modules:

```cadenza
// Module names: PascalCase
module UserService { ... }
module OrderManagement { ... }

// Function names: camelCase
function createUser(...) { ... }
function processOrder(...) { ... }
```

### 6. Document Module Purpose

Each module should have a clear, single responsibility:

```cadenza
// Good: Single responsibility
module EmailService {
    // Handles all email-related operations
    function sendWelcomeEmail(...) { ... }
    function sendPasswordReset(...) { ... }
}

// Avoid: Mixed responsibilities
module UserEmailDatabaseService {
    // Too many responsibilities mixed together
}
```

The module system in Cadenza provides powerful tools for organizing code into maintainable, reusable units. By following these patterns and best practices, you can build well-structured applications with clear separation of concerns and minimal coupling between components.