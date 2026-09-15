# Chuẩn AIP — gói hồ sơ, tài liệu lưu trữ (Phụ lục I & II)

> **Phần 3/5** · trước: [tt05-chuan-sip.md](tt05-chuan-sip.md) · mục lục: [tt05-thong-tu.md](tt05-thong-tu.md)
>
> Hình dạng gói **được bảo quản trong Hệ thống**. `AIP_hoso` = Phụ lục I (trang 33–109), `AIP_tailieu` =
> Phụ lục II (trang 110–181). Cập nhật 2026-09-08.

## 1. Khi nào ra gói AIP

Ba đường đều dẫn tới AIP:

1. **Thu nộp cùng Hệ thống** (Điều 17) — không phải nhập lại nên đóng thẳng thành AIP.
2. **Số hóa tài liệu** (Điều 8.3) — sản phẩm số hóa đóng theo `AIP_hoso` hoặc `AIP_tailieu`, **không bao giờ là SIP**.
3. **Chuyển đổi từ SIP** (Điều 25.3.a) — lưu trữ lịch sử phê duyệt gói nộp rồi chuyển SIP thành AIP.

Và AIP là **cấu trúc duy nhất được dùng để bảo quản** (Điều 28).

| | `AIP_hoso` (PL I) | `AIP_tailieu` (PL II) |
|---|---|---|
| Đơn vị | Một **hồ sơ lưu trữ** | Một **gói tài liệu lưu trữ** rời lẻ |
| `EAD.xml` cấp gói | **18 trường** mô tả hồ sơ | **4 trường** |
| Tài liệu liên kết `.fetch.txt` | Có quy định | Không |
| `mets/@TYPE` | `Mixed` | `Collection` |

## 2. Cấu trúc vật lý — khác SIP đúng bốn dòng

```text
<OBJID đã thay ":" bằng "_">/            vd urn_G09_uuid_9C13E70E-08B2-4C54-8BAF-979B35D01B4D
├── METS.xml                              bắt buộc, 01
├── metadata/
│   ├── descriptive/
│   │   └── EAD.xml                       bắt buộc, 01
│   └── preservation/                     ★ bắt buộc, 01 — SIP KHÔNG CÓ
│       └── PREMIS.xml                    ★ bắt buộc, 01 — thông tin bảo quản của cả gói
├── representations/rep1/
│   ├── METS.xml                          bắt buộc, 01
│   ├── metadata/
│   │   ├── descriptive/
│   │   │   └── EAD_doc_File1.xml …       ≥01 — [Tiêu chuẩn]_[Loại tài liệu]_[Tên file]
│   │   └── preservation/                 ★ bắt buộc, 01
│   │       └── PREMIS_rep1.xml           ★ bắt buộc, 01 — thông tin bảo quản của bản đại diện
│   └── data/
│       ├── File1.pdf …                   ≥01 tài liệu
│       └── File2.pdf.fetch.txt           chỉ AIP_hoso — tài liệu liên kết
├── schemas/
│   ├── METS.xsd                          bắt buộc, 01
│   └── EAD.xsd · EAD_doc.xsd · EAD_media.xsd · EAD_pic.xsd    ≥01
└── documentation/UserManual.pdf          KHÔNG bắt buộc
```

★ = bốn mục AIP có mà SIP không có. Ngoài ra `AIP_hoso` cho phép `data/` chứa `.fetch.txt`, còn `AIP_tailieu`
chỉ ghi "mỗi File tương ứng với 1 tài liệu **hoặc các tài liệu đính kèm**".

⚠️ Trong bảng cấu trúc của cả hai phụ lục, dòng `metadata/preservation/PREMIS.xml` bị ghi nhầm cột "Định dạng"
là **Thư mục** trong khi nó là **Tệp**. Đọc theo nghĩa, đừng tạo thư mục tên `PREMIS.xml`.

## 3. METS — ba chỗ khác SIP

