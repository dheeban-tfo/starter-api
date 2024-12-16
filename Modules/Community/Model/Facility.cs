using starterapi;

namespace StarterApi.Models
{
    public class Facility : BaseEntity
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Capacity { get; set; }
        public int CommunityId { get; set; }
        public Community Community { get; set; }
        public ICollection<FacilityBooking> Bookings { get; set; }
    }

    public class FacilityBooking : BaseEntity
    {
        public int FacilityId { get; set; }
        public Facility Facility { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public BookingStatus Status { get; set; }
    }

    public enum BookingStatus
    {
        Pending,
        Approved,
        Rejected,
        Cancelled
    }
}