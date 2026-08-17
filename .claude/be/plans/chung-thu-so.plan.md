# Plan — Lấy và chọn chứng thư số (phía BE)

> **Trạng thái: ✅ đã thi công.** Nhưng **màn ký số không dùng đường này** — nó lấy chứng thư từ **plugin** ở
> máy người dùng. Đường BE còn lại phục vụ cấu hình template trên máy dev và trường hợp
> `Signing:Nguon = store`. Xem [../../contracts/chung-thu-so.contract.md](../../contracts/chung-thu-so.contract.md).

## Requirements

- **Lấy danh sách** chứng thư số dùng được để ký, kèm lý do vì sao cert nào không dùng được.
- **Chọn** một chứng thư: thẩm định lại rồi trả về chi tiết để FE hiển thị và gắn vào template.
- Không bao giờ chạm private key, không bật hộp thoại PIN ở bước liệt kê.

## Giới hạn đã biết của giai đoạn này

Nguồn cert hiện tại là **cert store của máy chạy API**. Trên máy dev (BE + trình duyệt cùng máy) chạy đúng;
deploy lên server thật thì đọc cert của server chứ không phải của người dùng.

Đây là **chủ ý**: agent phía client chưa làm. Interface `ICertificateProvider` tách sẵn để đổi nguồn sang
agent mà không sửa service/controller. Xem [../../docs/ky-so-web-vs-desktop.md](../../docs/ky-so-web-vs-desktop.md)
và [../../docs/bao-mat-agent-ky-so.md](../../docs/bao-mat-agent-ky-so.md).

## Steps

1. **External / Certificates** — `ICertificateProvider.GetCertificates()` quét `CurrentUser\My` +
   `LocalMachine\My`, suy nguồn cert (token / local / server) từ tên CSP/KSP, trả kèm `CanSign` + `Reason`.
2. **External / Certificates** — `ICertificateTrustValidator.IsTrusted(cert, atUtc)` dựng chain về Root G1
   hoặc G2 đã ghim, nạp CA trung gian vào `ExtraStore`, đối chiếu pin SHA-256 khi nạp file `.crt`.
3. **Shared** — `ErrorCodes` 1020–1039 + `ErrorMessages`.
4. **Application** — `ICertificateService`: `GetCertificates(query)`, `SelectCertificate(thumbprint)`.
5. **API** — `CertificateController`, route `api/core/chung-thu-so`.
6. **DI** — đăng ký Singleton.

## API

| Method | Route | Query/Body | Trả về |
|---|---|---|---|
| GET | `chung-thu-so` | `onlySignable?` (bool) | `List<ViewSignCertDto>` |
| GET | `chung-thu-so/chan-doan` | — | `CertDiagnosticDto` |
| POST | `chung-thu-so/chon` | `SelectCertDto { thumbprint }` | `ViewSignCertDto` |

`GET chung-thu-so` mặc định trả **hết** kèm `canSign` + `reason`; `onlySignable=true` thì lọc.

`chan-doan` trả `StoreDiagnostics` (store nào mở được, bao nhiêu cert) — trên máy người dùng không có
debugger, đây là đường duy nhất biết vì sao danh sách rỗng.

## Điều kiện `CanSign`

Đủ **cả bốn**: có private key · còn hạn · KeyUsage cho phép (`digitalSignature`/`nonRepudiation`, hoặc **không
khai** KeyUsage — RFC 5280: thiếu extension nghĩa là không giới hạn) · **chain về CA Ban Cơ yếu**.

Vế chain **bắt buộc** có mặt: thiếu nó thì cert ngoài Ban Cơ yếu vẫn hiện "ký được", người dùng chọn xong mới
bị chặn ở bước ký. Phép thử ở đây phải trùng khít bước ký, nếu không danh sách này nói dối.

`Reason` nêu **lý do đầu tiên** chặn việc ký; `null` = ký được.

## Nguyên tắc bảo mật giữ từ đầu

- Bước liệt kê **chỉ đọc metadata khoá**, không ký, nên **không bật hộp thoại PIN**.
- Dựng chain **offline** (`RevocationMode.NoCheck`) nên không chạm khoá bí mật.
- **Không cache danh sách cert** — enumerate lại mỗi lần. Cache là nguồn của loạt bug "token rút ra vẫn hiện".
- Khi cert đến từ agent: **server tự dựng chain**, không tin cờ `IsTrusted` client gửi.

## Expected output

- 3 endpoint chạy, trả `ApiResponse`.
- Không đọc được store nào ⇒ vẫn trả danh sách rỗng + `StoreDiagnostics`, không ném.
- Chọn thumbprint không tồn tại ⇒ `ErrorCodes.CertificateNotFound`.
- Chọn cert không đủ điều kiện ⇒ `ErrorCodes.CertificateCannotSign` kèm `Reason`.
- `dotnet build` sạch.
