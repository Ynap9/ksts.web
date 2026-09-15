# Thông tư 05/2025/TT-BNV — nghiệp vụ lưu trữ tài liệu lưu trữ số

> Luật gốc của mọi việc đóng gói SIP/AIP/DIP. Tổng hợp từ bản PDF chính thức trên `moha.gov.vn` (364 trang);
> thân thông tư Điều 1–52 nằm ở trang 1–32. File này là **mục lục** của bộ năm tài liệu. Cập nhật 2026-09-08.

**Bộ tài liệu:** file này · [tt05-chuan-sip.md](tt05-chuan-sip.md) · [tt05-chuan-aip.md](tt05-chuan-aip.md) ·
[tt05-chuan-dip.md](tt05-chuan-dip.md) · [tt05-pdfa.md](tt05-pdfa.md)

> 🎯 **Khi thông tư và gói mẫu lệch nhau thì theo gói mẫu.** Gói `b6acafed-a809-4b78-ae9f-0373d50d52f0` (loại
> `SIP_hoso`, 137 tài liệu) là bản chuẩn để học theo; bảng đối chiếu 15 điểm lệch và 3 lỗi của chính gói mẫu
> nằm ở [tt05-chuan-sip.md §10–§11](tt05-chuan-sip.md).

## 1. Nhận dạng văn bản

| | |
|---|---|
| Số hiệu | **05/2025/TT-BNV**, Bộ Nội vụ, Thứ trưởng Cao Huy ký |
| Căn cứ | Luật Lưu trữ ngày 21/6/2024 · Nghị định 25/2025/NĐ-CP |
| Hiệu lực | **01/7/2025** |
| Bãi bỏ | **Thông tư 02/2019/TT-BNV** (tiêu chuẩn dữ liệu đầu vào tài liệu lưu trữ điện tử) |
| Chuyển tiếp | Dữ liệu đã hình thành theo 02/2019 **vẫn được lưu trữ cho tới khi chuyển đổi**; hệ thống cũ phải nâng cấp (Điều 51) |

Phạm vi (Điều 1): thể thức + quy trình số hóa · chuyển đổi tài liệu số sang giấy · thu nộp, bảo quản, sử dụng
và hủy tài liệu lưu trữ số hết giá trị.

Đối tượng áp dụng (Điều 2): cơ quan nhà nước, đơn vị sự nghiệp công lập, doanh nghiệp nhà nước; tổ chức/cá nhân
sử dụng tài liệu lưu trữ số và làm dịch vụ lưu trữ. Tài liệu lưu trữ tư thì **tự quyết định** có áp dụng hay không.

## 2. Ba loại gói tin (Điều 3)

| Viết tắt | Tên tiếng Việt | Là gì |
|---|---|---|
| **SIP** | Gói hồ sơ, tài liệu **nộp** | Chuẩn bị tại lưu trữ hiện hành để nộp vào lưu trữ lịch sử, hoặc chuyển giao **giữa các hệ thống** |
| **AIP** | Gói hồ sơ, tài liệu **lưu trữ** | Gói được **bảo quản trong** hệ thống, ở lưu trữ hiện hành hoặc lưu trữ lịch sử |
| **DIP** | Gói tài liệu lưu trữ **sử dụng** | Bản giao cho **người dùng** |

Cả ba đều mở rộng từ **E-ARK (CSIP) v2.0.4**, tồn tại dưới dạng cây thư mục và được **nén ZIP** khi truyền nhận.

`Đối tượng thông tin` = phông/công trình/sưu tập lưu trữ · hồ sơ · văn bản, tài liệu · tài liệu ảnh (dương bản) ·
ghi âm, ghi hình (phim âm bản) · video.

## 3. Chọn gói nào — bảng quyết định của Điều 17

Đây là **hai câu hỏi độc lập**, không phải một trục "loại gói":

| | Thu nộp **cùng** Hệ thống | Thu nộp **khác** Hệ thống |
|---|---|---|
| Đã lập **hồ sơ** | `AIP_hoso` — Phụ lục I | `SIP_hoso` — Phụ lục III |
| Chỉ có **tài liệu rời lẻ** | `AIP_tailieu` — Phụ lục II | `SIP_tailieu` — Phụ lục IV |

Vì sao cùng Hệ thống lại ra AIP: thu nộp nội bộ thì gói **không phải nhập lại**, nó đã ở đúng hình dạng bảo quản
nên là AIP luôn. Thu nộp khác Hệ thống thì ra SIP, và lưu trữ lịch sử **chuyển SIP thành AIP** sau khi phê duyệt
(Điều 25.3.a). Bước chuyển đó được ghi thành một `event` PREMIS với `eventType = "information package creation"`
— xem [tt05-chuan-aip.md](tt05-chuan-aip.md).

