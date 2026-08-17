# Shared

`ksts.be.shared` giữ thứ mọi tầng dùng chung. **Không có nghiệp vụ ở đây.**

```
Constants/            DateTimeConstants
Constants/Auth/       CustomClaimTypes, PermissionKeys, RoleConstants, ProgramExtensions
Constants/Db/         DbSchemas
Constants/Signing/    SigningConstants, SignatureConstants, SealPlacementConstants, SigningQueueConstants
Constants/Template/   TemplateConstants, TemplatePositionKind
Constants/LoKy/       LoKyConstants, TrangThaiLoKy, TrangThaiFileKy
Constants/GiayBao/    GiayBaoConstants, LoaiTrungTuyenConstants
Constants/Plugin/     PluginConstants
Interfaces/           IFullAudited, ISoftDeleted, ICreatedBy, IModifiedBy
Requests/             ApiResponse, AppException, BaseRequest, ErrorRequest
Settings/             AuthServerSettings, S3Settings, FileSettings, ConvertFileSettings
Templates/sample/     file-mau-ky-so.pdf (copy ra output)
Templates/html/       giay-bao-trung-tuyen.html — mẫu TỰ CHỨA, không gọi CDN
Utils/                CryptoUtils
```

## ApiResponse

```csharp
enum StatusCodeE { Success = 1, Error = 0 }
```

Mọi REST trả **HTTP 200**; trạng thái thật nằm ở `Status`. FE phải gate `status === 1` trước khi đọc `data`.

## ErrorCodes

| Dải | Nhóm |
|---|---|
| `1`, `400`, `404`, `409`, `500` | Mã căn bản |
| `101–106` | Auth |
| `1001–1019` | Template chữ ký |
| `1020–1039` | Chứng thư số |
| `1040–1059` | Lưu trữ file / MinIO |
| `1060–1079` | Đặt dấu & chữ ký tươi |
| `1080–1099` | Plugin ký số ở máy người dùng |
| `1100–1119` | Mã QR tra cứu trên giấy báo |
| `1120–1139` | In giấy báo trúng tuyển hàng loạt (`1127`, `1128` **đã khai tử**, không cấp lại) |
| `1140–1159` | Ký số PDF |
| `1160–1179` | Lô ký số hàng loạt |

Thêm mã mới phải thêm câu tiếng Việt tương ứng vào `ErrorMessages._messages` — thiếu thì FE nhận
`"Unknown error."`.

## Settings

Bind bằng `Configure<T>` trong `Program.cs`, inject bằng `IOptions<T>`.

Khoá JSON của `S3` dùng `SNAKE_CASE` (`S3_URL`, `S3_ACCESS_KEY`) nên property phải gắn
`[ConfigurationKeyName("S3_URL")]` — binder mặc định khớp theo tên property, không tự hiểu snake case.

## Paging

`BaseRequestPagingDto` (`PageNumber` từ 1, `PageSize`, `Keyword` tự trim, `Sort`) →
`.Paging(input)` → `BaseResponsePagingDto<T>` (`Items`, `TotalItems`, `CustomData`).

**`PageSize = -1` là lấy hết**, không phân trang (`PagingParameter.DefaultPageSize`).

Đếm `TotalItems` **trước khi** cắt trang — đó là tổng số bản ghi khớp lọc, không phải số dòng của trang này.

> Quirk giữ nguyên: namespace là `ksts.be.shared.HttpRequest.BaseRequest` trong khi thư mục là `Requests/`.
> Đang được dùng thật.

## Hằng số ký số

`SigningConstants` (tạo chữ ký) và `SignatureConstants` (kiểm chữ ký) bê nguyên từ SIP, **số đo trích từ file
thật** chứ không tự chọn. Đừng sửa nếu không đo lại trên file production.

`SealPlacementConstants` là của KSTS: chuỗi mốc chức danh / tên người ký, dung sai canh cột, DPI mặc định khi
ảnh thiếu metadata.

`SigningQueueConstants` là **ba con số quyết định lô 5000 file chạy bao lâu** — `SoYeuCauMoiDot = 8`,
`GiayChoLayViec = 25`, `GiaySongCuaYeuCau = 120`. Đổi cái nào cũng phải đo lại; lý do từng con số ghi ngay
trong file.

## Hằng số nghiệp vụ giấy báo

`GiayBaoConstants` và `LoaiTrungTuyenConstants` là **chỗ duy nhất** mô tả quan hệ cột Excel ⇄ thẻ HTML ⇄ câu
chữ trên giấy. Sửa câu chữ phải sửa ở đây, không rải vào mẫu HTML hay service.

⚠️ `NamTuyenSinh` và `Khoa` phải đổi **mỗi mùa tuyển sinh**, đồng thời với năm và khoá ghi cứng trong
`Templates/html/giay-bao-trung-tuyen.html`. Thiếu một chỗ là giấy sai năm.

## File PDF mẫu

`Templates/sample/file-mau-ky-so.pdf` copy ra output (`PreserveNewest`), là asset **chỉ đọc** cạnh app nên
resolve từ `AppContext.BaseDirectory`. Dùng để người dùng đặt thử vị trí khi chưa có hồ sơ thật.
