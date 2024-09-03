using FluentAssertions;
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
    public async Task PredictAsync_PartialMatch_ReturnsClosestTicker()
    {
        var trainingData = new List<StockData>
        {
            new() { StockName = "Apple Inc.", Ticker = "AAPL" },
            new() { StockName = "Microsoft Corporation", Ticker = "MSFT" },
        };

        await _tickerPredictor.TrainAsync(trainingData, _modelPath);

        var prediction = await _tickerPredictor.PredictAsync("Microsft", _modelPath);

        prediction.PredictedTicker.Should().Be("MSFT");

        if (File.Exists(_modelPath))
        {
            File.Delete(_modelPath);
        }
    }

    [Fact]
    public async Task PredictAsync_EmptyInput_ReturnsNullPrediction()
    {
        var trainingData = new List<StockData>
        {
            new() { StockName = "Apple Inc.", Ticker = "AAPL" },
            new() { StockName = "Microsoft Corporation", Ticker = "MSFT" },
        };

        await _tickerPredictor.TrainAsync(trainingData, _modelPath);

        var prediction = await _tickerPredictor.PredictAsync("", _modelPath);

        prediction.Should().BeNull();

        if (File.Exists(_modelPath))
        {
            File.Delete(_modelPath);
        }
    }

    [Fact]
    public async Task TrainAsync_ValidData_ModelIsSaved()
    {
        var trainingData = new List<StockData>
        {
            new() { StockName = "Apple Inc.", Ticker = "AAPL" },
            new() { StockName = "Microsoft Corporation", Ticker = "MSFT" },
        };

        await _tickerPredictor.TrainAsync(trainingData, _modelPath);

        File.Exists(_modelPath).Should().BeTrue();

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
            new() { StockName = "Apple Inc.", Ticker = "AAPL" },
            new() { StockName = "Microsoft Corporation", Ticker = "MSFT" },
        };

        await _tickerPredictor.TrainAsync(trainingData, _modelPath);

        var prediction = await _tickerPredictor.PredictAsync("Appl", _modelPath);

        prediction.PredictedTicker.Should().Be("AAPL");

        if (File.Exists(_modelPath))
        {
            File.Delete(_modelPath);
        }
    }

    [Fact]
    public async Task PredictAsync_ModelNotFound_ThrowsException()
    {
        var nonExistentPath = "NonExistentModel.zip";

        var action = async () => await _tickerPredictor.PredictAsync("Apple", nonExistentPath);

        await action.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task PredictAsync_ValidModel_Returns_LowScore()
    {
        var trainingData = new List<StockData>
        {
            new() { StockName = "Apple Inc.", Ticker = "AAPL" },
            new() { StockName = "Microsoft Corporation", Ticker = "MSFT" },
        };

        await _tickerPredictor.TrainAsync(trainingData, _modelPath);

        var prediction = await _tickerPredictor.PredictAsync("Amazon", _modelPath);

        prediction.MaxScore.Should().BeLessThan(0.8F);

        if (File.Exists(_modelPath))
        {
            File.Delete(_modelPath);
        }
    }

    [Fact]
    public async Task PredictBatchAsync_ValidModel_ReturnsCorrectTickers()
    {
        var trainingData = new List<StockData>
        {
            new() { StockName = "Apple Inc.", Ticker = "AAPL US" },
            new() { StockName = "Microsoft Corporation", Ticker = "MSFT US" },
            new() { StockName = "Tesla inc.", Ticker = "TSLA US" },
            new() { StockName = "Nvidia corp.", Ticker = "NVDA US" },
            new() { StockName = "Adobe inc.", Ticker = "ADBE US" },
            new() { StockName = "Meta corp.", Ticker = "META US" },
            new() { StockName = "Berkshire Hathaway", Ticker = "BRK US" },
            new() { StockName = "Accenture plc", Ticker = "ACN US" },
        };

        var stockDataList = new List<StockData>
        {
            new() { StockName = "Apple" },
            new() { StockName = "Microsoft" },
            new() { StockName = "Tesla" },
            new() { StockName = "Nvidia" },
            new() { StockName = "Adobe" },
            new() { StockName = "Meta" },
            new() { StockName = "Berkshire" },
            new() { StockName = "Accenture" },
        };

        await _tickerPredictor.TrainAsync(trainingData, _modelPath);

        var batchPredictions = await _tickerPredictor.PredictBatchAsync(stockDataList, _modelPath);

        var expectedPredictions = new List<StockPrediction>
        {
            new() { PredictedTicker = "AAPL US" },
            new() { PredictedTicker = "MSFT US" },
            new() { PredictedTicker = "TSLA US" },
            new() { PredictedTicker = "NVDA US" },
            new() { PredictedTicker = "ADBE US" },
            new() { PredictedTicker = "META US" },
            new() { PredictedTicker = "BRK US" },
            new() { PredictedTicker = "ACN US" },
        };

        var actualTickers = batchPredictions.Select(p => p.PredictedTicker).ToList();
        var expectedTickers = expectedPredictions.Select(p => p.PredictedTicker).ToList();

        actualTickers.Should().Contain(expectedTickers);

        if (File.Exists(_modelPath))
        {
            File.Delete(_modelPath);
        }
    }

    [Fact]
    public async Task PredictAsync_CaseInsensitiveMatching_ReturnsCorrectTicker()
    {
        var trainingData = new List<StockData>
        {
            new() { StockName = "Apple Inc.", Ticker = "AAPL" },
            new() { StockName = "Microsoft Corporation", Ticker = "MSFT" },
        };

        await _tickerPredictor.TrainAsync(trainingData, _modelPath);

        var prediction = await _tickerPredictor.PredictAsync("apple", _modelPath);

        prediction.PredictedTicker.Should().Be("AAPL");

        if (File.Exists(_modelPath))
        {
            File.Delete(_modelPath);
        }
    }
}