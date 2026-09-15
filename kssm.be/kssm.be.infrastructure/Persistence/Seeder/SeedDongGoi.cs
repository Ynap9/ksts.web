using kssm.be.domain.DongGoi;
using kssm.be.shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace kssm.be.infrastructure.Persistence.Seeder
{
    public static class SeedDongGoi
    {
        public static async Task SeedAsync(KssmDbContext db)
        {
            await SeedCodeValuesAsync(db);
            await SeedMetadataFieldsAsync(db);
            await SeedPackageVariantsAsync(db);
            await SeedPackageFieldConfigsAsync(db);
        }

        public static async Task SeedCodeValuesAsync(KssmDbContext db)
        {
            var known = (await db.CodeValue
                    .Where(x => !x.Deleted)
                    .Select(x => new { x.CodeGroup, x.Code })
                    .ToListAsync())
                .Select(x => $"{x.CodeGroup}|{x.Code}")
                .ToHashSet();

            var added = DongGoiSeedData.CodeValues
                .Where(x => !known.Contains($"{x.CodeGroup}|{x.Code}"))
                .Select(x => new CodeValue
                {
                    CodeGroup = x.CodeGroup,
                    Code = x.Code,
                    DisplayName = x.DisplayName,
                    SortOrder = x.SortOrder,
                    CreatedDate = DateTimeConstants.VietnamNow,
                })
                .ToList();

            if (added.Count == 0) return;

            db.CodeValue.AddRange(added);
            await db.SaveChangesAsync();
        }

        public static async Task SeedMetadataFieldsAsync(KssmDbContext db)
        {
            var known = (await db.MetadataField
                    .Where(x => !x.Deleted)
                    .Select(x => x.FieldKey)
                    .ToListAsync())
                .ToHashSet();

            var added = DongGoiSeedData.MetadataFields
                .Where(x => !known.Contains(x.FieldKey))
                .Select(x => new MetadataField
                {
                    FieldKey = x.FieldKey,
                    DisplayName = x.DisplayName,
                    Description = x.Description,
                    DataType = x.DataType,
                    FieldLength = x.FieldLength,
                    CodeGroup = x.CodeGroup,
                    CreatedDate = DateTimeConstants.VietnamNow,
                })
                .ToList();

            if (added.Count == 0) return;

            db.MetadataField.AddRange(added);
            await db.SaveChangesAsync();
        }

        public static async Task SeedPackageVariantsAsync(KssmDbContext db)
        {
            var known = (await db.PackageVariant
                    .Where(x => !x.Deleted)
                    .Select(x => x.VariantKey)
                    .ToListAsync())
                .ToHashSet();

            var added = DongGoiSeedData.PackageVariants
                .Where(x => !known.Contains(x.VariantKey))
                .Select(x => new PackageVariant
                {
                    VariantKey = x.VariantKey,
                    PackageType = x.PackageType,
                    ObjectType = x.ObjectType,
                    CreatedDate = DateTimeConstants.VietnamNow,
                })
                .ToList();

            if (added.Count == 0) return;

            db.PackageVariant.AddRange(added);
            await db.SaveChangesAsync();
        }

        public static async Task SeedPackageFieldConfigsAsync(KssmDbContext db)
        {
            var fieldIds = await db.MetadataField
                .Where(x => !x.Deleted)
                .ToDictionaryAsync(x => x.FieldKey, x => x.Id);

            var variantIds = await db.PackageVariant
                .Where(x => !x.Deleted)
                .ToDictionaryAsync(x => x.VariantKey, x => x.Id);

            var known = (await db.PackageFieldConfig
                    .Where(x => !x.Deleted)
                    .Select(x => new { x.PackageVariantId, x.MetadataFieldId })
                    .ToListAsync())
                .Select(x => $"{x.PackageVariantId}|{x.MetadataFieldId}")
                .ToHashSet();

            var added = new List<PackageFieldConfig>();

            foreach (var variant in DongGoiSeedData.PackageVariants)
            {
                if (!variantIds.TryGetValue(variant.VariantKey, out var variantId)) continue;

                for (var index = 0; index < variant.Fields.Count; index++)
                {
                    var field = variant.Fields[index];
                    if (!fieldIds.TryGetValue(field.FieldKey, out var fieldId)) continue;
                    if (known.Contains($"{variantId}|{fieldId}")) continue;

                    added.Add(new PackageFieldConfig
                    {
                        PackageVariantId = variantId,
                        MetadataFieldId = fieldId,
                        EadElement = field.EadElement ?? field.FieldKey,
                        Requirement = field.Requirement,
                        ConditionField = field.ConditionField,
                        ConditionValues = field.ConditionValues,
                        SortOrder = index + 1,
                        DescriptionOverride = field.DescriptionOverride,
                        DataTypeOverride = field.DataTypeOverride,
                        FieldLengthOverride = field.FieldLengthOverride,
                        CreatedDate = DateTimeConstants.VietnamNow,
                    });
                }
            }

            if (added.Count == 0) return;

            db.PackageFieldConfig.AddRange(added);
            await db.SaveChangesAsync();
        }
    }
}
