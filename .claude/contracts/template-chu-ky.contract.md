# Contract — Template cấu hình chữ ký

Gốc: `api/core/template-chu-ky`. Yêu cầu Bearer token. Xem luật chung ở [README.md](README.md).

## Routes

| Method | Route | Content-Type | `data` trả về |
|---|---|---|---|
| POST | `template-chu-ky` | `application/json` | `ViewTemplate` |
| PUT | `template-chu-ky` | `application/json` | `ViewTemplate` |
| POST | `template-chu-ky/cau-hinh` | `multipart/form-data` | `ViewTemplate` |
| PUT | `template-chu-ky/cau-hinh` | `multipart/form-data` | `ViewTemplate` |
| DELETE | `template-chu-ky/{id}` | — | `null` |
| GET | `template-chu-ky/{id}` | — | `ViewTemplate` |
| GET | `template-chu-ky/find-paging` | query | paged `ViewTemplate` |
| GET | `template-chu-ky/file-mau` | — | `SampleFile` |
| GET | `template-chu-ky/file-mau/noi-dung` | — | **bytes PDF thô, không envelope** |
| POST | `template-chu-ky/vi-tri-goi-y` | `multipart/form-data` | `SuggestedPlacement` |

`find-paging` chỉ trả template của **chính người đăng nhập** (lọc theo `createdBy`); tài khoản admin thấy hết.
`keyword` lọc trên tên template.

## POST / PUT `template-chu-ky` — body JSON

Chỉ quản lý **tên** template. Người tạo và ngày tạo do BE tự đặt.

| Field | Kiểu | Bắt buộc |
|---|---|---|
| `tenTemplate` | string | ✅ |
| `id` | int | ✅ **chỉ PUT** |

Không kiểm tra rỗng hay trùng tên ở BE — FE tự validate `required`.

## POST / PUT `template-chu-ky/cau-hinh` — form fields

Phần cấu hình ký của màn chi tiết, chạy trên template **đã tạo**.

| Field | Kiểu | Bắt buộc |
|---|---|---|
| `id` | int | ✅ |
| `thumbprint` | string | ✅ |
| `tenChungThu` | string | — |
| `lyDoKy` | string | — |
| `noiKy` | string | — |
| `hienThiChuKySo` | bool | — (mặc định `true`) |
| `nhoiChuKySoVaoAnh` | bool | — |
| `kyDe` | bool | — (mặc định `false`) |
| `mauChuKySo` | string `#RRGGBB` | — (mặc định `#000000`) |
| `mauChuKyTuoi` | string `#RRGGBB`, **rỗng = chưa chọn** | — (mặc định rỗng) |
| `doDamDauDo` | int, 40–250 | — (mặc định 140) |
| `doDamChuKyTuoi` | int, 40–250 | — (mặc định 140) |
| `doDayNetChuKyTuoi` | int, 0–200 | — (mặc định 100) |
| `anhDauDo` | file (.png/.jpg/.jpeg, ≤ 5 MB) | — |
| `anhChuKyTuoi` | file | — |
| `positions[i].kind` … | xem dưới | — |
| `xoaAnhDauDo` | bool | — **chỉ PUT** |
| `xoaAnhChuKyTuoi` | bool | — **chỉ PUT** |

Mỗi phần tử `positions` gửi dạng `positions[0].kind`, `positions[0].pageNumber`, `positions[0].xRatio`,
`positions[0].yRatio`, `positions[0].widthRatio`, `positions[0].heightRatio`.

### Quy tắc ảnh khi PUT `cau-hinh`

| Gửi gì | Kết quả |
|---|---|
| Có file | Thay ảnh, xoá bản cũ trên kho |
| Không gửi file, cờ xoá = `false` | **Giữ nguyên ảnh cũ** |
| Không gửi file, cờ xoá = `true` | Bỏ ảnh |

Cần cờ xoá riêng vì `multipart` không phân biệt được "không gửi trường" với "gửi null" như JSON.

### Ba cờ và hai màu

`kyDe` là **chốt cửa lúc ký**, không phải hình thức: tắt thì lô ký đánh trượt mọi file nguồn **đã có chữ ký**
(mã `1148`), bật thì ký thêm bình thường và chữ ký cũ vẫn giữ nguyên. FE chỉ mở ô này khi file đang xem trước
thật sự có chữ ký.

Màu ghi dạng `#RRGGBB`. `mauChuKySo` mặc định `#000000` là **chữ đen thật**. `mauChuKyTuoi` nhuộm **ảnh chữ ký
tươi** và **gửi rỗng nghĩa là chưa chọn ⇒ giữ nguyên mực ảnh gốc**; gửi `#000000` là nhuộm đen thật, không còn
là cờ giữ nguyên như bản trước. Giá trị sai khuôn bị BE lùi về "chưa chọn" (`mauChuKySo` lùi về `#000000`) chứ
không đánh trượt cả lần lưu. Ảnh dấu đỏ không có tuỳ chọn màu.

