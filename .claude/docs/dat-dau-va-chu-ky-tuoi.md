# Đặt con dấu và chữ ký tươi

> ⚠️ Cập nhật 2026-08-14: với giấy báo trúng tuyển, **con dấu đã được vẽ sẵn vào mẫu HTML ở khâu dựng giấy
> báo** nên luồng ký **không** đặt dấu. Phần dò mốc dưới đây hiện chỉ còn phục vụ **chữ ký tươi** và màn "vị
> trí gợi ý" của template. Vẫn giữ nguyên tài liệu vì thuật toán không đổi và tài liệu khác (không phải giấy
> báo) sẽ cần lại đường đặt dấu.

## Yêu cầu

Con dấu và chữ ký tươi phải nằm ở **trung điểm đoạn thẳng nối chức danh người ký với tên người ký** — trên
giấy báo trúng tuyển mẫu là từ **`PHÓ HIỆU TRƯỞNG`** xuống **`PGS.TS BÙI PHÚ DOANH`**.

```
                          KT. HIỆU TRƯỞNG
                          PHÓ HIỆU TRƯỞNG     ← mốc trên
                                 •
                                 •            ← TRUNG ĐIỂM: đặt dấu + chữ ký tươi
                                 •
                       PGS.TS BÙI PHÚ DOANH   ← mốc dưới
```

Đây đúng chỗ trống mà con dấu được đóng trên bản giấy thật.

## Vì sao BE tự dò chữ, không chốt cứng toạ độ

Ba cách từng cân nhắc:

| Cách | Vì sao chọn / loại |
|---|---|
| Chốt cứng tỉ lệ đo từ file mẫu | ❌ Lệch ngay khi tên người ký dài/ngắn khác hoặc thêm một dòng chức danh |
| FE gửi 2 mốc, BE tính trung điểm | ❌ Đẩy nghiệp vụ sang FE; mỗi client một kiểu dò là mỗi kiểu lệch |
| **BE tự dò text + toạ độ trong PDF** | ✅ Chạy đúng cho mọi giấy báo cùng mẫu dù tên người ký khác nhau |

Giấy báo được sinh hàng loạt từ một mẫu, nhưng **tên và chức danh người ký có thể đổi** (đổi phó hiệu trưởng
phụ trách). Dò theo chữ thì đổi người ký không phải sửa code.

## Cách tính

Thư viện: **PdfPig** (`UglyToad.PdfPig`) — đọc được text kèm bounding box, MIT, không phụ thuộc native.

1. Trích toàn bộ word kèm toạ độ của trang.
2. Gom word thành **dòng** theo baseline (word cùng baseline, sát nhau ⇒ một dòng).
3. Tìm **mốc trên**: dòng khớp một trong `SealPlacementConstants.AnchorChucDanh`
   (`PHÓ HIỆU TRƯỞNG`, `HIỆU TRƯỞNG`, `KT. HIỆU TRƯỞNG`…). So khớp **bỏ dấu, bỏ hoa/thường, gộp khoảng trắng**
   — PDF hay tách chữ có dấu thành nhiều glyph.
4. Tìm **mốc dưới**: dòng khớp `AnchorTenNguoiKy` (mặc định `PGS.TS BÙI PHÚ DOANH`). Không khớp thì lùi về
   quy tắc suy ra: **dòng kế tiếp bên dưới mốc trên có tâm ngang lệch không quá `AnchorAlignTolerance`**.
   Quy tắc lùi này giữ cho tính năng chạy khi đổi người ký mà chưa kịp sửa hằng số.
5. **Trung điểm** = trung điểm đoạn nối *tâm mốc trên* và *tâm mốc dưới*.
6. Trả về **tỉ lệ 0..1** so với khổ trang, gốc **trên-trái, Y hướng xuống** — cùng hệ quy chiếu với
   `TemplatePosition`, để template áp thẳng sang lúc ký không phải quy đổi.

Không tìm thấy mốc trên ⇒ ném `UserFriendlyException(ErrorCodes.SealAnchorNotFound)`. **Không** tự đoán một vị
trí mặc định: đóng dấu sai chỗ trên giấy báo trúng tuyển tệ hơn hẳn báo lỗi để người dùng tự đặt tay.

## Kích thước

### Con dấu — KHÔNG resize

Vẽ **đúng kích thước gốc của ảnh**: đọc số pixel + DPI trong metadata file ảnh rồi quy ra point.

```
points = pixels / dpi * 72
```

Thiếu metadata DPI ⇒ dùng `SealPlacementConstants.DefaultImageDpi` (96, mặc định của Windows).

Con dấu **không** nhân theo khổ trang như khối chữ ký số (`AppearanceReferencePageWidth/Height`), cũng **không**
co theo khung người dùng kéo. Dấu cơ quan có kích thước thật; phóng to thu nhỏ là làm sai con dấu.

> ⚠️ Hệ quả phải chấp nhận: ảnh scan sai DPI thì dấu ra sai cỡ. Cách chữa là scan lại đúng DPI, **không phải**
> cho phép resize.

