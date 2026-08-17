# Domain

`ksts.be.domain` chứa entity thuần. Chỉ tham chiếu `ksts.be.shared` (interface audit + enum).

## Entity

| Entity | Bảng | Ghi chú |
|---|---|---|
| `AppUser` | Identity | Kế thừa `IdentityUser` |
| `Template` | `Template` | Bộ cấu hình chữ ký dựng sẵn của một người dùng |
| `TemplatePosition` | `TemplatePosition` | Toạ độ một khối, thuộc về một `Template` |
| `LoKy` | `LoKy` | Một lô ký số hàng loạt |
| `LoKyFile` | `LoKyFile` | Một file trong lô, kèm trạng thái ký |

## Template

```csharp
public class Template : ISoftDeleted
{
    public int Id { get; set; }
    public string IdUser { get; set; }        // chủ sở hữu, lấy từ token
    public string TenTemplate { get; set; }
    public string Thumbprint { get; set; }    // chứng thư đã chọn
    public string? TenChungThu { get; set; }  // CHỈ để hiển thị, không dùng tra cứu
    public string? LyDoKy { get; set; }
    public string? NoiKy { get; set; }

    public string? AnhDauDoUrl { get; set; }        // URL công khai MinIO
    public string? AnhDauDoObjectKey { get; set; }  // key để xoá/thay ảnh
    public string? AnhChuKyTuoiUrl { get; set; }
    public string? AnhChuKyTuoiObjectKey { get; set; }

    public List<TemplatePosition> Positions { get; set; }
    // + CreatedBy/Date, ModifiedBy/Date, Deleted/DeletedBy/DeletedDate
}
```

**`TenChungThu` không dùng để tra cứu.** Khoá tra cứu là `Thumbprint`; tên chủ thể chỉ để người dùng nhận ra
template trong danh sách, vì token có thể chưa cắm nên không phải lúc nào cũng đọc được cert thật.

**Lưu cả `Url` lẫn `ObjectKey`.** Chỉ lưu URL thì muốn xoá ảnh phải parse ngược chuỗi — hỏng ngay khi đổi
endpoint MinIO. Xem [../../docs/luu-tru-minio.md](../../docs/luu-tru-minio.md).

## TemplatePosition

Toạ độ lưu theo **tỉ lệ 0..1** so với khổ trang, **không** theo point/pixel. Gốc toạ độ **trên-trái, Y hướng
xuống** (hệ màn hình).

Lý do: một template được chọn một lần rồi áp cho nhiều file khác khổ giấy (A4 dọc/ngang, A3, bản quét lệch);
toạ độ tuyệt đối sẽ tràn ở trang nhỏ và teo ở trang lớn.

`Kind` là `TemplatePositionKind` — **đánh số tường minh, cấm chèn/đảo** (xem
[03-dtos-mapping.md](03-dtos-mapping.md)).

## LoKy / LoKyFile

```csharp
public class LoKy : ISoftDeleted
{
    public int Id { get; set; }
    public string IdUser { get; set; }        // chủ lô, lấy từ token
    public int TemplateId { get; set; }
    public string? Thumbprint { get; set; }   // chứng thư dùng cho lô này
    public string TaiToken { get; set; }      // bí mật chặn đường tải zip, phát ngay lúc tạo lô
    public TrangThaiLoKy TrangThai { get; set; }   // MoiTao | DangKy | Xong | Huy | Loi
    public int TongSo { get; set; }
    public int DaXong { get; set; }
    public int SoLoi { get; set; }
    public string? LoiChung { get; set; }     // sự cố làm dừng CẢ lô, khác lỗi từng file
    public DateTime? ThoiDiemBatDau { get; set; }
    public DateTime? ThoiDiemXong { get; set; }
    public string? TienToKho { get; set; }    // nơi bản ký nằm trên kho, để FE chỉ đường
    public List<LoKyFile> Files { get; set; }
}
```

`LoKyFile` giữ `ThuTu`, `TenFile`, `ObjectKeyNguon`, `ObjectKeyDaKy`, `TrangThai` (`Cho`/`DangKy`/`Xong`/
`Loi`), `LyDoLoi`, `ThoiGianKy`, `DauThoiGian`.

**Ba bộ đếm `TongSo`/`DaXong`/`SoLoi` nằm trên `LoKy`** thay vì đếm lại bảng file mỗi lần hỏi: 8 luồng cùng
đếm sẽ ghi đè kết quả của nhau, và đếm lại là hai lần quét bảng cho mỗi file. Cộng dồn bằng một câu
`ExecuteUpdateAsync`.

`ObjectKeyNguon` có thể trỏ vào **thư mục dùng chung trên kho** (lô lấy từ `them-tu-kho`) chứ không nhất thiết
nằm trong `lo-ky/{id}/nguon/` — đó là lý do dọn file nguồn phải chịu được trường hợp không có gì để dọn.

## Soft delete & audit

`ISoftDeleted` (`Deleted`, `DeletedDate`, `DeletedBy`) — xoá là gắn cờ, **mọi truy vấn phải lọc `!x.Deleted`**.
`CreatedBy/Date` và `ModifiedBy/Date` service tự gán bằng `getCurrentName()` + `GetVietnamTime()`.

> Quirk giữ nguyên: namespace của interface audit là `Sip.be.Shared.Interfaces` (file
> `ksts.be.shared/Interfaces/IFullAudited.cs` bê từ SIP sang, chưa đổi tên). Đang được dùng thật — đừng đổi
> nếu không sửa hết chỗ dùng.

## Thêm entity mới

1. Tạo class trong `ksts.be.domain/{Feature}/`.
2. Thêm `DbSet` + cấu hình quan hệ trong `KstsDbContext.OnModelCreating`.
3. Sinh migration (xem [05-infrastructure.md](05-infrastructure.md)).
