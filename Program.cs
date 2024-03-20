using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using CsvHelper;
using DataNormalization.DbMethods;
using DataNormalization.Manufacturers;
using DataNormalization.Objects;

namespace DataNormalization;

internal class Program
{
    private static void Main()
    {
        try
        {
            // Set file paths
            const string outputFilePath = @"C:\Users\ajones\Documents\ItemsToImport\NormalizedPartInfo.csv";
            const string abbFilePath = @"C:\Users\ajones\Documents\ItemsToImport\ABB.csv";
            const string benshawDescriptionOnlyPath = @"C:\Users\ajones\Documents\ItemsToImport\BenshawDescriptionOnly.csv";
            const string benshawWithInfoPath = @"C:\Users\ajones\Documents\ItemsToImport\BenshawWithInfo.csv";
            const string leesonMarathonPath = @"C:\Users\ajones\Documents\ItemsToImport\Leeson&Marathon.csv";
            
            // Get parts from each manufacturer
            var abbParts = Abb.GetAbbParts(abbFilePath);
            var benshawDescriptionOnlyParts = Benshaw.GetBenshawDescriptionOnlyParts(benshawDescriptionOnlyPath);
            var benshawWithInfoParts = Benshaw.GetBenshawWithInfoParts(benshawWithInfoPath);
            var leesonMarathonParts = LeesonMarathon.GetLeesonMarathonParts(leesonMarathonPath);

            // Combine all parts into one list
            var allParts = new List<Part>();
            allParts.AddRange(abbParts);
            allParts.AddRange(benshawDescriptionOnlyParts);
            allParts.AddRange(benshawWithInfoParts);
            allParts.AddRange(leesonMarathonParts);

            // Write all parts to a CSV file
            //using var writer = new StreamWriter(outputFilePath);
            //using var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture);
            //csvWriter.WriteRecords(allParts);
            
            var addPartsToDbTask = AddPartsToDbAsync(allParts);
            addPartsToDbTask.Wait();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    // Add all parts to the database
    public static async Task AddPartsToDbAsync(List<Part> parts)
    {
        var percentToShow = 0;

        foreach (var part in parts)
        {
            await AddItemToDb.AddItemToDbAsync(part);
            var percentComplete = (parts.IndexOf(part) + 1) / (double)parts.Count * 100;
            var percentCompleteRounded = (int)Math.Floor(percentComplete);

            if (percentCompleteRounded <= percentToShow) continue;
            percentToShow = percentCompleteRounded;
            Console.WriteLine($"{percentToShow}% complete");
        }
    }
}
