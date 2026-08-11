using ksts.plugin.external.Setup.Interfaces;
using ksts.plugin.shared.Constants;
using Microsoft.Win32;
using System.Diagnostics;
using System.Reflection;

namespace ksts.plugin.external.Setup.Implements
{
    /// <inheritdoc/>
    public class MiddlewareService : IMiddlewareService
    {
        /// <inheritdoc/>
        public bool DaCoTrenMay()
        {
            string[] duong =
            [
                @"SOFTWARE\Microsoft\Cryptography\Defaults\Provider",
                @"SOFTWARE\Microsoft\Cryptography\Providers"
            ];

            foreach (var d in duong)
            {
                using var khoa = Registry.LocalMachine.OpenSubKey(d);
                if (khoa is null) continue;

                if (khoa.GetSubKeyNames().Any(ten =>
                        ten.Contains("bit4id", StringComparison.OrdinalIgnoreCase) ||
                        ten.Contains("bit4xpki", StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }

            var system32 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System));
            return Directory.Exists(system32) && Directory.EnumerateFiles(system32, "bit4*.dll").Any();
        }

        /// <inheritdoc/>
        public bool CoBanNhungKem() => TimTaiNguyen() is not null;

        /// <summary>
        /// Tên tài nguyên nhúng của bộ cài middleware, null nếu bản build này không kèm. Tài nguyên nằm
        /// trong assembly khởi động (KstsPlugin) chứ không phải assembly chứa lớp này.
        /// </summary>
        public string? TimTaiNguyen()
        {
            var ten = Assembly.GetEntryAssembly()?.GetManifestResourceNames() ?? [];

            if (ten.Contains(CaiDatConstants.TaiNguyenMiddlewareExe)) return CaiDatConstants.TaiNguyenMiddlewareExe;
            if (ten.Contains(CaiDatConstants.TaiNguyenMiddlewareMsi)) return CaiDatConstants.TaiNguyenMiddlewareMsi;

            return null;
        }

        /// <inheritdoc/>
        public void CaiNgam()
        {
            var taiNguyen = TimTaiNguyen()
                ?? throw new InvalidOperationException("Ban nay khong nhung kem bo cai middleware.");

            var duongTam = BungRaFileTam(taiNguyen);

            try
            {
                ChayBoCai(duongTam);
            }
            finally
            {
                // Xoá được thì tốt, không xoá được cũng không sao: file nằm trong thư mục tạm của Windows.
                try { File.Delete(duongTam); } catch (IOException) { }
            }
        }

        /// <summary>Bung tài nguyên nhúng ra file tạm để chạy được như một bộ cài bình thường.</summary>
        public string BungRaFileTam(string taiNguyen)
        {
            var duoi = Path.GetExtension(taiNguyen);
            var duong = Path.Combine(Path.GetTempPath(), $"ksts-middleware-{Guid.NewGuid():N}{duoi}");

            using var nguon = Assembly.GetEntryAssembly()!.GetManifestResourceStream(taiNguyen)!;
            using var dich = File.Create(duong);
            nguon.CopyTo(dich);

            return duong;
        }

        /// <summary>
        /// Chạy bộ cài ở chế độ ngầm và chờ xong. Mã thoát khác 0 và khác 3010 là hỏng thật, phải ném ra để
        /// người dùng biết middleware chưa cài được thay vì tưởng đã xong.
        /// </summary>
        public void ChayBoCai(string duongBoCai)
        {
            var laMsi = Path.GetExtension(duongBoCai).Equals(".msi", StringComparison.OrdinalIgnoreCase);

            var thongTin = laMsi
                ? new ProcessStartInfo("msiexec.exe", $"/i \"{duongBoCai}\" {CaiDatConstants.CoNgamMsi}")
                : new ProcessStartInfo(duongBoCai, CaiDatConstants.CoNgamExe);

            thongTin.UseShellExecute = false;

            using var tienTrinh = Process.Start(thongTin)
                ?? throw new InvalidOperationException("Khong khoi chay duoc bo cai middleware.");

            tienTrinh.WaitForExit();

            if (tienTrinh.ExitCode != 0 && tienTrinh.ExitCode != CaiDatConstants.MaLoiCanKhoiDongLai)
            {
                throw new InvalidOperationException($"Bo cai middleware tra ve ma loi {tienTrinh.ExitCode}.");
            }
        }
    }
}
