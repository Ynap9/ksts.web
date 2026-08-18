# Plan — Tách Dừng khỏi Huỷ ở lô ký (BE)

> **Trạng thái: ✅ đã thi công, chưa chạy thử trên máy thật** (2026-08-18). Hợp đồng đang chạy:
> [../../contracts/lo-ky.contract.md](../../contracts/lo-ky.contract.md). Nền:
> [../../docs/luong-ky-so-hang-loat.md](../../docs/luong-ky-so-hang-loat.md). Phần FE tách sang
> [../../fe/plans/dung-va-huy-lo-ky.plan.md](../../fe/plans/dung-va-huy-lo-ky.plan.md).

## Vì sao

Hôm nay chỉ có **một** cách dừng lô (`huy`) và nó gánh hai nghĩa trái nhau. Tách thành hai thao tác:

| | Dừng | Huỷ |
|---|---|---|
| File đang ký dở | trả về `Cho` | để nguyên |
| Ký tiếp từ file kế tiếp | ✅ | ❌ |
| Bản đã ký trên kho | giữ | **giữ** |
| File nguồn `lo-ky/{id}/nguon/` | giữ, còn cần | **dọn** |
| Hiện lại ở `lo-ky/dang-chay` | ✅ | ❌ |
| Tải zip phần đã ký | ✅ | bản ghi còn, nhưng FE không còn đường bấm |

## Input — cái đã có

- `NhanViecAsync` lọc `TrangThai == Cho` nên **chạy tiếp vốn đã idempotent**, không ký đè file `Xong`.
- `BatDauAsync` không kiểm `TrangThai` của lô ⇒ gọi lại trên lô đã dừng là chạy tiếp được ngay.
- `GhiNenAsync` chỉ chặn khi `DangChay(loKyId)` ⇒ lô đã dừng nén được, **không phải sửa đường nén**.
- `KyMotFileAsync` đã bắt riêng `OperationCanceledException` để trả file về `Cho`.

Không cần migration: `TrangThaiLoKy` lưu dạng int, chỉ thêm giá trị vào cuối.

## Steps

1. **Shared** — `TrangThaiLoKy` thêm `TamDung = 5` **vào cuối**. Sửa XML doc của `Huy = 3` sang nghĩa mới
   (huỷ hẳn, không ký tiếp). Cấm chèn vào giữa: số đã nằm trong DB.
2. **Application / Runner** — `IKySoRunner.Dung(loKyId)` nhận thêm **ý định** (dừng hay huỷ).
   `KetThucLoAsync` chốt trạng thái cuối theo ý định đó thay vì chỉ theo `biHuy`.
3. **Application / Runner** — đảo điều kiện dọn nguồn trong `KetThucLoAsync`: **huỷ thì dọn**
   `lo-ky/{id}/nguon/`, dừng thì giữ. Sửa luôn comment tại chỗ, nó đang ghi ngược.
4. **Application / Service** — tách `DungAsync` khỏi `HuyAsync`. Cả hai gọi `_hangDoiKy.DongPhien(loKyId)`
   — hiện `HuyAsync` **không** gọi nên phiên và `X509Certificate2` rớt lại trong bộ nhớ tới lúc restart.
5. **Application / Service** — `LoDangChayAsync` nới bộ lọc để trả cả lô `TamDung`, không thì dừng xong mở
   lại màn là mất dấu lô dở.
6. **Application / Dto** — `ViewTienDoDto` thêm `CoTheTaiZip` do **BE tính**. Giữ `HoanTat = false` cho
   `TamDung`: cờ đó nghĩa là lô đã chốt, dùng cho lô còn ký tiếp được sẽ lẫn nghĩa.
