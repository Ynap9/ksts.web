# Ký số trên macOS

> 🔬 **NGHIÊN CỨU — chưa thi công.** Khảo sát 2026-09-08, bổ sung 2026-09-17. Bản trình bày đầy đủ (HTML):
> https://claude.ai/code/artifact/55928828-28b3-4a69-9126-41fa0042b1cd. Đọc cùng
> [../../contracts/plugin-ky-so.contract.md](../../contracts/plugin-ky-so.contract.md) và
> [../../plugin/plans/ky-so-plugin.plan.md](../../plugin/plans/ky-so-plugin.plan.md).

Câu hỏi: plugin ký số (`ksts.plugin`, dùng chung cho `ksts.be` và `kssm.be`) có chạy được trên Mac không, nếu
làm mới thì stack nào, và **ký được chứng thư Ban Cơ yếu bằng Rust trên macOS không** — ưu tiên giải pháp miễn
phí.

## Kết luận nhanh

| Câu hỏi | Trả lời |
|---|---|
| Cài plugin hiện tại lên Mac? | ❌ Không — khoá ở `net9.0-windows` + WinForms + Windows certificate store |
| Dựa vào driver của hãng? | ❌ Không — quyết định dự án; driver VGCA cho Mac là `x86_64` thuần, không nạp được trên chip M |
| Tự nói chuyện với token? | ✅ Được — token VGCA là **PKCS#15 chuẩn**, đọc sạch không cần PIN (đo trên thẻ thật) |
| Ký chứng thư Ban Cơ yếu bằng Rust trên Mac? | ✅ **Được về nguyên lý**, 0 đồng tới bản nội bộ — **chưa ký thật**, còn 3 phép đo |
| Một bản cho cả Mac Intel lẫn chip M? | ✅ Universal binary, tối thiểu đề xuất **macOS 11** |

## Mục lục — đọc theo thứ tự

| Phần | Nội dung |
|---|---|
| [01-token-vgca.md](01-token-vgca.md) | Token VGCA đo trên thẻ thật, gói driver macOS của Ban Cơ yếu, OpenSC, phần chứng thư bị nén |
| [02-ky-so-da-nang.md](02-ky-so-da-nang.md) | Mổ app Rust “Ký số đa năng”: crate, luồng chạm thẻ, hỏi PIN, bảo vệ localhost, TLS, phát hành |
| [03-kha-thi-rust.md](03-kha-thi-rust.md) | Bảy mắt xích để ký chứng thư Ban Cơ yếu bằng Rust, phát hành miễn phí, phạm vi Mac Intel / chip M |
| [04-phuong-an.md](04-phuong-an.md) | Phụ thuộc Windows của plugin hiện tại, chi phí ẩn, bốn phương án .NET/Rust, lộ trình, đường vòng |
| [05-chuan-bi-rust.md](05-chuan-bi-rust.md) | Rust cho người viết C#: ánh xạ khái niệm, bộ crate, bẫy, thứ tự học R1–R5 |

## Ba phép đo còn thiếu

1. **macOS có nhận đầu đọc `bit4id TokenME EVO v2`** — cần máy Mac.
2. **Giải nén chứng thư thẻ `7A`** — làm offline trên Windows, đã có cả bản nén lẫn bản gốc.
3. **`VERIFY` + ký thật** (`MSE:SET` key `0x10` → `PSO:CDS`) — làm trên Windows, **bằng token dự phòng**.

⚠️ Mọi phép đo trên thẻ tới nay **chỉ là lệnh đọc**. Sai PIN 3 lần là khoá chết token — xem
[01-token-vgca.md](01-token-vgca.md).

> **Tiếp:** [01-token-vgca.md](01-token-vgca.md) — bên trong token Ban Cơ yếu có gì.
