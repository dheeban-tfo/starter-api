using StarterApi.Models.Communities;

public class UnitDto
{
    public int Id { get; set; }
    public string UnitNumber { get; set; }
    public UnitType Type { get; set; }
    public int FloorId { get; set; }
    public string FloorNumber { get; set; }
    public string BlockName { get; set; }
    public List<UnitOwnershipDto> Ownerships { get; set; }
    public List<UnitResidentDto> Residents { get; set; }
}

public class CreateUnitDto
{
    public string UnitNumber { get; set; }
    public UnitType Type { get; set; }
    public int FloorId { get; set; }
}

public class UpdateUnitDto
{
    public string UnitNumber { get; set; }
    public UnitType Type { get; set; }
}

public class UnitOwnershipDto
{
    public int Id { get; set; }
    public string OwnerName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class UnitResidentDto
{
    public int Id { get; set; }
    public string ResidentName { get; set; }
    public string ContactNumber { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
} 