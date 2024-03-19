#nullable enable
namespace DataNormalization.Objects;

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