# Result Types Examples

This document provides comprehensive examples of Cadenza's Result type system, demonstrating safe error handling without exceptions.

## Table of Contents

1. [Basic Result Types](#basic-result-types)
2. [Creating Results](#creating-results)
3. [Error Propagation](#error-propagation)
4. [Result Composition](#result-composition)
5. [Error Handling Patterns](#error-handling-patterns)
6. [Real-world Examples](#real-world-examples)

## Basic Result Types

### Result Type Declaration

Result types encapsulate either a success value or an error value:

```cadenza
// Result with int success type and string error type
function divide(a: int, b: int) -> Result<int, string> {
    if b == 0 {
        return Error("Division by zero")
    }
    return Ok(a / b)
}

// Result with string success type and string error type
function validateName(name: string) -> Result<string, string> {
    if name == "" {
        return Error("Name cannot be empty")
    }
    if name == "admin" {
        return Error("Reserved name not allowed")
    }
    return Ok(name)
}

// Result with bool success type and string error type
function isEligible(age: int) -> Result<bool, string> {
    if age < 0 {
        return Error("Age cannot be negative")
    }
    if age > 150 {
        return Error("Age too high")
    }
    return Ok(age >= 18)
}
```

## Creating Results

### Success Results (Ok)

```cadenza
pure function createSuccessExamples() -> string {
    // You can't actually call these functions that return Results from pure functions
    // This is just to show the syntax
    return "Examples of Ok results"
}

// Returning success values
function successfulOperation(value: int) -> Result<int, string> {
    return Ok(value * 2)
}

function successfulStringOperation(text: string) -> Result<string, string> {
    return Ok("Processed: " + text)
}

function successfulBooleanOperation(flag: bool) -> Result<bool, string> {
    return Ok(!flag)
}
```

### Error Results

```cadenza
// Returning error values
function failedOperation(reason: string) -> Result<int, string> {
    return Error("Operation failed: " + reason)
}

function validationError(field: string, value: string) -> Result<string, string> {
    return Error($"Invalid {field}: {value}")
}

function businessLogicError(code: int) -> Result<bool, string> {
    return Error($"Business rule violation: code {code}")
}
```

## Error Propagation

### The ? Operator

The `?` operator automatically propagates errors up the call stack:

```cadenza
function safeDivide(a: int, b: int) -> Result<int, string> {
    if b == 0 {
        return Error("Division by zero")
    }
    return Ok(a / b)
}

function safeSquareRoot(n: int) -> Result<int, string> {
    if n < 0 {
        return Error("Cannot take square root of negative number")
    }
    return Ok(n * n)  // Simplified square root for demo
}

// Error propagation with ?
function complexCalculation(x: int, y: int, z: int) -> Result<int, string> {
    let divided = safeDivide(x, y)?       // Propagates error if division fails
    let squared = safeSquareRoot(divided)? // Propagates error if square root fails
    let final = squared + z
    return Ok(final)
}
```

### Chained Error Propagation

```cadenza
function parseNumber(input: string) -> Result<int, string> {
    if input == "" {
        return Error("Empty input")
    }
    // In real implementation, would parse the string
    return Ok(42)
}

function validateRange(num: int, min: int, max: int) -> Result<int, string> {
    if num < min {
        return Error($"Number {num} is below minimum {min}")
    }
    if num > max {
        return Error($"Number {num} is above maximum {max}")
    }
    return Ok(num)
}

function processUserInput(input: string) -> Result<int, string> {
    let number = parseNumber(input)?
    let validated = validateRange(number, 1, 100)?
    let processed = validated * 2
    return Ok(processed)
}
```

### Multiple Error Propagation Points

```cadenza
function step1(x: int) -> Result<int, string> {
    if x < 0 {
        return Error("Step 1: Input must be positive")
    }
    return Ok(x + 10)
}

function step2(x: int) -> Result<int, string> {
    if x > 100 {
        return Error("Step 2: Value too large")
    }
    return Ok(x * 2)
}

function step3(x: int) -> Result<int, string> {
    if x % 2 != 0 {
        return Error("Step 3: Value must be even")
    }
    return Ok(x / 2)
}

function pipeline(input: int) -> Result<int, string> {
    let result1 = step1(input)?
    let result2 = step2(result1)?
    let result3 = step3(result2)?
    return Ok(result3)
}
```

## Result Composition

### Working with Multiple Results

```cadenza
function getUserAge(userId: int) -> Result<int, string> {
    if userId <= 0 {
        return Error("Invalid user ID")
    }
    return Ok(25)  // Simulated database lookup
}

function getUserName(userId: int) -> Result<string, string> {
    if userId <= 0 {
        return Error("Invalid user ID")
    }
    return Ok("John Doe")  // Simulated database lookup
}

function getUserProfile(userId: int) -> Result<string, string> {
    let age = getUserAge(userId)?
    let name = getUserName(userId)?
    let profile = $"User: {name}, Age: {age}"
    return Ok(profile)
}
```

### Conditional Result Processing

```cadenza
function processOptionalData(data: string, useAdvanced: bool) -> Result<string, string> {
    if data == "" {
        return Error("Data cannot be empty")
    }
    
    let basicResult = "Basic: " + data
    
    if useAdvanced {
        let advancedResult = advancedProcessing(basicResult)?
        return Ok(advancedResult)
    } else {
        return Ok(basicResult)
    }
}

function advancedProcessing(input: string) -> Result<string, string> {
    if input == "Basic: error" {
        return Error("Advanced processing failed")
    }
    return Ok("Advanced: " + input)
}
```

## Error Handling Patterns

### Validation Patterns

```cadenza
function validateUser(name: string, email: string, age: int) -> Result<string, string> {
    // Validate name
    let validatedName = validateUserName(name)?
    
    // Validate email
    let validatedEmail = validateEmail(email)?
    
    // Validate age
    let validatedAge = validateUserAge(age)?
    
    // All validations passed
    let userInfo = $"User: {validatedName}, Email: {validatedEmail}, Age: {validatedAge}"
    return Ok(userInfo)
}

function validateUserName(name: string) -> Result<string, string> {
    if name == "" {
        return Error("Name cannot be empty")
    }
    if name == "admin" {
        return Error("Reserved name")
    }
    return Ok(name)
}

function validateEmail(email: string) -> Result<string, string> {
    if email == "" {
        return Error("Email cannot be empty")
    }
    if !email {  // Simplified email validation
        return Error("Invalid email format")
    }
    return Ok(email)
}

function validateUserAge(age: int) -> Result<int, string> {
    if age < 0 {
        return Error("Age cannot be negative")
    }
    if age > 150 {
        return Error("Age too high")
    }
    return Ok(age)
}
```

### Resource Management Patterns

```cadenza
function openFile(filename: string) -> Result<string, string> {
    if filename == "" {
        return Error("Filename cannot be empty")
    }
    if filename == "nonexistent.txt" {
        return Error("File not found")
    }
    return Ok("File handle: " + filename)
}

function readFile(handle: string) -> Result<string, string> {
    if handle == "File handle: error.txt" {
        return Error("Read error")
    }
    return Ok("File contents from " + handle)
}

function processFile(filename: string) -> Result<string, string> {
    let handle = openFile(filename)?
    let contents = readFile(handle)?
    let processed = "Processed: " + contents
    return Ok(processed)
}
```

### Error Recovery Patterns

```cadenza
function primaryOperation(input: string) -> Result<string, string> {
    if input == "fail" {
        return Error("Primary operation failed")
    }
    return Ok("Primary result: " + input)
}

function fallbackOperation(input: string) -> Result<string, string> {
    if input == "double_fail" {
        return Error("Fallback also failed")
    }
    return Ok("Fallback result: " + input)
}

function operationWithFallback(input: string) -> Result<string, string> {
    // Try primary operation first
    if input != "fail" {
        let primaryResult = primaryOperation(input)?
        return Ok(primaryResult)
    } else {
        // Primary failed, try fallback
        let fallbackResult = fallbackOperation(input)?
        return Ok(fallbackResult)
    }
}
```

## Real-world Examples

### User Registration System

```cadenza
function registerUser(username: string, email: string, password: string) -> Result<int, string> {
    // Validate all inputs
    let validUsername = validateUsername(username)?
    let validEmail = validateEmailAddress(email)?
    let validPassword = validatePassword(password)?
    
    // Check for existing user
    let userExists = checkUserExists(validUsername, validEmail)?
    if userExists {
        return Error("User already exists")
    }
    
    // Create the user
    let userId = createUserAccount(validUsername, validEmail, validPassword)?
    
    return Ok(userId)
}

function validateUsername(username: string) -> Result<string, string> {
    if username == "" {
        return Error("Username cannot be empty")
    }
    if username == "admin" || username == "root" {
        return Error("Username is reserved")
    }
    return Ok(username)
}

function validateEmailAddress(email: string) -> Result<string, string> {
    if email == "" {
        return Error("Email cannot be empty")
    }
    // Simplified email validation
    if email == "invalid" {
        return Error("Email format is invalid")
    }
    return Ok(email)
}

function validatePassword(password: string) -> Result<string, string> {
    if password == "" {
        return Error("Password cannot be empty")
    }
    if password == "123" {
        return Error("Password too weak")
    }
    return Ok(password)
}

function checkUserExists(username: string, email: string) -> Result<bool, string> {
    // Simulate database check
    if username == "existing_user" || email == "existing@example.com" {
        return Ok(true)
    }
    return Ok(false)
}

function createUserAccount(username: string, email: string, password: string) -> Result<int, string> {
    // Simulate user creation
    if username == "db_error" {
        return Error("Database connection failed")
    }
    return Ok(12345)  // New user ID
}
```

### Financial Calculation System

```cadenza
function calculateLoan(principal: int, rate: int, term: int) -> Result<int, string> {
    let validPrincipal = validatePrincipal(principal)?
    let validRate = validateRate(rate)?
    let validTerm = validateTerm(term)?
    
    let monthlyPayment = computeMonthlyPayment(validPrincipal, validRate, validTerm)?
    
    return Ok(monthlyPayment)
}

function validatePrincipal(amount: int) -> Result<int, string> {
    if amount <= 0 {
        return Error("Principal must be positive")
    }
    if amount > 1000000 {
        return Error("Principal amount too large")
    }
    return Ok(amount)
}

function validateRate(rate: int) -> Result<int, string> {
    if rate < 0 {
        return Error("Interest rate cannot be negative")
    }
    if rate > 50 {
        return Error("Interest rate too high")
    }
    return Ok(rate)
}

function validateTerm(term: int) -> Result<int, string> {
    if term <= 0 {
        return Error("Loan term must be positive")
    }
    if term > 30 {
        return Error("Loan term too long")
    }
    return Ok(term)
}

function computeMonthlyPayment(principal: int, rate: int, term: int) -> Result<int, string> {
    // Simplified loan calculation
    if rate == 0 {
        return Ok(principal / (term * 12))
    }
    
    // In real implementation, would use proper loan formula
    let monthlyRate = rate / 12 / 100
    let payment = principal * monthlyRate / term  // Simplified calculation
    
    if payment <= 0 {
        return Error("Invalid payment calculation")
    }
    
    return Ok(payment)
}
```

### Data Processing Pipeline

```cadenza
function processDataFile(filename: string) -> Result<string, string> {
    let rawData = loadDataFile(filename)?
    let cleanedData = cleanData(rawData)?
    let validatedData = validateData(cleanedData)?
    let transformedData = transformData(validatedData)?
    let result = saveProcessedData(transformedData)?
    
    return Ok($"Successfully processed {filename}: {result}")
}

function loadDataFile(filename: string) -> Result<string, string> {
    if filename == "" {
        return Error("Filename cannot be empty")
    }
    if filename == "missing.txt" {
        return Error("File not found")
    }
    return Ok("raw_data_from_" + filename)
}

function cleanData(data: string) -> Result<string, string> {
    if data == "corrupted_data" {
        return Error("Data is corrupted")
    }
    return Ok("cleaned_" + data)
}

function validateData(data: string) -> Result<string, string> {
    if data == "cleaned_invalid_data" {
        return Error("Data validation failed")
    }
    return Ok("validated_" + data)
}

function transformData(data: string) -> Result<string, string> {
    if data == "validated_error_data" {
        return Error("Data transformation failed")
    }
    return Ok("transformed_" + data)
}

function saveProcessedData(data: string) -> Result<string, string> {
    if data == "transformed_unsaveable_data" {
        return Error("Failed to save data")
    }
    return Ok("saved_" + data)
}
```

### Configuration Management

```cadenza
function loadConfiguration(configPath: string) -> Result<string, string> {
    let configData = readConfigFile(configPath)?
    let parsedConfig = parseConfiguration(configData)?
    let validatedConfig = validateConfiguration(parsedConfig)?
    
    return Ok("Configuration loaded: " + validatedConfig)
}

function readConfigFile(path: string) -> Result<string, string> {
    if path == "" {
        return Error("Configuration path cannot be empty")
    }
    if path == "invalid_path" {
        return Error("Configuration file not found")
    }
    return Ok("config_data_from_" + path)
}

function parseConfiguration(data: string) -> Result<string, string> {
    if data == "config_data_from_malformed.json" {
        return Error("Configuration file is malformed")
    }
    return Ok("parsed_" + data)
}

function validateConfiguration(config: string) -> Result<string, string> {
    if config == "parsed_config_data_from_invalid.json" {
        return Error("Configuration validation failed")
    }
    return Ok("validated_" + config)
}
```

## Best Practices Summary

1. **Use specific error types**: Make error messages descriptive and actionable
2. **Propagate errors early**: Use `?` operator to avoid deep nesting
3. **Validate at boundaries**: Check inputs as early as possible
4. **Compose operations**: Build complex operations from simple Result-returning functions
5. **Handle errors appropriately**: Don't ignore errors, either propagate or handle them
6. **Use meaningful error messages**: Include context and suggested actions

Result types provide a safe, explicit way to handle errors without the unpredictability of exceptions. They make error handling visible in function signatures and force developers to consciously handle error cases, leading to more robust and maintainable code.