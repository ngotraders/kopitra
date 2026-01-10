# Kopitra Backend Implementation Guide for AI Agents

This document provides detailed guidance for AI agents implementing features in the Kopitra FX copy trading platform backend (Azure Functions + .NET 8.0 + EventFlow).

---

## 📋 Overview

**Technology Stack:**
- Azure Functions v4 (Worker SDK)
- .NET 8.0
- Entity Framework Core 8.0
- EventFlow 1.2.0 (event sourcing)
- SQL Server / SQLite (testing)

**Architecture Pattern:**
- Event Sourcing with EventFlow
- CQRS (Commands / Queries)
- Aggregate-driven domain model
- SQL-based message queues

---

## 🎯 AI Tasks and Responsibilities

### When Implementing New Features

1. **Always start by updating SYSTEM_DESIGN.md**
   - Add API endpoint specification (Section 6)
   - Add data model if needed (Section 5)
   - Document permission requirements
   - Include request/response examples

2. **Implement in this order:**
   - Domain events
   - Aggregate (entity)
   - Commands and command handlers
   - API endpoints (Functions)
   - Tests (Unit + Integration)

3. **Verify changes:**
   - Run migrations: `dotnet ef database update`
   - Run tests: `dotnet test`
   - Test API locally: `func start`

---

## 🏗️ File Organization and Code Placement

### Domain Layer (`Domain/`)

#### Events (`Domain/Events/`)
```
Domain/Events/
├── Signal/
│   ├── SignalCreatedEvent.cs
│   ├── SignalModifiedEvent.cs
│   └── SignalClosedEvent.cs
├── Order/
│   ├── OrderExecutedEvent.cs
│   ├── OrderModifiedEvent.cs
│   └── OrderClosedEvent.cs
└── Subscription/
    ├── SubscriptionStartedEvent.cs
    └── SubscriptionEndedEvent.cs
```

**Naming Convention:**
- Suffix with `Event`
- Use PastTense (SignalCreated, not SignalCreating)
- One file per event class

#### Aggregates (`Domain/Entities/`)
```
Domain/Entities/
├── Signal.cs              # Aggregate root
├── SignalId.cs            # Value object (aggregate ID)
├── Order.cs
├── OrderId.cs
├── Subscription.cs
└── SubscriptionId.cs
```

**Naming Convention:**
- One aggregate per file
- ID class in same folder (or separate ValueObjects folder)

#### Value Objects (`Domain/ValueObjects/`)
```
Domain/ValueObjects/
├── SignalId.cs
├── OrderId.cs
├── UserId.cs
├── Pair.cs
├── PositionSize.cs
└── FundingStrategy.cs
```

#### Exceptions (`Domain/Exceptions/`)
```
Domain/Exceptions/
├── SignalNotFoundException.cs
├── InvalidPositionSizeException.cs
├── SubscriptionAlreadyActiveException.cs
└── InsufficientAccountBalanceException.cs
```

**Convention:**
- Inherit from `Exception`
- Suffix with `Exception`
- Include meaningful error messages

### Infrastructure Layer (`Infrastructure/`)

#### Data Context (`Infrastructure/Data/`)
```
Infrastructure/Data/
├── KopitraDbContext.cs           # EF Core context
├── Repositories/
│   ├── ISignalRepository.cs
│   ├── SignalRepository.cs
│   ├── IOrderRepository.cs
│   └── OrderRepository.cs
└── Migrations/
    ├── 20260111_InitialCreate.cs
    └── 20260112_AddNewTable.cs
```

#### Event Store Integration (`Infrastructure/EventStore/`)
```
Infrastructure/EventStore/
├── EventStoreFactory.cs
├── DomainEventStore.cs
└── Snapshots/
    └── SnapshotStore.cs
```

#### Command Handlers (`Infrastructure/Services/CommandHandlers/`)
```
Infrastructure/Services/CommandHandlers/
├── Signal/
│   ├── CreateSignalCommandHandler.cs
│   ├── ModifySignalCommandHandler.cs
│   └── CloseSignalCommandHandler.cs
├── Order/
│   ├── ExecuteOrderCommandHandler.cs
│   └── CloseOrderCommandHandler.cs
└── Subscription/
    ├── StartSubscriptionCommandHandler.cs
    └── EndSubscriptionCommandHandler.cs
```

