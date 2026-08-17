# Plan — Dựng PDF, CMS và dấu thời gian

> **Trạng thái: ✅ đã thi công, đã ký PDF thật với TSA thật và verify hợp lệ** (cập nhật 2026-08-14).

Nền: [../../docs/luong-ky-so-hang-loat.md](../../docs/luong-ky-so-hang-loat.md).
Phần lô/hàng đợi tách sang [ky-so-lo-va-job.plan.md](ky-so-lo-va-job.plan.md).
Bê tri thức từ `Sip.be.External/Documents` — xem [../../docs/ky-so-web-vs-desktop.md](../../docs/ky-so-web-vs-desktop.md).

## Input

- 1 file PDF nguồn + `Template` (vị trí tỉ lệ 0..1, ảnh chữ ký tươi trên kho, lý do/nơi ký, hai cờ mặt chữ ký).
- Chữ ký thô do plugin ở máy người dùng trả về.

## Steps

1. **External / Pdf** — `IPdfPreparer.Prepare(pdf, options)`:
   - Dựng revision mới bằng **nối bản** (incremental update): giữ nguyên byte gốc, ghi thêm vào cuối.
   - Chèn signature dictionary (`SubFilter = ETSI.CAdES.detached`) + widget + placeholder `/Contents` 32KB.
   - Trả về bytes đã prepare + **hai dải `/ByteRange`**.
2. **External / Pdf** — `IPdfAppearanceBuilder` vẽ appearance stream theo **hai cờ độc lập** của template —
   **không phải "kiểu A / kiểu B" loại trừ nhau**: FE là hai checkbox riêng, DB là hai cột bool riêng.
   - `HienThiChuKySo`: ô 170×30pt, dòng 1 CN người ký, dòng 2 giờ ký giờ VN; co giãn theo khổ trang
     (`AppearanceReferencePageWidth/Height`), kẹp trong trang, sàn `AppearanceMinScale`.
   - `NhoiChuKySoVaoAnh`: widget chữ ký đặt **trùm lên ảnh chữ ký tươi và con dấu** ⇒ bấm vào ảnh ra bảng
     thông tin ký. Tắt cờ này thì ảnh chữ ký tươi vẫn được vẽ, chỉ là annotation Stamp thường.
   - Bật **cả hai** ⇒ một chữ ký, nhiều widget `/Kids`. Tắt cả hai ⇒ chữ ký **vô hình**, vẫn hợp lệ.
   - Ảnh chữ ký tươi còn chịu `DoDamChuKyTuoi` (mảng `/Decode`) và `DoDayNetChuKyTuoi` (vẽ 8 lớp lệch quanh
     tâm + lớp lõi, **cùng một instance `XImage`** nên không phình file).
3. **External / Signing** — `ICmsAssembler`:
   - `BuildSignedAttributes(hash, cert)` — `Pkcs9SigningTime` + **`signingCertificateV2` tự dựng bằng
     `AsnWriter`** (.NET không tự thêm).
   - `Assemble(signedAttrs, chuKyTho, cert, tsaToken)` — CMS detached, `IncludeOption = WholeChain`.
4. **External / Tsa** — `ITimestampClient.RequestTokenAsync(chuKy)` theo RFC 3161, **thử lại tối đa 3 lần**,
   giãn cách tăng dần. Đo thật: TSA chỉ **11–23 ms**, không phải nút thắt, không cần chỉnh số luồng riêng.
5. **External / Pdf** — `IPdfContentWriter` ghi CMS đã hoàn chỉnh vào đúng chỗ `/Contents` đã chừa.
6. **Shared** — `SigningConstants` + `SignatureConstants` bê từ SIP; `ErrorCodes` 1140–1159 cho ký số.

## Output — đã đạt

