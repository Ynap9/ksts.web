# External

`ksts.be.external` giữ **dịch vụ bên thứ ba và dịch vụ dùng chung**: những thứ không phải nghiệp vụ KSTS mà là
kỹ thuật thuần — gọi S3, đọc PDF, đo ảnh, đọc chứng thư, tính toán hình học.

## Vì sao tách riêng

1. **Nghiệp vụ không nên biết chi tiết hạ tầng.** `TemplateService` cần "URL của ảnh sau khi lưu", không cần
   biết MinIO hay `CannedACL`.
2. **Đổi nguồn không phải sửa nghiệp vụ.** `ICertificateProvider` hôm nay đọc cert store của server; mai đọc
   payload agent gửi lên. Đổi implement + đổi dòng DI, service và controller không đụng tới.
3. **Hàm thuần thì test được bằng số liệu tay.** `ISealPlacementResolver` chỉ ăn số đo, không đọc file.

## Nội dung

```
Storage/      IS3FileStorage        → upload/tải/xoá/liệt kê object trên MinIO, trả URL công khai
              ITemplateImageStorage → ảnh dấu đỏ + chữ ký tươi của template
              ILoKyFileStorage      → bản nguồn / bản ký của lô
Certificates/ ICertificateProvider  → liệt kê chứng thư số
              ICertificateTrustValidator → dựng chain về CA Ban Cơ yếu đã ghim
Pdf/          IPdfPreparer          → dựng bản ký nối, chèn placeholder /ByteRange
              IPdfAppearanceBuilder → vẽ mặt chữ ký + ảnh chữ ký tươi
              IPdfContentWriter     → ghi CMS vào chỗ /Contents đã chừa
              IPdfRevisionReader · IPdfObjectWriter · IPdfTextLocator
Signing/      ICmsAssembler         → SignedAttributes + CMS detached (tự dựng bằng AsnWriter)
              ISigningKey           → nguồn ký: PluginSigningKey | StoreSigningKey
              IHangDoiKy            → chỗ hẹn giữa tiến trình ký và token ở máy người dùng
Tsa/          ITimestampClient      → RFC 3161, thử lại tối đa 3 lần
Images/       IImageSizeReader      → đọc pixel + DPI của ảnh, quy ra point
Placement/    ISealPlacementResolver → tính ô đặt con dấu / chữ ký tươi
Excel/        IExcelSheetReader     → đọc sheet thành Dictionary theo TÊN cột
Html/         IHtmlDocumentFiller   → đổ giá trị vào mẫu HTML, ẩn/hiện khối
Gotenberg/    IGotenbergConverter   → HTML → PDF qua dịch vụ ngoài
Qr/           IQrCodeSvgRenderer    → mã QR tra cứu, dạng SVG nhúng thẳng
Jobs/         IZipJobStore          → trạng thái lô dựng giấy báo (bộ nhớ tiến trình)
```

Mỗi nhóm có `Interfaces/`, `Implements/`, `Dtos/`.

## Luật

- **External tự chứa hợp đồng của mình** — interface và DTO đều nằm trong `external`, không nhồi ngược vào
  `applications`. Chiều phụ thuộc là `applications → external`, không bao giờ ngược lại.
- External **chỉ tham chiếu `ksts.be.shared`** (hằng số, mã lỗi). Không đụng DbContext, không đụng entity.
- Đăng ký DI dạng **Singleton** — không giữ trạng thái theo request.

## Ghi chú từng service

### `ISigningKey` — seam quan trọng nhất

Hai implement, chọn bằng cấu hình `Signing:Nguon`:

| Implement | Khoá nằm ở | Dùng khi |
|---|---|---|
| `PluginSigningKey` (mặc định) | Token cắm ở **máy người dùng** | Đường chạy thật |
| `StoreSigningKey` (`"store"`) | Cert store của **máy chạy API** | API và token cùng một máy Windows |

`PluginSigningKey` không tự nói chuyện với máy người dùng: nó bỏ `SignedAttributes` vào `IHangDoiKy` rồi **ngủ
chờ** chữ ký. Nhờ vậy lớp lõi không biết ai là người đưa thư — hôm nay là trang web đang mở, mai có thể là
plugin tự gọi ra qua WebSocket.

### `ICertificateProvider`

Hiện đọc `CurrentUser\My` + `LocalMachine\My` của **máy chạy API**. Store mở lỗi thì bỏ qua store đó chứ không
ném — `LocalMachine` thường không mở được khi chạy quyền user thường, đó là chuyện bình thường. Lý do bỏ qua
ghi vào `StoreDiagnostics` để chẩn đoán được trên máy không có debugger.

Trả về **cả cert không ký được**, kèm `CanSign` + `Reason`. Lọc là việc của tầng service — danh sách rỗng mà
không rõ vì sao là kiểu lỗi tốn cả buổi để chẩn đoán.

Chỉ đọc **metadata** của khoá (tên CSP/KSP), **không** dùng khoá để ký, nên **không bật hộp thoại PIN**.

Xem [../../docs/ky-so-web-vs-desktop.md](../../docs/ky-so-web-vs-desktop.md) về giới hạn của nguồn cert hiện tại.

### `ICertificateTrustValidator`

Dựng chain cert người ký về **Root G1 hoặc Root G2** đã ghim của Ban Cơ yếu (chỉ cần đạt một nhánh). Cert
`.crt` nằm ở `ksts.be.api/Cert`, đối chiếu **pin SHA-256** trong `SignatureConstants` khi nạp — tráo file thì
nạp fail chứ không âm thầm tin cert lạ. CA trung gian phải vào `ExtraStore` vì cert thật chỉ nhúng leaf.

Tách riêng khỏi `ICertificateProvider` vì lý do bảo mật: khi cert đến từ agent ở máy client, **server bắt buộc
tự dựng chain và không được tin cờ do client gửi**.

### `IS3FileStorage`

`AmazonS3Config` bắt buộc `ForcePathStyle = true` + `AuthenticationRegion` — MinIO không hỗ trợ virtual-host
style. Object key do **caller** quyết định, không lấy tên file người dùng upload. Xem
[../../docs/luu-tru-minio.md](../../docs/luu-tru-minio.md).

### `IPdfTextLocator` / `ISealPlacementResolver` / `IImageSizeReader`

Ba mảnh của bài toán đặt dấu: dò mốc chữ → đo ảnh → tính ô. Xem
[../../docs/dat-dau-va-chu-ky-tuoi.md](../../docs/dat-dau-va-chu-ky-tuoi.md).
