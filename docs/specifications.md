# Cadenza Specification Blocks Guide

## Overview

Specification blocks solve a fundamental problem in software development: **the loss of context between intent and implementation**. While we carefully version control our code, we often lose the reasoning, business requirements, and decision-making process that led to that code.

Cadenza's specification blocks create an **atomic link** between the "why" (specification) and the "how" (implementation), ensuring that context is preserved throughout the software lifecycle.

## The Problem Statement

### Traditional Development Workflow
```
Business Requirements → Specification Document → Code → Binary
         ↓                        ↓              ↓        ↓
   Version Controlled?           Sometimes       Yes      No
```

**What happens:** The specification often becomes stale, disconnected from the actual implementation. When requirements change or bugs need fixing, developers must reverse-engineer the intent from the code.

**Impact on LLMs:** AI assistants lack the context of *why* code exists, making maintenance, debugging, and enhancement significantly harder.

### Cadenza's Solution
```
Business Requirements → Specification Block + Code → Binary
         ↓                        ↓                    ↓
   Version Controlled?           YES (atomic)         No
```

**What happens:** Specification and code are versioned together as a single unit. Context is never lost.

**Impact on LLMs:** AI assistants have complete context for every function, enabling better reasoning about changes, testing, and maintenance.

## Specification Block Syntax

### Basic Structure

```cadenza
/*spec
intent: "Required: What this function does and why it exists"
rules:
  - "Optional: Business rule or constraint"
  - "Additional constraints as needed"
postconditions:
  - "Optional: Expected outcomes or state changes"
  - "What should be true after successful execution"
source_doc: "Optional: Reference to external documentation"
spec*/
function functionName(parameters) -> ReturnType {
    // Implementation follows specification above
}
```

### Field Descriptions

#### `intent` (Required)
The natural language description of what the function does and **why it exists** in the business context.

**Best Practices:**
- Start with an active verb
- Include business context, not just technical details
- Be specific about the purpose
- Mention any important constraints or assumptions

**Examples:**
```cadenza
intent: "Transfer funds between user accounts atomically while maintaining audit trail"
intent: "Validate user email format according to RFC 5322 for registration system"
intent: "Calculate shipping cost based on weight, distance, and delivery speed preferences"
```

#### `rules` (Optional)
A list of business rules, constraints, validations, or requirements that the implementation must satisfy.

**Best Practices:**
- Use actionable, testable statements
- Focus on business logic, not implementation details
- Include edge cases and error conditions
- Be specific about data constraints

**Examples:**
```cadenza
rules:
  - "Transfer amount must be positive and non-zero"
  - "Source account must have sufficient available balance"
  - "Both accounts must belong to the same customer"
  - "Daily transfer limit cannot be exceeded"
  - "Accounts must be active and not frozen"
```

#### `postconditions` (Optional)
Expected outcomes or state changes that should be true after successful execution.

**Best Practices:**
- Describe the end state, not the process
- Include side effects and state changes
- Mention what gets logged or recorded
- Consider both success and failure scenarios

**Examples:**
```cadenza
postconditions:
  - "Source account balance is reduced by transfer amount"
  - "Destination account balance is increased by transfer amount"
  - "Transaction record is created with unique ID and timestamp"
  - "Both accounts' transaction histories are updated"
  - "Customer receives transfer confirmation via preferred channel"
```

#### `source_doc` (Optional)
Reference to external documentation, requirements documents, or design specifications.

**Examples:**
```cadenza
source_doc: "requirements/banking-transfers-v2.3.md"
source_doc: "https://wiki.company.com/payment-processing"
source_doc: "design/user-authentication-flow.pdf"
```

## Specification Levels

### Function-Level Specifications

Most common usage - specify individual function behavior:

```cadenza
/*spec
intent: "Authenticate user credentials and create secure session"
rules:
  - "Password must be verified using bcrypt hashing"
  - "Account must not be locked or suspended"
  - "Rate limiting prevents brute force attacks"
  - "Session token must be cryptographically secure"
postconditions:
  - "Valid session token is returned on success"
  - "Login attempt is logged for security audit"
  - "User's last login timestamp is updated"
  - "Failed attempts increment security counter"
source_doc: "security/authentication-requirements.md"
spec*/
function authenticateUser(email: string, password: string) 
    uses [Database, Logging, Security] -> Result<SessionToken, AuthError> {
    
    // Rate limiting check
    guard !isRateLimited(email) else {
        return Error(AuthError.TooManyAttempts)
    }
    
    // Verify credentials
    let user = findUserByEmail(email)?
    guard user.isActive else {
        return Error(AuthError.AccountDisabled)
    }
    
    guard verifyPassword(password, user.passwordHash)? else {
        logFailedLogin(email)
        return Error(AuthError.InvalidCredentials)
    }
    
    // Create session
    let token = generateSecureToken()?
    let session = createSession(user.id, token)?
    
    logSuccessfulLogin(user.id)
    updateLastLogin(user.id)
    
    return Ok(token)
}
```

### Module-Level Specifications

Define the purpose and scope of entire modules:

```cadenza
/*spec
intent: "Comprehensive user management system for e-commerce platform"
rules:
  - "All user data operations must be GDPR compliant"
  - "Password handling follows OWASP security guidelines"
  - "User profile changes require email verification"
  - "Administrative operations require elevated permissions"
  - "All operations are logged for compliance audit"
postconditions:
  - "Secure user lifecycle management"
  - "Compliant data handling and privacy protection"
  - "Comprehensive audit trail for regulatory compliance"
source_doc: "business/user-management-requirements.md"
spec*/
module UserManagement {
    /*spec
    intent: "Create new user account with email verification"
    rules:
      - "Email must be unique in system"
      - "Password must meet security policy requirements"
      - "Email verification required before account activation"
    postconditions:
      - "User account created but inactive until verified"
      - "Verification email sent to provided address"
    spec*/
    function createUser(email: string, password: string) 
        uses [Database, Email, Logging] -> Result<UserId, UserError> {
        // Implementation
    }
    
    /*spec
    intent: "Update user profile information with validation"
    rules:
      - "Only account owner or admin can update profile"
      - "Email changes require re-verification"
      - "Profile data must pass validation rules"
    postconditions:
      - "Profile updated with new information"
      - "Change history recorded for audit"
    spec*/
    function updateProfile(userId: UserId, updates: ProfileData) 
        uses [Database, Logging] -> Result<Unit, UserError> {
        // Implementation
    }
    
    export {createUser, updateProfile}
}
```

## Specification-Driven Development Workflow

### 1. Specification-First Development

Start with the specification, then implement:

```cadenza
/*spec
intent: "Process customer order with inventory validation and payment"
rules:
  - "All ordered items must be in stock"
  - "Payment must be processed before inventory allocation"
  - "Failed payment must not reserve inventory"
  - "Successful orders must trigger shipping workflow"
  - "Order total must include taxes and shipping costs"
postconditions:
  - "Order is confirmed and assigned tracking number"
  - "Inventory is allocated to the order"
  - "Payment is captured and recorded"
  - "Customer receives order confirmation email"
  - "Shipping label is generated and printed"
source_doc: "business/order-processing-workflow.md"
spec*/
function processOrder(order: OrderDetails) 
    uses [Database, Payment, Email, Shipping] -> Result<OrderConfirmation, OrderError> {
    
    // TODO: Implement based on specification above
    // 1. Validate inventory availability
    // 2. Process payment
    // 3. Allocate inventory
    // 4. Create shipping record
    // 5. Send confirmation
    
    return Error(OrderError.NotImplemented)
}
```

### 2. Test Generation from Specifications

Specifications provide clear criteria for test generation:

