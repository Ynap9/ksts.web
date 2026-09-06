# Contract — Template cấu hình chữ ký (kssm.be)

> Cập nhật 2026-09-04. Bộ route của **`kssm.be`**, khác bản KSTS ở
> [template-chu-ky.contract.md](template-chu-ky.contract.md) — đọc đúng file của backend mình đang gọi.

Gốc: `api/core/template-chu-ky`. **Không có Bearer token.** Luật envelope chung ở [README.md](README.md).

## Bên gọi là ai

```text
FE  →  sao_mai_be  →  kssm.be  →  SQL Server (KY_SO_SAO_MAI)
                          ↓
                    trả Id template
                          ↓
              sao_mai_be lưu Id vào MongoDB (bảng template ký số)
```

`kssm.be` đứng sau `sao_mai_be` như một **API external**: người dùng đã được `sao_mai_be` xác thực từ trước
nên ở đây không kiểm token, và template **không thuộc về ai cả** — chủ sở hữu do bảng Mongo bên `sao_mai_be`
giữ, khoá theo `Id` mà `POST` đầu tiên trả về.

Hệ quả so với bản KSTS: **không có** `idUser`, không có mã lỗi `1004` (template của người khác), `find-paging`
trả **mọi** template chứ không lọc theo người đăng nhập, và `createdBy` luôn `null`.

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
| GET | `template-chu-ky/file-mau` | — | `{ fileName, exists }` |
| GET | `template-chu-ky/file-mau/noi-dung` | — | **bytes PDF thô** |

**Không có** `vi-tri-goi-y`: dấu đỏ ở đây đặt đúng toạ độ người dùng kéo thả trong template, không dò mốc chữ
trên trang. Mốc dò của KSTS chốt cứng chức danh và tên người ký của Trường Đại học Xây dựng Hà Nội nên với
tài liệu của sao_mai thì gần như luôn trượt.

`file-mau/noi-dung` là endpoint **duy nhất** của module không bọc `ApiResponse` — bên gọi cần bytes thô để
dựng trang. Nó là nền mặc định của màn cấu hình: chưa chọn file nào thì khối được đặt lên file PDF mẫu đi
kèm bản cài.

## Thứ tự gọi bắt buộc

1. `POST template-chu-ky` với `{ tenTemplate }` → nhận `data.id`, **lưu ngay vào Mongo**.
2. `POST template-chu-ky/cau-hinh` (hoặc `PUT` cho lần sau) với `id` đó + chứng thư + ảnh + `positions`.

Tách hai bước vì object key của ảnh chứa `templateId` — chưa có Id thì chưa dựng được chỗ lưu ảnh.

## POST / PUT `template-chu-ky` — body JSON

Chỉ quản lý **tên** template.

| Field | Kiểu | Bắt buộc |
|---|---|---|
| `tenTemplate` | string | ✅ |
| `id` | int | ✅ **chỉ PUT** |

Không kiểm rỗng hay trùng tên ở BE — bên gọi tự validate.

## POST / PUT `template-chu-ky/cau-hinh` — form fields

| Field | Kiểu | Bắt buộc |
|---|---|---|
| `id` | int | ✅ |
| `thumbprint` | string | ✅ |
| `tenChungThu`, `lyDoKy`, `noiKy` | string | — |
| `hienThiChuKySo` | bool | — (mặc định `true`) |
| `nhoiChuKySoVaoAnh` | bool | — |
| `kyDe` | bool | — (mặc định `false`) |
| `mauChuKySo` | string `#RRGGBB` | — (mặc định `#000000`) |
| `mauChuKyTuoi` | string `#RRGGBB`, **rỗng = chưa chọn** | — |
| `doDamDauDo`, `doDamChuKyTuoi` | int, 40–250 | — (mặc định 140) |
| `doDayNetChuKyTuoi` | int, 0–200 | — (mặc định 100) |
| `anhDauDo`, `anhChuKyTuoi` | file (.png/.jpg/.jpeg, ≤ 5 MB) | — |
| `positions[i].kind` … | xem dưới | — |
| `xoaAnhDauDo`, `xoaAnhChuKyTuoi` | bool | — **chỉ PUT** |

Mỗi phần tử gửi dạng `positions[0].kind`, `positions[0].pageNumber`, `positions[0].xRatio`,
`positions[0].yRatio`, `positions[0].widthRatio`, `positions[0].heightRatio`.

