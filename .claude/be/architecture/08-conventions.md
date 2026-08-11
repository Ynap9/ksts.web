# Quy ước code (BE)

## Hai luật cứng của dự án này

### 1. Không viết hàm `private`

Cần tách việc thì tách thành **service có interface**, đặt đúng tầng (nghiệp vụ → `applications`, kỹ thuật →
`external`). Không đẻ helper riêng tư trong class.

```csharp
// ❌ private string? PersistImage(string? path, int id, string ten) { … }
// ✅ IS3FileStorage.UploadAsync(...) — có interface, test được, đổi được
```

Hệ quả tích cực: mọi mảnh logic đều có tên, có hợp đồng, và thay được. Hệ quả phải chịu: method public dài
hơn — chấp nhận, đừng lách bằng cách nhét vào `static` class.

### 2. Comment chỉ ở đầu hàm, và chỉ ở nơi có nghiệp vụ

XML `<summary>` ở đầu class và đầu **mỗi method public**. **Không comment bên trong thân hàm.** Ngắn gọn,
chuyên nghiệp, tiếng Việt, giải thích **vì sao** chứ không thuật lại code.

```csharp
/// <summary>Xoá mềm template và dọn luôn ảnh trên MinIO.</summary>
public async Task DeleteAsync(int id)
```

**DTO, entity và class settings KHÔNG comment.** Chúng chỉ là túi dữ liệu; tên trường đã tự nói. Kiến thức về
ý nghĩa từng trường nằm ở [04-domain.md](04-domain.md) và [03-dtos-mapping.md](03-dtos-mapping.md), không rải
vào code.

**Constant nghiệp vụ ký số thì PHẢI comment.** `SigningConstants`, `SignatureConstants`,
`SealPlacementConstants`, `TemplateConstants` — mỗi hằng số ghi rõ con số ở đâu ra và vì sao không được đổi.
Đây là chỗ duy nhất kiến thức đó tồn tại; một con số trần không ai dám sửa mà cũng không ai dám giữ.

## Đặt tên

- Project/namespace: `ksts.be.<layer>` — **viết thường**, giữ nguyên (`ksts.be.applications` số nhiều,
  `ksts.be.domain` số ít).
- Interface `I<Feature>Service` · implement `<Feature>Service`.
- Method async: hậu tố `Async`, trả `Task`/`Task<T>`.
- Route: **kebab-case tiếng Việt** (`template-chu-ky`, `chung-thu-so`, `file-mau`).
- Tên trường entity/DTO: **tiếng Việt không dấu** khớp nghiệp vụ (`TenTemplate`, `LyDoKy`, `AnhDauDoUrl`).

## Ngôn ngữ

Comment và XML doc viết **tiếng Việt** — nghiệp vụ là tiếng Việt. Tài liệu trong `.claude/` cũng tiếng Việt.

## Thời gian

**Cấm `DateTime.Now` / `DateTime.Today`.** Service dùng `BaseService.GetVietnamTime()`; tầng khác nhận thời
gian từ ngoài truyền vào. Ngoại lệ: dựng chain chứng thư dùng `DateTime.UtcNow` (mốc tuyệt đối, không phụ
thuộc múi giờ máy).

## Xử lý lỗi

1. Service `throw new UserFriendlyException(ErrorCodes.X, "câu tiếng Việt")`.
2. Controller bọc `try/catch`, trả `OkException(ex)`.
3. Mọi response là `ApiResponse`, HTTP luôn 200.
4. Log: một dòng đầu mỗi service method; nhánh lỗi đã được `OkException` log.

Việc phụ hỏng **không được** kéo theo hỏng cả thao tác chính: xoá ảnh MinIO thất bại sau khi đã xoá mềm
template thì `LogWarning` rồi đi tiếp — bản ghi đã xoá, ảnh chỉ còn là rác.

## Layering

- Controller **không** có nghiệp vụ.
- Service dùng `KstsDbContext` trực tiếp, không repository.
- `domain` chỉ tham chiếu `shared`.
- `external` chỉ tham chiếu `shared`, **tự chứa interface + DTO của mình**.
- `shared` không có nghiệp vụ.

## Không được làm

- Không đổi số của `TemplatePositionKind`.
- Không hardcode chuỗi đã có trong `*Constants`.
- Không tự dựng response lỗi — dùng `OkException`.
- Không trả entity ra controller — luôn qua DTO.
- Không quên `!x.Deleted` trong truy vấn.
- Không thêm repository/unit-of-work khi chưa được yêu cầu.

## Chốt công việc

Kết thúc bằng `dotnet build` sạch (`ksts.be/ksts.be.api/ksts.be.api.sln`).
