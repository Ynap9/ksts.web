# Contract — Plugin ký số ở máy người dùng

Plugin chạy trên máy người dùng, nghe **`http://127.0.0.1:17739`**. FE gọi thẳng, không qua BE. Không có
Bearer token — plugin không biết gì về tài khoản. Mã nguồn: `ksts.plugin/`.

> Topology **A** (plugin mở listener loopback) là cái **đang chạy**; topology B (plugin tự gọi ra qua WSS) để
> **tối ưu sau**, xem [docs/bao-mat-agent-ky-so.md](../docs/bao-mat-agent-ky-so.md) §2. Hệ quả phải nắm: trang
> `https://` gọi `http://127.0.0.1` **không** dính mixed content, nhưng Private Network Access của Chrome đã
> đổi cơ chế vài lần — phải test lại trên đúng bản trình duyệt đang dùng. `Origin` **không** phải hàng rào
> bảo mật.

## Routes

| Method | Route | Tham số | `data` trả về |
|---|---|---|---|
| GET | `api/plugin/trang-thai` | — | `TrangThai` |
| GET | `api/plugin/chung-thu-so` | `onlySignable?` (bool) | `CertScanResult` |
| POST | `api/plugin/chung-thu-so/kiem-tra-token` | body `{ thumbprint }` | `TokenVerify` |

Envelope giống hệt BE: `{ "status": 1, "data": …, "code": 200, "message": "Ok" }`, enum serialize thành **SỐ**.

## Ký hộ máy chủ — `api/plugin/ky-so`

| Method | Route | Body | `data` trả về |
|---|---|---|---|
| POST | `ky-so/mo-phien` | `{ thumbprint }` | `{ thumbprint, commonName, chungThuBase64 }` |
| POST | `ky-so/ky` | `{ yeuCau: [{ yeuCauId, duLieuBase64 }] }` | `[{ yeuCauId, chuKyBase64, loi }]` |
| POST | `ky-so/do-toc-do` | `{ thumbprint, soLan }` (mặc định 20, trần 100) | `DoTocDoKetQua` |
| POST | `ky-so/dong-phien` | — | `true` |

`mo-phien` là **chỗ duy nhất hộp PIN bật lên** trong luồng ký: nó mở khoá rồi GIỮ handle cho cả lô. Giữ handle
khác cache PIN — PIN vẫn đi thẳng từ bàn phím vào middleware, không byte nào vào tiến trình plugin. Phiên tự
đóng sau **15 phút không dùng** (`KySoConstants.PhutTuDongDongPhien`).

`chungThuBase64` là chứng thư phần **CÔNG KHAI** (DER), nộp lên máy chủ để dựng chuỗi tin cậy và lắp vào CMS.

`ky` nhận **cả một đợt** yêu cầu chứ không phải một: token ký tuần tự nên mỗi vòng đi-về cộng thẳng vào từng
file, gom đợt là cách duy nhất chia nhỏ khoản đó. Một yêu cầu hỏng trả `loi` riêng cho nó, các yêu cầu còn
lại vẫn ký.

`duLieuBase64` là **SignedAttributes** do máy chủ dựng — plugin không cần biết nội dung file vẫn ký được, và
cũng không nhận file nào. Đổi lại, plugin **không tự kiểm được mình đang ký gì** (WYSIWYS ở
[docs/bao-mat-agent-ky-so.md](../docs/bao-mat-agent-ky-so.md) §5 chưa thi công) — phải nói thẳng khi bàn giao.

## `do-toc-do` — đo sàn cứng của token

```jsonc
// { "thumbprint": "A1B2…", "soLan": 20 }  ->
{
  "soLan": 20, "trungBinhMs": 0, "nhanhNhatMs": 0, "chamNhatMs": 0,
  "kichThuocKhoaBit": 2048, "thuatToan": "RSA", "tenProvider": "bit4id xPKI CSP"
}
```

Mở phiên thật nên **hộp PIN sẽ bật**. Đây là chỗ duy nhất đo được `T` — thời gian một lượt ký qua token — con
số quyết định thời lượng của cả lô, vì token ký tuần tự (5000 × `T` không rút ngắn được bằng thêm luồng). Trần
`SoLanDoToiDa = 100` để không ai biến nó thành vòng lặp vô tận chạm vào token thật.

