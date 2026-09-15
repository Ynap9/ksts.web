using AutoMapper;
using kssm.be.applications.Base;
using kssm.be.applications.DongGoi.Package.Dtos;
using kssm.be.applications.DongGoi.Package.Interfaces;
using kssm.be.external.DongGoi.Interfaces;
using kssm.be.infrastructure.Persistence;
using kssm.be.shared.Constants.DongGoi;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace kssm.be.applications.DongGoi.Package.Implements
{
    public class HeaderMatchService : BaseService, IHeaderMatchService
    {
        private readonly ITextSimilarity _textSimilarity;

        public HeaderMatchService(
            KssmDbContext kstsDbContext,
            IHttpContextAccessor httpContextAccessor,
            ILogger<HeaderMatchService> logger,
            IMapper mapper,
            ITextSimilarity textSimilarity
        ) : base(kstsDbContext, logger, httpContextAccessor, mapper)
        {
            _textSimilarity = textSimilarity;
        }

        public HeaderMatchReportDto Match(PackageSchemaDto schema, IReadOnlyList<string> headers)
        {
            var report = new HeaderMatchReportDto
            {
                ObjectType = schema.ObjectType,
                SheetName = schema.SheetName,
                VariantKey = schema.VariantKey,
            };

            var ungVien = new List<(int FieldIndex, int HeaderIndex, double Score)>();

            for (var f = 0; f < schema.Fields.Count; f++)
            {
                for (var h = 0; h < headers.Count; h++)
                {
                    if (string.IsNullOrWhiteSpace(headers[h]))
                    {
                        continue;
                    }

                    var diem = DiemCaoNhat(schema.Fields[f], headers[h]);
                    if (diem >= KiemTraConstants.NguongGhepTen)
                    {
                        ungVien.Add((f, h, diem));
                    }
                }
            }

            var fieldDaGhep = new Dictionary<int, (int HeaderIndex, double Score)>();
            var headerDaGhep = new HashSet<int>();

            foreach (var cap in ungVien.OrderByDescending(x => x.Score))
            {
                if (fieldDaGhep.ContainsKey(cap.FieldIndex) || headerDaGhep.Contains(cap.HeaderIndex))
                {
                    continue;
                }

                fieldDaGhep[cap.FieldIndex] = (cap.HeaderIndex, cap.Score);
                headerDaGhep.Add(cap.HeaderIndex);
            }

            for (var f = 0; f < schema.Fields.Count; f++)
            {
                var field = schema.Fields[f];
                var dong = new FieldMatchDto
                {
                    FieldKey = field.FieldKey,
                    DisplayName = field.DisplayName,
                    Requirement = field.Requirement,
                };

                if (fieldDaGhep.TryGetValue(f, out var ghep))
                {
                    dong.MatchedHeader = headers[ghep.HeaderIndex];
                    dong.Score = Math.Round(ghep.Score, 4);
                    report.Matched.Add(dong);
                }
                else
                {
                    report.Missing.Add(dong);
                }
            }

            for (var h = 0; h < headers.Count; h++)
            {
                if (!headerDaGhep.Contains(h) && !string.IsNullOrWhiteSpace(headers[h]))
                {
                    report.ExtraHeaders.Add(headers[h]);
                }
            }

            report.Passed = report.Missing.All(x => x.Requirement != FieldRequirement.Required);
            return report;
        }

        public double DiemCaoNhat(SchemaFieldDto field, string header)
        {
            var cao = _textSimilarity.Score(field.DisplayName, header);

            foreach (var alias in field.Aliases)
            {
                cao = Math.Max(cao, _textSimilarity.Score(alias, header));
            }

            return cao;
        }

        public string? ChonSheet(IReadOnlyList<string> sheetNames, string tenChuan)
        {
            string? tot = null;
            var diemTot = 0d;

            foreach (var ten in sheetNames)
            {
                var diem = _textSimilarity.Score(ten, tenChuan);
                if (diem >= KiemTraConstants.NguongGhepTen && diem > diemTot)
                {
                    diemTot = diem;
                    tot = ten;
                }
            }

            return tot;
        }

        public string? ChonHeader(HeaderMatchReportDto report, string fieldKey)
        {
            return report.Matched
                .FirstOrDefault(x => string.Equals(x.FieldKey, fieldKey, StringComparison.Ordinal))
                ?.MatchedHeader;
        }
    }
}
