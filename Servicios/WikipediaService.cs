using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace app1.Servicios
{
    public class WikipediaService
    {
        private readonly HttpClient _httpClient;
        public WikipediaService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<(string? resumen, string? imagen, string? url, string[]? sugerencias)> ObtenerResumenAsync(string termino)
        {
            var url = $"https://es.wikipedia.org/api/rest_v1/page/summary/{HttpUtility.UrlEncode(termino)}";
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);
                var resumen = doc.RootElement.TryGetProperty("extract", out var ext) ? ext.GetString() : null;
                var urlWiki = doc.RootElement.TryGetProperty("content_urls", out var urls) && urls.TryGetProperty("desktop", out var desk) && desk.TryGetProperty("page", out var page) ? page.GetString() : null;
                var imagen = doc.RootElement.TryGetProperty("originalimage", out var img) && img.TryGetProperty("source", out var src) ? src.GetString() : null;
                return (resumen, imagen, urlWiki, null);
            }
            // Si no se encontró, buscar sugerencias
            var searchUrl = $"https://es.wikipedia.org/w/api.php?action=opensearch&search={HttpUtility.UrlEncode(termino)}&limit=5&namespace=0&format=json";
            var searchResp = await _httpClient.GetAsync(searchUrl);
            if (!searchResp.IsSuccessStatusCode) return (null, null, null, null);
            var searchJson = await searchResp.Content.ReadAsStringAsync();
            using var searchDoc = JsonDocument.Parse(searchJson);
            if (searchDoc.RootElement.ValueKind == JsonValueKind.Array && searchDoc.RootElement.GetArrayLength() > 1)
            {
                var sugerenciasElem = searchDoc.RootElement[1];
                if (sugerenciasElem.ValueKind == JsonValueKind.Array)
                {
                    var sugerencias = sugerenciasElem.EnumerateArray().Select(x => x.GetString() ?? string.Empty).Where(x => !string.IsNullOrEmpty(x)).ToArray();
                    return (null, null, null, sugerencias.Length > 0 ? sugerencias : null);
                }
            }
            return (null, null, null, null);
        }
    }
}
