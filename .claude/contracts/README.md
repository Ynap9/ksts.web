# Contracts BE ↔ FE

Hợp đồng API do BE công bố, FE cài đặt theo. **BE là nguồn chân lý** — lệch nhau thì sửa FE.

| File | Nội dung |
|---|---|
| [template-chu-ky.contract.md](template-chu-ky.contract.md) | CRUD template cấu hình chữ ký + vị trí gợi ý |
| [chung-thu-so.contract.md](chung-thu-so.contract.md) | Lấy và chọn chứng thư số |
| [plugin-ky-so.contract.md](plugin-ky-so.contract.md) | Plugin ở máy người dùng: dò đã cài chưa + đọc chứng thư thật |

## Luật chung

- Mọi endpoint trả **HTTP 200**; trạng thái thật nằm trong envelope.

```jsonc
{ "status": 1, "data": { }, "code": 200, "message": "Ok" }   // status: 1 = ok, 0 = lỗi
```

- FE **phải gate `status === 1`** trước khi đọc `data`. Lỗi thì `code` là mã trong `ErrorCodes` và `message`
  là câu tiếng Việt hiển thị được cho người dùng.
- JSON trả về **camelCase**.
- Enum serialize thành **SỐ**.
- Phân trang: `pageNumber` đếm từ **1**; **`pageSize = -1` lấy hết**; envelope là
  `{ items, totalItems, customData }` (một chữ *m* trong `customData`).
- Toạ độ luôn là **tỉ lệ 0..1** so với khổ trang, gốc **trên-trái**, **Y hướng xuống**.
- Mọi endpoint yêu cầu **Bearer token**.

## Ngoại lệ duy nhất không bọc envelope

`GET api/core/template-chu-ky/file-mau/noi-dung` trả bytes PDF thô — trình xem PDF cần nội dung, không cần
envelope.
