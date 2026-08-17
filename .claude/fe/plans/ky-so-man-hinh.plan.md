# Plan — FE màn ký số hàng loạt

> **Trạng thái: ✅ đã thi công** (`pages/ky-so`, cập nhật 2026-08-14). Hợp đồng:
> [../../contracts/lo-ky.contract.md](../../contracts/lo-ky.contract.md) +
> [../../contracts/plugin-ky-so.contract.md](../../contracts/plugin-ky-so.contract.md).
> Nền: [../../docs/luong-ky-so-hang-loat.md](../../docs/luong-ky-so-hang-loat.md).

## Vai của FE — NGƯỜI ĐƯA THƯ

Đây là thay đổi lớn nhất so với bản plan đầu. Trang web **không chỉ** xem tiến độ: nó là đường duy nhất nối
máy chủ với token, chạy một vòng lặp suốt lô.

```
vongDuaThu(loKyId):
    GET  lo-ky/{id}/cho-ky          <- lời gọi bị GIỮ tới khi có việc, tối đa 25s
    POST plugin ky-so/ky            <- token ký, không hỏi PIN lại
    POST lo-ky/{id}/chu-ky
    lặp cho tới khi lô hoàn tất
```

⚠️ **Đóng tab là lô dừng.** Phải nói rõ trên màn hình. File đã ký giữ nguyên; bấm Bắt đầu lại chạy tiếp từ file
dở.

## Đã làm

1. **Scaffold** — `pages/ky-so` + `ky-so.routes.ts`, `lo-ky.service.ts`, `plugin.service.ts`.
2. **Chọn nguồn** — hai đường: kéo thả **cả thư mục** (`webkitdirectory`, lọc `.pdf`, đẩy ~50 file/đợt), hoặc
   **dán đường dẫn thư mục trên kho** (`them-tu-kho`) — đường này không tải lên byte nào và là đường dùng thật
   cho giấy báo vừa dựng.
3. **Chọn template** — lấy từ API template.
4. **Chọn chứng thư** — gọi **plugin** liệt kê (không hỏi PIN). Chưa cài plugin ⇒ popup tải bộ cài qua BE.
5. **Xác thực chứng thư** — nút riêng, gọi `kiem-tra-token` (hộp PIN bật).
6. **Bảng file** — nạp một lần bằng `danh-sach-file`, rồi vá dần bằng `filesVuaXong` / `filesLoi` của tiến độ.
   Cột STT · tên file · tình trạng · thời gian ký · dấu thời gian.
7. **Tiến độ** — hỏi `lo-ky/{id}/trang-thai` theo nhịp, **tách khỏi** vòng đưa thư.
8. **Nút** — Bắt đầu · Huỷ · Tải zip (điều hướng thẳng `window.location` kèm `taiToken`).
9. **Mở lại màn** — `lo-ky/dang-chay` nạp lại tiến độ, không bắt tạo lô mới.

## Điểm cần chú ý

- **Vòng đưa thư và vòng hỏi tiến độ là hai vòng riêng.** Gộp làm một thì mỗi nhịp tiến độ lại chặn một lượt
  ký, hoặc ngược lại.
- `cho-ky` **không đặt timeout ngắn** và gọi lại **ngay** khi nó trả về — đừng chờ thêm giữa hai lượt: token ký
  tuần tự nên mọi khoảng chờ nhân thẳng với số file.
- Không đặt timeout cho lời gọi cần người dùng thao tác (nhập PIN).
- Danh sách chứng thư **không cache** — token có thể vừa cắm hoặc vừa rút.
- Poll tiến độ phải **nhẹ**: BE chỉ trả file lỗi + tối đa 100 file vừa xong, không bao giờ cả 5000 dòng.
- Tải zip **phải điều hướng thẳng**, không `HttpClient` + blob: lô vài GB vào bộ nhớ trang là hết bộ nhớ.
- Upload chia đợt: đợt hỏng thì thử lại **đúng đợt đó**, không bắt chọn lại cả thư mục. Tiến độ upload hiện
  riêng, tách khỏi tiến độ ký.
- Theo khuôn sakai: breadcrumb **trong** page, tiêu đề ngay dưới, bọc trong một `<div class="card">`.

## Còn treo

- **Nối lại vòng đưa thư** khi mở lại màn giữa lô: hiện thấy tiến độ nhưng không ai mang chữ ký đi, các lượt
  ký hết hạn sau 120 giây và file tính lỗi. Vướng ở chỗ nối lại thì phải hỏi PIN lần nữa — cần quyết định giao
  diện trước.
- **Người dùng nhập PIN hai lần** một lô (xác thực chứng thư, rồi mở phiên ký). Bỏ bước xác thực thì lỗi cert
  sai hiện muộn hơn, sau khi đã tải file lên. Chưa quyết.
