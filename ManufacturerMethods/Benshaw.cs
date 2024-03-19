#nullable enable
using CsvHelper;
using DataNormalization.Objects;
using DataNormalization.Shared;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System;
using System.Linq;

namespace DataNormalization.ManufacturerMethods;

public class Benshaw
{
    public static IEnumerable<Part> GetBenshawWithInfoParts(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return new List<Part>();

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        var records = csv.GetRecords<BenshawWithInfoPart>();
        var benshawParts = records.ToList();

        return benshawParts.Select(part => new Part
        {
            PartNumber = part.PartNumber,
            Manufacturer = part.Manufacturer,
            Description = part.Description,
            Weight = GetDecimalFromString(part.Weight),
            WeightUnit = "lb",
            ListPrice = part.ListPrice,
            Dimensions = new Dimensions
            {
                DimensionUnit = "in",
                Height = part.Height,
                Length = part.Length,
                Width = part.Width
            },
            // This seems to work for both files
            Template = GetBenshawDescriptionOnlyTemplate(part.Description ?? string.Empty)
        }).Where(IsValidPartPredicate.IsValidPart).ToList();
    }

    public static decimal? GetDecimalFromString(string? text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        var numberString = Regex.Replace(text, "[^0-9.]", string.Empty);
        return string.IsNullOrEmpty(numberString) ? null : decimal.Parse(numberString);
    }

    public static IEnumerable<Part> GetBenshawDescriptionOnlyParts(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return new List<Part>();

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        var records = csv.GetRecords<BenshawDescriptionOnlyPart>();
        var benshawParts = records.ToList();

        return benshawParts.Select(part => new Part
        {
            PartNumber = part.PartNumber,
            Manufacturer = "Benshaw",
            Description = part.Description,
            Template = GetBenshawDescriptionOnlyTemplate(part.Description ?? string.Empty)
        }).Where(IsValidPartPredicate.IsValidPart).ToList();
    }

    private static Template? GetBenshawDescriptionOnlyTemplate(string description)
    {
        if (string.IsNullOrEmpty(description)) return null;
        var result = new Template();
        var valuesInParentheses = Regex.Match(description, @"\(([^)]+)\)").Groups[1].Value;
        var templateValues = valuesInParentheses.Split(',').ToList();

        var lowerDescription = description.ToLower();
        var isSoftStarter = lowerDescription.Contains("soft starter") && lowerDescription.Contains("hp");
        var isVariableFrequencyDrive = !isSoftStarter && lowerDescription.Contains("drive");
        if (!isSoftStarter && !isVariableFrequencyDrive) return null;

        // The only template we have for Benshaw is soft starters
        result.Name = isSoftStarter ? "Soft Starter" : "Drive";
        var hp = GetBenshawDescriptionOnlyHp(templateValues);
        var voltage = GetBenshawDescriptionOnlyVoltage(templateValues);
        var maxAmperage = GetBenshawDescriptionOnlyMaxAmperage(templateValues);
        var enclosure = GetBenshawDescriptionOnlyEnclosure(templateValues);

        if (isSoftStarter)
        {
            result.Value1 = hp;
            result.Value2 = voltage;
            result.Value3 = maxAmperage;
            result.Value4 = enclosure;
        }
        else
        {
            result.Value1 = hp;
            result.Value4 = voltage;
        }

        var allValues = result.Value1 + result.Value2 + result.Value3 + result.Value4;
        return string.IsNullOrEmpty(allValues) ? null : result;
    }

    private static string? GetBenshawDescriptionOnlyHp(IReadOnlyCollection<string> values)
    {
        if (!values.Any()) return null;
        var value = values.FirstOrDefault(x => x.ToLower().Contains("hp") && x.ToLower().Contains("v") is false);
        return string.IsNullOrEmpty(value) ? null : ExtractNumberBeforeString(value, "hp");
    }

    private static string? GetBenshawDescriptionOnlyVoltage(IReadOnlyCollection<string> values)
    {
        if (!values.Any()) return null;
        var value = values.FirstOrDefault(x => x.ToLower().Contains("v") && x.ToLower().Contains("hp") is false);
        return string.IsNullOrEmpty(value) ? null : ExtractNumberBeforeString(value, "v");
    }

    public static string? GetBenshawDescriptionOnlyMaxAmperage(IReadOnlyCollection<string> values)
    {
        if (!values.Any()) return null;

        var valuesSplit = new List<string>();

        foreach (var val in values)
        {
            var split = val.Split('-');
            valuesSplit.AddRange(split);
        }

        var value = valuesSplit.FirstOrDefault(x => x.ToLower().Contains("a"));
        return string.IsNullOrEmpty(value) ? null : ExtractNumberBeforeString(value, "a");
    }

    public static string? GetBenshawDescriptionOnlyEnclosure(IReadOnlyCollection<string> values)
    {
        var enclosureString = values.FirstOrDefault(x => x.ToLower().Contains("nema"))?.ToLower();
        if (string.IsNullOrEmpty(enclosureString)) return null;
        var targetIndex = enclosureString?.IndexOf("nema", StringComparison.OrdinalIgnoreCase);
        return targetIndex is null or -1 ? null : enclosureString?.Substring(targetIndex.Value, enclosureString.Length - targetIndex.Value).ToUpper();
    }

    public static string? ExtractNumberBeforeString(string input, string target)
    {
        var targetIndex = input.IndexOf(target, StringComparison.OrdinalIgnoreCase);
        if (targetIndex == -1) return null;
        var substringBeforeTarget = input.Substring(0, targetIndex);
        return Regex.Replace(substringBeforeTarget, "[^0-9./]", string.Empty);
    }
}