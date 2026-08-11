# Luồng ký số hàng loạt — quyết định đã chốt

> Chốt ngày 2026-08-11. Đọc sau [ky-so-web-vs-desktop.md](ky-so-web-vs-desktop.md) và
> [bao-mat-agent-ky-so.md](bao-mat-agent-ky-so.md). File kia là nghiên cứu; file này là **cái đã quyết**.

## Phân vai

| Việc | Ở đâu |
|---|---|
| Nhận thư mục PDF upload, tạo lô ký | Server |
| Đọc template từ DB, dựng PDF có placeholder `/ByteRange` | Server |
| Tính hash 2 dải ByteRange, dựng `SignedAttributes` | Server |
| **`sign(hash)` bằng khoá token** | **Plugin máy user** |
| Gọi TSA, ghép CMS vào `/Contents`, ghi file | Server |
| Dựng chain về Root G1/G2 đã ghim | Server |

Thao tác mật mã **không thể** chạy trên server: khoá bí mật nằm trong chip token, không trích xuất được. Qua
mạng chỉ có hash + cert phần public; PIN và khoá không bao giờ rời máy user.

## Topology B — plugin tự gọi ra server

```
Browser (FE) ──HTTPS──> Server <──WSS outbound── Plugin ──> Token
                       (hàng đợi job)
```

Plugin **không mở cổng nào**. Website khác không gọi được plugin (xoá sổ T1), hết mixed content, hết Private
Network Access, hết xung đột port. FE chỉ tạo lô và xem tiến độ — **đóng tab thì lô vẫn ký tiếp**.

Đây là thay đổi so với [contracts/plugin-ky-so.contract.md](../contracts/plugin-ky-so.contract.md) đang mô tả
topology A (plugin nghe `127.0.0.1:17739`). Contract đó phải viết lại khi thi công.

## Vòng đời một lô

1. Upload thư mục PDF → server lưu thành **lô**, mỗi file một dòng trạng thái `chờ`.
2. Chọn **template** (DB) và **chứng thư số** (plugin liệt kê, **không hỏi PIN**).
3. Bấm Bắt đầu → server tạo **job** + phát **job ticket**.
4. Plugin mở **phiên**: ký thử mẩu ngẫu nhiên → **hộp PIN bật đúng một lần** → giữ handle khoá.
5. Plugin lấy việc từ hàng đợi, ký từng file; server đóng TSA và ghi file.
6. Hết lô → dispose handle. Người dùng tải zip.

Phiên sống hết lô, **tự đóng sau 15 phút không dùng**. Kết thúc ngay khi: hết lô · rút token · user huỷ ·
plugin thoát.

## Trình tự một file

```
Server  dựng PDF theo template → hash 2 dải ByteRange → SignedAttributes
   ↓  WSS (kèm PDF đã prepare)
Plugin  tự tính lại digest từ bytes → so với messageDigest   ← lệch là TỪ CHỐI
        kiểm signingCertificateV2 trỏ đúng cert của mình
        sign(hash) bằng handle đã mở
   ↓  chữ ký thô
Server  CMS detached + signingCertificateV2 → TSA (thử lại tối đa 3 lần, giãn cách tăng dần)
        → gắn token TSA làm unsigned attribute → ghi vào /Contents
```

Plugin **tự tính digest**, không tin server nói (chặn T3: server bị chiếm). Server nói dối được về *tên* file
nhưng không nói dối được về *nội dung* đang ký.

**Fail-closed**: TSA hỏng sau 3 lần thử ⇒ file đó hỏng. Không bao giờ phát hành chữ ký thiếu dấu thời gian.

## Bảo mật

**Job ticket** server ký, public key ghim cứng vào plugin lúc build. Plugin kiểm đủ, thiếu một mục là từ chối:
chữ ký server hợp lệ · `nonce` chưa dùng · `exp` còn hạn · `certThumbprint` khớp cert vừa chọn ·
`signedAttrs.Length == opCount`. Ticket khoá vào **một** cert, **một** user, **đúng** số lần ký; hết là chết,
không gia hạn.

**Không lưu**: PIN không nhận vào process · khoá bí mật bất khả thi · cert enumerate lại mỗi lần · nonce đã
dùng giữ RAM kèm TTL · handle khoá chỉ RAM, dispose tường minh. Audit log ghi thời điểm / jobId / thumbprint /
số file — **không** PIN, **không** nội dung file.

**PIN** do middleware bit4id tự bật qua CNG. Không tự vẽ ô nhập PIN.

## Template quyết định gì

| Trường | Dùng để |
|---|---|
| `TemplatePosition` (tỉ lệ 0..1) | Vị trí khối chữ ký số, áp được mọi khổ giấy |
| Ảnh chữ ký tươi (MinIO) | Vẽ lúc ký, co giãn theo khung, giữ tỉ lệ ảnh |
| Lý do ký / nơi ký | Vào signature dictionary |
| Kiểu hiển thị | A hoặc B bên dưới |

**Kiểu A — khối chữ ký số hiển thị**: ô 170×30pt, hai dòng CN người ký + giờ ký, đặt tại `TemplatePosition`,
co giãn theo khổ trang.

**Kiểu B — ẩn khối, chữ ký tươi làm mặt chữ ký**: không vẽ khối text. Widget chữ ký số đặt **trùng lên ảnh
chữ ký tươi**, appearance stream chính là ảnh đó ⇒ mở bằng Foxit/Adobe, **bấm vào chữ ký tươi là ra bảng
thông tin chữ ký**. Vẫn là chữ ký PAdES hợp lệ, chỉ khác phần vẽ.

Vị trí kiểu B: **dò chữ trước** (trung điểm chức danh ↔ tên người ký, bằng PdfPig); dò không ra thì **lùi về
`TemplatePosition`** chứ không ném lỗi.

**Con dấu** vẽ sẵn trong mẫu HTML ở bước import — luồng ký **không** đặt dấu. Hệ quả đã chấp nhận: chữ ký tươi
vẽ sau nên nằm **trên** dấu, ngược thứ tự lớp so với bản giấy. Ảnh dấu nền trong suốt nên vẫn đọc được cả hai.

## Chỗ lưu file

PDF nguồn và PDF đã ký đều nằm trên **MinIO**, tiền tố `lo-ky/{loKyId}/`. Object key do server đặt, không lấy
tên file người dùng upload. Không giữ file trên đĩa máy chạy API — API có thể chạy nhiều instance hoặc trong
container không có ổ bền vững.

## Dừng và chạy tiếp

Rút token (plugin quét mỗi 2s) hoặc mất kết nối ⇒ dừng lô ngay, dispose handle. **File đã ký xong giữ nguyên
và vẫn hợp lệ**; cắm lại token, nhập PIN, lô chạy tiếp từ file dở. Không làm lại từ đầu.

## Upload

Chia thành nhiều đợt, ghép dần vào lô. Đợt nào hỏng thì gửi lại đúng đợt đó — vài GB một request sẽ đụng giới
hạn proxy và timeout, hỏng là tải lại từ đầu.

## Còn phải chốt

**Số luồng TSA và tốc độ token** — đo thật rồi mới chốt. Nút thắt cứng là **token** (một phiên phần cứng, ký
tuần tự), không phải TSA: 5000 lượt × 200ms là 17 phút không cách nào rút ngắn. Nâng luồng TSA chỉ có tác dụng
khi TSA đang là nút thắt; cần `C ≥ L / T` với `L` là độ trễ TSA, `T` là thời gian một lượt ký qua token.
