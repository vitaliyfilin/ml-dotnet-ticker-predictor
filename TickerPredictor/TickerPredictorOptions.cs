namespace TickerPredictor;

public sealed class TickerPredictorOptions
{
    public double TestFraction { get; set; } = 0.2;
    public double PredictionConfidence { get; set; } = 0.08;
}