```cadenza
// Test cases derived from specification rules
function testProcessOrder() {
    // Test rule: "All ordered items must be in stock"
    let outOfStockOrder = createOrderWithUnavailableItem()
    let result = processOrder(outOfStockOrder)
    assert(result.IsError && result.Error == OrderError.InsufficientInventory)
    
    // Test rule: "Payment must be processed before inventory allocation"
    let invalidPaymentOrder = createOrderWithInvalidPayment()
    let result2 = processOrder(invalidPaymentOrder)
    assert(result2.IsError && result2.Error == OrderError.PaymentFailed)
    assert(!inventoryIsAllocated(invalidPaymentOrder.id))
    
    // Test postcondition: "Customer receives order confirmation email"
    let validOrder = createValidOrder()
    let result3 = processOrder(validOrder)
    assert(result3.IsOk)
    assert(emailWasSent(validOrder.customerEmail))
}
```

### 3. Consistency Validation

Use specifications to validate implementation consistency:

```cadenza
/*spec
intent: "Calculate user discount based on loyalty tier and purchase history"
rules:
  - "Platinum members get 15% discount"
  - "Gold members get 10% discount"  
  - "Silver members get 5% discount"
  - "New members get no discount"
  - "Discount cannot exceed 20% even with promotions"
postconditions:
  - "Discount percentage is returned as decimal (0.0 to 0.20)"
  - "Discount calculation is logged for audit"
spec*/
function calculateDiscount(user: User) -> Result<float, DiscountError> {
    // Implementation must match specification rules exactly
    let baseDiscount = match user.loyaltyTier {
        LoyaltyTier.Platinum => 0.15,
        LoyaltyTier.Gold => 0.10,
        LoyaltyTier.Silver => 0.05,
        LoyaltyTier.New => 0.0
    }
    
    // Ensure postcondition: never exceed 20%
    let finalDiscount = Math.min(baseDiscount, 0.20)
    
    logDiscountCalculation(user.id, finalDiscount)
    
    return Ok(finalDiscount)
}
```

## Real-World Examples

### E-Commerce Example

```cadenza
/*spec
intent: "Process product return request with validation and refund calculation"
rules:
  - "Returns only allowed within 30 days of delivery"
  - "Product must be in original condition (not damaged/used)"
  - "Digital products cannot be returned"
  - "Refund amount includes original product price minus restocking fee"
  - "Shipping costs are not refundable unless item was defective"
  - "Customer must provide return reason"
postconditions:
  - "Return request is created with unique RMA number"
  - "Refund amount is calculated and held for approval"
  - "Customer receives return shipping instructions"
  - "Inventory is flagged for inspection upon receipt"
  - "Original order status is updated to 'return-pending'"
source_doc: "policy/return-refund-policy.md"
spec*/
function processReturnRequest(
    orderId: OrderId, 
    productId: ProductId, 
    reason: ReturnReason
) uses [Database, Email, Inventory] -> Result<ReturnAuthorization, ReturnError> {
    
    // Validate return eligibility
    let order = getOrder(orderId)?
    let product = getProduct(productId)?
    
    guard order.deliveryDate.isWithinDays(30) else {
        return Error(ReturnError.PastReturnWindow)
    }
    
    guard !product.isDigital else {
        return Error(ReturnError.DigitalProductNotReturnable)
    }
    
    // Calculate refund
    let restockingFee = product.category.restockingFee
    let refundAmount = product.price - restockingFee
    
    // Create return authorization
    let rmaNumber = generateRMANumber()
    let returnAuth = createReturnAuthorization(
        rmaNumber, orderId, productId, refundAmount, reason
    )?
    
    // Send instructions to customer
    sendReturnInstructions(order.customerEmail, returnAuth)?
    
    // Update order status
    updateOrderStatus(orderId, OrderStatus.ReturnPending)?
    
    return Ok(returnAuth)
}
```

### Financial Services Example

