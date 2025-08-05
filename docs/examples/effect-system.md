# Effect System Examples

This document provides comprehensive examples of Cadenza's effect system, demonstrating how to explicitly declare and manage side effects in your code.

## Table of Contents

1. [Pure Functions](#pure-functions)
2. [Effect Declarations](#effect-declarations)
3. [Individual Effects](#individual-effects)
4. [Effect Composition](#effect-composition)
5. [Effect Validation](#effect-validation)
6. [Real-world Examples](#real-world-examples)

## Pure Functions

### What are Pure Functions?

Pure functions have no side effects and always return the same output for the same input:

```cadenza
// Mathematical operations are naturally pure
pure function add(a: int, b: int) -> int {
    return a + b
}

pure function multiply(a: int, b: int) -> int {
    return a * b
}

pure function square(x: int) -> int {
    return x * x
}

// String operations can be pure
pure function concatenate(a: string, b: string) -> string {
    return a + b
}

pure function formatCurrency(amount: int) -> string {
    return "$" + amount
}

// Boolean logic is pure
pure function isEven(n: int) -> bool {
    return n % 2 == 0
}

pure function isInRange(value: int, min: int, max: int) -> bool {
    return value >= min && value <= max
}
```

### Pure Function Composition

```cadenza
pure function calculateTax(amount: int, rate: int) -> int {
    return amount * rate / 100
}

pure function calculateTotal(subtotal: int, taxRate: int) -> int {
    let tax = calculateTax(subtotal, taxRate)
    return subtotal + tax
}

pure function formatPrice(amount: int, currency: string) -> string {
    return currency + amount
}

pure function processOrderTotal(subtotal: int, taxRate: int) -> string {
    let total = calculateTotal(subtotal, taxRate)
    return formatPrice(total, "$")
}
```

## Effect Declarations

### Basic Effect Syntax

Functions declare their side effects using the `uses` keyword:

```cadenza
// Function with single effect
function logMessage(message: string) uses [Logging] -> Result<string, string> {
    // Would actually log the message in real implementation
    return Ok("Logged: " + message)
}

// Function with multiple effects
function saveUser(name: string, email: string) 
    uses [Database, Logging] 
    -> Result<int, string> {
    
    // Would log the operation and save to database
    return Ok(42)  // User ID
}

// Function with all available effects
function complexOperation(data: string) 
    uses [Database, Network, Logging, FileSystem, Memory, IO] 
    -> Result<string, string> {
    
    // Complex operation using multiple side effects
    return Ok("Processed: " + data)
}
```

### Effect Inheritance

When a function calls other functions with effects, it must declare all used effects:

```cadenza
function writeLog(message: string) uses [Logging] -> Result<string, string> {
    return Ok("Logged: " + message)
}

function saveToDatabase(data: string) uses [Database] -> Result<int, string> {
    return Ok(42)
}

// Must declare both Logging and Database effects
function saveWithLogging(data: string) 
    uses [Database, Logging] 
    -> Result<int, string> {
    
    let logResult = writeLog("Saving data: " + data)?
    let saveResult = saveToDatabase(data)?
    return Ok(saveResult)
}
```

## Individual Effects

### Database Effect

```cadenza
// Simple database operations
function createUser(name: string) uses [Database] -> Result<int, string> {
    if name == "" {
        return Error("Name cannot be empty")
    }
    // Would create user in database
    return Ok(123)  // User ID
}

function getUser(userId: int) uses [Database] -> Result<string, string> {
    if userId <= 0 {
        return Error("Invalid user ID")
    }
    // Would fetch from database
    return Ok("User name for ID " + userId)
}

function updateUser(userId: int, name: string) uses [Database] -> Result<bool, string> {
    if userId <= 0 {
        return Error("Invalid user ID")
    }
    if name == "" {
        return Error("Name cannot be empty")
    }
    // Would update in database
    return Ok(true)
}

function deleteUser(userId: int) uses [Database] -> Result<bool, string> {
    if userId <= 0 {
        return Error("Invalid user ID")
    }
    // Would delete from database
    return Ok(true)
}
```

### Network Effect

```cadenza
// HTTP operations
function fetchUserData(userId: int) uses [Network] -> Result<string, string> {
    if userId <= 0 {
        return Error("Invalid user ID")
    }
    // Would make HTTP request
    return Ok("External user data for " + userId)
}

function sendNotification(message: string, endpoint: string) 
    uses [Network] 
    -> Result<bool, string> {
    
    if message == "" {
        return Error("Message cannot be empty")
    }
    if endpoint == "" {
        return Error("Endpoint cannot be empty")
    }
    // Would send HTTP POST request
    return Ok(true)
}

function downloadFile(url: string) uses [Network] -> Result<string, string> {
    if url == "" {
        return Error("URL cannot be empty")
    }
    // Would download file from URL
    return Ok("File content from " + url)
}
```

### Logging Effect

```cadenza
// Logging operations
function logInfo(message: string) uses [Logging] -> Result<string, string> {
    // Would write to log file or service
    return Ok("INFO: " + message)
}

function logError(error: string) uses [Logging] -> Result<string, string> {
    // Would write error to log
    return Ok("ERROR: " + error)
}

function logWarning(warning: string) uses [Logging] -> Result<string, string> {
    // Would write warning to log
    return Ok("WARNING: " + warning)
}

function auditLog(action: string, userId: int) uses [Logging] -> Result<string, string> {
    let message = $"User {userId} performed action: {action}"
    // Would write to audit log
    return Ok("AUDIT: " + message)
}
```

### FileSystem Effect

```cadenza
// File operations
function readFile(filename: string) uses [FileSystem] -> Result<string, string> {
    if filename == "" {
        return Error("Filename cannot be empty")
    }
    // Would read file from disk
    return Ok("Contents of " + filename)
}

function writeFile(filename: string, content: string) 
    uses [FileSystem] 
    -> Result<bool, string> {
    
    if filename == "" {
        return Error("Filename cannot be empty")
    }
    // Would write file to disk
    return Ok(true)
}

function deleteFile(filename: string) uses [FileSystem] -> Result<bool, string> {
    if filename == "" {
        return Error("Filename cannot be empty")
    }
    // Would delete file from disk
    return Ok(true)
}

function listFiles(directory: string) uses [FileSystem] -> Result<string, string> {
    if directory == "" {
        return Error("Directory cannot be empty")
    }
    // Would list files in directory
    return Ok("Files in " + directory)
}
```

### Memory Effect

```cadenza
// Memory allocation operations
function allocateBuffer(size: int) uses [Memory] -> Result<string, string> {
    if size <= 0 {
        return Error("Buffer size must be positive")
    }
    if size > 1000000 {
        return Error("Buffer size too large")
    }
    // Would allocate memory buffer
    return Ok("Buffer of size " + size)
}

function processLargeDataset(data: string) uses [Memory] -> Result<string, string> {
    // Would use significant memory for processing
    return Ok("Processed large dataset: " + data)
}

function cacheData(key: string, value: string) uses [Memory] -> Result<bool, string> {
    if key == "" {
        return Error("Cache key cannot be empty")
    }
    // Would store in memory cache
    return Ok(true)
}
```

### IO Effect

```cadenza
// Input/output operations
function readUserInput() uses [IO] -> Result<string, string> {
    // Would read from stdin
    return Ok("User input")
}

function printMessage(message: string) uses [IO] -> Result<bool, string> {
    // Would write to stdout
    return Ok(true)
}

function readEnvironmentVariable(name: string) uses [IO] -> Result<string, string> {
    if name == "" {
        return Error("Variable name cannot be empty")
    }
    // Would read environment variable
    return Ok("Value of " + name)
}
```

## Effect Composition

### Combining Multiple Effects

```cadenza
// User service combining multiple effects
function createUserWithAudit(name: string, email: string) 
    uses [Database, Logging] 
    -> Result<int, string> {
    
    // Log the operation start
    let logStart = logInfo($"Creating user: {name}")?
    
    // Create the user
    let userId = createUser(name)?
    
    // Log the success
    let logSuccess = auditLog("user_created", userId)?
    
    return Ok(userId)
}

// Data processing pipeline
function processUserData(userId: int) 
    uses [Database, Network, Logging, FileSystem] 
    -> Result<string, string> {
    
    // Fetch user from database
    let userData = getUser(userId)?
    
    // Fetch additional data from external API
    let externalData = fetchUserData(userId)?
    
    // Log the processing
    let logResult = logInfo($"Processing data for user {userId}")?
    
    // Save processed data to file
    let combinedData = userData + " | " + externalData
    let saveResult = writeFile($"user_{userId}.txt", combinedData)?
    
    return Ok("Processed and saved data for user " + userId)
}

// Complete workflow with all effects
function completeUserWorkflow(name: string, email: string) 
    uses [Database, Network, Logging, FileSystem, Memory, IO] 
    -> Result<string, string> {
    
    // Create user
    let userId = createUserWithAudit(name, email)?
    
    // Process their data
    let processResult = processUserData(userId)?
    
    // Send welcome notification
    let notifyResult = sendNotification("Welcome!", "api.example.com/notify")?
    
    // Cache user data
    let cacheResult = cacheData($"user_{userId}", name)?
    
    // Print confirmation
    let printResult = printMessage($"User {name} created successfully")?
    
    return Ok($"Workflow completed for user {userId}")
}
```

### Effect Propagation

```cadenza
// Low-level operations
function writeToLog(level: string, message: string) uses [Logging] -> Result<bool, string> {
    return Ok(true)
}

function saveToDatabase(table: string, data: string) uses [Database] -> Result<int, string> {
    return Ok(42)
}

// Mid-level operations that combine effects
function loggedDatabaseOperation(data: string) 
    uses [Database, Logging] 
    -> Result<int, string> {
    
    let logResult = writeToLog("INFO", "Saving data: " + data)?
    let saveResult = saveToDatabase("users", data)?
    return Ok(saveResult)
}

// High-level operations that use mid-level operations
function businessOperation(userData: string) 
    uses [Database, Logging] 
    -> Result<int, string> {
    
    // Uses the mid-level operation, inheriting its effects
    let result = loggedDatabaseOperation(userData)?
    return Ok(result)
}
```

## Effect Validation

### Effect System Rules

The effect system enforces these rules:

1. **Pure functions cannot have effects**
2. **Pure functions cannot call functions with effects**
3. **Functions must declare all effects they use**
4. **Effect declarations are validated at compile time**

```cadenza
// ✅ Valid: Pure function calling pure function
pure function validPureFunction(x: int) -> int {
    return add(x, 5)  // add is pure, so this is allowed
}

// ❌ Invalid: Pure function cannot declare effects
// pure function invalidPureFunction(x: int) uses [Database] -> int {
//     return x
// }

// ❌ Invalid: Pure function cannot call function with effects  
// pure function invalidPureCall() -> string {
//     return logMessage("test")  // logMessage has Logging effect
// }

// ✅ Valid: Function declares effects it uses
function validEffectFunction(message: string) uses [Logging] -> Result<string, string> {
    return logMessage(message)  // logMessage uses Logging effect
}

// ❌ Invalid: Function doesn't declare required effects
// function invalidEffectFunction(message: string) -> Result<string, string> {
//     return logMessage(message)  // Missing Logging effect declaration
// }
```

### Effect Checking Examples

```cadenza
// Helper functions with different effects
function dbOperation() uses [Database] -> Result<int, string> {
    return Ok(42)
}

function logOperation() uses [Logging] -> Result<string, string> {
    return Ok("logged")
}

function networkOperation() uses [Network] -> Result<string, string> {
    return Ok("network result")
}

// ✅ Correct: Declares all used effects
function correctEffectUsage() uses [Database, Logging, Network] -> Result<string, string> {
    let dbResult = dbOperation()?
    let logResult = logOperation()?
    let netResult = networkOperation()?
    return Ok("all operations completed")
}

// ❌ Would be invalid: Missing Network effect
// function incorrectEffectUsage() uses [Database, Logging] -> Result<string, string> {
//     let dbResult = dbOperation()?
//     let logResult = logOperation()?
//     let netResult = networkOperation()?  // Error: Network effect not declared
//     return Ok("operations")
// }
```

## Real-world Examples

### E-commerce Order Processing

```cadenza
function processOrder(customerId: int, items: string, total: int) 
    uses [Database, Network, Logging, FileSystem] 
    -> Result<int, string> {
    
    // Log order start
    let logStart = logInfo($"Processing order for customer {customerId}")?
    
    // Validate customer
    let customer = getUser(customerId)?
    
    // Create order in database
    let orderId = createOrder(customerId, items, total)?
    
    // Send confirmation email
    let emailResult = sendOrderConfirmation(customer, orderId)?
    
    // Generate invoice file
    let invoiceResult = generateInvoice(orderId, items, total)?
    
    // Log completion
    let logComplete = auditLog("order_processed", customerId)?
    
    return Ok(orderId)
}

function createOrder(customerId: int, items: string, total: int) 
    uses [Database] 
    -> Result<int, string> {
    
    if customerId <= 0 {
        return Error("Invalid customer ID")
    }
    if total <= 0 {
        return Error("Order total must be positive")
    }
    return Ok(12345)  // Order ID
}

function sendOrderConfirmation(customer: string, orderId: int) 
    uses [Network] 
    -> Result<bool, string> {
    
    let message = $"Order {orderId} confirmed for {customer}"
    return sendNotification(message, "email.service.com/send")
}

function generateInvoice(orderId: int, items: string, total: int) 
    uses [FileSystem] 
    -> Result<bool, string> {
    
    let invoiceContent = $"Invoice for Order {orderId}\nItems: {items}\nTotal: ${total}"
    return writeFile($"invoice_{orderId}.txt", invoiceContent)
}
```

### User Authentication System

```cadenza
function authenticateUser(username: string, password: string) 
    uses [Database, Logging, Memory] 
    -> Result<int, string> {
    
    // Log authentication attempt
    let logAttempt = auditLog("login_attempt", 0)?  // 0 for unknown user initially
    
    // Look up user
    let userId = findUserByUsername(username)?
    
    // Verify password
    let isValid = verifyPassword(userId, password)?
    
    if !isValid {
        let logFail = auditLog("login_failed", userId)?
        return Error("Invalid credentials")
    }
    
    // Cache session
    let sessionResult = createSession(userId)?
    
    // Log successful login
    let logSuccess = auditLog("login_success", userId)?
    
    return Ok(userId)
}

function findUserByUsername(username: string) uses [Database] -> Result<int, string> {
    if username == "" {
        return Error("Username cannot be empty")
    }
    if username == "nonexistent" {
        return Error("User not found")
    }
    return Ok(456)  // User ID
}

function verifyPassword(userId: int, password: string) uses [Database] -> Result<bool, string> {
    if password == "" {
        return Error("Password cannot be empty")
    }
    // Would verify against stored hash
    return Ok(password != "wrong_password")
}

function createSession(userId: int) uses [Memory] -> Result<string, string> {
    let sessionId = "session_" + userId
    let cacheResult = cacheData(sessionId, "active")?
    return Ok(sessionId)
}
```

### Data Backup System

```cadenza
function performBackup(dataType: string, destination: string) 
    uses [Database, FileSystem, Network, Logging] 
    -> Result<string, string> {
    
    // Log backup start
    let logStart = logInfo($"Starting backup of {dataType} to {destination}")?
    
    // Extract data from database
    let data = extractData(dataType)?
    
    // Compress and save locally
    let localFile = saveBackupLocally(data, dataType)?
    
    // Upload to remote location
    let uploadResult = uploadBackup(localFile, destination)?
    
    // Verify backup integrity
    let verifyResult = verifyBackup(destination, dataType)?
    
    // Log completion
    let logComplete = auditLog("backup_completed", 0)?
    
    return Ok($"Backup completed: {uploadResult}")
}

function extractData(dataType: string) uses [Database] -> Result<string, string> {
    if dataType == "" {
        return Error("Data type cannot be empty")
    }
    // Would extract data from appropriate tables
    return Ok("Backup data for " + dataType)
}

function saveBackupLocally(data: string, dataType: string) 
    uses [FileSystem] 
    -> Result<string, string> {
    
    let filename = $"backup_{dataType}.dat"
    let saveResult = writeFile(filename, data)?
    return Ok(filename)
}

function uploadBackup(filename: string, destination: string) 
    uses [Network] 
    -> Result<string, string> {
    
    if destination == "" {
        return Error("Destination cannot be empty")
    }
    // Would upload file to remote server
    return Ok($"Uploaded {filename} to {destination}")
}

function verifyBackup(destination: string, dataType: string) 
    uses [Network] 
    -> Result<bool, string> {
    
    // Would verify backup exists and is valid
    return Ok(true)
}
```

### Configuration Management

```cadenza
function loadApplicationConfig(environment: string) 
    uses [FileSystem, Network, Logging, Memory] 
    -> Result<string, string> {
    
    // Log config loading
    let logStart = logInfo($"Loading config for environment: {environment}")?
    
    // Load base configuration
    let baseConfig = loadConfigFile("config/base.json")?
    
    // Load environment-specific config
    let envConfig = loadConfigFile($"config/{environment}.json")?
    
    // Fetch remote configuration overrides
    let remoteConfig = fetchRemoteConfig(environment)?
    
    // Merge configurations
    let mergedConfig = mergeConfigs(baseConfig, envConfig, remoteConfig)
    
    // Cache the final configuration
    let cacheResult = cacheData($"config_{environment}", mergedConfig)?
    
    // Log success
    let logComplete = logInfo("Configuration loaded successfully")?
    
    return Ok(mergedConfig)
}

function loadConfigFile(filename: string) uses [FileSystem] -> Result<string, string> {
    return readFile(filename)
}

function fetchRemoteConfig(environment: string) uses [Network] -> Result<string, string> {
    let url = $"https://config.service.com/{environment}"
    return downloadFile(url)
}

pure function mergeConfigs(base: string, env: string, remote: string) -> string {
    // Pure function to merge configuration strings
    return base + "|" + env + "|" + remote
}
```

## Best Practices

1. **Use pure functions when possible**: Minimize side effects in your code
2. **Be explicit about effects**: Always declare all effects a function uses
3. **Compose effects carefully**: Understand how effects propagate through function calls
4. **Validate at boundaries**: Check inputs before performing side effects
5. **Handle errors properly**: Use Result types with effect functions
6. **Keep effects minimal**: Don't declare effects you don't actually use
7. **Group related effects**: Functions that work together often use similar effects

The effect system makes side effects visible and trackable, leading to more predictable and maintainable code. It helps developers understand the full impact of calling any function and makes testing and reasoning about code much easier.