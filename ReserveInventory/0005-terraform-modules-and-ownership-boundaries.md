# ADR-0005: Use Terraform modules for cohesive platform capabilities

- Status: Accepted
- Date: 2026-08-03

## Context

The first Terraform implementation defined Azure resources directly in the root platform configuration. As the environment grew, Service Bus, Storage, and Function hosting each accumulated related resources, inputs, outputs, naming rules, security defaults, and dependencies.

Copying those resource blocks for additional environments would create duplication and configuration drift.

## Decision

Organize Terraform into three layers:

```text
infra/bootstrap
  Creates the remote-state resource group, storage account, and container.

infra/modules
  Implements reusable capabilities such as Service Bus, Storage, and Function hosting.

infra/platform
  Composes capabilities for an environment and connects their outputs through RBAC and app settings.
```

Modules expose explicit variables and outputs. The root platform configuration owns environment composition and relationships between modules.

Use Terraform `moved` blocks when refactoring existing resources into modules so state addresses change without recreating Azure resources.

## Consequences

### Positive

- Root Terraform reads as an architectural composition.
- Common defaults and Azure-specific implementation details are centralized.
- Modules can be reused across environments.
- Outputs provide stable contracts between capabilities.
- Existing infrastructure can be safely refactored through state moves.

### Tradeoffs

- Modules add indirection and require clear interfaces.
- Over-modularization can make simple resources harder to understand.
- Module changes can affect multiple callers and require versioning discipline.

## Follow-up

Keep modules capability-oriented rather than creating a module for every individual resource. Add validation and documentation as interfaces mature.