#### Message Queue Services (`Infrastructure/Services/MessageQueue/`)
```
Infrastructure/Services/MessageQueue/
├── ISignalMessageService.cs
├── SignalMessageService.cs
├── IExecutionMessageService.cs
└── ExecutionMessageService.cs
```

### API Functions Layer (`Functions/`)

```
Functions/
├── Auth/
│   ├── RegisterFunction.cs
│   ├── LoginFunction.cs
│   └── RefreshTokenFunction.cs
├── Signals/
│   ├── CreateSignalFunction.cs
│   ├── GetSignalFunction.cs
│   ├── ModifySignalFunction.cs
│   └── CloseSignalFunction.cs
├── Subscriptions/
│   ├── StartSubscriptionFunction.cs
│   ├── GetSubscriptionsFunction.cs
│   ├── ModifySubscriptionFunction.cs
│   └── EndSubscriptionFunction.cs
├── Orders/
│   ├── ExecuteOrderFunction.cs
│   ├── GetOrdersFunction.cs
│   └── CloseOrderFunction.cs
├── EA/
│   ├── GetSignalsFunction.cs        # GET /api/ea/signals
│   └── PostExecutionsFunction.cs    # POST /api/ea/executions
├── Metrics/
│   ├── GetSignalMetricsFunction.cs
│   ├── GetProviderMetricsFunction.cs
│   └── GetSubscriberMetricsFunction.cs
├── Admin/
│   ├── GetUsersFunction.cs
│   ├── UpdateUserFunction.cs
│   └── GetAuditLogsFunction.cs
└── Health/
    └── HealthCheckFunction.cs
```

**Naming Convention:**
- Suffix with `Function`
- One function per endpoint
- Clear, descriptive names matching API operation

### Models Layer (`Models/`)

```
Models/
├── Requests/
│   ├── CreateSignalRequest.cs
│   ├── ModifySignalRequest.cs
│   ├── StartSubscriptionRequest.cs
│   └── ExecuteOrderRequest.cs
├── Responses/
│   ├── SignalResponse.cs
│   ├── OrderResponse.cs
│   ├── SubscriptionResponse.cs
│   └── MetricsResponse.cs
└── Common/
    ├── ApiErrorResponse.cs
    ├── PaginationRequest.cs
    └── PaginationResponse.cs
```

### Tests (`tests/Kopitra.Api.Tests/`)

```
tests/Kopitra.Api.Tests/
├── Unit/
│   ├── Domain/
│   │   ├── SignalAggregateTests.cs
│   │   ├── OrderAggregateTests.cs
│   │   └── SubscriptionAggregateTests.cs
│   ├── Infrastructure/
│   │   ├── CommandHandlers/
│   │   │   ├── CreateSignalCommandHandlerTests.cs
│   │   │   └── ExecuteOrderCommandHandlerTests.cs
│   │   └── Services/
│   │       └── SignalMessageServiceTests.cs
│   └── Common/
│       └── ValidationHelperTests.cs
└── Integration/
    ├── Functions/
    │   ├── CreateSignalFunctionTests.cs
    │   ├── GetSignalsFunctionTests.cs
    │   └── PostExecutionsFunctionTests.cs
    └── Fixtures/
        ├── DatabaseFixture.cs
        ├── SampleData.cs
        └── TestAuthHelper.cs
```

---

## 💻 Coding Style and Patterns

### 1. Event Definitions

```csharp
// Location: Domain/Events/Signal/SignalCreatedEvent.cs

public class SignalCreatedEvent : AggregateEvent<Signal, SignalId>
{
    public string ProviderUserId { get; }
    public string Pair { get; }
    public decimal PositionSize { get; }
    public SignalDirection Direction { get; }
    public decimal? StopLoss { get; }
    public decimal? TakeProfit { get; }
    
    public SignalCreatedEvent(
        string providerUserId,
        string pair,
        decimal positionSize,
        SignalDirection direction,
        decimal? stopLoss = null,
        decimal? takeProfit = null)
    {
        ProviderUserId = providerUserId;
        Pair = pair;
        PositionSize = positionSize;
        Direction = direction;
        StopLoss = stopLoss;
        TakeProfit = takeProfit;
    }
}
```