## Dò plugin đã cài hay chưa

`GET api/plugin/trang-thai` là **phép dò**: gọi được nghĩa là máy đã cài và plugin đang chạy. Timeout ngắn
(1–2 s) rồi coi như chưa cài — FE hiện popup tải bộ cài (plugin + middleware bit4id gói trong một exe).
Endpoint này **không** chạm tới chứng thư hay token nên không bao giờ bật hộp thoại lên màn hình người dùng.

```jsonc
// TrangThai
{ "ten": "KSTS Plugin ký số", "phienBan": "1.0.0", "sanSang": true }
```

## TokenVerify — kiểm token cắm thật hay chưa

> Ba route bật hộp PIN, và chỉ ba: `chung-thu-so/kiem-tra-token`, `ky-so/mo-phien`, `ky-so/do-toc-do`. Đều vì
> cùng một lý do — chúng **chạm vào khoá bí mật**. Mọi phép đọc metadata thì không.

```jsonc
{
  "thumbprint": "A1B2…", "commonName": "…",
  "foundInStore": true, "hasPrivateKey": true, "notExpired": true,
  "allowsSigning": true, "onUsbToken": true, "canSignTest": true,
  "valid": true, "reason": null
}
```

`kiem-tra-token` **ký thử một mẩu dữ liệu ngẫu nhiên** bằng khoá bí mật của chứng thư. Đó là bằng chứng duy
nhất rằng token đang cắm thật và PIN dùng được — mọi phép đọc metadata đều có thể "đạt hết" trong khi token
đã rút từ lâu. Ký thử buộc phải chạm vào khoá, nên **middleware bit4id tự bật hộp nhập PIN của Windows**.

- **Liệt kê chứng thư KHÔNG bao giờ hỏi PIN.** Muốn hiện hộp PIN thì phải gọi endpoint này.
- **PIN không đi qua tiến trình plugin** — đi thẳng từ bàn phím vào middleware qua CNG/minidriver. Plugin
  không tự vẽ ô nhập PIN, xem [docs/bao-mat-agent-ky-so.md](../docs/bao-mat-agent-ky-so.md) §4.
- Kết quả chỉ là **một cờ `valid`** kèm `reason` hiển thị được: không trả số lần thử PIN còn lại.
- Plugin **không giữ handle khoá** sau khi kiểm: mỗi lần kiểm là một lần hỏi PIN. Luồng ký cả lô đi đường
  khác — `ky-so/mo-phien` giữ handle cho cả phiên nên N file chỉ hỏi PIN một lần; giữ handle **không** phải
  cache PIN.
- ⚠️ Màn ký số hiện **bắt xác thực chứng thư trước khi mở nút Bắt đầu**, nên người dùng nhập PIN **hai lần**
  cho một lô: một lần ở `kiem-tra-token`, một lần ở `ky-so/mo-phien`. Đổi được bằng cách coi `mo-phien` chính
  là phép xác thực (nó cũng chạm khoá thật), nhưng khi đó lỗi cert sai sẽ hiện muộn hơn — sau khi đã tải file
  lên. Chưa quyết.
- FE **không đặt timeout** cho lời gọi này: người dùng cần thời gian nhập PIN.

> ⚠️ Còn phải kiểm trên máy thật: plugin chạy nền, **không sở hữu cửa sổ nào**, nên hộp PIN có thể hiện chìm
> sau trình duyệt. SIPPACK xử lý bằng cách set thuộc tính CNG `"HWND Handle"` trỏ vào cửa sổ app trước khi ký
> thử — plugin chưa có cửa sổ để trỏ. Nếu gặp, hướng sửa là cho plugin chạy dạng tray app có cửa sổ ẩn rồi
> truyền HWND đó vào.

## Bộ cài — phát từ BE, không phải từ plugin

Dò không thấy plugin thì FE mở popup và tải bộ cài qua **BE** (yêu cầu Bearer token như mọi API khác):

| Method | Route | `data` trả về |
|---|---|---|
| GET | `api/core/plugin/bo-cai` | `{ "fileName": "KstsPlugin.exe", "exists": true }` |
| GET | `api/core/plugin/bo-cai/noi-dung` | **bytes exe thô, không envelope** |

