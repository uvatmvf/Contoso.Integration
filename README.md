
# Contoso.Integration

Contoso.Integration is a .NET 10 solution that contains integration components and functions used to communicate between Contoso systems and external services using Azure ServiceBus, Durable Queues, Azure Logic Apps & Azure Functions. The projects implement inventory reservation and release workflows, background jobs, and supporting libraries.

## Prerequisites

- .NET 10 SDK
- Visual Studio 2022/2026 (or newer) or `dotnet` CLI
- PowerShell (Windows) or a compatible shell

## Getting started

1. Clone the repository:

   git clone https://github.com/uvatmvf/Contoso.Integration.git

2. Open the solution in Visual Studio:

   - Open `Contoso.Integration.slnx` in Visual Studio

   or build and run from the command line:

   dotnet restore
   dotnet build Contoso.Integration.slnx

3. Run projects

   - Use Visual Studio to run specific startup projects
   - Or use `dotnet run --project <path-to-project>` from the solution root

## Solution structure

- ReserveInventory/Functions - Azure Functions or serverless-style functions for inventory reservation and release (example: ReleaseInventory.cs)
- (Other projects) - Supporting libraries, tests, and integration components

Note: Project folders and names may vary. Use the Solution Explorer in Visual Studio to inspect all projects.

### Architecture

✅ HTTP entry point (Logic App)

✅ Durable command queue

✅ Function triggered from Service Bus

✅ Azure Table state store

✅ Idempotent operations

✅ Command publisher abstraction

✅ Service Bus Topic

✅ Domain event publisher

✅ Topic subscription

✅ Function subscribing to an event
### Flow
HTTP Logic App
      │
      ▼
Service Bus (place-order)
      │
      ▼
ProcessPlaceOrder
    │
    ├── Create order state
    └── Publish ReserveInventory
              │
              ▼
ReserveInventory
    │
    ├── Reserve stock
    ├── Update state
    └── Publish InventoryReserved
              │
              ▼
AuthorizePayment
    │
    ├── Load order state
    ├── Authorize payment
    ├── Update state
    └── (next) Publish PaymentAuthorized

## Building and testing

- Build the entire solution:

  dotnet build

- Run tests (if present):

  dotnet test

## Contributing

- Open issues or submit pull requests against the `master` branch
- Follow repository coding standards and run tests before submitting

## License

This repository does not include a license file. Add a LICENSE file if you intend to make the project public under a specific license.

## Contact

## POC Features
- Saga orchestration layer with compensating transactions
- Azure functions for unit of work
- HTTP request entry point

### Synchronous HTTP Request Flow
<img width="1699" height="977" alt="image" src="https://github.com/user-attachments/assets/5ceb73a3-5f34-459b-9df4-ca44b8aeab4c" />
