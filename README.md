# KSTS — Hệ thống ký số giấy báo trúng tuyển

Dựng và ký số hàng loạt giấy báo trúng tuyển của Trường Đại học Xây dựng Hà Nội.

Mỗi mùa tuyển sinh phát ra vài nghìn giấy báo. Trước đây mỗi tờ phải in ra, đóng dấu, ký tay rồi gửi đi. KSTS
đưa toàn bộ việc đó lên web: nhập danh sách thí sinh từ Excel, sinh ra từng file PDF theo mẫu chính thức, rồi
ký số cả lô bằng chứng thư số của lãnh đạo trường. Chữ ký có giá trị pháp lý, kèm dấu thời gian, mở bằng
Adobe Reader hay Foxit đều kiểm tra được.

## Mục lục

- [Ba thành phần](#ba-thành-phần)
- [Vì sao phải cài phần mềm trên máy người dùng](#vì-sao-phải-cài-phần-mềm-trên-máy-người-dùng)
- [Luồng nghiệp vụ](#luồng-nghiệp-vụ)
- [Kiến trúc triển khai](#kiến-trúc-triển-khai)
- [Công nghệ](#công-nghệ)
- [Cấu trúc mã nguồn](#cấu-trúc-mã-nguồn)
- [Chạy dự án](#chạy-dự-án)
- [Quy ước làm việc](#quy-ước-làm-việc)
- [Trạng thái tính năng](#trạng-thái-tính-năng)
- [Hiệu năng thực đo](#hiệu-năng-thực-đo)

## Ba thành phần

| Thành phần | Chạy ở đâu | Vai trò |
|---|---|---|
| [ksts.be](ksts.be/README.md) | Máy chủ | API nghiệp vụ: dựng PDF, lắp ráp chữ ký, quản lý lô ký, kho file |
| [ksts.fe](ksts.fe/README.md) | Trình duyệt | Giao diện quản trị: cấu hình mẫu chữ ký, import dữ liệu, theo dõi tiến độ |
| [ksts.plugin](ksts.plugin/README.md) | Máy người dùng | Cầu nối tới USB token, thứ duy nhất chạm được vào khoá bí mật |

## Vì sao phải cài phần mềm trên máy người dùng

Đây là quyết định định hình toàn bộ kiến trúc, cần hiểu trước mọi thứ khác.

Khoá bí mật của chứng thư số nằm trong chip của USB token và **không trích xuất ra được**. Ngay cả máy đang
cắm token cũng chỉ ra lệnh cho chip ký chứ không đọc được khoá. Máy chủ vì thế không bao giờ có khoá để tự ký.

Câu hỏi "làm sao máy chủ đọc được chứng thư ở máy người dùng" không có lời giải, và cũng không cần lời giải.
Câu hỏi đúng là:

> Ký ở máy người dùng, lắp ráp ở máy chủ. Máy chủ dựng PDF và tính giá trị băm; máy người dùng dùng token ký
> giá trị băm đó; máy chủ nhận chữ ký về, đóng dấu thời gian rồi ghi vào file.

Cái đi qua mạng chỉ là **giá trị băm và phần công khai của chứng thư**. Mã PIN và khoá bí mật không bao giờ
rời khỏi máy người dùng.

JavaScript trong trình duyệt không gọi được token vì thao tác đó cần API native của hệ điều hành. Đây là lý
do mọi giải pháp ký số USB token tại Việt Nam (VGCA, VNPT, Viettel, FPT, MISA, eTax) đều bắt cài một ứng dụng
nhỏ trên máy.

## Luồng nghiệp vụ

**Bước 1 — Cấu hình mẫu chữ ký.** Làm một lần cho mỗi mùa tuyển sinh. Người dùng tải lên ảnh dấu đỏ và ảnh
chữ ký tươi, kéo thả vị trí đặt chữ ký trên file PDF mẫu, chỉnh độ đậm và độ dày nét cho ảnh quét. Ảnh lưu
trên MinIO; toạ độ lưu theo tỉ lệ 0..1 nên một lần cấu hình áp được cho mọi khổ giấy.

**Bước 2 — Import dữ liệu tuyển sinh.** Tải lên file Excel danh sách thí sinh, chọn sheet và dòng tiêu đề.
Hệ thống nhồi dữ liệu từng thí sinh vào mẫu HTML rồi gọi Gotenberg chuyển sang PDF, chạy nền nhiều file song
song. Kết quả đóng thành zip để tải về, hoặc đẩy thẳng lên MinIO.

**Bước 3 — Ký số hàng loạt.** Chọn nguồn file (tải từ máy, hoặc trỏ vào thư mục có sẵn trên MinIO để bỏ hẳn
khâu tải lên), chọn mẫu chữ ký và chứng thư số, bấm Bắt đầu. Với mỗi file:

```
Máy chủ    dựng bản ký (nối bản, chừa chỗ trống /Contents)
           tính giá trị băm hai dải /ByteRange
           dựng SignedAttributes
Máy user   ký giá trị băm bằng khoá trong token
Máy chủ    ghép CMS, xin dấu thời gian từ TSA, ghi vào /Contents
```

Lô chạy nền ở máy chủ, đóng tab trình duyệt thì lô vẫn ký tiếp; mở lại màn hình thấy đúng tiến độ.

**Bước 4 — Nhận kết quả.** Tải zip về máy, hoặc đẩy bản đã ký lên MinIO cho hệ thống khác lấy.

## Kiến trúc triển khai

```
                     Trình duyệt (ksts.fe)
                             |
                           HTTPS
                             |
                     Máy chủ (ksts.be) --------- SQL Server
                        |     |     |
               MinIO ---+     |     +--- Gotenberg (HTML sang PDF)
                              |
                       TSA (tsa.ca.gov.vn)


     Máy người dùng:  ksts.plugin ---- USB token
```

Máy chủ không giữ file trên đĩa cục bộ. API có thể chạy nhiều bản hoặc nằm trong container không có ổ bền
vững, nên MinIO là chỗ duy nhất mọi bản nhìn thấy chung.

Bố cục thư mục trên MinIO:

```
AnhDauVaChuKyTuoi/{templateId}/                        ảnh dấu đỏ và chữ ký tươi
GiayBaoTrungTuyen/{khoá}/GiayBaoTrungTuyen/            giấy báo chưa ký
GiayBaoTrungTuyen/{khoá}/GiayBaoTrungTuyenDaKySo/      giấy báo đã ký
lo-ky/{loKyId}/                                        chỗ làm việc tạm, dọn sau khi xong
```

## Công nghệ

| Lớp | Công nghệ |
|---|---|
| Backend | .NET 9, ASP.NET Core Web API, EF Core, SQL Server, OpenIddict, AutoMapper, NLog |
| Frontend | Angular 20, PrimeNG, Tailwind CSS, pdf.js |
| Plugin | .NET 9 Windows, ASP.NET Core Minimal Hosting |
| Hạ tầng | MinIO (S3), Gotenberg, TSA của Ban Cơ yếu Chính phủ |
| Thư viện ký số | PdfSharp (vẽ mặt chữ ký), PdfPig (đọc toạ độ text), `System.Security.Cryptography` |

## Cấu trúc mã nguồn

```
ksts/
├── ksts.be/          Backend .NET, 7 project
├── ksts.fe/          Frontend Angular
├── ksts.plugin/      Plugin máy người dùng và bộ cài
├── deploy/           docker-compose.yml
└── .claude/          Tài liệu thiết kế: quyết định kiến trúc, kế hoạch, hợp đồng API, số đo
```

## Chạy dự án

### Yêu cầu môi trường

- .NET 9 SDK
- Node.js 20.x, khuyến nghị quản lý bằng [nvm-windows](https://github.com/coreybutler/nvm-windows)
- SQL Server và SSMS
- Quyền truy cập một MinIO và một dịch vụ Gotenberg

```bash
nvm install 20.9.0
nvm use 20.9.0
npm i -g @angular/cli
```

### Backend

```bash
cd ksts.be/ksts.be.api
dotnet ef database update
dotnet run
```

### Frontend

```bash
cd ksts.fe
npm install
npm start
```

### Plugin

Chỉ chạy trên Windows, cần USB token và middleware của hãng token.

```powershell
cd ksts.plugin
./dong-goi.ps1
```

### Docker

```bash
cd ksts.be
docker build -f ksts.be.api/Dockerfile -t ksts-be:latest .
```

Build context là thư mục `ksts.be` vì project API tham chiếu các project anh em nằm ngoài thư mục của nó.

## Quy ước làm việc

### Commit

- Tách riêng commit sửa lỗi và commit tính năng mới.
- Cần lấy code mới thì dùng `git stash` để cất phần đang làm dở, `git stash apply` để lấy lại, tránh tạo
  commit thừa mỗi lần pull.
- Không merge thẳng vào nhánh chung, phải tạo merge request để review.
- Nội dung commit theo mẫu:
  - Sửa lỗi: `[Bug] nguồn lỗi - nội dung đã sửa`
  - Tính năng: `[Feature] tên chức năng - mô tả`

### Comment

- Comment và `<summary>` viết **tiếng Việt**, vì nghiệp vụ là tiếng Việt.
- Comment đặt ở **đầu hàm**, nói *vì sao* chứ không nhắc lại *cái gì* đã hiển nhiên trong code.
- Chỗ nào có quyết định kỹ thuật không hiển nhiên thì ghi rõ lý do và hệ quả nếu làm khác.

### Mã lỗi

Mã lỗi chia dải theo nghiệp vụ, khai trong `ksts.be.shared/Requests/ErrorRequest/ErrorCodes.cs`, thêm mới thì
nối tiếp vào dải sẵn có, không nhảy cách:

| Dải | Nghiệp vụ |
|---|---|
| 1001 – 1019 | Template cấu hình chữ ký |
| 1040 – 1049 | Kho lưu trữ (MinIO) |
| 1080 – 1099 | Plugin ký số |
| 1120 – 1139 | Import và dựng giấy báo |
| 1140 – 1159 | Ký số PDF |
| 1160 – 1179 | Lô ký hàng loạt |

## Trạng thái tính năng

| Tính năng | Trạng thái |
|---|---|
| Cấu hình mẫu chữ ký, ảnh dấu đỏ và chữ ký tươi | Hoàn thành |
| Import Excel, dựng PDF hàng loạt, đẩy lên MinIO | Hoàn thành |
| Lắp ráp chữ ký PAdES, dấu thời gian TSA, ghi file | Hoàn thành, đã ký thử file thật và kiểm tra hợp lệ |
| Lô ký chạy nền, theo dõi tiến độ, tải zip | Hoàn thành |
| Quản lý người dùng, vai trò, phân quyền menu | Hoàn thành |
| Nguồn thực hiện phép ký | Tạm thời đọc chứng thư của máy chạy API |


Dòng áp chót cần lưu ý khi triển khai: hiện phép ký dùng chứng thư trong kho của **máy chạy API**. Cách này
chạy được khi API và trình duyệt cùng một máy, nhưng trên máy chủ thật thì đó là chứng thư của máy chủ chứ
không phải của người ký. Interface `ISigningKey` đã được tách sẵn để đổi nguồn ký sang plugin mà không phải
sửa tầng nghiệp vụ.

## Hiệu năng thực đo

Đo trên dữ liệu thật, giấy báo 911 KB mỗi tờ:

| Công việc | Kết quả |
|---|---|
| Dựng 5000 giấy báo qua Gotenberg, 8 luồng | 1685 giây, file zip 3860 MB |
| Ký 5000 file, 8 luồng | 163 ms mỗi file, tổng khoảng 13 phút 35 |

Nút thắt của khâu ký là **băng thông tới MinIO**, không phải phép mật mã (dưới 1 ms) cũng không phải TSA
(11–23 ms). Nâng số luồng lên 16 không nhanh hơn vì đường truyền đã bão hoà.

Khi thay nguồn ký bằng token thật, nút thắt sẽ chuyển sang token: token chỉ có một phiên và ký tuần tự từng
lượt, nên 5000 lượt ký là khoảng 17 phút không cách nào rút ngắn bằng cách tăng số luồng.