```cadenza
/*spec
intent: "Calculate loan eligibility and terms based on applicant financial profile"
rules:
  - "Minimum credit score of 650 required for approval"
  - "Debt-to-income ratio must be below 40%"
  - "Minimum annual income of $30,000 required"
  - "Employment history must show 2+ years stability"
  - "Maximum loan amount is 5x annual income"
  - "Interest rate varies by credit score and loan term"
  - "All calculations must comply with federal lending regulations"
postconditions:
  - "Loan eligibility decision (approved/denied/conditional)"
  - "If approved: loan amount, interest rate, and monthly payment"
  - "If denied: specific reasons for denial"
  - "Credit check is recorded for applicant credit history"
  - "Application decision is logged for regulatory compliance"
source_doc: "compliance/lending-criteria-v3.1.md"
spec*/
function assessLoanEligibility(application: LoanApplication) 
    uses [CreditBureau, Database, Logging] -> Result<LoanDecision, AssessmentError> {
    
    // Pull credit report
    let creditReport = getCreditReport(application.ssn)?
    
    // Apply eligibility rules
    guard creditReport.score >= 650 else {
        logDecision(application.id, "DENIED", "Credit score below minimum")
        return Ok(LoanDecision.denied("Credit score below minimum requirement"))
    }
    
    let debtToIncomeRatio = application.monthlyDebt / application.monthlyIncome
    guard debtToIncomeRatio < 0.40 else {
        logDecision(application.id, "DENIED", "High debt-to-income ratio")
        return Ok(LoanDecision.denied("Debt-to-income ratio exceeds maximum"))
    }
    
    guard application.annualIncome >= 30000 else {
        logDecision(application.id, "DENIED", "Income below minimum")
        return Ok(LoanDecision.denied("Annual income below minimum requirement"))
    }
    
    // Calculate loan terms
    let maxLoanAmount = application.annualIncome * 5
    let requestedAmount = Math.min(application.requestedAmount, maxLoanAmount)
    let interestRate = calculateInterestRate(creditReport.score, application.term)
    let monthlyPayment = calculatePayment(requestedAmount, interestRate, application.term)
    
    let approval = LoanApproval.new(
        requestedAmount, interestRate, monthlyPayment, application.term
    )
    
    logDecision(application.id, "APPROVED", $"Amount: {requestedAmount}, Rate: {interestRate}")
    
    return Ok(LoanDecision.approved(approval))
}
```

### Healthcare Example

```cadenza
/*spec
intent: "Schedule patient appointment with conflict resolution and insurance verification"
rules:
  - "Appointments must be during provider's available hours"
  - "No double-booking of providers or examination rooms"
  - "Emergency appointments take priority over routine visits"
  - "Insurance coverage must be verified before scheduling"
  - "Patient must confirm appointment 24 hours in advance"
  - "Cancelled appointments must free up the time slot"
  - "HIPAA compliance required for all patient data handling"
postconditions:
  - "Appointment is confirmed with unique confirmation number"
  - "Provider calendar is updated with appointment details"
  - "Patient receives appointment confirmation and instructions"
  - "Insurance pre-authorization is initiated if required"
  - "Reminder notifications are scheduled"
  - "Examination room is reserved for appointment duration"
source_doc: "compliance/hipaa-scheduling-requirements.md"
spec*/
function scheduleAppointment(
    patientId: PatientId,
    providerId: ProviderId,
    appointmentType: AppointmentType,
    requestedTime: DateTime
) uses [Database, Insurance, Email, HIPAA] -> Result<AppointmentConfirmation, SchedulingError> {
    
    // Verify patient and provider exist
    let patient = getPatient(patientId)?
    let provider = getProvider(providerId)?
    
    // Check provider availability
    guard provider.isAvailable(requestedTime, appointmentType.duration) else {
        return Error(SchedulingError.ProviderUnavailable)
    }
    
    // Verify insurance coverage
    let insuranceVerification = verifyInsurance(
        patient.insurance, appointmentType.procedureCode
    )?
    
    guard insuranceVerification.isCovered else {
        return Error(SchedulingError.InsuranceNotCovered)
    }
    
    // Reserve time slot and room
    let room = reserveExaminationRoom(requestedTime, appointmentType.duration)?
    let timeSlot = reserveTimeSlot(providerId, requestedTime, appointmentType.duration)?
    
    // Create appointment
    let confirmationNumber = generateConfirmationNumber()
    let appointment = createAppointment(
        confirmationNumber, patientId, providerId, room.id, 
        requestedTime, appointmentType
    )?
    
    // Schedule notifications
    scheduleReminderNotifications(appointment)?
    
    // Send confirmation to patient (HIPAA compliant)
    sendAppointmentConfirmation(patient.email, appointment)?
    
    // Initiate pre-authorization if needed
    if insuranceVerification.requiresPreAuth {
        initiatePreAuthorization(appointment, insuranceVerification)?
    }
    
    logSchedulingActivity(patientId, providerId, "SCHEDULED", appointment.id)
    
    return Ok(AppointmentConfirmation.new(confirmationNumber, appointment))
}
```

