#nullable enable
namespace DataNormalization.Objects;

// This should match the CSV file for Benshaw parts with only a part number and description
public class BenshawDescriptionOnlyPart
{
    public string? PartNumber { get; set; }
    public string? Description { get; set; }
}