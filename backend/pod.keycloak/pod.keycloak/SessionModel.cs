namespace pod.keycloak
{
    public class SessionModel
    {
        public string SessionId { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime Expiration { get; set; }
        public string Username { get; set; }
    }
}
