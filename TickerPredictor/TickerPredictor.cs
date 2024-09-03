using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using TickerPredictor.Models;
using TickerPredictor.Services;

namespace TickerPredictor;

public sealed class TickerPredictor : ITickerPredictor
{
    private readonly MLContext _mlContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TickerPredictor> _logger;

    public TickerPredictor(IConfiguration configuration, ILogger<TickerPredictor> logger = null)
    {
        _mlContext = new MLContext(seed: 0);
        _configuration = configuration;
        _logger = logger;
    }

    public async Task TrainAsync(IEnumerable<StockData> trainingData, string modelPath)
    {
        try
        {
            _logger?.LogInformation("Starting model training.");

            var data = _mlContext.Data.LoadFromEnumerable(trainingData);

            var pipeline = _mlContext.Transforms.Text.FeaturizeText("Features", nameof(StockData.StockName))
                .Append(_mlContext.Transforms.Conversion.MapValueToKey("Label", nameof(StockData.Ticker)))
                .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy("Label", "Features"))
                .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedTicker", "PredictedLabel"));

            var testFraction = _configuration.GetValue<double>("TestFraction", 0.2);
            var trainTestSplit = _mlContext.Data.TrainTestSplit(data, testFraction);

            _logger?.LogInformation("Fitting the model.");
            var model = pipeline.Fit(trainTestSplit.TrainSet);

            _logger?.LogInformation("Evaluating the model.");
            var predictions = model.Transform(trainTestSplit.TestSet);
            var metrics = _mlContext.MulticlassClassification.Evaluate(predictions, labelColumnName: "Label",
                predictedLabelColumnName: "PredictedLabel");

            _logger?.LogInformation($"Log-loss: {metrics.LogLoss}");

            _logger?.LogInformation("Saving the model.");
            Directory.CreateDirectory(Path.GetDirectoryName(modelPath));
            _mlContext.Model.Save(model, data.Schema, modelPath);

            _logger?.LogInformation("Model training completed successfully.");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "An error occurred during model training.");
            throw;
        }
    }

    public async Task<string> PredictAsync(string stockName, string modelPath)
    {
        try
        {
            _logger?.LogInformation($"Loading model from {modelPath}.");
            ITransformer model;
            await using (var stream = new FileStream(modelPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                model = _mlContext.Model.Load(stream, out var modelInputSchema);
            }

            var predictionEngine = _mlContext.Model.CreatePredictionEngine<StockData, StockPrediction>(model);

            var input = new StockData { StockName = stockName };
            var prediction = predictionEngine.Predict(input);

            _logger?.LogInformation($"Prediction completed: {stockName} => {prediction.PredictedTicker}");

            return prediction.PredictedTicker;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "An error occurred during prediction.");
            throw;
        }
    }

    public async Task<IEnumerable<StockPrediction>> PredictBatchAsync(IEnumerable<StockData> stockDataList,
        string modelPath)
    {
        try
        {
            _logger?.LogInformation($"Loading model from {modelPath}.");
            ITransformer model;
            await using (var stream = new FileStream(modelPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                model = _mlContext.Model.Load(stream, out var modelInputSchema);
            }

            var predictions = new List<StockPrediction>();

            var predictionEngine = _mlContext.Model.CreatePredictionEngine<StockData, StockPrediction>(model);

            foreach (var stockData in stockDataList)
            {
                var prediction = predictionEngine.Predict(new StockData { StockName = stockData.StockName });
                predictions.Add(prediction);

                _logger?.LogInformation($"Predicted Ticker for '{stockData.StockName}': {prediction.PredictedTicker}");
            }

            _logger?.LogInformation("Batch prediction completed.");

            return predictions;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "An error occurred during batch prediction.");
            throw;
        }
    }
}