using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace app1.Servicios
{
    public class ClimaService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey = "713eb61186640652ec4cea92eebc7b17"; // Reemplaza por tu API key de OpenWeatherMap

        public ClimaService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<JsonDocument?> ObtenerClimaAsync(string ciudad)
        {
            var url = $"https://api.openweathermap.org/data/2.5/weather?q={ciudad}&appid={_apiKey}&units=metric&lang=es";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;
            var json = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(json);
        }

        public async Task<JsonDocument?> ObtenerPronosticoAsync(string ciudad)
        {
            var url = $"https://api.openweathermap.org/data/2.5/forecast?q={ciudad}&appid={_apiKey}&units=metric&lang=es";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;
            var json = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(json);
        }
    }
}
