# Đang làm dở — đọc file này đầu phiên

> Cập nhật 2026-09-15. Chỉ ghi **trạng thái và việc kế tiếp**; tri thức bền vững nằm ở `docs/`, `contracts/`
> và `be/architecture/`, đừng chép lại vào đây.

## Đang làm (2026-09-16) — gói SIP đẩy lên Google Drive, trả link thư mục

Gói ZIP ghi vào thư mục con của `Drive:DRIVE_PACKAGE_FOLDER_ID`, tên theo thư mục gốc người dùng tải lên.
`dong-goi` và `danh-sach` trả thêm `thuMucDongGoi` + `driveFolderUrl`. Đóng gói xong thì **xoá cả
`dong-goi/{sessionId}/` trên MinIO** và đóng phiên bằng `PackagedDate` — gọi lại `dong-goi`, `them-file`,
`bat-dau` trên phiên đó trả mã **1224**.

Migration đã sinh (2026-09-16). `lo-ky/{id}/danh-sach-file` trả thêm `driveFileUrl` từng file.

`sao_mai_fe` đã thay nút tải zip / tải gói bằng link Drive (thư mục + cột link từng file / từng gói), và gửi
`tenThuMuc` (từ `webkitRelativePath`) lúc tạo lô. `sao_mai_be` chuyển tiếp `tenThuMuc` và đã gỡ hai route proxy
`lo-ky/:id/zip`, `goi/:id/ho-so/:hoSoId/tai-ve`. Cả ba khối **chưa build, chưa chạy thử**.

## Đang làm (2026-09-15) — bản đã ký đẩy lên Google Drive thay cho MinIO

Xong tầng `kssm.be`, **chưa build lần nào và chưa sinh migration**. Kiến trúc và các bẫy ở
[docs/ky-so-kssm.md](docs/ky-so-kssm.md).

Đã làm: `external/Drive/` (`IDriveFileStorage` — tìm/tạo thư mục con, đẩy file), `DriveSettings` +
mục `Drive` trong `appsettings.json`, mã lỗi **1060-1062**, và bỏ hẳn đường tải zip (`lo-ky/{id}/zip`,
`GhiNenAsync`, cột `TaiToken`). Bản nguồn TH1 **vẫn lên MinIO** rồi xoá cả `lo-ky/{id}/` khi lô xong.

Đổi lược đồ, **phải `dotnet ef migrations add` rồi `database update`**: `LoKy` bỏ `TaiToken`, đổi
`TienToKho` → `ThuMucDaKy`, thêm `DriveFolderId`; `LoKyFile` đổi `ObjectKeyDaKy` → `DriveFileId`.

Việc kế tiếp, đúng thứ tự: **`sao_mai_be`** (chuyển tiếp `tenThuMuc` lúc tạo lô, bỏ route proxy zip) rồi
**`sao_mai_fe`** (gửi tên thư mục từ `webkitRelativePath`, thay nút Tải zip bằng nút mở link Drive).

## Đang làm (2026-09-09) — đóng gói TT05 ở `kssm.be`, đường local

Nhận thư mục tải lên rồi kiểm trước khi đóng gói. Kế hoạch đầy đủ ở
[be/plans/dong-goi-kiem-tra.plan.md](be/plans/dong-goi-kiem-tra.plan.md).

Đã viết xong tầng BE: 3 entity `PackageSession`/`PackageDossier`/`PackageDocument`, module
`applications/DongGoi/Package/` (5 service), `external/DongGoi/` (soi PDF/A, đo giống tên cột, kho tạm),
port `external/Excel/` từ `ksts.be`, và `api/core/dong-goi/kiem-tra` 6 route. Dải mã lỗi mới **1200-1219**.

**Chưa chạy được:** phải `dotnet ef migrations add` cho 3 bảng mới rồi `database update`, và **chưa build lần
nào**. Việc kế tiếp sau khi chạy thông: dựng gói SIP thật (METS 2 tầng, EAD_DOC, checksum, ZIP), rồi mới tới
đường **dự án** và FE.

Ba điểm đã chốt khác với hình dung ban đầu:

