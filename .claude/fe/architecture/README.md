# Kiến trúc Frontend KSTS

> Viết 2026-08-17 cho phần **đã chạy**. Angular 21 standalone + zoneless, signals, PrimeNG 21, Tailwind 4.
> Mã nguồn: `ksts.fe/`. Kế hoạch từng màn hình nằm ở [../plans/](../plans/).

## Nền

| Thứ | Bản | Ghi chú |
|---|---|---|
| Angular | 21 | **Standalone toàn bộ**, không `NgModule`; `provideZonelessChangeDetection()` |
| PrimeNG | 21 + `@primeuix/themes` Aura | Chế độ tối bật bằng class `.app-dark` |
| Tailwind | 4 (`@tailwindcss/postcss`) | Kèm `tailwindcss-primeui` |
| Khung giao diện | **sakai-ng** | Layout, menu, topbar bê từ template — xem mục dưới |
| Khác | `pdfjs-dist` 6 · `jwt-decode` · `moment` | pdf.js dùng ở màn cấu hình template |

TypeScript `strict` + `strictTemplates`, alias **`@/*` → `src/*`** (`tsconfig.json`).

## Bản đồ thư mục

```
src/main.ts                 bootstrapApplication(AppComponent, appConfig)
src/app.config.ts           composition root: router · http + authInterceptor · zoneless · PrimeNG · Message/Dialog/Confirmation
src/app.routes.ts           khung route: AppLayout + authGuard, con nạp lười
src/config/                 auth.interceptor.ts — gắn token, gắn tiền tố apiUrl, làm mới token khi 401
src/environments/           apiUrl · appUrl · auth* (OpenIddict) · pluginUrl (127.0.0.1:17739)

src/app/layout/             vỏ sakai: app.layout · app.menu · app.topbar · app.sidebar · layout.service
src/app/pages/              màn hình nghiệp vụ, một thư mục một màn
src/app/models/             *.models.ts — interface khớp payload BE/plugin
src/app/service/            một service cho một nhóm API + service giữ trạng thái phiên; auth/ cho phần đăng nhập
src/app/shared/components/  base (BaseComponent) · breadcrumb · data-table
src/app/shared/constants/   hằng số dùng chung + hằng số nghiệp vụ của từng nhóm màn
src/app/shared/guard/       auth-guard · permission-guard
src/app/shared/models/      envelope, paging, cột bảng, payload JWT, kiểu environment
src/app/shared/utils.ts     Utils tĩnh: localStorage, token, ngày tháng, bỏ dấu
src/app/shared/import.shared.ts  SharedImports — một mảng import PrimeNG dùng cho mọi màn
```

Chiều phụ thuộc: `pages → service → models` và `pages → shared`. Service **không** biết gì về component;
`shared/` không import từ `pages/`.

## Màn hình của KSTS

| Route | Thư mục | Việc |
|---|---|---|
| `''` (trang chủ) và `/ky-so` | `pages/ky-so` | Ký số hàng loạt — vòng đưa thư giữa BE và plugin |
| `/chung-thu-so` | `pages/chung-thu-so` (+ `cai-plugin`) | Đọc chứng thư từ plugin, xác thực token, popup tải bộ cài |
| `/template` · `/template/config-template/:id` | `pages/template` | CRUD template + màn cấu hình chữ ký trên bản xem trước PDF |
| `/import-tuyen-sinh` | `pages/import-tuyen-sinh` | Dựng giấy báo trúng tuyển từ Excel, theo dõi lô, tải zip |
| `/user-management/{user,role}` | `pages/user-management` | Tài khoản và vai trò |
| `/auth/*` | `pages/auth` | Đăng nhập, chặn truy cập |

⚠️ **`pages/uikit`, `pages/crud`, `pages/service`, `pages/documentation`, `pages/landing`, `pages/empty`,
`pages/dashboard` là phần demo còn lại của sakai** — không thuộc KSTS. `pages/service/customer.service.ts` là
9057 dòng dữ liệu mẫu. **Đừng lấy chúng làm mẫu quy ước** và đừng sửa theo chúng; mẫu đúng là `pages/template`
(CRUD) và `pages/ky-so` (màn nặng). `pages.routes.ts` chỉ nối ba màn demo; `Dashboard` được import trong
`app.routes.ts` mà **không gắn vào route nào** — import chết, xoá được khi dọn.

## Chốt công việc

```bash
cd ksts.fe && npm run build        # bắt buộc sạch trước khi coi là xong
npm start                          # ng serve khi làm
npm run format                     # prettier, 4 space, nháy đơn
```

13 file `*.spec.ts` trong repo là bản CLI sinh sẵn (chỉ `should create`), **không được bảo trì** — `npm test`
không nằm trong khuôn chốt việc, `npm run build` mới là mốc.

## Đọc tiếp

| File | Nội dung |
|---|---|
| [01-routing-guards.md](01-routing-guards.md) | Khung route, nạp lười, hai guard, menu theo quyền |
| [02-http-services.md](02-http-services.md) | Interceptor, khuôn service, envelope, gọi plugin, tải file lớn |
| [03-components-state.md](03-components-state.md) | `BaseComponent`, signals, `DataTable`, dialog, khuôn một màn CRUD |
| [04-man-hinh-dac-thu.md](04-man-hinh-dac-thu.md) | Màn ký số và màn cấu hình template — hai chỗ khó nhất |
| [05-conventions.md](05-conventions.md) | **Quy ước bắt buộc**: đặt tên, comment, hằng số, việc không được làm |

> **Tiếp:** [01-routing-guards.md](01-routing-guards.md) — khung route và cách chặn cửa theo quyền.
