using starterapi.DTOs;

public class UserAccessDTO
{
    public Guid UserId { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public List<UserRoleAccessDTO> Roles { get; set; }
}

public class UserRoleAccessDTO
{
    public int RoleId { get; set; }
    public string RoleName { get; set; }
    public List<RoleModulePermissionDTO> Permissions { get; set; }
} 