using ksts.plugin.external.Setup.Interfaces;
using ksts.plugin.shared.Constants;
using Microsoft.Win32;
using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;

namespace ksts.plugin.external.Setup.Implements
{
    /// <inheritdoc/>
    public class TuCaiDatService : ITuCaiDatService
    {
        /// <inheritdoc/>
        public bool DangChayTuThuMucCai()
        {
            var dangChay = Path.GetDirectoryName(Environment.ProcessPath);
            if (string.IsNullOrEmpty(dangChay)) return false;

            return string.Equals(
                Path.TrimEndingDirectorySeparator(dangChay),
                Path.TrimEndingDirectorySeparator(CaiDatConstants.DuongDanThuMucCai()),
                StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc/>
        // IL3000 cảnh báo Location trả chuỗi rỗng trong bản single-file — ở đây chuỗi rỗng CHÍNH LÀ dấu hiệu
        // cần tìm, nên tắt cảnh báo thay vì đổi cách viết.
#pragma warning disable IL3000
        public bool LaBanPhatHanh() => string.IsNullOrEmpty(Assembly.GetEntryAssembly()?.Location);
#pragma warning restore IL3000

        /// <inheritdoc/>
        public bool DangCoQuyenQuanTri()
        {
            using var danhTinh = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(danhTinh).IsInRole(WindowsBuiltInRole.Administrator);
        }

        /// <inheritdoc/>
        public int ChayLaiVoiQuyenQuanTri(string thamSo)
        {
            var thongTin = new ProcessStartInfo(Environment.ProcessPath!, thamSo)
            {
                UseShellExecute = true,
                Verb = "runas"
            };

            using var tienTrinh = Process.Start(thongTin)
                ?? throw new InvalidOperationException("Khong nang quyen duoc.");

            tienTrinh.WaitForExit();
            return tienTrinh.ExitCode;
        }

        /// <inheritdoc/>
        public void DungBanDangChay()
        {
            var chinhMinh = Environment.ProcessId;
            var ten = Path.GetFileNameWithoutExtension(CaiDatConstants.TenExe);

            foreach (var tienTrinh in Process.GetProcessesByName(ten))
            {
                using (tienTrinh)
                {
                    if (tienTrinh.Id == chinhMinh) continue;

                    try
                    {
                        tienTrinh.Kill();
                        tienTrinh.WaitForExit(CaiDatConstants.ChoNhaFileMs * 4);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
                    {
                        // Tiến trình vừa tự thoát, hoặc là của người dùng khác trên cùng máy - không đụng tới.
                    }
                }
            }

            Thread.Sleep(CaiDatConstants.ChoNhaFileMs);
        }

        /// <inheritdoc/>
        public void ChepVaoThuMucCai()
        {
            var thuMuc = CaiDatConstants.DuongDanThuMucCai();
            Directory.CreateDirectory(thuMuc);

            File.Copy(Environment.ProcessPath!, Path.Combine(thuMuc, CaiDatConstants.TenExe), overwrite: true);
        }

        /// <inheritdoc/>
        public void BatTuKhoiDong()
        {
            using var khoa = Registry.CurrentUser.CreateSubKey(CaiDatConstants.KhoaAutostart);
            khoa.SetValue(CaiDatConstants.TenAutostart, $"\"{DuongExeDaCai()}\"", RegistryValueKind.String);
        }

        /// <inheritdoc/>
        public void GhiMucGoCaiDat()
        {
            using var khoa = Registry.CurrentUser.CreateSubKey(CaiDatConstants.KhoaGoCaiDat);
            var exe = DuongExeDaCai();

            khoa.SetValue("DisplayName", PluginConstants.Ten);
            khoa.SetValue("DisplayVersion", PluginConstants.PhienBan);
            khoa.SetValue("Publisher", "Truong Dai hoc Xay dung Ha Noi");
            khoa.SetValue("DisplayIcon", exe);
            khoa.SetValue("InstallLocation", CaiDatConstants.DuongDanThuMucCai());
            khoa.SetValue("UninstallString", $"\"{exe}\" {CaiDatConstants.ThamSoGoCaiDat}");
            khoa.SetValue("NoModify", 1, RegistryValueKind.DWord);
            khoa.SetValue("NoRepair", 1, RegistryValueKind.DWord);
        }

        /// <inheritdoc/>
        public void ChayBanDaCai()
        {
            Process.Start(new ProcessStartInfo(DuongExeDaCai()) { UseShellExecute = true });
        }

        /// <inheritdoc/>
        public void XoaTuKhoiDong()
        {
            using var khoa = Registry.CurrentUser.OpenSubKey(CaiDatConstants.KhoaAutostart, writable: true);
            khoa?.DeleteValue(CaiDatConstants.TenAutostart, throwOnMissingValue: false);
        }

        /// <inheritdoc/>
        public void XoaMucGoCaiDat()
        {
            Registry.CurrentUser.DeleteSubKeyTree(CaiDatConstants.KhoaGoCaiDat, throwOnMissingSubKey: false);
        }

        /// <summary>
        /// Xoá thư mục cài. Bỏ qua lỗi vì lệnh gỡ thường do CHÍNH file exe trong thư mục đó chạy: file đang
        /// chạy thì Windows không cho xoá, phần còn lại vẫn phải dọn sạch.
        /// </summary>
        public void XoaThuMucCai()
        {
            var thuMuc = CaiDatConstants.DuongDanThuMucCai();
            if (!Directory.Exists(thuMuc)) return;

            foreach (var file in Directory.EnumerateFiles(thuMuc))
            {
                try { File.Delete(file); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            }

            try { Directory.Delete(thuMuc, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }

        /// <inheritdoc/>
        public void HenXoaThuMucCai()
        {
            var thuMuc = CaiDatConstants.DuongDanThuMucCai();
            if (!Directory.Exists(thuMuc)) return;

            var thongTin = new ProcessStartInfo("cmd.exe",
                $"/c timeout /t 3 /nobreak > nul & rd /s /q \"{thuMuc}\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };

            Process.Start(thongTin);
        }

        /// <summary>Đường dẫn file exe sau khi đã cài.</summary>
        public string DuongExeDaCai() =>
            Path.Combine(CaiDatConstants.DuongDanThuMucCai(), CaiDatConstants.TenExe);
    }
}
