# PDF/A hai lớp — định dạng tệp nội dung trong gói

> **Phần 5/5** · trước: [tt05-chuan-dip.md](tt05-chuan-dip.md) · mục lục: [tt05-thong-tu.md](tt05-thong-tu.md)
>
> PDF/A hai lớp là gì, thông tư đòi tới đâu, gói mẫu làm ra sao — và vì sao trong luồng đóng gói của dự án
> này nó là **tuỳ chọn** chứ không phải cổng chặn. Cập nhật 2026-09-08.

> 🎯 **Chốt: sinh PDF/A là TUỲ CHỌN.** Gói vẫn hợp lệ khi tệp nội dung không phải PDF/A — xem
> [§3](#3-vì-sao-là-tuỳ-chọn). Bật hay không quyết định theo nguồn tài liệu ([§4](#4-khi-nào-nên-bật)),
> không bật mặc định.

## 1. Hai lớp là hai lớp gì

PDF/A hai lớp = **ảnh bản quét** ở dưới + **lớp chữ vô hình** đè đúng vị trí từng chữ ở trên. Người đọc thấy
đúng bản gốc; máy bôi đen, tìm kiếm, copy được vì lớp chữ nằm sẵn ở đó. Lớp chữ do OCR sinh ra, vẽ bằng chế độ
tô chữ số **3** (`3 Tr` — vẽ nhưng không hiện).

Phần "PDF/A" (ISO 19005) là ràng buộc **tự chứa** để mở được sau hàng chục năm:

| Ràng buộc | Vì sao |
|---|---|
| Nhúng toàn bộ font | Máy đọc sau này không còn font đó thì chữ vẫn dựng đúng |
| Có `/OutputIntent` kèm hồ sơ màu ICC | Màu tái hiện đúng, không phụ thuộc thiết bị |
| Không mã hoá, không JavaScript, không liên kết tệp ngoài | Mở được mà không cần khoá hay tài nguyên bên ngoài |

Đây là hai thứ **độc lập**: một file có thể là PDF/A mà không có lớp text (bản gốc điện tử), hoặc có lớp text
mà không đạt PDF/A (thiếu font nhúng). Thông tư đòi cả hai cùng lúc, nên gọi là "PDF/A hai lớp".

## 2. Thông tư đòi tới đâu

**Điều 8** đặt PDF/A hai lớp cho tài liệu **số hóa từ bản giấy** — xem bảng thể thức ở
[tt05-thong-tu.md §4](tt05-thong-tu.md). Ràng buộc gắn vào **khâu số hóa**, không gắn vào khâu đóng gói.

Tài liệu **gốc điện tử** (`confidenceLevel = 01`) không đi qua khâu số hóa nên không rơi vào diện Điều 8.

## 3. Vì sao là tuỳ chọn

Ba căn cứ, xếp theo sức nặng:

1. **Bảng mimetype của chính phụ lục nhận nhiều định dạng khác.** Nhóm `DOC` ở
   [tt05-chuan-sip.md §8](tt05-chuan-sip.md) liệt kê `.txt`, `.rtf`, `.doc`, `.docx`, `.odt` **bên cạnh**
   `.pdf/a`. Nếu PDF/A là điều kiện sống còn của gói thì bảng đó đã chỉ có đúng một dòng. Gói chứa `.docx`
   vẫn là gói hợp lệ về cấu trúc.
2. **Điều 8 phủ khâu số hóa, không phủ khâu đóng gói** — xem §2. Gói dựng từ file gốc điện tử không phải
   chứng minh gì với Điều 8.
3. **Nguồn file của `kssm.be` không phải ảnh scan thô.** File vào đây là PDF đã có sẵn — từ MinIO của dự án
   hoặc từ máy người dùng. Dựng lớp text bằng OCR là một khâu khác hẳn, tốn kém, và phần lớn trường hợp
   không cần.

Nói ngược lại cho rõ: **tuỳ chọn không có nghĩa là không quan trọng.** Với tài liệu số hóa từ giấy thì đây là
yêu cầu bắt buộc của Điều 8 — chỗ tuỳ chọn nằm ở việc *luồng đóng gói không tự ý chuyển đổi*, chứ không phải
ở việc được phép nộp bừa.

## 4. Khi nào nên bật

| Nguồn tài liệu | `confidenceLevel` | PDF/A |
|---|---|---|
| Ảnh quét từ bản giấy | `02` Số hóa | **Bật** — đúng diện Điều 8, và cần lớp text để tìm kiếm |
| File gốc điện tử, đã là PDF văn bản | `01` Gốc điện tử | Không cần — Điều 8 không phủ tới, chữ đã có sẵn |
| Hồ sơ trộn cả hai | `03` Hỗn hợp | Bật cho riêng phần số hóa |

## 5. Gói mẫu làm thế nào — số đo thật

Đo trên 137 file `representations/rep1/data/*.pdf` của gói mẫu
`b6acafed-a809-4b78-ae9f-0373d50d52f0` ngày 2026-09-08, bằng cách đọc XMP trong file thô và giải nén
content stream bằng zlib:

| Phép đo | Kết quả |
|---|---|
| Header | `%PDF-1.7` |
| `pdfaid:part` / `pdfaid:conformance` trong XMP | `2` / `B` → **PDF/A-2b**, 137/137 |
| `/OutputIntent` | có, 137/137 |
| `/Font` | có, 137/137 |
| Lớp chữ vô hình | content stream giải nén có `3 Tr` đè trên ảnh — đúng cấu trúc hai lớp |
| `xmp:CreatorTool` | `OCRmyPDF 16.10.4 / Tesseract OCR-hOCR 5.5.0.20241111` |

Hai điều rút ra:

- **Mức tuân thủ là PDF/A-2b.** Thông tư chỉ viết "PDF/A hai lớp", không nói part nào, conformance nào. Theo
  luật "gói mẫu thắng thông tư" thì lấy **2b** làm đích khi bật tuỳ chọn.
- **Gói mẫu dựng lớp text bằng OCRmyPDF + Tesseract**, không phải bằng thư viện PDF thông thường. Đây là
  công cụ ngoài, không phải một lời gọi hàm.

## 6. Bẫy phải biết trước khi bật

⚠️ **Chuyển PDF/A sau khi ký là mất chữ ký.** Chữ ký số phủ một dải byte cố định của file (`/ByteRange`); mọi
công cụ chuyển PDF/A đều ghi lại toàn bộ file nên dải byte đó không còn trỏ đúng vào đâu nữa — chữ ký thành
vô hiệu, và dấu thời gian TSA đã đóng cũng mất theo. Thứ tự bắt buộc là **chuyển PDF/A trước, ký sau** (ký
bằng incremental update thì bản PDF/A vẫn giữ nguyên tuân thủ). **Đừng** xếp khâu chuyển đổi xuống sau khâu
ký để tiện tái dùng file đã ký.

⚠️ **`kssm.be` hiện chưa có công cụ nào sinh được PDF/A.** Thư viện PDF duy nhất khai trong `*.csproj` của
`kssm.be` là **PdfSharp** — nó đọc và ghi PDF, nhưng không chuẩn hoá sang PDF/A: không nhúng `/OutputIntent`,
không ép nhúng font, không dựng được lớp text. Gotenberg chỉ có ở `ksts.be` (luồng dựng giấy báo),
`kssm.be` không gọi tới. Bật tuỳ chọn này nghĩa là **thêm phụ thuộc ngoài vào ảnh Docker** — Ghostscript, hoặc
OCRmyPDF + Tesseract kèm dữ liệu ngôn ngữ tiếng Việt như gói mẫu đã dùng. Đó chính là lý do nó nằm sau một cờ
chứ không bật sẵn.

⚠️ **Đừng suy `Deleted`/`confidenceLevel` ra định dạng.** `confidenceLevel = 02` nói tài liệu **được số hóa**,
không bảo đảm tệp đang cầm đã là PDF/A. Muốn biết thì đọc `pdfaid:part` trong XMP của chính file, như bảng §5.

## 7. Nếu bật thì phải đạt

Đích lấy theo gói mẫu, kiểm được bằng đúng các phép đo ở §5:

1. PDF/A-2b — `pdfaid:part = 2`, `pdfaid:conformance = B`.
2. Nhúng toàn bộ font, kể cả font của lớp text OCR.
3. Có `/OutputIntent` kèm hồ sơ màu ICC.
4. Lớp chữ vô hình (`3 Tr`) đè đúng vị trí chữ trên ảnh — sai vị trí thì bôi đen và tìm kiếm lệch.
5. Không mã hoá, không JavaScript, không liên kết tệp ngoài.
6. Ảnh nền giữ đúng thể thức Điều 8: ≥24 bit màu, **200 dpi** hành chính / **300 dpi** bản đồ, bản vẽ.
