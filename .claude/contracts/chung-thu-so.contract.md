# Contract — Chứng thư số

Gốc: `api/core/chung-thu-so`. Yêu cầu Bearer token. Xem luật chung ở [README.md](README.md).

> ⚠️ **Đây KHÔNG phải đường mà màn ký số dùng.** Nguồn chứng thư ở đây là cert store của **máy chạy API**;
> trên máy dev (BE và trình duyệt cùng máy) chạy đúng, lên server thật thì đọc cert của server.
>
> Màn ký số lấy danh sách chứng thư **từ plugin ở máy người dùng** —
> [plugin-ky-so.contract.md](plugin-ky-so.contract.md). Giữ đường này vì nó vẫn tiện cho cấu hình template
> trên máy dev và là seam đối chứng khi `Signing:Nguon = store`.

## Routes

| Method | Route | Tham số | `data` trả về |
|---|---|---|---|
| GET | `chung-thu-so` | `onlySignable?` (bool) | `SignCert[]` |
| GET | `chung-thu-so/chan-doan` | — | `CertDiagnostic` |
| POST | `chung-thu-so/chon` | body `{ thumbprint }` | `SignCert` |

`GET chung-thu-so` mặc định trả **hết**, gồm cả cert không ký được, kèm `canSign` + `reason`.
`onlySignable=true` thì lọc còn cert ký được.

## SignCert

```jsonc
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
  "isTrusted": true,
  "canSign": true,
  "reason": null
}
```

- `source`: `0` Local · `1` Server · `2` UsbToken.
- `thumbprint` là **khoá định danh** — dùng nó khi chọn cert và khi lưu vào template.
- `canSign` = `hasPrivateKey && !isExpired && allowsSigning && isTrusted`. **Tính ở BE, đừng tự tính lại ở FE.**
- `reason` nêu **lý do đầu tiên** chặn việc ký; `null` nghĩa là ký được. Hiển thị nguyên văn cho người dùng.

## CertDiagnostic

```jsonc
{
  "totalCertificates": 3,
  "signableCertificates": 1,
  "storeDiagnostics": [
    "CurrentUser\\My: 3 chứng thư",
    "LocalMachine\\My: không mở được (CryptographicException: Access denied)"
  ]
}
```

Dùng khi danh sách rỗng mà không rõ nguyên nhân. Trên máy người dùng không có debugger, đây là đường duy nhất
để biết vì sao. FE nên đặt sau một nút "Chẩn đoán", không hiện mặc định.

## Ghi chú FE phải nắm

- **Danh sách không được cache.** Gọi lại mỗi lần mở màn chọn cert: token có thể vừa được cắm hoặc vừa rút.
- **Bước lấy danh sách không bật hộp thoại PIN** — BE chỉ đọc metadata của khoá, không ký thử.
- **Luôn gọi `chon` trước khi dùng cert.** Nó thẩm định lại tại thời điểm chọn; cert có thể vừa hết hạn hoặc
  token vừa bị rút giữa hai lời gọi.
- Cert `canSign = false` vẫn **hiển thị** trong danh sách nhưng **không cho chọn**, và hiện `reason` bên cạnh.
  Ẩn hẳn đi là người dùng cắm token rồi vẫn không hiểu vì sao không thấy cert của mình.

## Mã lỗi

| Code | Ý nghĩa |
|---|---|
| `1020` | Không tìm thấy chứng thư theo thumbprint (token chưa cắm?) |
| `1021` | Chứng thư không đủ điều kiện ký — `message` chính là `reason` |
| `1022` | Không đọc được kho chứng thư |
