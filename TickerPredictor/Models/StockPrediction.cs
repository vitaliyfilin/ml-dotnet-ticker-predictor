namespace TickerPredictor.Models;

public sealed class StockPrediction
{
    public string? StockName { get; set; }
    public string? PredictedTicker { get; set; }

    public float[]? Score { get; set; }

//Return the best found match
    public float MaxScore
    {
        get
        {
            if (Score is null || Score.Length == 0)
            {
                return 0f;
            }

            return Score.Max();
        }
    }
}