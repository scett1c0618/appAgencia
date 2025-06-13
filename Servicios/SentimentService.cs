using Microsoft.ML;
using Microsoft.ML.Data;
using System;
using System.IO;
using System.Threading.Tasks;

namespace app1.Servicios
{
    public class SentimentService
    {
        private static PredictionEngine<SentimentData, SentimentPrediction>? _predEngine;
        private static readonly object _lock = new();
        private static bool _initialized = false;

        public SentimentService()
        {
            if (!_initialized)
            {
                lock (_lock)
                {
                    if (!_initialized)
                    {
                        // Buscar archivo en output y fallback a ruta de desarrollo
                        var dataPath = Path.Combine(AppContext.BaseDirectory, "Data", "sentiment-data.tsv");
                        if (!File.Exists(dataPath))
                        {
                            dataPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "sentiment-data.tsv");
                        }
                        var mlContext = new MLContext();
                        var data = mlContext.Data.LoadFromTextFile<SentimentData>(dataPath, hasHeader: true, separatorChar: '\t');
                        var pipeline = mlContext.Transforms.Text.FeaturizeText("Features", nameof(SentimentData.Text))
                            .Append(mlContext.BinaryClassification.Trainers.SdcaLogisticRegression());
                        var model = pipeline.Fit(data);
                        _predEngine = mlContext.Model.CreatePredictionEngine<SentimentData, SentimentPrediction>(model);
                        _initialized = true;
                    }
                }
            }
        }

        public Task<(string etiqueta, float puntuacion)> AnalizarSentimientoAsync(string texto)
        {
            if (_predEngine == null)
                throw new InvalidOperationException("El modelo de sentimiento no está inicializado.");
            var prediction = _predEngine.Predict(new SentimentData { Text = texto ?? string.Empty });
            string etiqueta;
            if (prediction.Probability > 0.75)
                etiqueta = "Positivo";
            else if (prediction.Probability < 0.25)
                etiqueta = "Negativo";
            else
                etiqueta = "Neutro";
            return Task.FromResult((etiqueta, prediction.Probability));
        }
    }

    public class SentimentData
    {
        [LoadColumn(0)]
        public bool Label { get; set; }
        [LoadColumn(1)]
        public string Text { get; set; } = string.Empty;
    }

    public class SentimentPrediction
    {
        [ColumnName("PredictedLabel")]
        public bool Prediction { get; set; }
        public float Probability { get; set; }
        public float Score { get; set; }
    }
}
