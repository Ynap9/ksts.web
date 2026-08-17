# Plan — Lô ký và hàng đợi chữ ký

> **Trạng thái: ✅ đã thi công và chạy thật** (cập nhật 2026-08-14). Hợp đồng đang chạy:
> [../../contracts/lo-ky.contract.md](../../contracts/lo-ky.contract.md). Nền:
> [../../docs/luong-ky-so-hang-loat.md](../../docs/luong-ky-so-hang-loat.md). Phần dựng PDF + CMS tách sang
> [ky-so-dung-pdf.plan.md](ky-so-dung-pdf.plan.md).

## Input

- PDF nguồn: **tải lên** từ máy người dùng, hoặc **trỏ vào thư mục có sẵn trên kho**.
- `templateId` đã cấu hình sẵn trong DB.
- `thumbprint` chứng thư người dùng chọn (plugin liệt kê, chưa hỏi PIN).

## Đã làm

| # | Việc | Kết quả |
|---|---|---|
| 1 | **Domain** — `LoKy` + `LoKyFile`, trạng thái file `Cho`/`DangKy`/`Xong`/`Loi` | ✅ |
| 2 | **Infrastructure** — migration `AddLoKy`, `AddTaiTokenLoKy`, `BoBuocDayLenKhoRieng` | ✅ |
| 3 | **External / Storage** — `ILoKyFileStorage` ghi bản nguồn, `IS3FileStorage` đẩy bản ký | ✅ |
| 4 | **Application** — `ILoKyService`: tạo lô · thêm file (2 đường) · mở/đóng phiên · bắt đầu · tiến độ · huỷ · nén | ✅ |
| 5 | **External / Signing** — `IHangDoiKy` thay cho `IJobKyQueue`: chỗ hẹn giữa tiến trình ký và token | ✅ |
| 6 | **Application** — `IKySoRunner` chạy nền, 8 luồng, mỗi luồng tự rút việc | ✅ |
| 7 | **API** — `LoKyController`, route `api/core/lo-ky` | ✅ |
| — | **External / Ticket** — `IJobTicketSigner` | 🔬 **không làm** — xem phần cuối |

## Khác so với plan gốc

**Không có kênh WSS và không có job ticket.** Trang web đang mở làm **người đưa thư**: nó lấy
`SignedAttributes` từ máy chủ (`cho-ky`, lời gọi bị giữ), đưa xuống plugin qua `127.0.0.1`, rồi nộp chữ ký về
(`chu-ky`). Cùng mức bảo mật đường truyền — qua mạng vẫn chỉ có `SignedAttributes` + chữ ký thô + cert công
khai — nhưng ít việc hơn hẳn và kịp cho bản chạy thật.

Hệ quả **phải nói rõ**: ⚠️ **đóng tab là lô dừng**. File đã ký giữ nguyên, bấm Bắt đầu lại thì chạy tiếp từ file
dở. Plan gốc hứa "đóng tab lô vẫn chạy" — điều đó chỉ đúng với topology B.

**Thêm ngoài plan:**

- `them-tu-kho` — nhận file từ thư mục có sẵn trên kho, **không chép byte nào**. Đây là đường dùng thật, vì
  giấy báo chưa ký đã nằm sẵn trên kho sau khâu dựng.
- `mo-phien` / `dong-phien` — nhận chứng thư phần công khai, server tự dựng chuỗi tin cậy.
- `danh-sach-file` — danh sách đầy đủ, gọi **một lần** khi mở màn; tiến độ chỉ trả file lỗi + file vừa xong.
- `dang-chay` — mở lại màn hình thấy đúng tiến độ.
- `taiToken` + đường zip `AllowAnonymous`, zip **dựng ngay lúc tải**.
- Bỏ hẳn bước "đẩy lô lên kho" riêng: mỗi file ký xong ghi **thẳng** vào thư mục dùng chung
  (migration `BoBuocDayLenKhoRieng`) — bước cũ tốn hai vòng đi-về tới kho cho mỗi file.

## Điểm cần chú ý (vẫn đúng)

- **Không gom PDF vào RAM.** Zip ghi thẳng vào luồng gửi đi, tải trước 8 file; máy chủ không giữ file nén nào.
- **Chạy tiếp phải idempotent**: lấy việc luôn theo `TrangThai = Cho` nên cấp trùng cũng không ký đè file đã
  `Xong`.
- Lỗi một file **không** làm dừng lô. Ngoại lệ: **không tải được ảnh chữ ký tươi** của template ⇒ dừng cả lô,
  vì phát hiện 5000 tờ thiếu chữ ký sau khi ký xong thì phải ký lại toàn bộ.
- Bấm Huỷ ⇒ các luồng ném `OperationCanceledException`; **phải nuốt riêng loại đó** rồi mới chốt trạng thái,
  không thì lô bị ghi thành **Lỗi** thay vì **Huỷ**.
- **Upload chia đợt** chịu được gọi lại cùng một đợt — khử trùng theo tên file trong phạm vi lô.
- Zip chỉ mở khi lô đã chốt; đang chạy dở thì `1162`.
- Lô bị huỷ **không** dọn file nguồn: còn phải ký tiếp từ file dở.

## 🔬 Nghiên cứu — tối ưu sau

**Job ticket + hàng đợi job phía server + kênh WSS cho plugin.** Chỉ cần khi nghiệp vụ đòi **đóng tab mà lô
vẫn chạy**, hoặc khi phải chặn được T3 (server bị chiếm). Đổi sang đó **không phải viết lại phần lõi**:
`IHangDoiKy` và `PluginSigningKey` giữ nguyên, chỉ thay lớp vận chuyển. Thiết kế ticket ở
[../../docs/bao-mat-agent-ky-so.md](../../docs/bao-mat-agent-ky-so.md) §3.
