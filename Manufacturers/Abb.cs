#nullable enable
using CsvHelper;
using DataNormalization.Objects;
using DataNormalization.Shared;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DataNormalization.Manufacturers;

public class Abb
{
    public static IEnumerable<Part> GetAbbParts(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return new List<Part>();

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        var records = csv.GetRecords<AbbPart>();
        var abbParts = records.ToList();

        return abbParts.Select(part => new Part
        {
            PartNumber = part.PartNumber,
            Manufacturer = part.Manufacturer,
            Description = part.Description,
            Weight = part.Weight,
            WeightUnit = "lb",
            ListPrice = part.ListPrice,
            OurPrice = part.OurPrice,
            Dimensions = GetAbbDimensions(part.Dimensions ?? string.Empty),
            Template = GetAbbTemplate(part.Description ?? string.Empty),
            VendorId = 10009 // ABB/Baldor
        }).Where(IsValidPartPredicate.IsValidPart).ToList();
    }

    private static Template? GetAbbTemplate(string description)
    {
        if (string.IsNullOrEmpty(description)) return null;
        var result = new Template();
        description = description.Replace(' ', ',');
        var templateValues = description.Split(',').ToList();

        // Some descriptions are missing commas, so we'll split on spaces if there are less than 3 commas
        if (templateValues.Count < 3) templateValues = description.Split(' ').ToList();

        // The only template we have for ABB is motors
        result.Name = "Motor";

        // Frame
        result.Value1 = null; // Not listed in the description

        // Voltage
        result.Value2 = null; // Not listed in the description

        // RPM
        result.Value3 = GetNumericTemplateValue(templateValues, "rpm");

        // Efficiency
        result.Value4 = null; // Not listed in the description

        // Enclosure
        var enclosureTypes = new List<string> { "ANSI", "NEMA", "ODP", "TEFC", "TENV", "TEWAC", "WPI", "WPII", "WDN" };
        result.Value5 = templateValues.FirstOrDefault(x => enclosureTypes.Any(x.Trim().ToUpper().Contains));

        // HP
        result.Value6 = GetNumericTemplateValue(templateValues, "hp");

        // KW
        result.Value7 = null; // Not listed in the description

        // Frequency
        result.Value8 = GetNumericTemplateValue(templateValues, "hz");

        // Amperage
        result.Value9 = null; // Not listed in the description

        // Phase
        result.Value10 = GetNumericTemplateValue(templateValues, "ph");

        var allValues = result.Value1 + result.Value2 + result.Value3 + result.Value4 + result.Value5 + result.Value6 + result.Value7 + result.Value8 + result.Value9 + result.Value10;

        return string.IsNullOrEmpty(allValues) ? null : result;
    }

    private static string? GetNumericTemplateValue(IEnumerable<string> values, string contains)
    {
        var value = values.FirstOrDefault(x => x.ToLower().Contains(contains));
        return string.IsNullOrEmpty(value) ? null : Regex.Replace(value, "[^0-9./-]", string.Empty);
    }

    private static Dimensions? GetAbbDimensions(string dimensions)
    {
        if (string.IsNullOrEmpty(dimensions)) return null;
        var split = dimensions.Split('X');
        if (split.Length != 3) return null;

        return new Dimensions
        {
            Length = GetDimensionValueFromString(split[0]),
            Width = GetDimensionValueFromString(split[1]),
            Height = GetDimensionValueFromString(split[2]),
            DimensionUnit = "in"
        };
    }

    private static decimal GetDimensionValueFromString(string value) => decimal.Parse(Regex.Replace(value, "[^0-9.]", string.Empty));
}