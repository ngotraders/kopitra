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
   - Tests (Unit for aggregate if needed)
   - Commands and command handlers
   - Tests (Unit for commands)
   - API endpoints (Functions)
   - Tests (Unit + Integration)

3. **Verify changes:**
   - Run migrations: `dotnet ef database update`
   - Run tests: `dotnet test`
   - Test API locally: `func start`

---

## 🏗️ File Organization and Code Placement

This section provides a concise, project-agnostic guideline for organizing code. Apply these recommendations flexibly to match team conventions and project scale.

### Guiding Principles
- Separate responsibilities by layer: Domain (business logic), Application (use cases / commands & queries), Infrastructure (persistence, external systems), API/Functions (endpoints), Models/DTO, and Tests.
- Prioritize discoverability and clarity when placing files. Public classes should generally live in files named after the class, but rigid "one class per file" is optional where grouping improves readability.
- Keep domain logic strictly focused on business rules (aggregates, entities, value objects, domain events). Do not mix infrastructure or application concerns into Domain.

### Domain Layer (`Domain/`)
- Purpose: Aggregates, entities, value objects, domain events, and pure domain services.
- Example layout:
    - `Domain/Users/UserAggregate.cs`
    - `Domain/Users/Events/UserRegisteredEvent.cs`
    - `Domain/ValueObjects/UserId.cs`
- Note: Commands, queries, and their handlers should not be placed in Domain. Domain should expose a clear API (aggregate methods) to express business intent.

### Application Layer (`Application/`)
- Purpose: Implement use cases, define commands/queries and handlers, map DTOs, coordinate transactions and external calls.
- Example layout:
    - `Application/Users/Commands/RegisterUserCommand.cs`
    - `Application/Users/Commands/RegisterUserCommandHandler.cs`
    - `Application/Users/Queries/GetUserByIdQuery.cs`
- Best practice: Handlers orchestrate domain calls (invoke aggregate methods) and perform input validation; let domain enforce invariants.

### Infrastructure Layer (`Infrastructure/`)
- Purpose: Persistence adapters (ORM / SQL mapping), message queue clients, external API clients, and shared low-level implementations.
- Example: `Infrastructure/Persistence/DomainEventEntity.cs`, `Infrastructure/MessageQueue/SignalMessageService.cs`.

### API / Functions Layer (`Functions/`)
- Purpose: HTTP / Azure Functions entrypoints. Handle authentication/authorization, translate requests into application commands/queries, and format responses. Keep business logic out of functions.
- Naming: Use clear, operation-oriented names (e.g., `CreateSignalFunction`).

### Models / DTO (`Models/`)
- Purpose: Lightweight request/response DTOs used by the API surface. Keep DTOs separate from domain models and map explicitly.

### Tests (`tests/`)
- Purpose: Unit tests for domain rules, application-level tests for use cases, and integration tests for endpoints and persistence.
- Example layout: `tests/Unit/Domain/*`, `tests/Unit/Application/*`, `tests/Integration/Functions/*`.

### Naming and conventions
- Use `XxxAggregate` for aggregates and past-tense `XxxCreatedEvent` for events.
- Prefix all events with domain name (e.g. if domain name is `Users` then event names is like `UserRegisteredEvent`).
- Place identity and value objects under `Domain/ValueObjects`.
- Put commands and queries (and their handlers) under `Application/[Aggregate]/Commands` and `Application/[Aggregate]/Queries` respectively, keeping handlers in the same namespace as their messages.

Adjust these guidelines as needed for your team's workflow; if desired, we can expand this section with concrete folder trees tailored to this repository.

---

## 💻 Coding Style and Patterns

### 1. Event Definitions

```csharp
// Location: Domain/Events/Signal/SignalCreatedEvent.cs

public class SignalCreatedEvent : AggregateEvent<Signal, SignalId>
{
    public UserId ProviderUserId { get; }
    public string Pair { get; }
    public decimal PositionSize { get; }
    public SignalDirection Direction { get; }
    public decimal? StopLoss { get; }
    public decimal? TakeProfit { get; }
    
    public SignalCreatedEvent(
        UserId providerUserId,
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
    public UserId ProviderUserId { get; private set; }
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
    public UserId ProviderUserId { get; }
    public string Pair { get; }
    public decimal PositionSize { get; }
    public SignalDirection Direction { get; }
    public decimal? StopLoss { get; }
    public decimal? TakeProfit { get; }
    
    public CreateSignalCommand(
        SignalId signalId,
        UserId providerUserId,
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

- **OpenAPI annotations are mandatory**: decorate every function with `[OpenApiOperation]`, `[OpenApiRequestBody]`, `[OpenApiResponseWithBody]` (and `[OpenApiParameter]` for path/query/header inputs) from `Microsoft.Azure.WebJobs.Extensions.OpenApi`. This keeps swagger in sync and documents request/response contracts alongside the code.

```csharp
// Location: Functions/Signals/CreateSignalFunction.cs

[Function("CreateSignal")]
[OpenApiOperation(operationId: "CreateSignal", tags: new[] { "Signals" }, Summary = "Create a new trading signal")]
[OpenApiRequestBody(contentType: "application/json", bodyType: typeof(CreateSignalRequest), Description = "Signal creation payload")]
[OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(SignalResponse), Description = "Signal created successfully")]
[OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = "Invalid request payload")]
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

## 🏛️ Architectural Patterns & Best Practices

This section documents critical architectural decisions learned through implementation to prevent common mistakes and maintain clean code separation.

### 1. Interface/Implementation Separation Across Layers

