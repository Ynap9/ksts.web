# KSTS — Ký số tuyển sinh

Module web ký số giấy báo trúng tuyển, Trường Đại học Xây dựng Hà Nội.

Toàn bộ tri thức dự án nằm trong thư mục này. Đọc theo thứ tự dưới đây trước khi viết code.

## Bản đồ tài liệu

| Đường dẫn | Nội dung |
|---|---|
| [dang-lam.md](dang-lam.md) | **Đọc đầu phiên.** Đang làm dở tới đâu, việc kế tiếp là gì |
| [docs/ky-so-web-vs-desktop.md](docs/ky-so-web-vs-desktop.md) | **Đọc đầu tiên.** Vì sao không bê thẳng SIPPACK sang được |
| [docs/bao-mat-agent-ky-so.md](docs/bao-mat-agent-ky-so.md) | Thiết kế bảo mật agent/token đã nghiên cứu — threat model, job ticket, PIN, WYSIWYS |
| [docs/luong-ky-so-hang-loat.md](docs/luong-ky-so-hang-loat.md) | **Quyết định đã chốt** cho luồng ký số hàng loạt — topology B, vòng đời lô, kiểu hiển thị chữ ký |
| [docs/luu-tru-minio.md](docs/luu-tru-minio.md) | Ảnh dấu đỏ / chữ ký tươi lưu ở đâu, key đặt thế nào |
| [docs/dat-dau-va-chu-ky-tuoi.md](docs/dat-dau-va-chu-ky-tuoi.md) | Cách tính vị trí đặt dấu + chữ ký tươi trên trang |
| [be/architecture/](be/architecture/README.md) | Kiến trúc BE: 6 project, trách nhiệm từng tầng |
| [be/plans/](be/plans/) | Kế hoạch từng tính năng phía BE |
| [plugin/plans/](plugin/plans/) | Kế hoạch cho plugin ký số ở máy người dùng |
| [fe/plans/](fe/plans/) | Kế hoạch từng màn hình FE |
| [contracts/](contracts/) | Hợp đồng API BE↔FE — BE là nguồn chân lý |

## Bối cảnh

KSTS kế thừa tri thức từ **SIPPACK** (`C:\Users\Admin\workspace\Sip`) — app **desktop** đóng gói tài liệu lưu
trữ, đã có luồng ký số PDF chạy thật. KSTS dùng lại phần lớn tri thức đó nhưng **là web**, nên mọi giả định
"BE chạy cùng máy với người dùng" đều sai.

## Trạng thái

| Phần | Trạng thái |
|---|---|
| Template cấu hình chữ ký (CRUD) | ✅ Xong |
| Upload ảnh dấu đỏ / chữ ký tươi lên MinIO | ✅ Xong |
| Lấy + chọn chứng thư số | ✅ Xong — **nguồn cert tạm thời là cert store của máy chạy API** |
| Tính vị trí đặt dấu + chữ ký tươi | ✅ Xong |
| Import data tuyển sinh → PDF hàng loạt qua Gotenberg | ✅ Xong — mẫu HTML tự chứa, không gọi CDN |
| Agent đọc token ở máy client | ❌ Chưa — luồng đã chốt ở `docs/luong-ky-so-hang-loat.md`, plan ở `plugin/plans/` |
| Luồng ký PDF + đóng dấu thời gian TSA | ❌ Chưa — plan ở `be/plans/ky-so-*.plan.md` |
| FE | 🔶 Có màn Template, Chứng thư số, Import data tuyển sinh |

## Quy ước viết code (bắt buộc)

1. **Không viết hàm `private`.** Cần tách việc thì tách thành service có interface, không đẻ helper riêng tư.
2. **Comment chỉ ở đầu hàm** (`<summary>` XML), ngắn gọn, chuyên nghiệp. **Không comment bên trong thân hàm.**
3. Service bên thứ ba hoặc dùng chung (S3, đọc PDF, đo ảnh, tính vị trí) → đặt ở `ksts.be.external`.
4. Dấu đỏ và chữ ký tươi là **tuỳ chọn** — mọi trường liên quan đều nullable (`?`).
5. **Con dấu không được resize** — vẽ đúng kích thước gốc của ảnh.

Chi tiết đầy đủ: [be/architecture/08-conventions.md](be/architecture/08-conventions.md).
