#nullable enable
namespace DataNormalization.Objects;

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