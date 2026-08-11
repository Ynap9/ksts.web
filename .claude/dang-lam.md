# Đang làm dở — đọc file này đầu phiên

> Cập nhật 2026-08-11. Ghi lại trạng thái giữa chừng để phiên sau vào việc ngay, không phải dò lại.

## Bắt đầu từ đâu

> Cập nhật 2026-08-12. Phép ký đã chuyển sang **plugin ở máy người dùng**, không còn đọc certificate store
> của máy chạy API.

**Việc kế tiếp: chạy thử một lô thật trên prod với token cắm ở máy người dùng.**

Luồng ký hiện tại — trang web làm **người đưa thư** giữa máy chủ và token:

```
1. FE  -> plugin  ky-so/mo-phien {thumbprint}     <- hộp PIN bật ĐÚNG MỘT LẦN, plugin giữ handle khoá
2. FE  -> BE      lo-ky/{id}/mo-phien {chungThuBase64}   <- chỉ phần CÔNG KHAI
3. FE  -> BE      lo-ky/{id}/bat-dau
4. lặp: BE giữ lời gọi lo-ky/{id}/cho-ky tới khi có việc, trả tối đa 8 SignedAttributes một đợt
        FE -> plugin ky-so/ky  -> chữ ký thô
        FE -> BE     lo-ky/{id}/chu-ky
5. xong -> plugin ky-so/dong-phien + BE lo-ky/{id}/dong-phien
```

Qua mạng chỉ có **SignedAttributes**, **chữ ký thô** và **chứng thư phần công khai**. Máy chủ tự dựng chuỗi
tin cậy (`ICertificateTrustValidator`), không tin cờ nào máy người dùng gửi lên.

Ba chỗ quyết định tốc độ, đừng đụng nếu chưa đo lại: gom 8 yêu cầu một đợt · giữ lời gọi lấy việc thay vì hỏi
theo nhịp · **không** khoá tuần tự phép ký ở máy chủ (token tự xếp hàng bên plugin).

⚠️ **Đóng tab là lô dừng** — mất người đưa thư. File đã ký giữ nguyên, bấm Bắt đầu lại thì chạy tiếp từ file
dở. Muốn đóng tab mà lô vẫn chạy thì phải chuyển sang WSS; khi đó `IHangDoiKy` và `PluginSigningKey` giữ
nguyên, chỉ thay lớp vận chuyển.

⚠️ **Mở lại màn hình khi lô đang chạy thì chưa nối lại được vòng đưa thư** — màn hình thấy tiến độ nhưng
không ai mang chữ ký đi, các lượt ký hết hạn sau 120 giây và file tính lỗi. Chưa làm nút nối lại phiên vì
việc đó phải hỏi PIN lần nữa; cần quyết định về mặt giao diện trước.

`Signing:Nguon` trong `appsettings.json` đổi được nguồn ký: bỏ trống là **plugin**, đặt `store` thì quay về
đọc certificate store của máy chạy API (chỉ dùng khi API và token cùng một máy Windows).

## Hợp đồng `api/core/lo-ky` (đã chạy)

| Method | Route | Trả về |
|---|---|---|
| POST | `lo-ky` | `ViewLoKyDto` |
| POST | `lo-ky/{id}/them-file` | `ViewLoKyDto` |
| POST | `lo-ky/{id}/bat-dau` | `ViewLoKyDto` |
| GET | `lo-ky/{id}/trang-thai` | `ViewTienDoDto` |
| POST | `lo-ky/{id}/huy` | — |
| GET | `lo-ky/dang-chay` | `ViewLoKyDto` hoặc `null` — thêm ngoài plan, để mở lại màn thấy đúng tiến độ |
| GET | `lo-ky/{id}/zip` | bytes zip thô |

`ViewFileKyDto.trangThai` gửi **chuỗi** (`cho`/`dangKy`/`xong`/`loi`) chứ không phải số thứ tự enum — bảng trên
FE đọc thẳng giá trị này.

## Ký số — đã xong tới đâu

