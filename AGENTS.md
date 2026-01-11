# Kopitra System - AI Agent Guidelines

This document provides a comprehensive guide for AI agents working on the Kopitra FX copy trading platform implementation.

---

## 📋 Project Overview

**Kopitra** is a sophisticated FX copy trading platform that enables experienced traders (providers) to distribute trading signals that are automatically executed on subscriber accounts. The system is designed with cost optimization, event sourcing, and polling-based architecture as core principles.

### Core Purpose
- Distribute trading signals from skilled providers to multiple subscribers
- Automatically synchronize trades across multiple accounts based on funding strategies
- Provide comprehensive analytics and performance tracking
- Support both automated (EA) and manual signal distribution
- Enable administrator oversight with impersonation and direct management capabilities

---

## 🏗️ Architecture Principles

### Cost-Optimized Design
- **Single Azure Functions Endpoint**: All EA operations go through one unified endpoint (`/api/ea/*`)
- **SQL-Based Messaging**: No complex Message Queues or Event Hubs; use Azure SQL Database for message persistence
- **Polling Architecture**: EAs periodically poll for signals rather than using pub/sub
- **Batch Metrics Processing**: Non-real-time analytics calculated nightly to reduce costs
- **Single SQL Database**: Initial design uses one database; domain separation planned for future scaling

### Event Sourcing Foundation
- **Append-Only Event Store**: All domain changes logged in `DomainEvent` table
- **Snapshots**: Periodic state snapshots cached for performance optimization
- **Message Queues**: SQL-based tables (`SignalMessage`, `ExecutionMessage`) for reliability
- **Immutable History**: Complete audit trail with exact state reconstruction capability

### EA Integration Pattern
```
Polling Flow:
1. EA calls GET /api/ea/signals?accountId=XXX (configurable 1-5 second intervals)
2. Azure Functions returns pending signals from SignalMessage table
3. EA executes orders locally
4. EA calls POST /api/ea/executions with execution confirmation
5. Azure Functions updates ExecutionMessage with ACK status
6. EA marks message as processed
```

---

## 🎯 Key Features

### User Management
- **JWT-based authentication** with Admin and User roles
- **RBAC**: Admin can impersonate users OR directly modify settings
- **Audit Logging**: All operations logged with action type distinction
- **UI Separation**: Admin site and User site using shared APIs with permission control

### Signal Distribution
- **Multiple channels**: EA auto-distribution, web manual distribution, admin delegation
- **Idempotency**: Signal ID-based duplicate prevention
- **Flexible position sizing**: Actual lots or percentage-based positioning
- **SL/TP Support**: Stop loss and take profit configuration

### Signal Execution
- **Funding Strategies**: Fixed lot, proportional, percentage-based
- **Position Synchronization**: Modify and close signals automatically update subscriber positions
- **Execution Delay Tolerance**: Configurable timeouts for order acceptance
- **Execution Confirmation**: EA-based confirmation with timeout handling

### Analytics & Metrics
- **Signal-Level Metrics**: Distribution count, execution rate, P&L, win rate
- **Provider Metrics**: Daily/weekly/monthly performance tracking
- **Subscriber Account Metrics**: Per-account copy trading performance
- **User Metrics**: Integrated performance for both provider and subscriber roles
- **Rankings**: Top provider rankings by win rate, profit, ROI
- **Batch Calculation**: Nightly batch jobs compute all metrics (non-real-time)

### Admin Management
- **Impersonation**: Directly act as any user without their credentials
- **Direct Settings Management**: Change user configurations, account settings, subscriptions
- **Audit Trail**: Complete tracking of all admin operations
- **Permission Management**: Grant/revoke provider and subscriber rights

---

## 📂 Project Structure

```
kopitra2/
├── AGENTS.md                          # This file - overall AI agent guidelines
├── docs/
│   └── SYSTEM_DESIGN.md               # Complete system specification (Japanese)
├── frontend/                          # React + TypeScript frontend applications
│   ├── AGENTS.md                      # Frontend-specific implementation guidelines
│   ├── admin-site/                    # Admin dashboard (admin.kopitra.com)
│   └── user-site/                     # User dashboard (app.kopitra.com)
├── functions/                         # Azure Functions backend
│   └── AGENTS.md                      # Backend implementation guidelines
└── infrastructure/                    # Infrastructure as Code
    └── AGENTS.md                      # Infrastructure deployment guidelines
```

---

## 🔄 Specification Change Process

### CRITICAL: Always Update SYSTEM_DESIGN.md When Specifications Change

Whenever you modify the system design, API contracts, data models, or any architectural decision:

