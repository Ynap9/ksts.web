# Plan — Nút Dừng, nút Huỷ và đường ký tiếp (FE)

> **Trạng thái: ✅ đã thi công, chưa chạy thử trên máy thật** (2026-08-18). Phần BE ở
> [../../be/plans/dung-va-huy-lo-ky.plan.md](../../be/plans/dung-va-huy-lo-ky.plan.md) ✅. Màn hình:
> `pages/ky-so`, xem
> [../architecture/04-man-hinh-dac-thu.md](../architecture/04-man-hinh-dac-thu.md).

## Vì sao — điều tài liệu từng nói sai

Trước bản này, ba tài liệu khẳng định "bấm Bắt đầu lại thì chạy tiếp từ file dở". Điều đó đúng với BE nhưng
**FE chưa bao giờ gọi tới**: `onBatDau` luôn chạy `taoLoVaDayFile()` ⇒ lô mới tinh, upload lại, ký lại từ file
số 1. Thêm một chốt cửa: sau khi mở lại màn, `files()` và `duongDanKho()` rỗng ⇒ `coNguon()` false ⇒
`coTheBatDau()` false ⇒ nút Bắt đầu không bấm được.

## Input — cái đã có

- `getLoDangChay()` đã nạp lại lô dở và tiến độ khi mở màn.
- `datLaiLo()` đã có, nhưng **chỉ** xoá `lo` / `tienDo` / `phanTramUpload`.
- `moPhienKy()` và `goiBatDau()` đã tách riêng, gọi lại được mà không cần đi qua `taoLoVaDayFile()`.

## Steps

1. **Service** — `lo-ky.service.ts` thêm `dung(loKyId)` trỏ `POST lo-ky/{id}/dung`.
2. **Model** — `IViewTienDoLoKy` thêm `coTheTaiZip`; bổ sung `TamDung` vào chỗ đọc `trangThai`.
3. **Ký tiếp** — `onBatDau` tách hai nhánh: có lô dở thì gọi thẳng `moPhienKy` + `goiBatDau` trên lô đó,
   **bỏ qua** `taoLo` và toàn bộ khâu upload; không có lô dở thì giữ nguyên đường hiện tại.
4. **Mở cổng** — `coTheBatDau` cho phép khi đang có lô dở, dù `coNguon()` false.
5. **Nút** — `ky-so.html` tách **Dừng** và **Huỷ**; nút chính đổi nhãn thành **Ký tiếp** khi có lô dở. Hai
   việc này khác hẳn nhau về hậu quả, gộp một nút là người dùng vô tình upload lại vài GB.
6. **Huỷ dọn màn** — `onHuy` gọi `datLaiLo()` **và** xoá `rows` + `thoiGianTheoThuTu`; bỏ lời gọi `hoiTienDo()`
   ngay sau đó. Hiện bảng nghìn dòng của lô vừa huỷ vẫn nằm nguyên trên màn.
7. **Rời màn là Dừng, không phải Huỷ** — `dungLoKhiRoiMan` chuyển sang `dung`, và gọi **tuần tự**: `dung` xong
   mới `dongPhienKy`.
8. **Tải zip** — `taiDuoc` đọc `coTheTaiZip` của BE thay vì tự suy từ `hoanTat`.
9. **Tiến độ** — `hoiTienDo` thêm nhánh cho lô dừng: `hoanTat = false` và `dangChay = false` ⇒ báo "đã dừng,
   bấm Ký tiếp để chạy tiếp", thay vì im lặng như hiện nay.
10. `npm run build` sạch trong `ksts.fe`.

## Expected output

- Bấm **Dừng** ⇒ lô ngừng, bảng giữ nguyên, nút đổi thành **Ký tiếp**, tải zip phần đã ký được.
- Bấm **Ký tiếp** ⇒ hỏi PIN một lần, chạy tiếp **từ file kế tiếp**, không upload lại file nào.
- Mở lại màn giữa lô dở ⇒ thấy đúng tiến độ **và** bấm Ký tiếp được ngay, không phải chọn lại nguồn.
- Bấm **Huỷ** ⇒ màn về trống, sẵn sàng lô mới; lần sau mở màn không thấy lô đó nữa.

## Điểm cần chú ý

⚠️ **Gọi `dongPhienKy` trước `dung` là hỏng đúng 8 file.** BE ném `InvalidOperationException` vào các lượt
đang chờ, chúng rơi vào nhánh lỗi thay vì được trả về hàng đợi ⇒ ký tiếp bỏ sót đúng ngần ấy file. Hôm nay
`dungLoKhiRoiMan` đang bắn **song song** hai lời gọi, phải sửa thành tuần tự.

⚠️ **`datLaiLo()` không đủ để dọn màn.** Nó không đụng `rows` và `thoiGianTheoThuTu` — hai signal giữ bảng
nghìn dòng. Huỷ mà quên chúng là lô mới hiện chồng lên danh sách của lô cũ.

- **Ký tiếp vẫn hỏi PIN lại** — phiên ký cũ đã đóng, không tránh được. Nói rõ trên màn hình.
- Giữ **hai vòng riêng** (đưa thư và hỏi tiến độ) khi khởi động lại đường ký; gộp là hỏng cả hai, xem
  [../architecture/04-man-hinh-dac-thu.md](../architecture/04-man-hinh-dac-thu.md).
- `TamDung` **không** mang `hoanTat = true`: cờ đó nghĩa là lô đã chốt. Tải zip đi theo `coTheTaiZip` riêng.
- Nhãn nút phải phân biệt rõ **Dừng** (còn ký tiếp) với **Huỷ** (bỏ hẳn) — dùng câu tiếng Việt đủ nghĩa trong
  hộp xác nhận, đừng để người dùng đoán.
