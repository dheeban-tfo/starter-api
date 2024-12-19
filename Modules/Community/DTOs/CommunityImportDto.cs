using StarterApi.Models;
using StarterApi.Models.Communities;

public class CommunityImportDto
{
    public string BlockName { get; set; }
    public int FloorNumber { get; set; }
    public string UnitNumber { get; set; }
    public UnitType UnitType { get; set; }
    public string OwnerName { get; set; }
    public string ContactNumber { get; set; }
    public string OwnerEmail { get; set; }
    public DateTime OwnershipStartDate { get; set; }
    public DateTime? OwnershipEndDate { get; set; }
    public decimal OwnershipPercentage { get; set; }
}