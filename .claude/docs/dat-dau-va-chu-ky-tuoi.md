# Đặt con dấu và chữ ký tươi

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
