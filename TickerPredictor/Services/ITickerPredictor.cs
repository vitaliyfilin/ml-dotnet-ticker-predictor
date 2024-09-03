using TickerPredictor.Models;

namespace TickerPredictor.Services;

public interface ITickerPredictor
{
    Task TrainAsync(IEnumerable<StockData> trainingData, string modelPath);
    Task<string> PredictAsync(string stockName, string modelPath);
    Task<IEnumerable<StockPrediction>> PredictBatchAsync(IEnumerable<StockData> stockDataList, string modelPath);
}