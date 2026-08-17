# Contract — Dựng giấy báo trúng tuyển hàng loạt

Gốc: `api/core/giay-bao`. Yêu cầu Bearer token, **trừ đường tải zip**. Xem luật chung ở [README.md](README.md).
Cơ chế và các quyết định: [docs/dung-giay-bao-tuyen-sinh.md](../docs/dung-giay-bao-tuyen-sinh.md).

## Routes

| Method | Route | Body / Query | `data` trả về |
|---|---|---|---|
| POST | `giay-bao/danh-sach-sheet` | `multipart`: `file` | `ExcelSheetInfo[]` |
| POST | `giay-bao/danh-sach-thi-sinh` | `multipart`: `file`, `sheetName?`, `startRow=1` | `ViewThiSinh[]` |
| POST | `giay-bao/tao-zip` | `multipart`: `file`, `sheetName?`, `startRow=1` | `ZipJob` |
| GET | `giay-bao/tao-zip/{jobId}` | — | `ZipJob` |
| GET | `giay-bao/tao-zip/{jobId}/tai-ve?token=…` | — | **bytes zip thô, không envelope** |

`startRow` là **dòng tiêu đề** trong sheet, đếm từ 1. File Excel được gửi lại ở cả ba lời gọi đầu — server
không giữ file giữa các bước.

## ZipJob

```jsonc
{
  "jobId": "…",
  "taiToken": "…",              // dùng cho đường tải zip
  "tongSo": 5000,
  "daXong": 1234,
  "soLoi": 2,
  "hoanTat": false,
  "loiChung": null,             // sự cố làm hỏng CẢ lô
  "dungLuong": 1160000000,      // tổng byte đã dựng
  "hetHanUtc": "2026-08-14T12:00:00Z",
  "tienToKho": "GiayBaoTrungTuyen/K71/GiayBaoTrungTuyen/",
  "dongLoi": [ { "thuTu": 41, "lyDo": "…" } ],
  "tenFileDaDay": [ "001234567890.pdf", … ]
}
```

`tongSo` là số **dòng hợp lệ** (có họ tên), không phải số dòng trong sheet. `dongLoi[].thuTu` đếm theo danh
sách dòng hợp lệ đó, không phải số dòng trên Excel.

⚠️ **Trạng thái lô nằm trong bộ nhớ tiến trình**, không phải DB: restart API là mất dấu lô đang chạy, và job
cũ tự hết hạn theo `hetHanUtc`. `jobId` không còn ⇒ `1126`.

## Tải zip

`GET giay-bao/tao-zip/{jobId}/tai-ve?token={taiToken}` — không envelope, không Bearer, cùng lý do và cùng cách
dùng như [lo-ky.contract.md](lo-ky.contract.md#tải-zip): FE điều hướng thẳng bằng `window.location`, không tải
qua `HttpClient`.

File nén dựng **ngay lúc tải** từ các bản đã nằm trên kho. Chỉ mở nút tải khi `hoanTat = true`; job sai token
hoặc chưa xong ⇒ **404 trơn**.

## Đầu ra nối sang luồng ký

Bản chưa ký nằm ở `tienToKho`. Muốn ký cả lô thì **không tải zip về rồi upload lại** — dán thẳng đường dẫn đó
vào `POST lo-ky/{id}/them-tu-kho`.

## Mã lỗi

| Code | Ý nghĩa |
|---|---|
| `1120` | Không đọc được file Excel |
| `1121` | Thiếu cột bắt buộc |
| `1122` | Không có dòng nào điền "Họ và Tên thí sinh" |
| `1123` | Bản cài thiếu mẫu HTML giấy báo |
| `1124` | Chưa cấu hình dịch vụ chuyển đổi (Gotenberg) |
| `1125` | Chuyển HTML sang PDF thất bại |
| `1126` | Lô dựng giấy báo không còn tồn tại |
| `1100` / `1101` | Nội dung mã QR rỗng / quá dài |
