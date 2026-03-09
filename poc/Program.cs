using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json")
    .Build();

var airwallex = new AirwallexClient(
    new HttpClient { BaseAddress = new Uri(config["Airwallex:BaseUrl"]!) },
    config["Airwallex:ClientId"]!,
    config["Airwallex:ApiKey"]!);

var amadeus = new AmadeusClient(
    new HttpClient { BaseAddress = new Uri(config["Amadeus:BaseUrl"]!) },
    config["Amadeus:ApiKey"]!,
    config["Amadeus:ApiSecret"]!);

Console.WriteLine("=== GetARoof POC ===\n");

Console.WriteLine("[1/7] Authenticating with Airwallex...");
var awToken = await airwallex.AuthAsync();

Console.WriteLine("[2/7] Creating cardholder...");
var cardholderId = await airwallex.CreateCardholderAsync(awToken);
Console.WriteLine($"      {cardholderId}");

Console.WriteLine("[3/7] Authenticating with Amadeus...");
var amToken = await amadeus.AuthAsync();

Console.WriteLine("[4/7] Searching hotels in Paris...");
var hotelIds = await amadeus.SearchHotelIdsAsync(amToken, "PAR");
Console.WriteLine($"      Found {hotelIds.Count} hotels");

var checkIn  = DateTime.Today.AddDays(30).ToString("yyyy-MM-dd");
var checkOut = DateTime.Today.AddDays(31).ToString("yyyy-MM-dd");

Console.WriteLine($"[5/7] Finding available offer ({checkIn} → {checkOut})...");
var (offerId, amount, currency) = await amadeus.GetFirstAvailableOfferAsync(amToken, hotelIds, checkIn, checkOut);
Console.WriteLine($"      Offer {offerId}: {amount} {currency}");

Console.WriteLine("      Re-pricing offer...");
offerId = await amadeus.VerifyOfferAsync(amToken, offerId);

Console.WriteLine("[6/7] Issuing single-use VCC...");
var cardId = await airwallex.CreateVccAsync(awToken, cardholderId, amount, currency);
var (cardNumber, cvv, expiryMonth, expiryYear) = await airwallex.GetCardDetailsAsync(awToken, cardId);
Console.WriteLine($"      Card: {cardNumber[..6]}XXXXXX{cardNumber[^4..]}  exp {expiryMonth.PadLeft(2, '0')}/{expiryYear}");

Console.WriteLine("[7/7] Booking hotel...");
var confirmation = await amadeus.BookHotelAsync(amToken, offerId, cardNumber, cvv, expiryMonth, expiryYear);

Console.WriteLine($"\n✓ Booking CONFIRMED — confirmation number: {confirmation}");
