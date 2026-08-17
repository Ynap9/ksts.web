# Plan — Plugin ký số ở máy người dùng

> **Trạng thái: 🔶 phần ký đã chạy thật, phần bảo mật nâng cao để tối ưu sau** (cập nhật 2026-08-14).
> Hợp đồng đang chạy: [../../contracts/plugin-ky-so.contract.md](../../contracts/plugin-ky-so.contract.md).
> Nền: [../../docs/luong-ky-so-hang-loat.md](../../docs/luong-ky-so-hang-loat.md). Mã nguồn `ksts.plugin/`.

## Hình dạng đang chạy

Plugin là **web API nghe `http://127.0.0.1:17739`** (topology A). Trang web gọi thẳng vào đó và làm **người
đưa thư**: lấy `SignedAttributes` từ máy chủ, đưa xuống plugin ký, mang chữ ký thô trả về. Plugin **không**
biết gì về tài khoản, **không** nhận file nào, **không** gọi ra máy chủ.

```
BE  <--HTTPS-->  Trang web  <--http://127.0.0.1-->  Plugin  -->  Token
```

## Đã làm

| # | Việc | Kết quả |
|---|---|---|
| 1 | `ICertificateProvider` — liệt kê chứng thư, **không** hỏi PIN | ✅ |
| 2 | `ITokenVerifier` — ký thử mẩu ngẫu nhiên, trả đúng một cờ `valid` + `reason` | ✅ |
| 3 | `ISigningSession` — `MoPhien(thumbprint)` giữ handle khoá, `Ky(đợt)`, `DongPhien()` | ✅ |
| 4 | Phiên tự đóng sau **15 phút** không dùng (`KySoConstants.PhutTuDongDongPhien`) | ✅ |
| 5 | `ky-so/ky` nhận **cả một đợt** yêu cầu, hỏng cái nào trả `loi` riêng cái đó | ✅ |
| 6 | `ky-so/do-toc-do` — đo thời gian một lượt ký thật trên token, trần 100 lượt | ✅ |
| 7 | Bộ cài một file exe, nhúng sẵn middleware **bit4id**, cài per-user | ✅ |
| — | Pairing one-time token, job ticket, WYSIWYS, consent dialog, giám sát rút token | 🔬 chưa — xem cuối |

**PIN bật đúng một lần cho cả lô** ở `ky-so/mo-phien`: mở khoá rồi GIỮ handle. Giữ handle **khác** cache PIN —
PIN vẫn đi thẳng từ bàn phím vào middleware qua CNG/minidriver, không byte nào vào tiến trình plugin.

Plugin đọc chứng thư qua **Windows certificate store** (`X509Store`), kể cả cert nằm trên token — middleware
bit4id tự bắc cầu khoá vào store. **Không** nạp `.dll` PKCS#11 nào. Đổi lại, máy **phải cài middleware** thì
token mới hiện trong store, đó là lý do bộ cài gói cả hai vào một exe.

## Điểm cần chú ý (vẫn đúng)

- **Không tự vẽ ô nhập PIN.** Để middleware bật qua CNG; PIN không vào process. Phải kiểm cấu hình middleware
  vì nó có thể tự cache PIN theo cách riêng.
- Plugin chạy nền **không sở hữu cửa sổ** ⇒ hộp PIN có thể hiện chìm sau trình duyệt. SIPPACK set thuộc tính
  CNG `"HWND Handle"`; plugin cần chạy dạng tray app có cửa sổ ẩn để có HWND mà trỏ. **Chưa kiểm trên máy thật
  có token.**
- **Không cache danh sách cert xuống đĩa** — enumerate lại mỗi lần.
- Cài **per-user** `%LocalAppData%`, autostart `HKCU\...\Run`, không driver, không service SYSTEM ⇒ không UAC.
- **Code signing (tối thiểu OV)** trước khi rollout thật: binary chưa ký + chạy nền + tự khởi động + đụng
  crypto token + kết nối mạng là chân dung malware với AV; máy cơ quan hay bật *Warn and prevent bypass*.
- CORS ở `ksts.plugin.api/appsettings.json` → `Cors:AllowedOrigins` là **điều kiện để chạy**, không phải lớp
  bảo mật: `Origin` chỉ có giá trị với request phát từ browser.

## Còn phải làm ngay

1. **Đo `T`** bằng `ky-so/do-toc-do` khi có token thật — con số quyết định thời lượng của cả lô.
2. **Kiểm hộp PIN có hiện chìm không** trên máy thật.
3. **Giám sát rút token**: quét cert store mỗi 2s, cert biến mất ⇒ đóng phiên. Hiện chỉ có mốc 15 phút không
   dùng, nên rút token giữa lô sẽ biểu hiện thành một loạt file lỗi thay vì một thông báo rõ ràng.

## 🔬 Nghiên cứu — tối ưu sau

Thiết kế đầy đủ ở [../../docs/bao-mat-agent-ky-so.md](../../docs/bao-mat-agent-ky-so.md). Bốn mảnh chưa làm,
xếp theo thứ tự đáng làm trước:

1. **Job ticket** server ký, public key ghim cứng lúc build; kiểm `nonce` chưa dùng, `exp` còn hạn,
   `certThumbprint` khớp cert đang mở, `signedAttrs.Length == opCount`. Chặn T1 + T2 thật sự, thay cho việc
   hiện chỉ dựa vào CORS + phiên do người dùng tự mở.
2. **WYSIWYS** — nhận cả PDF đã prepare, tự tính digest hai dải `/ByteRange` so với `messageDigest`, render
   trang đầu cho người dùng xem. Chặn T3 (server bị chiếm).
3. **Consent dialog native**, OS-modal, topmost: user nào · bao nhiêu file + tên file · chứng thư CN nào.
   Rate limit 3 phiên/phút.
4. **Topology B** — bỏ listener, plugin tự gọi ra server qua WSS. Chỉ cần khi nghiệp vụ đòi **đóng tab mà lô
   vẫn chạy**; phần lõi phía BE (`IHangDoiKy`, `PluginSigningKey`) giữ nguyên, chỉ thay lớp vận chuyển.

Kèm theo là **pairing** (one-time token trong tên file installer) và **gỡ cài đặt báo server unpair** — hai
thứ chỉ có ý nghĩa khi đã có job ticket, vì chúng phục vụ việc server biết plugin nào thuộc về ai.
