# Ký số cho `kssm.be` — nghiên cứu

> 🔶 **Chưa thi công.** Cập nhật 2026-09-04. File này chốt **kiến trúc và các quyết định đã hỏi**; bề mặt API
> chi tiết chốt ở contract riêng khi bắt tay làm. Đọc kèm [luong-ky-so-hang-loat.md](luong-ky-so-hang-loat.md)
> — bản KSTS là thứ được bê sang, kể cả các nợ kỹ thuật ghi ở mục cuối.

## Hai trường hợp phải chạy

| | TH1 — file / thư mục trên máy | TH2 — dự án |
|---|---|---|
| Nguồn file | Người dùng tải lên qua FE | `ocrdocuments` của dự án, đọc từ MongoDB |
| Kho làm việc | MinIO **mặc định** của `kssm.be` (`appsettings`) | MinIO **của chính dự án** |
| Bản đã ký ghi vào | `lo-ky/{loKyId}/da-ky/` | `ky-so/{tên dự án}/` trong bucket dự án |
| Tải zip | ✅ | ✅ — kéo từ MinIO dự án |
| Sau khi xong | Không đụng gì thêm | Gọi ngược `sao_mai_be` đổi trạng thái dự án |

Dự án được chọn **chỉ gồm dự án `dang_nghiem_thu`**; ký xong chuyển sang trạng thái **đã ký số**.

⚠️ `PROJECT_STATUS` bên `sao_mai_be` hiện là `khoi_tao · dang_ocr · dang_nghiem_thu · da_xong` — **chưa có**
giá trị nào cho "đã ký số". Thêm giá trị mới kéo theo `projectStatusGroup()`, `PROJECT_STATUS_GROUP_ORDER`,
tab và badge của màn Quản lý dự án. Đây là việc bên repo `soHoaSaoMai`, phải làm trước khi bật TH2.

## Đọc MongoDB của sao_mai

`kssm.be` nối thẳng vào `ConnectionStrings:MongoDb` đã có sẵn (`v2NhapLieu`). Chỉ **đọc**, ba collection:

| Collection | Lấy gì | Dùng để |
|---|---|---|
| `projects` | `name`, `code`, `status`, `isDelete` | Dropdown chọn dự án (lọc `dang_nghiem_thu`), tên thư mục đích |
| `projectocrconfigs` | `minioEndpoint`, `minioUseSSL`, `storageBucket`, `minioAccessKey`, `minioSecretKey`, `fileStorageProvider` | Dựng client MinIO **riêng của dự án** |
| `ocrdocuments` | `project_id`, `pdfUrl`, `originalFileName`, `pageCount`, `orderIndex`, `isDelete` | Danh sách file vào lô |

Lấy **toàn bộ** file chưa xoá có `pdfUrl` khác rỗng, sắp theo `orderIndex` rồi `createdAt` — không lọc theo
trạng thái OCR.

`pdfUrl` là URL canonical dạng `{scheme}://{host[:port]}/{bucket}/{objectName}`; cắt tiền tố
`{domain}/{bucket}/` ra được object key. Đúng phép cắt mà `fileStorage.service.js` bên `sao_mai_be` đang dùng.

⚠️ `fileStorageProvider` của dự án có thể là `local` hoặc `google_cloud`. Gặp hai giá trị đó thì **đánh trượt
ngay lúc mở lô** kèm câu nói rõ dự án chưa dùng MinIO, đừng để tới lúc ký từng file mới hỏng.

⚠️ `minioAccessKey`/`minioSecretKey` nằm **thẳng trong Mongo** (cờ `private` chỉ giấu khỏi response của
`sao_mai_be`, không mã hoá). Không ghi hai trường này ra log, không trả về bất kỳ API nào của `kssm.be`.

## Một tiến trình, nhiều MinIO

`IS3FileStorage` hiện tại nhận `IOptions<S3Settings>` và dựng **một** `AmazonS3Client` lúc khởi tạo, đăng ký
Singleton. TH2 cần mỗi dự án một client khác endpoint, khác bucket, khác khoá.

Hướng: tách thành **`IS3ClientFactory`** trả client theo cấu hình, cache theo khoá
`endpoint|bucket|accessKey` (đúng cách `sao_mai_be` cache). `IS3FileStorage` hiện tại giữ nguyên bề mặt cho
ảnh template và cho TH1 — nó chỉ là factory gọi với cấu hình mặc định. Không nhân đôi code đọc/ghi object.

