# Shared

`ksts.be.shared` giữ thứ mọi tầng dùng chung. **Không có nghiệp vụ ở đây.**

```
Constants/Auth/       CustomClaimTypes, PermissionKeys, RoleConstants, ProgramExtensions
Constants/Db/         DbSchemas
Constants/Signing/    SigningConstants, SignatureConstants, SealPlacementConstants
Constants/Template/   TemplateConstants, TemplatePositionKind
Interfaces/           IFullAudited, ISoftDeleted, ICreatedBy, IModifiedBy
Requests/             ApiResponse, AppException, BaseRequest, ErrorRequest
Settings/             AuthServerSettings, S3Settings, FileSettings
Templates/sample/     file-mau-ky-so.pdf (copy ra output)
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

## File PDF mẫu

`Templates/sample/file-mau-ky-so.pdf` copy ra output (`PreserveNewest`), là asset **chỉ đọc** cạnh app nên
resolve từ `AppContext.BaseDirectory`. Dùng để người dùng đặt thử vị trí khi chưa có hồ sơ thật.
