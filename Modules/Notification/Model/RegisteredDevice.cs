using System;
using System.Collections.Generic;

namespace starterapi.Models
{
    public class RegisteredDevice
    {
        public int Id { get; set; }
        public string DeviceToken { get; set; }
        public string DeviceType { get; set; }
        public DateTime CreatedAt { get; set; }
        public ICollection<DeviceSubscription> Subscriptions { get; set; }
    }
}