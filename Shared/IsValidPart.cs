using DataNormalization.Objects;

namespace DataNormalization.Shared;

public class IsValidPartPredicate
{
    public static bool IsValidPart(Part part) => part.PartNumber is not null
                                                  && part.Manufacturer is not null
                                                  && part.Description is not null
                                                  && part.Description.Length >= 3;
}