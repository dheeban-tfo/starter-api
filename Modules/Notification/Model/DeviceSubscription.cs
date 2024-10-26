using System;

namespace starterapi.Models
{
    public class DeviceSubscription
    {
        public int Id { get; set; }
        public int RegisteredDeviceId { get; set; }
        public RegisteredDevice RegisteredDevice { get; set; }
        public string TopicName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }
}