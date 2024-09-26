using System.Text.Json.Serialization;

namespace starterapi;

public class Module
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<ModuleAction> Actions { get; set; }
}

public class ModuleAction
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int ModuleId { get; set; }
    public Module Module { get; set; }
    public ICollection<Role> Roles { get; set; }
}


public class Role
{
    public int Id { get; set; }
    public string Name { get; set; }
    [JsonIgnore]
    public ICollection<UserRole> UserRoles { get; set; }
    public ICollection<ModuleAction> AllowedActions { get; set; } = new List<ModuleAction>();

    public Role()
    {
        UserRoles = new List<UserRole>();
        AllowedActions = new List<ModuleAction>();
    }
}