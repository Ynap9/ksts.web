# Chuẩn DIP — gói tài liệu lưu trữ sử dụng (Phụ lục V)

> **Phần 4/5** · trước: [tt05-chuan-aip.md](tt05-chuan-aip.md) · mục lục: [tt05-thong-tu.md](tt05-thong-tu.md)
>
> Gói giao cho **người dùng cuối**, dạng bản sao có xác thực. Phụ lục V, trang 292–357. Cập nhật 2026-09-08.

## 1. DIP dùng khi nào

DIP không phải một lựa chọn song song với SIP/AIP mà là **một trong bốn mức bản dành cho người sử dụng** —
mức cao nhất, quy định ở **Điều 42**: *Bản sao tài liệu lưu trữ số có xác thực dạng gói tin DIP*.

Ba mức dưới nó (bản đọc, bản sao không xác thực, bản sao có xác thực — Điều 37, 40, 41) chỉ là **file lẻ** có
đóng dấu chữ "BẢN SAO"; chúng **không cần đóng gói**. Xem bảng bốn mức ở
[tt05-thong-tu.md §6](tt05-thong-tu.md).

Điều kiện của gói DIP (Điều 42.1–2): được nhân bản từ bản gốc tài liệu lưu trữ số và **bảo đảm các yếu tố xác
thực** đối với bản gốc — tức khác hẳn bản đọc, vốn "không kiểm tra được các yếu tố xác thực".

## 2. Tệp văn bản xác thực đi kèm (Điều 42.2)

Gói DIP phải gán một tệp văn bản xác thực của lưu trữ lịch sử, **định dạng `.pdf/a`**, gồm đúng 11 thông tin:

| # | Thông tin |
|---|---|
| a | **Mã xác thực lưu trữ** |
| b | Tên lưu trữ lịch sử |
| c | Thông tin người nhận: họ và tên, mã định danh công dân hoặc số giấy tờ tùy thân |
| d | **Mã lưu trữ của tài liệu gốc** |
| đ | Số và ký hiệu của tài liệu gốc (nếu có) |
| e | Tên loại tài liệu |
| g | Trích yếu nội dung hoặc tiêu đề tài liệu |
| h | Mục đích sử dụng |
| i | Ngày cấp |
| k | **Thời hạn sử dụng** |
| l | Số lượng bản |

Đây là thứ biến gói DIP thành một bản sao *có thể truy nguyên*: đọc tệp này là biết ai nhận, cho việc gì, lấy
từ tài liệu gốc nào, và dùng được tới bao giờ.

## 3. Cấu trúc vật lý — giống AIP nhưng phần bảo quản là tùy chọn

```text
<OBJID đã thay ":" bằng "_">/            vd uuid_9C13E70E-08B2-4C54-8BAF-979B35D01B4D
├── METS.xml                              bắt buộc, 01
├── metadata/
│   ├── descriptive/
│   │   └── EAD.xml                       bắt buộc, 01 — mô tả YÊU CẦU KHAI THÁC, xem §5
│   └── preservation/                     ○ KHÔNG bắt buộc
│       └── PREMIS.xml                    ○ KHÔNG bắt buộc
├── representations/rep1/
│   ├── METS.xml                          bắt buộc, 01
│   ├── metadata/
│   │   ├── descriptive/
│   │   │   └── EAD_doc_File1.xml …       ≥01 — [Tiêu chuẩn]_[Loại tài liệu]_[Tên file]
│   │   └── preservation/                 ○ KHÔNG bắt buộc
│   │       └── PREMIS_rep1.xml           ○ KHÔNG bắt buộc
│   └── data/
│       └── File1.pdf … Filen.pdf         ≥01 tài liệu
├── schemas/
│   ├── METS.xsd                          bắt buộc, 01
│   └── EAD.xsd · EAD_doc.xsd · EAD_media.xsd · EAD_pic.xsd    ≥01
└── documentation/UserManual.pdf          KHÔNG bắt buộc
```

○ = **khác AIP**: bốn mục bảo quản chuyển từ *bắt buộc* sang *không bắt buộc*. Hợp lý — người dùng nhận bản
sao để đọc, không phải để bảo quản lâu dài; PREMIS chỉ đi kèm khi cần chứng minh chuỗi nguồn gốc.

Hai khác biệt nhỏ nữa so với AIP: `data/` của DIP **không có `.fetch.txt`** (không có cơ chế tài liệu liên kết —
bản giao người dùng phải tự đủ), và mọi ví dụ đều là `.pdf`.

⚠️ Phụ lục V đánh số mục bắt đầu từ **"2. CẤU TRÚC GÓI TIN"** — không có mục 1. Lỗi đánh số của bản gốc, không
phải thiếu nội dung.

## 4. METS

