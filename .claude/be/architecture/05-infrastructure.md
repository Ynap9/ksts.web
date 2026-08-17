# Infrastructure

`ksts.be.infrastructure` giữ EF Core: `KstsDbContext`, `Migrations/`, `Persistence/Seeder/`.

## KstsDbContext

Một DbContext duy nhất, kế thừa `IdentityDbContext<AppUser>`, dùng chung cho Identity, OpenIddict và nghiệp vụ.

```csharp
public DbSet<Template> Template { get; set; }
public DbSet<TemplatePosition> TemplatePosition { get; set; }
public DbSet<LoKy> LoKy { get; set; }
public DbSet<LoKyFile> LoKyFile { get; set; }
```

`OnModelCreating` phải giữ đúng thứ tự đang có:

```csharp
modelBuilder.UseOpenIddict();
modelBuilder.HasDefaultSchema(DbSchemas.Core);
base.OnModelCreating(modelBuilder);
// cấu hình entity nghiệp vụ đặt SAU base
```

Cấu hình `Template`:

- `Positions` quan hệ 1-n, `OnDelete(Cascade)` — xoá cứng template thì position đi theo, không để mồ côi.
- Index `(IdUser, Deleted)`: mọi truy vấn danh sách đều lọc theo hai cột này.
- Index `(IdUser, TenTemplate)`: phục vụ kiểm trùng tên trong phạm vi một người dùng.

Cấu hình `LoKy` / `LoKyFile`: `Files` quan hệ 1-n cascade; index `(IdUser, Deleted)` cho lô,
`(LoKyId, TrangThai, ThuTu)` để **lấy việc kế tiếp** — truy vấn đó chạy một lần cho *mỗi* file của lô 5000
file — và `(LoKyId, TenFile)` để khử trùng khi upload gửi lại một đợt.

Kết nối: `ConnectionStrings:KY_SO_WEB`, SQL Server, `CommandTimeout(600)`.

## Migration

Chạy từ thư mục `ksts.be`, chỉ định rõ project chứa migration:

```bash
dotnet ef migrations add <Ten> -p ksts.be.infrastructure -s ksts.be.api
dotnet ef database update    -p ksts.be.infrastructure -s ksts.be.api
```

`-s ksts.be.api` là bắt buộc: chuỗi kết nối và cấu hình OpenIddict nằm ở project khởi động.

## Seeder

`Persistence/Seeder/SeedUser.cs` chạy trong scope ngay sau `app.Build()`, tạo role + tài khoản mặc định.
Seeder phải **idempotent** — nó chạy lại mỗi lần khởi động.

## Không có repository layer

Service dùng `KstsDbContext` trực tiếp qua `BaseService._kstsDbContext`. Đừng thêm repository/unit-of-work trừ
khi được yêu cầu.