**Principle**: Always use interfaces in the Application layer; place implementations in the Infrastructure layer. This enables Inversion of Control (IoC), testability, and loose coupling.

**Pattern:**

```csharp
// Location: Application/Users/Services/IPasswordHasher.cs
// Interface in Application layer (depends on nothing)
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string hash, string password);
}
```

```csharp
// Location: Infrastructure/Security/Pbkdf2PasswordHasher.cs
// Implementation in Infrastructure layer
public class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int IterationCount = 100000;
    private const int SaltSize = 16; // bytes
    private const int HashSize = 32; // bytes
    
    public string Hash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty", nameof(password));
        
        using var rng = new System.Security.Cryptography.RNGCryptoServiceProvider();
        var salt = new byte[SaltSize];
        rng.GetBytes(salt);
        
        using var pbkdf2 = new System.Security.Cryptography.Rfc2898DeriveBytes(
            password, salt, IterationCount, System.Security.Cryptography.HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(HashSize);
        
        // Embed salt in output: salt (16 bytes) + hash (32 bytes) = 48 bytes total
        var output = new byte[SaltSize + HashSize];
        Array.Copy(salt, 0, output, 0, SaltSize);
        Array.Copy(hash, 0, output, SaltSize, HashSize);
        
        return Convert.ToBase64String(output);
    }
    
    public bool Verify(string hash, string password)
    {
        if (string.IsNullOrWhiteSpace(hash) || string.IsNullOrWhiteSpace(password))
            return false;
        
        try
        {
            var hashBytes = Convert.FromBase64String(hash);
            if (hashBytes.Length != SaltSize + HashSize)
                return false;
            
            var salt = new byte[SaltSize];
            Array.Copy(hashBytes, 0, salt, 0, SaltSize);
            
            using var pbkdf2 = new System.Security.Cryptography.Rfc2898DeriveBytes(
                password, salt, IterationCount, System.Security.Cryptography.HashAlgorithmName.SHA256);
            var computedHash = pbkdf2.GetBytes(HashSize);
            
            for (int i = 0; i < HashSize; i++)
                if (hashBytes[SaltSize + i] != computedHash[i])
                    return false;
            
            return true;
        }
        catch
        {
            return false;
        }
    }
}
```

**DI Registration in Domain/ServiceCollectionExtension.cs:**

```csharp
public static IServiceCollection AddKopitra<TDbContextProvider>(this IServiceCollection services)
    where TDbContextProvider : class, IDbContextProvider<KopitraDbContext>
{
    // Register interfaces to implementations (Infrastructure provides the impl)
    services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
    
    // ... rest of EventFlow registration
    return services.AddEventFlow(ef => ...);
}
```

**Benefits:**
- **Testability**: Mock `IPasswordHasher` in unit tests
- **Swappability**: Change implementation without touching command handlers
- **No coupling**: Commands layer doesn't reference Infrastructure layer directly
- **Dependency Inversion**: High-level modules (Commands) depend on abstractions (Interfaces), not low-level modules (Implementations)

**❌ WRONG - Do NOT do this:**

```csharp
// WRONG: Concrete class reference in Commands layer
public class LoginCommandHandler
{
    private readonly Pbkdf2PasswordHasher _hasher; // ❌ Concrete class - couples to Infrastructure
}

// WRONG: Registering without interface
services.AddSingleton<PasswordHasher>(); // ❌ No interface - harder to mock in tests
```

### 2. Commands Layer Must NOT Reference Queries Layer

**Principle**: Commands layer handles write operations; Queries layer handles read operations. They must be completely independent. If you need to read data in a command handler, use the database directly via repositories or EF DbContext, never via query handlers or read models.

**✅ CORRECT - Commands layer accesses DbContext directly:**

```csharp
// Location: Application/Auth/Commands/LoginCommandHandler.cs
public class LoginCommandHandler
{
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokenService;
    private readonly ICommandBus _commandBus;
    private readonly IDbContextProvider<KopitraDbContext> _contextProvider; // ✅ Direct DB access
    
    public async Task<(string AccessToken, string RefreshToken)> HandleAsync(LoginCommand command)
    {
        // Database access is OK - using EF DbContext directly
        using var context = _contextProvider.CreateContext();
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Email == command.Email)
            .ConfigureAwait(false);
        
        if (user == null)
            throw new InvalidOperationException("Invalid credentials");
        
        // Validate password
        if (!_hasher.Verify(user.PasswordHash, command.Password))
            throw new InvalidOperationException("Invalid credentials");
        
        // Generate tokens and issue refresh token via aggregate command
        var accessToken = _tokenService.GenerateToken(user.Id);
        var refreshToken = Guid.NewGuid().ToString("N");
        
        // Emit event through aggregate
        var userId = new UserId(user.Id);
        var cmd = new IssueRefreshTokenCommand(userId) { RefreshToken = refreshToken };
        await _commandBus.PublishAsync(cmd, CancellationToken.None);
        
        return (accessToken, refreshToken);
    }
}
```

**❌ WRONG - Do NOT reference Queries layer:**

```csharp
// WRONG: Commands referencing Queries layer
public class LoginCommandHandler
{
    private readonly UserReadModelQuery _userQuery; // ❌ DO NOT REFERENCE QUERIES
    
    public async Task HandleAsync(LoginCommand command)
    {
        // ❌ WRONG: Using Queries in Commands layer
        var user = await _userQuery.GetUserByEmailAsync(command.Email);
    }
}

// Namespace check:
// Commands: Kopitra.Api.Application.Auth.Commands
// Queries: Kopitra.Api.Application.Users.Queries
// ❌ Never import Kopitra.Api.Application.Users.Queries in Commands layer
```

