#nullable enable
namespace DataNormalization.Objects;

public class Dimensions
{
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public string? DimensionUnit { get; set; }

    public decimal? Volume => Length ?? 0 * Width ?? 0 * Height ?? 0;
}