### Chữ ký tươi — được co giãn

Chữ ký tay không có kích thước pháp lý cố định nên co giãn bình thường theo khung, giữ nguyên tỉ lệ khung
hình của ảnh để chữ không bị bóp méo.

## Vị trí tương đối và thứ tự vẽ

Cả hai khối **lấy trung điểm làm tâm**. Trên bản giấy thật con dấu được đóng **đè lên** chữ ký tay, nên thứ tự
vẽ là **chữ ký tươi trước, con dấu sau**.

Cả hai đều **tuỳ chọn**: template có thể chỉ có dấu, chỉ có chữ ký tươi, có cả hai, hoặc không có gì. Mọi
trường liên quan đều nullable.

## Kẹp trong trang

Con dấu giữ nguyên cỡ nên có thể tràn mép nếu mốc nằm sát đáy trang. Khi đó **dịch khối vào trong trang**,
tuyệt đối **không thu nhỏ** — dịch chỗ thì dấu vẫn đúng cỡ, thu nhỏ thì hỏng con dấu.

---

# Độ ĐẬM của ảnh dấu / chữ ký tươi

Template có `DoDamDauDo` và `DoDamChuKyTuoi` — phần trăm so với ảnh gốc, 100 là giữ nguyên, khoảng 40–250,
`DEFAULT 140` ở DB (migration `DoiMacDinhDoDamAnh` đổi từ 100 lên 140). FE là hai thanh trượt trong khối Tuỳ
chọn, kéo tới đâu bản xem trước đổi tới đó.

Áp bằng **mảng `/Decode` của chính PDF**, không xử lý từng pixel: không phải kéo thêm thư viện ảnh (ImageSharp
v3 có ràng buộc bản quyền), không tốn CPU, và **byte ảnh gốc giữ nguyên** nên không mất nét vì mã hoá lại.

Độ đậm gồm **HAI vế**, không chỉ tăng tương phản: `contrast(f)` với `f = phần trăm / 100`, **đồng thời**
`brightness(b)` với `b = max(0.5, 1 - (f-1) * 0.5)` — chỉ tăng tương phản thì nét tách khỏi nền rõ hơn nhưng
**không sâu thêm**. Hai vế đều tuyến tính nên gộp vào một mảng `/Decode [d0 d1]`: `d0 = b(1-f)/2`, `d1 = d0 + bf`.

Mặc định **140%** — ở mức đó công thức ra đúng `contrast(1.4) brightness(0.8)`, bộ số mà bản dựng thiết kế gốc
(`print_giaybaotrungtuyen.html`, rule `.signature-sign`) đã cân cho ảnh chữ ký quét.

Vế `saturate(1.25)` của bản gốc **không** biểu diễn được bằng `/Decode` — nó trộn giữa các kênh màu, còn
`/Decode` chỉ ánh xạ từng kênh độc lập — nên chấp nhận bỏ.

Hai chỗ **bắt buộc chừa ra**, sai là hỏng ảnh: lớp mặt nạ trong suốt `/SMask` (nó tả độ mờ chứ không tả màu,
kéo tương phản lên đó là ăn mòn viền chữ ký) và ảnh dùng bảng màu `/Indexed` (đặt sai số phần tử `/Decode` là
loạn màu). `DemThanhPhanMau` trả 0 cho trường hợp không chắc và khi đó không đụng vào ảnh.

⚠️ **Con dấu chưa áp được `DoDamDauDo`**: dấu được vẽ sẵn vào mẫu HTML ở khâu dựng giấy báo chứ không phải lúc
ký, nên muốn áp thì phải áp ở luồng dựng — chỗ khác hẳn.

# MÀU mực chữ ký tươi và màu khối chữ ký số

Template có `MauChuKySo` và `MauChuKyTuoi`, dạng `#RRGGBB` (migration `ThemKyDeVaMauChuKy`). Hai cột **khác
nghĩa nhau**: `MauChuKySo` luôn có giá trị (`DEFAULT '#000000'`) và đen ở đó là **chữ đen thật**; còn
`MauChuKyTuoi` **để trống là chưa chọn ⇒ giữ nguyên mực ảnh gốc**, `#000000` là **nhuộm đen thật**.

⚠️ Bẫy đã sập: ban đầu đen của `MauChuKyTuoi` gánh luôn nghĩa "giữ nguyên" ⇒ bảng màu luôn hiện đen dù mực ảnh
màu xanh, và chọn đen thì chữ ký vẫn xanh, không cách nào nhuộm sang đen. Đã tách bằng migration
`MauChuKyTuoiChoPhepTrong`: cột cho phép NULL, hàng cũ `#000000` chuyển sang NULL để giữ đúng nghĩa cũ. **Đừng
"sửa" bằng cách đặt lại mặc định cho cột** — làm vậy là mất hẳn trạng thái "chưa chọn".