1. **Update SYSTEM_DESIGN.md** in the `docs/` folder
   - Modify the relevant section (System Architecture, API Design, Data Model, etc.)
   - Update any diagrams or examples
   - Ensure consistency across all sections

2. **Document the change reason** in a git commit message
   - Include WHAT changed and WHY
   - Reference which section(s) were modified

3. **Notify related implementation areas**
   - If API contracts change, update both `functions/AGENTS.md` and `frontend/AGENTS.md`
   - If data models change, ensure database schema documentation is updated
   - If architecture changes, verify deployment templates match

4. **Maintain consistency** across documents
   - SYSTEM_DESIGN.md is the source of truth
   - All implementation documents reference this spec
   - No conflicting information in different documents

### Example Specification Changes
- Adding new API endpoint → Update Section 6 (API Design)
- Modifying data model → Update Section 5 (Data Model)
- Changing EA polling interval → Update Section 4.1 (Architecture Features)
- Adding new metrics → Update Section 2.6 (Analytics Features)

---

## 🛠️ Development Guidelines

### Core Technologies
- **Backend**: C# (.NET 6+) on Azure Functions
- **Database**: Azure SQL Database with event sourcing pattern
- **Frontend**: React + TypeScript
- **API Gateway**: Azure API Management
- **Authentication**: JWT tokens with Azure AD integration
- **Infrastructure**: Bicep templates for IaC

### Code Organization
- **Shared Types**: Define DTOs and domain models in `functions/shared/`
- **Business Logic**: Implement in `functions/services/`
- **Data Access**: Keep database operations in `functions/data/`
- **API Handlers**: Minimal logic in function implementations
- **Testing**: Unit tests alongside source files

### AGENTS.md Content Policy
- **Full code examples required**: When AGENTS.md or any AGENTS.* guidance contains sample implementations or patterns, include complete, copy-paste runnable code snippets (types, method bodies, and any registration or wiring needed). AI agents must not leave only skeletons; provide full working examples consistent with the repository's conventions.

### API Design Principles
- **Unified Base Path**: All endpoints follow RESTful conventions
- **Permission Control**: Always verify user rights in API layer
- **Idempotency**: Use request/entity IDs for duplicate prevention
- **Error Handling**: Consistent error response format
- **Audit Logging**: Log all state-changing operations

### Database Patterns
- **Event Sourcing**: All domain events recorded in append-only store
- **Snapshots**: Periodically cache aggregate state
- **Message Persistence**: Use SQL tables for signal/execution queues
- **Metrics Caching**: Store calculated metrics for fast retrieval
- **No Soft Deletes**: Use status flags instead of actual deletion

### Implementation Checklist
- [ ] Update SYSTEM_DESIGN.md if requirements change
- [ ] Implement API with proper error handling
- [ ] Add comprehensive error responses
- [ ] Include audit logging for state changes
- [ ] Verify idempotency where applicable
- [ ] Document API contracts (swagger/OpenAPI)
- [ ] Add unit tests
- [ ] Consider scaling implications
- [ ] Update database schema if needed

---

## 🔐 Security Considerations

### Authentication & Authorization
- All endpoints require JWT token validation
- Role-based access control enforced at API layer
- Resource ownership verified (e.g., user can only access own data)
- Admin operations require explicit Admin role

### Data Protection
- All API communication via HTTPS/TLS 1.2+
- Sensitive data (API keys) stored encrypted in Key Vault
- Passwords hashed with bcrypt or equivalent
- No plaintext passwords in logs or configurations

### Admin Operations
- Admin impersonation logged with action timestamp
- Direct setting changes tracked with before/after values
- All admin actions auditable by timestamp and user ID
- Consider implementing approval workflows for critical changes

---

## 📊 Key Concepts

### Signal Idempotency
- Each signal has a unique `signalId`
- Duplicate signals with same ID are detected and ignored
- Prevents double execution from network retries
- Essential for reliable distributed system

### Message Lifecycle
```
Pending → Dispatched → Acknowledged → Deleted
```
- **Pending**: Signal ready for delivery
- **Dispatched**: Sent to subscriber, awaiting execution
- **Acknowledged**: EA confirmed execution
- **Deleted**: Message cleanup after ACK received

### Funding Strategies
- **FixedLot**: Use exact position size from provider
- **Proportional**: Adjust based on account balance ratio
- **Percentage**: Calculate from subscriber account percentage

### Metrics Collection
- **Real-time**: Signals and executions recorded immediately
- **Batch Processing**: Nightly job aggregates metrics
- **Caching**: Calculated metrics stored in database
- **Queries**: API returns cached metrics (not real-time recalculation)

---

## 🚀 Development Workflow

