# Plan — Plugin: topology B, phiên PIN, route ký

Nền: [../../docs/luong-ky-so-hang-loat.md](../../docs/luong-ky-so-hang-loat.md). Mã nguồn `ksts.plugin/`.

## Input

- Bộ cài mang one-time token trong tên file (`KySoSetup-a3f9c2.exe`).
- Job ticket do server ký, kèm PDF đã prepare + `SignedAttributes` cho từng file.

## Đổi so với hiện trạng

Plugin hiện là **web API nghe `127.0.0.1:17739`** với 3 route đọc cert. Phải bỏ listener, đổi sang **client
WSS gọi ra server**. Phần `CertificateProvider` / `TokenVerifier` giữ nguyên, chỉ đổi đường vào.

## Steps

1. **Pairing** — installer đọc token trong tên file của chính nó, ghi vào `%LocalAppData%`; plugin đăng ký với
   server, nhận `deviceId` + khoá phiên. Token dùng một lần, sống 30 phút, gắn cứng vào user.
2. **Kết nối** — mở WSS ra server, tự kết nối lại khi rớt (backoff). Không mở cổng lắng nghe nào.
3. **External / Certificates** — giữ `ICertificateProvider` (liệt kê, **không** hỏi PIN) và `ITokenVerifier`.
4. **External / Signing** — `ISigningSession` mới:
   - `Open(thumbprint)` — ký thử mẩu ngẫu nhiên (**chỗ duy nhất PIN bật**), giữ handle khoá.
   - `Sign(hash)` — ký bằng handle đã mở, không hỏi PIN lại.
   - `Close()` — dispose handle tường minh.
   - Tự đóng sau **15 phút không dùng**; đóng ngay khi rút token.
5. **Kiểm job ticket** — verify chữ ký server bằng public key **ghim cứng lúc build**; kiểm `nonce` chưa dùng,
   `exp` còn hạn, `certThumbprint` khớp cert đang mở, `signedAttrs.Length == opCount`. Thiếu một mục ⇒ từ chối.
6. **WYSIWYS** — với mỗi file: tự tính digest 2 dải `/ByteRange` từ bytes thật, so với `messageDigest` trong
   `SignedAttributes`; kiểm `signingCertificateV2` trỏ đúng cert của mình. Lệch ⇒ từ chối, báo server.
7. **Giám sát token** — quét cert store mỗi 2s. Cert biến mất ⇒ đóng phiên, báo server dừng lô.
8. **Consent** — dialog native, OS-modal, topmost, hiện đúng ba thứ: user nào · bao nhiêu file + tên file ·
   chứng thư CN nào. Rate limit 3 phiên/phút.
9. **Gỡ cài đặt** — mục trong Apps & Features, icon khay có Thoát + Gỡ; khi gỡ: kill process → xoá autostart →
   xoá `%LocalAppData%` → **báo server unpair**.

## Output mong muốn

- Plugin chạy nền, không mở cổng nào, tự kết nối lại sau khi rớt mạng.
- Cả lô hỏi PIN **đúng một lần**; đóng tab trình duyệt không làm dừng lô.
- Rút token giữa chừng ⇒ phiên đóng trong ≤2s, server nhận được tín hiệu dừng.
- Ticket sai / digest lệch ⇒ từ chối ký, ghi audit, **không** ký bừa.

## Điểm cần chú ý

- **Không tự vẽ ô nhập PIN.** Để middleware bit4id bật qua CNG; PIN không vào process.
- Phân biệt **cache PIN** với **giữ key handle**: giữ handle một phiên là thứ khiến N file hỏi PIN một lần,
  đó *không* phải cache PIN. Phải kiểm cấu hình middleware vì nó có thể tự cache PIN theo cách riêng.
- Plugin chạy nền **không sở hữu cửa sổ** ⇒ hộp PIN có thể hiện chìm sau trình duyệt. SIPPACK set thuộc tính
  CNG `"HWND Handle"`; plugin cần chạy dạng tray app có cửa sổ ẩn để có HWND mà trỏ.
- **Không cache danh sách cert xuống đĩa** — enumerate lại mỗi lần.
- Cài **per-user** `%LocalAppData%`, autostart `HKCU\...\Run`, không driver, không service SYSTEM ⇒ không UAC.
- **Code signing (tối thiểu OV)** trước khi rollout thật: binary chưa ký + chạy nền + tự khởi động + đụng
  crypto token + kết nối ra ngoài là chân dung malware với AV; máy cơ quan hay bật *Warn and prevent bypass*.
- Rủi ro tồn dư phải nói thẳng với khách: malware có quyền user trên máy đang cắm token, PIN đã nhập thì dùng
  thẳng CryptoAPI — agent **không phải** lớp phòng thủ ở đây.
