public class FloorDto
{
    public int Id { get; set; }
    public int FloorNumber { get; set; }
    public int BlockId { get; set; }
    public string BlockName { get; set; }
    public int NumberOfUnits { get; set; }
}

public class CreateFloorDto
{
    public int FloorNumber { get; set; }
    public int BlockId { get; set; }
}

public class UpdateFloorDto
{
    public int FloorNumber { get; set; }
} 