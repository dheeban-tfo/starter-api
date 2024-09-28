# Permission System Documentation

## Overview
Our application uses a role-based access control (RBAC) system combined with fine-grained permissions. This system allows us to control access to various modules and actions within the application.

## Key Components

1. **Roles**: Predefined sets of permissions (e.g., Admin, User, Manager)
2. **Modules**: Distinct sections of the application (e.g., CommunityManagement, UserManagement)
3. **Actions**: Specific operations within modules (e.g., Read, Write, Delete)
4. **Permissions**: Combinations of modules and actions (e.g., CommunityManagement.Read)

## Implementation

### 1. Permission Attribute
We use a custom `PermissionAttribute` to decorate controller actions:

[Permission(nameof(ModuleActions.CommunityManagement.Read))]
public async Task<ActionResult<CommunityStatisticsDto>> GetCommunityStatistics()
{
// Method implementation
}

### 2. Permission Policy
Our application uses a "PermissionPolicy" which is enforced through the `AuthorizationHandler<PermissionRequirement>` class.

### 3. User Permissions
User permissions are stored in the database and cached for performance. They are loaded when a user logs in.

## How to Add New Permissions

1. Define the new permission in the `ModuleActions` class.
2. Add the permission to the relevant roles in the database.
3. Use the `PermissionAttribute` on the controller action that should be protected.

## Best Practices

1. Always use the `PermissionAttribute` for actions that require specific permissions.
2. Keep the `ModuleActions` class up-to-date with all available permissions.
3. Regularly review and audit user roles and permissions.
4. Use the least privilege principle: assign the minimum permissions necessary for each role.

## Troubleshooting

If a user is unable to access a resource they should have permission for:
1. Check the user's assigned role in the database.
2. Verify that the role has the necessary permission.
3. Ensure that the controller action is decorated with the correct `PermissionAttribute`.
4. Check the logs for any authorization failures.

## Future Improvements

- Implement a user interface for managing roles and permissions.
- Add support for temporary permissions or permission delegation.
- Implement more granular permissions at the entity level.