**Why this matters:**
- **Separation of Concerns**: Write logic shouldn't depend on read logic
- **Avoiding Circular Dependencies**: If Queries depends on Commands (for event sourcing), but Commands depend on Queries, you get circular deps
- **Database Consistency**: In event-sourced systems, read models can lag behind the event store. Don't use potentially stale data to make write decisions
- **Testing**: Easier to test commands in isolation when they don't depend on read models

### 3. Missing Using Directives (Common Compilation Errors)

Several classes require specific `using` directives that are easy to forget:

```csharp
// Commands/Queries using EF Core
using Microsoft.EntityFrameworkCore; // Required for FirstOrDefaultAsync, ToListAsync, etc.
using EventFlow.EntityFramework;      // Required for IDbContextProvider<T>

// Commands/Queries using EventFlow
using EventFlow;                       // Required for ICommandBus, IEventStore, etc.
using EventFlow.Commands;              // Required for Command<T>, CommandHandler<>, etc.

// Infrastructure services using JWT
using System.IdentityModel.Tokens.Jwt; // Required for JwtSecurityToken, JwtSecurityTokenHandler
using Microsoft.IdentityModel.Tokens;  // Required for SymmetricSecurityKey, SigningCredentials

// Password hashing
using System.Security.Cryptography;    // Required for RNGCryptoServiceProvider, Rfc2898DeriveBytes
```

**Error Examples:**

| Error | Missing Using | Solution |
|-------|---------------|----------|
| `'DbSet<T>' has no definition for 'FirstOrDefaultAsync'` | `Microsoft.EntityFrameworkCore` | Add `using Microsoft.EntityFrameworkCore;` |
| `'IDbContextProvider<>' not found` | `EventFlow.EntityFramework` | Add `using EventFlow.EntityFramework;` |
| `'ICommandBus' not found` | `EventFlow` or `EventFlow.Commands` | Add `using EventFlow.Commands;` |
| `'JwtSecurityToken' not found` | `System.IdentityModel.Tokens.Jwt` | Add `using System.IdentityModel.Tokens.Jwt;` |
| `'Rfc2898DeriveBytes' not found` | `System.Security.Cryptography` | Add `using System.Security.Cryptography;` |

### 4. Null-Forgiving Operator for Test Mode

When you have multiple interfaces/implementations with different null-handling needs, the null-forgiving operator (`!`) helps suppress nullability warnings:

```csharp
private readonly IPasswordHasher _hasher = null!;

// This tells the compiler: "I know this might be null, but I'll handle it appropriately"
// You must then check _testMode before using these fields
```

---

## ✅ Implementation Checklist

When implementing a new feature:

- [ ] **SYSTEM_DESIGN.md updated** with API endpoint and data model changes
- [ ] **Domain events created** in `Domain/[Aggregate]/Events/`
- [ ] **Aggregate root created** in `Domain/[Aggregate]/[Aggregate]Aggregate.cs`
- [ ] **Value objects created** in `Domain/ValueObjects/` (one file per class)
- [ ] **Commands defined** in `Application/[Aggregate]/Commands/[Action]Command.cs`
- [ ] **Command handlers implemented** in `Application/[Aggregate]/Commands/[Action]CommandHandler.cs`
- [ ] **ReadModels created** in `Application/[Aggregate]/Queries/` (if applicable)
- [ ] **Queries defined** in `Application/[Aggregate]/Queries/` (if applicable)
- [ ] **Query handlers implemented** in `Application/[Aggregate]/Queries/` (if applicable)
- [ ] **Azure Function created** in `Functions/[Module]/[Operation]Function.cs`
- [ ] **Database migration generated** and reviewed
- [ ] **Unit tests written** for aggregates (events, state changes)
- [ ] **Unit tests written** for command handlers (business logic validation)
- [ ] **Unit tests written** for query handlers (read model logic)
- [ ] **Integration tests written** for API endpoints (full flow)
- [ ] **All tests passing** (`dotnet test`)
- [ ] **Build successful** (`dotnet build`)
- [ ] **Code formatted** (`dotnet format`)
- [ ] **Logging added** at appropriate points (domain, handlers, functions)
- [ ] **Error handling** for all exception types (domain, validation, infrastructure)
- [ ] **JWT/Permission validation** in API layer
- [ ] **Idempotency handling** where applicable (duplicate prevention)

### Additional Architectural Checks
- [ ] **Service interfaces in Application layer** - All external services (e.g., `IPasswordHasher`, `ITokenService`) defined in `Application/[Aggregate]/Services/`
- [ ] **Service implementations in Infrastructure layer** - Concrete classes (e.g., `Pbkdf2PasswordHasher`, `JwtTokenService`) placed in `Infrastructure/Security/` or appropriate subdirectory
- [ ] **DI registrations correct** - Interfaces registered to implementations in `ServiceCollectionExtension.cs` using `services.AddSingleton<IInterface, ConcreteImplementation>()`
- [ ] **Commands layer independent from Queries** - No imports of `Kopitra.Api.Application.[Aggregate].Queries` anywhere in Commands handlers
- [ ] **Direct database access in Commands** - Use `IDbContextProvider<KopitraDbContext>` for read operations, not Queries layer
- [ ] **All using directives present** - Verify required namespaces included:
  - `using Microsoft.EntityFrameworkCore;` for DbSet extension methods
  - `using EventFlow.EntityFramework;` for IDbContextProvider
  - `using EventFlow.Commands;` for Command base classes
  - `using System.IdentityModel.Tokens.Jwt;` for JWT utilities
  - `using System.Security.Cryptography;` for hashing
