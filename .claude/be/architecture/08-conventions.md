# Quy ước code (BE)

## Hai luật cứng của dự án này

### 1. Không viết hàm `private`

Cần tách việc thì tách thành **service có interface**, đặt đúng tầng (nghiệp vụ → `applications`, kỹ thuật →
`external`). Không đẻ helper riêng tư trong class.

```csharp
// ❌ private string? PersistImage(string? path, int id, string ten) { … }
// ✅ IS3FileStorage.UploadAsync(...) — có interface, test được, đổi được
```

Đổi lại: mọi mảnh logic đều có tên và thay được, method public dài hơn — đừng lách bằng `static` class.

### 2. Comment ít, bằng TIẾNG ANH, chỉ ở nơi có lý do

XML `<summary>` **tiếng Anh, một câu** ở đầu class và đầu mỗi method public. **Không comment trong thân hàm.**

```csharp
/// <summary>Soft-deletes the template and removes its images from MinIO.</summary>
public async Task DeleteAsync(int id)
```

Comment trả lời **vì sao**, không thuật lại code. Cấm:

- Comment kể lại việc code đang làm (`// loop through files`, `// gán giá trị`).
- Comment lan man nhiều dòng; ghi lịch sử sửa đổi, tên người sửa, ngày sửa — git giữ những thứ đó.
- Comment code chết — xoá hẳn đoạn code đó đi.
- `// TODO` trống nghĩa. Việc còn dở ghi vào [../../dang-lam.md](../../dang-lam.md).

**DTO, entity và class settings KHÔNG comment** — túi dữ liệu, tên trường đã tự nói; ý nghĩa từng trường nằm ở
[04-domain.md](04-domain.md) và [03-dtos-mapping.md](03-dtos-mapping.md), không rải vào code.

**Constant nghiệp vụ ký số thì PHẢI comment**: `SigningConstants`, `SignatureConstants`,
`SealPlacementConstants`, `TemplateConstants` — con số ở đâu ra, vì sao không được đổi. Đây là chỗ duy nhất
kiến thức đó tồn tại; một con số trần không ai dám sửa mà cũng không ai dám giữ.

⚠️ Comment cũ trong repo đang là tiếng Việt. **Không dịch hàng loạt** — đụng vào file nào thì đổi comment của
phần mình sửa, để diff còn đọc được.

## Đặt tên — TIẾNG ANH trước

Method, biến, tham số, class kỹ thuật: **ưu tiên tiếng Anh**. Chỉ giữ tiếng Việt khi khái niệm nghiệp vụ
**không có từ tiếng Anh sát nghĩa** — dịch ép ra một từ gần đúng còn tệ hơn, vì mỗi người sẽ dịch một kiểu.

```csharp
// ✅ CreateBatchAsync, OpenSessionAsync, BuildDownloadUrl, IsExpired
// ✅ GiayBaoTrungTuyen, ChuKyTuoi, DauDo — không có từ tiếng Anh nào sát nghĩa
// ❌ TaoLoAsync, MoPhienAsync — batch / session có từ tiếng Anh rõ nghĩa
```

Project/namespace `ksts.be.<layer>` **viết thường**, giữ nguyên (`applications` số nhiều, `domain` số ít);
interface `I<Feature>Service` + implement `<Feature>Service`; method async có hậu tố `Async`.

**Bốn chỗ vẫn là tiếng Việt, không đổi được:** route kebab-case (`template-chu-ky`, `lo-ky/them-tu-kho`) đã công
bố ở [../../contracts/](../../contracts/); tên trường entity/DTO (`TenTemplate`, `LyDoKy`) là cột DB và khoá
JSON trên dây, đổi là migration + phá contract; tên class nghiệp vụ đã có (`LoKy`, `Template`); và câu hiển thị
cho người dùng (`ErrorMessages`). Tên **mới** theo luật tiếng Anh ở trên; tên **cũ** giữ nguyên, không đổi
hàng loạt.

Tóm lại: comment và XML doc **tiếng Anh**, câu cho người dùng **tiếng Việt** có dấu, tài liệu `.claude/`
**tiếng Việt**.

## Thời gian

**Cấm `DateTime.Now` / `DateTime.Today`.** Service dùng `BaseService.GetVietnamTime()`; lớp chạy nền ngoài scope
request (`KySoRunner`) dùng `DateTimeConstants.VietnamNow`; tầng còn lại nhận thời gian từ ngoài truyền vào.
Ngoại lệ: dựng chain chứng thư và `SigningTime` của CMS dùng `DateTime.UtcNow` (mốc tuyệt đối, không phụ thuộc
múi giờ máy).

## Xử lý lỗi

1. Service `throw new UserFriendlyException(ErrorCodes.X, "câu tiếng Việt")`.
2. Controller bọc `try/catch`, trả `OkException(ex)`.
3. Mọi response là `ApiResponse`, HTTP luôn 200.
4. Log: một dòng đầu mỗi service method; nhánh lỗi đã được `OkException` log.

Việc phụ hỏng **không được** kéo theo hỏng thao tác chính: xoá ảnh MinIO thất bại sau khi đã xoá mềm template
thì `LogWarning` rồi đi tiếp — bản ghi đã xoá, ảnh chỉ còn là rác.

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
