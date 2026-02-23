using System.Text.Json;

namespace pod.keycloak
{
    public class KeycloakService
    {
        private readonly HttpClient _http = new HttpClient();

        public async Task<JsonDocument> Login(string username, string password)
        {
            var content = new FormUrlEncodedContent(new[]
            {
            new KeyValuePair<string,string>("client_id","kong-test"),
            new KeyValuePair<string,string>("client_secret","cXpca2l2t69eNlAw8gWK7vXZqPOVJ5EQ"),
            new KeyValuePair<string,string>("grant_type","password"),
            new KeyValuePair<string,string>("username",username),
            new KeyValuePair<string,string>("password",password)
        });

            var response = await _http.PostAsync(
                "http://localhost:8080/realms/master/protocol/openid-connect/token",
                content);

            response.EnsureSuccessStatusCode();

            return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        }
    }
}
