# Application Services

## Vị trí

```
ksts.be.applications/{Feature}/Interfaces/I{Feature}Service.cs
ksts.be.applications/{Feature}/Implements/{Feature}Service.cs
ksts.be.applications/{Feature}/Dtos/*.cs
```

| Feature | Service |
|---|---|
| `Auth` | `IUsersService`, `IRoleService`, `IPermissionsService` |
| `Template` | `ITemplateService` |
| `Signing` | `ICertificateService` |
| `Plugin` | `IPluginService` — phát bộ cài plugin |
| `GiayBao` | `IGiayBaoService` — dựng giấy báo trúng tuyển hàng loạt |
| `LoKy` | `ILoKyService` (Scoped) + `IKySoRunner` (**Singleton**, chạy nền) |

## BaseService

Mọi service kế thừa `BaseService` để có sẵn `_kstsDbContext`, `_logger`, `_httpContextAccessor`, `_mapper` và:

| Thành viên | Dùng khi |
|---|---|
| `getCurrentUserId()` | Lấy `IdUser` để lọc dữ liệu theo người dùng |
| `getCurrentName()` | Ghi `CreatedBy` / `ModifiedBy` |
| `IsSuperAdmin()` | Cho admin xem vượt phạm vi của mình |
| `GetVietnamTime()` | **Mọi mốc thời gian** — không dùng `DateTime.Now` |

## Khuôn một service method

```csharp
/// <summary>Tạo template mới, upload ảnh dấu đỏ / chữ ký tươi lên MinIO nếu có.</summary>
public async Task<ViewTemplateDto> CreateAsync(AddTemplateDto input)
{
    _logger.LogInformation($"{nameof(CreateAsync)} input={JsonSerializer.Serialize(input.TenTemplate)}");
    ...
}
```

- Log **một dòng ở đầu** mỗi method public.
- Lỗi nghiệp vụ ⇒ `throw new UserFriendlyException(ErrorCodes.X, "câu tiếng Việt cho người dùng")`.
- Trả **DTO**, không bao giờ trả entity.
- **Không viết hàm `private`.** Việc lặp lại thì đẩy sang một service ở `external` có interface riêng.

## Phạm vi dữ liệu theo người dùng

`Template.IdUser` giữ chủ sở hữu. Mọi truy vấn template lọc theo `getCurrentUserId()`; `IsSuperAdmin()` thì
thấy hết. Tên template chỉ cần duy nhất **trong phạm vi một người dùng**.

> Đây là quyết định mặc định khi dựng module, chưa được nghiệp vụ xác nhận. Nếu template phải dùng chung toàn
> trường thì bỏ bộ lọc và đổi ràng buộc trùng tên thành phạm vi toàn hệ thống.

## Việc chạy nền, sống lâu hơn request

`IKySoRunner` (lô ký) và `IZipJobStore` (lô dựng giấy báo) là **Singleton**: lô sống hàng chục phút, dài hơn
mọi request. Hệ quả bắt buộc nhớ:

- Chúng **không được** inject `KstsDbContext` trực tiếp — DbContext là Scoped. Phải nhận `IServiceScopeFactory`
  rồi **tự mở scope riêng cho từng file**.
- Chúng nằm ngoài `BaseService` nên không có `GetVietnamTime()`; dùng `DateTimeConstants.VietnamNow`.
- Trạng thái lô dựng giấy báo nằm trong **bộ nhớ tiến trình** — restart API là mất dấu. Lô ký thì nằm ở DB nên
  không sao.

## Đăng ký DI

Tất cả trong `Program.cs`, khối `#region service`:

```csharp
builder.Services.AddScoped<ITemplateService, TemplateService>();     // applications: Scoped (đụng DbContext)
builder.Services.AddScoped<ILoKyService, LoKyService>();
builder.Services.AddScoped<IGiayBaoService, GiayBaoService>();

builder.Services.AddSingleton<IS3FileStorage, S3FileStorage>();      // external: Singleton
builder.Services.AddSingleton<ICertificateTrustValidator, CertificateTrustValidator>();
builder.Services.AddSingleton<IPdfPreparer, PdfPreparer>();
builder.Services.AddSingleton<IHangDoiKy, HangDoiKy>();              // giữ trạng thái phiên của từng lô
builder.Services.AddSingleton<IKySoRunner, KySoRunner>();            // tiến trình ký chạy nền
```

Service của `external` là **Singleton** — không giữ trạng thái theo request, và `S3FileStorage` giữ
`AmazonS3Client` nên tạo lại mỗi request là lãng phí kết nối.

**Nguồn ký chọn bằng cấu hình**, đây là seam để đổi chỗ giữ khoá mà không đụng service lẫn controller:

```csharp
// Signing:Nguon = "store" -> StoreSigningKey (cert store của MÁY CHẠY API, chỉ dùng khi cùng máy)
// bỏ trống              -> PluginSigningKey (mặc định: khoá ở token máy người dùng)
builder.Services.AddSingleton<ISigningKey, PluginSigningKey>();
```
