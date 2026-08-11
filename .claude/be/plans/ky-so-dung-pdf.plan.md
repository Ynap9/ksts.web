# Plan — Dựng PDF, CMS và dấu thời gian

Nền: [../../docs/luong-ky-so-hang-loat.md](../../docs/luong-ky-so-hang-loat.md).
Phần lô/job tách sang [ky-so-lo-va-job.plan.md](ky-so-lo-va-job.plan.md).
Bê tri thức từ `Sip.be.External/Documents` — xem [../../docs/ky-so-web-vs-desktop.md](../../docs/ky-so-web-vs-desktop.md).

## Input

- 1 file PDF nguồn + `Template` (vị trí tỉ lệ 0..1, ảnh chữ ký tươi trên MinIO, lý do/nơi ký, kiểu hiển thị).
- Chữ ký thô do plugin trả về (ở bước sau).

## Steps

1. **External / Pdf** — `IPdfPreparer.Prepare(pdf, template)`:
   - Dựng revision mới bằng **nối bản** (incremental update): giữ nguyên byte gốc, ghi thêm vào cuối.
   - Chèn signature dictionary (`SubFilter = ETSI.CAdES.detached`) + widget + placeholder `/Contents` 32KB.
   - Trả về bytes đã prepare + **hai dải `/ByteRange`**.
2. **External / Pdf** — `IPdfAppearanceBuilder` vẽ appearance stream theo **kiểu hiển thị** của template:
   - **Kiểu A**: ô 170×30pt, dòng 1 CN người ký, dòng 2 giờ ký ISO 8601 giờ VN; co giãn theo khổ trang
     (`AppearanceReferencePageWidth/Height`), kẹp trong trang, sàn `AppearanceMinScale`.
   - **Kiểu B**: appearance = **ảnh chữ ký tươi**, không vẽ chữ; widget đặt trùng lên ảnh. Vị trí lấy từ
     trung điểm dò được (`ISealPlacementResolver`); **dò không ra thì lùi về `TemplatePosition`**, không ném.
3. **External / Signing** — `ICmsAssembler`:
   - `BuildSignedAttributes(hash, cert)` — `Pkcs9SigningTime` + **`signingCertificateV2` tự dựng bằng
     `AsnWriter`** (.NET không tự thêm).
   - `Assemble(signedAttrs, chuKyTho, cert, tsaToken)` — CMS detached, `IncludeOption = WholeChain`.
4. **External / Tsa** — `ITimestampClient.RequestTokenAsync(chuKy)` theo RFC 3161, **thử lại tối đa 3 lần**,
   giãn cách tăng dần. Số luồng lấy từ cấu hình, chưa chốt (đo sau).
5. **External / Pdf** — `IPdfContentWriter` ghi CMS đã hoàn chỉnh vào đúng chỗ `/Contents` đã chừa.
6. **Shared** — `SigningConstants` + `SignatureConstants` bê từ SIP; `ErrorCodes` dải mới cho ký số.

## Output mong muốn

- File ký ra mở bằng Foxit/Adobe: chữ ký **hợp lệ**, có dấu thời gian, chain về CA Ban Cơ yếu.
- Kiểu B: bấm vào **ảnh chữ ký tươi** ra bảng thông tin chữ ký.
- File PDF đã có chữ ký từ trước ⇒ ký thêm **không giết** chữ ký cũ.
- TSA hỏng sau 3 lần thử ⇒ file báo lỗi, **không** sinh file thiếu dấu thời gian.
- `dotnet build` sạch.

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
- Dò chữ hỏng ở kiểu A thì vẫn ném `SealAnchorNotFound` như cũ; chỉ **kiểu B** mới có đường lùi về
  `TemplatePosition`, vì kiểu B luôn có sẵn toạ độ template để dùng.
