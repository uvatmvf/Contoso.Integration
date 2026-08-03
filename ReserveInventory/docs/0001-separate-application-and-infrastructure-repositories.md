# ADR-0001: Separate application and infrastructure repositories

- Status: Accepted
- Date: 2026-08-03

## Context

The application code and Terraform infrastructure change for different reasons and at different rates. Application developers need to build, test, and deploy Function code without reapplying infrastructure. Platform changes need independent review, Terraform planning, and state management.

A monorepo would simplify atomic changes across code and infrastructure, but it would also couple two delivery lifecycles and broaden repository permissions.

## Decision

Maintain two repositories:

- `Contoso.Integration` owns Function source, tests, application packaging, and application deployment.
- `contoso-order-platform` owns Terraform bootstrap, remote state, platform composition, reusable modules, managed identities, RBAC, and Azure resources.

The infrastructure repository creates deployment targets and environment configuration. The application repository deploys the same compiled artifact to those targets.

## Consequences

### Positive

- Application and infrastructure have clear ownership boundaries.
- Each repository has focused CI/CD workflows and permissions.
- Application releases do not require Terraform execution.
- Infrastructure can evolve without rebuilding application binaries.
- The split mirrors common platform-team and product-team responsibilities.

### Tradeoffs

- Cross-repository changes require coordination.
- Version compatibility between application expectations and infrastructure configuration must be documented.
- A deployment may require sequencing infrastructure first, then application code.

## Follow-up

Document required app settings and module outputs as explicit contracts between the repositories.
