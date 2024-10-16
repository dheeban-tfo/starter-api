using System.Security.Claims;

namespace StarterApi.Helpers
{
    public static class UserContext
    {
        private static AsyncLocal<string> _currentUserId = new AsyncLocal<string>();

        public static string CurrentUserId
        {
            get => _currentUserId.Value;
            set => _currentUserId.Value = value;
        }
    }
}