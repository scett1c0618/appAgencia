using Google.Cloud.Language.V1;
using System.Threading.Tasks;

namespace app1.Servicios
{
    public class SentimentService
    {
        public SentimentService() { }

        public async Task<(string etiqueta, float puntuacion)> AnalizarSentimientoAsync(string texto)
        {
            var client = await LanguageServiceClient.CreateAsync();
            var document = new Document
            {
                Content = texto,
                Type = Document.Types.Type.PlainText,
                Language = "es"
            };
            var response = await client.AnalyzeSentimentAsync(document);
            var score = response.DocumentSentiment.Score; // -1 (negativo) a 1 (positivo)
            string etiqueta;
            if (score > 0.25)
                etiqueta = "Positivo";
            else if (score < -0.25)
                etiqueta = "Negativo";
            else
                etiqueta = "Neutro";
            return (etiqueta, score);
        }
    }
}
