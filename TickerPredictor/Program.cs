using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TickerPredictor.Extensions;
using TickerPredictor.Helpers;
using TickerPredictor.Services;

namespace TickerPredictor;

internal class Program
{
    private static readonly string WorkingDirectory = Environment.CurrentDirectory;

    private static readonly string ProjectDirectory = Directory.GetParent(WorkingDirectory)?.Parent?.Parent?.FullName ??
                                                      throw new InvalidOperationException(
                                                          "Project directory could not be determined.");

    private static readonly string DataSetLocation = Path.Combine(ProjectDirectory, "Data", "data.csv");
    private static readonly string ModelPath = Path.Combine(ProjectDirectory, "MLModels", "StockModel.zip");

    private static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(ProjectDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var serviceProvider = new ServiceCollection()
            .AddTickerPredictor(configuration)
            .BuildServiceProvider();

        var tickerPredictor = serviceProvider.GetService<ITickerPredictor>();
        
        var trainingData = CsvDataHelper.GetTrainingData(DataSetLocation);

        await tickerPredictor.TrainAsync(trainingData, ModelPath);

        await tickerPredictor.PredictAsync("Appl", ModelPath);

        var batchData = CsvDataHelper.GetTrainingData(DataSetLocation);

        await tickerPredictor.PredictBatchAsync(batchData, ModelPath);
    }
}