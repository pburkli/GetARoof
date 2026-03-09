using System.Net.Http.Json;
using System.Text.Json.Nodes;

public class AirwallexClient(HttpClient http, string clientId, string apiKey)
{
    public async Task<string> AuthAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/authentication/login");
        request.Headers.Add("x-client-id", clientId);
        request.Headers.Add("x-api-key", apiKey);
        var response = await http.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) throw new Exception($"Airwallex auth failed: {json}");
        return JsonNode.Parse(json)!["token"]!.GetValue<string>();
    }

    public async Task<string> CreateCardholderAsync(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/issuing/cardholders/create");
        request.Headers.Add("Authorization", $"Bearer {token}");
        request.Content = JsonContent.Create(new
        {
            type = "INDIVIDUAL",
            individual = new
            {
                name = new { first_name = "GetARoof", last_name = "Platform" },
                date_of_birth = "1990-01-01",
                express_consent_obtained = "yes",
                address = new { line1 = "1 Test Street", city = "Sydney", state = "NSW", postcode = "2000", country = "AU" }
            },
            email = $"payments+{Guid.NewGuid():N}@getaroof.com",
            request_id = $"cardholder-{Guid.NewGuid()}"
        });
        var response = await http.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) throw new Exception($"Cardholder create failed: {json}");
        return JsonNode.Parse(json)!["cardholder_id"]!.GetValue<string>();
    }

    public async Task<string> CreateVccAsync(string token, string cardholderId, decimal amount, string currency)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/issuing/cards/create");
        request.Headers.Add("Authorization", $"Bearer {token}");
        request.Content = JsonContent.Create(new
        {
            program = new { purpose = "COMMERCIAL", type = "DEBIT" },
            form_factor = "VIRTUAL",
            is_personalized = false,
            cardholder_id = cardholderId,
            created_by = "GetARoof Platform",
            authorization_controls = new
            {
                allowed_transaction_count = "SINGLE",
                allowed_merchant_categories = new[] { "7011" },
                transaction_limits = new
                {
                    currency,
                    limits = new[] { new { amount, interval = "ALL_TIME" } }
                }
            },
            request_id = $"vcc-{Guid.NewGuid()}"
        });
        var response = await http.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) throw new Exception($"VCC create failed: {json}");
        return JsonNode.Parse(json)!["card_id"]!.GetValue<string>();
    }

    public async Task<(string Number, string Cvv, string ExpiryMonth, string ExpiryYear)> GetCardDetailsAsync(string token, string cardId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/issuing/cards/{cardId}/details");
        request.Headers.Add("Authorization", $"Bearer {token}");
        var response = await http.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) throw new Exception($"Get card details failed: {json}");
        var node = JsonNode.Parse(json)!;
        return (
            node["card_number"]!.GetValue<string>(),
            node["cvv"]!.GetValue<string>(),
            node["expiry_month"]!.GetValue<int>().ToString(),
            node["expiry_year"]!.GetValue<int>().ToString()
        );
    }
}
