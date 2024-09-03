using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TickerPredictor.Models;
using TickerPredictor.Services;

namespace TickerPredictorTests;

public class TickerPredictorTests
{
    private readonly ITickerPredictor _tickerPredictor;
    private readonly string _modelPath = "TestModels/TestStockModel.zip";

    public TickerPredictorTests()
    {
        var inMemorySettings = new Dictionary<string, string>
        {
            { "TestFraction", "0.2" },
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var loggerMock = new Mock<ILogger<TickerPredictor.TickerPredictor>>();
        _tickerPredictor = new TickerPredictor.TickerPredictor(configuration, loggerMock.Object);
    }

    [Fact]
    public async Task TrainAsync_ValidData_ModelIsSaved()
    {
        var trainingData = new List<StockData>
        {
            new StockData { StockName = "Apple Inc.", Ticker = "AAPL" },
            new StockData { StockName = "Microsoft Corporation", Ticker = "MSFT" },
        };

        await _tickerPredictor.TrainAsync(trainingData, _modelPath);

        Assert.True(File.Exists(_modelPath));

        if (File.Exists(_modelPath))
        {
            File.Delete(_modelPath);
        }
    }

    [Fact]
    public async Task PredictAsync_ValidModel_ReturnsCorrectTicker()
    {
        var trainingData = new List<StockData>
        {
            new StockData { StockName = "Apple Inc.", Ticker = "AAPL" },
            new StockData { StockName = "Microsoft Corporation", Ticker = "MSFT" },
        };

        await _tickerPredictor.TrainAsync(trainingData, _modelPath);

        var prediction = await _tickerPredictor.PredictAsync("Appl", _modelPath);

        Assert.Equal("AAPL", prediction);

        if (File.Exists(_modelPath))
        {
            File.Delete(_modelPath);
        }
    }

    [Fact]
    public async Task PredictAsync_ModelNotFound_ThrowsException()
    {
        // Arrange
        var nonExistentPath = "NonExistentModel.zip";

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => _tickerPredictor.PredictAsync("Apple", nonExistentPath));
    }
}