**Conventions:**
- Inherit from `AggregateEvent<TAggregateRoot, TAggregateId>`
- Use auto-properties (get-only, initialize in constructor)
- Include all necessary event data (immutable)
- Use descriptive constructor parameters

### 2. Aggregate Implementation

```csharp
// Location: Domain/Entities/Signal.cs

public class Signal : AggregateRoot<Signal, SignalId>
{
    // State - private setters (write only through events)
    public string ProviderUserId { get; private set; }
    public string Pair { get; private set; }
    public decimal PositionSize { get; private set; }
    public SignalDirection Direction { get; private set; }
    public decimal? StopLoss { get; private set; }
    public decimal? TakeProfit { get; private set; }
    public SignalStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    // Event handlers (must be public for EventFlow reflection)
    public void Apply(SignalCreatedEvent @event)
    {
        ProviderUserId = @event.ProviderUserId;
        Pair = @event.Pair;
        PositionSize = @event.PositionSize;
        Direction = @event.Direction;
        StopLoss = @event.StopLoss;
        TakeProfit = @event.TakeProfit;
        Status = SignalStatus.Active;
        CreatedAt = DateTimeOffset.UtcNow.DateTime;
    }
    
    public void Apply(SignalModifiedEvent @event)
    {
        if (@event.NewStopLoss.HasValue)
            StopLoss = @event.NewStopLoss.Value;
        if (@event.NewTakeProfit.HasValue)
            TakeProfit = @event.NewTakeProfit.Value;
    }
    
    public void Apply(SignalClosedEvent @event)
    {
        if (Status == SignalStatus.Closed)
            throw new InvalidOperationException("Signal is already closed");
        Status = SignalStatus.Closed;
    }
    
    // Factory method (domain constructor alternative)
    public static Signal Create(
        SignalId id,
        string providerUserId,
        string pair,
        decimal positionSize,
        SignalDirection direction,
        decimal? stopLoss = null,
        decimal? takeProfit = null)
    {
        var signal = new Signal { Id = id };
        signal.Emit(new SignalCreatedEvent(
            providerUserId, pair, positionSize, direction, stopLoss, takeProfit));
        return signal;
    }
    
    // Business logic methods (emit events, don't modify state directly)
    public void Modify(decimal? stopLoss, decimal? takeProfit)
    {
        if (Status == SignalStatus.Closed)
            throw new InvalidOperationException("Cannot modify closed signal");
        
        Emit(new SignalModifiedEvent(stopLoss, takeProfit));
    }
    
    public void Close()
    {
        Emit(new SignalClosedEvent());
    }
}
```

**Conventions:**
- Inherit from `AggregateRoot<TAggregateRoot, TAggregateId>`
- Use private setters for state properties
- `Apply()` methods are public (EventFlow reflection)
- Business logic methods are public, emit events, use validation
- Use `@event` naming for event parameters (avoid `event` keyword conflict)

### 3. Value Objects (Aggregate IDs)

```csharp
// Location: Domain/Entities/SignalId.cs

public class SignalId : Identity<SignalId>
{
    public SignalId(string value) : base(value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("SignalId cannot be empty", nameof(value));
    }
}
```

**Conventions:**
- Inherit from `Identity<T>`
- Include validation in constructor
- Immutable (no setters)
- Used as aggregate root ID

### 4. Commands