1. **Tên file PDF khớp `docId`** (*Mã định danh tài liệu*), không phải `docCode` (*Mã lưu trữ*) — gói mẫu đặt
   `H49.64.33.2001.160.1.pdf` còn `docCode` là `…160.0000001`, so nhầm là trượt 100% số file.
2. **Không kiểm cấu trúc thư mục**, chỉ phân biệt một hay nhiều hồ sơ theo đường dẫn tương đối của file.
3. **Soi chữ ký số và PDF/A ngay lúc upload**, lúc bytes còn trong tay — để bước kiểm tự tải về thì lô 10.000
   file kéo thêm ~8,7 GB chiều ngược, tức 9-14 phút chỉ để đọc lại thứ vừa đi qua.

## Đang nghiên cứu (2026-09-04) — ký số cho `kssm.be`

Hai trường hợp: **file/thư mục trên máy** (ký rồi đẩy lên MinIO mặc định, tải zip) và **dự án** (đọc
MongoDB của sao_mai lấy MinIO riêng của dự án, ký toàn bộ file có `pdfUrl`, ghi bản ký vào
`ky-so/{tên dự án}/` trong chính bucket đó, tải zip). Chỉ chọn được dự án `dang_nghiem_thu`; ký xong
`kssm.be` **gọi ngược `sao_mai_be`** đổi dự án sang trạng thái "đã ký số" — giá trị này **chưa có** trong
`PROJECT_STATUS`, phải thêm bên repo `soHoaSaoMai` trước.

Ba nợ kỹ thuật của `ksts.be` **không bê theo**: rút token giữa lô, phiên chết bị lẫn với file hỏng, và thiếu
hẳn Tạm dừng / Ký tiếp. Cách sửa đã chốt: thêm trạng thái `TamDung`, plugin quét cert store mỗi 2 giây và trả
mã riêng khi phiên mất, Ký tiếp thì hỏi PIN lại. Kiến trúc, phần bê được từ `ksts.be` và bốn việc chưa chốt ở
[docs/ky-so-kssm.md](docs/ky-so-kssm.md).

## Vừa xong (2026-09-04) — màn cấu hình template ở `sao_mai_fe` xem được PDF thật

Trước đó khối chữ ký được thả lên một khung A4 vẽ tay. Nay khung đó là **bản dựng pdf.js của file thật**,
cuộn dọc đủ mọi trang, và khối lưu kèm **đúng số trang đã thả** thay vì luôn `pageNumber = 1`.

Bốn nguồn file, đổi bằng thanh trên khung xem trước: **file mẫu** đi kèm bản cài (nền mặc định lúc mở màn,
qua `file-mau/noi-dung` của `kssm.be`), **chọn file** và **chọn thư mục** trên máy (`webkitdirectory`, lọc
`.pdf`, sắp theo tên, bấm ‹ › qua lại), và **chọn dự án** — `sao_mai_be` đọc `OcrDocument` của dự án rồi ký
tạm từng URL MinIO (`file-du-an`, trần 50 file). Ba route ở `sao_mai_be` đã có sẵn từ trước, phiên này chỉ
làm phần FE: `getFileMauNoiDung` + `getFileDuAn` ở `template-ky-so.service.ts` và toàn bộ khung xem trước ở
`config-template`.

Worker pdf.js trỏ vào `shared/assets/pdf.worker.min.js` — bản `.js` do `scripts/copy-pdf-worker.mjs` chép ra,
**không** dùng bản `.mjs` của `pdfjs-dist`, cùng bẫy MIME đã sập bên `ksts.fe`.

⚠️ Chưa mắt thấy trên trình duyệt: **file của dự án tải qua đường ký tạm của MinIO nên kho phải mở CORS cho
origin của `sao_mai_fe`**; thiếu tiêu đề đó thì pdf.js báo tải hỏng chứ không phải file lỗi. File mẫu và file
chọn từ máy không dính CORS nên vẫn xem được — đừng lần theo hướng "file PDF hỏng" khi chỉ nguồn dự án trắng.

## Vừa xong (2026-09-04) — `kssm.be` đã có DB và chạy hết một vòng

