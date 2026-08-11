# Plan — Lô ký và hàng đợi job

Nền: [../../docs/luong-ky-so-hang-loat.md](../../docs/luong-ky-so-hang-loat.md).
Phần dựng PDF + CMS tách sang [ky-so-dung-pdf.plan.md](ky-so-dung-pdf.plan.md).

## Input

- Thư mục PDF người dùng upload (có thể vài nghìn file, vài GB).
- `templateId` đã cấu hình sẵn trong DB.
- `thumbprint` chứng thư người dùng chọn (plugin liệt kê, chưa hỏi PIN).

## Steps

1. **Domain** — `LoKy` (id, tên, templateId, thumbprint, trạng thái, thời điểm tạo/xong) và `LoKyFile`
   (id, loKyId, tên file, đường dẫn nguồn, đường dẫn đã ký, trạng thái, lý do lỗi, thứ tự).
   Trạng thái file: `Cho` · `DangKy` · `Xong` · `Loi`.
2. **Infrastructure** — migration cho hai bảng, index theo `(loKyId, trangThai)` để lấy việc kế tiếp nhanh.
3. **External / Storage** — lưu PDF nguồn và PDF đã ký lên MinIO theo tiền tố `lo-ky/{loKyId}/`.
   Object key do server đặt, **không** lấy tên file người dùng upload.
4. **Application** — `ILoKyService`:
   - `TaoLoAsync(templateId)` — mở lô rỗng, trả `loKyId` để FE bắt đầu đẩy file.
   - `ThemFileAsync(loKyId, files)` — nhận **một đợt** upload, ghi file nguồn, thêm dòng `LoKyFile`. Gọi lại
     nhiều lần cho tới hết thư mục; đợt hỏng thì FE gửi lại đúng đợt đó.
   - `BatDauAsync(loKyId, thumbprint)` — kiểm template + cert, dựng **job ticket**, đẩy job vào hàng đợi.
   - `TrangThaiAsync(loKyId)` — tiến độ + danh sách file lỗi cho FE poll.
   - `HuyAsync(loKyId)` · `TaiZipAsync(loKyId)`.
5. **Application** — `IJobKyQueue`: plugin lấy việc kế tiếp, nộp chữ ký, báo lỗi. Mỗi lần cấp việc gắn
   `fileId` + hạn hoàn thành; quá hạn thì trả file về `Cho` để cấp lại.
6. **External / Ticket** — `IJobTicketSigner`: dựng và ký ticket bằng khoá riêng của server (không liên quan
   chứng thư người dùng). Public key tương ứng ghim cứng trong plugin.
7. **API** — `LoKyController`, route `api/core/lo-ky`. Kênh WSS cho plugin tách riêng.

## API

| Method | Route | Body/Query | Trả về |
|---|---|---|---|
| POST | `lo-ky` | `{ templateId }` | `ViewLoKyDto` |
| POST | `lo-ky/{id}/them-file` | multipart một đợt file | `ViewLoKyDto` |
| POST | `lo-ky/{id}/bat-dau` | `{ thumbprint }` | `ViewLoKyDto` |
| GET | `lo-ky/{id}/trang-thai` | — | `ViewTienDoDto` |
| POST | `lo-ky/{id}/huy` | — | `ViewLoKyDto` |
| GET | `lo-ky/{id}/zip` | — | **bytes zip thô, không envelope** |

## Output mong muốn

- Tạo lô 5000 file rồi bấm Bắt đầu ⇒ plugin nhận việc và ký, FE poll thấy tiến độ tăng dần.
- Đóng tab ⇒ lô **vẫn chạy tiếp**; mở lại thấy đúng tiến độ.
- Rút token / mất kết nối ⇒ lô dừng, file đã ký **giữ nguyên**, bấm Bắt đầu lại thì chạy tiếp từ file dở.
- File lỗi hiện rõ lý do; ký lại được riêng phần lỗi.
- `dotnet build` sạch.

## Điểm cần chú ý

- **Không gom PDF vào RAM.** Đọc/ghi theo stream; zip kết quả ghi thẳng ra file tạm trên đĩa
  (`FileOptions.DeleteOnClose`), 5000 giấy báo là hàng trăm MB đến vài GB.
- **Chạy tiếp phải idempotent**: lấy việc kế tiếp luôn theo `trangThai = Cho`, nên cấp trùng cũng không ký đè
  file đã `Xong`.
- Ticket khoá vào **một** cert + **một** user + **đúng** `opCount`; hết là chết, **không gia hạn**.
- `nonce` đã dùng giữ RAM kèm TTL — chống replay (T6).
- Lỗi một file **không** làm dừng lô; chỉ rút token / mất kết nối mới dừng.
- Zip chỉ đóng khi lô đã xong; đang dở thì nút tải khoá lại.
- **Upload chia đợt**: `them-file` phải chịu được gọi lại cùng một đợt (FE gửi lại khi hỏng) mà không nhân đôi
  dòng — khử trùng theo tên file trong phạm vi lô.
- Chỉ cho `bat-dau` khi lô đã nhận đủ file và người dùng xác nhận xong bước upload.
