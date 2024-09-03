using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using TickerPredictor.Models;

namespace TickerPredictor.Helpers;

internal static class CsvDataHelper
{
    public static IReadOnlyList<StockData> GetTrainingData(string csvFilePath)
    {
        using var reader = new StreamReader(csvFilePath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        });

        return csv.GetRecords<StockData>().ToList();
    }
}