Migration `Initial_Create` + `Update` **đã áp** lên `KY_SO_SAO_MAI`. Đã chạy thử qua API: tạo template →
`cau-hinh` kèm ảnh (ảnh lên MinIO, URL công khai đọc được) → `GET`/`find-paging` → `PUT` không gửi file mà
giữ nguyên ảnh cũ. Mã lỗi `1005` (toạ độ ngoài trang) và `1001` (không có template) trả đúng như hợp đồng.
Còn thiếu: chạy `DELETE` và soi lại hai bảng xem xoá mềm có ăn cả `TemplatePosition` không.

## Vừa xong (2026-09-03) — template cấu hình chữ ký ở `kssm.be`

Bê module template từ `ksts.be` sang `kssm.be`, cắt phần phụ thuộc người đăng nhập và phần dò mốc đặt dấu:
`api/core/template-chu-ky` (CRUD tên · `cau-hinh` multipart · `find-paging` · `file-mau`), **không**
`vi-tri-goi-y` — dấu đỏ dùng đúng toạ độ người dùng kéo thả. Hợp đồng ở
[contracts/template-chu-ky-kssm.contract.md](contracts/template-chu-ky-kssm.contract.md).

Khác `ksts.be` ba chỗ, đều do không có auth và do entity bên này để **quan hệ mềm**: bỏ `IdUser` +
`TemplateAccessDenied`, `createdBy` để trống (chủ sở hữu do bảng Mongo bên `sao_mai_be` giữ theo Id trả về);
và `TemplatePosition` không có navigation lẫn khoá ngoại nên tự nạp theo `TemplateId`, tự thay khi lưu, tự
xoá mềm khi xoá template.

Bê kèm `external/Colors` (chuẩn hoá `#RRGGBB`). Đăng ký DI còn thiếu đã thêm: `IS3FileStorage`,
`ITemplateImageStorage`, `IHexColorReader`, `Configure<S3Settings>` và `AddHttpContextAccessor()` — xem dòng
⚠️ cuối bản hợp đồng, thiếu dòng cuối là **mọi** service của `kssm.be` hỏng lúc dựng.

Phần DB và vòng chạy thử đã xong ngày 2026-09-04, xem mục trên.

## Vừa xong (2026-09-02) — nối được plugin từ cả hai backend

Exe đổi tên thành **`Ký số plugin.exe`** (`<AssemblyName>`, `CaiDatConstants.TenExe`, `SetupFileName` của cả
hai BE). Thư mục cài và khoá registry vẫn là `KySoPlugin` — chỉ tên file đổi. Tên có dấu và có khoảng trắng
nên `dong-goi.ps1` **lấy tên từ chính bản publish** thay vì ghi cứng: PowerShell 5.1 đọc `.ps1` không BOM theo
bảng mã ANSI, viết thẳng chuỗi có dấu vào script là ra sai tên file.

`PluginConstants.PhienBan` nay **đọc từ assembly** nên `<Version>` trong csproj là nguồn duy nhất; phải cắt ở
dấu `+` vì .NET gắn commit SHA vào `InformationalVersion`. FE đã gọi `phien-ban` ở màn Chứng thư số và màn Ký
số — chỉ nhắc, không chặn. CORS của plugin thêm `localhost:3000` (cả `http` lẫn `https`).

Đã chạy `dong-goi.ps1`: bộ cài mới nằm ở `Plugins/` của **cả hai** BE, `GET bo-cai` trả `exists = true`. Bản
`KstsPlugin.exe` cũ vẫn còn trong `ksts.be/.../Plugins/` — xoá tay, `.csproj` khớp `Plugins\*.exe` nên nó vẫn
bị chép sang output.

## Vừa xong (2026-08-29) — plugin tách khỏi KSTS, nối thêm `kssm.be`

Plugin không còn thuộc riêng KSTS nữa: **`KstsPlugin` → `KySoPlugin`** (tên hiển thị, exe, thư mục cài, khoá
autostart, khoá gỡ cài đặt). Thư mục và namespace `ksts.plugin.*` **giữ nguyên** — đổi nốt chúng chỉ tạo một
diff khổng lồ mà không đổi hành vi.

`kssm.be` giờ phát được bộ cài như `ksts.be`: `dong-goi.ps1` chép cùng một exe sang `Plugins/` của cả hai bên.
Thêm **`GET api/core/plugin/phien-ban`** ở cả hai BE, đối chiếu phiên bản plugin với whitelist
`Plugin:PhienBanPhuHop` trong `appsettings.json` (khớp chính xác cả chuỗi, sửa file là ăn ngay không phải khởi
động lại API). Chi tiết ở [contracts/plugin-ky-so.contract.md](contracts/plugin-ky-so.contract.md).

