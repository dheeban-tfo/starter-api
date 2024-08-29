using System;
using System.ComponentModel.DataAnnotations;

namespace starterapi.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; }
        
        [Required]
        public string Action { get; set; }
        
        [Required]
        public string EntityName { get; set; }
        
        public string EntityId { get; set; }
        
        public string OldValues { get; set; }
        
        public string NewValues { get; set; }
        
        [Required]
        public DateTime Timestamp { get; set; }
    }
}