- [ ] **Null-forgiving operators used appropriately** - Parameterless constructors use `null!` to suppress nullability warnings
- [ ] **No circular dependency issues** - Commands → Queries pattern does not exist

## 🌐 Azure Functions & OpenAPI Design

### Aggregate-Based Endpoint Organization

**Principle**: Group HTTP endpoints by aggregate (User, Account, Signal, Subscription, ExpertAdvisorSession, etc.), not by individual function. Each aggregate should have a dedicated `FunctionClass.cs` that orchestrates all operations for that aggregate.

**File Structure:**
```
Functions/
├── Users/
│   ├── Models/
│   │   ├── RegisterUserRequest.cs
│   │   ├── LoginRequest.cs
│   │   ├── LoginResponse.cs
│   │   └── UserResponse.cs
│   └── UserFunctions.cs          # All User endpoints (Register, Login, GetProfile, etc.)
├── Accounts/
│   ├── Models/
│   │   ├── CreateAccountRequest.cs
│   │   ├── UpdateAccountRequest.cs
│   │   └── AccountResponse.cs
│   └── AccountFunctions.cs       # All Account endpoints (Create, Get, Update, Delete, Verify)
├── Signals/
│   ├── Models/
│   │   ├── CreateSignalRequest.cs
│   │   ├── ModifySignalRequest.cs
│   │   └── SignalResponse.cs
│   └── SignalFunctions.cs        # All Signal endpoints
├── Subscriptions/
│   ├── Models/
│   │   ├── CreateSubscriptionRequest.cs
│   │   ├── UpdateSubscriptionRequest.cs
│   │   └── SubscriptionResponse.cs
│   └── SubscriptionFunctions.cs  # All Subscription endpoints
└── ExpertAdvisors/
    ├── Models/
    │   ├── CreateSessionRequest.cs
    │   ├── HeartbeatRequest.cs
    │   ├── ExecutionConfirmRequest.cs
    │   └── SessionResponse.cs
    └── ExpertAdvisorFunctions.cs # All EA Session endpoints
```

**Rationale:**
- **Aggregate-scoped DTOs**: Request/Response models are grouped with their function class, making it clear which models belong to which API operations
- **Single folder per aggregate**: All related code (DTOs + Functions) in one logical folder
- **Namespace clarity**: `Kopitra.Api.Functions.Users.Models.RegisterUserRequest` clearly indicates scope
- **Scalability**: As aggregate grows, Models subfolder becomes a natural place for all input/output contracts

**Benefit**: One class per aggregate boundary makes it easy to locate and manage all related endpoints and models. Reduces file clutter and improves maintainability.

### DTO Placement and Naming Convention

**Location Rule**: Each aggregate's DTOs are placed in `Functions/{AggregateType}/Models/` folder.

**File Naming:**
- Request models: `{OperationName}Request.cs` (e.g., `RegisterUserRequest.cs`, `CreateAccountRequest.cs`)
- Response models: `{EntityName}Response.cs` (e.g., `UserResponse.cs`, `AccountResponse.cs`) or `{OperationName}Response.cs` (e.g., `LoginResponse.cs`)
- Complex value objects: `{ObjectName}.cs` (e.g., `FundingStrategy.cs`, `ExecutionConfirm.cs`)

**Example Structure:**
```csharp
// Location: Functions/Users/Models/RegisterUserRequest.cs
namespace Kopitra.Api.Functions.Users.Models;

public class RegisterUserRequest
{
    [Required]
    [EmailAddress]
    public string? Email { get; set; }

    [Required]
    [MinLength(8)]
    public string? Password { get; set; }

    public string? FullName { get; set; }
}

// Location: Functions/Users/Models/UserResponse.cs
namespace Kopitra.Api.Functions.Users.Models;

public class UserResponse
{
    public string? UserId { get; set; }
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public bool CanProvide { get; set; }
    public bool CanSubscribe { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}

// Location: Functions/Users/UserFunctions.cs
namespace Kopitra.Api.Functions.Users;

using Models;  // Reference local Models namespace

public class UserFunctions
{
    [Function("RegisterUser")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(RegisterUserRequest))]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(UserResponse))]
    public async Task<HttpResponseData> RegisterUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "users")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        var body = await req.ReadFromJsonAsync<RegisterUserRequest>(cancellationToken);
        // ... implementation
    }
}
```

**DO NOT**: Place DTOs in a shared Models folder at project root (e.g., `/Models/Users/RegisterUserRequest.cs`).  
Instead: Always place within function scope (e.g., `/Functions/Users/Models/RegisterUserRequest.cs`)

### OpenAPI Definition with Azure WebJobs Extensions

**Pattern**: Use `Microsoft.Azure.WebJobs.Extensions.OpenApi` attributes to declare OpenAPI schema inline with each function.

**Required NuGet Package:**
```xml
<PackageReference Include="Microsoft.Azure.WebJobs.Extensions.OpenApi" Version="1.5.0" />
```

**Function Structure Example:**