- ✅ File ký ra mở bằng Foxit/Adobe: chữ ký **hợp lệ**, có dấu thời gian, chain về CA Ban Cơ yếu.
- ✅ Bật `NhoiChuKySoVaoAnh`: bấm vào **ảnh chữ ký tươi** ra bảng thông tin chữ ký.
- ✅ File PDF đã có chữ ký từ trước ⇒ ký thêm **không giết** chữ ký cũ, byte bản cũ giữ nguyên tuyệt đối.
- ✅ TSA hỏng sau 3 lần thử ⇒ file báo lỗi, **không** sinh file thiếu dấu thời gian.
- CMS hoàn chỉnh ~5,1 KB / chỗ trống 32 KB — thừa gấp 6 lần, an toàn.

## Điểm cần chú ý

- **Tuyệt đối không dùng `PdfSharp.Save()`** để ghi file đã ký: nó ghi lại cả tài liệu và **giết chữ ký có
  sẵn**. Phải tự nối bản — đây là lý do SIPPACK có `PdfIncrementalSigner` viết tay.
- PDFsharp 6 không tự dùng font hệ thống: bật `GlobalFontSettings.UseWindowsFontsUnderWindows` một lần cho cả
  process, nếu không vẽ chữ ký ném lỗi thiếu font. Cần font Unicode cho tên có dấu.
- Chỗ trống `/Contents` **ước lượng thừa có chủ đích** (32KB): chữ ký + chain + token TSA không đoán chính xác
  được, chừa thiếu là hỏng file, chừa thừa chỉ tốn vài KB.
- `signingCertificateV2` thiếu ⇒ không conform CAdES, Adobe/Foxit có thể từ chối.
- **Fail-closed** với TSA: không bao giờ phát hành chữ ký thiếu dấu thời gian.
- Toạ độ template là **tỉ lệ 0..1**, gốc trên-trái, Y hướng xuống — quy đổi sang hệ PDF (gốc dưới-trái) đúng
  một chỗ, không rải phép đổi khắp nơi.
- **Con dấu đã vẽ sẵn trong mẫu HTML** ở bước import ⇒ luồng ký **không** đặt dấu. `ISealPlacementResolver`
  hiện có chỉ còn dùng cho chữ ký tươi.
- Chữ ký tươi vẽ sau nên nằm **trên** dấu — ngược thứ tự lớp so với bản giấy, đã chấp nhận. Đừng "sửa" bằng
  cách vẽ lại dấu ở tầng ký: sẽ thành hai con dấu trên một tờ.
- Vị trí lấy thẳng từ `TemplatePosition` của template. Đường dò chữ (`ISealPlacementResolver`) chỉ còn phục vụ
  màn "vị trí gợi ý" lúc cấu hình template, không nằm trong luồng ký hàng loạt.
- **Chữ ký ký rời**: không dùng được `SignedCms.ComputeSignature` vì khoá nằm ở máy người dùng. Tự dựng CMS
  bằng `AsnWriter`. Mẹo cốt lõi: `SignedAttributes` gửi đi ký là **DER SET OF** (thẻ `0x31`); khi nhét vào
  `SignerInfo` thì **đổi đúng byte thẻ đầu thành `0xA0`** ([0] IMPLICIT), giữ nguyên phần còn lại. Dựng lại bộ
  thuộc tính lần hai là nguồn lệch một byte làm chữ ký hợp lệ bị coi là sai.
- **PDFsharp 6**: `XImage.FromStream` nhận thẳng `Stream` (không phải `Func<Stream>`), và stream phải **sống
  tới sau `document.Save`** — đóng sớm là mất ảnh, không báo lỗi.
- `PdfAppearanceBuilder.Draw` phải nằm trong khoá: PDFsharp giữ bộ nhớ đệm font dùng chung cho cả tiến trình.
- Bộ thử ký nằm ở scratchpad (`kytest`), dựng lại dễ: console app tham chiếu `ksts.be.external`, tự ký một
  chứng thư tạm, prepare → ký → TSA thật → ghi file → đọc lại `/ByteRange` từ file và `CheckSignature`.
- Độ đậm / độ dày nét ảnh chữ ký tươi: xem
  [../../docs/dat-dau-va-chu-ky-tuoi.md](../../docs/dat-dau-va-chu-ky-tuoi.md).
