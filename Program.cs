#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CsvHelper;

namespace DataNormalization
{
    internal class Program
    {
        private static void Main()
        {
            try
            {
                var allParts = new List<Part>();
                var outputFilePath = @"C:\Users\ajones\Documents\ItemsToImport\NormalizedPartInfo.csv";

                var abbFilePath = @"C:\Users\ajones\Documents\ItemsToImport\ABB.csv";
                var abbParts = GetAbbParts(abbFilePath);
                allParts.AddRange(abbParts);

                var benshawDescriptionOnlyPath = @"C:\Users\ajones\Documents\ItemsToImport\BenshawDescriptionOnly.csv";
                var benshawDescriptionOnlyParts = GetBenshawDescriptionOnlyParts(benshawDescriptionOnlyPath);
                allParts.AddRange(benshawDescriptionOnlyParts);

                var benshawWithInfoPath = @"C:\Users\ajones\Documents\ItemsToImport\BenshawWithInfo.csv";
                var benshawWithInfoParts = GetBenshawWithInfoParts(benshawWithInfoPath);
                allParts.AddRange(benshawWithInfoParts);

                var leesonMarathonPath = @"C:\Users\ajones\Documents\ItemsToImport\Leeson&Marathon.csv";
                var leesonMarathonParts = GetLeesonMarathonParts(leesonMarathonPath);
                allParts.AddRange(leesonMarathonParts);

                using var writer = new StreamWriter(outputFilePath);
                using var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture);
                csvWriter.WriteRecords(allParts);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static IEnumerable<Part> GetLeesonMarathonParts(string? filePath)
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
                Template = GetLeesonMarathonTemplate(part.Description, part.Manufacturer, part.Category)
            }).Where(IsValidPart).ToList();
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
                description = description.Replace(".5HP", " 1/2HP");
                description = description.Replace("..", ",");
                description = description.Replace(".", ",");

                if (description.IndexOf(",", StringComparison.Ordinal) == 0)
                    description = description.Substring(1);

                if (templateValues.Count >= 5) // One of the Leeson motors that is separated by ','
                {
                    // Not sure if we can do anything else with this brand, the data is too inconsistent
                    //result.Value6 = templateValues[0]; // HP
                    //result.Value3 = templateValues[1]; // RPM
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

        private static IEnumerable<Part> GetBenshawWithInfoParts(string? filePath)
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
            }).Where(IsValidPart).ToList();
        }

        public static decimal? GetDecimalFromString(string? text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            var numberString = Regex.Replace(text, "[^0-9.]", string.Empty);
            return string.IsNullOrEmpty(numberString) ? null : decimal.Parse(numberString);
        }

        private static IEnumerable<Part> GetBenshawDescriptionOnlyParts(string? filePath)
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
            }).Where(IsValidPart).ToList();
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

        private static IEnumerable<Part> GetAbbParts(string? filePath)
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
                Template = GetAbbTemplate(part.Description ?? string.Empty)
            }).Where(IsValidPart).ToList();
        }

        private static bool IsValidPart(Part part) => part.PartNumber is not null 
                                                      && part.Manufacturer is not null 
                                                      && part.Description is not null 
                                                      && part.Description.Length >= 3;

        private static Template? GetAbbTemplate(string description)
        {
            if (string.IsNullOrEmpty(description)) return null;
            var result = new Template();
            var templateValues = description.Split(',').ToList();

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

    public class Part
    {
        public string? PartNumber { get; set; }
        public string? Manufacturer { get; set; }
        public string? Description { get; set; }
        public decimal? Weight { get; set; }
        public string? WeightUnit { get; set; }
        public decimal? ListPrice { get; set; }
        public decimal? OurPrice { get; set; }
        public string Uom { get; set; } = "ea";
        public string? LifecycleStatus { get; set; }
        public bool AesIsAuthorizedDistributor = true;
        public Dimensions? Dimensions { get; set; }
        public Template? Template { get; set; }
    }

    public class Template
    {
        public string? Name { get; set; }
        public string? Value1 { get; set; }
        public string? Value2 { get; set; }
        public string? Value3 { get; set; }
        public string? Value4 { get; set; }
        public string? Value5 { get; set; }
        public string? Value6 { get; set; }
        public string? Value7 { get; set; }
        public string? Value8 { get; set; }
        public string? Value9 { get; set; }
        public string? Value10 { get; set; }
    }

    public class Dimensions
    {
        public decimal? Length { get; set; }
        public decimal? Width { get; set; }
        public decimal? Height { get; set; }
        public string? DimensionUnit { get; set; }
    }

    // This should match the CSV file for ABB parts
    public class AbbPart
    {
        public string? PartNumber { get; set; }
        public string? Manufacturer { get; set; }
        public string? Description { get; set; }
        public decimal? Weight { get; set; }
        public decimal? ListPrice { get; set; }
        public decimal? OurPrice { get; set; }
        public string? Upc { get; set; }
        public string? Dimensions { get; set; }
    }

    // This should match the CSV file for Benshaw parts with only a part number and description
    public class BenshawDescriptionOnlyPart
    {
        public string? PartNumber { get; set; }
        public string? Description { get; set; }
    }

    // This should match the CSV file for Benshaw parts with information
    public class BenshawWithInfoPart
    {
        public string? Manufacturer { get; set; }
        public string? PartNumber { get; set; }
        public string? Description { get; set; }
        public decimal? ListPrice { get; set; }
        public string? Weight { get; set; }
        public decimal? Length { get; set; }
        public decimal? Width { get; set; }
        public decimal? Height { get; set; }
        public string? Category { get; set; }
    }

    // This should match the CSV file for Leeson/Marathon parts
    public class LeesonMarathonPart
    {
        public string? PartNumber { get; set; }
        public string? Manufacturer { get; set; }
        public decimal? OurPrice { get; set; }
        public decimal? ListPrice { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? LifecycleStatus { get; set; }
    }
}
