namespace starterapi.Models;

public class UserProfileResponse
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public List<string> Roles { get; set; }
    public Dictionary<string, List<string>> ModulePermissions { get; set; }
}