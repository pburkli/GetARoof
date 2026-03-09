using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

public class AmadeusClient(HttpClient http, string apiKey, string apiSecret)
{
    public async Task<string> AuthAsync()
    {
        var content = new FormUrlEncodedContent([
            new("grant_type", "client_credentials"),
            new("client_id", apiKey),
            new("client_secret", apiSecret)
        ]);
        var response = await http.PostAsync("/v1/security/oauth2/token", content);
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) throw new Exception($"Amadeus auth failed: {json}");
        return JsonNode.Parse(json)!["access_token"]!.GetValue<string>();
    }

    public async Task<List<string>> SearchHotelIdsAsync(string token, string cityCode)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"/v1/reference-data/locations/hotels/by-city?cityCode={cityCode}&radius=5&radiusUnit=KM&hotelSource=ALL");
        request.Headers.Add("Authorization", $"Bearer {token}");
        var response = await http.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) throw new Exception($"Hotel list failed: {json}");
        return JsonNode.Parse(json)!["data"]!.AsArray()
            .Select(h => h!["hotelId"]!.GetValue<string>())
            .ToList();
    }

    public async Task<(string OfferId, decimal Total, string Currency)> GetFirstAvailableOfferAsync(
        string token, List<string> hotelIds, string checkIn, string checkOut)
    {
        foreach (var batch in hotelIds.Chunk(10))
        {
            var ids = string.Join(",", batch);
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"/v3/shopping/hotel-offers?hotelIds={ids}&adults=1&checkInDate={checkIn}&checkOutDate={checkOut}&currency=EUR&bestRateOnly=true");
            request.Headers.Add("Authorization", $"Bearer {token}");
            var response = await http.SendAsync(request);
            if (!response.IsSuccessStatusCode) continue;

            var data = JsonNode.Parse(await response.Content.ReadAsStringAsync())!["data"]?.AsArray();
            if (data == null) continue;

            foreach (var hotel in data)
            {
                if (hotel!["available"]?.GetValue<bool>() != true) continue;
                var offer = hotel["offers"]?[0];
                if (offer == null) continue;
                return (
                    offer["id"]!.GetValue<string>(),
                    decimal.Parse(offer["price"]!["total"]!.GetValue<string>()),
                    offer["price"]!["currency"]!.GetValue<string>()
                );
            }
        }
        throw new Exception("No available hotel offers found.");
    }

    public async Task<string> VerifyOfferAsync(string token, string offerId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/v3/shopping/hotel-offers/{offerId}");
        request.Headers.Add("Authorization", $"Bearer {token}");
        var response = await http.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) throw new Exception($"Offer verify failed: {json}");
        return JsonNode.Parse(json)!["data"]!["offers"]![0]!["id"]!.GetValue<string>();
    }

    public async Task<string> BookHotelAsync(
        string token, string offerId, string cardNumber, string cvv, string expiryMonth, string expiryYear)
    {
        var expiryDate = expiryMonth.PadLeft(2, '0') + expiryYear[^2..];
        using var request = new HttpRequestMessage(HttpMethod.Post, "/v2/booking/hotel-orders");
        request.Headers.Add("Authorization", $"Bearer {token}");
        var body = JsonContent.Create(new
        {
            data = new
            {
                type = "hotel-order",
                guests = new[]
                {
                    new { tid = 1, title = "MR", firstName = "John", lastName = "Smith",
                          email = "john.smith@getaroof.com", phone = "+33123456789" }
                },
                travelAgent = new { contact = new { email = "agent@getaroof.com" } },
                roomAssociations = new[]
                {
                    new
                    {
                        hotelOfferId = offerId,
                        guestReferences = new[] { new { guestReference = "1" } }
                    }
                },
                payment = new
                {
                    method = "CREDIT_CARD",
                    paymentCard = new
                    {
                        paymentCardInfo = new { vendorCode = "VI", cardNumber, expiryDate, holderName = "GetARoof Platform", securityCode = cvv }
                    }
                }
            }
        });
        body.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.amadeus+json");
        request.Content = body;
        var response = await http.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) throw new Exception($"Booking failed: {json}");
        return JsonNode.Parse(json)!["data"]!["hotelBookings"]![0]!
            ["hotelProviderInformation"]![0]!["confirmationNumber"]!.GetValue<string>();
    }
}