### Creating a New Feature
1. **Check SYSTEM_DESIGN.md** for existing specification
2. **If not specified**, propose changes and update SYSTEM_DESIGN.md first
3. **Implement backend** in `functions/` folder
4. **Implement frontend** in `frontend/` folder
5. **Update infrastructure** if new Azure resources needed
6. **Write tests** for critical business logic
7. **Document changes** in relevant AGENTS.md files

### Adding a New API Endpoint
1. **Document in SYSTEM_DESIGN.md** Section 6 (API Design)
   - Include HTTP method, path, parameters, response format
   - Specify permission requirements
2. **Create in functions/api/** with proper routing
3. **Implement authentication** and authorization checks
4. **Add to Swagger/OpenAPI** documentation
5. **Create corresponding frontend** form/component if needed
6. **Test** with various user roles and permissions

### Database Schema Changes
1. **Update SYSTEM_DESIGN.md** Section 5 (Data Model)
2. **Create migration script** for existing databases
3. **Update data access layer** in functions/data/
4. **Verify backward compatibility** if applicable
5. **Test migration** against sample data

---

## 🧪 Testing Strategy

### Unit Tests
- Test business logic in isolation
- Mock database and external dependencies
- Cover happy path and error cases
- Verify idempotency where applicable

### Integration Tests
- Test API endpoints with real(or mocked) database
- Verify permissions and authentication
- Test signal distribution flows
- Validate metrics calculation

### E2E Tests
- Test complete user journeys (frontend + backend)
- Verify admin operations
- Test EA polling and execution flows
- Monitor for performance bottlenecks

---

## 📝 Implementation Notes

### EA Integration
- EAs should handle network retries gracefully
- Use exponential backoff for failed requests
- Store signals locally until acknowledged
- Implement timeout handling for unresponsive servers

### Metrics Computation
- Batch jobs run nightly (configurable schedule)
- Aggregate by signal, provider, account, and user
- Calculate: P&L, win rate, ROI, max drawdown
- Handle edge cases (no trades, partial fills)

### Scaling Considerations
- Azure Functions auto-scales based on polling load
- SQL Database connection pooling essential
- Message retention: keep for configured period, then delete
- Consider read replicas for metrics queries

---

## 🔗 Related Documentation

- **System Design**: `docs/SYSTEM_DESIGN.md` (source of truth)
- **Frontend Guidelines**: `frontend/AGENTS.md`
- **Backend Guidelines**: `functions/AGENTS.md`
- **Infrastructure Guidelines**: `infrastructure/AGENTS.md`
- **Database Schema**: `docs/DATABASE_SCHEMA.md` (when created)
- **API Specification**: `docs/API_SPECIFICATION.md` (when created)
- **EA Development Guide**: `docs/EA_DEVELOPMENT_GUIDE.md` (when created)

---

## ✅ Pre-Implementation Checklist

Before starting any feature implementation:

- [ ] SYSTEM_DESIGN.md contains specification for this feature
- [ ] API contracts defined in Section 6
- [ ] Data model changes specified in Section 5
- [ ] Permission requirements documented
- [ ] Idempotency strategy defined (if applicable)
- [ ] Error scenarios considered
- [ ] Related AGENTS.md files reviewed
- [ ] Database migration strategy planned
- [ ] Tests planned before implementation

---

## 📞 Quick Reference

### Key Files
| File | Purpose |
|------|---------|
| SYSTEM_DESIGN.md | Source of truth for all specifications |
| AGENTS.md (root) | This file - overall guidelines |
| functions/AGENTS.md | Backend implementation specifics |
| frontend/AGENTS.md | Frontend implementation specifics |
| infrastructure/AGENTS.md | Deployment and infrastructure specifics |

### Important Concepts
| Concept | Key Point |
|---------|-----------|
| Idempotency | Use signal ID to prevent duplicates |
| Event Sourcing | All changes logged in append-only store |
| Polling | EAs pull signals periodically |
| Cost Optimization | Single Functions endpoint, SQL-based messaging |
| Batch Metrics | Calculated nightly, not real-time |

### Common Tasks
| Task | Where to Start |
|------|----------------|
| Add new API | Update SYSTEM_DESIGN.md Section 6, then implement in functions/api/ |
| Change data model | Update SYSTEM_DESIGN.md Section 5, create migration |
| New feature | Spec in SYSTEM_DESIGN.md first, implement in relevant folder |
| Admin feature | Document in UC11-15, implement in functions/admin/ |
| Metrics | Document in Section 2.6, implement batch job |

---

**Last Updated**: January 11, 2026

**Remember**: SYSTEM_DESIGN.md is the single source of truth. Always keep it updated when specifications change.
