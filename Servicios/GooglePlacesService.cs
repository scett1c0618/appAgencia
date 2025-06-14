using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using System.Collections.Generic;
using System.Linq;

namespace app1.Servicios
{
    public class GooglePlacesService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private const string BaseUrl = "https://maps.googleapis.com/maps/api/place";

        public GooglePlacesService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            // API key fija para demo, en producción usar variable de entorno
            _apiKey = "AIzaSyA9DyydAK3897wOLpYuTzzxcMsQhvs9dro";
        }

        public async Task<(string? resumen, string? imagen, string? url, List<string>? sugerencias, string? sitioWeb, string? tipoLugar, double? rating, string? telefono, string? horario, List<(string autor, string texto, int rating, string fecha)>? reviews, string? nombreLugar)> ObtenerInfoLugarAsync(string ciudadOPlaceId, bool esPlaceId = false)
        {
            string placeId = ciudadOPlaceId;
            string? nombre = null;
            string? direccion = null;
            string? tipoLugar = null;
            if (!esPlaceId)
            {
                // Buscar el place_id de la ciudad
                var searchUrl = $"{BaseUrl}/findplacefromtext/json?input={HttpUtility.UrlEncode(ciudadOPlaceId)}&inputtype=textquery&fields=place_id,formatted_address,name,geometry,types&key={_apiKey}";
                var searchResp = await _httpClient.GetAsync(searchUrl);
                if (!searchResp.IsSuccessStatusCode) return (null, null, null, null, null, null, null, null, null, null, null);
                var searchJson = await searchResp.Content.ReadAsStringAsync();
                using var searchDoc = JsonDocument.Parse(searchJson);
                var candidates = searchDoc.RootElement.GetProperty("candidates");
                if (candidates.GetArrayLength() == 0) return (null, null, null, null, null, null, null, null, null, null, null);
                placeId = candidates[0].GetProperty("place_id").GetString();
                nombre = candidates[0].GetProperty("name").GetString();
                direccion = candidates[0].GetProperty("formatted_address").GetString();
                tipoLugar = candidates[0].TryGetProperty("types", out var types) && types.ValueKind == JsonValueKind.Array && types.GetArrayLength() > 0 ? types[0].GetString() : null;
            }
            // 2. Obtener detalles del lugar (más campos)
            var detailsUrl = $"{BaseUrl}/details/json?place_id={placeId}&fields=name,formatted_address,editorial_summary,photos,url,website,types,rating,formatted_phone_number,opening_hours,reviews&language=es&key={_apiKey}";
            var detailsResp = await _httpClient.GetAsync(detailsUrl);
            var sugerencias = new List<string>();
            if (!detailsResp.IsSuccessStatusCode)
                return (nombre, null, null, sugerencias, null, tipoLugar, null, null, null, null, null);
            var detailsJson = await detailsResp.Content.ReadAsStringAsync();
            using var detailsDoc = JsonDocument.Parse(detailsJson);
            if (!detailsDoc.RootElement.TryGetProperty("result", out var result))
            {
                return (nombre, null, null, sugerencias, null, tipoLugar, null, null, null, null, null);
            }
            if (esPlaceId)
            {
                nombre = result.TryGetProperty("name", out var n) ? n.GetString() : null;
                direccion = result.TryGetProperty("formatted_address", out var d) ? d.GetString() : null;
                tipoLugar = result.TryGetProperty("types", out var types2) && types2.ValueKind == JsonValueKind.Array && types2.GetArrayLength() > 0 ? types2[0].GetString() : null;
            }
            var resumen = result.TryGetProperty("editorial_summary", out var summary) && summary.TryGetProperty("overview", out var overview) ? overview.GetString() : null;
            var url = result.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : null;
            var sitioWeb = result.TryGetProperty("website", out var webProp) ? webProp.GetString() : null;
            double? rating = result.TryGetProperty("rating", out var ratingProp) ? ratingProp.GetDouble() : (double?)null;
            var telefono = result.TryGetProperty("formatted_phone_number", out var telProp) ? telProp.GetString() : null;
            string? horario = null;
            if (result.TryGetProperty("opening_hours", out var hours) && hours.TryGetProperty("weekday_text", out var weekdayText) && weekdayText.ValueKind == JsonValueKind.Array)
            {
                horario = string.Join("<br>", weekdayText.EnumerateArray().Select(x => x.GetString()));
            }
            string? imagen = null;
            if (result.TryGetProperty("photos", out var photos) && photos.ValueKind == JsonValueKind.Array && photos.GetArrayLength() > 0)
            {
                var photoRef = photos[0].GetProperty("photo_reference").GetString();
                imagen = $"https://maps.googleapis.com/maps/api/place/photo?maxwidth=800&photoreference={photoRef}&key={_apiKey}";
            }
            // Reviews
            List<(string autor, string texto, int rating, string fecha)>? reviews = null;
            if (result.TryGetProperty("reviews", out var reviewsArr) && reviewsArr.ValueKind == JsonValueKind.Array)
            {
                reviews = new List<(string, string, int, string)>();
                foreach (var rev in reviewsArr.EnumerateArray())
                {
                    var autor = rev.TryGetProperty("author_name", out var a) ? a.GetString() : "";
                    var texto = rev.TryGetProperty("text", out var t) ? t.GetString() : "";
                    var calif = rev.TryGetProperty("rating", out var r) ? r.GetInt32() : 0;
                    var fecha = rev.TryGetProperty("relative_time_description", out var f) ? f.GetString() : "";
                    if (!string.IsNullOrWhiteSpace(texto))
                        reviews.Add((autor ?? "", texto ?? "", calif, fecha ?? ""));
                }
            }
            if (string.IsNullOrWhiteSpace(resumen))
            {
                resumen = $"<strong>{nombre}</strong><br/>{direccion}";
                if (!string.IsNullOrWhiteSpace(tipoLugar)) resumen += $"<br/><span class='badge bg-info text-dark'>{tipoLugar}</span>";
                if (!string.IsNullOrWhiteSpace(sitioWeb)) resumen += $"<br/><a href='{sitioWeb}' target='_blank'>Sitio web oficial</a>";
                if (rating != null) resumen += $"<br/>Calificación: {rating}/5";
                if (!string.IsNullOrWhiteSpace(telefono)) resumen += $"<br/>Teléfono: {telefono}";
                if (!string.IsNullOrWhiteSpace(horario)) resumen += $"<br/>Horario:<br/>{horario}";
            }
            return (resumen, imagen, url, sugerencias, sitioWeb, tipoLugar, rating, telefono, horario, reviews, nombre);
        }

        public async Task<List<(string placeId, string nombre, string direccion)>> BuscarLugaresCercanosAsync(string ciudad)
        {
            // 1. Buscar coordenadas del lugar
            var geoUrl = $"{BaseUrl}/findplacefromtext/json?input={HttpUtility.UrlEncode(ciudad)}&inputtype=textquery&fields=geometry&key={_apiKey}";
            var geoResp = await _httpClient.GetAsync(geoUrl);
            if (!geoResp.IsSuccessStatusCode) return new List<(string, string, string)>();
            var geoJson = await geoResp.Content.ReadAsStringAsync();
            using var geoDoc = JsonDocument.Parse(geoJson);
            var candidates = geoDoc.RootElement.TryGetProperty("candidates", out var c) ? c : default;
            if (candidates.ValueKind != JsonValueKind.Array || candidates.GetArrayLength() == 0) return new List<(string, string, string)>();
            var location = candidates[0].TryGetProperty("geometry", out var geom) && geom.TryGetProperty("location", out var loc) ? loc : default;
            if (location.ValueKind != JsonValueKind.Object) return new List<(string, string, string)>();
            var lat = location.TryGetProperty("lat", out var latProp) ? latProp.GetDouble() : 0;
            var lng = location.TryGetProperty("lng", out var lngProp) ? lngProp.GetDouble() : 0;
            if (lat == 0 && lng == 0) return new List<(string, string, string)>();
            // 2. Buscar lugares turísticos y populares cerca de esas coordenadas
            var tipos = new[] { "tourist_attraction", "museum", "restaurant", "art_gallery", "park", "church", "zoo", "amusement_park" };
            var lugares = new List<(string placeId, string nombre, string direccion, int reviews)>();
            var placeIds = new HashSet<string>();
            foreach (var tipo in tipos)
            {
                var searchUrl = $"{BaseUrl}/nearbysearch/json?location={lat},{lng}&radius=15000&type={tipo}&key={_apiKey}&language=es";
                var resp = await _httpClient.GetAsync(searchUrl);
                if (!resp.IsSuccessStatusCode) continue;
                var json = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array) continue;
                foreach (var r in results.EnumerateArray())
                {
                    // Excluir lugares cuyo tipo principal sea 'travel_agency' o 'tourist_guide'
                    if (r.TryGetProperty("types", out var typesArr) && typesArr.ValueKind == JsonValueKind.Array)
                    {
                        var tiposLugar = typesArr.EnumerateArray().Select(x => x.GetString()).ToList();
                        if (tiposLugar.Contains("travel_agency") || tiposLugar.Contains("tourist_guide"))
                            continue;
                        // Si es restaurante, solo incluir si tiene muchas reseñas
                        if (tiposLugar.Contains("restaurant"))
                        {
                            int nReviews = r.TryGetProperty("user_ratings_total", out var urt) ? urt.GetInt32() : 0;
                            if (nReviews < 200) continue;
                        }
                    }
                    var placeId = r.TryGetProperty("place_id", out var pid) ? pid.GetString() : null;
                    var nombre = r.TryGetProperty("name", out var n) ? n.GetString() : null;
                    var direccion = r.TryGetProperty("vicinity", out var d) ? d.GetString() : null;
                    int reviews = r.TryGetProperty("user_ratings_total", out var urt2) ? urt2.GetInt32() : 0;
                    if (!string.IsNullOrWhiteSpace(placeId) && !string.IsNullOrWhiteSpace(nombre) && placeIds.Add(placeId!))
                        lugares.Add((placeId!, nombre ?? "", direccion ?? "", reviews));
                }
            }
            // Ordenar por cantidad de reseñas descendente y devolver solo los datos necesarios
            return lugares.OrderByDescending(l => l.reviews).Select(l => (l.placeId, l.nombre, l.direccion)).ToList();
        }
    }
}