Nhánh "tài liệu rời lẻ" chỉ được dùng khi cơ quan **chưa lập hồ sơ** trong quá trình giải quyết công việc **và**
Hệ thống có tìm kiếm thông minh, liên kết được tài liệu rời lẻ theo chủ đề/quá trình xử lý (Điều 17.2).

## 4. Thể thức tài liệu số hóa (Điều 8) — một file hợp lệ trông thế nào

| Nguồn gốc | Định dạng | Màu sắc / độ phân giải | Vị trí chữ ký số |
|---|---|---|---|
| Giấy | **PDF/A hai lớp** | ảnh màu theo màu tài liệu, độ sâu màu ≥24 bit; **200 dpi** hành chính, **300 dpi** bản đồ/bản vẽ | góc trên bên phải, **trang đầu** |
| Ảnh dương bản, phim âm bản | `.JPEG` · `.PDF` · `.TIFF` · `.PNG` | theo màu gốc, ≥200 dpi | góc trên bên phải tệp tin |
| Ghi hình (video) | MPEG-4 · `.AVI` · `.WMA`; `.WAV` không nén | theo màu gốc, **bit rate ≥1500 kbps** | theo khoản 6 Điều 36 Luật Lưu trữ |
| Ghi âm | `.MP3` · `.wma` | — | bit rate **≥128 kbps** |

Yêu cầu chung (Điều 8.1): tỷ lệ số hóa **100%**; rõ ràng, trung thực với bản gốc; chữ ký số của cơ quan hiển thị
**tên cơ quan + thời gian ký** (ngày tháng năm, giờ phút giây, múi giờ Việt Nam theo ISO 8601), trình bày bằng
**Times New Roman, in thường, đứng, cỡ 10, màu đen**, và **không hiển thị hình ảnh dấu** của cơ quan.

⚠️ Ràng buộc này khớp đúng với cách KSTS/kssm vẽ khối chữ ký số — cỡ 10, đen, không kèm dấu đỏ. Dấu đỏ và chữ ký
tươi là **tùy chọn nghiệp vụ riêng**, không phải thứ TT05 đòi trên bản số hóa.

Ràng buộc **PDF/A hai lớp** ở dòng đầu bảng gắn vào **khâu số hóa từ bản giấy**, không gắn vào khâu đóng gói:
gói vẫn hợp lệ khi tệp văn bản là `.docx`, `.odt`… Trong luồng đóng gói của dự án, sinh PDF/A là **tuỳ chọn** —
hai lớp là gì, mức PDF/A-2b đo từ gói mẫu, bẫy thứ tự với ký số: [tt05-pdfa.md](tt05-pdfa.md).

**Tên tệp tin** (Điều 8.1.d): tối thiểu gồm `mã hồ sơ` + `.` + số thứ tự tài liệu trong hồ sơ. Nếu số hóa cả hồ
sơ thành một tệp thì tên tệp chính là `mã hồ sơ`. Đây đúng là hình dạng `docId` kiểu `H49.64.33.2001.160.1`.

Tài liệu số hóa xong được đóng theo cấu trúc **AIP_hoso hoặc AIP_tailieu** (Điều 8.3) — **không phải SIP**.

## 5. Các chương và nội dung cốt lõi

| Chương | Điều | Nội dung |
|---|---|---|
| II — Số hóa | 5–12 | Lập kế hoạch; an toàn tài liệu và an toàn thông tin trong lúc số hóa; thể thức đầu ra (§4); quy trình riêng cho giấy, ảnh, phim âm bản, ghi âm/video |
| III — Số → giấy | 13–16 | Bản chuyển đổi phải ghi **"TÀI LIỆU LƯU TRỮ CHUYỂN ĐỔI"**, `Mã lưu trữ tài liệu gốc`, tên cơ quan chuyển đổi, chữ ký + dấu — đặt **sau phần nội dung ở trang cuối** (ảnh: mặt sau). Mẫu ở Phụ lục VI |
| IV — Thu nộp | 17–25 | Cấu trúc gói (§3) · trực tiếp hay trực tuyến · nộp vào lưu trữ hiện hành trong **60 ngày** kể từ khi kết thúc công việc · đăng ký nộp vào lưu trữ lịch sử **trước 12 tháng** · kiểm virus + tính xác thực · chuyển SIP thành AIP khi duyệt |
| V — Bảo quản | 26–31 | **Chỉ dùng AIP** (Điều 28). Sao lưu **≥2 bộ** trên phương tiện độc lập; **hằng ngày sao lưu gia tăng, hằng tháng sao lưu đầy đủ**; 3 năm/lần sao lưu đầy đủ; kiểm tra toàn bộ tài liệu trong vòng **3 năm**; chuyển đổi phương tiện lưu trữ **sớm hơn ít nhất 1 năm** so với hạn độ bền |
| VI — Sử dụng | 32–47 | Bản đọc · bản sao không xác thực · bản sao có xác thực · **bản sao có xác thực dạng DIP** (Điều 42) · cung cấp danh mục / trích xuất / tổng hợp thông tin |
| VII — Hủy | 48–50 | Hồ sơ trùng lặp và hết thời hạn được Hệ thống tự đưa vào Danh mục hết giá trị, chuyển trạng thái **"Xem xét hủy"**; Hội đồng xét hủy làm việc ngay trong Hệ thống; Hệ thống **lưu vết toàn bộ** quá trình hủy |

