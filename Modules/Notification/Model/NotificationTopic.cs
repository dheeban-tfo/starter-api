using starterapi.Helpers;

namespace starterapi.Models
{
    public enum NotificationTopic
    {
        [Metadata(Name = "Community Announcements", Value = "community_announcements")]
        CommunityAnnouncements,

        [Metadata(Name = "Maintenance Updates", Value = "maintenance_updates")]
        MaintenanceUpdates,

        [Metadata(Name = "Event Reminders", Value = "event_reminders")]
        EventReminders,

        [Metadata(Name = "Billing Notices", Value = "billing_notices")]
        BillingNotices,

        [Metadata(Name = "Emergency Alerts", Value = "emergency_alerts")]
        EmergencyAlerts
    }
}