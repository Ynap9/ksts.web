# KSTS — Ký số tuyển sinh

Module web ký số giấy báo trúng tuyển, Trường Đại học Xây dựng Hà Nội.

Toàn bộ tri thức dự án nằm trong thư mục này. Đọc theo thứ tự dưới đây trước khi viết code.

## Bản đồ tài liệu

| Đường dẫn | Nội dung |
|---|---|
| [dang-lam.md](dang-lam.md) | **Đọc đầu phiên.** Đang làm dở tới đâu, việc kế tiếp là gì |
| [docs/ky-so-web-vs-desktop.md](docs/ky-so-web-vs-desktop.md) | **Đọc đầu tiên.** Vì sao không bê thẳng SIPPACK sang được |
| [docs/luong-ky-so-hang-loat.md](docs/luong-ky-so-hang-loat.md) | **Luồng ký đang chạy** — trang web làm người đưa thư, vòng đời lô, số đo thật |
| [docs/dung-giay-bao-tuyen-sinh.md](docs/dung-giay-bao-tuyen-sinh.md) | Luồng dựng giấy báo từ Excel — khâu đứng trước luồng ký |
| [docs/bao-mat-agent-ky-so.md](docs/bao-mat-agent-ky-so.md) | 🔬 **Nghiên cứu, tối ưu sau**: threat model, topology B, job ticket, WYSIWYS |
| [docs/luu-tru-minio.md](docs/luu-tru-minio.md) | Kho object: bản đồ tiền tố, quy tắc đặt key |
| [docs/dat-dau-va-chu-ky-tuoi.md](docs/dat-dau-va-chu-ky-tuoi.md) | Cách tính vị trí đặt dấu + chữ ký tươi trên trang |
| [be/architecture/](be/architecture/README.md) | Kiến trúc BE: 6 project, trách nhiệm từng tầng |
| [fe/architecture/](fe/architecture/README.md) | Kiến trúc FE: Angular 21 zoneless, route/guard, service, hai màn đặc thù |
| [be/plans/](be/plans/) | Kế hoạch từng tính năng phía BE |
| [plugin/plans/](plugin/plans/) | Kế hoạch cho plugin ký số ở máy người dùng |
| [fe/plans/](fe/plans/) | Kế hoạch từng màn hình FE |
| [contracts/](contracts/) | Hợp đồng API BE↔FE — BE là nguồn chân lý |
| [skills/](skills/) | Khuôn làm việc: [task-workflow](skills/task-workflow/SKILL.md) · [write-markdown](skills/write-markdown/SKILL.md) |

Tài liệu gắn nhãn 🔬 là **nghiên cứu chưa thi công**, giữ lại để tối ưu sau. Đừng đọc chúng như mô tả hệ thống
đang chạy.

## Bối cảnh

KSTS kế thừa tri thức từ **SIPPACK** (`C:\Users\Admin\workspace\Sip`) — app **desktop** đóng gói tài liệu lưu
trữ, đã có luồng ký số PDF chạy thật. KSTS dùng lại phần lớn tri thức đó nhưng **là web**, nên mọi giả định
"BE chạy cùng máy với người dùng" đều sai.

## Trạng thái (2026-08-14)

| Phần | Trạng thái |
|---|---|
| Template cấu hình chữ ký (CRUD) + ảnh trên kho | ✅ Xong |
| Tính vị trí đặt dấu + chữ ký tươi | ✅ Xong |
| Dựng giấy báo trúng tuyển hàng loạt qua Gotenberg | ✅ Xong — mẫu HTML tự chứa, không gọi CDN |
| Luồng ký PDF + CMS + dấu thời gian TSA | ✅ Xong — đã ký PDF thật, TSA thật, verify hợp lệ |
| Lô ký hàng loạt (`api/core/lo-ky`) | ✅ Xong — 8 luồng, tiến độ, tạm dừng / huỷ, chạy tiếp, tải zip |
| Plugin ký ở máy người dùng (`ksts.plugin`) | ✅ Xong phần ký — mở phiên giữ handle khoá, ký theo đợt |
| FE | ✅ Template · Chứng thư số · Import tuyển sinh · Ký số |
| **Chạy thử lô thật với token trên prod** | ❌ **Việc kế tiếp** — xem `dang-lam.md` |
| Job ticket · WYSIWYS · topology B (plugin gọi ra WSS) | 🔬 Nghiên cứu, tối ưu sau |

Nguồn ký chọn bằng `Signing:Nguon` trong `appsettings.json`: bỏ trống là **plugin ở máy người dùng** (mặc
định), `store` là đọc cert store của máy chạy API (chỉ khi API và token cùng một máy).

## Quy ước viết code (bắt buộc)

1. **Không viết hàm `private`.** Cần tách việc thì tách thành service có interface, không đẻ helper riêng tư.
2. **Comment ít, bằng tiếng Anh, chỉ ở đầu hàm** (`<summary>` XML / JSDoc), một câu, trả lời **vì sao**. Không
   comment trong thân hàm, không comment lan man, không kể lịch sử sửa đổi, không để lại code chết.
3. **Tên hàm và biến ưu tiên tiếng Anh**; chỉ giữ tiếng Việt cho khái niệm nghiệp vụ không có từ tiếng Anh sát
   nghĩa (`GiayBaoTrungTuyen`, `ChuKyTuoi`, `DauDo`). Route, cột DB, khoá JSON và câu cho người dùng **giữ
   nguyên tiếng Việt** — chúng là hợp đồng đã công bố. Tên cũ không đổi hàng loạt.
4. Service bên thứ ba hoặc dùng chung (S3, đọc PDF, đo ảnh, tính vị trí) → đặt ở `ksts.be.external`.
5. Dấu đỏ và chữ ký tươi là **tuỳ chọn** — mọi trường liên quan đều nullable (`?`).
6. **Con dấu không được resize** — vẽ đúng kích thước gốc của ảnh.

Chi tiết đầy đủ: [be/architecture/08-conventions.md](be/architecture/08-conventions.md) (BE) ·
[fe/architecture/05-conventions.md](fe/architecture/05-conventions.md) (FE).

Cách chạy một việc nhiều bước và cách viết tài liệu: [skills/task-workflow](skills/task-workflow/SKILL.md) ·
[skills/write-markdown](skills/write-markdown/SKILL.md).
