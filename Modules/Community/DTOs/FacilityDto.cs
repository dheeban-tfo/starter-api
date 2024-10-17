using StarterApi.Models;

public class FacilityDto :BaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int Capacity { get; set; }
    public int CommunityId { get; set; }
}

public class CreateFacilityDto :BaseEntity
{
    public string Name { get; set; }
    public string Description { get; set; }
    public int Capacity { get; set; }
    public int CommunityId { get; set; }
}

public class UpdateFacilityDto :BaseEntity
{
    public string Name { get; set; }
    public string Description { get; set; }
    public int Capacity { get; set; }
}