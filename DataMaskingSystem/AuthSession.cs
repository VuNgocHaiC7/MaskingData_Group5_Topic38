namespace DataMaskingSystem
{
    public static class AuthSession
    {
        private static readonly object Sync = new object();

        public static string AccessToken { get; private set; } = string.Empty;
        public static string Role { get; private set; } = string.Empty;
        public static string ClientId { get; private set; } = string.Empty;

        public static void Set(string token, string role, string clientId)
        {
            lock (Sync)
            {
                AccessToken = token ?? string.Empty;
                Role = role ?? string.Empty;
                ClientId = clientId ?? string.Empty;
            }
        }

        public static void Clear()
        {
            lock (Sync)
            {
                AccessToken = string.Empty;
                Role = string.Empty;
                ClientId = string.Empty;
            }
        }
    }
}
