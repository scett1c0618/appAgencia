using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Globalization;
using System.Text;

namespace app1.Servicios
{
    public class HotelService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey = "303e0d6014msh8b2638310849fcdp18a2c0jsn6e5bdd04e295"; // Reemplaza por tu API key de Booking (RapidAPI)
        public HotelService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<(JsonDocument? hoteles, string? error)> BuscarHotelesAsync(string ciudad)
        {
            try
            {
                var url = $"https://booking-com.p.rapidapi.com/v1/hotels/search?dest_type=city&order_by=popularity&adults_number=2&units=metric&room_number=1&locale=es&checkin_date=2025-06-05&checkout_date=2025-06-06&dest_id={{DEST_ID}}";
                // Primero obtener el dest_id de la ciudad
                var destId = await ObtenerDestinoIdAsync(ciudad);
                if (string.IsNullOrEmpty(destId))
                    return (null, "No se encontró el destino para la ciudad seleccionada.");
                url = url.Replace("{DEST_ID}", destId);
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("X-RapidAPI-Key", _apiKey);
                request.Headers.Add("X-RapidAPI-Host", "booking-com.p.rapidapi.com");
                var response = await _httpClient.SendAsync(request);
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    return (null, "No se pudo acceder a la API de hoteles (acceso denegado). Verifica tu clave o intenta más tarde.");
                if (!response.IsSuccessStatusCode)
                    return (null, $"Error al consultar hoteles: {response.ReasonPhrase}");
                var json = await response.Content.ReadAsStringAsync();
                return (JsonDocument.Parse(json), null);
            }
            catch
            {
                return (null, "Ocurrió un error al buscar hoteles. Intenta nuevamente más tarde.");
            }
        }

        private string NormalizarCiudad(string ciudad)
        {
            if (string.IsNullOrWhiteSpace(ciudad)) return string.Empty;
            var normalized = ciudad.Trim().ToLowerInvariant();
            normalized = normalized.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(sb.ToString().Normalize(NormalizationForm.FormC));
        }

        public async Task<string?> ObtenerDestinoIdAsync(string ciudad)
        {
            try
            {
                var ciudadNormalizada = NormalizarCiudad(ciudad);
                var url = $"https://booking-com.p.rapidapi.com/v1/hotels/locations?name={ciudadNormalizada}&locale=es";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("X-RapidAPI-Key", _apiKey);
                request.Headers.Add("X-RapidAPI-Host", "booking-com.p.rapidapi.com");
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return null;
                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);
                var first = doc.RootElement.EnumerateArray().FirstOrDefault();
                if (first.ValueKind == JsonValueKind.Object && first.TryGetProperty("dest_id", out var destId))
                    return destId.GetString();
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
