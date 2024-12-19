

namespace starterapi.Modules.Extensions;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class ModuleAttribute : Attribute
{
    public ModuleName Name { get; }

    public ModuleAttribute(ModuleName name)
    {
        Name = name;
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class PermissionAttribute : Attribute
{
    public string Name { get; }

    public PermissionAttribute(string name)
    {
        Name = name;
    }
}

// [Module(ModuleName.UserManagement)]
// [Permission(nameof(ModuleActions.UserManagement.Create))]
// public IActionResult CreateUser()
// {
//     // Method implementation
// }