Còn thiếu đúng một việc của mảng này: **khai origin prod của FE gọi `kssm.be`** vào
`PluginConstants.OriginMacDinh` rồi đóng gói lại. Hai việc kia đã xong ngày 2026-09-02, xem mục trên.

## Vừa xong (2026-08-17 → 18), chưa chạy thử trên máy thật

Ba tuỳ chọn mới của template — migration `ThemKyDeVaMauChuKy` và `MauChuKyTuoiChoPhepTrong` **đã sinh, chưa
`database update`**:

1. **Cờ ký đè** (`KyDe`): tắt thì lô ký đánh trượt file nguồn đã có chữ ký (`1148`), bật thì ký thêm bình
   thường. Mặc định tắt nên template cũ được bảo vệ sẵn.
2. **Tự cuộn khung xem trước khi kéo khối** ở màn cấu hình template.
3. **Màu khối chữ ký số và màu mực chữ ký tươi** (`MauChuKySo`, `MauChuKyTuoi`). `MauChuKyTuoi` **để trống là
   chưa chọn ⇒ giữ nguyên mực ảnh gốc**; đen nay là màu thật, nhuộm được. Bảng màu trên màn cấu hình khởi đầu
   bằng **màu mực trích từ chính ảnh** (`InkColorService` đọc pixel qua canvas).

Còn phải mắt thấy: nhuộm ảnh chữ ký tươi **thang xám** (đường bảng màu `/Indexed`) trên file ký thật, đối
chiếu bản xem trước FE với bản in ra, và **màu trích ra có đúng không** — cả lúc vừa chọn ảnh (blob cùng gốc)
lẫn lúc mở lại template (ảnh từ kho object; kho thiếu tiêu đề CORS thì canvas bị nhiễm và bảng màu lùi về đen).

Sửa kèm: **worker pdf.js phát dưới đuôi `.js`** thay cho `.mjs` — trên prod host không map `.mjs` nên trả
`application/octet-stream` và ô xem trước PDF trắng trơn. Phải **build lại và deploy lại FE** thì màn cấu hình
template mới xem được PDF; chi tiết ở [fe/architecture/04-man-hinh-dac-thu.md](fe/architecture/04-man-hinh-dac-thu.md).

## Việc kế tiếp

**Chạy thử một lô thật trên prod với token cắm ở máy người dùng.** Toàn bộ đường ký đã chạy được từ đầu tới
cuối trên máy dev; thứ chưa có là **token thật**.

Ba việc đi kèm, làm ngay trong lần chạy thử đó:

1. **Đo `T`** — thời gian một lượt ký qua token — bằng `POST plugin ky-so/do-toc-do`. Đây là con số cuối cùng
   còn thiếu để biết lô 5000 file mất bao lâu: token ký tuần tự nên `T = 200 ms` là riêng phần ký đã 16 ph 40,
   không rút ngắn được bằng thêm luồng.
2. **Kiểm hộp PIN có hiện chìm sau trình duyệt không.** Plugin chạy nền không sở hữu cửa sổ; nếu chìm thì
   hướng sửa là cho nó chạy dạng tray app có cửa sổ ẩn rồi truyền HWND vào thuộc tính CNG `"HWND Handle"`.
3. **Xem lô 5000 file qua đường zip mới**: tiến độ có nhích đều không, và tải file ~4 GB trình duyệt có nhận
   không.

## Đọc gì trước khi động vào

