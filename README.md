# Starter API

## Overview

This project is an ASP.NET Core Web API that implements a role-based access control (RBAC) system. It provides a secure, scalable, and maintainable foundation for building enterprise-level APIs.

## Features

- **Authentication**: JWT-based authentication system.
- **Authorization**: Custom role-based access control (RBAC) system.
- **User Management**: CRUD operations for user accounts.
- **Role Management**: Create roles and assign permissions.
- **API Versioning**: Support for multiple API versions.
- **Rate Limiting**: Protect the API from abuse and ensure fair usage.
- **Logging**: Advanced structured logging using Serilog.
- **Database**: Entity Framework Core with SQL Server.
- **Swagger Documentation**: API documentation and testing interface.

## Prerequisites

- .NET 7.0 SDK or later
- SQL Server
- (Optional) Redis for distributed caching

## Setup

1. Clone the repository:
   ```
   git clone https://github.com/yourusername/starterapi.git
   cd starterapi
   ```

2. Update the connection string in `appsettings.json` to point to your SQL Server instance.

3. Apply database migrations:
   ```
   dotnet ef database update
   ```

4. Run the application:
   ```
   dotnet run
   ```

5. Access Swagger UI at `https://localhost:5001/swagger` (the port might be different on your machine).

## Project Structure

- `Controllers/`: API endpoints
- `Models/`: Data models and DTOs
- `Services/`: Business logic and data access
- `Middleware/`: Custom middleware components
- `Migrations/`: Database migrations

## Key Components

### Authentication

JWT-based authentication is implemented. Users can obtain a token by sending their credentials to the `/api/Auth/login` endpoint.

### Authorization

Custom RBAC is implemented using policy-based authorization. Permissions are defined at the action level using the `[Permission]` attribute.

### Rate Limiting

Basic rate limiting is implemented using ASP.NET Core's built-in rate limiting middleware. It's configured in `Program.cs`.

### Logging

Serilog is used for structured logging. Logs are written to both console and file. The `RequestLoggingMiddleware` logs details of each HTTP request.

## Configuration

Key configuration options are available in `appsettings.json`:

- `ConnectionStrings`: Database connection string
- `JwtSettings`: JWT token configuration
- `Serilog`: Logging configuration
- `ClientRateLimiting`: Rate limiting rules

## API Versioning

The API supports versioning. Different versions of the API can be accessed by including the version in the URL, e.g., `/api/v1/users` or `/api/v2/users`.

## Contributing

Please read `CONTRIBUTING.md` for details on our code of conduct and the process for submitting pull requests.

## License

This project is licensed under the MIT License - see the `LICENSE.md` file for details.