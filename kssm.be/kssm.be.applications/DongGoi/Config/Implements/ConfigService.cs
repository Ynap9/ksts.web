using AutoMapper;
using ClosedXML.Excel;
using kssm.be.applications.Base;
using kssm.be.applications.DongGoi.Config.Dtos;
using kssm.be.applications.DongGoi.Config.Interfaces;
using kssm.be.infrastructure.Persistence;
using kssm.be.shared.Constants.DongGoi;
using kssm.be.shared.Requests.AppException;
using kssm.be.shared.Requests.BaseRequest;
using kssm.be.shared.Requests.ErrorRequest;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kssm.be.applications.DongGoi.Config.Implements
{
    public class ConfigService: BaseService,IConfigService
    {
    
        public ConfigService(
            KssmDbContext kstsDbContext,
            IHttpContextAccessor httpContextAccessor,
            ILogger<ConfigService> logger,
            IMapper mapper
        ) : base(kstsDbContext, logger, httpContextAccessor, mapper)
        {
        }

        public async Task<GetDropDownTypeTT05Dto> GetDropDownTypeTT05Async()
        {
            _logger.LogInformation($"{nameof(GetDropDownTypeTT05Async)}");

            var variants = await _kstsDbContext.PackageVariant
                .AsNoTracking()
                .Where(x => !x.Deleted)
                .OrderBy(x => x.PackageType)
                .ThenBy(x => x.ObjectType)
                .ToListAsync();

            var objectType = variants
                .GroupBy(x => x.PackageType)
                .Select(group => new GetDropDownTypeTT05ValueDto
                {
                    Value = PackageTypeLabels.Get(group.Key),
                    PackageType = group.Key,
                    ObjectTypes = group
                        .Select(x => x.ObjectType)
                        .Distinct()
                        .Select(value => new GetDropDownObjectTypeDto
                        {
                            Value = value,
                            DisplayName = MetadataObjectTypeLabels.Get(value),
                        })
                        .ToList(),
                })
                .ToList();

            return new GetDropDownTypeTT05Dto
            {
                ObjectType = objectType
            };
        }

        public async Task<BaseResponsePagingDto<ViewPackageFieldDto>> FindPagingAsync(FindPagingPackageFieldDto input)
        {
            _logger.LogInformation(
                $"{nameof(FindPagingAsync)} packageType={input.PackageType}, objectType={input.ObjectType}");

            var keyword = input.Keyword;
            var objectType = input.ObjectType;

            var query = from config in _kstsDbContext.PackageFieldConfig.AsNoTracking()
                        join variant in _kstsDbContext.PackageVariant.AsNoTracking()
                            on config.PackageVariantId equals variant.Id
                        join field in _kstsDbContext.MetadataField.AsNoTracking()
                            on config.MetadataFieldId equals field.Id
                        where !config.Deleted && !variant.Deleted && !field.Deleted
                            && variant.PackageType == input.PackageType
                            && (objectType == null || variant.ObjectType == objectType)
                            && (string.IsNullOrWhiteSpace(keyword)
                                || field.DisplayName.Contains(keyword)
                                || config.EadElement.Contains(keyword))
                        select new { Config = config, Variant = variant, Field = field };

            var totalItems = await query.CountAsync();

            var rows = await query
                .OrderBy(x => x.Variant.ObjectType)
                .ThenBy(x => x.Config.SortOrder)
                .Paging(input)
                .ToListAsync();

            var codeGroups = rows
                .Where(x => !string.IsNullOrWhiteSpace(x.Field.CodeGroup))
                .Select(x => x.Field.CodeGroup!)
                .Distinct()
                .ToList();

            var codes = await _kstsDbContext.CodeValue
                .AsNoTracking()
                .Where(x => !x.Deleted && codeGroups.Contains(x.CodeGroup))
                .OrderBy(x => x.SortOrder)
                .ToListAsync();

            var items = rows
                .Select(x => new ViewPackageFieldDto
                {
                    Id = x.Config.Id,
                    VariantKey = x.Variant.VariantKey,
                    ObjectType = x.Variant.ObjectType,
                    FieldKey = x.Field.FieldKey,
                    DisplayName = x.Field.DisplayName,
                    Description = x.Config.DescriptionOverride ?? x.Field.Description,
                    EadElement = x.Config.EadElement,
                    Requirement = x.Config.Requirement,
                    ConditionField = x.Config.ConditionField,
                    ConditionValues = x.Config.ConditionValues,
                    DataType = x.Config.DataTypeOverride ?? x.Field.DataType,
                    FieldLength = x.Config.FieldLengthOverride ?? x.Field.FieldLength,
                    SortOrder = x.Config.SortOrder,
                    Codes = codes
                        .Where(code => code.CodeGroup == x.Field.CodeGroup)
                        .Select(code => new ViewCodeValueDto
                        {
                            Code = code.Code,
                            DisplayName = code.DisplayName,
                        })
                        .ToList(),
                })
                .ToList();

            return new BaseResponsePagingDto<ViewPackageFieldDto>
            {
                Items = items,
                TotalItems = totalItems,
            };
        }

        public async Task<ExportFileDto> ExportExcelAsync(ExportPackageFieldDto input)
        {
            _logger.LogInformation(
                $"{nameof(ExportExcelAsync)} packageType={input.PackageType}, objectType={input.ObjectType}");

            var objectTypes = input.ObjectType ?? new List<MetadataObjectType>();

            var query = from config in _kstsDbContext.PackageFieldConfig.AsNoTracking()
                        join variant in _kstsDbContext.PackageVariant.AsNoTracking()
                            on config.PackageVariantId equals variant.Id
                        join field in _kstsDbContext.MetadataField.AsNoTracking()
                            on config.MetadataFieldId equals field.Id
                        where !config.Deleted && !variant.Deleted && !field.Deleted
                            && variant.PackageType == input.PackageType
                        select new { variant.ObjectType, config.SortOrder, field.DisplayName };

            if (objectTypes.Count > 0)
            {
                query = query.Where(x => objectTypes.Contains(x.ObjectType));
            }

            var rows = await query
                .OrderBy(x => x.ObjectType)
                .ThenBy(x => x.SortOrder)
                .ToListAsync();

            if (rows.Count == 0)
            {
                throw new UserFriendlyException(
                    ErrorCodes.NotFound,
                    "Chuẩn đóng gói và loại đối tượng này chưa có cấu hình trường để xuất.");
            }

            using var workbook = new XLWorkbook();

            foreach (var group in rows.GroupBy(x => x.ObjectType))
            {
                var headers = group.Select(x => x.DisplayName).ToList();
                var sheet = workbook.Worksheets.Add(MetadataObjectTypeLabels.Get(group.Key));

                for (var index = 0; index < headers.Count; index++)
                {
                    sheet.Cell(1, index + 1).Value = headers[index];
                }

                var headerRange = sheet.Range(1, 1, 1, headers.Count);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                headerRange.Style.Alignment.WrapText = true;

                sheet.SheetView.FreezeRows(1);
                sheet.Columns().AdjustToContents(
                    XuatExcelConstants.DoRongCotToiThieu,
                    XuatExcelConstants.DoRongCotToiDa);
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return new ExportFileDto
            {
                Content = stream.ToArray(),
                FileName = string.Format(
                    XuatExcelConstants.TenFileMau,
                    PackageTypeLabels.Get(input.PackageType)) + XuatExcelConstants.Extension,
            };
        }
    }
}