```csharp
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;
using System.Net;
using System.ComponentModel.DataAnnotations;
using EventFlow.Commands;
using EventFlow.Queries;
using Microsoft.Extensions.Logging;

namespace Kopitra.Api.Functions.Users;

/// <summary>
/// User aggregate functions: Registration, login, profile management.
/// Endpoints are grouped by aggregate lifecycle, not individual operations.
/// </summary>
public class UserFunctions
{
    private readonly ICommandBus _commandBus;
    private readonly IQueryProcessor _queryProcessor;
    private readonly ILogger<UserFunctions> _logger;

    public UserFunctions(ICommandBus commandBus, IQueryProcessor queryProcessor, ILogger<UserFunctions> logger)
    {
        _commandBus = commandBus;
        _queryProcessor = queryProcessor;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user account.
    /// Returns 201 Created with the new user profile on success.
    /// </summary>
    [Function("RegisterUser")]
    [OpenApiOperation(operationId: "RegisterUser", tags: new[] { "Users" }, Summary = "Register a new user account")]
    [OpenApiParameter(name: "Authorization", In = ParameterLocation.Header, Required = true, Description = "JWT Bearer token (optional for public registration)")]
    [OpenApiRequestBody(
        contentType: "application/json",
        bodyType: typeof(RegisterUserRequest),
        Description = "User registration details")]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.Created,
        contentType: "application/json",
        bodyType: typeof(UserResponse),
        Description = "User created successfully")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = "Invalid request (email format, missing fields, email already exists)")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.InternalServerError, Description = "Server error during registration")]
    public async Task<HttpResponseData> RegisterUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "users")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("RegisterUser function triggered");

            // Read request body
            var body = await req.ReadFromJsonAsync<RegisterUserRequest>(cancellationToken).ConfigureAwait(false);

            // Validate input
            if (body == null || string.IsNullOrWhiteSpace(body.Email) || string.IsNullOrWhiteSpace(body.Password))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(
                    new { error = "Email and password are required" },
                    cancellationToken).ConfigureAwait(false);
                return badResponse;
            }

            if (!IsValidEmail(body.Email))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(
                    new { error = "Invalid email format" },
                    cancellationToken).ConfigureAwait(false);
                return badResponse;
            }

            // Create command
            var userId = new UserId(Guid.NewGuid().ToString());
            var command = new RegisterUserCommand(userId)
            {
                Email = body.Email,
                Password = body.Password,
                FullName = body.FullName
            };

            // Execute command
            await _commandBus.PublishAsync(command, cancellationToken).ConfigureAwait(false);

            // Retrieve created user via query
            var user = await _queryProcessor.ProcessAsync(
                new GetUserByIdQuery(userId.Value),
                cancellationToken).ConfigureAwait(false);

            // Return created user
            var response = req.CreateResponse(HttpStatusCode.Created);
            response.Headers.Add("Content-Type", "application/json");
            response.Headers.Add("Location", $"/api/users/{user.Id}");
            await response.WriteAsJsonAsync(
                new UserResponse
                {
                    UserId = user.Id.Value,
                    Email = user.Email,
                    FullName = user.FullName,
                    CanProvide = user.CanProvide,
                    CanSubscribe = user.CanSubscribe,
                    CreatedAt = user.CreatedAt
                },
                cancellationToken).ConfigureAwait(false);

            return response;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error in RegisterUser");
            var response = req.CreateResponse(HttpStatusCode.BadRequest);
            await response.WriteAsJsonAsync(new { error = ex.Message }, CancellationToken.None).ConfigureAwait(false);
            return response;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business rule violation in RegisterUser");
            var response = req.CreateResponse(HttpStatusCode.BadRequest);
            await response.WriteAsJsonAsync(new { error = ex.Message }, CancellationToken.None).ConfigureAwait(false);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in RegisterUser");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(
                new { error = "Internal server error" },
                CancellationToken.None).ConfigureAwait(false);
            return response;
        }
    }

    /// <summary>
    /// User login: authenticate with email and password, return JWT tokens.
    /// </summary>
    [Function("LoginUser")]
    [OpenApiOperation(operationId: "LoginUser", tags: new[] { "Users" }, Summary = "Authenticate user and return JWT tokens")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(LoginRequest))]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(LoginResponse))]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = "Invalid email or password")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = "Missing email or password")]
    public async Task<HttpResponseData> LoginUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "users/login")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("LoginUser function triggered");

            var body = await req.ReadFromJsonAsync<LoginRequest>(cancellationToken).ConfigureAwait(false);

            if (body == null || string.IsNullOrWhiteSpace(body.Email) || string.IsNullOrWhiteSpace(body.Password))
            {
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteAsJsonAsync(new { error = "Email and password required" }, cancellationToken).ConfigureAwait(false);
                return response;
            }

            // Use injected handler to avoid direct construction
            var handler = new LoginCommandHandler();
            var loginCommand = new LoginCommand { Email = body.Email, Password = body.Password };
            var (accessToken, refreshToken) = await handler.HandleAsync(loginCommand).ConfigureAwait(false);

            var response200 = req.CreateResponse(HttpStatusCode.OK);
            await response200.WriteAsJsonAsync(
                new LoginResponse { AccessToken = accessToken, RefreshToken = refreshToken },
                cancellationToken).ConfigureAwait(false);

            return response200;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Authentication failed");
            var response = req.CreateResponse(HttpStatusCode.Unauthorized);
            await response.WriteAsJsonAsync(new { error = "Invalid credentials" }, CancellationToken.None).ConfigureAwait(false);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in LoginUser");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new { error = "Internal server error" }, CancellationToken.None).ConfigureAwait(false);
            return response;
        }
    }

    /// <summary>
    /// Get current user profile (requires JWT authentication).
    /// </summary>
    [Function("GetUserProfile")]
    [OpenApiOperation(operationId: "GetUserProfile", tags: new[] { "Users" }, Summary = "Get current authenticated user profile")]
    [OpenApiParameter(name: "Authorization", In = ParameterLocation.Header, Required = true, Description = "JWT Bearer token")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(UserResponse))]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = "Missing or invalid JWT token")]
    public async Task<HttpResponseData> GetUserProfile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/me")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("GetUserProfile function triggered");

            // Extract JWT and verify
            var token = ExtractBearerToken(req);
            if (string.IsNullOrEmpty(token))
            {
                var response = req.CreateResponse(HttpStatusCode.Unauthorized);
                await response.WriteAsJsonAsync(new { error = "Missing JWT token" }, cancellationToken).ConfigureAwait(false);
                return response;
            }

            // Decode token to get userId (simplified - in production, use proper JWT validation)
            var userId = DecodeUserId(token);
            if (string.IsNullOrEmpty(userId))
            {
                var response = req.CreateResponse(HttpStatusCode.Unauthorized);
                await response.WriteAsJsonAsync(new { error = "Invalid JWT token" }, cancellationToken).ConfigureAwait(false);
                return response;
            }

            // Fetch user
            var user = await _queryProcessor.ProcessAsync(
                new GetUserByIdQuery(userId),
                cancellationToken).ConfigureAwait(false);

            if (user == null)
            {
                var response = req.CreateResponse(HttpStatusCode.NotFound);
                await response.WriteAsJsonAsync(new { error = "User not found" }, cancellationToken).ConfigureAwait(false);
                return response;
            }

            var okResponse = req.CreateResponse(HttpStatusCode.OK);
            await okResponse.WriteAsJsonAsync(
                new UserResponse
                {
                    UserId = user.Id.Value,
                    Email = user.Email,
                    FullName = user.FullName,
                    CanProvide = user.CanProvide,
                    CanSubscribe = user.CanSubscribe,
                    CreatedAt = user.CreatedAt
                },
                cancellationToken).ConfigureAwait(false);

            return okResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetUserProfile");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new { error = "Internal server error" }, CancellationToken.None).ConfigureAwait(false);
            return response;
        }
    }

    // Helper methods
    private string ExtractBearerToken(HttpRequestData req)
    {
        if (req.Headers.TryGetValues("Authorization", out var authHeaders))
        {
            var authHeader = authHeaders.FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return authHeader.Substring("Bearer ".Length);
        }
        return null;
    }

    private string DecodeUserId(string token)
    {
        // Simplified token decode (in production, use proper JWT validation library)
        // This is just a placeholder - implement proper JWT validation
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3) return null;

            // Decode payload (base64url)
            var payload = parts[1];
            var padding = 4 - (payload.Length % 4);
            if (padding != 4) payload += new string('=', padding);
            payload = payload.Replace('-', '+').Replace('_', '/');

            var decodedBytes = Convert.FromBase64String(payload);
            var json = System.Text.Encoding.UTF8.GetString(decodedBytes);

            // Parse JSON and extract sub claim
            // In production, use proper JSON parsing
            var subStart = json.IndexOf("\"sub\":\"") + 7;
            var subEnd = json.IndexOf("\"", subStart);
            return json.Substring(subStart, subEnd - subStart);
        }
        catch
        {
            return null;
        }
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
```