Mốc thời gian hay bị hỏi: lưu trữ hiện hành xác nhận hồ sơ nộp **≤60 ngày**; lưu trữ lịch sử trả lời đăng ký nộp
**≤07 ngày làm việc**; xử lý nghiệp vụ và ra quyết định phê duyệt **≤60 ngày**; tiếp nhận và xét duyệt yêu cầu
cung cấp thông tin **≤06 ngày làm việc**; cấp tài khoản người dùng **≤01 ngày làm việc**.

## 6. Các loại bản dành cho người dùng (Điều 37, 40–42)

Bốn mức, khác nhau ở chỗ **có kiểm được yếu tố xác thực không** và **có tải về được không**:

| Loại | Ký số của cơ quan | Tải về | Đặc điểm trình bày |
|---|---|---|---|
| **Bản đọc trên Hệ thống** (Điều 37) | không | **không cho tải** | tên cơ quan ở **lề dưới, chính giữa mọi trang**; Hệ thống tự xóa sau 05 ngày kể từ khi hết hạn đọc (hạn đọc ≤15 ngày) |
| **Bản sao không xác thực** (Điều 40) | không | có, trong 15 ngày | như trên, thêm chữ **"BẢN SAO"** góc trên bên phải trang đầu (Times New Roman, in thường, đứng, cỡ 10, đen). Không áp dụng chữ "BẢN SAO" cho ghi âm/ghi hình |
| **Bản sao có xác thực** (Điều 41) | **có** | có | thể thức như trên; chữ ký số **thể hiện bằng chính chữ "BẢN SAO"** |
| **Bản sao có xác thực dạng DIP** (Điều 42) | **có, giữ nguyên yếu tố xác thực của bản gốc** | có | đóng theo cấu trúc **gói DIP** (Phụ lục V), kèm tệp văn bản xác thực `.pdf/a` |

Tệp văn bản xác thực trong DIP gồm 11 thông tin — liệt kê ở [tt05-chuan-dip.md](tt05-chuan-dip.md).

## 7. Bản đồ phụ lục

| Phụ lục | Trang | Nội dung | Đọc ở |
|---|---|---|---|
| I | 33–109 | Cấu trúc `AIP_hoso` | [tt05-chuan-aip.md](tt05-chuan-aip.md) |
| II | 110–181 | Cấu trúc `AIP_tailieu` | [tt05-chuan-aip.md](tt05-chuan-aip.md) |
| III | 182–238 | Cấu trúc `SIP_hoso` | [tt05-chuan-sip.md](tt05-chuan-sip.md) |
| IV | 239–291 | Cấu trúc `SIP_tailieu` | [tt05-chuan-sip.md](tt05-chuan-sip.md) |
| V | 292–357 | Cấu trúc `DIP` | [tt05-chuan-dip.md](tt05-chuan-dip.md) |
| VI | 358–359 | Mẫu trình bày khi chuyển tài liệu số sang giấy | — |
| VII | 360–364 | Mẫu biên bản kiểm tra · nhật ký sao lưu · biên bản sao lưu · biên bản xử lý sự cố và phục hồi | — |

## 8. Ba trục khác nhau giữa SIP, AIP và DIP

Đọc ba tài liệu sau sẽ thấy chúng chỉ khác nhau ở đúng ba chỗ; phần còn lại (cây thư mục, bộ phần tử METS,
metadata từng tài liệu, schema, mimetype) gần như trùng nhau:

| Trục | SIP | AIP | DIP |
|---|---|---|---|
| **Siêu dữ liệu bảo quản** | **không có** PREMIS (`amdSec` vẫn phát nhưng **rỗng** — theo gói mẫu) | `PREMIS.xml` ở gốc + `PREMIS_rep1.xml` ở rep1, **bắt buộc**; `amdSec` có nội dung | có PREMIS nhưng **không bắt buộc** |
| **Mã định danh nghiệp vụ** | `fileCode` / `docCode` | `arcFileCode` / `arcDocCode` — **cộng thêm mã cơ quan lưu trữ** ở đầu | `arcDocCode` (dùng lại của AIP) |
| **Metadata cấp gói mô tả cái gì** | hồ sơ (18 trường) hoặc lô tài liệu nộp (5 trường) | hồ sơ lưu trữ (18 trường) hoặc gói tài liệu lưu trữ (4 trường) | **yêu cầu khai thác của người dùng** (7 trường) — không mô tả tài liệu |

> **Tiếp:** [tt05-chuan-sip.md](tt05-chuan-sip.md) — hai hình dạng gói nộp, thứ đang được thi công trong Sip.