```xml
<mets xmlns:mets="http://www.loc.gov/METS/"
      xmlns:csip="https://DILCIS.eu/XML/METS/CSIPExtensionMETS"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
      xmlns:xlink="http://www.w3.org/1999/xlink"
      OBJID="uuid-4422c185-5407-4918-83b1-7abfa77de182"
      LABEL="Sample E-ARK DIP Information Package"
      TYPE="MIXED"
      PROFILE="https://earkdip.dilcis.eu/profile/E-ARK-DIP.xml"
      xsi:schemaLocation="http://www.loc.gov/METS/ http://www.loc.gov/standards/mets/mets.xsd …">
```

| Điểm | Giá trị cho DIP |
|---|---|
| `csip:OAISPACKAGETYPE` | **`DIP`** — bắt buộc; nhận `SIP`/`AIP`/`DIP`, **mặc định `DIP`** |
| `PROFILE` gốc | **`https://earkdip.dilcis.eu/profile/E-ARK-DIP.xml`** — profile riêng của DIP |
| `PROFILE` rep1 | `https://earkcsip.dilcis.eu/profile/E-ARK-CSIP.xml` — quay lại CSIP, giống AIP |
| `mets/@TYPE` | **`MIXED`** (viết hoa toàn bộ; AIP_hoso dùng `Mixed`, SIP_tailieu dùng `Collection`) |
| `OBJID` | `uuid-{UUID}`; tên thư mục gói **đổi `:` thành `_`** |
| `schemaLocation` gốc | trỏ thẳng ra `http://www.loc.gov/standards/mets/mets.xsd`; ở rep1 lại dùng `schemas/mets1_12.xsd` cục bộ |
| `RECORDSTATUS` | `NEW` mặc định · `SUPPLEMENT` · `REPLACEMENT` · `TEST` · `OTHER` |

`structMap` của DIP dùng cả `@AMDID` lẫn `@DMDID` trên `div[@LABEL="MetadataLink/File"]`, bên cạnh
`div[@LABEL="Data"]` và `div[@LABEL="AttachmentFile"]` — giống AIP.

⚠️ **Hai lỗi trong chính ví dụ của Phụ lục V, đừng chép theo.** Ví dụ `metsHdr` ở trang 311 ghi
`csip:OAISPACKAGETYPE="SIP"` trong khi bảng mô tả ngay trước đó (trang 299) chốt giá trị phải là `DIP`. Cũng ví
dụ đó dùng `agent/@ROLE="ARCHIVIST"` thay vì `CREATOR` như mọi phụ lục khác. Bám bảng mô tả, không bám ví dụ.

## 5. `EAD.xml` cấp gói — mô tả **yêu cầu khai thác**, không mô tả tài liệu

Đây là chỗ DIP khác SIP và AIP về bản chất, không chỉ về tên trường:

```xml
<simpledc>
  <requestID/><requestDate/><purpose/><purposeContent/>
  <feeObjectType/><researchTopic/><description/>
</simpledc>
```

| # | Trường | Tên tiếng Việt | Kiểu | Dài | Quy tắc |
|---|---|---|---|---|---|
| 1 | `requestID` | Mã yêu cầu khai thác | String | 100 | |
| 2 | `requestDate` | Ngày yêu cầu | Date | | |
| 3 | `purpose` | Mục đích | String | 100 | `01` Cá nhân · `02` Công vụ · `03` Công vụ đặc biệt |
| 4 | `purposeContent` | Nội dung mục đích | String | 500 | |
| 5 | `feeObjectType` | Đối tượng | String | 100 | `01`–`10`, xem dưới — dùng để **tính phí** |
| 6 | `researchTopic` | Chủ đề nghiên cứu | String | 250 | |
| 7 | `description` | Ghi chú (nếu có) | String | 2000 | |

**`feeObjectType`** — `01` Học sinh/sinh viên/học viên/nghiên cứu sinh · `02` Thân nhân liệt sĩ ·
`03` Thương binh, bệnh binh · `04` Người hoạt động kháng chiến · `05` Người có công giúp đỡ cách mạng ·
`06` Người thờ cúng liệt sỹ · `07` Người hưởng chế độ hưu trí · `08` Người mất sức lao động, tai nạn lao động ·
`09` Người bị mắc bệnh nghề nghiệp · `10` Khác

Nhóm `02`–`09` là các diện chính sách được ưu tiên/miễn giảm phí; `feeObjectType` chính là chỗ Hệ thống căn cứ
để tính phí đọc và phí cấp bản sao (Điều 36.1.c, Điều 39.1.c).

⚠️ Hệ quả thiết kế: **`EAD.xml` của DIP không mang `fileCode`, `arcFileCode`, `title`, `totalDoc` hay bất kỳ
trường mô tả tài liệu nào.** Bộ sinh gói không thể tái sử dụng đường ghi `EAD.xml` của SIP/AIP cho DIP — phải
là một đường riêng, lấy dữ liệu từ **bản ghi yêu cầu khai thác**, không phải từ hồ sơ.

## 6. Metadata từng tài liệu — gần SIP hơn AIP

⚠️ **Không phải "bê nguyên bộ của AIP".** Đọc thẳng schema Phụ lục V (mục 3.4.2), `EAD_doc` của DIP có
**20 trường** và giống **bản SIP** chứ không giống AIP — chỉ khác hai chỗ: `docCode` → `arcDocCode`, và
**bỏ hẳn `organName`**:

