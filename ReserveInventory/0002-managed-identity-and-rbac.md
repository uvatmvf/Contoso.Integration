# ADR-0002: Use managed identity and Azure RBAC

- Status: Accepted
- Date: 2026-08-03

## Context

The initial implementation used Service Bus and Storage connection strings. Connection strings introduce long-lived secrets that must be stored, distributed, rotated, and protected across local development, Azure configuration, and CI/CD.

Azure Functions, Service Bus, and Table Storage support Microsoft Entra authentication and Azure RBAC.

## Decision

Use system-assigned managed identity for deployed Function Apps and `DefaultAzureCredential` in application code.

Grant the Function App identity only the data-plane roles it requires:

- `Azure Service Bus Data Receiver`
- `Azure Service Bus Data Sender`
- `Storage Table Data Contributor`

Configure resource endpoints rather than credentials:

```text
ServiceBusConnection__fullyQualifiedNamespace
OrderStateStorage__tableEndpoint
```

Use GitHub OIDC federation for deployment authentication rather than client secrets or publish profiles.

## Consequences

### Positive

- No Service Bus or Table Storage secrets are stored by the application.
- Azure rotates and issues credentials automatically.
- The same application code runs locally and in Azure through `DefaultAzureCredential`.
- Access is auditable through Azure role assignments.
- Permissions can be scoped to individual Azure resources.

### Tradeoffs

- RBAC propagation can delay initial deployments or tests.
- Local developers need suitable Azure data-plane roles.
- Troubleshooting shifts from secret validity to identity, scope, and authorization analysis.

## Follow-up

Remove unused legacy connection-string settings after migration validation and periodically review role assignment scope.