**DTOs (Request/Response Models):**

```csharp
// Location: Models/Users/RegisterUserRequest.cs
public class RegisterUserRequest
{
    /// <summary>User email address (unique identifier)</summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; }

    /// <summary>User password (minimum 8 characters)</summary>
    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string Password { get; set; }

    /// <summary>User full name (optional)</summary>
    public string FullName { get; set; }
}

// Location: Models/Users/LoginRequest.cs
public class LoginRequest
{
    [Required(ErrorMessage = "Email is required")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; }
}

// Location: Models/Users/LoginResponse.cs
public class LoginResponse
{
    /// <summary>JWT access token for API requests</summary>
    [Required]
    public string AccessToken { get; set; }

    /// <summary>Refresh token for obtaining new access tokens</summary>
    [Required]
    public string RefreshToken { get; set; }
}

// Location: Models/Users/UserResponse.cs
public class UserResponse
{
    public string UserId { get; set; }
    public string Email { get; set; }
    public string FullName { get; set; }
    public bool CanProvide { get; set; }
    public bool CanSubscribe { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### OpenAPI Best Practices

1. **Operation IDs**: Unique, kebab-cased identifiers for each endpoint
   ```csharp
   [OpenApiOperation(operationId: "create-user", tags: new[] { "Users" })]
   ```

2. **Parameter Documentation**: Every parameter documented with `Description`
   ```csharp
   [OpenApiParameter(name: "Authorization", In = ParameterLocation.Header, Required = true, Description = "JWT Bearer token")]
   ```

3. **Response Codes**: Document all possible HTTP status codes
   ```csharp
   [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(UserResponse))]
   [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = "Invalid input")]
   [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = "Missing or invalid JWT token")]
   ```

4. **Request Bodies**: Always specify type and content type
   ```csharp
   [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(LoginRequest))]
   ```

5. **DTOs with Descriptions**: Use XML documentation comments for model properties
   ```csharp
   /// <summary>User email address (unique identifier)</summary>
   [Required]
   public string Email { get; set; }
   ```

### Error Handling in Functions

**Consistent Error Response Format:**

```csharp
public class ErrorResponse
{
    public string Error { get; set; }
    public string Code { get; set; }  // e.g., "INVALID_REQUEST", "USER_NOT_FOUND"
    public string[] Details { get; set; }  // Additional context
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

**Error Handling Pattern:**

```csharp
try
{
    // Business logic
}
catch (ArgumentException ex)
{
    _logger.LogWarning(ex, "Validation error: {Message}", ex.Message);
    var response = req.CreateResponse(HttpStatusCode.BadRequest);
    await response.WriteAsJsonAsync(
        new ErrorResponse { Error = ex.Message, Code = "VALIDATION_ERROR" },
        cancellationToken);
    return response;
}
catch (InvalidOperationException ex)
{
    _logger.LogWarning(ex, "Business rule violation: {Message}", ex.Message);
    var response = req.CreateResponse(HttpStatusCode.BadRequest);
    await response.WriteAsJsonAsync(
        new ErrorResponse { Error = ex.Message, Code = "BUSINESS_RULE_VIOLATION" },
        cancellationToken);
    return response;
}
catch (NotFoundException ex)
{
    _logger.LogWarning(ex, "Resource not found: {Message}", ex.Message);
    var response = req.CreateResponse(HttpStatusCode.NotFound);
    await response.WriteAsJsonAsync(
        new ErrorResponse { Error = ex.Message, Code = "NOT_FOUND" },
        cancellationToken);
    return response;
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error");
    var response = req.CreateResponse(HttpStatusCode.InternalServerError);
    await response.WriteAsJsonAsync(
        new ErrorResponse { Error = "Internal server error", Code = "INTERNAL_ERROR" },
        CancellationToken.None);
    return response;
}
```

### JWT Validation Helper (Recommended Pattern)

Create a shared utility for JWT validation:

```csharp
// Location: Shared/Auth/JwtValidator.cs
public class JwtValidator
{
    private readonly IConfiguration _configuration;

    public JwtValidator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (bool IsValid, string UserId, string Error) ValidateToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            return (false, null, "Token is empty");

        try
        {
            var key = new System.IdentityModel.Tokens.Jwt.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]));
            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();

            var principal = tokenHandler.ValidateToken(token, new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out var validatedToken);

            var userId = principal.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
                return (false, null, "Token missing 'sub' claim");

            return (true, userId, null);
        }
        catch (Exception ex)
        {
            return (false, null, $"Token validation failed: {ex.Message}");
        }
    }
}
```

### Aggregate Endpoint Structure Summary

**One class per Aggregate:**
- All CRUD operations for that aggregate in one `{AggregateType}Functions.cs` class
- Related operations grouped logically within the class
- OpenAPI attributes on each function for documentation
- Consistent error handling and logging throughout
- JWT validation for protected endpoints
- DTOs separate from domain models

**Advantages:**
- Single responsibility: Each class owns all endpoints for one aggregate
- Discoverability: Easy to find all User endpoints in UserFunctions.cs
- Maintainability: Modification to one aggregate doesn't affect others
- OpenAPI generation: Automatic documentation from attributes
- Testing: Clear context for unit/integration tests

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

## 📖 Implementation Example: User Management Feature (2.1 ユーザー管理)

This section provides a complete implementation example of the User Management feature as specified in SYSTEM_DESIGN.md Section 2.1.

### 1. Domain Events (新規イベント定義)

**File**: `Domain/Users/Events/UserEvents.cs`

```csharp
// ユーザー情報更新イベント
public class UserInfoUpdatedEvent : AggregateEvent<UserAggregate, UserId>
{
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// 権限変更イベント
public class UserPermissionsChangedEvent : AggregateEvent<UserAggregate, UserId>
{
    public bool? CanProvide { get; set; }
    public bool? CanSubscribe { get; set; }
    public DateTime ChangedAt { get; set; }
}

// Admin 直接設定変更イベント
public class AdminDirectChangeAppliedEvent : AggregateEvent<UserAggregate, UserId>
{
    public UserId AdminUserId { get; set; } = null!;
    public string ChangeType { get; set; } = null!;  // BasicInfo, Permissions
    public Dictionary<string, object> OldValues { get; set; } = new();
    public Dictionary<string, object> NewValues { get; set; } = new();
    public string? Memo { get; set; }
    public DateTime ChangedAt { get; set; }
}
```

### 2. Aggregate Methods (新規メソッド)

**File**: `Domain/Users/UserAggregate.cs`

```csharp
public class UserAggregate : AggregateRoot<UserAggregate, UserId>
{
    public string Email { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public bool IsProviderEnabled { get; private set; }

    // ユーザー情報更新（本人またはAdmin）
    public void UpdateInfo(string? email, string? displayName)
    {
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("At least one field must be provided.");

        Emit(new UserInfoUpdatedEvent
        {
            Email = email,
            DisplayName = displayName,
            UpdatedAt = DateTime.UtcNow
        });
    }

    // 権限変更（Admin のみ）
    public void ChangePermissions(bool? canProvide, bool? canSubscribe)
    {
        if (canProvide == null && canSubscribe == null)
            throw new ArgumentException("At least one permission must be specified.");

        Emit(new UserPermissionsChangedEvent
        {
            CanProvide = canProvide,
            CanSubscribe = canSubscribe,
            ChangedAt = DateTime.UtcNow
        });
    }

    // Admin 直接設定変更
    public void AdminDirectChange(UserId adminUserId, string changeType,
        Dictionary<string, object> oldValues, Dictionary<string, object> newValues, string? memo = null)
    {
        if (adminUserId == null)
            throw new ArgumentException("Admin ID is required.", nameof(adminUserId));

        Emit(new AdminDirectChangeAppliedEvent
        {
            AdminUserId = adminUserId,
            ChangeType = changeType,
            OldValues = oldValues ?? new(),
            NewValues = newValues,
            Memo = memo,
            ChangedAt = DateTime.UtcNow
        });

        // Apply state changes
        switch (changeType.ToLower())
        {
            case "basicinfo":
                if (newValues.TryGetValue("email", out var email) && email is string emailStr)
                    Email = emailStr;
                break;
        }
    }

    // イベント適用
    private void Apply(UserInfoUpdatedEvent domainEvent)
    {
        if (!string.IsNullOrWhiteSpace(domainEvent.Email))
            Email = domainEvent.Email;
        if (!string.IsNullOrWhiteSpace(domainEvent.DisplayName))
            DisplayName = domainEvent.DisplayName;
    }

    private void Apply(AdminDirectChangeAppliedEvent domainEvent)
    {
        // Event recorded for audit trail
    }
}
```

### 3. Command と Handler

**File**: `Application/Users/Commands/UpdateUserInfoCommand.cs`

```csharp
public class UpdateUserInfoCommand : Command<UserAggregate, UserId>
{
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public UserId? RequestingAdminId { get; set; }  // Admin による変更の場合のみ設定
    public string? AdminMemo { get; set; }

    public UpdateUserInfoCommand(UserId aggregateId) : base(aggregateId) { }
}
```

**File**: `Application/Users/Commands/UpdateUserInfoCommandHandler.cs`

```csharp
public class UpdateUserInfoCommandHandler : CommandHandler<UserAggregate, UserId, UpdateUserInfoCommand>
{
    public override Task ExecuteAsync(UserAggregate aggregate, UpdateUserInfoCommand command, CancellationToken cancellationToken)
    {
        if (command.RequestingAdminId != null)
        {
            // Admin 直接設定変更の場合
            var oldValues = new Dictionary<string, object>
            {
                { "email", aggregate.Email },
                { "displayName", aggregate.DisplayName }
            };

            var newValues = new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(command.Email))
                newValues["email"] = command.Email;
            if (!string.IsNullOrWhiteSpace(command.DisplayName))
                newValues["displayName"] = command.DisplayName;

            aggregate.AdminDirectChange(
                command.RequestingAdminId,
                "BasicInfo",
                oldValues,
                newValues,
                command.AdminMemo);
        }
        else
        {
            // ユーザー自身による更新
            aggregate.UpdateInfo(command.Email, command.DisplayName);
        }

        return Task.CompletedTask;
    }
}
```

### 4. Authorization Service

**File**: `Infrastructure/Services/IAuthorizationService.cs`

```csharp
public interface IAuthorizationService
{
    bool IsAdmin(UserId requestingUserId);
    bool CanManageUser(UserId requestingUserId, UserId targetUserId);
    bool CanPerformAdminActions(UserId requestingUserId);
}
```

**File**: `Infrastructure/Services/AuthorizationService.cs`

```csharp
public class AuthorizationService : IAuthorizationService
{
    private readonly HashSet<UserId> _admins = new();

    public bool IsAdmin(UserId requestingUserId) => _admins.Contains(requestingUserId);

    public bool CanManageUser(UserId requestingUserId, UserId targetUserId)
    {
        // ユーザーは自分自身のみ管理可能、Admin は全員管理可能
        return requestingUserId.Equals(targetUserId) || IsAdmin(requestingUserId);
    }

    public bool CanPerformAdminActions(UserId requestingUserId)
    {
        return IsAdmin(requestingUserId);
    }
}
```

### 5. Audit Log Service

**File**: `Infrastructure/Services/IAuditLogService.cs`

```csharp
public interface IAuditLogService
{
    Task LogUserActionAsync(UserId userId, string action, string resource, 
        string? resourceId = null, Dictionary<string, object>? details = null,
        CancellationToken cancellationToken = default);

    Task LogAdminDirectChangeAsync(UserId adminUserId, UserId targetUserId, string action,
        string resource, string? resourceId = null, Dictionary<string, object>? details = null,
        string? memo = null, CancellationToken cancellationToken = default);
}
```

### 6. API Functions

**File**: `Functions/Users/AdminUserManagementFunction.cs`

```csharp
public class AdminUserManagementFunction
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IAuditLogService _auditLogService;

    // Admin: PUT /api/admin/users/{userId}/permissions - 権限変更
    [Function("AdminChangePermissions")]
    public async Task<IActionResult> ChangePermissions(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "admin/users/{userId}/permissions")] HttpRequest req,
        string userId)
    {
        var adminUserId = ExtractUserIdFromToken(req);
        if (adminUserId == null || !_authorizationService.CanPerformAdminActions(adminUserId))
            return new ForbidResult();

        var body = await req.ReadAsJsonAsync<ChangePermissionsRequest>();
        var targetUserId = new UserId(userId);

        var changes = new Dictionary<string, object>();
        if (body.CanProvide.HasValue)
            changes["canProvide"] = body.CanProvide.Value;

        await _auditLogService.LogAdminDirectChangeAsync(
            adminUserId,
            targetUserId,
            "PermissionsChanged",
            "User",
            userId,
            changes,
            body.Memo);

        return new OkObjectResult(new { message = "Permissions updated successfully" });
    }

    private UserId? ExtractUserIdFromToken(HttpRequest req)
    {
        var authHeader = req.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authHeader))
            return null;

        var token = authHeader.Replace("Bearer ", "");
        return new UserId($"user-{token.GetHashCode()}");
    }
}
```

### 7. Dependency Injection

**File**: `Program.cs`

```csharp
builder.Services
    .AddSingleton<IAuthorizationService, AuthorizationService>()
    .AddSingleton<IAuditLogService, InMemoryAuditLogService>();
```

---