## Working Example

For a complete working example with multiple specification blocks demonstrating real-world usage, see:
- **`examples/specification_example.cdz`** - Shows loyalty discount calculation, payment processing, user validation, and reporting functions with comprehensive specification blocks

This example demonstrates:
- **Business Logic Preservation**: Loyalty program rules with tier-based discounts
- **Security Requirements**: Payment processing with fraud detection requirements
- **Compliance Documentation**: User data validation with privacy protection rules
- **Audit Trail Requirements**: Activity reporting with proper anonymization

## Benefits for LLM Development

### 1. Enhanced Context Understanding

**Without Specifications:**
```cadenza
function processPayment(amount: float, cardNumber: string) -> bool {
    // What business rules apply?
    // What error conditions should be handled?
    // What happens after payment succeeds?
    return true
}
```

**With Specifications:**
```cadenza
/*spec
intent: "Process credit card payment with fraud detection and compliance"
rules:
  - "Payment amount must be positive and within card limit"
  - "Card number must pass Luhn algorithm validation"
  - "Transaction must be screened for fraud patterns"
  - "PCI DSS compliance required for card data handling"
postconditions:
  - "Payment is authorized and funds are reserved"
  - "Transaction record is created with unique ID"
  - "Customer receives payment confirmation"
  - "Merchant account is credited within 2 business days"
spec*/
function processPayment(amount: float, cardNumber: string) -> Result<PaymentResult, PaymentError> {
    // LLM now understands complete context
}
```

### 2. Intelligent Test Generation

LLMs can generate comprehensive tests directly from specifications:

```cadenza
// LLM can automatically generate:
function testProcessPayment() {
    // From rule: "Payment amount must be positive"
    assert(processPayment(-10.0, validCard).IsError)
    
    // From rule: "Card number must pass Luhn algorithm"
    assert(processPayment(100.0, invalidCard).IsError)
    
    // From postcondition: "Transaction record is created"
    let result = processPayment(100.0, validCard)
    assert(result.IsOk)
    assert(transactionExists(result.Value.transactionId))
}
```

### 3. Consistent Change Management

When requirements change, LLMs can ensure implementation stays aligned:

```cadenza
/*spec
intent: "Calculate shipping cost with new express delivery option"
rules:
  - "Standard shipping: $5 base + $0.50 per pound"
  - "Express shipping: $15 base + $1.00 per pound"
  - "Overnight shipping: $25 base + $2.00 per pound"  // NEW REQUIREMENT
  - "Free shipping for orders over $50 (standard only)"
postconditions:
  - "Shipping cost calculated based on weight and delivery speed"
  - "Available delivery options are returned with costs"
spec*/
function calculateShipping(weight: float, orderTotal: float) 
    -> Result<ShippingOptions, ShippingError> {
    // LLM can update implementation to match new specification
}
```

## Best Practices

### 1. Write Specifications First

Before writing any code, start with the specification:

