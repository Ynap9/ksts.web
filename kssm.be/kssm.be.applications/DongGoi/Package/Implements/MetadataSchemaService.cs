using AutoMapper;
using kssm.be.applications.Base;
using kssm.be.applications.DongGoi.Package.Dtos;
using kssm.be.applications.DongGoi.Package.Interfaces;
using kssm.be.infrastructure.Persistence;
using kssm.be.shared.Constants.DongGoi;
using kssm.be.shared.Requests.AppException;
using kssm.be.shared.Requests.ErrorRequest;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace kssm.be.applications.DongGoi.Package.Implements
{
    public class MetadataSchemaService : BaseService, IMetadataSchemaService
    {
        public MetadataSchemaService(
            KssmDbContext kstsDbContext,
            IHttpContextAccessor httpContextAccessor,
            ILogger<MetadataSchemaService> logger,
            IMapper mapper
        ) : base(kstsDbContext, logger, httpContextAccessor, mapper)
        {
        }

        public async Task<PackageSchemaDto> GetAsync(PackageType packageType, MetadataObjectType objectType,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"{nameof(GetAsync)} packageType={packageType}, objectType={objectType}");

            var variant = await _kstsDbContext.PackageVariant
                .AsNoTracking()
                .FirstOrDefaultAsync(x => !x.Deleted && x.PackageType == packageType && x.ObjectType == objectType,
                    cancellationToken);

            if (variant == null)
            {
                throw new UserFriendlyException(ErrorCodes.KiemTraChuanChuaCauHinh,
                    $"Chuẩn {PackageTypeLabels.Get(packageType)} chưa cấu hình loại đối tượng "
                    + $"\"{MetadataObjectTypeLabels.Get(objectType)}\".");
            }

            var rows = await (from config in _kstsDbContext.PackageFieldConfig.AsNoTracking()
                              join field in _kstsDbContext.MetadataField.AsNoTracking()
                                  on config.MetadataFieldId equals field.Id
                              where !config.Deleted && !field.Deleted && config.PackageVariantId == variant.Id
                              orderby config.SortOrder
                              select new { Config = config, Field = field })
                .ToListAsync(cancellationToken);

            if (rows.Count == 0)
            {
                throw new UserFriendlyException(ErrorCodes.KiemTraChuanChuaCauHinh,
                    $"Biến thể {variant.VariantKey} chưa có trường nào được cấu hình.");
            }

            var fieldIds = rows.Select(x => x.Field.Id).ToList();

            var aliases = await _kstsDbContext.MetadataFieldAlias
                .AsNoTracking()
                .Where(x => !x.Deleted && fieldIds.Contains(x.MetadataFieldId))
                .ToListAsync(cancellationToken);

            var codeGroups = rows
                .Where(x => !string.IsNullOrWhiteSpace(x.Field.CodeGroup))
                .Select(x => x.Field.CodeGroup!)
                .Distinct()
                .ToList();

            var codes = await _kstsDbContext.CodeValue
                .AsNoTracking()
                .Where(x => !x.Deleted && codeGroups.Contains(x.CodeGroup))
                .OrderBy(x => x.SortOrder)
                .ToListAsync(cancellationToken);

            return new PackageSchemaDto
            {
                ObjectType = objectType,
                VariantKey = variant.VariantKey,
                SheetName = MetadataObjectTypeLabels.Get(objectType),
                Fields = rows.Select(x => new SchemaFieldDto
                {
                    FieldKey = x.Field.FieldKey,
                    DisplayName = x.Field.DisplayName,
                    EadElement = x.Config.EadElement,
                    Aliases = aliases
                        .Where(alias => alias.MetadataFieldId == x.Field.Id)
                        .Select(alias => alias.DisplayName)
                        .ToList(),
                    Requirement = x.Config.Requirement,
                    ConditionField = x.Config.ConditionField,
                    ConditionValues = x.Config.ConditionValues,
                    DataType = x.Config.DataTypeOverride ?? x.Field.DataType,
                    FieldLength = x.Config.FieldLengthOverride ?? x.Field.FieldLength,
                    Codes = codes
                        .Where(code => code.CodeGroup == x.Field.CodeGroup)
                        .Select(code => code.Code)
                        .ToList(),
                }).ToList(),
            };
        }
    }
}
