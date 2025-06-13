using Microsoft.ML;
using Microsoft.ML.Data;
using System;
using System.IO;

namespace app1.Servicios
{
    public class ClasificacionMensajeService
    {
        private static PredictionEngine<MensajeCategoriaData, MensajeCategoriaPrediction>? _predEngine;
        private static readonly object _lock = new();
        private static bool _initialized = false;
        private static readonly string[] Etiquetas = new[] { "Consulta", "Queja", "Sugerencia", "Felicitación", "Otros" };

        public ClasificacionMensajeService()
        {
            if (!_initialized)
            {
                lock (_lock)
                {
                    if (!_initialized)
                    {
                        var dataPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "mensaje-categoria-data.tsv");
                        var mlContext = new MLContext();
                        var data = mlContext.Data.LoadFromTextFile<MensajeCategoriaData>(dataPath, hasHeader: true, separatorChar: '\t');
                        var pipeline = mlContext.Transforms.Conversion.MapValueToKey("Label", nameof(MensajeCategoriaData.Categoria))
                            .Append(mlContext.Transforms.Text.FeaturizeText("Features", nameof(MensajeCategoriaData.Texto)))
                            .Append(mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy())
                            .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));
                        var model = pipeline.Fit(data);
                        _predEngine = mlContext.Model.CreatePredictionEngine<MensajeCategoriaData, MensajeCategoriaPrediction>(model);
                        _initialized = true;
                    }
                }
            }
        }

        public string Clasificar(string texto)
        {
            if (_predEngine == null)
                throw new InvalidOperationException("El modelo de clasificación no está inicializado.");
            var prediction = _predEngine.Predict(new MensajeCategoriaData { Texto = texto ?? string.Empty });
            return prediction.PredictedLabel ?? "Otros";
        }
    }

    public class MensajeCategoriaData
    {
        [LoadColumn(0)]
        public string Categoria { get; set; } = string.Empty;
        [LoadColumn(1)]
        public string Texto { get; set; } = string.Empty;
    }

    public class MensajeCategoriaPrediction
    {
        [ColumnName("PredictedLabel")]
        public string? PredictedLabel { get; set; }
    }
}
