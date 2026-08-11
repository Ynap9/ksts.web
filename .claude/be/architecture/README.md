# Kiến trúc Backend KSTS

.NET 9, ASP.NET Core Web API, EF Core + SQL Server, OpenIddict, AutoMapper, NLog.

## Sáu project

```
ksts.be.api             Controllers + Program.cs (composition root) + BaseController + Cert/*.crt
ksts.be.applications    BaseService, MappingProfile, {Feature}/{Interfaces,Implements,Dtos}
ksts.be.domain          Entity thuần: AppUser, Template, TemplatePosition
ksts.be.infrastructure  KstsDbContext, Migrations, Seeder
ksts.be.shared          ApiResponse, ErrorCodes/Messages, Constants, Settings, Templates/sample
ksts.be.external        Dịch vụ bên thứ ba / dùng chung: S3, đọc PDF, đo ảnh, cert, tính vị trí
```

Chiều phụ thuộc:

```
api → { applications, infrastructure, external, shared }
applications → { infrastructure, external }
infrastructure → { domain, shared }
domain → shared
external → shared          ← TỰ CHỨA: sở hữu cả interface lẫn DTO của mình
```

`external` **không** tham chiếu `applications`. Chiều là `applications → external`, không bao giờ ngược lại.

## Đọc tiếp

| File | Nội dung |
|---|---|
| [01-api-controllers.md](01-api-controllers.md) | Controller mỏng, `ApiResponse`, `OkException` |
| [02-application-services.md](02-application-services.md) | Service, `BaseService`, DI |
| [03-dtos-mapping.md](03-dtos-mapping.md) | Quy ước DTO và AutoMapper |
| [04-domain.md](04-domain.md) | Entity, soft delete, audit |
| [05-infrastructure.md](05-infrastructure.md) | DbContext, migration |
| [06-external.md](06-external.md) | Cái gì được vào `external` và vì sao |
| [07-shared.md](07-shared.md) | Constants, Settings, ErrorCodes, paging |
| [08-conventions.md](08-conventions.md) | **Quy ước code bắt buộc** |

## Nguyên tắc không đổi

- Controller **không có nghiệp vụ** — chỉ gọi service rồi bọc `ApiResponse`.
- Service dùng `KstsDbContext` trực tiếp, **không có repository layer**.
- Mọi REST trả **HTTP 200**, trạng thái thật nằm trong `ApiResponse.Status` (`1` = ok, `0` = lỗi).
- Lỗi nghiệp vụ ⇒ `throw new UserFriendlyException(ErrorCodes.X, "…")`, controller bắt bằng `OkException(ex)`.
- Giờ giấc: dùng `BaseService.GetVietnamTime()`, **không** `DateTime.Now`.
- Comment và XML `<summary>` viết **tiếng Việt** — nghiệp vụ là tiếng Việt.