```xml
<mets xmlns:ext="ExtensionMETS"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
      xmlns:xlink="http://www.w3.org/1999/xlink"
      xmlns="http://www.loc.gov/METS/"
      PROFILE="http://www.ra.ee/METS/v01/IP.xml"
      TYPE="AIP" OBJID="urn:uuid:7d0d1987-0f1c-47a7-8fd6-cc5c7de4064f"
      LABEL="METS file describing the AIP matching the OBJID."
      xsi:schemaLocation="http://www.loc.gov/METS/ schemas/mets_1_11.xsd …">
```

| Điểm | SIP | **AIP** |
|---|---|---|
| `csip:OAISPACKAGETYPE` | `SIP` | **`AIP`** |
| `PROFILE` gốc | `earkcsip.dilcis.eu/…/E-ARK-CSIP.xml` | **`http://www.ra.ee/METS/v01/IP.xml`** (profile của Lưu trữ quốc gia Estonia) |
| `PROFILE` rep1 | như trên | `earkcsip.dilcis.eu/…/E-ARK-CSIP.xml` — **quay lại CSIP** |
| `OBJID` | `uuid-{UUID}` | **`urn:uuid:{UUID}`** hoặc **`urn:{mã phông}:uuid-{UUID}`**, vd `urn:Phong_BNV:uuid-DB15CB0C-…` |
| Tên thư mục gói | lấy nguyên `OBJID` | lấy `OBJID` rồi **đổi mọi ký tự `:` thành `_`** → `urn_G09_uuid_9C13E70E-…` |
| `amdSec` | thông tư không kể; **gói mẫu vẫn phát một `amdSec` rỗng** | **có và có nội dung** — trỏ tới `PREMIS.xml` |
| `schemaLocation` | `mets1_12.xsd` | ví dụ dùng cả `mets_1_11.xsd` (gốc) lẫn `mets1_12.xsd` (các ví dụ sau) |

⚠️ Chính TT05 dùng lẫn `mets_1_11.xsd` và `mets1_12.xsd` giữa các ví dụ trong cùng phụ lục. Bám theo file
`schemas/METS.xsd` thật nằm trong gói, đừng ghi cứng số hiệu bản.

Bộ phần tử METS của AIP có **sáu** phần: `mets` · `metsHdr` · `dmdSec` · **`amdSec`** · `fileSec` · `structMap`.
`amdSec` là chỗ trỏ tới `PREMIS.xml`, và `structMap` của AIP dùng thêm thuộc tính `@ADMID` (bên cạnh `@DMDID`)
để nối một `div` với khối bảo quản tương ứng.

`metsHdr` cũng đổi vai `agent`: ở gốc `AIP_hoso` ví dụ ghi `TYPE="OTHER" OTHERTYPE="SOFTWARE"` với
`name = VietNam Fonds Archival System` và note `SOFTWARE VERSION`; ở rep1 lại là `TYPE="ORGANIZATION"` với note
`IDENTIFICATIONCODE` mang **mã phông** (vd `P623`). `div` gốc của `structMap` trong `AIP_hoso` mang
`@TYPE="NORMALIZED"` — khác `@TYPE="ORIGINAL"` của SIP, phản ánh việc bản đại diện đã qua chuẩn hóa.

## 4. `EAD.xml` — metadata cấp gói

### 4.1. `AIP_hoso` — 18 trường

Cùng bộ trường với `SIP_hoso` ([tt05-chuan-sip.md §4.1](tt05-chuan-sip.md)), **đổi đúng một tên**:

| Trường | Tên tiếng Việt | Kiểu | Dài | Quy tắc |
|---|---|---|---|---|
| **`arcFileCode`** | **Mã hồ sơ lưu trữ** | String | 100 | **Mã cơ quan lưu trữ + Mã hồ sơ**. Trong đó `Mã hồ sơ` = Mã định danh cơ quan/tổ chức/cá nhân **hoặc Mã phông (với phông đóng)** + năm hình thành hồ sơ + số và ký hiệu hồ sơ + **Mục lục số (nếu có)** |