```csharp
// Location: Domain/Commands/CreateSignalCommand.cs

public class CreateSignalCommand : Command<SignalId>
{
    public string ProviderUserId { get; }
    public string Pair { get; }
    public decimal PositionSize { get; }
    public SignalDirection Direction { get; }
    public decimal? StopLoss { get; }
    public decimal? TakeProfit { get; }
    
    public CreateSignalCommand(
        SignalId signalId,
        string providerUserId,
        string pair,
        decimal positionSize,
        SignalDirection direction,
        decimal? stopLoss = null,
        decimal? takeProfit = null)
        : base(signalId)
    {
        if (string.IsNullOrWhiteSpace(providerUserId))
            throw new ArgumentException("ProviderUserId cannot be empty");
        if (positionSize <= 0)
            throw new ArgumentException("PositionSize must be positive");
            
        ProviderUserId = providerUserId;
        Pair = pair;
        PositionSize = positionSize;
        Direction = direction;
        StopLoss = stopLoss;
        TakeProfit = takeProfit;
    }
}
```

**Conventions:**
- Inherit from `Command<TAggregateId>`
- Constructor validation (fail fast)
- Immutable properties
- Aggregate ID in constructor base call

### 5. Command Handlers

```csharp
// Location: Infrastructure/Services/CommandHandlers/Signal/CreateSignalCommandHandler.cs

public class CreateSignalCommandHandler
    : CommandHandler<Signal, SignalId, CreateSignalCommand>
{
    private readonly ILogger<CreateSignalCommandHandler> _logger;
    private readonly IAccountRepository _accountRepository;
    
    public CreateSignalCommandHandler(
        ILogger<CreateSignalCommandHandler> logger,
        IAccountRepository accountRepository)
    {
        _logger = logger;
        _accountRepository = accountRepository;
    }
    
    public override async Task ExecuteAsync(
        Signal aggregate,
        CreateSignalCommand command,
        IResolver resolver,
        CancellationToken cancellationToken)
    {
        // Validation: check provider exists and has permission
        var account = await _accountRepository.GetAsync(
            command.ProviderUserId, cancellationToken);
        
        if (account == null)
            throw new NotFoundException($"Account not found for provider {command.ProviderUserId}");
        
        // Create aggregate (new or existing)
        var signal = aggregate.IsNew
            ? Signal.Create(
                command.AggregateId,
                command.ProviderUserId,
                command.Pair,
                command.PositionSize,
                command.Direction,
                command.StopLoss,
                command.TakeProfit)
            : aggregate;
        
        _logger.LogInformation(
            "Signal created: AggregateId={AggregateId}, Pair={Pair}",
            command.AggregateId, command.Pair);
    }
}
```

**Conventions:**
- Inherit from `CommandHandler<TAggregateRoot, TAggregateId, TCommand>`
- Use dependency injection
- Validate business rules before emitting events
- Throw domain exceptions on validation failure
- Log operations with structured data

### 6. Azure Functions (API Layer)

```csharp
// Location: Functions/Signals/CreateSignalFunction.cs

[Function("CreateSignal")]
public async Task<HttpResponseData> CreateSignal(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "signals")]
    HttpRequestData req)
{
    try
    {
        // Extract JWT and verify
        var token = ExtractJwtToken(req);
        var userId = GetUserIdFromToken(token);
        if (string.IsNullOrEmpty(userId))
            return CreateErrorResponse(req, HttpStatusCode.Unauthorized, "Invalid token");
        
        // Parse request
        var requestBody = await req.ReadAsAsync<CreateSignalRequest>();
        if (!ValidateRequest(requestBody, out var errors))
            return CreateErrorResponse(req, HttpStatusCode.BadRequest, errors);
        
        // Create command with generated ID (idempotency)
        var signalId = new SignalId(Guid.NewGuid().ToString());
        var command = new CreateSignalCommand(
            signalId,
            userId,
            requestBody.Pair,
            requestBody.PositionSize,
            requestBody.Direction,
            requestBody.StopLoss,
            requestBody.TakeProfit);
        
        // Execute command via bus
        await _commandBus.PublishAsync(command);
        
        // Retrieve created aggregate
        var signal = await _eventStore.LoadAsync(signalId);
        
        // Return response
        var response = new SignalResponse
        {
            SignalId = signal.Id.Value,
            Pair = signal.Pair,
            Direction = signal.Direction,
            Status = signal.Status
        };
        
        var httpResponse = req.CreateResponse(HttpStatusCode.Created);
        await httpResponse.WriteAsJsonAsync(response);
        return httpResponse;
    }
    catch (ArgumentException ex)
    {
        _logger.LogWarning(ex, "Invalid input for CreateSignal");
        return CreateErrorResponse(req, HttpStatusCode.BadRequest, ex.Message);
    }
    catch (InvalidOperationException ex)
    {
        _logger.LogWarning(ex, "Business rule violation for CreateSignal");
        return CreateErrorResponse(req, HttpStatusCode.BadRequest, ex.Message);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error in CreateSignal");
        return CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal error");
    }
}

private HttpResponseData CreateErrorResponse(
    HttpRequestData req,
    HttpStatusCode statusCode,
    string message)
{
    var response = req.CreateResponse(statusCode);
    response.Headers.Add("Content-Type", "application/json; charset=utf-8");
    var errorDto = new { error = message };
    response.WriteString(JsonSerializer.Serialize(errorDto));
    return response;
}
```