Số ngoài khoảng bị **kẹp** về sàn/trần chứ không đánh trượt cả lần lưu; màu sai khuôn lùi về "chưa chọn"
(`mauChuKySo` lùi về `#000000`). Toạ độ nằm ngoài trang thì **ném lỗi** `1005` — sai ở đó là hỏng bản ký.

### Quy tắc ảnh khi PUT

| Gửi gì | Kết quả |
|---|---|
| Có file | Thay ảnh, xoá bản cũ trên kho |
| Không gửi file, cờ xoá = `false` | **Giữ nguyên ảnh cũ** |
| Không gửi file, cờ xoá = `true` | Bỏ ảnh |

Cần cờ xoá riêng vì `multipart` không phân biệt được "không gửi trường" với "gửi null" như JSON.

### PUT `cau-hinh` ghi đè toàn bộ

`positions` bị **thay hết** theo payload, không vá từng phần. Luôn gửi trạng thái đầy đủ đang hiển thị.
Đổi tên template thì dùng `PUT template-chu-ky`, không đụng tới cấu hình ký.

## ViewTemplate

```jsonc
{
  "id": 1,
  "tenTemplate": "Mẫu hợp đồng số hoá",
  "thumbprint": "A1B2…",
  "tenChungThu": "Công ty Sao Mai",
  "lyDoKy": null,
  "noiKy": null,
  "anhDauDoUrl": "http://10.20.0.56:9000/ocrtest/AnhDauVaChuKyTuoi/1/dau-do.png",
  "anhChuKyTuoiUrl": null,
  "hienThiChuKySo": true,
  "nhoiChuKySoVaoAnh": false,
  "kyDe": false,
  "doDamDauDo": 140,
  "doDamChuKyTuoi": 140,
  "doDayNetChuKyTuoi": 100,
  "mauChuKySo": "#000000",
  "mauChuKyTuoi": null,
  "createdDate": "2026-09-03T14:30:00",
  "modifiedDate": null,
  "positions": [
    { "kind": 0, "pageNumber": 1, "xRatio": 0.62, "yRatio": 0.71, "widthRatio": 0.28, "heightRatio": 0.035 }
  ]
}
```

`anhDauDoUrl` là URL công khai trên MinIO, dùng thẳng trong `<img src>`. **Không** có trường object key — đó
là chi tiết nội bộ của BE.

## Enum `positions[].kind`

`0` ChuKy · `1` DauDo · `2` ChuKyTuoi. **Không bao giờ đảo thứ tự** — số đã nằm trong DB.

## Mã lỗi

| Code | Ý nghĩa |
|---|---|
| `1001` | Không tìm thấy template |
| `1003` | Ảnh sai đuôi / rỗng / quá 5 MB |
| `1005` | Toạ độ khối nằm ngoài trang hoặc kích thước bằng 0 |
| `1006` | Bản cài thiếu file PDF mẫu |
| `1040` | Chưa cấu hình kho lưu trữ (thiếu `S3` trong `appsettings.json`) |
| `1041` | Tải ảnh lên MinIO thất bại |

## Bên gọi phải nắm

- **Quan hệ Template ↔ TemplatePosition là quan hệ MỀM** — không navigation, không khoá ngoại, không cascade.
  Xoá template là xoá mềm **cả hai** bảng bằng tay trong cùng một `SaveChanges`; mọi truy vấn toạ độ đều lọc
  `TemplateId` kèm `!Deleted`.
- Lưu cấu hình thì toạ độ cũ bị **xoá hẳn** rồi ghi lại, không xoá mềm: mỗi lần lưu là ghi đè toàn bộ danh
  sách, giữ bản cũ chỉ để lại rác không ai đọc.
- `find-paging` theo quy ước chung: `pageNumber` đếm từ 1, **`pageSize = -1` lấy hết**, `keyword` lọc trên tên
  template.

⚠️ Bẫy đã sập: `kssm.be` thiếu `builder.Services.AddHttpContextAccessor()` từ lúc dựng. `BaseService` nhận
`IHttpContextAccessor` nên **mọi** service nghiệp vụ — kể cả `CertificateService` và `PluginService` đã có từ
trước — ném lỗi dựng ngay lượt gọi đầu tiên, mà thông báo lại chỉ nói về constructor chứ không nói thiếu đăng
ký. Đã thêm cùng lúc đăng ký `ITemplateService`; đừng gỡ dòng đó khi dọn `Program.cs`.
