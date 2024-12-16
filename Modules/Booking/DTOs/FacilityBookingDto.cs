using StarterApi.Models;

public class FacilityBookingDto :BaseEntity
{
    public int Id { get; set; }
    public int FacilityId { get; set; }
    public Guid UserId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public BookingStatus Status { get; set; }
}

public class CreateBookingDto
{
    public int FacilityId { get; set; }
    public Guid UserId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string CreatedBy { get; set; }
    public string ModifiedBy { get; set; }
}

public class UpdateBookingStatusDto
{
    public BookingStatus Status { get; set; }
    public string ModifiedBy { get; set; }
}