**Conventions:**
- Function names match API operation (CreateSignal, GetSignals, etc.)
- Minimal logic: validation → command creation → command execution → response
- Try-catch blocks for each exception type
- Structured logging with context
- Return appropriate HTTP status codes
- Extract JWT and verify user permissions

### 7. Error Handling

```csharp
// Domain exceptions - specific, business-focused
try
{
    signal.Close();
}
catch (InvalidOperationException ex)
{
    _logger.LogWarning(ex, "Business rule violation");
    return CreateErrorResponse(req, HttpStatusCode.BadRequest, ex.Message);
}

// Infrastructure exceptions - data access issues
catch (NotFoundException ex)
{
    _logger.LogWarning(ex, "Resource not found");
    return CreateErrorResponse(req, HttpStatusCode.NotFound, ex.Message);
}

// Validation exceptions
catch (ArgumentException ex)
{
    _logger.LogWarning(ex, "Invalid argument");
    return CreateErrorResponse(req, HttpStatusCode.BadRequest, ex.Message);
}

// System exceptions
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error");
    return CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal error");
}
```

**Conventions:**
- Throw domain-specific exceptions from domain/infrastructure layers
- Catch and log at Function layer
- Return appropriate HTTP status codes
- Never expose sensitive error details to client

### 8. Logging

```csharp
// Structured logging with context
_logger.LogInformation(
    "Signal created successfully: SignalId={SignalId}, ProviderId={ProviderId}, Pair={Pair}",
    signal.Id, signal.ProviderUserId, signal.Pair);

_logger.LogWarning(ex,
    "Failed to create signal: ProviderId={ProviderId}, Reason={Reason}",
    userId, ex.Message);

_logger.LogError(ex,
    "Unexpected error processing signal: SignalId={SignalId}",
    signalId);
```

**Conventions:**
- Use named parameters in log messages
- Include relevant context (IDs, values)
- Use appropriate log levels (Information, Warning, Error)
- Never log sensitive data (passwords, secrets)

---

## 🧪 Testing Guidelines

### Unit Tests - Domain Logic

```csharp
// Location: tests/Kopitra.Api.Tests/Unit/Domain/SignalAggregateTests.cs

[TestClass]
public class SignalAggregateTests
{
    [TestMethod]
    public void Create_WithValidInputs_EmitsSignalCreatedEvent()
    {
        // Arrange
        var signalId = new SignalId(Guid.NewGuid().ToString());
        var providerId = "provider-123";
        var pair = "EURUSD";
        
        // Act
        var signal = Signal.Create(signalId, providerId, pair, 1.5m, SignalDirection.Buy);
        
        // Assert
        Assert.AreEqual(signalId, signal.Id);
        Assert.AreEqual(providerId, signal.ProviderUserId);
        Assert.AreEqual(SignalStatus.Active, signal.Status);
    }
    
    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Close_WhenAlreadyClosed_ThrowsException()
    {
        // Arrange
        var signal = Signal.Create(
            new SignalId(Guid.NewGuid().ToString()),
            "provider-123",
            "EURUSD", 1.5m, SignalDirection.Buy);
        signal.Close();
        
        // Act
        signal.Close();
        
        // Assert - exception expected
    }
}
```