7. **API** — `LoKyController` thêm `POST lo-ky/{id}/dung`.
8. **Tài liệu** — cập nhật [../../contracts/lo-ky.contract.md](../../contracts/lo-ky.contract.md) (route mới,
   trường mới, bảng trạng thái), [../../docs/luong-ky-so-hang-loat.md](../../docs/luong-ky-so-hang-loat.md) và
   [../../dang-lam.md](../../dang-lam.md) — cả ba đang mô tả `Huy` theo nghĩa cũ.
9. `dotnet build ksts.be/ksts.be.api/ksts.be.api.sln` sạch.

**Phát sinh giữa đường, không có trong thiết kế ban đầu:** `HangDoiKy.XinChuKyAsync` dựng hạn 120 giây bằng
**linked token** với token của lô, nên lô bị dừng cũng bật đúng token đó và callback ném `TimeoutException`
cho cả hai trường hợp ⇒ file đang chờ chữ ký luôn bị ghi **Lỗi** thay vì trả về `Cho`. Nghĩa là nhánh trả file
về hàng đợi ở `KyMotFileAsync` **chưa bao giờ chạy** cho đúng những file đang dở. Đã sửa: callback hỏi
`cancellationToken.IsCancellationRequested` để phân biệt lô bị dừng với lượt ký quá hạn.

## Expected output

- `POST lo-ky/{id}/dung` ⇒ lô về `TamDung`, mọi file `DangKy` về `Cho`, file nguồn còn nguyên.
- `POST lo-ky/{id}/bat-dau` trên lô `TamDung` ⇒ ký tiếp **từ file kế tiếp**, không ký lại file đã `Xong`.
- `GET lo-ky/dang-chay` thấy lô `TamDung`, không thấy lô `Huy`.
- `POST lo-ky/{id}/huy` ⇒ lô về `Huy`, bản đã ký trên kho **còn nguyên**, `lo-ky/{id}/nguon/` được dọn.
- `GET lo-ky/{id}/zip` chạy được với lô `TamDung`.

## Điểm cần chú ý

⚠️ **Runner ghi đè trạng thái.** `KetThucLoAsync` chạy **sau** khi service đã ghi DB. Không truyền ý định
xuống là nó ghi `TamDung` thành `Huy` — người dùng bấm Dừng lại thấy báo đã huỷ, hỏng lặng lẽ. Đây là chỗ dễ
sập nhất của cả tính năng.

⚠️ **Thứ tự đóng phiên.** `HangDoiKy.DongPhien` ném `InvalidOperationException` (**không phải**
`OperationCanceledException`) vào mọi lượt đang chờ ⇒ file rơi vào nhánh `Loi` thay vì `Cho` ⇒ đúng 8 file đó
không ký tiếp được. Phải chốt trạng thái lô **xong rồi mới** đóng phiên. Bug này hôm nay ẩn vì huỷ rồi thì
không ai ký tiếp.

⚠️ **Giữ nguyên chỗ nuốt `OperationCanceledException`** ở `ChayLoAsync`. Để nó thoát khỏi `Task.WhenAll` là lô
bị ghi **Lỗi** thay vì trạng thái dừng — bẫy này đã sập một lần, xem
[../../docs/luong-ky-so-hang-loat.md](../../docs/luong-ky-so-hang-loat.md).

- Luồng đang chờ chữ ký mất tới `GiaySongCuaYeuCau = 120` giây mới thoát, nên **Dừng và Huỷ không tức thì**.
  `DangChay(loKyId)` là mốc đáng tin để biết runner đã buông hẳn.
- Dữ liệu cũ mang `Huy` sẽ được đọc theo nghĩa mới. Vô hại về vận hành vì chúng vốn đã không hiện lên FE.
- **Không** đụng `IS3FileStorage` và **không** xoá gì trên kho: bản đã ký nằm ở thư mục dùng chung
  `GiayBaoTrungTuyen/{khoá}/GiayBaoTrungTuyenDaKySo/`, tên file là số CCCD nên hai lô ký cùng một thí sinh ghi
  trùng key — xoá theo lô là xoá mất bản của lô khác.
