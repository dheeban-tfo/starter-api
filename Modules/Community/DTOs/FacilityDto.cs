using StarterApi.Models;

public class FacilityDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int Capacity { get; set; }
    public int CommunityId { get; set; }
}

public class CreateFacilityDto 
{
    public string Name { get; set; }
    public string Description { get; set; }
    public int Capacity { get; set; }
    public int CommunityId { get; set; }
}

public class UpdateFacilityDto 
{
    public string Name { get; set; }
    public string Description { get; set; }
    public int Capacity { get; set; }
}