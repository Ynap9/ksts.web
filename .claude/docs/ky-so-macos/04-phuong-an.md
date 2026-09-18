# Phương án và lộ trình

> **Phần 4/5** · trước: [03-kha-thi-rust.md](03-kha-thi-rust.md) · mục lục: [README.md](README.md)

## Bảy phụ thuộc Windows trong plugin hiện tại

| Chỗ | Đang dùng | Trên macOS |
|---|---|---|
| TFM & giao diện | `net9.0-windows`, `UseWindowsForms` | Không build được |
| Đọc chứng thư | `X509Store(My, CurrentUser)` | Map sang Keychain nhưng **không thấy cert của token** |
| Nhận diện khoá phần cứng | `RSACng.Key.Provider` | `RSACng` chỉ có trên Windows; không có KSP/CSP |
| Ký | `cert.GetRSAPrivateKey()` | Trả khoá của Keychain, không chạm token |
| Khay | WinForms `NotifyIcon` + ẩn console | Menu bar item |
| Tự cài middleware | Nhúng `.exe/.msi` | Phải là `.pkg` |
| Tự khởi động | Khoá Registry Run | `LaunchAgent` plist |

Phụ thuộc sâu nhất: plugin **cố ý không nói PKCS#11** — middleware bit4id bắc cầu khoá vào store qua minidriver.
macOS không có minidriver, và CryptoTokenKit đời mới không hiện cert thẻ trong Keychain Access (token CTK vẫn nằm
trong miền tìm kiếm của Security.framework với `kSecAttrTokenID`, nhưng driver VGCA thì không chạy trên chip M).

## Ba chi phí bản Windows được miễn

1. **Safari chặn `http://localhost` từ trang HTTPS** — Chrome/Edge/Firefox coi loopback là *potentially trustworthy*,
   Safari thì không. Bản Mac bắt buộc HTTPS loopback + chứng thư được tin ở máy đó.
2. **Code signing** — ad-hoc (`codesign -s - --deep --force`) miễn phí nhưng **bắt buộc từ bản build đầu** trên Apple
   Silicon; Developer ID + notarization chỉ cần khi phát qua web.
3. **Apple Silicon** — plugin tự build thì dễ; chỗ chết là driver token (VGCA chỉ `x86_64`) — lý do dự án tự nói PC/SC.

## Bốn phương án

