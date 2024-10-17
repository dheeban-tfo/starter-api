namespace starterapi;

public enum ModuleName
{
    UserManagement,
    CommunityManagement,
    FacilityManagement,
    FacilityBooking
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

  public enum FacilityManagement
    {
        Create,
        Read,
        Update,
        Delete
    }

    public enum FacilityBooking
    {
        Create,
        Read,
        Update,
        Delete
    }
    // Add other module action enums as needed
}