FE lấy giá trị khởi đầu của bảng màu bằng **màu mực trích từ chính ảnh chữ ký tươi**, không phải đen: hiện đen
cho một chữ ký mực xanh là nói dối, và người dùng chọn đen sẽ tưởng vừa đổi màu trong khi không đổi gì.

### PUT `cau-hinh` ghi đè toàn bộ

`positions` bị **thay hết** theo payload, không vá từng phần. Luôn gửi trạng thái đầy đủ đang hiển thị.
Đổi tên template thì dùng `PUT template-chu-ky`, không đụng tới cấu hình ký.

## ViewTemplate

```jsonc
{
  "id": 1,
  "idUser": "…",
  "tenTemplate": "Mẫu giấy báo K71",
  "thumbprint": "A1B2…",
  "tenChungThu": "Trường Đại học Xây dựng Hà Nội",
  "lyDoKy": null,
  "noiKy": null,
  "anhDauDoUrl": "https://s3-2.huce.edu.vn:9000/demo/AnhDauVaChuKyTuoi/1/dau-do.png",
  "anhChuKyTuoiUrl": null,
  "hienThiChuKySo": true,
  "nhoiChuKySoVaoAnh": false,
  "kyDe": false,
  "mauChuKySo": "#000000",
  "mauChuKyTuoi": null,
  "doDamDauDo": 140,
  "doDamChuKyTuoi": 140,
  "doDayNetChuKyTuoi": 100,
  "createdBy": "Nguyễn Văn A",
  "createdDate": "2026-08-10T14:30:00",
  "modifiedDate": null,
  "positions": [
    { "kind": 0, "pageNumber": 1, "xRatio": 0.62, "yRatio": 0.71, "widthRatio": 0.28, "heightRatio": 0.035 }
  ]
}
```

`anhDauDoUrl` là URL công khai trên MinIO, dùng thẳng trong `<img src>`. **Không** có trường object key —
đó là chi tiết nội bộ của BE.

## SampleFile / SuggestedPlacement

```jsonc
// GET file-mau
{ "fileName": "file-mau-ky-so.pdf", "exists": true }
```

```jsonc
// POST vi-tri-goi-y — form: filePdf?, anhDauDo?, anhChuKyTuoi?, pageNumber (mặc định 1)
{
  "pageNumber": 1,
  "pageWidthPoints": 595.0, "pageHeightPoints": 842.0,
  "anchorChucDanh": "PHÓ HIỆU TRƯỞNG",
  "anchorTenNguoiKy": "PGS.TS BÙI PHÚ DOANH",
  "midXRatio": 0.775, "midYRatio": 0.793,
  "dauDo":      { "xRatio": 0.69, "yRatio": 0.74, "widthRatio": 0.17, "heightRatio": 0.12 },
  "chuKyTuoi":  { "xRatio": 0.67, "yRatio": 0.75, "widthRatio": 0.21, "heightRatio": 0.09 },
  "canhBao": null
}
```

- `midXRatio`/`midYRatio` = **trung điểm** đoạn nối chức danh với tên người ký — chỗ đóng dấu.
- `dauDo` **giữ nguyên kích thước gốc của ảnh** (suy từ pixel + DPI). FE **không được** cho người dùng kéo
  giãn con dấu; chỉ được kéo **di chuyển**.
- `chuKyTuoi` được co giãn, trần bằng 25% bề rộng trang.
- Không gửi ảnh nào thì trường tương ứng là `null`.
- `canhBao` khác `null` khi ảnh dấu không khai DPI — hiển thị cho người dùng biết dấu có thể sai cỡ.

## Enum `positions[].kind`

`0` ChuKy · `1` DauDo · `2` ChuKyTuoi. **Không bao giờ đảo thứ tự** — số đã nằm trong DB.

## Mã lỗi

| Code | Ý nghĩa |
|---|---|
| `1001` | Không tìm thấy template |
| `1002` | Trùng tên template (không còn kiểm ở BE) |
| `1003` | Ảnh sai đuôi / rỗng / quá 5 MB |
| `1004` | Template của người dùng khác |
| `1005` | Toạ độ khối nằm ngoài trang hoặc kích thước bằng 0 |
| `1041` | Tải ảnh lên MinIO thất bại |
| `1060` | Không dò được mốc đặt dấu trên tài liệu |
| `1061` | Không đọc được kích thước ảnh (chỉ hỗ trợ PNG/JPEG) |
| `1062` | Bản cài thiếu file PDF mẫu |