| Bước trong plan | Trạng thái |
|---|---|
| 6 · Shared constants + ErrorCodes | ✅ Bổ sung `SignaturePlaceholderBytes`, `ByteRangeFieldWidth`, `PdfDateFormat`, `VietnamUtcOffsetHours`, ErrorCodes 1143–1147 |
| 4 · `ITimestampClient` | ✅ `external/Tsa/` |
| 3 · `ICmsAssembler` | ✅ `external/Signing/` |
| 1 · `IPdfPreparer` | ✅ `external/Pdf/` — **đã ký thử PDF thật, TSA thật, hợp lệ** |
| 2 · `IPdfAppearanceBuilder` | ✅ vẽ khối chữ + vẽ ảnh chữ ký tươi, cùng một đường cấy Form XObject |
| 5 · `IPdfContentWriter` | ✅ |
| FE màn ký số | ✅ `pages/ky-so`, `ng build` sạch |
| BE lô ký (`LoKy`/`LoKyFile`, `ILoKyService`, `IKySoRunner`, `LoKyController`) | ✅ migration `AddLoKy` đã update DB |
| Nguồn ký `ISigningKey` | 🔶 tạm đọc cert store máy chạy API — seam để đổi sang plugin |
| Plugin đổi sang topology B | ❌ **việc kế tiếp** |

## Mặt chữ ký đi theo CỜ TEMPLATE, không phải "kiểu A/B"

Docs cũ (`docs/luong-ky-so-hang-loat.md`, `be/plans/ky-so-dung-pdf.plan.md`) mô tả hai **kiểu A/B loại trừ
nhau**. Sai so với model thật: FE là **hai checkbox riêng**, DB là **hai cột bool riêng**. Code đã làm đúng
theo cờ, hai tài liệu kia cần sửa lại khi có dịp.

| Cờ | Việc |
|---|---|
| `HienThiChuKySo` | Vẽ khối chữ ký số 2 dòng tại position `ChuKy` |
| `NhoiChuKySoVaoAnh` | Widget chữ ký trùm lên **ảnh chữ ký tươi và con dấu** ⇒ bấm vào ảnh ra bảng thông tin ký |

Bật cả hai ⇒ **một** chữ ký, nhiều widget `/Kids`. Tắt cả hai ⇒ chữ ký **vô hình**, vẫn hợp lệ.
Ảnh chữ ký tươi luôn được vẽ nếu template có; cờ nhồi chỉ quyết định nó là **widget chữ ký** hay
**annotation Stamp** thường. Con dấu **không bao giờ** vẽ lại — đã có sẵn trong trang từ bước import.

## Số đo thật của luồng ký

### Bản đo mới nhất — TRỌN đường, kho `s3-2.huce.edu.vn`, giấy báo thật 911 KB, 24 file/vòng

Đo cả hai chặng MinIO (tải nguồn về + đẩy bản ký lên), TSA thật, ảnh chữ ký tươi có bật nong nét:

| Đồng thời | Tải nguồn | Dựng + ký | TSA | Đẩy bản ký | **ms/file** | **5000 file** |
|---|---|---|---|---|---|---|
| 1 | 135 ms | 55 ms | 11 ms | 115 ms | 317 | 26 phút |
| 4 | 395 ms | 48 ms | 21 ms | 211 ms | 183 | 15 phút |
| **8** | 913 ms | 46 ms | 21 ms | 205 ms | **163** | **13 ph 35** |
| 16 | 1.839 ms | 49 ms | 23 ms | 234 ms | 169 | 14 phút |

**Nút thắt là BĂNG THÔNG tới MinIO.** Cột tải nguồn tăng gần đúng tỉ lệ với số luồng (135 → 395 → 913 →
1.839) — dấu hiệu đường truyền đã bão hoà, thêm luồng chỉ chia nhỏ cùng một lượng băng thông. Vì vậy 16 luồng
**chậm hơn** 8 luồng, và `SoFileSongSong = 8` đang là đúng mức, không cần chỉnh.

Mỗi file đi qua ~1,83 MB (911 KB về + 920 KB lên) ⇒ ở 163 ms/file là **~11 MB/s ≈ 90 Mbps**. Cả lô 5000 tờ là
**~9 GB** qua máy chạy API.

⚠️ Ba điều kiện của con số 13 phút, thiếu một là sai:

1. **Đo với chứng thư trong cert store máy chạy API**, phép ký RSA tại chỗ dưới 1 ms. Lắp plugin vào thì token
   ký **tuần tự**: T ≈ 200 ms một lượt là riêng phần ký đã 5000 × 0,2s = **16 ph 40**, không rút ngắn được
   bằng thêm luồng. Tổng khi đó ≈ **17 phút**, token thành nút thắt mới thay cho băng thông. **T chưa đo
   được** vì chưa có token lẫn plugin.
