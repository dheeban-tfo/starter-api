# Starter API

## Overview

Starter API is a robust, scalable ASP.NET Core Web API project that implements a role-based access control (RBAC) system. It provides a secure foundation for building enterprise-level APIs with features such as authentication, authorization, audit logging, and health monitoring.

## Features

- **Authentication**: JWT-based authentication system.
- **Authorization**: Custom role-based access control (RBAC) system.
- **User Management**: CRUD operations for user accounts.
- **Role Management**: Create roles and assign permissions.
- **Audit Logging**: Comprehensive logging of user activities and system changes.
- **Health Checks**: Endpoints to monitor the health of the API and its dependencies.
- **API Versioning**: Support for multiple API versions.
- **Swagger Documentation**: Interactive API documentation and testing interface.
- **Entity Framework Core**: ORM for database operations with SQL Server.
- **Password Reset**: Secure password reset functionality with email notifications.
- **Error Handling**: Global exception handling and consistent error responses.

## Prerequisites

- .NET 7.0 SDK or later
- SQL Server
- SMTP server for email notifications (e.g., SendGrid, Mailjet)

## Setup

1. Clone the repository:
   ```
   git clone https://github.com/yourusername/starterapi.git
   cd starterapi
   ```

2. Update the connection string in `appsettings.json` to point to your SQL Server instance.

3. Update the email service configuration in `appsettings.json` with your SMTP server details.

4. Apply database migrations:
   ```
   dotnet ef database update
   ```

5. Run the application:
   ```
   dotnet run
   ```

6. Access Swagger UI at `https://localhost:5001/swagger` (the port might be different on your machine).

## Project Structure

- `Controllers/`: API endpoints
- `Models/`: Data models and DTOs
- `Services/`: Business logic and data access
- `Middleware/`: Custom middleware components
- `Filters/`: Action filters for cross-cutting concerns
- `Migrations/`: Database migrations

## Key Components

### Authentication

JWT-based authentication is implemented. Users can obtain a token by sending their credentials to the `/api/Auth/login` endpoint.

### Authorization

Custom RBAC is implemented using policy-based authorization. Permissions are defined at the action level using the `[Permission]` attribute.

### Audit Logging

The system automatically logs all database changes. The `AuditLogService` provides methods to query the audit logs.

### Health Checks

Health check endpoints (`/health`, `/health/ready`, `/health/live`) are available to monitor the status of the API and its dependencies.

## API Versioning

The API supports versioning. Different versions of the API can be accessed by including the version in the URL, e.g., `/api/v1/users` or `/api/v2/users`.

## Configuration

Key configuration options are available in `appsettings.json`:

- `ConnectionStrings`: Database connection string
- `Jwt`: JWT token configuration
- `Mailjet`: Email service configuration
- `Logging`: Logging configuration

## Development

To run the project in development mode:

```
dotnet run --environment Development
```

This will enable additional features like detailed error messages and Swagger UI.

## Testing

To run the unit tests:

```
dotnet test
```

## Deployment

For production deployment:

1. Update `appsettings.Production.json` with production-specific settings.
2. Publish the application:
   ```
   dotnet publish -c Release
   ```
3. Deploy the published files to your hosting environment.

## Contributing

Please read `CONTRIBUTING.md` for details on our code of conduct and the process for submitting pull requests.

## License

This project is licensed under the MIT License - see the `LICENSE.md` file for details.

## Acknowledgments

- ASP.NET Core team for the excellent framework
- All contributors who have helped shape this project
What you've implemented covers the basic requirements for user and role management with permissions. However, there are a few things that might be missing or could be improved:

1. Password reset functionality: There's no endpoint for users to reset their passwords.
2. Email verification: If you want to verify user emails, you might need additional endpoints.
3. Refresh token: There's no endpoint to refresh the JWT token, which is useful for maintaining user sessions.
4. User search or filtering: The current GET /api/v1/User endpoint doesn't seem to have parameters for searching or filtering users.
5. Pagination: For endpoints that return lists (like users or roles), you might want to add pagination to handle large datasets efficiently.
6. User self-registration: While you can create users, there's no specific endpoint for user self-registration (which might have different logic than admin user creation).
7. Multi-factor authentication: If you want to add an extra layer of security, you might consider implementing MFA.
8. Account lockout: Implement account lockout after a certain number of failed login attempts.
9. Audit logging: Add endpoints to view logs of user activities and system changes.
10. Health checks: While you mentioned implementing health checks, I don't see an endpoint for it in the Swagger documentation. You might want to add /health endpoints.

Remember, the necessity of these additional features depends on your specific requirements and use cases. The current implementation provides a solid foundation for basic user and role management with permissions.

dotnet ef migrations add InitialTenantManagementMigration -c TenantManagementDbContext
dotnet ef migrations add InitialTenantMigration -c TenantDbContext


---new

dotnet ef migrations add InitialTenantManagementMigration -c TenantManagementDbContext

dotnet ef migrations add InitialTenantMigration -c TenantDbContext  