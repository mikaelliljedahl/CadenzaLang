# Cadenza Specification Examples

This document provides practical examples of specification blocks across different domains and use cases, demonstrating how to effectively capture intent, business rules, and expected outcomes.

## Table of Contents

1. [Basic Examples](#basic-examples)
2. [E-Commerce Examples](#e-commerce-examples)
3. [Financial Services Examples](#financial-services-examples)
4. [Healthcare Examples](#healthcare-examples)
5. [Authentication & Security Examples](#authentication--security-examples)
6. [Data Processing Examples](#data-processing-examples)
7. [API & Integration Examples](#api--integration-examples)
8. [Module-Level Examples](#module-level-examples)

## Basic Examples

### Simple Validation Function

```cadenza
/*spec
intent: "Validate email address format for user registration"
rules:
  - "Email must contain exactly one @ symbol"
  - "Email must have valid domain extension"
  - "Email length must be between 5 and 254 characters"
  - "Local part cannot start or end with dots"
postconditions:
  - "Returns true for valid email addresses"
  - "Returns false for invalid email addresses"
spec*/
pure function validateEmail(email: string) -> bool {
    guard email != "" && email.length >= 5 && email.length <= 254 else {
        return false
    }
    
    let atCount = email.count("@")
    guard atCount == 1 else {
        return false
    }
    
    let parts = email.split("@")
    let localPart = parts[0]
    let domain = parts[1]
    
    guard !localPart.startsWith(".") && !localPart.endsWith(".") else {
        return false
    }
    
    guard domain.contains(".") else {
        return false
    }
    
    return true
}
```

### Mathematical Calculation

```cadenza
/*spec
intent: "Calculate compound interest for investment planning"
rules:
  - "Principal amount must be positive"
  - "Interest rate must be between 0 and 100 percent"
  - "Time period must be positive number of years"
  - "Compounding frequency must be at least 1 per year"
postconditions:
  - "Returns final amount after compound interest"
  - "Result is rounded to 2 decimal places for currency"
spec*/
pure function calculateCompoundInterest(
    principal: float, 
    rate: float, 
    time: float, 
    frequency: int
) -> Result<float, string> {
    guard principal > 0 else {
        return Error("Principal must be positive")
    }
    
    guard rate >= 0 && rate <= 100 else {
        return Error("Interest rate must be between 0 and 100")
    }
    
    guard time > 0 else {
        return Error("Time period must be positive")
    }
    
    guard frequency >= 1 else {
        return Error("Compounding frequency must be at least 1")
    }
    
    let decimalRate = rate / 100
    let amount = principal * Math.pow(1 + (decimalRate / frequency), frequency * time)
    let rounded = Math.round(amount * 100) / 100
    
    return Ok(rounded)
}
```

## E-Commerce Examples

### Product Inventory Management

```cadenza
/*spec
intent: "Update product inventory after successful order processing"
rules:
  - "Inventory quantity cannot go below zero"
  - "Reserved quantities must be properly released on order cancellation"
  - "Inventory updates must be atomic to prevent overselling"
  - "Low stock alerts triggered when quantity falls below threshold"
postconditions:
  - "Product quantity is reduced by ordered amount"
  - "Inventory transaction is logged for audit trail"
  - "Low stock notification sent if threshold reached"
  - "Product availability status updated if needed"
source_doc: "inventory/stock-management-procedures.md"
spec*/
function updateInventory(
    productId: ProductId, 
    quantityOrdered: int, 
    orderId: OrderId
) uses [Database, Logging, Notifications] -> Result<InventoryUpdate, InventoryError> {
    
    let product = getProduct(productId)?
    
    guard product.availableQuantity >= quantityOrdered else {
        return Error(InventoryError.InsufficientStock)
    }
    
    // Atomic inventory update
    let newQuantity = product.availableQuantity - quantityOrdered
    let updated = updateProductQuantity(productId, newQuantity)?
    
    // Log inventory transaction
    logInventoryTransaction(productId, orderId, quantityOrdered, "DEDUCTED")?
    
    // Check low stock threshold
    if newQuantity <= product.lowStockThreshold {
        sendLowStockAlert(productId, newQuantity)?
    }
    
    // Update availability status if out of stock
    if newQuantity == 0 {
        updateProductStatus(productId, ProductStatus.OutOfStock)?
    }
    
    return Ok(InventoryUpdate.new(productId, newQuantity, orderId))
}
```

### Shopping Cart Operations

```cadenza
/*spec
intent: "Add product to user's shopping cart with validation and pricing"
rules:
  - "Product must be available and in stock"
  - "Requested quantity must not exceed available inventory"
  - "User must be authenticated to add items to cart"
  - "Duplicate products should update quantity, not create new entries"
  - "Cart total must be recalculated after each addition"
postconditions:
  - "Product is added to cart with current pricing"
  - "Cart totals are updated to reflect new item"
  - "Cart modification timestamp is updated"
  - "User receives cart update confirmation"
spec*/
function addToCart(
    userId: UserId, 
    productId: ProductId, 
    quantity: int
) uses [Database, Pricing] -> Result<CartUpdate, CartError> {
    
    guard quantity > 0 else {
        return Error(CartError.InvalidQuantity)
    }
    
    let user = getUser(userId)?
    let product = getProduct(productId)?
    
    guard product.isAvailable && product.quantity >= quantity else {
        return Error(CartError.ProductUnavailable)
    }
    
    let cart = getOrCreateCart(userId)?
    let currentPrice = getCurrentPrice(productId)?
    
    // Check if product already in cart
    let existingItem = cart.findItem(productId)
    if existingItem.exists {
        let newQuantity = existingItem.quantity + quantity
        guard newQuantity <= product.quantity else {
            return Error(CartError.ExceedsAvailableStock)
        }
        updateCartItemQuantity(cart.id, productId, newQuantity)?
    } else {
        addCartItem(cart.id, productId, quantity, currentPrice)?
    }
    
    // Recalculate cart totals
    let updatedCart = recalculateCartTotals(cart.id)?
    
    return Ok(CartUpdate.new(updatedCart, productId, quantity))
}
```

## Financial Services Examples

### Loan Approval System

```cadenza
/*spec
intent: "Evaluate mortgage loan application using underwriting criteria"
rules:
  - "Credit score must be 620 or higher for conventional loans"
  - "Debt-to-income ratio cannot exceed 43%"
  - "Down payment must be at least 3% of home value"
  - "Employment history must show 2+ years stability"
  - "Property appraisal must meet or exceed loan amount"
  - "All income sources must be verified and documented"
postconditions:
  - "Application status set to approved, denied, or conditional"
  - "If approved: loan terms including rate and monthly payment"
  - "If denied: specific reason codes for regulatory compliance"
  - "All decisions logged for audit and fair lending review"
  - "Applicant notified of decision within regulatory timeframe"
source_doc: "underwriting/mortgage-criteria-2024.pdf"
spec*/
function evaluateMortgageApplication(application: MortgageApplication) 
    uses [CreditBureau, Database, Logging, Notifications] -> Result<LoanDecision, UnderwritingError> {
    
    // Pull credit report
    let creditReport = getCreditReport(application.ssn)?
    
    // Check credit score requirement
    guard creditReport.score >= 620 else {
        let decision = LoanDecision.denied("Credit score below minimum requirement")
        logUnderwritingDecision(application.id, decision)?
        return Ok(decision)
    }
    
    // Calculate debt-to-income ratio
    let monthlyIncome = application.grossMonthlyIncome
    let totalMonthlyDebt = application.existingDebt + application.estimatedPayment
    let dtiRatio = totalMonthlyDebt / monthlyIncome
    
    guard dtiRatio <= 0.43 else {
        let decision = LoanDecision.denied("Debt-to-income ratio exceeds maximum")
        logUnderwritingDecision(application.id, decision)?
        return Ok(decision)
    }
    
    // Verify down payment
    let downPaymentRatio = application.downPayment / application.homeValue
    guard downPaymentRatio >= 0.03 else {
        let decision = LoanDecision.denied("Down payment below minimum requirement")
        logUnderwritingDecision(application.id, decision)?
        return Ok(decision)
    }
    
    // Check employment stability
    guard application.employmentHistory.yearsAtCurrentJob >= 2 else {
        let decision = LoanDecision.conditional("Employment verification required")
        logUnderwritingDecision(application.id, decision)?
        return Ok(decision)
    }
    
    // All criteria met - approve loan
    let interestRate = calculateMortgageRate(creditReport.score, dtiRatio, downPaymentRatio)
    let monthlyPayment = calculateMonthlyPayment(application.loanAmount, interestRate, 360)
    
    let approval = LoanApproval.new(
        application.loanAmount, interestRate, monthlyPayment, 30
    )
    
    let decision = LoanDecision.approved(approval)
    logUnderwritingDecision(application.id, decision)?
    
    return Ok(decision)
}
```

### Fraud Detection

```cadenza
/*spec
intent: "Analyze transaction for potential fraud using behavioral patterns"
rules:
  - "Transactions over $500 require additional verification"
  - "Multiple transactions in different locations within 1 hour are suspicious"
  - "Transactions at unusual times for customer pattern trigger review"
  - "Velocity limits: max 5 transactions per hour, max $2000 per day"
  - "International transactions require customer notification"
postconditions:
  - "Risk score calculated between 0-100"
  - "High-risk transactions (>80) are blocked pending review"
  - "Medium-risk transactions (50-80) require additional authentication"
  - "All analysis results logged for machine learning model training"
  - "Customer notified of suspicious activity if applicable"
source_doc: "fraud/detection-algorithms-v3.2.md"
spec*/
function analyzeFraudRisk(transaction: Transaction) 
    uses [Database, MachineLearning, Notifications] -> Result<FraudAnalysis, FraudError> {
    
    let customer = getCustomer(transaction.customerId)?
    let recentTransactions = getRecentTransactions(customer.id, 24)?
    
    let riskScore = 0
    let riskFactors = []
    
    // High amount check
    if transaction.amount > 500 {
        riskScore += 25
        riskFactors.add("High transaction amount")
    }
    
    // Velocity analysis
    let recentCount = recentTransactions.filter(t => t.timestamp > now().minusHours(1)).count()
    if recentCount >= 5 {
        riskScore += 40
        riskFactors.add("High transaction velocity")
    }
    
    // Geographic analysis
    let recentLocations = recentTransactions.map(t => t.location).distinct()
    if recentLocations.count() > 3 {
        riskScore += 30
        riskFactors.add("Multiple locations")
    }
    
    // Time pattern analysis
    let customerPattern = getCustomerSpendingPattern(customer.id)?
    if !customerPattern.isTypicalTime(transaction.timestamp) {
        riskScore += 20
        riskFactors.add("Unusual transaction time")
    }
    
    // International transaction
    if transaction.isInternational {
        riskScore += 15
        riskFactors.add("International transaction")
        sendInternationalTransactionAlert(customer.id, transaction)?
    }
    
    // Machine learning model scoring
    let mlScore = getMachineLearningRiskScore(transaction, customer.profile)?
    riskScore += mlScore
    
    let finalScore = Math.min(riskScore, 100)
    let recommendation = if finalScore > 80 {
        FraudRecommendation.Block
    } else if finalScore > 50 {
        FraudRecommendation.RequireAuth
    } else {
        FraudRecommendation.Allow
    }
    
    let analysis = FraudAnalysis.new(finalScore, recommendation, riskFactors)
    
    // Log for model training
    logFraudAnalysis(transaction.id, analysis)?
    
    return Ok(analysis)
}
```

## Healthcare Examples

### Patient Data Management

```cadenza
/*spec
intent: "Update patient medical record with HIPAA compliance and audit trail"
rules:
  - "Only authorized healthcare providers can update records"
  - "All changes must be logged with timestamp and provider ID"
  - "Patient consent required for sharing data with third parties"
  - "Sensitive information requires additional encryption"
  - "Access attempts must be logged for security audit"
postconditions:
  - "Medical record updated with new information"
  - "Change history preserved for medical-legal requirements"
  - "Audit log entry created with update details"
  - "Patient privacy maintained throughout process"
source_doc: "compliance/hipaa-data-handling.md"
spec*/
function updatePatientRecord(
    patientId: PatientId,
    updates: MedicalRecordUpdate,
    providerId: ProviderId
) uses [Database, Encryption, Logging, HIPAA] -> Result<UpdateConfirmation, MedicalRecordError> {
    
    // Verify provider authorization
    let provider = getProvider(providerId)?
    guard provider.isAuthorized(PatientAccess.Write) else {
        logUnauthorizedAccess(providerId, patientId, "UPDATE_ATTEMPT")
        return Error(MedicalRecordError.Unauthorized)
    }
    
    // Get current patient record
    let patient = getPatient(patientId)?
    let currentRecord = patient.medicalRecord?
    
    // Log access attempt
    logPatientAccess(providerId, patientId, "UPDATE", updates.fields)?
    
    // Encrypt sensitive data
    let encryptedUpdates = encryptSensitiveFields(updates)?
    
    // Apply updates with versioning
    let updatedRecord = applyUpdates(currentRecord, encryptedUpdates)?
    let newVersion = saveRecordVersion(patientId, updatedRecord, providerId)?
    
    // Create audit trail entry
    let auditEntry = createAuditEntry(
        patientId, providerId, "RECORD_UPDATE", 
        updates.fields, newVersion.versionId
    )?
    
    // Update patient record
    updatePatientMedicalRecord(patientId, updatedRecord)?
    
    return Ok(UpdateConfirmation.new(newVersion.versionId, auditEntry.id))
}
```

## Authentication & Security Examples

### Multi-Factor Authentication

```cadenza
/*spec
intent: "Verify user identity using two-factor authentication"
rules:
  - "Primary factor (password) must be verified first"
  - "Secondary factor must be time-based (TOTP) or SMS code"
  - "TOTP codes are valid for 30-second windows with 1 window tolerance"
  - "SMS codes expire after 5 minutes"
  - "Maximum 3 attempts allowed before account lockout"
  - "Successful authentication resets failed attempt counter"
postconditions:
  - "Authentication token issued on successful verification"
  - "Failed attempts are logged for security monitoring"
  - "Account locked if maximum attempts exceeded"
  - "User notified of successful authentication via email"
spec*/
function verifyTwoFactorAuth(
    userId: UserId,
    password: string,
    mfaCode: string,
    mfaType: MFAType
) uses [Database, Encryption, SMS, Email, Logging] -> Result<AuthToken, AuthError> {
    
    let user = getUser(userId)?
    
    // Check account lock status
    guard !user.isLocked else {
        return Error(AuthError.AccountLocked)
    }
    
    // Verify primary factor (password)
    guard verifyPassword(password, user.passwordHash)? else {
        incrementFailedAttempts(userId)
        logFailedAuth(userId, "INVALID_PASSWORD")
        return Error(AuthError.InvalidCredentials)
    }
    
    // Verify secondary factor
    let mfaValid = match mfaType {
        MFAType.TOTP => verifyTOTPCode(user.totpSecret, mfaCode),
        MFAType.SMS => verifySMSCode(user.phoneNumber, mfaCode)
    }
    
    guard mfaValid? else {
        incrementFailedAttempts(userId)
        
        if user.failedAttempts >= 3 {
            lockAccount(userId)?
            logSecurityEvent(userId, "ACCOUNT_LOCKED_MFA_FAILURES")
            return Error(AuthError.AccountLocked)
        }
        
        logFailedAuth(userId, "INVALID_MFA_CODE")
        return Error(AuthError.InvalidMFACode)
    }
    
    // Successful authentication
    resetFailedAttempts(userId)?
    let token = generateAuthToken(userId)?
    
    logSuccessfulAuth(userId, "TWO_FACTOR_SUCCESS")
    sendAuthNotificationEmail(user.email)?
    
    return Ok(token)
}
```

## Data Processing Examples

### CSV Data Import

```cadenza
/*spec
intent: "Import customer data from CSV file with validation and error handling"
rules:
  - "CSV must have header row with required columns: name, email, phone"
  - "Email addresses must be valid format and unique"
  - "Phone numbers must be valid format for specified country"
  - "Names cannot be empty or contain special characters"
  - "Maximum 10,000 records per import batch"
  - "Invalid records are skipped but logged for review"
postconditions:
  - "Valid records are imported into customer database"
  - "Import summary shows success/failure counts"
  - "Error report generated for invalid records"
  - "Duplicate records are flagged but not imported"
source_doc: "data/import-specifications.md"
spec*/
function importCustomerCSV(filePath: string) 
    uses [FileSystem, Database, Logging] -> Result<ImportSummary, ImportError> {
    
    // Read and parse CSV file
    let csvContent = readFile(filePath)?
    let lines = csvContent.split("\n")
    
    guard lines.length > 1 else {
        return Error(ImportError.EmptyFile)
    }
    
    guard lines.length <= 10001 else { // Header + 10,000 data rows
        return Error(ImportError.TooManyRecords)
    }
    
    // Parse header
    let header = lines[0].split(",")
    let requiredColumns = ["name", "email", "phone"]
    
    for column in requiredColumns {
        guard header.contains(column) else {
            return Error(ImportError.MissingRequiredColumn(column))
        }
    }
    
    let importResults = ImportResults.new()
    let errorRecords = []
    
    // Process data rows
    for i in 1..lines.length {
        let line = lines[i].trim()
        if line == "" { continue }
        
        let fields = line.split(",")
        let record = parseCustomerRecord(header, fields)
        
        // Validate record
        let validation = validateCustomerRecord(record)
        if validation.isValid {
            // Check for duplicates
            let existing = findExistingCustomer(record.email)
            if existing.exists {
                importResults.duplicateCount++
                errorRecords.add(ErrorRecord.new(i, record, "Duplicate email"))
            } else {
                // Import valid record
                let customer = createCustomer(record)?
                importResults.successCount++
                importResults.importedIds.add(customer.id)
            }
        } else {
            importResults.errorCount++
            errorRecords.add(ErrorRecord.new(i, record, validation.errors))
        }
    }
    
    // Generate error report
    if errorRecords.length > 0 {
        generateErrorReport(filePath, errorRecords)?
    }
    
    let summary = ImportSummary.new(
        importResults.successCount,
        importResults.errorCount,
        importResults.duplicateCount,
        errorRecords.length
    )
    
    logImportCompletion(filePath, summary)?
    
    return Ok(summary)
}
```

## API & Integration Examples

### External API Integration

```cadenza
/*spec
intent: "Fetch weather data from external API with caching and error handling"
rules:
  - "API responses must be cached for 10 minutes to reduce API calls"
  - "Invalid coordinates (lat/lon) must be rejected before API call"
  - "API failures should return cached data if available"
  - "Rate limiting enforced: max 100 requests per hour"
  - "API timeout set to 5 seconds with retry logic"
postconditions:
  - "Current weather data returned for valid coordinates"
  - "Response includes temperature, conditions, and timestamp"
  - "Cache updated with fresh data on successful API call"
  - "API usage tracked for billing and monitoring"
source_doc: "integrations/weather-api-specs.md"
spec*/
function getWeatherData(latitude: float, longitude: float) 
    uses [Network, Cache, Logging] -> Result<WeatherData, WeatherError> {
    
    // Validate coordinates
    guard latitude >= -90 && latitude <= 90 else {
        return Error(WeatherError.InvalidLatitude)
    }
    
    guard longitude >= -180 && longitude <= 180 else {
        return Error(WeatherError.InvalidLongitude)
    }
    
    let cacheKey = $"weather:{latitude}:{longitude}"
    
    // Check cache first
    let cachedData = getFromCache(cacheKey)
    if cachedData.exists && !cachedData.isExpired(600) { // 10 minutes
        return Ok(cachedData.value)
    }
    
    // Check rate limiting
    let rateLimitKey = "weather_api_calls"
    let currentCalls = getRateLimitCount(rateLimitKey, 3600) // 1 hour window
    
    guard currentCalls < 100 else {
        // Try to return stale cache data if available
        if cachedData.exists {
            logWeatherEvent("RATE_LIMITED_USING_CACHE", latitude, longitude)
            return Ok(cachedData.value)
        }
        return Error(WeatherError.RateLimitExceeded)
    }
    
    // Make API call with retry logic
    let apiResponse = callWeatherAPI(latitude, longitude, retries: 2, timeout: 5000)?
    
    // Parse and validate response
    let weatherData = parseWeatherResponse(apiResponse)?
    
    // Update cache
    saveToCache(cacheKey, weatherData, ttl: 600)?
    
    // Track API usage
    incrementRateLimitCount(rateLimitKey, 3600)?
    logAPICall("weather", "success", latitude, longitude)?
    
    return Ok(weatherData)
}
```

## Module-Level Examples

### Complete E-Commerce Order Module

```cadenza
/*spec
intent: "Complete order processing module for e-commerce platform"
rules:
  - "All order operations must maintain data consistency"
  - "Inventory must be managed atomically to prevent overselling"
  - "Payment processing integrates with multiple providers"
  - "Order fulfillment follows configurable business rules"
  - "All operations are logged for business analytics"
  - "Failed operations must be properly rolled back"
postconditions:
  - "Reliable order processing from cart to fulfillment"
  - "Accurate inventory tracking and management"
  - "Secure payment processing with fraud detection"
  - "Comprehensive audit trail for all operations"
source_doc: "business/order-processing-requirements.md"
spec*/
module OrderProcessing {
    
    /*spec
    intent: "Create new order from customer shopping cart"
    rules:
      - "All cart items must be available and in stock"
      - "Customer must have valid payment method"
      - "Shipping address must be validated"
      - "Order total must match cart total plus taxes and shipping"
    postconditions:
      - "Order created with unique order number"
      - "Inventory reserved for order items"
      - "Customer receives order confirmation"
    spec*/
    function createOrder(customerId: CustomerId, cartId: CartId) 
        uses [Database, Inventory, Tax, Shipping] -> Result<Order, OrderError> {
        // Implementation
    }
    
    /*spec
    intent: "Process payment for confirmed order"
    rules:
      - "Payment amount must match order total exactly"
      - "Payment method must be valid and authorized"
      - "Fraud detection must be run on all payments"
      - "Failed payments must release reserved inventory"
    postconditions:
      - "Payment processed and funds captured"
      - "Order status updated to paid"
      - "Fulfillment process initiated"
    spec*/
    function processPayment(orderId: OrderId, paymentDetails: PaymentDetails) 
        uses [Payment, Fraud, Database, Logging] -> Result<PaymentResult, PaymentError> {
        // Implementation
    }
    
    /*spec
    intent: "Fulfill order by generating shipping labels and updating status"
    rules:
      - "Order must be paid and not already fulfilled"
      - "Items must be picked from inventory"
      - "Shipping carrier must be selected based on customer preference"
      - "Tracking information must be generated"
    postconditions:
      - "Shipping label created and printed"
      - "Tracking number assigned to order"
      - "Customer notified with tracking information"
      - "Order status updated to shipped"
    spec*/
    function fulfillOrder(orderId: OrderId) 
        uses [Inventory, Shipping, Email, Database] -> Result<FulfillmentResult, FulfillmentError> {
        // Implementation
    }
    
    export {createOrder, processPayment, fulfillOrder}
}
```

These examples demonstrate how specification blocks capture the complete context around functions and modules, providing LLMs and human developers with comprehensive understanding of intent, constraints, and expected outcomes. Each specification serves as both documentation and a contract that can be verified against the implementation.