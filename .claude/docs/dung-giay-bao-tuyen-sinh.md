# Dựng giấy báo trúng tuyển hàng loạt

> Viết 2026-08-14 cho phần **đã chạy thật**. Đây là khâu đứng **trước** luồng ký:
> [luong-ky-so-hang-loat.md](luong-ky-so-hang-loat.md) nhận đầu ra của khâu này.

## Đường đi

```
Excel danh sách trúng tuyển
   ↓ IExcelSheetReader          chọn sheet + dòng tiêu đề, đọc thành Dictionary theo TÊN cột
   ↓ GiayBaoConstants           bản đồ tên cột -> id thẻ trên mẫu HTML
   ↓ IQrCodeSvgRenderer         mã QR tra cứu, nhúng thẳng dạng SVG
   ↓ IHtmlDocumentFiller        đổ giá trị vào mẫu HTML tự chứa, ẩn/hiện khối theo loại trúng tuyển
   ↓ IGotenbergConverter        HTML -> PDF (dịch vụ ngoài, 8 bản song song)
   ↓ IS3FileStorage             đẩy NGAY lên kho rồi buông khỏi bộ nhớ
GiayBaoTrungTuyen/{khoá}/GiayBaoTrungTuyen/{soCCCD}.pdf
```

Lô chạy **nền**: `tao-zip` trả `jobId` ngay rồi FE hỏi tiến độ, thay vì giữ một request chạy 30 phút. Trạng
thái lô nằm trong `IZipJobStore` — **bộ nhớ tiến trình**, không phải DB, nên restart API là mất dấu lô đang
chạy.

## Ba quyết định không được đảo

**1. Dựng xong file nào đẩy ngay file đó lên kho.** Bản đầu gom cả lô vào một file nén tạm trên đĩa: 5000 giấy
báo là gần 4 GB, đủ làm đầy ổ máy chủ — mà đầy ổ thì Gotenberg hỏng theo và cả lô chết giữa chừng. Nay đĩa máy
chủ không giữ gì.

**2. File nén dựng NGAY LÚC TẢI.** `tao-zip/{jobId}/tai-ve` kéo từng file từ kho về rồi ghi thẳng vào luồng gửi
cho trình duyệt, tải trước `SoFileTaiTruocKhiNen = 8` file trong khi đang ghi file hiện tại. Không có file nén
nào tồn tại trên máy chủ.

**3. Ô "Loại bằng cấp" trống hoặc ghi mã lạ thì ĐỂ TRỐNG các thẻ liên quan**, không lùi về mã mặc định như bản
dựng thiết kế. Đoán sai sinh ra một tờ giấy trông hoàn chỉnh nhưng sai loại bằng và sai phương thức — người
soát không thấy; để trống thì thấy ngay.

## Mã loại trúng tuyển quyết định gì

`LoaiTrungTuyenConstants` là **chỗ duy nhất** mô tả quan hệ mã ⇄ câu chữ. Mỗi mã (`100_CN`, `DBĐH_KS`,
`TuyenThang_KTS`, …) khai ba thứ: **loại bằng cấp** (KS / KTS / CN, kèm số năm để suy niên khoá), **bố cục
trang** và **câu phương thức xét tuyển**.

| Bố cục | Khác ở chỗ |
|---|---|
| `full` | Câu mở "đã trúng tuyển ngành/chuyên ngành", có dòng điểm cộng, thủ tục chính quy |
| `dubi` | Câu mở "đủ điều kiện trúng tuyển", hiện dòng trường dự bị, thủ tục riêng cho dự bị |
| `tuyenthang` | Câu mở gộp luôn phương thức, hiện dòng đối tượng tuyển thẳng |

Câu mở đầu, vế nối và câu phương thức đi thành **bộ ba** theo bố cục — đặt lẻ một cái là ra câu văn lai giữa
hai mẫu giấy khác nhau.

Đối chiếu mã theo khoá đã **chuẩn hoá** (`NormalizeKey`) chứ không so khớp thô: file kết xuất hay ghi `DBDH_CN`
thiếu dấu, mà khớp trượt thì thí sinh dự bị lặng lẽ nhận giấy chính quy — sai mà không ai thấy.

## Đọc Excel

Đối chiếu theo **tên cột**, không theo thứ tự, nên file đảo cột hay thừa cột vẫn nhồi đúng. Một thẻ nhận
**nhiều** tên cột vì mỗi đợt kết xuất đặt tên một khác (`Họ và Tên thí sinh` ↔ `Họ tên`, `Số CCCD` ↔ `Số ĐDCN`)
— lấy tên nào có mặt trước trong danh sách. Giữ cả hai cách gọi thay vì bắt người dùng sửa tiêu đề trên file
vài nghìn dòng.

Dòng thiếu **họ tên** thì bỏ qua cả dòng: không xác định được giấy báo của ai. Không dòng nào hợp lệ ⇒
`ExcelNoValidRow`.

## Đặt tên file

Tên file là **đúng số định danh** (CCCD): không dấu, không khoảng trắng, vừa tra cứu được vừa làm object key
sạch. Dòng thiếu số định danh thì lùi về số thứ tự. Trùng tên trong cùng lô thì thêm hậu tố `-2`, `-3`.

Nhờ vậy bản ký giữ **nguyên tên** bản gốc, hai thư mục đối chiếu được ngay:

```
GiayBaoTrungTuyen/K71/GiayBaoTrungTuyen/001234567890.pdf         <- chưa ký
GiayBaoTrungTuyen/K71/GiayBaoTrungTuyenDaKySo/001234567890.pdf   <- đã ký
```

## Phải đổi mỗi mùa tuyển sinh

Ba chỗ, **đổi đồng thời**, thiếu một là giấy sai năm:

1. `GiayBaoConstants.NamTuyenSinh` — thay vào `{nam}` của câu phương thức và dùng để suy niên khoá.
2. `GiayBaoConstants.Khoa` — tách thư mục trên kho.
3. Mẫu HTML `Templates/html/giay-bao-trung-tuyen.html` — ô "KHÓA" của khung tên và dòng "Khóa 71".

## Số đo thật

- Dựng 5000 giấy báo qua Gotenberg: **1685 s (~28 phút)**, zip **3860 MB**, đồng thời 8.
- Throughput Gotenberg bão hoà quanh **2,9 file/s**; đồng thời 12 không nhanh hơn đáng kể so với 8
  (`ConvertFile.MaxDongThoi` trong `appsettings.json`).
- Mẫu HTML tự chứa sau khi gọn font: **648 KB**. **Không gọi CDN** — Gotenberg dựng trong container không ra
  được Internet, mà thiếu font là giấy hỏng hàng loạt.

⚠️ **Mỗi tờ 915 KB, ~72% là nội dung vector** của lưới kỹ thuật 10mm và hình compa. Muốn kéo 3,9 GB xuống thật
thì phải sửa mỹ thuật của mẫu — đang chờ quyết định.

## Hợp đồng API

Xem [../contracts/giay-bao.contract.md](../contracts/giay-bao.contract.md).