Bộ cài là **một file exe tự cài**: bấm đúp vào nó là cài luôn middleware **bit4id** đã nhúng sẵn bên trong,
chép plugin vào `%LocalAppData%` rồi chạy nền. Không giải nén, không có file phụ nào để chạy nhầm. File nằm
cạnh bản build BE tại `Plugins/KstsPlugin.exe`.

`exists = false` **không** phải lỗi — FE khoá nút tải kèm lời nhắn liên hệ quản trị. Chỉ khi gọi
`bo-cai/noi-dung` mà thiếu file mới ném `1080 PluginSetupMissing`.

FE **phải tải qua HttpClient rồi tạo blob**, không mở thẳng URL bằng `window.open`: endpoint đòi Bearer
token, mở trần sẽ nhận 401.

## CertScanResult

```jsonc
{
  "certificates": [
    {
      "subject": "CN=Trường Đại học Xây dựng Hà Nội, O=…",
      "commonName": "Trường Đại học Xây dựng Hà Nội",
      "issuer": "CN=CA phục vụ các cơ quan Nhà nước G2, …",
      "issuerCommonName": "CA phục vụ các cơ quan Nhà nước G2",
      "serialNumber": "540E…",
      "thumbprint": "A1B2C3…",
      "source": 2,
      "keyProvider": "bit4id xPKI CSP",
      "validFrom": "01/01/2026 00:00:00",
      "validTo": "01/01/2029 00:00:00",
      "hasPrivateKey": true,
      "isExpired": false,
      "allowsSigning": true,
      "reason": null
    }
  ],
  "storeDiagnostics": ["CurrentUser\\My: 12 chứng thư", "LocalMachine\\My: 2 chứng thư"]
}
```

- `source`: `0` Local · `1` Server · `2` UsbToken.
- `thumbprint` là **khoá định danh** — dùng nó khi chọn cert và khi lưu vào template.
- `reason` = lý do **tại máy** khiến cert không ký được, xét theo thứ tự: thiếu khoá bí mật → hết hạn →
  KeyUsage không cho ký → **khoá không nằm trên USB token**. `null` nghĩa là qua hết các điều kiện đó, FE
  hiển thị "Ký được". Cert nằm trong kho phần mềm của máy (`source` = `0`/`1`) luôn là **"Không ký được"** —
  ký giấy báo trúng tuyển phải bằng khoá trên token, khoá phần mềm sao chép được nên không đủ tư cách.
  Hiển thị `reason` nguyên văn cho người dùng.
- **Không có `canSign` và không có `isTrusted`.** Plugin chạy trên máy không kiểm soát được, cờ tin cậy do nó
  gửi lên là vô giá trị — thẩm định chuỗi về CA đã ghim là việc của BE
  (`ICertificateTrustValidator`), xem [docs/bao-mat-agent-ky-so.md](../docs/bao-mat-agent-ky-so.md) §7.
- `storeDiagnostics` để chẩn đoán khi danh sách rỗng mà không rõ nguyên nhân. Đặt sau một nút "Chẩn đoán",
  không hiện mặc định.
- **Danh sách không được cache** — gọi lại mỗi lần mở màn chọn cert: token có thể vừa cắm hoặc vừa rút.
- Bước lấy danh sách **không bật hộp thoại PIN**: plugin chỉ đọc metadata của khoá, không ký thử.

## Vì sao không cần DLL nào trong plugin

Plugin đọc chứng thư qua **Windows certificate store** (`X509Store`), kể cả cert nằm trên USB token — middleware
của token (bit4id) tự bắc cầu khoá vào store qua minidriver/CSP. Plugin **không** nạp `.dll` PKCS#11 nào, không
p/invoke thư viện của hãng token. Đổi lại, **máy phải cài middleware bit4id** thì token mới hiện trong store —
đó chính là lý do bộ cài gói cả hai vào một exe.

## CORS

Origin của FE khai ở `ksts.plugin.api/appsettings.json` → `Cors:AllowedOrigins`. Thiếu CORS thì trình duyệt
không đọc được kết quả; đây là điều kiện để chạy, **không** phải lớp bảo mật.