## Đường đi của file

```text
TH1  FE tải lên -> lo-ky/{loKyId}/nguon/{000001}.pdf   (MinIO mặc định)
                -> ký -> lo-ky/{loKyId}/da-ky/{tên gốc}.pdf
                -> zip: kéo từng bản đã ký, nén thẳng vào luồng gửi

TH2  ocrdocuments.pdfUrl -> object key trong bucket dự án   (KHÔNG chép sang chỗ khác)
                -> ký -> ky-so/{tên dự án}/{tên gốc}.pdf    (cùng bucket dự án)
                -> zip: kéo từ bucket dự án
```

Bản đã ký giữ **đúng tên file gốc** để đối chiếu được với bản chưa ký, giống cách KSTS đặt tên theo số CCCD.

⚠️ Tên thư mục là **tên dự án nguyên văn**, có dấu và có khoảng trắng (đã chốt). Object key kiểu đó hợp lệ với
S3 nhưng **mọi chỗ ghép vào URL đều phải encode**: đường presign, link tải, tên entry trong zip, và cả câu log.
Bỏ sót một chỗ là lỗi 404 khó lần. Đổi tên dự án giữa chừng thì lô sau ghi sang thư mục khác — chấp nhận.

## Bê gì từ `ksts.be`

| Bê nguyên | Vì sao giữ nguyên |
|---|---|
| `external/Pdf/*` — Preparer, AppearanceBuilder, ObjectWriter, ContentWriter, SignatureInspector | Không dính nghiệp vụ HUCE, chỉ làm việc với PDF |
| `external/Signing/*` — `HangDoiKy`, `CmsAssembler`, `PluginSigningKey` | Hàng đợi không biết ai là người đưa thư nên dùng lại được |
| `external/Tsa/TimestampClient` | RFC 3161 thuần |
| `shared/Constants/Signing/*`, `LoKy/*` | Số đo và OID; đổi là phải đo lại |
| `applications/LoKy` — `KySoRunner`, `LoKyService` | Bộ khung lô ký, sửa phần nguồn file và phần trạng thái |

| **Không** bê | Vì sao |
|---|---|
| `SealPlacementResolver` + `SealPlacementConstants` | Dò mốc chữ chốt cứng chức danh của Trường ĐH Xây dựng HN |
| `GiayBaoConstants`, `LoKyConstants.GetKhoDaKyKey` | Đường dẫn kho của nghiệp vụ giấy báo |
| `IdUser`, `TemplateAccessDenied`, mọi lời gọi `getCurrentUserId()` | `kssm.be` không có auth |
| `StoreSigningKey` | Chỉ tiện cho máy dev, và là bẫy K5 của KSTS |

Chứng thư CA đã ghim (`Cert/*.crt`) và `CertificateTrustValidator` **đã có sẵn** ở `kssm.be`. Template ký cũng
đã đủ dữ liệu (thumbprint, ảnh dấu và chữ ký tươi, `positions`, ba cờ, màu, độ đậm, độ dày nét) — tầng template
không phải sửa gì.

## Vòng đời lô và trạng thái

```text
MoiTao ──nạp file──> MoiTao ──bat-dau──> DangKy ──hết file──> Xong ──callback──> dự án "đã ký số"
                                            │                    ▲
                                   tam-dung │ phiên chết         │ bat-dau lại
                                            ▼                    │
                                         TamDung ────────────────┘
                                            │ huy
                                            ▼
                                           Huy
```

`TamDung` là trạng thái **mới so với KSTS** và là cốt lõi của phần sửa nợ kỹ thuật. `Huy` là kết thúc hẳn;
`TamDung` là còn ký tiếp được. Việc lấy file kế tiếp luôn lọc `TrangThai = Cho` nên "ký tiếp" không bao giờ ký
đè lên file đã xong — điều này KSTS đã làm đúng, giữ nguyên.

Lô gắn `userId` do `sao_mai_be` gửi kèm (không kiểm quyền, chỉ để lọc "lô đang chạy của tôi" và truy vết),
đúng cách `templates_ky_so` đang lưu `createdBy`.

## Ba nợ kỹ thuật phải sửa, không bê theo

### 1. Rút token giữa lô

