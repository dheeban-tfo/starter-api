namespace starterapi;

public enum ModuleName
{
    UserManagement,
    CommunityManagement,
    // Add other modules as needed
}

public static class ModuleActions
{
    public enum UserManagement
    {
        Create,
        Read,
        Update,
        Delete,
        Export,
        BulkUpdate
    }

    public enum CommunityManagement
    {
        Create,
        Read,
        Update,
        Delete,
        Export,
        Print,
        AssignManager
    }

    // Add other module action enums as needed
}
