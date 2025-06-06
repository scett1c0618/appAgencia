using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text;
using System;

namespace app1.Servicios
{
    public class AmadeusHotelService
    {
        private readonly HttpClient _httpClient;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private string? _accessToken;
        private DateTime _tokenExpiry;

        public AmadeusHotelService(HttpClient httpClient, string clientId, string clientSecret)
        {
            _httpClient = httpClient;
            _clientId = clientId;
            _clientSecret = clientSecret;
        }

        private async Task<string> GetAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry)
                return _accessToken;

            var request = new HttpRequestMessage(HttpMethod.Post, "https://test.api.amadeus.com/v1/security/oauth2/token");
            request.Content = new StringContent($"grant_type=client_credentials&client_id={_clientId}&client_secret={_clientSecret}", Encoding.UTF8, "application/x-www-form-urlencoded");
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            _accessToken = doc.RootElement.GetProperty("access_token").GetString();
            var expiresIn = doc.RootElement.GetProperty("expires_in").GetInt32();
            _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 60); // 1 min de margen
            return _accessToken;
        }

        public async Task<JsonDocument?> BuscarHotelesAsync(string ciudad, string checkIn, string checkOut)
        {
            var token = await GetAccessTokenAsync();
            var url = $"https://test.api.amadeus.com/v1/reference-data/locations?subType=CITY&keyword={Uri.EscapeDataString(ciudad)}";
            var reqCity = new HttpRequestMessage(HttpMethod.Get, url);
            reqCity.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var respCity = await _httpClient.SendAsync(reqCity);
            if (!respCity.IsSuccessStatusCode) return null;
            var cityJson = await respCity.Content.ReadAsStringAsync();
            var cityDoc = JsonDocument.Parse(cityJson);
            var cityCode = cityDoc.RootElement.GetProperty("data")[0].GetProperty("iataCode").GetString();
            if (string.IsNullOrEmpty(cityCode)) return null;

            var urlHotels = $"https://test.api.amadeus.com/v2/shopping/hotel-offers?cityCode={cityCode}&checkInDate={checkIn}&checkOutDate={checkOut}&adults=2&roomQuantity=1&currency=PEN&lang=ES";
            var reqHotels = new HttpRequestMessage(HttpMethod.Get, urlHotels);
            reqHotels.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var respHotels = await _httpClient.SendAsync(reqHotels);
            if (!respHotels.IsSuccessStatusCode) return null;
            var hotelsJson = await respHotels.Content.ReadAsStringAsync();
            return JsonDocument.Parse(hotelsJson);
        }
    }
}