2. **Đo từ máy dev, không phải máy chủ thật.** API chạy cùng hạ tầng với MinIO thì hai chặng kia rút ngắn
   nhiều, tổng có thể xuống dưới 10 phút; deploy API xa MinIO là 9 GB đi vòng qua Internet hai lần.
3. Chặng **"Dựng + ký" 46 ms** không phình theo số luồng (55→48→46→49) nên khoá quanh PDFsharp chưa thành nút
   thắt. Nếu về sau chuyển API sang gần MinIO thì đây là chặng nặng thứ hai, soi lại từ đây.

Nút "Đẩy lên `GiayBaoTrungTuyenDaKySo`" **không** cộng thêm 9 GB nữa: nó dùng `CopyObject` chép ngay trong
kho chứ không kéo qua máy chủ.

### Bản đo cũ — chỉ phần CPU, chưa tính hai chặng MinIO

Giữ lại vì vẫn đúng cho phần mật mã, nhưng **đừng dùng để ước lượng thời gian lô**: nó bỏ qua đúng chỗ tốn
thời gian nhất. File mẫu 870 KB, 30 vòng, đã bỏ vòng làm nóng.

| Chặng | Trung bình |
|---|---|
| Prepare (dựng bản ký + appearance, CHƯA có ảnh chữ ký tươi) | 4,7 ms |
| BuildSignedAttributes | 0,4 ms |
| Ký RSA tại chỗ | 0,4 ms |
| Assemble CMS | 0,0 ms |
| Ghi CMS vào /Contents | 0,3 ms |
| **Tổng CPU một file** | **6,0 ms** |

Prepare giờ là 46 ms chứ không phải 4,7 ms vì mỗi file phải nạp ảnh PNG 143 KB vào PDF và vẽ 9 lớp nong nét.

Hằng số `TsaTimeoutSeconds = 180` ghi "đo thực 2s-131s" là số bê từ SIP, **sai với thực tế**: TSA chỉ
11–23 ms. Mọi file đều verify được ở mọi mức đồng thời, tức không có chỗ nào dùng chung trạng thái sai.

## Tối ưu luồng ký — đã làm

1. **Phiên ký mở một lần cho cả lô** (`MoPhienAsync`): chứng thư, chuỗi chứng thư, cấu hình template và ảnh
   chữ ký tươi nạp đúng một lần. Trước đó mỗi file đều truy vấn lại template và **tải lại ảnh từ MinIO** —
   lô 5000 file là ngần ấy vòng đi mạng thừa.
2. **8 file chạy song song** (`SoFileSongSong`), mỗi luồng tự rút việc từ hàng đợi.
3. **Phép ký vẫn tuần tự** — khoá `_khoaKy`. Token phần cứng chỉ có MỘT phiên và ký lần lượt, giữ đúng hình
   dạng đó ngay bây giờ để lắp plugin vào không phải sửa lại. Ký chỉ tốn 0,4 ms nên không thành nút thắt.
4. **Nhận việc có khoá** (`_khoaNhanViec`) để hai luồng không nhận trúng cùng một file.
5. **Đếm kết quả bằng một câu lệnh cộng dồn** (`ExecuteUpdateAsync`) thay cho hai câu `COUNT` quét cả bảng
   sau mỗi file — nhiều luồng cùng đếm còn ghi đè kết quả của nhau.
6. **Khoá quanh PDFsharp** (`PdfAppearanceBuilder.Draw`): nó giữ bộ nhớ đệm font dùng chung cho cả tiến
   trình. Đo lại khi đã có ảnh chữ ký tươi: chặng dựng + ký là 46 ms nhưng KHÔNG phình theo số luồng, tức
   phần nằm trong khoá vẫn ngắn — chưa phải nút thắt.

⚠️ Bẫy đã sập một lần: người dùng bấm Huỷ thì các luồng ném `OperationCanceledException`, để nó thoát ra
khỏi `Task.WhenAll` là lô bị ghi thành **Lỗi** thay vì **Huỷ**. Phải nuốt riêng loại đó rồi mới chốt trạng thái.