### Integration Tests - API Endpoints

```csharp
// Location: tests/Kopitra.Api.Tests/Integration/Functions/CreateSignalFunctionTests.cs

[TestClass]
public class CreateSignalFunctionTests
{
    private ICommandBus _commandBus;
    private IEventStore _eventStore;
    private CreateSignalFunction _function;
    
    [TestInitialize]
    public async Task Setup()
    {
        // Use in-memory database for tests
        var dbOptions = new DbContextOptionsBuilder<KopitraDbContext>()
            .UseInMemoryDatabase("test-db")
            .Options;
        
        // Configure EventFlow and services
        _commandBus = // ... setup
        _eventStore = // ... setup
        _function = new CreateSignalFunction(_commandBus, _eventStore, _logger);
    }
    
    [TestMethod]
    public async Task CreateSignal_WithValidRequest_Returns201Created()
    {
        // Arrange
        var request = new CreateSignalRequest
        {
            Pair = "EURUSD",
            PositionSize = 1.5m,
            Direction = SignalDirection.Buy
        };
        
        // Act
        var response = await _function.CreateSignal(CreateMockRequest(request));
        
        // Assert
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        var result = await response.ReadAsAsync<SignalResponse>();
        Assert.IsNotNull(result.SignalId);
    }
}
```

**Conventions:**
- Test single responsibility per test method
- Use Arrange-Act-Assert pattern
- Mock external dependencies
- Use in-memory database for integration tests
- Test both happy path and error scenarios

---

## ✅ Implementation Checklist

When implementing a new feature:

- [ ] **SYSTEM_DESIGN.md updated** with API endpoint and data model changes
- [ ] **Domain events created** in `Domain/Events/`
- [ ] **Aggregate entity updated** in `Domain/Entities/`
- [ ] **Commands defined** in `Domain/Commands/`
- [ ] **Command handlers implemented** in `Infrastructure/Services/CommandHandlers/`
- [ ] **Azure Function created** in `Functions/<Module>/`
- [ ] **Database migration generated** and reviewed
- [ ] **Unit tests written** for aggregates and command handlers
- [ ] **Integration tests written** for API endpoints
- [ ] **All tests passing** (`dotnet test`)
- [ ] **Code formatted** (`dotnet format`)
- [ ] **Logging added** at appropriate points
- [ ] **Error handling** for all exception types
- [ ] **JWT/Permission validation** in API layer

---

## 🔗 Key Concepts

### Idempotency
- Signal ID uniquely identifies a signal
- Re-sending same command with same ID returns same result
- EventFlow handles this automatically via aggregate ID

### Event Immutability
- Events never modified after creation
- Only new events emitted for state changes
- Provides complete audit trail

### Separation of Concerns
- **Domain**: Business logic, aggregates, events
- **Infrastructure**: Data access, command handlers, repositories
- **API**: HTTP handling, JWT validation, request/response mapping

### CQRS Pattern
- **Commands**: Create/Modify/Delete operations
- **Queries**: Read operations (separate from event store)
- Allows independent scaling of read/write operations

---

## 📚 Related Documentation

- **System Design**: [SYSTEM_DESIGN.md](../docs/SYSTEM_DESIGN.md) - Complete specification
- **README**: [README.md](./README.md) - Setup and technology overview
- **Root AGENTS.md**: [AGENTS.md](../AGENTS.md) - Overall project guidelines

---

## 🔗 Important Reminders

1. **Always update SYSTEM_DESIGN.md first** when adding new features
2. **Follow the exact file structure** for consistency
3. **Use EventFlow patterns** for all aggregates and events
4. **Write tests before/alongside implementation** (TDD)
5. **Log operations with context** for debugging
6. **Validate at domain layer** for business rules
7. **Validate at API layer** for input/format
8. **Never expose sensitive data** in logs or errors

---

**Last Updated**: January 11, 2026

**Remember**: Code quality, consistency, and clarity are paramount. Follow these patterns exactly to maintain codebase health.
