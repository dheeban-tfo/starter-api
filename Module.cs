namespace starterapi;

public class Module
{
    public int Id { get; set; }
    public string Name { get; set; } // e.g., "Product Management", "User Management"
    
    public ICollection<RoleModulePermission> RoleModulePermissions { get; set; }
}
