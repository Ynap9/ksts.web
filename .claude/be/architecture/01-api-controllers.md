# API & Controllers

## Vị trí

`ksts.be.api/Controllers/{Feature}/{Feature}Controller.cs`

| Controller | Route gốc |
|---|---|
| `AuthorizationController` | `` (OpenIddict: `~/connect/token`, `~/connect/authorize`) |
| `TemplateController` | `api/core/template-chu-ky` |
| `CertificateController` | `api/core/chung-thu-so` |

Route **kebab-case tiếng Việt**, khớp vốn từ nghiệp vụ.

## Khuôn một controller

```csharp
[Route("api/core/template-chu-ky")]
[ApiController]
public class TemplateController : BaseController
{
    private readonly ITemplateService _templateService;

    public TemplateController(ITemplateService templateService, ILogger<TemplateController> logger)
        : base(logger)
    {
        _templateService = templateService;
    }

    /// <summary>Tạo template mới.</summary>
    [HttpPost]
    public async Task<ApiResponse> Create([FromForm] AddTemplateDto dto)
    {
        try
        {
            var result = await _templateService.CreateAsync(dto);
            return new(result);
        }
        catch (Exception ex)
        {
            return OkException(ex);
        }
    }
}
```

Bắt buộc:

- Kế thừa `BaseController`, truyền `ILogger` xuống `base(logger)`.
- Mỗi action bọc `try/catch`, lỗi trả `OkException(ex)` — **không tự dựng response lỗi**.
- Trả `ApiResponse`; không dùng `IActionResult` trừ khi phải trả bytes thô.
- Action **không có nghiệp vụ**: không truy vấn DB, không validate nghiệp vụ, không map DTO.
- Ghi log ở service, không ở controller (`OkException` đã log nhánh lỗi).

## Nhận file upload

Action nhận ảnh dùng `[FromForm]` với DTO chứa `IFormFile?`:

```csharp
[HttpPost]
public async Task<ApiResponse> Create([FromForm] AddTemplateDto dto)
```

FE gửi `multipart/form-data`. Trường ảnh **nullable** — template không bắt buộc có dấu đỏ hay chữ ký tươi.

## Route cố định đặt trước route tham số

```csharp
[HttpGet("file-mau")]     // đặt trước
[HttpGet("{id:int}")]     // đặt sau
```

Segment cố định vẫn thắng route tham số nên không có xung đột, nhưng viết theo thứ tự này dễ đọc hơn.

## Phân trang

Action danh sách nhận `[FromQuery]` một DTO kế thừa `BaseRequestPagingDto`, trả
`BaseResponsePagingDto<TView>`. `pageSize = -1` là lấy hết (quy ước của `PagingExtension`); `pageNumber` đếm
từ 1.
