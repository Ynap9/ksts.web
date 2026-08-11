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

## Đăng ký DI

Tất cả `AddScoped` trong `Program.cs`, khối `#region service`:

```csharp
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<ICertificateService, CertificateService>();

builder.Services.AddSingleton<IS3FileStorage, S3FileStorage>();
builder.Services.AddSingleton<ICertificateProvider, CertificateProvider>();
builder.Services.AddSingleton<ICertificateTrustValidator, CertificateTrustValidator>();
builder.Services.AddSingleton<IPdfTextLocator, PdfTextLocator>();
builder.Services.AddSingleton<IImageSizeReader, ImageSizeReader>();
builder.Services.AddSingleton<ISealPlacementResolver, SealPlacementResolver>();
```

Service của `applications` là **Scoped** (đụng DbContext). Service của `external` là **Singleton** — chúng
không giữ trạng thái theo request, và `S3FileStorage` giữ `AmazonS3Client` nên tạo lại mỗi request là lãng phí
kết nối.
