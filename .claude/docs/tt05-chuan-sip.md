# Chuẩn SIP — gói hồ sơ, tài liệu nộp (Phụ lục III & IV)

> **Phần 2/5** · trước: [tt05-thong-tu.md](tt05-thong-tu.md) · mục lục: [tt05-thong-tu.md](tt05-thong-tu.md)
>
> Hai hình dạng gói khi thu nộp **khác Hệ thống**. `SIP_hoso` = Phụ lục III (trang 182–238), `SIP_tailieu` =
> Phụ lục IV (trang 239–291). Cập nhật 2026-09-08.

> 🎯 **Gói mẫu là hàng chuẩn.** `b6acafed-a809-4b78-ae9f-0373d50d52f0.zip` → thư mục
> `uuid-0F203B84-6641-4CDA-9F52-DAAE55FEF6B1` (137 tài liệu, do `ArchiveNex` sinh 12/6/2026) là **`SIP_hoso`**
> và là bản mẫu để học theo. **Chỗ nào gói mẫu khác câu chữ thông tư thì làm theo gói mẫu** — trừ ba lỗi thật
> đã khoanh ở [§11](#11-ba-lỗi-của-gói-mẫu-đừng-học-theo). Đối chiếu đầy đủ ở [§10](#10-gói-mẫu-chuẩn--những-gì-chốt-theo-gói-mẫu).

## 1. Chọn `SIP_hoso` hay `SIP_tailieu`

Cả hai đều là SIP, khác nhau ở **gói nói về cái gì**, quyết định bởi Điều 17:

| | `SIP_hoso` (PL III) | `SIP_tailieu` (PL IV) |
|---|---|---|
| Đơn vị đóng gói | Một **hồ sơ** cùng các tài liệu trong nó | Một **lô tài liệu rời lẻ** |
| Điều kiện dùng | Cơ quan đã lập hồ sơ | Chưa lập hồ sơ; Hệ thống có tìm kiếm thông minh |
| `EAD.xml` cấp gói | **18 trường** — mô tả hồ sơ đầy đủ | **5 trường** — chỉ là đầu lô |
| Tài liệu liên kết `.fetch.txt` | Có quy định | **Không quy định** (xem ⚠️ §6) |
| `mets/@TYPE` | `Mixed` | `Collection` |

Phần còn lại — cây thư mục, bộ phần tử METS, metadata từng tài liệu, schema, mimetype, quy tắc ZIP — **giống nhau**.

## 2. Cấu trúc vật lý (chung cho cả hai)

```text
<OBJID>/                                  tên thư mục = mets/@OBJID, vd uuid-7D0D1987-0F1C-...
├── METS.xml                              bắt buộc, 01 — mô tả cấu trúc gói
├── metadata/                             bắt buộc, 01
│   └── descriptive/                      bắt buộc, 01
│       └── EAD.xml                       bắt buộc, 01 — mô tả thông tin chung của gói
├── representations/                      bắt buộc, 01
│   └── rep1/                             bắt buộc, 01 — đúng một bản đại diện
│       ├── METS.xml                      bắt buộc, 01
│       ├── metadata/descriptive/
│       │   ├── EAD_doc_<tên>.xml         ≥01 — mỗi tài liệu một file
│       │   ├── EAD_pic_<tên>.xml         quy tắc tên: [Tiêu chuẩn]_[Loại tài liệu]_[Tên file]
│       │   └── EAD_media_<tên>.xml
│       └── data/                         bắt buộc, 01
│           ├── File1.pdf …               ≥01 — văn bản: PDF/A hai lớp (tuỳ chọn, xem tt05-pdfa.md)
│           └── File2.doc.fetch.txt       chỉ SIP_hoso — trỏ tới tài liệu đã nộp lần trước
├── schemas/                              bắt buộc, 01
│   ├── METS.xsd                          bắt buộc, 01
│   └── EAD.xsd · EAD_doc.xsd · EAD_media.xsd · EAD_pic.xsd     ≥01 file
└── documentation/                        KHÔNG bắt buộc
    └── UserManual.pdf                    KHÔNG bắt buộc
```

Ánh xạ schema theo loại đối tượng: gói/hồ sơ → `EAD.xsd`; văn bản → `EAD_doc.xsd`; video, âm thanh →
`EAD_media.xsd`; phim âm bản, ảnh → `EAD_pic.xsd`.

**SIP không có `metadata/preservation/` và không có `PREMIS.xml`.** Siêu dữ liệu bảo quản là thứ lưu trữ lịch
sử thêm vào lúc chuyển SIP → AIP.

Gói mẫu **không có** thư mục `documentation/` (đúng, vì nó không bắt buộc) và có đủ 8 file trong `schemas/`:
`mets1_12.xsd` · `xlink.xsd` · `DILCISExtensionMETS.xsd` · `DILCISExtensionSIPMETS.xsd` · `ead.xsd` ·
`ead_doc.xsd` · `ead_pic.xsd` · `ead_media.xsd`. Tức **nộp cả bốn `ead*.xsd` kể cả khi gói chỉ có văn bản**,
và tên file schema viết **thường** (`ead.xsd`, không phải `EAD.xsd` như bảng thông tư).

## 3. METS

Thông tư mô tả năm phần tử: `mets` → `metsHdr` → `dmdSec` → `fileSec` → `structMap`.

⚠️ **Gói mẫu phát thêm `<amdSec ID="uuid-…"/>` rỗng**, ở **cả METS gốc lẫn METS của rep1**, đặt giữa `dmdSec`
và `fileSec`. Thông tư không kể `amdSec` trong bộ phần tử của SIP, nhưng đây là chỗ chốt theo gói mẫu: **sinh
một `amdSec` rỗng có `@ID`**. Giữ nó rỗng — SIP không có PREMIS để trỏ tới.

```xml
<mets xmlns="http://www.loc.gov/METS/"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
      xmlns:xlink="http://www.w3.org/1999/xlink"
      xmlns:csip="https://DILCIS.eu/XML/METS/CSIPExtensionMETS"
      xmlns:sip="https://DILCIS.eu/XML/METS/SIPExtensionMETS"
      OBJID="uuid-7D0D1987-0F1C-47A7-8FD6-CC5C7DE4064F"
      LABEL="Tài liệu về hoạt động tổ chức cán bộ … năm 2020"
      TYPE="Collection" csip:CONTENTINFORMATIONTYPE="MIXED"
      PROFILE="https://earkcsip.dilcis.eu/profile/E-ARK-CSIP.xml"
      xsi:schemaLocation="http://www.loc.gov/METS/ schemas/mets1_12.xsd
                          http://www.w3.org/1999/xlink schemas/xlink.xsd
                          https://dilcis.eu/XML/METS/CSIPExtensionMETS schemas/DILCISExtensionMETS.xsd
                          https://dilcis.eu/XML/METS/SIPExtensionMETS schemas/DILCISExtensionSIPMETS.xsd">
```

| Thuộc tính `mets` | Giá trị cho SIP |
|---|---|
| `OBJID` | `uuid-{UUID}` — **không có tiền tố `urn:`**, UUID viết hoa. Tên thư mục gói lấy nguyên chuỗi này |
| `LABEL` | Mô tả nội dung gói (lấy từ `title` của metadata), không bắt buộc |
| `TYPE` | `Mixed` (hoso) / `Collection` (tailieu) |
| `PROFILE` | `https://earkcsip.dilcis.eu/profile/E-ARK-CSIP.xml` — **cả gốc lẫn rep1** |
| `csip:CONTENTINFORMATIONTYPE` | `MIXED`; không bắt buộc ở gốc nhưng **bắt buộc trong METS của rep1** |
| `xsi:schemaLocation` | `schemas/mets1_12.xsd` ở gốc; `../../schemas/mets1_12.xsd` trong rep1 |

**`metsHdr`** — `CREATEDATE` (bắt buộc), `LASTMODDATE`, `RECORDSTATUS` (`NEW` mặc định · `SUPPLEMENT` ·
`REPLACEMENT` · `TEST` · `OTHER`), **`csip:OAISPACKAGETYPE="SIP"`**, và:

| Phần tử | Gói gốc | Gói rep1 |
|---|---|---|
| `agent/@ROLE` | `CREATOR` | `CREATOR` |
| `agent/@TYPE` | `OTHER` kèm `@OTHERTYPE="SOFTWARE"` | `ORGANIZATION` |
| `agent/name` | tên phần mềm đóng gói, vd `VietNam Fonds Archival System` | tên cơ quan, vd `bnv` |
| `agent/note/@csip:NOTETYPE` | `SOFTWARE VERSION` | `IDENTIFICATIONCODE` → **mã phông**, vd `Phong_BNV` |
| `altRecordID/@TYPE` | `SUBMISSIONAGREEMENT`, giá trị là **mã đăng ký yêu cầu nộp** | — |

Gói mẫu làm khác ba chỗ trong bảng trên, và **đây là bản chốt theo**:

| Chỗ | Thông tư | **Gói mẫu — làm theo cái này** |
|---|---|---|
| `agent/note` ở **gốc** | `SOFTWARE VERSION` mang số phiên bản | `IDENTIFICATIONCODE` mang **mã phông** `P_KHCN`. Tức mã phông xuất hiện ở **cả hai** METS, còn số phiên bản phần mềm **không được ghi ở đâu cả** |
| `agent` ở **rep1** | `TYPE="ORGANIZATION"`, `name` = tên cơ quan | `TYPE="INDIVIDUAL" OTHERTYPE=""`, `name` = **tài khoản người đóng gói** (`cvlt`) |
| `altRecordID` | mã đăng ký yêu cầu nộp | **`fileCode` của hồ sơ** (`H49.64.33.2001.160`) |

Trích đúng từ gói mẫu:

```xml
<metsHdr CREATEDATE="2026-06-12T11:45:37.690+07:00" LASTMODDATE="2026-06-12T11:45:37.690+07:00"
         RECORDSTATUS="NEW" csip:OAISPACKAGETYPE="SIP">
    <agent ROLE="CREATOR" TYPE="OTHER" OTHERTYPE="SOFTWARE">
        <name>ArchiveNex</name>
        <note csip:NOTETYPE="IDENTIFICATIONCODE">P_KHCN</note>
    </agent>
    <altRecordID TYPE="SUBMISSIONAGREEMENT">H49.64.33.2001.160</altRecordID>
</metsHdr>
```

**`dmdSec`** trỏ tới các file trong `metadata/`: `@ID` = `uuid-{UUID}`, `@CREATED`, `@STATUS` (vd `CURRENT`), bên
trong là `mdRef` mang `@LOCTYPE="URL"`, `@MDTYPE`, `@MDTYPEVERSION`, `@xlink:href` (đường dẫn tính từ thư mục
gốc), `@MIMETYPE`, `@SIZE` (byte), `@CREATED`, `@CHECKSUMTYPE` (**SHA-256** mặc định), `@CHECKSUM`.

**`fileSec`** gom theo `fileGrp/@USE`: ở gốc là `Schemas`, `Representations/rep1`, tùy chọn `Documentation`;
trong rep1 là `Data` và — khi có tài liệu liên kết — **`Holeyfile`**. Mỗi `<file>` dùng `ID-{UUID}` (khác
`uuid-`), kèm `@MIMETYPE`, `@CREATED`, `@CHECKSUMTYPE`, `@CHECKSUM`, và `FLocat/@xlink:href` +
`@xlink:type="simple"` + `@LOCTYPE="URL"`.

Giá trị gói mẫu dùng, chốt theo:

| Thuộc tính | Giá trị trong gói mẫu | Ghi chú |
|---|---|---|
| `mdRef/@MDTYPE` | **`DC`** ở **cả** cấp gói lẫn cấp tài liệu | thông tư gợi ý `OTHER` cho cấp gói — gói mẫu dùng `DC` cho cả hai |
| `mdRef/@MDTYPEVERSION` | **`1.0.0`** | thông tư ghi mặc định `1.0` |
| `mdRef/@MIMETYPE` | **`application/xml`** | thông tư ví dụ `text/xml` |
| `file/@MIMETYPE` cho `.xsd` | **`application/octet-stream`** | bảng mimetype của thông tư không có mục nào cho `.xsd` |
| `file/@MIMETYPE` cho `METS.xml` của rep1 | `application/xml` | |
| `file/@MIMETYPE` cho tài liệu | `application/pdf` | |
| `@CHECKSUMTYPE` | `SHA-256`, giá trị băm **viết HOA** | |
| `@CREATED` | `2026-06-12T11:45:37.707+07:00` | có mili giây và **offset `+07:00`**, không dùng `Z` |

`fileGrp` trong gói mẫu: METS gốc có đúng hai nhóm `Schemas` (8 file) và `Representations/rep1` (1 file —
chính `rep1/METS.xml`); METS của rep1 có **đúng một** nhóm `Data` (137 file). Không có nhóm `Documentation`,
không có `Holeyfile` (gói này không dùng tài liệu liên kết).

**`structMap`** — `@LABEL="CSIP"`, `@TYPE="PHYSICAL"`:

| METS gốc | METS rep1 |
|---|---|
| `div[@LABEL="Metadata"]` → `@DMDID` trỏ `dmdSec` | `div[@LABEL="rep1"]` với `@TYPE="ORIGINAL"` |
| `div[@LABEL="Schemas"]/fptr@FILEID` → fileGrp `Schemas` | `div[@LABEL="Data"]/fptr@FILEID` → fileGrp `Data` |
| `div[@LABEL="Representations/rep1"]/mptr@xlink:href` → `representations/rep1/METS.xml`, `@xlink:title` = ID của fileGrp `Representations/rep1` | `div[@LABEL="MetadataLink"]/div[@LABEL="MetadataLink/File"]`: `@DMDID` = `dmdSec` của tài liệu, `fptr@FILEID` = file trong `data/` — đây là chỗ **nối metadata với tài liệu** |
| `div[@LABEL="Documentation"]/fptr` (tùy chọn) | `div[@LABEL="MetadataLink/Holey"]` cho tài liệu liên kết; `div[@LABEL="AttachmentFile"]` khi một tài liệu có file đính kèm |

Quy ước ID dùng chung: `uuid-{UUID}` ở mọi nơi, riêng `<file>` dùng `ID-{UUID}`; UUID **viết hoa**. Kiểu ngày giờ
`YYYY-MM-DDThh:mm:ss.sTZD`. Hàm băm mặc định **SHA-256**.

## 4. `EAD.xml` — metadata cấp gói

### 4.1. `SIP_hoso` — 18 trường, mô tả một hồ sơ

```xml
<simpledc>
  <fileCode/><title/><maintenance/><mode/><language/><startDate/><endDate/><keyword/>
  <totalDoc/><numberOfPaper/><numberOfPage/><format/><inforSign/><confidenceLevel/>
  <paperFileCode/><riskRecovery/><riskRecoveryStatus/><description/>
</simpledc>
```

| Trường | Tên tiếng Việt | Kiểu | Dài | Quy tắc |
|---|---|---|---|---|
| `fileCode` | Mã hồ sơ | String | 100 | Mã định danh cơ quan/tổ chức/cá nhân + năm hình thành hồ sơ + số và ký hiệu hồ sơ |
| `title` | Tiêu đề hồ sơ | String | 1000 | |
| `maintenance` | Thời hạn lưu trữ | String | 100 | `01`–`07`; **nguồn nộp và sưu tầm chỉ nhận `01` (vĩnh viễn)** |
| `mode` | Chế độ sử dụng | String | 30 | `01` công khai · `02` có điều kiện · `03` mật |
| `language` | Ngôn ngữ | String | 100 | `01`–`11`, **chọn một hoặc nhiều** |
| `startDate` / `endDate` | Thời gian bắt đầu / kết thúc | Date | | `DD/MM/YYYY` |
| `keyword` | Từ khóa (nếu có) | String | 100 | |
| `totalDoc` | Tổng số tài liệu trong hồ sơ | Number | 10 | văn bản + tài liệu kỹ thuật + âm bản/ảnh + ghi âm/phim |
| `numberOfPaper` | Số lượng tờ | Number | 10 | dành riêng tài liệu giấy số hóa — **xem ⚠️ dưới** |
| `numberOfPage` | Số lượng trang | Number | 10 | |
| `format` | Tình trạng vật lý (nếu có) | String | 50 | |
| `inforSign` | Ký hiệu thông tin (nếu có) | String | 30 | |
| `confidenceLevel` | Mức độ tin cậy (nếu có) | String | 40 | `01` gốc điện tử · `02` số hóa · `03` hỗn hợp |
| `paperFileCode` | Mã hồ sơ gốc giấy (nếu có) | String | 100 | `[Mã cơ quan lưu trữ].[Số kho/giá/hộp].[Số hồ sơ giấy]` — **bắt buộc với hồ sơ số hóa** |
| `riskRecovery` | Chế độ dự phòng | Boolean | 1 | `1` có · `0` không |
| `riskRecoveryStatus` | Tình trạng dự phòng | String | 2 | `01` đã dự phòng · `02` chưa — **bắt buộc khi `riskRecovery=1`** |
| `description` | Ghi chú | String | 2000 | tên người lập hồ sơ + nội dung cần làm rõ |

⚠️ **Bản thân TT05 tự mâu thuẫn ở đây, tới hai lần.** Khung XML trang 212 liệt kê **18** phần tử, có
`numberOfPaper`; nhưng bảng mô tả chi tiết trang 212–217 chỉ đánh số **17**, nhảy từ `totalDoc` (#9) sang
`numberOfPage` (#10). Tệ hơn, **`EAD.xsd` ở mục 4 của chính Phụ lục III chỉ khai 16 phần tử** — thiếu hẳn
`riskRecovery` và `riskRecoveryStatus` mà khung XML có. Gói mẫu có đủ **18** ở cả schema lẫn dữ liệu, nên
**theo gói mẫu: sinh đủ 18**. Đừng "sửa" bộ sinh cho khớp bảng hay cho khớp schema của thông tư.

### 4.2. `SIP_tailieu` — 5 trường, chỉ là đầu lô

| Trường | Tên tiếng Việt | Kiểu | Dài | Quy tắc |
|---|---|---|---|---|
| `fileCode` | Mã gói tin SIP_tailieu | String | 100 | Mã định danh cơ quan + năm hình thành tài liệu + **số lần nộp lưu (2 ký tự, `01`)** + **số thứ tự tài liệu trong lần nộp (7 ký tự, `0000001`)** |
| `title` | Tiêu đề gói tin | String | 1000 | tóm tắt nội dung và thời gian tài liệu trong gói |
| `source` | Nguồn gốc | Boolean | 1 | `0` = văn bản đi · `1` = văn bản đến |
| `totalDoc` | Tổng số tài liệu trong gói tin | Number | 10 | |
| `description` | Ghi chú | String | 2000 | |

`source` **chỉ có ở `SIP_tailieu`** — không loại gói nào khác trong TT05 có trường này. Đây là dấu hiệu nhận
dạng chắc chắn nhất khi phải đoán một gói lạ thuộc phụ lục nào.

## 5. Metadata từng tài liệu

Mỗi tài liệu một file XML trong `rep1/metadata/descriptive/`, tên theo `[Tiêu chuẩn]_[Loại tài liệu]_[Tên file]`.

### 5.1. `EAD_doc.xml` — tài liệu văn bản, 21 trường (cả hai biến thể SIP)

| Trường | Tên tiếng Việt | Kiểu | Dài | Quy tắc |
|---|---|---|---|---|
| `docId` | Mã định danh tài liệu | String | 25 | vd `H49.64.33.2001.160.1` |
| `docCode` | Mã lưu trữ của tài liệu | String | 100 | `fileCode` + số thứ tự tài liệu trong hồ sơ, **số thứ tự 7 ký tự** (`0000001`) |
| `maintenance` | Thời hạn lưu trữ | String | 100 | `01`–`07`; **lấy mặc định từ `maintenance` của hồ sơ** |
| `typeName` | Tên loại tài liệu | String | 10 | `01`–`32`, xem §7 |
| `codeNumber` | Số của tài liệu | String | 11 | nếu có |
| `codeNotation` | Ký hiệu của tài liệu | String | 30 | nếu có |
| `issuedDate` | Ngày tháng năm tài liệu | Date | | `DD/MM/YYYY` |
| `organName` | Cơ quan, tổ chức, cá nhân ban hành | String | 200 | |
| `subject` | Trích yếu nội dung | String | 500 | |
| `language` | Ngôn ngữ | String | 100 | `01`–`11`, một hoặc nhiều |
| `numberOfPage` | Số lượng trang | Number | 4 | |
| `inforSign` | Ký hiệu thông tin (nếu có) | String | 30 | |
| `keyword` | Từ khóa (nếu có) | String | 100 | ghi từ mang trọng tâm thông tin |
| `mode` | Chế độ sử dụng | String | 20 | `01`/`02`/`03` |
| `confidenceLevel` | Mức độ tin cậy (nếu có) | String | 30 | `01`/`02`/`03` |
| `autograph` | Bút tích (nếu có) | String | 2000 | |
| `format` | Tình trạng vật lý (nếu có) | String | 50 | |
| `riskRecovery` / `riskRecoveryStatus` | Dự phòng | Boolean / String | 1 / 2 | như §4.1 |
| `process` | Quy trình xử lý (nếu có) | Boolean | 1 | `1` = có file luồng xử lý đi kèm. **Bắt buộc với tài liệu điện tử xử lý trên Hệ thống**, áp dụng cho `confidenceLevel` `01` và `03` |
| `description` | Ghi chú | String | 500 | |

### 5.2. `EAD_pic.xml` — phim âm bản / ảnh

`docCode` · `maintenance` · `typePic` (`01` phim âm bản, `02` ảnh) · `archivesNumber` (Số lưu trữ đặc thù, 50) ·
`inforSign` · `eventName` (Tên sự kiện, 500) · `imageTitle` (Tiêu đề phim/ảnh, 500) · `photographer` (Tác giả,
300) · `photoPlace` (Địa điểm chụp, 300) · `photoTime` (Thời gian chụp, date) · `colour` (`01` màu, `02` đen
trắng) · `filmSize` (Cỡ phim/ảnh, 30) · `docAttached` (Tài liệu đi kèm, bool) · `mode` · `format` ·
`riskRecovery` · `riskRecoveryStatus` · `description`

### 5.3. `EAD_media.xml` — ghi âm / ghi hình

`docCode` · `maintenance` · `typeMedia` (`01` âm thanh, `02` video) · `archivesNumber` · `inforSign` ·
`eventName` · `movieTitle` (500) · `recorder` (Tác giả, 300) · `recordPlace` (300) · `recordDate` · `language` ·
`playTime` (Thời lượng, 8) · `docAttached` · `mode` · `quality` (Chất lượng, 50 — "bình thường, mờ, lẫn tạp âm,
tiếng lúc to lúc nhỏ") · `format` · `riskRecovery` · `riskRecoveryStatus` · `description`

Mọi schema đều lấy `<simpledc>` làm phần tử gốc (complexType `elementContainer`, group `elementsGroup`) và
import `xml.xsd`.

## 6. Tài liệu liên kết — holey file

Dùng khi nguồn nộp **đã nộp tài liệu đó ở lần trước** và không nộp lại nội dung file.

| | `SIP_hoso` (trang 211) |
|---|---|
| Đuôi file | `.fetch.txt`; tên đặt theo `Tên tài liệu liên kết.Định dạng tài liệu lưu trữ` → `File2.doc.fetch.txt` |
| Nội dung | chỉ `{Định danh tài liệu}` — văn bản → `Mã định danh tài liệu`; phim âm bản/ảnh → tiêu đề; phim/âm thanh → tiêu đề |
| Khai trong METS | `fileSec/fileGrp[@USE='Holeyfile']/file`, nối bằng `structMap/div/div/div[@LABEL="MetadataLink/Holey"]` |

⚠️ Hai chỗ chính bản TT05 tự lệch, **đừng lặng lẽ chuẩn hóa lại**: phụ lục viết `@USER='Holeyfile'` trong khi
mọi chỗ khác dùng `@USE`; và ví dụ `structMap` của `SIP_tailieu` (trang 267) đã dùng `LABEL="MetadataLink/Holey"`
mặc dù Phụ lục IV **không hề định nghĩa** cơ chế tài liệu liên kết.

## 7. Bảng mã dùng chung cho mọi loại gói

**`typeName`** — `01` Nghị quyết · `02` Quyết định · `03` Chỉ thị · `04` Quy chế · `05` Quy định · `06` Thông cáo ·
`07` Thông báo · `08` Hướng dẫn · `09` Chương trình · `10` Kế hoạch · `11` Phương án · `12` Đề án · `13` Dự án ·
`14` Báo cáo · `15` Tờ trình · `16` Giấy ủy quyền · `17` Phiếu gửi · `18` Phiếu chuyển · `19` Phiếu báo ·
`20` Biên bản · `21` Hợp đồng · `22` Công văn · `23` Công điện · `24` Bản ghi nhớ · `25` Bản thỏa thuận ·
`26` Giấy mời · `27` Giấy giới thiệu · `28` Giấy nghỉ phép · `29` Thư công · `30` Bản đồ · `31` Bản vẽ kỹ thuật ·
`32` Khác

**`language`** — `01` Tiếng Việt · `02` Anh · `03` Pháp · `04` Nga · `05` Trung · `06` Việt Anh · `07` Việt Nga ·
`08` Việt Pháp · `09` Hán Nôm · `10` Việt Trung · `11` Khác

**`maintenance`** — `01` Vĩnh viễn · `02` 70 năm · `03` 50 năm · `04` 30 năm · `05` 20 năm · `06` 10 năm · `07` Khác

**`mode`** — `01` Công khai · `02` Sử dụng có điều kiện · `03` Mật ·
**`confidenceLevel`** — `01` Gốc điện tử · `02` Số hóa · `03` Hỗn hợp ·
**`format`** (cách AIP diễn giải) — `01` Tốt · `02` Bình thường · `03` Hỏng ·
**`RECORDSTATUS`** — `NEW` · `SUPPLEMENT` · `REPLACEMENT` · `TEST` · `OTHER` ·
**`csip:OAISPACKAGETYPE`** — `SIP` · `AIP` · `DIP`

## 8. Mimetype và định dạng

| Code | Loại | Extension → mimetype |
|---|---|---|
| DOC | Văn bản | `.txt`→`text/plain` · `.rtf` (1.8/1.9.1)→`application/rtf` · `.docx`→`…wordprocessingml.document` · **`.pdf/a` hai lớp**→`application/pdf` · `.doc`→`application/msword` · `.odt` (1.2)→`…opendocument.text` |
| OTHER | Bảng tính | `.csv`→`text/csv` · `.xlsx`→`…spreadsheetml.sheet` · `.xls`→`application/vnd.ms-excel` · `.ods`→`…opendocument.spreadsheet` |
| OTHER | Trình chiếu | `.htm`→`text/html` · `.pptx`→`…presentationml.presentation` · `.ppt`→`application/vnd.ms-powerpoint` · `.odp`→`…opendocument.presentation` |
| PIC | Hình ảnh | `.jpeg`/`.jpg`→`image/jpeg` · `.gif` (89a)→`image/gif` · `.tif`/`.tiff`→`image/tiff` · `.png`→`image/png` |
| MEDIA | Video | MPEG-1/2/4→`video/mpeg` · `.avi`→`video/x-msvideo` · `.wmv`→`video/x-ms-wmv` · `.mov`/`.qt`→`video/quicktime` |
| MEDIA | Âm thanh | `.mp3`→`audio/mpeg` · `.wma`→`audio/x-ms-wma` · `.aac`→`audio/aac` |

Định dạng nội dung trong gói: văn bản và tài liệu kỹ thuật là **PDF/A hai lớp**; âm bản/ảnh là JPEG; ghi âm,
ghi hình là MPEG-4, MP3, avi, wma, wmv.

⚠️ **PDF/A không phải cổng chặn của gói.** Chính bảng trên nhận `.txt`, `.rtf`, `.doc`, `.docx`, `.odt` bên
cạnh `.pdf/a`, nên gói vẫn hợp lệ về cấu trúc khi tệp văn bản không phải PDF/A — ràng buộc PDF/A của Điều 8
gắn vào **khâu số hóa từ bản giấy**, không gắn vào khâu đóng gói. Trong luồng đóng gói của dự án, sinh PDF/A
là **tuỳ chọn**; mức tuân thủ, bẫy thứ tự với ký số và điều kiện bật ở [tt05-pdfa.md](tt05-pdfa.md).

## 9. Quy tắc đặt tên file nén ZIP

> `Tên ZIP = [Số thứ tự gói trong lần nộp] + [ID gói]`

Số thứ tự do người dùng tự đánh theo số Ả-rập; ID gói lấy từ `mets/@OBJID` của phần tử gốc METS.xml. Câu chữ
giống hệt nhau ở cả hai phụ lục (trang 238 và trang 291).

⚠️ **Chỉ SIP có quy tắc đặt tên ZIP.** Phụ lục I, II và V đều nói gói được nén ZIP khi truyền nhận nhưng
**không quy định tên file**.

🎯 **Gói mẫu làm khác, và chốt theo gói mẫu:** tên ZIP là **một GUID thường viết chữ nhỏ**,
`b6acafed-a809-4b78-ae9f-0373d50d52f0.zip` — **không có tiền tố `uuid-`, không có số thứ tự, và khác hẳn
`OBJID`** của gói (`uuid-0F203B84-…`). Thư mục gói mang tên `OBJID` **nằm bên trong** file ZIP, chỉ thấy sau
khi giải nén. Nghĩa là một gói có **hai định danh khác nhau**: GUID của file ZIP và `OBJID` của gói bên trong.

## 10. Gói mẫu chuẩn — những gì chốt theo gói mẫu

| | |
|---|---|
| File ZIP | `b6acafed-a809-4b78-ae9f-0373d50d52f0.zip` (~87 MB) |
| Thư mục gói | `uuid-0F203B84-6641-4CDA-9F52-DAAE55FEF6B1` |
| Loại | **`SIP_hoso`** — Phụ lục III |
| Nội dung | 137 tài liệu PDF + 137 `EAD_DOC_*.xml`, hồ sơ `H49.64.33.2001.160` "Tập tờ khai đăng ký kết hôn … phường Hà Tu năm 2001" |
| Phần mềm sinh | `ArchiveNex`, mã phông `P_KHCN`, đóng gói 12/6/2026 |

**Vì sao chắc chắn là `SIP_hoso` chứ không phải `SIP_tailieu`:** `schemas/ead.xsd` khai đúng **18 trường** mở
đầu bằng `fileCode` và có `numberOfPaper` — trùng khít khung `SIP_hoso` ở §4.1; `SIP_tailieu` chỉ có 5 trường
và **bắt buộc** có `source`, mà gói mẫu không có `source` ở đâu cả. Thêm `mets/@TYPE="Mixed"` và việc không có
`metadata/preservation/` thì loại trừ nốt Phụ lục IV lẫn AIP.

**Kết quả kiểm tra toàn vẹn — gói mẫu sạch tuyệt đối:**

| Phép kiểm | Kết quả |
|---|---|
| Checksum SHA-256 + `SIZE` của mọi tham chiếu (10 ở METS gốc, 274 ở METS rep1) | **284/284 đúng** |
| Ánh xạ `MetadataLink/File`: `@DMDID` → `EAD_DOC_X.xml` và `fptr@FILEID` → `X.pdf` | **137/137 khớp tên** |
| File trong `data/` được METS tham chiếu | 137/137, **không file mồ côi, không tham chiếu treo** |
| `dmdSec` và `<file>` được dùng trong `structMap` | 137/137 và 137/137 |
| Bộ tag của 137 file `EAD_DOC` | **đồng nhất tuyệt đối**, chỉ một bộ 21 tag |

Nói cách khác: cơ chế nối metadata ↔ tài liệu, băm và đếm của gói mẫu là **chuẩn để bắt chước**. Sai lệch so
với thông tư nằm ở câu chữ, không nằm ở tính đúng đắn.

**Bảng chốt — thông tư viết một đằng, gói mẫu làm một nẻo, theo gói mẫu:**

| # | Chỗ | Thông tư | **Gói mẫu (chuẩn)** |
|---|---|---|---|
| 1 | Bộ phần tử METS của SIP | 5 phần tử, không có `amdSec` | thêm **`<amdSec ID="uuid-…"/>` rỗng** ở cả METS gốc lẫn rep1 |
| 2 | `agent/note` ở METS gốc | `SOFTWARE VERSION` | **`IDENTIFICATIONCODE` = mã phông** `P_KHCN`; không ghi phiên bản phần mềm ở đâu |
| 3 | `agent` ở METS rep1 | `TYPE="ORGANIZATION"`, tên cơ quan | **`TYPE="INDIVIDUAL" OTHERTYPE=""`**, tên = tài khoản người đóng gói `cvlt` |
| 4 | `altRecordID` | mã đăng ký yêu cầu nộp | **`fileCode` của hồ sơ** |
| 5 | `mets/@OBJID` của rep1 | `uuid-{UUID}` riêng | **chuỗi cố định `rep1`**, không phải uuid |
| 6 | `mdRef/@MDTYPE` cấp gói | `OTHER` | **`DC`** (giống cấp tài liệu) |
| 7 | `mdRef/@MDTYPEVERSION` | `1.0` | **`1.0.0`** |
| 8 | `mdRef/@MIMETYPE` | `text/xml` | **`application/xml`** |
| 9 | Mimetype của `.xsd` | không quy định | **`application/octet-stream`** |
| 10 | Tên file schema | `EAD.xsd`, `EAD_doc.xsd` | **chữ thường**: `ead.xsd`, `ead_doc.xsd`, `ead_pic.xsd`, `ead_media.xsd` |
| 11 | Tên file metadata tài liệu | `EAD_doc_File1.xml` | **`EAD_DOC_` viết hoa** + `docId`: `EAD_DOC_H49.64.33.2001.160.1.xml` |
| 12 | Tên file trong `data/` | không quy định rõ | **`{docId}.pdf`**: `H49.64.33.2001.160.1.pdf` |
| 13 | Tên file ZIP | `[STT][OBJID]` | **GUID thường, không tiền tố, khác `OBJID`** (xem §9) |
| 14 | `format` của hồ sơ | String(50) tự do | dùng **mã `01`/`02`/`03`** như bảng của AIP |
| 15 | Thứ tự phần tử trong `EAD.xml` | theo thứ tự khung XML | **thứ tự xáo trộn** — hợp lệ vì cả hai `.xsd` dùng `xs:all`; **đừng ép thứ tự khi đọc** |

Hai điểm gói mẫu làm **đúng y thông tư**, đáng ghi để khỏi nghi ngờ: ID dùng `uuid-{UUID}` ở mọi nơi trừ
`<file>` dùng `ID-{UUID}`; và số thứ tự trong `docCode` đủ **7 ký tự** (`0000001` … `0000137`).

## 11. Ba lỗi của gói mẫu, đừng học theo

Gói mẫu là chuẩn cho **hình dạng**, không phải chuẩn cho **ba chỗ tự mâu thuẫn** sau. Cả ba đều kiểm được bằng
chính dữ liệu trong gói:

⚠️ **1. `docCode` sai năm ở toàn bộ 137 file.** `fileCode` của hồ sơ là `H49.64.33.**2001**.160` và `docId` là
`H49.64.33.**2001**.160.1`, nhưng `docCode` lại ghi `H49.64.33.**1994**.160.0000001`. Theo §5.1, `docCode` phải
là `fileCode` + số thứ tự 7 ký tự, tức đúng ra là `H49.64.33.2001.160.0000001`. Lệch **137/137 file**, luôn là
năm `1994` — một hằng số bị ghi cứng ở đâu đó trong bộ sinh. **Khi sinh gói mới thì lấy năm từ `fileCode`**,
đừng chép hằng số này.

⚠️ **2. Dữ liệu không hợp lệ với chính schema đi kèm.** `schemas/ead_doc.xsd` khai phần tử **`arcDocCode`** (tên
của AIP), trong khi cả **137/137** file `EAD_DOC_*.xml` đều ghi **`docCode`** (tên của SIP). Đem validate là
trượt hết. Thông tư đứng về phía dữ liệu — SIP dùng `docCode`. Vậy **giữ `docCode` trong dữ liệu và sửa
`ead_doc.xsd` thành `docCode`**, đừng đổi ngược lại.

⚠️ **3. `paperFileCode` để trống dù hồ sơ là bản số hóa.** `EAD.xml` có `confidenceLevel = 02` (số hóa), mà
theo §4.1 thì `paperFileCode` **bắt buộc nhập với hồ sơ số hóa**; trong gói mẫu thẻ này rỗng. Cùng kiểu:
`inforSign` và `description` của hồ sơ cũng rỗng (hai trường này thì không bắt buộc nên chấp nhận được), và
`process`/`autograph` rỗng ở cả 137 tài liệu (đúng luật, vì `confidenceLevel = 02` không thuộc diện bắt buộc
`process`).

> **Tiếp:** [tt05-chuan-aip.md](tt05-chuan-aip.md) — gói bảo quản: PREMIS, `amdSec` và nhóm mã `arc*`.