17 trường còn lại giữ nguyên tên và ràng buộc: `title` · `maintenance` · `mode` · `language` · `startDate` ·
`endDate` · `keyword` · `totalDoc` · `numberOfPaper` · `numberOfPage` · `format` · `inforSign` ·
`confidenceLevel` · `paperFileCode` · `riskRecovery` · `riskRecoveryStatus` · `description`.

Hai khác biệt nhỏ so với bản SIP: `startDate`/`endDate` của AIP cho phép thêm dạng rút gọn `MM/YYYY` và `YYYY`;
và `format` được liệt kê giá trị rõ ràng `01` Tốt · `02` Bình thường · `03` Hỏng.

⚠️ Ở `AIP_hoso`, `numberOfPaper` **có mặt trong cả khung XML lẫn bảng mô tả** (mục #10) — tức bản AIP không dính
lỗi thiếu dòng như bản `SIP_hoso`. Khi nghi ngờ trường này, lấy `AIP_hoso` làm trọng tài.

### 4.2. `AIP_tailieu` — 4 trường

```xml
<simpledc>
  <arcFileCode/><title/><source/><description/>
</simpledc>
```

| Trường | Tên tiếng Việt | Kiểu | Dài | Quy tắc |
|---|---|---|---|---|
| `arcFileCode` | Mã gói tin lưu trữ | String | 100 | **Mã cơ quan lưu trữ + Mã gói tin**; `Mã gói tin` = Mã định danh cơ quan + năm hình thành tài liệu + số thứ tự lần nộp lưu + số thứ tự gói tin trong lần nộp |
| `title` | Tiêu đề gói tin | String | 1000 | |
| `source` | Nguồn gốc | **String** | **100** | `0` = văn bản đi · `1` = văn bản đến |
| `description` | Ghi chú (nếu có) | String | 2000 | |

⚠️ `AIP_tailieu` **không có `totalDoc`**, trong khi `SIP_tailieu` có (5 trường). Và `source` ở đây khai kiểu
`String(100)` còn bên `SIP_tailieu` khai `Boolean(1)` — cùng một ý nghĩa, hai kiểu dữ liệu. Khi chuyển
SIP_tailieu → AIP_tailieu phải **bỏ `totalDoc` và ép kiểu `source`**, đừng bê nguyên.

## 5. Metadata từng tài liệu — KHÔNG phải bản SIP đổi tên

⚠️ **Đừng suy `EAD_doc` của AIP từ bản SIP.** Đây là bộ trường **khác hẳn**, đọc thẳng từ schema trong thông tư
(Phụ lục I trang 103, Phụ lục II trang 175 — hai phụ lục khai giống nhau). **19 trường**, theo đúng thứ tự:

```xml
<simpledc>
  <arcFileCode/><mode/><language/><keyword/><numberOfPage/><format/><inforSign/>
  <confidenceLevel/><description/><docCode/><docOrdinal/><typeName/><codeNumber/>
  <codeNotation/><issuedDate/><organName/><subject/><autograph/><process/>
</simpledc>
```

So với `EAD_doc` của SIP: **không có `docId`, `maintenance`, `riskRecovery`, `riskRecoveryStatus`**; **thêm
`arcFileCode` và `docOrdinal`**; và **`docCode` giữ nguyên tên**, không đổi thành `arcDocCode`.

| Trường | Tên tiếng Việt | Vai trò |
|---|---|---|
| **`arcFileCode`** | Mã hồ sơ lưu trữ | Mã hồ sơ **cha** chứa tài liệu — cấp tài liệu tự mang mã hồ sơ thay vì ghép sẵn |
| **`docOrdinal`** | Số thứ tự tài liệu | Số thứ tự trong hồ sơ |

Tức AIP tách cặp **`arcFileCode` + `docOrdinal`** thay cho một chuỗi `arcDocCode` ghép sẵn — khớp với quy tắc
`arcDocCode` = *Mã hồ sơ + Số thứ tự tài liệu* ở trang 82.

⚠️ **`docOrdinal` không có bảng mô tả nào trong toàn thông tư** — nó chỉ xuất hiện ở bốn trang định nghĩa schema
(103, 104, 105, 175). Kiểu và độ dài phải tự chọn.

⚠️ **`arcDocCode` vẫn tồn tại, nhưng không nằm ở đây**: nó là mã lưu trữ tài liệu (String 100, *Mã cơ quan lưu
trữ + Mã hồ sơ + số thứ tự 7 ký tự*) dùng trong `EAD_pic`/`EAD_media` của **`AIP_tailieu`** và trong cả ba
schema cấp tài liệu của **DIP**.

⚠️ **Phụ lục I và II mâu thuẫn nhau về `EAD_pic`/`EAD_media`.** PL-I (`AIP_hoso`) dùng `arcFileCode` +
`docOrdinal`, bỏ `maintenance`/`riskRecovery`; PL-II (`AIP_tailieu`) dùng `arcDocCode` + `maintenance` +
`riskRecovery`/`riskRecoveryStatus`. Không có gói mẫu AIP để phân xử, nên **giữ hai bộ riêng theo đúng phụ lục**.

## 6. PREMIS — siêu dữ liệu bảo quản

Thứ làm nên khác biệt lớn nhất giữa AIP và SIP. Chuẩn PREMIS v3.0
(`http://www.loc.gov/premis/v3`), bốn phần tử:

```xml
<premis xmlns:premis="http://www.loc.gov/premis/v3"
        xmlns:xlink="http://www.w3.org/1999/xlink"
        xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
        xsi:schemaLocation="http://www.loc.gov/premis/v3
                            http://www.loc.gov/standards/premis/premis-3-0-draft.xsd"
        version="3.0">
  <object><objectIdentifier>…</objectIdentifier></object>
  <event><eventIdentifier>…</eventIdentifier></event>
  <agent><agentIdentifier>…</agentIdentifier></agent>
</premis>
```

| Phần tử | Vai trò |
|---|---|
| `premis` | gốc; `@xmlns` bắt buộc, `@version` không bắt buộc |
| `object` | đối tượng được bảo quản |
| `event` | **mỗi sự kiện bảo quản/sao lưu là một `event` riêng biệt** |
| `agent` | tác nhân thực hiện sự kiện |

### 6.1. `object`

Bắt buộc: `@xmlID` · `objectIdentifier` (`objectIdentifierType` vd `File`/`Doc`/`Pic`/`Media`, và
`objectIdentifierValue` — **cặp này phải là duy nhất trong kho**) · `objectCategory` (nhận `bitstream`, `file`,
`intellectual entity`, `representation`).

Không bắt buộc nhưng đáng dùng: `preservationLevel` (`Type` vd *Bit preservation* / *Logical preservation*;
`Value` vd Low/Medium/High; `Role` vd requirement/intention/capability; `Rationale`; `DateAssigned`) ·
`originalName` (tên lúc thu thập, trước khi kho đổi tên) · `objectCharacteristics` (`compositionLevel`,
**`fixity`** với `messageDigestAlgorithm` = **SHA-256** và `messageDigest`, `size`, `format` gồm
`formatDesignation` và `formatRegistry` kiểu PRONOM `fmt/353`) · `storage`/`store` (`contentLocation` với
`Type`/`Value`, `storageMedium` vd *Hard disk*) · `signatureInformation`.

`signatureInformation/signature` là chỗ ghi chữ ký số của đối tượng: `signatureEncoding` (vd `base64`),
`signer`, `signatureMethod` (vd `DSA-SHA 1`), `signatureValue`, `keyInformation`. Đây là móc nối tự nhiên giữa
luồng ký số của KSTS/Sip và siêu dữ liệu bảo quản — bản ký xong có đủ dữ liệu để điền nhóm này.

### 6.2. `event` — nơi ghi lại "gói này từ đâu ra"

Bắt buộc: `eventIdentifier` (`Type` vd `UUID`/`local`, `Value`) · `eventType` · `eventDateTime`.

`eventType` lấy giá trị từ từ vựng **Event Type của Library of Congress** (vd `validation`, `virus check`), và
TT05 chốt cứng hai trường hợp nghiệp vụ:

| Tình huống | `eventType` |
|---|---|
| Số hóa | **`transfer`** |
| Chuyển `SIP` → `AIP_hoso` | **`information package creation`** |

Không bắt buộc: `eventDetailInformation` · `eventOutcomeInformation` (`eventOutcome` — thành công / thành công
một phần / thất bại, vd mã `00`; `eventOutcomeDetail/eventOutcomeDetailNote`) · `linkingAgentIdentifier`
(`Type`, `Value`, **`linkingAgentRole`** nhận `authorizer`, `implementer`, `validator`, `executing program`) ·
`linkingObjectIdentifier` (`Type`, `Value`, **`linkingObjectRole`** nhận `source`, `outcome`).

Cặp `linkingObjectRole = source` / `outcome` chính là cách diễn đạt "gói SIP nào đẻ ra gói AIP nào" — đọc được
cả chuỗi nguồn gốc chỉ bằng PREMIS.

### 6.3. `agent`

Bắt buộc: `agentIdentifier` · `agentIdentifierType` · `agentIdentifierValue` · `agentName`. Ví dụ trong TT05
dùng `linkingAgentIdentifierType = software` với giá trị `E-ARK Web 0.9.3 (task: SIP_to_AIP_hoso_Reset)` — tức
tên **và** phiên bản **và** tác vụ của phần mềm đã tạo gói.

## 7. Tài liệu liên kết trong `AIP_hoso`

Cùng ý tưởng holey file như SIP nhưng **nội dung giàu hơn hẳn**, vì AIP phải trỏ được tới vị trí vật lý thật:

| | `AIP_hoso` (trang 68) |
|---|---|
| Khi nào dùng | Gói `AIP_hoso_2` tham chiếu tài liệu đã nằm trong gói `AIP_hoso_1` đã lưu trong hệ thống |
| Quy tắc tên | `Tên của tài liệu liên kết.Định dạng tài liệu lưu trữ` |
| Đuôi | `.fetch.txt` |
| Nội dung | `urn_{mã phông}_{uuid của gói AIP_hoso_1}_{Đường dẫn đến tài liệu lưu trữ} {size} {đường dẫn đến tài liệu trong gói AIP_hoso_2} {ID tài liệu lưu trữ}` |
| Ràng buộc | **Chỉ tham chiếu được tài liệu trong cùng một phông** |

So sánh: bản `SIP_hoso` chỉ ghi `{Định danh tài liệu}`. Nghĩa là khi chuyển SIP → AIP, holey file **phải được
viết lại**, không chép nguyên — lúc đó mới biết uuid gói đích, kích thước và đường dẫn vật lý.

`structMap` của `AIP_hoso` nối chúng bằng `div[@LABEL="MetadataLink/Holey"]` (kèm `@DMDID` và `@ADMID`), song
song với `div[@LABEL="MetadataLink/File"]` cho tài liệu thường và `div[@LABEL="AttachmentFile"]` cho tài liệu
có đính kèm.

## 8. Những gì AIP **không** quy định

- **Không có quy tắc đặt tên file ZIP.** Phụ lục I và II chỉ nói gói được nén ZIP khi truyền nhận và lưu trữ.
  Chỉ SIP mới có mục "5. Quy định đặt tên file nén ZIP".
- **Không có trường `source` ở `AIP_hoso`** (chỉ `AIP_tailieu` có).
- Danh sách mimetype và các bảng mã (`typeName`, `language`, `maintenance`, `mode`, `confidenceLevel`) **giống
  hệt SIP** — xem [tt05-chuan-sip.md §7–§8](tt05-chuan-sip.md), không lặp lại ở đây.

> **Tiếp:** [tt05-chuan-dip.md](tt05-chuan-dip.md) — gói giao cho người dùng, nơi metadata cấp gói mô tả
> *yêu cầu khai thác* chứ không mô tả tài liệu.
