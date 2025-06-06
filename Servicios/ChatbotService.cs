using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace app1.Servicios
{
    public class ChatbotService
    {
        private readonly string endpoint = "https://api-inference.huggingface.co/models/microsoft/DialoGPT-medium";
        private readonly string? apiKey;

        public ChatbotService(IConfiguration configuration)
        {
            apiKey = configuration["HuggingFace:ApiKey"];
        }

        public async Task<string> ObtenerRespuestaAsync(string mensaje)
        {
            using var client = new HttpClient();
            if (!string.IsNullOrEmpty(apiKey))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            var body = JsonSerializer.Serialize(new { inputs = mensaje });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(endpoint, content);
            var json = await response.Content.ReadAsStringAsync();
            // Para depuración: muestra el JSON recibido
            if (json != null && json.Length > 0)
            {
                System.IO.File.WriteAllText("chatbot_response_debug.json", json);
            }
            if (string.IsNullOrWhiteSpace(json))
                return "No se recibió respuesta del bot.";
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
                {
                    var respuesta = doc.RootElement[0].GetProperty("generated_text").GetString();
                    return respuesta ?? "No entendí tu mensaje.";
                }
                return "No entendí tu mensaje.";
            }
            catch
            {
                return "No entendí tu mensaje.";
            }
        }
    }
}
