namespace starterapi;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ModulePermissionAttribute : Attribute
{
    public ModuleName Module { get; }
    public Enum Action { get; }

    public ModulePermissionAttribute(ModuleName module, Enum action)
    {
        Module = module;
        Action = action;
    }
}