# Contracts BE ↔ FE

Hợp đồng API do BE công bố, FE cài đặt theo. **BE là nguồn chân lý** — lệch nhau thì sửa FE.

| File | Nội dung |
|---|---|
| [template-chu-ky.contract.md](template-chu-ky.contract.md) | CRUD template cấu hình chữ ký + vị trí gợi ý |
| [chung-thu-so.contract.md](chung-thu-so.contract.md) | Lấy và chọn chứng thư số **từ máy chạy API** |
| [plugin-ky-so.contract.md](plugin-ky-so.contract.md) | Plugin ở máy người dùng: dò đã cài, đọc chứng thư thật, ký hộ |
| [lo-ky.contract.md](lo-ky.contract.md) | **Ký số hàng loạt** — lô, vòng đưa thư, tiến độ, tải zip |
| [giay-bao.contract.md](giay-bao.contract.md) | Dựng giấy báo trúng tuyển hàng loạt từ Excel |

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

## Ngoại lệ không bọc envelope

| Route | Trả về | Vì sao |
|---|---|---|
| `GET template-chu-ky/file-mau/noi-dung` | bytes PDF | Trình xem PDF cần nội dung, không cần envelope |
| `GET core/plugin/bo-cai/noi-dung` | bytes exe | Bộ cài plugin |
| `GET lo-ky/{id}/zip?token=…` | bytes zip | Vài GB, trình duyệt tải thẳng xuống đĩa |
| `GET giay-bao/tao-zip/{jobId}/tai-ve?token=…` | bytes zip | Như trên |

Hai đường zip cũng là **hai đường duy nhất không đòi Bearer**: trình duyệt điều hướng tới đó thì không gắn
được header `Authorization`, nên chặn bằng token phát riêng cho lô. Sai token ⇒ **404 trơn**, không nêu lý do.
