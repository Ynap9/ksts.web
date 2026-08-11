# DTO & Mapping

## Đặt tên

| Tiền tố | Dùng cho |
|---|---|
| `Add*Dto` | Payload tạo mới |
| `Update*Dto` | Payload sửa — kế thừa `Add*Dto` và thêm `Id` |
| `View*Dto` | Dữ liệu trả về FE |
| `FindPaging*Dto` | Query phân trang, kế thừa `BaseRequestPagingDto` |
| `*ResultDto` | Kết quả một thao tác không phải CRUD |

Tên trường DTO dùng **tiếng Việt không dấu** khớp nghiệp vụ (`TenTemplate`, `LyDoKy`, `NoiKy`,
`AnhDauDoUrl`), giống entity — đổi tên giữa hai tầng chỉ tạo chỗ để lệch.

## Trường tuỳ chọn

Dấu đỏ và chữ ký tươi **không bắt buộc**. Mọi trường liên quan phải `?`:

```csharp
public IFormFile? AnhDauDo { get; set; }
public IFormFile? AnhChuKyTuoi { get; set; }
public string? AnhDauDoUrl { get; set; }
```

Vào: `null` = không đặt / bỏ ảnh. Ra: `null` = template không có ảnh đó.

## Enum ra JSON dạng SỐ

`TemplatePositionKind` serialize thành số (`0 ChuKy`, `1 DauDo`, `2 ChuKyTuoi`). **Cấm chèn phần tử vào giữa
hoặc đảo thứ tự** — số đã nằm trong DB, đổi là đổi nghĩa dữ liệu cũ, mà lệch ở đây nghĩa là ảnh dấu đỏ bị vẽ
vào ô chữ ký.

DTO dùng thẳng enum của `shared`, không khai bản sao ở tầng DTO.

## Cập nhật là GHI ĐÈ, không vá

`UpdateTemplateDto` mang **toàn bộ** trạng thái, kể cả `Positions`. Service xoá hết position cũ rồi ghi lại
theo payload. Màn cấu hình luôn gửi trạng thái đầy đủ đang hiển thị; vá từng khối sẽ để sót khối người dùng
vừa xoá trên màn hình.

## AutoMapper

Một `MappingProfile` duy nhất ở `ksts.be.applications/Base/MappingProfile.cs`:

```csharp
CreateMap<Template, ViewTemplateDto>();
CreateMap<TemplatePosition, TemplatePositionDto>();
```

Chỉ map **entity → View DTO**. Chiều ngược lại (`Add*Dto` → entity) viết tay trong service: nó có nghiệp vụ
(chuẩn hoá tên, ép `PageNumber >= 1`, upload ảnh lấy URL) mà mapper không nên biết.
