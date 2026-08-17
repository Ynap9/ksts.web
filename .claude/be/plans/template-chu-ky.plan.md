# Plan — Template cấu hình chữ ký

> **Trạng thái: ✅ đã thi công.** Hợp đồng đang chạy (có thêm `cau-hinh`, `vi-tri-goi-y` dạng POST, ba thanh
> trượt độ đậm / độ dày): [../../contracts/template-chu-ky.contract.md](../../contracts/template-chu-ky.contract.md)
> — đọc contract chứ đừng đọc bảng API dưới đây, nó là bản dự kiến ban đầu.

## Requirements

CRUD bộ cấu hình chữ ký dựng sẵn để người dùng không phải kéo thả lại mỗi lần ký: chứng thư số, lý do/nơi ký,
ảnh dấu đỏ, ảnh chữ ký tươi, toạ độ từng khối.

- Ảnh upload lên **kho object** (bucket khai ở `S3:S3_BUCKET`), DB lưu **URL + object key**.
- Dấu đỏ và chữ ký tươi **tuỳ chọn** — có thể không có cái nào.
- Template thuộc về **người dùng đang đăng nhập**; admin xem hết.
- Trả thêm **vị trí gợi ý** cho dấu / chữ ký tươi tính từ file PDF mẫu.

## Steps

1. **Domain** — `Template` đổi 4 trường ảnh từ `*Path`/`*NguonPath` sang `AnhDauDoUrl` +
   `AnhDauDoObjectKey` + `AnhChuKyTuoiUrl` + `AnhChuKyTuoiObjectKey`. Cặp `*NguonPath` của SIP vô nghĩa trên
   web (không có đường dẫn ổ đĩa người dùng).
2. **Infrastructure** — thêm `DbSet<Template>`, `DbSet<TemplatePosition>`, quan hệ cascade, 2 index theo
   `IdUser`. Sinh migration.
3. **Shared** — `TemplateConstants` thêm tiền tố object key + tên file chuẩn; `ErrorCodes` 1001–1019 +
   1040–1059 kèm `ErrorMessages`.
4. **External** — `IS3FileStorage` (upload / xoá / dựng URL công khai).
5. **Application** — `ITemplateService` + DTO (`Add`, `Update`, `View`, `FindPaging`, `Position`, `SampleFile`).
6. **API** — `TemplateController`, route `api/core/template-chu-ky`, POST/PUT nhận `multipart/form-data`.
7. **DI** — đăng ký trong `Program.cs`; `Configure<S3Settings>` + `Configure<FileSettings>`.

## API

| Method | Route | Body | Trả về |
|---|---|---|---|
| POST | `template-chu-ky` | `AddTemplateDto` (form-data) | `ViewTemplateDto` |
| PUT | `template-chu-ky` | `UpdateTemplateDto` (form-data) | `ViewTemplateDto` |
| DELETE | `template-chu-ky/{id}` | — | `null` |
| GET | `template-chu-ky/{id}` | — | `ViewTemplateDto` |
| GET | `template-chu-ky/find-paging` | query paging | paged `ViewTemplateDto` |
| GET | `template-chu-ky/file-mau` | — | `SampleFileDto` |
| GET | `template-chu-ky/vi-tri-goi-y` | — | `SuggestedPlacementDto` |

## Quy tắc ảnh

| Tình huống | Hành vi |
|---|---|
| Tạo mới, không gửi ảnh | Không upload, `Url` và `ObjectKey` để `null` |
| Tạo mới, có ảnh | Kiểm đuôi + dung lượng → upload → lưu `Url` + `ObjectKey` |
| Sửa, gửi ảnh mới | Xoá object cũ → upload mới → ghi đè |
| Sửa, không gửi ảnh, `XoaAnhDauDo = false` | **Giữ nguyên** ảnh cũ |
| Sửa, `XoaAnhDauDo = true` | Xoá object → set `null` |
| Xoá template | Xoá mềm bản ghi + xoá cả hai object; xoá object hỏng chỉ `LogWarning` |

Cần cờ `XoaAnhDauDo` / `XoaAnhChuKyTuoi` riêng vì `multipart` không phân biệt được "không gửi trường" với
"gửi null" như JSON.

Upload **sau** `SaveChangesAsync` đầu tiên: object key chứa `templateId`, mà Id chỉ có sau khi lưu.

## Expected output

- 7 endpoint chạy, trả `ApiResponse` envelope.
- Trùng tên template trong phạm vi một người dùng ⇒ `ErrorCodes.TemplateNameDuplicated`.
- Ảnh sai đuôi / quá 5 MB ⇒ `ErrorCodes.TemplateImageInvalid`.
- Người dùng A không GET/PUT/DELETE được template của B (trừ admin).
- `dotnet build` sạch.
