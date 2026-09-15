using AutoMapper;
using kssm.be.applications.Base;
using kssm.be.applications.DongGoi.Package.Interfaces;
using kssm.be.infrastructure.Persistence;
using kssm.be.shared.Constants.DongGoi;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text;

namespace kssm.be.applications.DongGoi.Package.Implements
{
    public class DossierLayoutService : BaseService, IDossierLayoutService
    {
        public DossierLayoutService(
            KssmDbContext kstsDbContext,
            IHttpContextAccessor httpContextAccessor,
            ILogger<DossierLayoutService> logger,
            IMapper mapper
        ) : base(kstsDbContext, logger, httpContextAccessor, mapper)
        {
        }

        public string ChuanHoaDuongDan(string? relativePath, string fileName)
        {
            var nguon = string.IsNullOrWhiteSpace(relativePath) ? fileName : relativePath;
            var thay = nguon.Replace('\\', '/').Trim().Trim('/');
            return thay.Normalize(NormalizationForm.FormC);
        }

        public string LayThuMucGoc(string relativePath)
        {
            var doan = TachDoan(relativePath);
            return doan.Length >= 2 ? doan[0] : string.Empty;
        }

        public string LayThuMuc(string relativePath)
        {
            var doan = TachDoan(relativePath);
            return doan.Length >= 2 ? string.Join('/', doan[..^1]) : string.Empty;
        }

        public string TenHienThi(string thuMuc)
        {
            var doan = TachDoan(thuMuc);
            return doan.Length > 0 ? doan[^1] : string.Empty;
        }

        public string LayThuMucCha(string thuMuc)
        {
            var doan = TachDoan(thuMuc);
            return doan.Length > 1 ? string.Join('/', doan[..^1]) : string.Empty;
        }

        public string? TimExcelGanNhat(string thuMuc, IReadOnlyDictionary<string, string> excelTheoThuMuc)
        {
            string? totNhat = null;
            var daiNhat = -1;

            foreach (var cap in excelTheoThuMuc)
            {
                if (!BaoTrum(cap.Key, thuMuc) || cap.Key.Length <= daiNhat)
                {
                    continue;
                }

                daiNhat = cap.Key.Length;
                totNhat = cap.Value;
            }

            return totNhat;
        }

        public string? TimMaHoSoKhopNhat(string docId, IEnumerable<string> danhSachMa)
        {
            string? totNhat = null;

            foreach (var ma in danhSachMa)
            {
                if (string.IsNullOrWhiteSpace(ma) || !LaMaCuaTaiLieu(ma, docId))
                {
                    continue;
                }

                if (totNhat == null || ma.Length > totNhat.Length)
                {
                    totNhat = ma;
                }
            }

            return totNhat;
        }

        public bool BaoTrum(string pham, string thuMuc)
        {
            if (string.IsNullOrEmpty(pham))
            {
                return true;
            }

            return string.Equals(pham, thuMuc, StringComparison.OrdinalIgnoreCase)
                || thuMuc.StartsWith(pham + '/', StringComparison.OrdinalIgnoreCase);
        }

        public bool LaMaCuaTaiLieu(string maHoSo, string docId)
        {
            return string.Equals(maHoSo, docId, StringComparison.OrdinalIgnoreCase)
                || docId.StartsWith(maHoSo + KiemTraConstants.DauNoiMaTaiLieu, StringComparison.OrdinalIgnoreCase);
        }

        public string[] TachDoan(string duongDan)
        {
            return duongDan.Split('/', StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
