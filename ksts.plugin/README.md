# KSTS Plugin

Ứng dụng chạy trên **máy người dùng**, làm cầu nối giữa trang web ký số và USB token. Đọc
[README tổng](../README.md) để nắm bối cảnh trước.

.NET 9 cho Windows, đóng gói thành một file `.exe` chạy độc lập, máy người dùng không cần cài .NET runtime.

Tài liệu dành cho **người dùng cuối** nằm ở [`installer/README.md`](installer/README.md); file này dành cho
người phát triển.

## Mục lục

- [Vì sao cần plugin](#vì-sao-cần-plugin)
- [Kiến trúc](#kiến-trúc)
- [API](#api)
- [Quan hệ với middleware của token](#quan-hệ-với-middleware-của-token)
- [Đóng gói bộ cài](#đóng-gói-bộ-cài)
- [Trình cài đặt](#trình-cài-đặt)
- [Chạy khi phát triển](#chạy-khi-phát-triển)
- [Nguyên tắc bảo mật](#nguyên-tắc-bảo-mật)
- [Việc còn lại](#việc-còn-lại)

## Vì sao cần plugin

Khoá bí mật của chứng thư số nằm trong chip USB token và không trích xuất ra được. Ký một tài liệu nghĩa là
ra lệnh cho chip ký, thao tác này phải gọi API native của Windows (CNG/CSP) nên **JavaScript trong trình
duyệt không làm được**.

Đó là lý do mọi giải pháp ký số USB token tại Việt Nam đều yêu cầu cài một ứng dụng nhỏ trên máy. Plugin này
là ứng dụng đó.

## Kiến trúc

Bốn project, cùng cách chia tầng với backend:

```
ksts.plugin.api            Controller, Program.cs, cấu hình CORS
ksts.plugin.applications   Nghiệp vụ mỏng, gọi xuống external
ksts.plugin.external       Đọc chứng thư, kiểm tra token
ksts.plugin.shared         Hằng số, envelope ApiResponse
```

Plugin nghe tại `http://127.0.0.1:17739`, **chỉ trên loopback**, không lộ ra mạng LAN. Envelope trả về giống
hệt backend để frontend dùng chung một cách đọc:

```jsonc
{ "status": 1, "data": {}, "code": 200, "message": "Ok" }
```

CORS khai origin của trang web trong `ksts.plugin.api/appsettings.json`. Đây là **điều kiện để trình duyệt
đọc được kết quả, không phải lớp bảo mật**: header `Origin` do phía gọi tự đặt, `curl` hay mã độc đặt tuỳ ý.

## API

| Method | Route | Việc |
|---|---|---|
| GET | `api/plugin/trang-thai` | Phép dò: gọi được nghĩa là máy đã cài plugin và plugin đang chạy |
| GET | `api/plugin/chung-thu-so` | Liệt kê chứng thư trong kho của Windows |
| POST | `api/plugin/chung-thu-so/kiem-tra-token` | Ký thử một mẩu dữ liệu để xác nhận token dùng được |

Hai điểm quan trọng về hành vi:

- **Liệt kê chứng thư không bao giờ hỏi mã PIN.** Nó chỉ đọc metadata của khoá.
- **`kiem-tra-token` là chỗ duy nhất hộp PIN bật lên**, vì nó thực sự chạm vào khoá. Đây cũng là bằng chứng
  duy nhất rằng token đang cắm thật: mọi phép đọc metadata đều có thể "đạt hết" trong khi token đã rút từ lâu.

Kết quả liệt kê không có cờ `isTrusted`. Máy người dùng không kiểm soát được nên cờ tin cậy do nó gửi lên là
vô giá trị; thẩm định chuỗi chứng thư về CA gốc là việc của backend.

Chứng thư nằm trong kho phần mềm của máy luôn bị đánh dấu **không ký được**: ký giấy báo trúng tuyển phải
bằng khoá trên token, khoá phần mềm sao chép được nên không đủ tư cách.

## Quan hệ với middleware của token

Plugin đọc chứng thư qua **Windows certificate store** (`X509Store`), kể cả chứng thư nằm trên USB token.
Plugin **không nạp thư viện PKCS#11 nào** và không gọi trực tiếp vào phần mềm của hãng token.

Cầu nối là **middleware của hãng token** (bit4id với token của Ban Cơ yếu Chính phủ): nó đăng ký một provider
mật mã với Windows, nhờ đó chứng thư trên token hiện ra trong certificate store như chứng thư thường.

Hệ quả: **máy chưa cài middleware thì plugin không thấy token**. Bộ cài vì thế phải kèm middleware.

## Đóng gói bộ cài

```powershell
cd ksts.plugin
./dong-goi.ps1
```

Script làm bốn việc:

1. Publish plugin thành một file `.exe` chạy độc lập (self-contained, single file).
2. Gom `.exe`, `appsettings.json` và bộ script cài đặt trong `installer/`.
3. Nhặt file cài middleware trong `vendor/bit4id/` nếu có.
4. Nén tất cả thành `ksts.be/ksts.be.api/Plugins/ksts-plugin-setup.zip`.

Backend phát file zip này qua `api/core/plugin/bo-cai/noi-dung`. Sau khi đóng gói phải **build lại
`ksts.be.api`** để file được chép sang thư mục output.

### Đưa bộ cài lên máy chủ

File zip **không nằm trong git** (~43 MB, là sản phẩm build), nên máy chủ dựng image từ bản clone của repo sẽ
không có nó — thiếu bước này thì màn Ký số báo *"Máy chủ chưa có bộ cài plugin"*. Chép tay lên thư mục đã
mount sẵn vào container:

```bash
scp ksts.be/ksts.be.api/Plugins/ksts-plugin-setup.zip <user>@<may-chu>:<repo>/ksts.be/ksts.be.api/Plugins/
```

`deploy/docker-compose.yml` mount thẳng thư mục đó vào `/app/Plugins` chỉ đọc, nên bản mới có hiệu lực ngay,
không phải build lại image cũng không phải khởi động lại container. Chi tiết:
[`ksts.be/ksts.be.api/Plugins/README.md`](../ksts.be/ksts.be.api/Plugins/README.md).

### Middleware không nằm trong repo

`vendor/bit4id/` là chỗ cắm sẵn cho file cài middleware, nhưng file đó là **phần mềm của hãng token**, phải
lấy từ đơn vị cấp chứng thư số. Không có file thì vẫn đóng gói được, chỉ là bộ cài không tự cài middleware
và người dùng phải tự cài trước.

Cờ chạy ngầm mặc định là `/qn /norestart` cho `.msi` và `/S` cho `.exe`; loại bộ cài khác thì ghi cờ đúng vào
`vendor/bit4id/tham-so.txt`. Chi tiết xem tài liệu trong thư mục đó.

Không tự tải middleware từ Internet về. Đây là phần mềm đụng tới kho khoá mật mã của cả máy và được cài ngầm
với quyền quản trị; tải một binary không rõ nguồn rồi làm vậy đúng là kịch bản một cuộc tấn công chuỗi cung
ứng cần.

## Trình cài đặt

Nằm trong `installer/`, được gói cùng plugin:

| File | Việc |
|---|---|
| `CAI-DAT.cmd` | Người dùng bấm đúp vào đây |
| `cai-dat.ps1` | Kiểm middleware, cài ngầm nếu thiếu, cài plugin per-user, bật tự khởi động |
| `go-cai-dat.ps1` | Dừng plugin, xoá tự khởi động, xoá thư mục cài |
| `README.md` | Hướng dẫn cho người dùng cuối |

Cách nhận biết middleware đã có: hỏi thẳng danh sách provider mật mã đã đăng ký với Windows trong registry,
không dò tên trong Programs and Features. Thứ quyết định token có hiện trong certificate store là **provider
có được đăng ký hay không**; mục trong Programs and Features chỉ nói ai đó từng chạy bộ cài, có bản gỡ lỗi để
lại mục mà mất provider.

Plugin cài **per-user** vào `%LocalAppData%\KstsPlugin`, tự khởi động qua `HKCU\...\Run`, không cài driver,
không dựng service SYSTEM. Nhờ vậy phần cài plugin **không cần quyền quản trị**; chỉ bước cài middleware mới
xin nâng quyền, và chỉ khi thực sự phải cài.

Gỡ plugin **không** gỡ middleware: đó là phần mềm dùng chung cho mọi ứng dụng chữ ký số trên máy.

## Chạy khi phát triển

```bash
cd ksts.plugin/ksts.plugin.api
dotnet run
```

Kiểm tra nhanh:

```bash
curl http://127.0.0.1:17739/api/plugin/trang-thai
curl "http://127.0.0.1:17739/api/plugin/chung-thu-so?onlySignable=false"
```

Máy phát triển phải cài middleware của token thì mới liệt kê được chứng thư thật.

## Nguyên tắc bảo mật

**Không bao giờ chạm vào mã PIN.** Để middleware tự bật hộp nhập PIN qua CNG; PIN đi thẳng từ bàn phím vào
provider mật mã. Tiến trình plugin không thấy một byte PIN nào. Tuyệt đối không tự vẽ ô nhập PIN — tự viết là
giữ PIN trong bộ nhớ, có thể bị dump, bị keylog, hoặc lỡ tay ghi vào log.

**Không cache danh sách chứng thư xuống đĩa.** Liệt kê lại mỗi lần: token có thể vừa được cắm hoặc vừa rút.

**Phân biệt giữ handle khoá và cache PIN.** Giữ handle một phiên là thứ khiến cả lô chỉ hỏi PIN một lần; đó
không phải cache PIN. Middleware có thể tự cache PIN theo cấu hình riêng của nó, nên code sạch là chưa đủ,
phải kiểm cả cấu hình middleware.

**Ghi log không kèm PIN và không kèm nội dung file.** Chỉ ghi thời điểm, mã công việc, vân tay chứng thư và
số lượng file.

Rủi ro còn lại phải nói thẳng với khách hàng: mã độc có quyền người dùng trên máy đang cắm token và PIN đã
nhập thì gọi thẳng CryptoAPI được, không cần qua plugin. Plugin **không phải** lớp phòng thủ ở đây; phòng thủ
thật là rút token khi không dùng và bảo vệ máy trạm.

## Việc còn lại

**Chuyển sang topology B.** Hiện plugin mở cổng nghe ở loopback và frontend gọi vào. Thiết kế đã chốt là
plugin **tự gọi ra máy chủ qua WebSocket** để nhận việc, không mở cổng nào cả: như vậy không trang web nào
gọi được plugin, hết chuyện mixed content, hết Private Network Access, hết xung đột cổng. Kế hoạch chi tiết
nằm ở `.claude/plugin/plans/ky-so-plugin.plan.md`.

**Phiên ký giữ handle khoá** để cả lô chỉ hỏi PIN một lần, tự đóng sau 15 phút không dùng và đóng ngay khi
rút token.

**Xác minh job ticket** do máy chủ ký, với public key ghim cứng lúc build, và tự tính lại giá trị băm từ
bytes PDF thật trước khi ký.

**Ký mã nguồn (code signing)** trước khi phát hành rộng. Binary chưa ký thì SmartScreen chặn ở lần chạy đầu;
máy cơ quan thường bật chính sách không cho bỏ qua cảnh báo, và phần mềm diệt virus xem binary chưa ký chạy
nền, tự khởi động, đụng crypto token, kết nối ra ngoài là chân dung mã độc điển hình.

**Hộp PIN hiện chìm.** Plugin chạy nền không sở hữu cửa sổ nào nên hộp PIN có thể nằm sau trình duyệt. Hướng
xử lý là cho plugin chạy dạng tray app có cửa sổ ẩn rồi truyền handle cửa sổ đó vào thuộc tính CNG
`"HWND Handle"` trước khi ký.
