using ksts.plugin.applications.CaiDat.Interfaces;
using ksts.plugin.external.Setup.Interfaces;
using ksts.plugin.shared.Constants;

namespace ksts.plugin.applications.CaiDat.Implements
{
    /// <inheritdoc/>
    public class CaiDatService : ICaiDatService
    {
        private readonly IMiddlewareService _middlewareService;
        private readonly ITuCaiDatService _tuCaiDatService;

        public CaiDatService(IMiddlewareService middlewareService, ITuCaiDatService tuCaiDatService)
        {
            _middlewareService = middlewareService;
            _tuCaiDatService = tuCaiDatService;
        }

        /// <inheritdoc/>
        public bool LaLuotChayPlugin() =>
            !_tuCaiDatService.LaBanPhatHanh() || _tuCaiDatService.DangChayTuThuMucCai();

        /// <inheritdoc/>
        public void ChayLuotCaiDat()
        {
            Console.WriteLine($"Cài {PluginConstants.Ten} {PluginConstants.PhienBan}");
            Console.WriteLine();

            Console.WriteLine("[1/2] Trình đọc USB token...");
            LoMiddleware();

            Console.WriteLine("[2/2] Plugin ký số...");
            _tuCaiDatService.DungBanDangChay();
            _tuCaiDatService.ChepVaoThuMucCai();
            _tuCaiDatService.BatTuKhoiDong();
            _tuCaiDatService.GhiMucGoCaiDat();
            _tuCaiDatService.ChayBanDaCai();
            Console.WriteLine($"      Đã cài vào {CaiDatConstants.DuongDanThuMucCai()} và bật tự khởi động.");

            Console.WriteLine();
            Console.WriteLine("HOÀN TẤT. Quay lại trang ký số và bấm Kiểm tra lại.");
        }

        /// <summary>
        /// Phần middleware của lượt cài. Máy đã có thì bỏ qua; chưa có mà bản này không nhúng kèm thì báo rõ
        /// rồi vẫn cài plugin tiếp — plugin chạy được, chỉ là chưa thấy chứng thư trên token.
        /// </summary>
        public void LoMiddleware()
        {
            if (_middlewareService.DaCoTrenMay())
            {
                Console.WriteLine("      Máy đã có sẵn, bỏ qua.");
                return;
            }

            if (!_middlewareService.CoBanNhungKem())
            {
                Console.WriteLine("      CHƯA CÓ trình đọc token, mà bản cài này không kèm sẵn.");
                Console.WriteLine("      Plugin vẫn cài được nhưng SẼ KHÔNG THẤY chứng thư trên token.");
                Console.WriteLine("      Lấy bộ cài bit4id từ đơn vị cấp chứng thư số rồi cài trước.");
                return;
            }

            if (!_tuCaiDatService.DangCoQuyenQuanTri())
            {
                Console.WriteLine("      Cần quyền quản trị để cài, đang xin nâng quyền...");
                var maThoat = _tuCaiDatService.ChayLaiVoiQuyenQuanTri(CaiDatConstants.ThamSoCaiMiddleware);

                if (maThoat != 0)
                {
                    throw new InvalidOperationException(
                        $"Bước cài trình đọc token với quyền quản trị thất bại (mã {maThoat}).");
                }

                Console.WriteLine("      Cài xong.");
                return;
            }

            _middlewareService.CaiNgam();
            Console.WriteLine("      Cài xong.");
        }

        /// <inheritdoc/>
        public void ChayLuotCaiMiddleware()
        {
            Console.WriteLine("Đang cài trình đọc USB token ở chế độ ngầm...");
            _middlewareService.CaiNgam();
        }

        /// <inheritdoc/>
        public void ChayLuotGoCaiDat()
        {
            Console.WriteLine($"Gỡ {PluginConstants.Ten}");

            _tuCaiDatService.XoaTuKhoiDong();
            _tuCaiDatService.XoaMucGoCaiDat();
            _tuCaiDatService.DungBanDangChay();
            _tuCaiDatService.XoaThuMucCai();
            _tuCaiDatService.HenXoaThuMucCai();

            Console.WriteLine("Đã gỡ plugin. Trình đọc token bit4id được giữ nguyên vì phần mềm khác còn dùng.");
        }
    }
}
