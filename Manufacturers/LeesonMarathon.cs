#nullable enable
using CsvHelper;
using DataNormalization.Objects;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System;
using DataNormalization.Shared;

namespace DataNormalization.Manufacturers;

public class LeesonMarathon
{
    public static IEnumerable<Part> GetLeesonMarathonParts(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return new List<Part>();

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        var records = csv.GetRecords<LeesonMarathonPart>();
        var leesonMarathonParts = records.ToList();

        return leesonMarathonParts.Select(part => new Part
        {
            PartNumber = part.PartNumber,
            Manufacturer = part.Manufacturer,
            Description = part.Description,
            ListPrice = part.ListPrice,
            OurPrice = part.OurPrice,
            LifecycleStatus = part.LifecycleStatus,
            Template = GetLeesonMarathonTemplate(part.Description, part.Manufacturer, part.Category),
            VendorId = part.Manufacturer?.ToLower().Contains("leeson") is true ? 10325 : 13332 // Leeson or Marathon
        }).Where(IsValidPartPredicate.IsValidPart).OrderByDescending(x => x.Manufacturer).ToList();
    }

    private static Template? GetLeesonMarathonTemplate(string? description, string? manufacturer, string? category)
    {
        if (string.IsNullOrEmpty(description) || string.IsNullOrEmpty(manufacturer) || string.IsNullOrEmpty(category)) return null;
        var templateValues = description!.Split(',').ToList();

        // The only template we have for Leeson/Marathon is motors
        var result = new Template();
        result.Name = "Motor";

        if (manufacturer?.ToLower().Contains("leeson") is true)
        {
            // This is the best we can do to normalize this data, it's extremely inconsistent
            description = description.Replace(".5HP", " 1/2HP");
            description = description.Replace("..", ",");
            description = description.Replace(".", ",");

            if (description.IndexOf(",", StringComparison.Ordinal) == 0)
                description = description.Substring(1);

            templateValues = description.Split(',').ToList();

            if (templateValues.Count >= 4) // If it has less than 4 sections we probably can't map it
            {
                result.Value6 = templateValues[0]; // HP
                result.Value3 = templateValues[1]; // RPM
                result.Value5 = templateValues[2]; // Enclosure
                result.Value1 = templateValues[3]; // Frame
                result.Value10 = templateValues.FirstOrDefault(x => x.ToLower().Contains("ph")); // Phase
                result.Value8 = templateValues.FirstOrDefault(x => x.ToLower().Contains("hz")); // Frequency

                var voltage = templateValues.FirstOrDefault(x => x.ToLower().Contains("v") && x.ToLower().Contains("tenv") is false);

                if (!string.IsNullOrEmpty(voltage))
                {
                    var splitBySlash = voltage.Split('/');

                    if (splitBySlash.Length < 3)
                    {
                        result.Value2 = voltage; // Voltage
                    }
                    else
                    {
                        result.Value10 = splitBySlash[0]; // Phase
                        result.Value8 = splitBySlash[1]; // Frequency
                        result.Value2 = string.Join("/", splitBySlash.Skip(2)); // Voltage
                    }
                }
            }
        }
        else if (manufacturer?.ToLower().Contains("marathon") is true || manufacturer?.ToLower().Contains("century") is true)
        {
            if (templateValues.Count < 5) return null;

            result.Value6 = templateValues[0]; // HP
            result.Value3 = templateValues[1]; // RPM
            result.Value5 = templateValues[2]; // Enclosure
            result.Value1 = templateValues[3]; // Frame

            if (templateValues.Count == 5) // The rest must be separated by '/'
            {
                var splitBySlash = templateValues[4].Split('/');
                if (splitBySlash.Length < 3) return null;

                result.Value10 = splitBySlash[0]; // Phase
                result.Value8 = splitBySlash[1]; // Frequency
                result.Value2 = string.Join("/", splitBySlash.Skip(2)); // Voltage
            }

            if (templateValues.Count >= 7) // The rest must be separated by ','
            {
                result.Value10 = templateValues[4]; // Phase
                result.Value8 = templateValues[5]; // Frequency
                result.Value2 = templateValues[6]; // Voltage
            }
        }

        var allValues = result.Value1 + result.Value2 + result.Value3 + result.Value4 + result.Value5 + result.Value6 + result.Value7 + result.Value8 + result.Value9 + result.Value10;

        return string.IsNullOrEmpty(allValues) ? null : result;
    }
}