```cadenza
/*spec
intent: "Your intent here"
rules:
  - "Rule 1"
  - "Rule 2"
postconditions:
  - "Expected outcome 1"
  - "Expected outcome 2"
spec*/
function myFunction() -> ResultType {
    // TODO: Implement according to specification above
    return Error("Not implemented")
}
```

### 2. Keep Specifications Current

Update specifications whenever requirements change:

```cadenza
/*spec
intent: "Process user login with new 2FA requirement"  // Updated intent
rules:
  - "Username and password must be valid"
  - "Account must not be locked"
  - "Two-factor authentication required for all users"  // NEW RULE
postconditions:
  - "Session token created on successful authentication"
  - "2FA challenge sent via user's preferred method"  // NEW POSTCONDITION
spec*/
function loginUser(username: string, password: string) 
    uses [Database, SMS, Email] -> Result<LoginResult, AuthError> {
    // Update implementation to match updated specification
}
```

### 3. Be Specific and Testable

Use concrete, measurable criteria:

**Good:**
```cadenza
rules:
  - "Password must be at least 8 characters long"
  - "Password must contain at least one uppercase letter"
  - "Password must contain at least one number"
  - "Password must contain at least one special character"
```

**Avoid:**
```cadenza
rules:
  - "Password must be secure"  // Too vague
  - "Password should be good"  // Not testable
```

### 4. Focus on Business Value

Capture the "why" not just the "what":

**Good:**
```cadenza
intent: "Send order confirmation email to improve customer confidence and provide shipment tracking information"
```

**Avoid:**
```cadenza
intent: "Send an email"  // Lacks business context
```

### 5. Use Consistent Language

Establish a common vocabulary for specifications across your project:

```cadenza
// Use consistent terms throughout specifications
rules:
  - "User must be authenticated"  // Not "logged in" or "signed in"
  - "Account must be active"      // Not "enabled" or "valid"
  - "Payment must be authorized"  // Not "approved" or "processed"
```

## Integration with Development Tools

### Code Generation

Specifications can drive code generation for boilerplate and validation:

```cadenza
/*spec
intent: "Validate user registration form data"
rules:
  - "Email must be valid format and unique"
  - "Password must meet security requirements"
  - "Terms of service must be accepted"
postconditions:
  - "Validation errors are collected and returned"
  - "Valid data is normalized and ready for storage"
spec*/
function validateRegistration(form: RegistrationForm) -> Result<ValidatedData, ValidationErrors> {
    // Code generator can create validation boilerplate from specification
}
```

### Documentation Generation

Rich documentation is automatically generated from specifications:

```csharp
/// <summary>
/// Process credit card payment with fraud detection and compliance
/// 
/// Business Rules:
/// - Payment amount must be positive and within card limit
/// - Card number must pass Luhn algorithm validation  
/// - Transaction must be screened for fraud patterns
/// - PCI DSS compliance required for card data handling
///
/// Expected Outcomes:
/// - Payment is authorized and funds are reserved
/// - Transaction record is created with unique ID
/// - Customer receives payment confirmation
/// - Merchant account is credited within 2 business days
/// </summary>
public static Result<PaymentResult, PaymentError> processPayment(float amount, string cardNumber)
```

### Static Analysis

Linting rules can verify specification-code consistency:

```bash
cadenzac lint --check-specs
# Warns if implementation doesn't handle all specified error conditions
# Warns if postconditions aren't being met
# Suggests tests based on specification rules
```

## Conclusion

Specification blocks transform Cadenza from a simple transpilation language into a **context-preserving development platform**. By atomically linking intent with implementation, we solve the fundamental problem of lost context that plagues traditional software development.

For LLM-assisted development, this means:
- **Complete Context**: AI assistants understand both what and why
- **Intelligent Assistance**: Better code generation, testing, and refactoring
- **Consistency Checking**: Automated verification that code matches intent
- **Living Documentation**: Self-updating documentation that never goes stale

The result is more maintainable, understandable, and reliable software that preserves the human reasoning that created it.