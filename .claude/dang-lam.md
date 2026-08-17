# Đang làm dở — đọc file này đầu phiên

> Cập nhật 2026-08-17. Chỉ ghi **trạng thái và việc kế tiếp**; tri thức bền vững nằm ở `docs/`, `contracts/`
> và `be/architecture/`, đừng chép lại vào đây.

## Vừa xong (2026-08-17 → 18), chưa chạy thử trên máy thật

Ba tuỳ chọn mới của template — migration `ThemKyDeVaMauChuKy` và `MauChuKyTuoiChoPhepTrong` **đã sinh, chưa
`database update`**:

1. **Cờ ký đè** (`KyDe`): tắt thì lô ký đánh trượt file nguồn đã có chữ ký (`1148`), bật thì ký thêm bình
   thường. Mặc định tắt nên template cũ được bảo vệ sẵn.
2. **Tự cuộn khung xem trước khi kéo khối** ở màn cấu hình template.
3. **Màu khối chữ ký số và màu mực chữ ký tươi** (`MauChuKySo`, `MauChuKyTuoi`). `MauChuKyTuoi` **để trống là
   chưa chọn ⇒ giữ nguyên mực ảnh gốc**; đen nay là màu thật, nhuộm được. Bảng màu trên màn cấu hình khởi đầu
   bằng **màu mực trích từ chính ảnh** (`InkColorService` đọc pixel qua canvas).

Còn phải mắt thấy: nhuộm ảnh chữ ký tươi **thang xám** (đường bảng màu `/Indexed`) trên file ký thật, đối
chiếu bản xem trước FE với bản in ra, và **màu trích ra có đúng không** — cả lúc vừa chọn ảnh (blob cùng gốc)
lẫn lúc mở lại template (ảnh từ kho object; kho thiếu tiêu đề CORS thì canvas bị nhiễm và bảng màu lùi về đen).

## Việc kế tiếp

**Chạy thử một lô thật trên prod với token cắm ở máy người dùng.** Toàn bộ đường ký đã chạy được từ đầu tới
cuối trên máy dev; thứ chưa có là **token thật**.

Ba việc đi kèm, làm ngay trong lần chạy thử đó:

1. **Đo `T`** — thời gian một lượt ký qua token — bằng `POST plugin ky-so/do-toc-do`. Đây là con số cuối cùng
   còn thiếu để biết lô 5000 file mất bao lâu: token ký tuần tự nên `T = 200 ms` là riêng phần ký đã 16 ph 40,
   không rút ngắn được bằng thêm luồng.
2. **Kiểm hộp PIN có hiện chìm sau trình duyệt không.** Plugin chạy nền không sở hữu cửa sổ; nếu chìm thì
   hướng sửa là cho nó chạy dạng tray app có cửa sổ ẩn rồi truyền HWND vào thuộc tính CNG `"HWND Handle"`.
3. **Xem lô 5000 file qua đường zip mới**: tiến độ có nhích đều không, và tải file ~4 GB trình duyệt có nhận
   không.

## Đọc gì trước khi động vào

| Việc định làm | Đọc |
|---|---|
| Bất cứ thứ gì thuộc luồng ký | [docs/luong-ky-so-hang-loat.md](docs/luong-ky-so-hang-loat.md) |
| Sửa API lô ký / màn ký số | [contracts/lo-ky.contract.md](contracts/lo-ky.contract.md) |
| Sửa plugin | [contracts/plugin-ky-so.contract.md](contracts/plugin-ky-so.contract.md) · [plugin/plans/](plugin/plans/) |
| Sửa luồng dựng giấy báo | [docs/dung-giay-bao-tuyen-sinh.md](docs/dung-giay-bao-tuyen-sinh.md) |
| Sửa bất cứ màn hình FE nào | [fe/architecture/](fe/architecture/README.md) |
| Sửa bất cứ tầng BE nào | [be/architecture/](be/architecture/README.md) |
| Bàn chuyện bảo mật nâng cao | [docs/bao-mat-agent-ky-so.md](docs/bao-mat-agent-ky-so.md) — 🔬 nghiên cứu |

## Hai giới hạn đã biết, phải nói với người dùng

⚠️ **Đóng tab là lô dừng.** Trang web là người đưa thư giữa máy chủ và token. File đã ký giữ nguyên và vẫn hợp
lệ; bấm Bắt đầu lại thì chạy tiếp từ file dở.

⚠️ **Mở lại màn hình giữa lô thì chưa nối lại được vòng đưa thư** — thấy đúng tiến độ nhưng không ai mang chữ
ký đi, các lượt ký hết hạn sau 120 giây và file tính lỗi. Chưa làm nút nối lại vì phải hỏi PIN lần nữa; cần
quyết định giao diện trước.

## Việc treo, chưa tới lượt

1. **Dung lượng PDF** — mỗi tờ 915 KB, **~72% là nội dung vector** của lưới kỹ thuật 10mm và hình compa. Muốn
   kéo 3,9 GB xuống thật thì phải sửa mỹ thuật của mẫu — **đang chờ quyết định của người dùng**.
2. **Người dùng nhập PIN hai lần một lô** (xác thực chứng thư, rồi mở phiên ký). Bỏ bước xác thực thì lỗi cert
   sai hiện muộn hơn, sau khi đã tải file lên. Chưa quyết.
3. **Giám sát rút token** ở plugin (quét cert store mỗi 2s). Hiện rút token giữa lô biểu hiện thành một loạt
   file lỗi thay vì một thông báo rõ ràng.
4. **Mùa tuyển sinh sau**: đổi `GiayBaoConstants.NamTuyenSinh` + `Khoa` + năm/khoá ghi cứng trong
   `Templates/html/giay-bao-trung-tuyen.html`, cả ba cùng lúc.
