namespace starterapi;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ModuleAttribute : Attribute
{
    public string Name { get; }

    public ModuleAttribute(string name)
    {
        Name = name;
    }
}

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class PermissionAttribute : Attribute
{
    public string Name { get; }

    public PermissionAttribute(string name)
    {
        Name = name;
    }
}

