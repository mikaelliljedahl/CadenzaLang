# Basic Syntax Examples

This document provides comprehensive examples of Cadenza's basic syntax features, demonstrating core language constructs with working code samples.

## Table of Contents

1. [Function Declarations](#function-declarations)
2. [Variables and Let Statements](#variables-and-let-statements)
3. [Data Types](#data-types)
4. [Expressions and Operators](#expressions-and-operators)
5. [Function Calls](#function-calls)
6. [Comments](#comments)

## Function Declarations

### Pure Functions

Pure functions have no side effects and are marked with the `pure` keyword:

```cadenza
// Simple pure function
pure function add(a: int, b: int) -> int {
    return a + b
}

// Pure function with string concatenation
pure function greet(name: string) -> string {
    return "Hello, " + name + "!"
}

// Pure function with boolean logic
pure function isEven(n: int) -> bool {
    return n % 2 == 0
}

// Pure function with conditional logic
pure function max(a: int, b: int) -> int {
    if a > b {
        return a
    } else {
        return b
    }
}
```

### Regular Functions

Functions without the `pure` keyword can have side effects:

```cadenza
// Function that returns a value
function calculateTax(amount: int) -> int {
    return amount * 8 / 100
}

// Function with multiple parameters
function formatMessage(user: string, count: int, active: bool) -> string {
    if active {
        return $"User {user} has {count} active items"
    } else {
        return $"User {user} is inactive"
    }
}
```

## Variables and Let Statements

### Basic Variable Declaration

```cadenza
function demonstrateVariables() -> string {
    // Integer variable
    let age = 25
    
    // String variable
    let name = "Alice"
    
    // Boolean variable
    let isActive = true
    
    // Expression result
    let doubled = age * 2
    
    // Function call result
    let greeting = greet(name)
    
    return $"User: {name}, Age: {doubled}, Active: {isActive}, Message: {greeting}"
}
```

### Variable Usage in Calculations

```cadenza
pure function complexCalculation(x: int, y: int, z: int) -> int {
    let sum = x + y
    let product = sum * z
    let final = product - x
    return final
}

pure function stringProcessing(first: string, last: string) -> string {
    let fullName = first + " " + last
    let length = 42  // In real implementation, would get string length
    let message = $"Name: {fullName}, Length: {length}"
    return message
}
```

## Data Types

### Integer Type

```cadenza
pure function integerExamples() -> int {
    let positive = 42
    let zero = 0
    let calculation = positive + zero * 2
    return calculation
}

pure function integerOperations(a: int, b: int) -> int {
    let sum = a + b
    let difference = a - b
    let product = a * b
    let quotient = a / b
    return sum + difference + product + quotient
}
```

### String Type

```cadenza
pure function stringExamples() -> string {
    let simple = "Hello, World!"
    let withEscapes = "Line 1\nLine 2\nLine 3"
    let withQuotes = "She said \"Hello\" to me"
    let pathExample = "C:\\Users\\Name\\Documents"
    return simple + withEscapes + withQuotes + pathExample
}

pure function stringConcatenation(prefix: string, name: string, suffix: string) -> string {
    let part1 = prefix + " "
    let part2 = part1 + name
    let result = part2 + " " + suffix
    return result
}
```

### Boolean Type

```cadenza
pure function booleanExamples(age: int, hasPermission: bool) -> bool {
    let isAdult = age >= 18
    let isMinor = age < 18
    let canAccess = isAdult && hasPermission
    let needsParent = isMinor || !hasPermission
    return canAccess || needsParent
}

pure function logicalOperations(a: bool, b: bool, c: bool) -> bool {
    let andResult = a && b
    let orResult = a || b
    let notResult = !a
    let complex = (a && b) || (!c && a)
    return andResult || orResult || notResult || complex
}
```

## Expressions and Operators

### Arithmetic Operators

```cadenza
pure function arithmeticDemo(x: int, y: int) -> int {
    let addition = x + y
    let subtraction = x - y
    let multiplication = x * y
    let division = x / y
    return addition + subtraction + multiplication + division
}

pure function operatorPrecedence(a: int, b: int, c: int) -> int {
    // Demonstrates operator precedence
    let result1 = a + b * c        // b * c first, then + a
    let result2 = (a + b) * c      // a + b first, then * c
    let result3 = a * b + c * a    // Multiplications first, then addition
    return result1 + result2 + result3
}
```

### Comparison Operators

```cadenza
pure function comparisonDemo(x: int, y: int) -> bool {
    let equal = x == y
    let notEqual = x != y
    let greater = x > y
    let less = x < y
    let greaterEqual = x >= y
    let lessEqual = x <= y
    
    // Complex comparison
    let inRange = x >= 0 && x <= 100
    return equal || notEqual || greater || less || greaterEqual || lessEqual || inRange
}
```

### Logical Operators

```cadenza
pure function logicalDemo(isAdmin: bool, isOwner: bool, hasPermission: bool) -> bool {
    // AND operator
    let adminWithPermission = isAdmin && hasPermission
    
    // OR operator
    let canAccess = isAdmin || isOwner
    
    // NOT operator
    let notAdmin = !isAdmin
    
    // Complex logical expression
    let finalAccess = (isAdmin || isOwner) && (hasPermission || isAdmin)
    
    return adminWithPermission || canAccess || notAdmin || finalAccess
}
```

### String Operations

```cadenza
pure function stringOperations(first: string, last: string) -> string {
    // String concatenation
    let fullName = first + " " + last
    
    // String comparison
    let isEmpty = first == ""
    let isNotEmpty = first != ""
    
    // Complex string building
    let prefix = "Mr. "
    let suffix = " Jr."
    let formal = prefix + fullName + suffix
    
    return formal
}
```

## Function Calls

### Simple Function Calls

```cadenza
pure function helper(x: int) -> int {
    return x * 2
}

pure function demonstrateCalls() -> int {
    // Simple function call
    let doubled = helper(21)
    
    // Nested function calls
    let quadrupled = helper(helper(21))
    
    // Function call in expression
    let result = helper(10) + helper(20)
    
    return doubled + quadrupled + result
}
```

### Function Calls with Multiple Parameters

```cadenza
pure function multiply(a: int, b: int) -> int {
    return a * b
}

pure function formatNumber(num: int, prefix: string) -> string {
    return prefix + num
}

function complexExample() -> string {
    // Multiple parameter calls
    let product = multiply(6, 7)
    let formatted = formatNumber(product, "Result: ")
    
    // Nested calls with multiple parameters
    let nested = formatNumber(multiply(3, 4), "Nested: ")
    
    return formatted + " | " + nested
}
```

### Function Call Chains

```cadenza
pure function increment(x: int) -> int {
    return x + 1
}

pure function double(x: int) -> int {
    return x * 2
}

function chainExample() -> int {
    let x = 5
    
    // Chain of function calls
    let step1 = increment(x)      // 6
    let step2 = double(step1)     // 12
    let step3 = increment(step2)  // 13
    
    // Inline chaining
    let chained = increment(double(increment(5)))  // Same result
    
    return step3 + chained
}
```

## Comments

### Single-line Comments

```cadenza
// This is a single-line comment
pure function commentExample(x: int) -> int {
    // Comments can explain what the code does
    let doubled = x * 2  // End-of-line comments are also allowed
    
    // You can have multiple comment lines
    // to explain complex logic
    return doubled
}

function moreComments() -> string {
    // TODO: This could be improved
    // FIXME: Handle edge cases
    // NOTE: This follows the standard pattern
    let result = "Hello"
    return result  // Return the greeting
}
```

### Commenting Best Practices

```cadenza
// Calculate compound interest using the standard formula
// Formula: A = P(1 + r/n)^(nt)
pure function calculateCompoundInterest(
    principal: int,    // Initial amount
    rate: int,         // Annual interest rate (as percentage)
    time: int          // Time in years
) -> int {
    // For simplicity, assume annual compounding (n=1)
    // and use integer math for demo purposes
    let rateDecimal = rate / 100
    let growth = 1 + rateDecimal
    
    // Apply compound interest formula (simplified)
    let amount = principal * growth * time  // Simplified calculation
    
    return amount
}

// Process user data with validation and formatting
function processUserData(name: string, age: int) -> string {
    // Validate input parameters
    // In a real implementation, these would be proper validations
    let validName = name != ""
    let validAge = age >= 0 && age <= 150
    
    // Format the output string
    // Include both name and age in a readable format
    if validName && validAge {
        return $"Valid user: {name}, age {age}"
    } else {
        return "Invalid user data"
    }
}
```

## Complete Example Program

Here's a complete example that demonstrates many of the basic syntax features:

```cadenza
// Mathematical utility functions
pure function add(a: int, b: int) -> int {
    return a + b
}

pure function multiply(a: int, b: int) -> int {
    return a * b
}

pure function isPositive(n: int) -> bool {
    return n > 0
}

// String utility functions
pure function formatName(first: string, last: string) -> string {
    return first + " " + last
}

pure function createGreeting(name: string, formal: bool) -> string {
    if formal {
        return "Dear " + name + ","
    } else {
        return "Hi " + name + "!"
    }
}

// Main demonstration function
function demonstrateBasicSyntax() -> string {
    // Integer variables and calculations
    let x = 10
    let y = 5
    let sum = add(x, y)
    let product = multiply(x, y)
    
    // Boolean logic
    let xIsPositive = isPositive(x)
    let yIsPositive = isPositive(y)
    let bothPositive = xIsPositive && yIsPositive
    
    // String operations
    let firstName = "John"
    let lastName = "Doe"
    let fullName = formatName(firstName, lastName)
    let greeting = createGreeting(fullName, true)
    
    // Complex expression
    let calculation = multiply(add(x, y), 2)  // (x + y) * 2
    
    // String interpolation with variables
    let summary = $"Summary: {fullName}, sum={sum}, product={product}, positive={bothPositive}, calc={calculation}"
    
    // Return final result
    return greeting + " " + summary
}
```

This example demonstrates:

- **Pure and regular functions**
- **Integer, string, and boolean types**
- **Variable declarations with let statements**
- **Arithmetic and logical operators**
- **Function calls with parameters**
- **Conditional logic with if/else**
- **String concatenation and interpolation**
- **Comments explaining the code**
- **Complex expressions and operator precedence**

When transpiled to C#, this produces clean, efficient code that follows C# best practices while maintaining the safety and explicitness of Cadenza.