**FE trích màu mực thật từ chính ảnh** (`InkColorService`) rồi hiện lên bảng màu làm giá trị khởi đầu: vẽ ảnh
ra canvas, bỏ điểm trong suốt và điểm sáng hơn 90% (giấy), lấy trung bình 10% điểm tối nhất — trung bình tất
cả sẽ kéo kết quả về phía giấy vì viền nét nhiều pixel hơn lõi nét. Đọc không được (kho object trả ảnh thiếu
tiêu đề CORS ⇒ canvas nhiễm, `getImageData` ném lỗi) thì bảng màu lùi về đen, không chặn màn hình.

- **Khối chữ ký số** chỉ là chữ vẽ ra nên đổi màu là đổi `XBrush` — không có gì phải bàn.
- **Ảnh chữ ký tươi** nhuộm bằng chính phép tuyến tính đang dùng cho độ đậm: điểm tối kéo về màu đã chọn, điểm
  sáng vẫn là trắng ⇒ `out = c + (1 - c) * in`. Nền trắng ảnh quét **không** bị nhuộm theo; chưa chọn màu thì
  không có `c` nào và công thức rút gọn về đúng phần độ đậm.

| Ảnh | Cách nhuộm |
|---|---|
| `/DeviceRGB` | `/Decode` **sáu số** — mỗi kênh một cặp cận-trần riêng |
| `/DeviceGray` | Đổi hẳn sang `/ColorSpace [/Indexed /DeviceRGB hival <bảng>]`; `/Decode` một kênh chỉ ra được sắc xám nên không đủ |
| `/DeviceCMYK` | **Không nhuộm**, chỉ giữ phần độ đậm |

⚠️ Ảnh thang xám đã đổi sang bảng màu thì **tuyệt đối không thêm `/Decode`** nữa: với `/Indexed`, `/Decode`
đánh vào *chỉ số ô màu* chứ không phải giá trị màu, đặt vào là loạn màu cả ảnh. Bảng màu đã gánh sẵn cả độ đậm.

⚠️ Không nhuộm CMYK vì ở đó giá trị 0 là **không mực** chứ không phải điểm tối — chạy cùng công thức sẽ ra ảnh
âm bản. Ảnh chữ ký quét gần như luôn là RGB hoặc thang xám nên đây là ngoại lệ hiếm, không phải lỗ hổng.

Bản xem trước FE nhuộm bằng bộ lọc SVG `feComponentTransfer type="linear"` — đúng cùng một phép tuyến tính, nên
kéo màu tới đâu thấy gần đúng bản in ra tới đó. CSS `filter` không có phép nào làm được việc này.

# Độ DÀY nét chữ ký tươi — vế thứ hai, khác hẳn độ đậm

Template có `DoDayNetChuKyTuoi` — phần trăm, khoảng 0–200, `DEFAULT 100` (migration `ThemDoDayNetChuKyTuoi`).
Thanh trượt thứ ba trong khối Tuỳ chọn, chỉ hiện khi template có ảnh chữ ký tươi.

**Vì sao cần thanh riêng**: `/Decode` ánh xạ lại độ sáng từng pixel **tại chỗ** nên nét mảnh chỉ sẫm màu hơn
chứ không dày thêm — kéo hết thanh Độ đậm vẫn không ra nét chắc như bản giấy. Muốn nét DÀY thì mực phải lan
sang pixel bên cạnh, việc `/Decode` về nguyên lý không làm được.

Bản dựng thiết kế gốc làm bằng **hai lớp `drop-shadow(0 0 0.35px #1e3a8a)`** chồng lên nhau. Tái hiện trong
PDF: **vẽ lại chính ảnh đó 8 lần lệch quanh tâm** rồi vẽ lớp lõi sau cùng (`IPdfAppearanceBuilder.TinhLopNongNet`).
Lõi thu vào đúng bằng bán kính nong để vòng lệch không tràn khỏi BBox, và xếp cuối để nét gốc nằm trên.

| Chốt | Số |
|---|---|
| Bán kính ở 100% | `0.35 / 158` **bề rộng ảnh** — theo tỉ lệ, không phải số điểm cứng, để ô to ô nhỏ đều dày tương đương |
| Số hướng | 8 — bốn hướng để lại vết chữ thập ở nét chéo |
| Đo thật, khổ 118,5pt | 9 lớp vẽ, **vẫn đúng 2 ảnh nhúng** (ảnh + `/SMask`), tổng byte object **không đổi** ở cả 0/50/100/200% |

Không phình file vì mọi lệnh vẽ trỏ về **cùng một XObject** — điều kiện là dùng **một instance `XImage` duy
nhất** cho cả 8 lớp; dựng `XImage` mới cho từng lớp là nhân byte ảnh lên 9 lần.

Bản xem trước FE dùng đúng hai lớp `drop-shadow` như bản gốc. CSS bắt buộc nêu màu, nên quầng lấy màu đã chọn,
không có thì lấy **màu mực trích từ chính ảnh**, đọc không được mới lùi về `#1e3a8a` của bản dựng gốc — chỉ
trong nước cuối đó bản xem trước mới ngả xanh so với bản in, vì BE vẽ chồng chính ảnh nên giữ đúng mực thật.