Hiện tượng ở KSTS: rút token thì mọi lượt ký sau đó ném lỗi riêng lẻ, `KySoRunner` ghi lỗi **từng file** rồi
lấy file tiếp theo, trang web vẫn chạy vòng đưa thư — lô 5.000 file cháy dần tới file cuối cùng.

Sửa ở **hai tầng**:

- **Plugin**: trong lúc phiên mở, quét certificate store mỗi 2 giây; thumbprint biến mất thì đóng phiên và
  ghi lại lý do. `ky-so/ky` trả **một mã riêng** cho "phiên không còn" thay vì lỗi theo từng yêu cầu.
- **`kssm.be`**: nhận mã đó ở `chu-ky` ⇒ dừng runner, trả mọi file `DangKy` về `Cho`, chuyển lô sang
  `TamDung` kèm lý do. **Không** đếm file nào thành lỗi.

### 2. Phiên chết không được lẫn với file hỏng

Cùng gốc với mục trên nhưng rộng hơn: plugin khởi động lại, phiên quá 15 phút, hoặc người dùng đóng tab đều
phải ra cùng một kết cục `TamDung`. Chỉ lỗi **của riêng file** (PDF hỏng, đã có chữ ký mà template chưa bật
`kyDe`, TSA trượt 3 lần) mới được ghi vào `LyDoLoi`.

Tiến độ trả thêm cờ **`phienConSong`**. Mở lại màn hình giữa lô mà cờ này `false` thì FE **không** chạy vòng
đưa thư, chỉ hiện nút Ký tiếp — đây chính là chỗ KSTS đang đốt file thành lỗi mà không báo gì.

### 3. Tạm dừng · Ký tiếp · Huỷ

| Thao tác | Máy chủ làm gì | Người dùng thấy gì |
|---|---|---|
| **Tạm dừng** | Dừng runner, file `DangKy` về `Cho`, lô sang `TamDung` | Tiến độ đứng lại, số đã ký giữ nguyên |
| **Ký tiếp** | Mở phiên mới rồi `bat-dau` lại, chạy từ file `Cho` đầu tiên | **Hộp PIN bật lại** — handle khoá cũ đã mất, không tránh được |
| **Huỷ** | Dừng runner, lô sang `Huy`, giữ nguyên file đã ký | Lô đóng hẳn, muốn ký tiếp phải mở lô mới |

⚠️ Bẫy đã sập ở KSTS, phải bê theo cách sửa: bấm dừng thì tám luồng cùng ném `OperationCanceledException`; để
nó thoát khỏi `Task.WhenAll` là lô bị chốt thành **Lỗi** thay vì `Huy`/`TamDung`. Nuốt riêng loại đó rồi mới
chốt trạng thái.

## Bẫy bê theo nguyên si

- **Fail-closed với TSA**: hỏng sau 3 lần thử ⇒ file đó hỏng, không bao giờ phát hành chữ ký thiếu dấu thời gian.
- **Ảnh chữ ký tươi tải hỏng ⇒ dừng cả lô**, không ký thiếu ảnh rồi mới phát hiện sau 5.000 tờ.
- **Không khoá tuần tự phép ký ở máy chủ** — token tự xếp hàng bên plugin; khoá ở đây là mất hết tác dụng của
  việc gom 8 yêu cầu mỗi đợt.
- **Chỗ trống `/Contents` 32 KB** và bề rộng số `/ByteRange` 10 chữ số: đổi là hỏng file.
- **Zip nén ngay lúc tải**, không dựng file nén trên đĩa máy chủ.

## Chưa chốt, quyết trước khi thi công

1. **Callback đổi trạng thái dự án**: `sao_mai_be` chưa có đường nào cho service khác gọi vào —
   `project/update/:id` đòi Bearer của người dùng. Cần một route nội bộ kèm khoá dùng chung, và cách xử lý khi
   callback hỏng (lô đã ký xong nhưng trạng thái dự án chưa đổi).
2. **Có ghi ngược đường dẫn bản đã ký vào `ocrdocuments` không** (thêm trường `signedPdfUrl`), hay chỉ để file
   nằm trong `ky-so/{tên dự án}/`.
3. **Ký lại một dự án đã ký**: gặp file trùng tên trong thư mục đích thì ghi đè hay bỏ qua.
4. **Origin production của `sao_mai_fe`** phải vào `PluginConstants.OriginMacDinh` rồi đóng gói lại — việc cũ
   còn treo từ 2026-08-29, TH1 lẫn TH2 đều không chạy được nếu thiếu.