| Việc định làm | Đọc |
|---|---|
| Bất cứ thứ gì thuộc luồng ký | [docs/luong-ky-so-hang-loat.md](docs/luong-ky-so-hang-loat.md) |
| Sửa API lô ký / màn ký số | [contracts/lo-ky.contract.md](contracts/lo-ky.contract.md) |
| Tách Dừng khỏi Huỷ, làm đường ký tiếp | [be/plans/dung-va-huy-lo-ky.plan.md](be/plans/dung-va-huy-lo-ky.plan.md) · [fe/plans/](fe/plans/dung-va-huy-lo-ky.plan.md) |
| Tách Dừng khỏi Huỷ, làm đường ký tiếp | [be/plans/dung-va-huy-lo-ky.plan.md](be/plans/dung-va-huy-lo-ky.plan.md) · [fe/plans/](fe/plans/dung-va-huy-lo-ky.plan.md) |
| Sửa plugin | [contracts/plugin-ky-so.contract.md](contracts/plugin-ky-so.contract.md) · [plugin/plans/](plugin/plans/) |
| Sửa luồng dựng giấy báo | [docs/dung-giay-bao-tuyen-sinh.md](docs/dung-giay-bao-tuyen-sinh.md) |
| Sửa bất cứ màn hình FE nào | [fe/architecture/](fe/architecture/README.md) |
| Sửa màn ký số của `sao_mai_fe` | [contracts/template-chu-ky-kssm.contract.md](contracts/template-chu-ky-kssm.contract.md) · `soHoaSaoMai/.claude/rules/frontend.md` |
| Làm phần ký số của `kssm.be` | [docs/ky-so-kssm.md](docs/ky-so-kssm.md) · [docs/luong-ky-so-hang-loat.md](docs/luong-ky-so-hang-loat.md) |
| Sửa bất cứ tầng BE nào | [be/architecture/](be/architecture/README.md) |
| Bàn chuyện bảo mật nâng cao | [docs/bao-mat-agent-ky-so.md](docs/bao-mat-agent-ky-so.md) — 🔬 nghiên cứu |

## Hai giới hạn đã biết, phải nói với người dùng

⚠️ **Đóng tab là lô dừng.** Trang web là người đưa thư giữa máy chủ và token. File đã ký giữ nguyên và vẫn hợp
lệ.
lệ.

⚠️ **Mở lại màn hình giữa lô thì chưa nối lại được vòng đưa thư** — thấy đúng tiến độ nhưng không ai mang chữ
ký đi. Nay có đường đi vòng: bấm **Ký tiếp** để chạy tiếp từ file kế tiếp (hỏi PIN lại một lần), thay vì phải
lập lô mới và ký lại từ đầu.
ký đi. Nay có đường đi vòng: bấm **Ký tiếp** để chạy tiếp từ file kế tiếp (hỏi PIN lại một lần), thay vì phải
lập lô mới và ký lại từ đầu.

## Việc treo, chưa tới lượt

1. **Dung lượng PDF** — mỗi tờ 915 KB, **~72% là nội dung vector** của lưới kỹ thuật 10mm và hình compa. Muốn
   kéo 3,9 GB xuống thật thì phải sửa mỹ thuật của mẫu — **đang chờ quyết định của người dùng**.
2. **Người dùng nhập PIN hai lần một lô** (xác thực chứng thư, rồi mở phiên ký). Bỏ bước xác thực thì lỗi cert
   sai hiện muộn hơn, sau khi đã tải file lên. Chưa quyết.
3. **Giám sát rút token** ở plugin (quét cert store mỗi 2s). Hiện rút token giữa lô biểu hiện thành một loạt
   file lỗi thay vì một thông báo rõ ràng.
4. **Mùa tuyển sinh sau**: đổi `GiayBaoConstants.NamTuyenSinh` + `Khoa` + năm/khoá ghi cứng trong
   `Templates/html/giay-bao-trung-tuyen.html`, cả ba cùng lúc.
5. **Ký dự án lưu file trên đĩa** (`fileStorageProvider = "local"`, và dự án chưa có bản ghi
   `projectocrconfigs` nào). `ProjectReader.GetProjectStorageAsync` đang ném `DuAnKhongDungMinio`. Hai bên
   chạy cùng một máy chủ nên hướng đã chốt là **đọc thẳng đĩa**, cần một setting trỏ tới thư mục chứa
   `uploads` của sao_mai_be; bản ký vẫn đẩy lên kho mặc định. Chưa thi công.
6. **Ký dự án dùng Google Cloud Storage** (`fileStorageProvider = "google_cloud"`). Vẫn ném
   `DuAnKhongDungMinio`. Cần thêm SDK và service account, phạm vi lớn hơn hẳn mục 5. Chưa thi công.