Hai trục độc lập: **ngôn ngữ** (.NET hay Rust) × **cách chạm thẻ** (nhúng OpenSC hay tự đọc PKCS#15).

Sự thật làm nhẹ bài toán: **plugin không dựng CMS** — `KySoRunner` phía máy chủ băm, dựng `SignedAttributes`, gọi
TSA, ghép CMS. Plugin chỉ **ký thô một dãy byte bằng khoá trong token**. Cả `ksts.plugin` chỉ ~1.400 dòng C#.

| | A · .NET + OpenSC | B · .NET + tự viết | C · Rust + tự viết | D · Rust + OpenSC |
|---|---|---|---|---|
| Dùng lại code cũ | ~70% | ~70% | 0% | 0% |
| Đội đã biết | có | có | chưa | chưa |
| Bẻ phần nén cert | ⚠️ chưa chắc | có | có | ⚠️ chưa chắc |
| Cỡ bản phát hành | ~80 MB self-contained | ~80 MB | **~8 MB** | ~10 MB |
| Phụ thuộc lúc chạy | runtime .NET | như A | **không** | **không** |
| Tiền lệ ở VN | — | — | “Ký số đa năng” | — |
| Ước công sức | 1–2 tuần | 3–4 tuần | 4–6 tuần + học Rust | 3–4 tuần + học Rust |

⚠️ Cột A/D “bẻ phần nén: chưa chắc” sửa ngày 2026-09-17 — bản đầu ghi “không” vì tưởng OpenSC tự giải nén; xem
[01-token-vgca.md](01-token-vgca.md). Nếu nén là việc bắt buộc ở mọi nhánh thì lợi thế công sức của A và D co lại.

**Khuyến nghị gốc (2026-09-08)**: đi **A** trước, giữ **C** làm hướng chiến lược, chỉ chuyển Rust khi có người viết
Rust thật, cần hợp nhất hai bản plugin, hoặc dung lượng/khởi động của .NET thành vấn đề thật. Người dùng đã chọn
**nghiên cứu hướng Rust** (2026-09-17) — chuẩn bị ở [05-chuan-bi-rust.md](05-chuan-bi-rust.md).

## Kiến trúc nếu đi .NET — một mã nguồn, hai lớp thiết bị

```text
ksts.plugin.shared        net10.0   giữ nguyên
ksts.plugin.applications  net10.0   giữ nguyên
ksts.plugin.api           net10.0   Kestrel + controller, thêm HTTPS loopback
ksts.plugin.external      net10.0   ICertificateProvider · ISigningSession   ← hai lớp cần thay
  ├─ Windows/ (net10.0-windows)     X509Store + CNG — đang có
  └─ Pcsc/    (net10.0)             a) Pkcs11Interop 5.3.0 + opensc-pkcs11 nhúng · b) tự đọc PKCS#15 bằng APDU
ksts.plugin.desktop       net10.0   vỏ Avalonia 11: TrayIcon/NativeMenu, thay external/Tray
```

`MoPhien` / `Ky` / `DongPhien` đã đúng hình dạng cho cả PKCS#11 lẫn APDU trần. Đặt tên `Pcsc/` chứ không `Mac/` vì
PC/SC chạy cả trên Windows — sau này có thể dùng chung, bỏ phụ thuộc minidriver bit4id, nhưng chỉ làm sau khi bản
Mac chạy thật.

## Lộ trình gốc

1. **Bước 0 · Windows** — chĩa OpenSC vào token ([01-token-vgca.md](01-token-vgca.md)); song song dựng lớp PC/SC.
2. **Bước 1** — hạ TFM, tách lớp thiết bị, xác nhận bản Windows vẫn chạy y cũ.
3. **Bước 2** — headless trên Mac: HTTPS loopback, gọi `chung-thu-so` và `ky-so/mo-phien` bằng `curl`.
4. **Bước 3** — vỏ giao diện, LaunchAgent, cài chứng thư loopback, `.app` ký ad-hoc chép USB. Kèm việc treo từ 29/08:
   thêm origin prod của FE gọi `kssm.be` vào `PluginConstants.OriginMacDinh`.
5. **Bước 4** — lô thật trên Safari và Chrome, đo `T` qua `ky-so/do-toc-do`. **Mốc quyết định đầu tư tiếp.**
6. **Bước 5 · chỉ khi bước 4 xanh** — Developer ID: xin miễn phí diện giáo dục → nhờ đối tác ký hộ (như gói VCTK do
   Mobile-ID ký) → hoặc 99 USD/năm.

## Nếu phép thử thẻ bế tắc — hai đường vòng

- **Ký số từ xa** (khoá trong HSM nhà cung cấp, xác thực app/OTP): không plugin, chạy mọi hệ điều hành. Vướng: là PKI
  công cộng của NEAC (`VIETNAM NATIONAL ROOT CA - RS`), không phải PKI chuyên dùng Ban Cơ yếu —
  `CertificateTrustValidator` của `kssm.be` chỉ ghim root Ban Cơ yếu nên sẽ từ chối. Là **quyết định nghiệp vụ**.
- **Ký phía máy chủ** (token/HSM cắm tại server): máy trạm không cần gì; `ISigningKey` đã là interface, `ksts.be` có
  nhánh `Signing:Nguon = store`. Vướng: mất chống chối bỏ cá nhân — hợp giấy báo trúng tuyển, không hợp chữ ký
  đích danh người có thẩm quyền.

> **Tiếp:** [05-chuan-bi-rust.md](05-chuan-bi-rust.md) — chuẩn bị Rust và thứ tự học.
