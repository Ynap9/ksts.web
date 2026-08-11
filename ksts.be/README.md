# KSTS Backend

API nghiệp vụ của hệ thống ký số giấy báo trúng tuyển. Đọc [README tổng](../README.md) để nắm bối cảnh trước.

.NET 9, ASP.NET Core Web API, EF Core với SQL Server, OpenIddict, AutoMapper, NLog.

## Mục lục

- [Trách nhiệm](#trách-nhiệm)
- [Kiến trúc](#kiến-trúc)
- [Luồng dựng giấy báo](#luồng-dựng-giấy-báo)
- [Luồng ký số](#luồng-ký-số)
- [Kho file trên MinIO](#kho-file-trên-minio)
- [Cấu hình](#cấu-hình)
- [Chạy dự án](#chạy-dự-án)
- [Migration](#migration)
- [Docker](#docker)
- [Coding convention](#coding-convention)
- [Phân quyền](#phân-quyền)

## Trách nhiệm

- Dựng giấy báo từ Excel: đọc danh sách thí sinh, nhồi vào mẫu HTML, gọi Gotenberg chuyển sang PDF.
- Lắp ráp chữ ký số PAdES: dựng bản ký, tính giá trị băm, ghép CMS, xin dấu thời gian, ghi vào file.
- Quản lý lô ký: nhận file theo đợt, chạy ký nền nhiều luồng, theo dõi tiến độ, đóng gói kết quả.
- Quản trị mẫu chữ ký, tài khoản, vai trò, phân quyền.
- Đọc ghi kho file MinIO.

## Kiến trúc

Bảy project, chia theo tầng:

```
ksts.be.api                      Controller, Program.cs, chứng thư CA gốc, Dockerfile
ksts.be.applications             Nghiệp vụ: {TinhNang}/{Interfaces,Implements,Dtos}
ksts.be.domain                   Entity thuần
ksts.be.infrastructure           KstsDbContext, Migrations, Seeder
ksts.be.infrastructure.external  Hạ tầng phụ trợ
ksts.be.shared                   ApiResponse, ErrorCodes, Constants, Settings, mẫu HTML giấy báo
ksts.be.external                 Dịch vụ bên thứ ba và dịch vụ kỹ thuật dùng chung
```

Chiều phụ thuộc, không bao giờ đi ngược:

```
api             -> applications, infrastructure, external, shared
applications    -> infrastructure, external
infrastructure  -> domain, shared
domain          -> shared
external        -> shared
```

`ksts.be.external` **tự chứa**: nó sở hữu cả interface lẫn DTO của mình và không tham chiếu `applications`.
Nhờ vậy đổi một nhà cung cấp bên thứ ba chỉ là viết implement mới rồi đăng ký DI khác đi, không lan sang
tầng nghiệp vụ.

Nội dung `ksts.be.external`:

| Thư mục | Nội dung |
|---|---|
| `Storage` | MinIO: tải lên, tải về, chép, liệt kê, xoá theo tiền tố |
| `Pdf` | Dựng bản ký, vẽ mặt chữ ký, ghi CMS, đọc cấu trúc PDF, dò toạ độ text |
| `Signing` | Lắp ráp CMS, nguồn thực hiện phép ký |
| `Tsa` | Xin dấu thời gian theo RFC 3161 |
| `Certificates` | Đọc chứng thư, thẩm định chuỗi tin cậy |
| `Excel`, `Html`, `Gotenberg`, `Qr`, `Images` | Đọc Excel, nhồi HTML, chuyển PDF, sinh mã QR, đo ảnh |
| `Jobs` | Trạng thái lô dựng giấy báo, giữ trong bộ nhớ tiến trình |

Nguyên tắc không đổi:

- Controller **không chứa nghiệp vụ**, chỉ gọi service rồi bọc `ApiResponse`.
- Service dùng `KstsDbContext` trực tiếp, **không có repository layer**.
- Mọi REST trả **HTTP 200**, trạng thái thật nằm ở `ApiResponse.Status` (`1` thành công, `0` lỗi). Riêng
  endpoint trả file thô thì không bọc envelope, vì gói file vài GB vào JSON là hết bộ nhớ.

## Luồng dựng giấy báo

```
Excel  ->  IExcelSheetReader     đọc sheet, chuẩn hoá tên cột
       ->  IQrCodeSvgRenderer    sinh mã QR từ số CCCD
       ->  IHtmlDocumentFiller   nhồi giá trị vào mẫu HTML theo id thẻ
       ->  IGotenbergConverter   chuyển HTML sang PDF
       ->  ZipArchive            ghi thẳng vào file zip tạm trên đĩa
```

Bản đồ cột Excel sang id thẻ HTML khai duy nhất tại `GiayBaoConstants.ColumnToElementId`. Đối chiếu theo
**tên cột** chứ không theo thứ tự, nên file đảo cột hay thừa cột vẫn nhồi đúng.

Tên file đặt theo **số CCCD** vì đó là khoá định danh thí sinh, không dấu, không khoảng trắng, dùng làm
object key trên MinIO được ngay.

Lô chạy nền và không gom PDF vào RAM: 5000 giấy báo là gần 4 GB, ghi thẳng vào zip tạm trên đĩa.

## Luồng ký số

Phần khó nhất của backend. Nắm được nó là nắm được hệ thống.

```
1. Tải PDF nguồn từ MinIO
2. IPdfPreparer.Prepare
      Nối bản mới vào cuối file, giữ nguyên toàn bộ byte gốc
      Chèn signature dictionary, widget, và chỗ trống /Contents 32 KB
      Trả về bytes đã dựng kèm hai dải /ByteRange
3. Băm SHA-256 hai dải /ByteRange
4. ICmsAssembler.BuildSignedAttributes
      Pkcs9SigningTime và signingCertificateV2 tự dựng bằng AsnWriter
5. ISigningKey.Ky              <- chỗ duy nhất cần khoá bí mật
6. ITimestampClient            Xin dấu thời gian, thử lại tối đa 3 lần
7. ICmsAssembler.Assemble      CMS detached, chuỗi chứng thư, token TSA
8. IPdfContentWriter.Write     Ghi CMS vào đúng chỗ trống đã chừa
9. Đẩy bản đã ký lên MinIO
```

Bốn điều tuyệt đối không được làm sai:

- **Không dùng `PdfSharp.Save()`** để ghi file đã ký. Nó ghi lại cả tài liệu và giết chữ ký đã có sẵn trong
  file. Phải tự nối bản.
- **Fail-closed với TSA**: hỏng sau 3 lần thử thì file đó báo lỗi. Không bao giờ phát hành chữ ký thiếu dấu
  thời gian.
- **Không dựng lại `SignedAttributes` lần thứ hai.** Bộ byte đem đi ký là DER SET OF (thẻ `0x31`); khi nhét
  vào SignerInfo chỉ đổi đúng byte thẻ đầu thành `0xA0`, giữ nguyên phần còn lại. Dựng lại là lệch một byte,
  chữ ký hợp lệ bị coi là sai.
- **Thiếu `signingCertificateV2`** thì không conform CAdES, Adobe và Foxit có thể từ chối file.

Mặt chữ ký do hai cờ của template quyết định, độc lập nhau:

| Cờ | Tác dụng |
|---|---|
| `HienThiChuKySo` | Vẽ khối chữ ký số hai dòng tại vị trí đã cấu hình |
| `NhoiChuKySoVaoAnh` | Widget chữ ký trùm lên ảnh chữ ký tươi và con dấu, bấm vào ảnh ra bảng thông tin ký |

Bật cả hai vẫn chỉ **một** chữ ký, chỉ là nhiều widget. Tắt cả hai thì chữ ký vô hình nhưng vẫn hợp lệ.

Về hiệu năng: lô chạy 8 file song song, nhưng riêng **phép ký vẫn tuần tự** qua khoá `_khoaKy` trong
`KySoRunner`. Token phần cứng chỉ có một phiên và ký lần lượt; giữ đúng hình dạng đó từ bây giờ để khi lắp
plugin vào không phải sửa lại.

### Nguồn ký hiện là giải pháp tạm

`ISigningKey` đang được cài đặt bởi `StoreSigningKey`, đọc chứng thư trong kho của **máy chạy API**. Chạy
được ở môi trường phát triển, nhưng trên máy chủ thật thì đó là chứng thư của máy chủ.

Đổi sang plugin **không phải sửa service lẫn controller**: viết một implement `ISigningKey` khác lấy chữ ký
từ plugin rồi đăng ký DI khác đi.

`ICertificateTrustValidator` tách riêng vì lý do bảo mật chứ không phải cho gọn. Khi chứng thư đến từ plugin,
máy chủ **bắt buộc tự dựng chuỗi tin cậy** về CA gốc đã ghim và **không được tin cờ tin cậy do máy người dùng
gửi lên** — máy đó không kiểm soát được.

## Kho file trên MinIO

```
AnhDauVaChuKyTuoi/{templateId}/dau-do.{ext}
AnhDauVaChuKyTuoi/{templateId}/chu-ky-tuoi.{ext}

GiayBaoTrungTuyen/{khoá}/GiayBaoTrungTuyen/{cccd}.pdf
GiayBaoTrungTuyen/{khoá}/GiayBaoTrungTuyenDaKySo/{cccd}.pdf

lo-ky/{loKyId}/nguon/{thứ tự}.pdf
lo-ky/{loKyId}/da-ky/{thứ tự}.pdf
```

`lo-ky/` chỉ là chỗ làm việc tạm: file nguồn được dọn ngay sau khi lô ký xong, bản đã ký được chuyển sang
thư mục dùng chung khi người dùng bấm đẩy lên kho. Kho vì thế không tích rác.

Object key **do máy chủ đặt**, không lấy tên file người dùng tải lên: hai người cùng đặt tên `A.pdf` sẽ ghi
đè lên nhau, mà tên người dùng còn có thể chứa ký tự phá đường dẫn.

Khoá tuyển sinh trong đường dẫn lấy từ hằng số `GiayBaoConstants.Khoa`. Mỗi mùa tuyển sinh phải sửa hằng số
này **đồng thời** với khoá ghi trong mẫu HTML `Templates/html/giay-bao-trung-tuyen.html`.

## Cấu hình

Khai trong `appsettings.json`:

| Mục | Nội dung |
|---|---|
| `ConnectionStrings:KY_SO_WEB` | Chuỗi kết nối SQL Server |
| `S3` | Địa chỉ MinIO, bucket, khoá truy cập |
| `ConvertFile` | Địa chỉ Gotenberg, số file dựng đồng thời, timeout |
| `AuthServer` | Khoá ký token, địa chỉ ứng dụng |
| `AllowedHosts` | Danh sách origin cho CORS, ngăn cách bằng dấu chấm phẩy |

Hai điểm hay sai khi cấu hình MinIO:

- `S3_URL` phải là cổng **API**, không phải cổng giao diện quản trị. Hai cổng khác nhau; trỏ nhầm thì mọi
  thao tác trả về HTTP 404 chứ không phải lỗi xác thực.
- Đổi bucket thì ảnh dấu đỏ và chữ ký tươi của các template cũ **không tự chuyển theo**. Phải vào màn Cấu
  hình template tải lại ảnh cho từng template, nếu không lô ký sẽ dừng ngay ở bước mở phiên.

Giá trị của môi trường phát triển nên để ở `appsettings.Development.json`, file này đã được gitignore.

## Chạy dự án

```bash
cd ksts.be/ksts.be.api
dotnet ef database update
dotnet run
```

Hoặc mở `ksts.be.api/ksts.be.api.sln` bằng Visual Studio rồi chạy.

## Migration

Mở terminal tại thư mục `ksts.be.api`:

```bash
dotnet ef migrations add <TenMigration> --project ../ksts.be.infrastructure --startup-project .
dotnet ef database update --project ../ksts.be.infrastructure --startup-project .
```

Không sửa thẳng schema trên máy chủ. Mọi thay đổi cấu trúc đều phải đi qua migration để môi trường khác cập
nhật lại được.

Cột thêm mới vào bảng đã có dữ liệu phải khai `HasDefaultValue` hợp lý trong `KstsDbContext`. Ví dụ độ đậm
ảnh mặc định là 140 chứ không phải 0 của kiểu `int`, vì 0 nghĩa là ảnh biến mất khỏi giấy đã ký.

## Docker

```bash
cd ksts.be
docker build -f ksts.be.api/Dockerfile -t ksts-be:latest .
```

Build context là `ksts.be` vì project API tham chiếu các project anh em nằm ngoài thư mục của nó.

Ảnh Docker cài thêm `tzdata` cho múi giờ Việt Nam và bộ font để vẽ khối chữ ký.

Lưu ý khi triển khai Linux: `PdfSharp` 6 **không tự đọc font hệ thống trên Linux**, cần đăng ký font resolver
thì bước vẽ khối chữ ký mới chạy — phần này chưa làm. Cùng lý do, `CertificateProvider` và `StoreSigningKey`
dựa vào kho chứng thư Windows nên trên Linux trả về danh sách rỗng; điều này chấp nhận được vì cả hai vốn là
giải pháp tạm.

## Coding convention

### Tổng quan

- `PascalCase` cho class, method, property; `camelCase` cho biến cục bộ.
- Comment và `<summary>` viết **tiếng Việt**.
- **Không viết hàm `private`.** Cần tách việc thì tách thành service có interface. Việc kỹ thuật hoặc dùng
  chung thì đặt ở `ksts.be.external`.
- Comment đặt ở **đầu hàm**, nói vì sao chứ không nhắc lại code. Không comment rải trong thân hàm trừ khi
  giải thích một quyết định không hiển nhiên.
- Giờ giấc dùng `BaseService.GetVietnamTime()` hoặc `DateTimeConstants.VietnamNow`, không dùng `DateTime.Now`.

### DTO

- DTO giống hệt entity thì đặt tên `TenEntityDto`. DTO mở rộng thêm trường thì đặt tên nói rõ phần mở rộng.
- Không dùng chung một class DTO cho hai API trả dữ liệu khác nhau.
- DTO của nghiệp vụ đặt trong `ksts.be.applications/{TinhNang}/Dtos`; DTO của dịch vụ kỹ thuật đặt cùng chỗ
  với interface của nó trong `ksts.be.external`.
- DTO và entity **không comment** từng trường; tên trường phải tự nói lên nội dung.

### Xử lý lỗi

Lỗi nghiệp vụ ném `UserFriendlyException`, controller bắt bằng `OkException`:

```csharp
throw new UserFriendlyException(ErrorCodes.LoKyRong, "Lô chưa có file nào để ký.");
```

Mã lỗi khai trong `ErrorCodes`, thêm mới thì nối tiếp vào dải của nghiệp vụ tương ứng, không nhảy cách.

### Ghi log

Log tham số đầu vào ở đầu method:

```csharp
public async Task<ViewLoKyDto> BatDauAsync(int loKyId, BatDauKyDto input)
{
    _logger.LogInformation($"{nameof(BatDauAsync)} loKyId={loKyId}");
}
```

### Việc chạy nền

Việc sống lâu hơn một request **phải** nằm trong service đăng ký `Singleton` và tự mở scope riêng cho từng
đơn vị việc (`KySoRunner`, `DayLenKhoRunner`). Service theo phạm vi request sẽ bị huỷ kèm `DbContext` ngay
khi trả lời xong, dùng tiếp là lỗi đối tượng đã giải phóng.

Đếm kết quả bằng một câu lệnh cộng dồn `ExecuteUpdateAsync` thay vì đọc lên rồi ghi xuống: nhiều luồng cùng
đếm sẽ ghi đè kết quả của nhau.

## Phân quyền

Khai quyền trong `ksts.be.shared/Constants/Auth/PermissionKeys.cs`:

1. Thêm hằng số: `public const string MenuImportTuyenSinh = Menu + "ImportTuyenSinh";`
2. Thêm dòng tương ứng vào mảng `All` kèm tên hiển thị và nhóm, để nó hiện ra ở màn gán quyền.
3. Bên frontend thêm hằng số cùng tên vào `shared/constants/permission.constants.ts` rồi gắn vào route và
   menu.

Endpoint cần chặn ở tầng API thì gắn attribute:

```csharp
[Permission(PermissionKeys.UserView)]
```

Hiện chỉ nhóm controller xác thực dùng attribute này; các controller nghiệp vụ mới chặn ở frontend.

## Tài liệu thiết kế

`.claude/be/architecture/` mô tả từng tầng. `.claude/be/plans/` là kế hoạch từng tính năng.
`.claude/docs/` ghi các quyết định kiến trúc và số đo hiệu năng.
