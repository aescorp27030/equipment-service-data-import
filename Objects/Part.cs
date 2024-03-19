namespace DataNormalization.Objects;

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