- CMS hoàn chỉnh (chữ ký + chain + token TSA): **~5,1 KB** — chỗ trống 32 KB đang thừa gấp 6 lần, an toàn.
- Token TSA thật của `tsa.ca.gov.vn`: **~3,75 KB**, gọi được bình thường.
- Ký chồng lên file đã ký: **cả hai chữ ký đều còn hợp lệ**, byte của bản cũ giữ nguyên tuyệt đối.

## Độ đậm ảnh dấu / chữ ký tươi

Template có `DoDamDauDo` và `DoDamChuKyTuoi` — phần trăm so với ảnh gốc, 100 là giữ nguyên, khoảng 40–250,
`DEFAULT 100` ở DB. FE là hai thanh trượt trong khối Tuỳ chọn, kéo tới đâu bản xem trước đổi tới đó.

Áp bằng **mảng `/Decode` của chính PDF**, không xử lý từng pixel. Nhờ vậy không phải kéo thêm thư viện ảnh
(ImageSharp v3 có ràng buộc bản quyền), không tốn CPU, và **byte ảnh gốc giữ nguyên** nên không mất nét vì
mã hoá lại.

Độ đậm gồm **HAI vế**, không chỉ tăng tương phản: `contrast(f)` với `f = phần trăm / 100`, **đồng thời**
`brightness(b)` với `b = max(0.5, 1 - (f-1) * 0.5)`. Chỉ tăng tương phản thì nét mực tách khỏi nền rõ hơn
nhưng **không sâu thêm** — kéo hết thanh vẫn không ra nét chắc như bản giấy. Cả hai vế đều tuyến tính nên gộp
được vào một mảng `/Decode [d0 d1]` với `d0 = b(1-f)/2` và `d1 = d0 + bf`.

Mặc định **140%** — ở mức đó công thức ra đúng `contrast(1.4) brightness(0.8)`, bộ số mà bản dựng thiết kế gốc
(`print_giaybaotrungtuyen.html`, rule `.signature-sign`) đã cân cho ảnh chữ ký quét.

Vế `saturate(1.25)` của bản gốc **không** biểu diễn được bằng `/Decode` vì nó trộn giữa các kênh màu, còn
`/Decode` chỉ ánh xạ từng kênh độc lập. Chấp nhận bỏ: contrast + brightness đã ra gần đủ độ sâu.

Hai chỗ **bắt buộc chừa ra**, sai là hỏng ảnh: lớp mặt nạ trong suốt `/SMask` (nó tả độ mờ chứ không tả màu,
kéo tương phản lên đó là ăn mòn viền chữ ký) và ảnh dùng bảng màu `/Indexed` (đặt sai số phần tử /Decode là
loạn màu). `DemThanhPhanMau` trả 0 cho trường hợp không chắc và khi đó không đụng vào ảnh.

## Độ DÀY nét chữ ký tươi — vế thứ hai, khác hẳn độ đậm

Template có thêm `DoDayNetChuKyTuoi` — phần trăm, khoảng 0–200, `DEFAULT 100` ở DB (migration
`ThemDoDayNetChuKyTuoi`). Thanh trượt thứ ba trong khối Tuỳ chọn, chỉ hiện khi template có ảnh chữ ký tươi.

**Vì sao cần thanh riêng**: `/Decode` ánh xạ lại độ sáng từng pixel **tại chỗ** nên nét mảnh chỉ sẫm màu hơn
chứ không dày thêm — kéo hết thanh Độ đậm vẫn không ra nét chắc như bản giấy. Muốn nét DÀY thì mực phải lan
sang pixel bên cạnh, việc `/Decode` về nguyên lý không làm được.

Bản dựng thiết kế gốc (`print_giaybaotrungtuyen.html`, rule `.signature-sign`) làm bằng **hai lớp
`drop-shadow(0 0 0.35px #1e3a8a)`** chồng lên nhau — đây là vế thứ ba của bộ lọc gốc mà trước đó KSTS bỏ sót,
chỉ mới bê `contrast(1.4) brightness(0.8)`.

Tái hiện trong PDF: **vẽ lại chính ảnh đó 8 lần lệch quanh tâm** rồi vẽ lớp lõi sau cùng
(`IPdfAppearanceBuilder.TinhLopNongNet`). Lõi thu vào đúng bằng bán kính nong để vòng lệch không tràn khỏi
BBox, và xếp cuối để nét gốc nằm trên, sắc nét.

