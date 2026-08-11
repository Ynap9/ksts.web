# Plan — FE màn ký số hàng loạt

Nền: [../../docs/luong-ky-so-hang-loat.md](../../docs/luong-ky-so-hang-loat.md).
API: [../../be/plans/ky-so-lo-va-job.plan.md](../../be/plans/ky-so-lo-va-job.plan.md).

## Input

- Thư mục PDF trên máy người dùng.
- Danh sách template (DB) và danh sách chứng thư số (plugin).

## Steps

1. **Scaffold** — `ng generate component pages/ky-so` + `ky-so.routes.ts`, thêm vào `app.routes.ts` và menu.
   Không tạo tay từng file.
2. **Models / Service** — `giay-ky.models.ts`, `lo-ky.service.ts` gọi 5 endpoint của `api/core/lo-ky`.
3. **Chọn nguồn** — ô kéo thả nhận **cả thư mục** (`webkitdirectory`), lọc `.pdf`, hiện số file đã nhận.
   Tạo lô rỗng rồi đẩy file **theo từng đợt** (~50 file/đợt); đợt hỏng thì gửi lại đúng đợt đó.
4. **Chọn template** — `p-select` lấy từ API template; hiện kiểu hiển thị (A/B) để người dùng biết trước.
5. **Chọn chứng thư** — gọi plugin liệt kê (**không** hỏi PIN). Chưa cài plugin ⇒ popup tải bộ cài.
6. **Bảng file** — cột STT · tên file · tình trạng (`Chờ` / `Đang ký` / `Xong` / `Lỗi` + lý do).
   Scroll **trong bảng**, không scroll trang.
7. **Tiến độ** — thanh process theo `đã xong / tổng`, poll `lo-ky/{id}/trang-thai` theo nhịp cố định.
8. **Nút** — Bắt đầu · Huỷ · Tải zip (chỉ mở khi lô đã xong).
9. **Mở lại màn** — còn lô đang chạy thì tự nạp lại tiến độ, **không** bắt tạo lô mới.

## Output mong muốn

- Chọn thư mục → chọn template → chọn cert → Bắt đầu → bảng chạy dần, thanh tiến độ tăng.
- **Đóng tab rồi mở lại vẫn thấy đúng tiến độ** (lô chạy nền ở server + plugin).
- Rút token giữa chừng ⇒ màn báo dừng, nêu rõ số file đã ký; bấm Bắt đầu lại thì chạy tiếp phần còn lại.
- File lỗi hiện lý do đọc được, ký lại được riêng phần lỗi.
- `ng build` sạch.

## Điểm cần chú ý

- Theo khuôn sakai: breadcrumb **trong** page, tiêu đề ngay dưới, tất cả bọc trong một `<div class="card">`.
- **FE không nói chuyện với plugin trong lúc ký.** Topology B: plugin tự nhận việc từ server. FE chỉ gọi
  plugin đúng một chỗ là **liệt kê chứng thư**, và chỉ để hiển thị.
- Bước liệt kê chứng thư **không bao giờ** hỏi PIN — PIN chỉ bật khi plugin mở phiên ký.
- Không đặt timeout cho lời gọi cần người dùng thao tác (nhập PIN).
- Upload vài GB: hiện tiến độ upload riêng, tách khỏi tiến độ ký — hai việc khác nhau, gộp một thanh là
  người dùng hiểu nhầm. Đợt hỏng thì thử lại đúng đợt, **không** bắt chọn lại cả thư mục.
- Chỉ mở nút Bắt đầu khi đã đẩy xong toàn bộ đợt.
- Poll trạng thái phải **nhẹ**: chỉ xin số đếm + danh sách file đổi trạng thái, không tải lại cả 5000 dòng.
- Danh sách chứng thư **không cache** — token có thể vừa cắm hoặc vừa rút.
