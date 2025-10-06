# Azure Functions Contribution Guide

These instructions apply to the entire `functions` directory, including nested content.

## Documentation
- Author comments and docs in English.
- Capture any outstanding follow-up work in repository TODO lists so the team can track action items.

## Required Checks
Before submitting changes under `functions/`, run:

- `dotnet format --verify-no-changes`
- `dotnet format analyzers --verify-no-changes`
- `dotnet build functions/Functions.sln`
- `dotnet test functions/Functions.sln`
