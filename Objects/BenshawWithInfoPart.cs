#nullable enable
namespace DataNormalization.Objects;

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