| Chốt | Số |
|---|---|
| Bán kính ở 100% | `0.35 / 158` **bề rộng ảnh** — theo tỉ lệ, không phải số điểm cứng, để ô to ô nhỏ đều dày tương đương |
| Số hướng | 8 — bốn hướng để lại vết chữ thập ở nét chéo |
| Đo thật, khổ 118,5pt | 9 lớp vẽ, **vẫn đúng 2 ảnh nhúng** (ảnh + /SMask), tổng byte object **không đổi** (~130 KB ở cả 0/50/100/200%) |

Không phình file vì mọi lệnh vẽ trỏ về **cùng một XObject** — điều kiện là dùng **một instance `XImage` duy
nhất** cho cả 8 lớp; dựng `XImage` mới cho từng lớp là nhân byte ảnh lên 9 lần.

Bản xem trước FE dùng đúng hai lớp `drop-shadow` như bản gốc, bán kính suy từ bề rộng khối đang hiển thị.
CSS bắt buộc nêu màu nên lấy `#1e3a8a` của bản gốc ⇒ chữ ký mực đen kéo thanh cao sẽ thấy quầng hơi ngả xanh
**ở bản xem trước**, bản in ra thì không (BE vẽ chồng chính ảnh nên giữ đúng màu mực thật).

⚠️ **Con dấu chưa áp được `DoDamDauDo`**: dấu được vẽ sẵn vào mẫu HTML ở bước import chứ không phải lúc ký,
nên phải áp ở luồng dựng giấy báo — chỗ khác hẳn.

## Ba điều đã học, đừng làm lại

- **Chữ ký ký rời**: không dùng được `SignedCms.ComputeSignature` vì khoá nằm ở máy người dùng. Đã tự dựng CMS
  bằng `AsnWriter`. Mẹo cốt lõi: SignedAttributes gửi cho plugin ký là **DER SET OF** (thẻ `0x31`); khi nhét
  vào SignerInfo thì **đổi đúng byte thẻ đầu thành `0xA0`** ([0] IMPLICIT), giữ nguyên phần còn lại. Dựng lại
  bộ thuộc tính lần hai là nguồn lệch một byte làm chữ ký hợp lệ bị coi là sai.
- **PDFsharp 6**: `XImage.FromStream` nhận thẳng `Stream` (không phải `Func<Stream>`), và stream phải **sống
  tới sau `document.Save`** — đóng sớm là mất ảnh, không báo lỗi.
- Bộ thử ký nằm ở scratchpad (`kytest`), dựng lại dễ: console app tham chiếu `ksts.be.external`, tự ký một
  chứng thư tạm, prepare → ký → TSA thật → ghi file → đọc lại `/ByteRange` từ file và `CheckSignature`.

## Việc treo ngoài ký số

1. **Chạy thử 5000 file qua luồng zip mới** — cần restart `ksts.be.api`. Cần soi: tiến độ có nhích đều không,
   và tải file ~4 GB trình duyệt có nhận không.
2. **Dung lượng PDF** — mỗi tờ 915 KB, **~72% là nội dung vector** của lưới kỹ thuật 10mm và hình compa. Muốn
   kéo 3,9 GB xuống thật thì phải sửa mỹ thuật của mẫu — đang chờ quyết định.
3. **Đo tốc độ ký** — ✅ đã đo, xem bảng ở trên: 163 ms/file ở 8 luồng, lô 5000 ≈ 13 ph 35. Còn thiếu đúng
   một số: **`T`, thời gian một lượt ký qua token thật** — chỉ đo được khi đã có token và plugin. Đó là thứ
   quyết định con số cuối cùng, vì token ký tuần tự nên `T = 200 ms` là riêng phần ký đã 16 ph 40.

## Số đo thực đã có (luồng dựng giấy báo)

- Dựng 5000 giấy báo qua Gotenberg: **1685 s (~28 phút)**, zip **3860 MB**, đồng thời 8.
- Throughput Gotenberg bão hoà quanh 2,9 file/s; conc 12 không nhanh hơn đáng kể so với conc 8.
- Mẫu HTML tự chứa sau khi gọn font: **648 KB**.
