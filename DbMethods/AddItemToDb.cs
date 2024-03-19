using System;
using System.Threading.Tasks;
using DataNormalization.Models;
using DataNormalization.Objects;
using Microsoft.EntityFrameworkCore;

namespace DataNormalization.DbMethods;

public class AddItemToDb
{
    public static async Task AddItemToDbAsync(Part part)
    {
        try
        {
            await using var context = new AesContext(new DbContextOptions<AesContext>());

            // Determine if manufacturer exists and add it if it doesn't
            var manufacturer = await context.Manufacturers.FirstOrDefaultAsync(m => m.ManfName == part.Manufacturer);

            if (manufacturer is null)
                await context.AddAsync(new tblManufacturers { ManfName = part.Manufacturer, AuthorizedDistributor = true });

            // If manufacturer exists and is not an authorized distributor, update it to be an authorized distributor
            if (manufacturer is not null && manufacturer.AuthorizedDistributor is not true)
                manufacturer.AuthorizedDistributor = true;

            // Retrieve the Lifecycle ID if it exists
            var lifecycleId = await context.LifeCycles.FirstOrDefaultAsync(l => l.lifecycle.ToLower() == part.LifecycleStatus.ToLower());

            // Add a part to the database
            var addedPart = await context.ItemInfo.AddAsync(new EquipmentService_ItemInfo
            {
                PartNumber = part.PartNumber,
                Description = part.Description,
                Manufacturer = part.Manufacturer,
                Uom = part.Uom,
                Weight = part.Weight,
                WeightUnit = part.WeightUnit,
                ListPrice = part.ListPrice,
                CreatedBy = "SYSTEM",
                lifecycleId = lifecycleId?.id
            });

            await context.SaveChangesAsync();

            // Add dimensions to the database if they exist and update the part with the dimension ID
            if (part.Dimensions is not null && part.Dimensions.Volume > 0)
            {
                // Get the dimension UOM ID
                var uom = await context.DimensionsUom.FirstOrDefaultAsync(u => u.Uom == part.Dimensions.DimensionUnit);

                if (uom?.UomId is not null)
                {
                    var dimension = await context.ItemDimensions.AddAsync(new EquipmentService_ItemDimensions
                    {
                        // We can safely assume that none of the dimensions are null or 0 because the volume is > 0
                        Height = part.Dimensions.Height!.Value,
                        Length = part.Dimensions.Length!.Value,
                        Width = part.Dimensions.Width!.Value,
                        UomId = uom.UomId
                    });

                    await context.SaveChangesAsync();

                    addedPart.Entity.DimensionId = dimension.Entity.DimensionId;
                }
            }

            // Add template values to the database if they exist
            if (part.Template is not null && !string.IsNullOrEmpty(part.Template?.Name))
            {
                // Get a template with the specified name
                var template = await context.ItemInfoTemplate.FirstOrDefaultAsync(x => x.TemplateName.ToLower() == part.Template.Name.ToLower());

                if (template?.TemplateId is not null)
                {
                    await context.ItemInfoTemplateValues.AddAsync(new EquipmentService_ItemInfoTemplateValues
                    {
                        ItemInfoId = addedPart.Entity.ItemInfoId,
                        TemplateId = template.TemplateId,
                        FieldValue1 = part.Template.Value1,
                        FieldValue2 = part.Template.Value2,
                        FieldValue3 = part.Template.Value3,
                        FieldValue4 = part.Template.Value4,
                        FieldValue5 = part.Template.Value5,
                        FieldValue6 = part.Template.Value6,
                        FieldValue7 = part.Template.Value7,
                        FieldValue8 = part.Template.Value8,
                        FieldValue9 = part.Template.Value9,
                        FieldValue10 = part.Template.Value10
                    });
                }
            }

            // Add vendor prices to the database if Our Price has a value


            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to add Part Number {part.PartNumber} from Manufacturer {part.Manufacturer}.");
            Console.WriteLine(ex);
        }
    }
}