```xml
<simpledc>
  <docId/><arcDocCode/><maintenance/><typeName/><codeNumber/><codeNotation/>
  <issuedDate/><subject/><language/><numberOfPage/><inforSign/><keyword/>
  <mode/><confidenceLevel/><autograph/><format/><process/>
  <riskRecovery/><riskRecoveryStatus/><description/>
</simpledc>
```

Đối chiếu ba chuẩn ở cấp tài liệu văn bản:

| | Số trường | Mã tài liệu | Khác biệt riêng |
|---|---|---|---|
| SIP | 21 (gói mẫu) / 19 (thông tư) | `docCode` | gói mẫu thêm `riskRecovery`, `riskRecoveryStatus` |
| AIP | 19 | `arcFileCode` + `docOrdinal` + `docCode` | bỏ `docId`, `maintenance`, `riskRecovery*` — xem [tt05-chuan-aip.md §5](tt05-chuan-aip.md) |
| **DIP** | **20** | `arcDocCode` | **không có `organName`** |

`arcDocCode` giữ nguyên quy tắc của AIP: **Mã cơ quan lưu trữ + Mã hồ sơ + Số thứ tự tài liệu trong hồ sơ**,
số thứ tự 7 ký tự (`0000001`); `Mã hồ sơ` = Mã định danh cơ quan/tổ chức/cá nhân hoặc Mã phông (phông đóng) +
năm hình thành hồ sơ + số và ký hiệu hồ sơ + Mục lục số (nếu có).

Phụ lục V có đủ bốn schema như các phụ lục khác (mục 3.4): `3.4.1` Schema Gói tin · `3.4.2` tài liệu văn bản ·
`3.4.3` tài liệu phim âm bản/ảnh · `3.4.4` tài liệu phim/âm thanh. Bộ trường của `EAD_pic` và `EAD_media` giống
bản đã liệt kê ở [tt05-chuan-sip.md §5.2–5.3](tt05-chuan-sip.md), với `docCode` đổi thành `arcDocCode`.

## 7. PREMIS trong DIP

Khi có, `PREMIS.xml` của DIP dùng **cùng cấu trúc PREMIS v3.0** như AIP — `premis` · `object` · `event` ·
`agent`, cùng bộ thuộc tính, cùng cách nối `linkingObjectIdentifier`. Xem
[tt05-chuan-aip.md §6](tt05-chuan-aip.md), không lặp lại.

Điểm dùng được: khi cần chứng minh bản giao cho người dùng bắt nguồn từ gói lưu trữ nào, `event` với
`linkingObjectRole = source` trỏ về AIP gốc là bằng chứng sẵn có trong chính gói.

## 8. Bảng đối chiếu nhanh ba chuẩn

| | SIP | AIP | DIP |
|---|---|---|---|
| Phụ lục | III (hoso) · IV (tailieu) | I (hoso) · II (tailieu) | V |
| `csip:OAISPACKAGETYPE` | `SIP` | `AIP` | `DIP` |
| `PROFILE` gốc | E-ARK-**CSIP** | `ra.ee/METS/v01/IP.xml` | E-ARK-**DIP** |
| `PROFILE` rep1 | E-ARK-CSIP | E-ARK-CSIP | E-ARK-CSIP |
| `mets/@TYPE` | `Mixed` / `Collection` | `Mixed` / `Collection` | `MIXED` |
| `OBJID` | `uuid-…` | `urn:uuid:…` hoặc `urn:{mã phông}:uuid-…` | `uuid-…` |
| Tên thư mục | nguyên `OBJID` | `OBJID` đổi `:` → `_` | `OBJID` đổi `:` → `_` |
| `amdSec` | có nhưng **rỗng** (theo gói mẫu) | **có, trỏ PREMIS** | có |
| `metadata/preservation/` + PREMIS | **không có** | **bắt buộc** | **tùy chọn** |
| Mã gói / mã tài liệu | `fileCode` / `docCode` | `arcFileCode` / `arcDocCode` | — / `arcDocCode` |
| `EAD.xml` cấp gói mô tả | hồ sơ (18) hoặc lô nộp (5) | hồ sơ lưu trữ (18) hoặc gói lưu trữ (4) | **yêu cầu khai thác (7)** |
| `.fetch.txt` | `SIP_hoso` — chỉ định danh tài liệu | `AIP_hoso` — urn + size + đường dẫn + ID | không có |
| Quy tắc tên ZIP | **có** (STT + OBJID) | không quy định | không quy định |

Đọc dọc cột cuối sẽ thấy DIP là gói "mỏng" nhất về nghĩa vụ bảo quản nhưng lại là gói duy nhất mang thông tin
về **người nhận và mục đích sử dụng** — đúng vai trò của nó trong OAIS.

> **Tiếp:** [tt05-pdfa.md](tt05-pdfa.md) — PDF/A hai lớp, định dạng tệp nội dung chung cho cả ba loại gói, và
> vì sao luồng đóng gói để nó ở dạng tuỳ chọn.
