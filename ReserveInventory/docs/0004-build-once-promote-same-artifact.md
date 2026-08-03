# ADR-0004: Build once and promote the same artifact

- Status: Accepted
- Date: 2026-08-03

## Context

Publishing directly from Visual Studio is convenient for development but does not provide a repeatable release record or guarantee that different environments receive identical binaries.

Rebuilding separately for dev and stage risks environmental drift and makes it harder to prove which code was tested.

## Decision

Use GitHub Actions to:

1. Restore, build, and test the .NET solution.
2. Run `dotnet publish` once.
3. Package the publish output as a ZIP artifact.
4. Deploy the artifact to dev.
5. Promote the exact same artifact to stage.

Use GitHub Environments for target-specific variables and approvals. Authenticate to Azure with OIDC federation through the `GitHub-Contoso-Integration` app registration.

## Consequences

### Positive

- Dev and stage receive identical binaries.
- Every deployment maps to a Git commit and workflow run.
- No Visual Studio publish profile or Azure client secret is required.
- Environment configuration remains external to the artifact.
- Stage approvals can be added without changing build logic.

### Tradeoffs

- Failed stage deployment requires diagnosing environment configuration rather than rebuilding.
- Artifact retention and release metadata must be managed.
- Rollback requires retaining or reproducing a known-good artifact.

## Follow-up

Add deployment verification, integration tests, and a controlled production promotion path.
