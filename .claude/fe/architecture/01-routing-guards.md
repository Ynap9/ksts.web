# Route và chặn cửa theo quyền

> **Phần 1/5** · mục lục: [README.md](README.md)

## Khung route

`src/app.routes.ts` chỉ có ba nhánh gốc: khung ứng dụng, các trang ngoài khung, và bắt tất.

```ts
{ path: '', component: AppLayout, canActivate: [authGuard], children: [
    { path: '', component: KySo },                                          // trang chủ = màn ký số
    { path: 'template', loadChildren: () => import('./app/pages/template/template.routes') },
    …
]},
{ path: 'auth', loadChildren: () => import('./app/pages/auth/auth.routes') },  // NGOÀI khung, không guard
{ path: '**', redirectTo: '/notfound' }
```

`authGuard` đặt ở **route cha** nên chặn một lần cho mọi màn trong khung, không rải vào từng route con.
`auth/*` phải nằm ngoài nhánh đó — đặt vào trong là màn đăng nhập cũng bị guard chặn, thành vòng lặp điều hướng.

## Route con nạp lười

Mỗi màn có một file `<ten>.routes.ts` **export default một mảng**, khớp với `loadChildren` không cần `.then()`:

```ts
export default [
  { path: '', data: { breadcrumb: 'template', permission: PermissionConstants.MenuTemplate },
    component: Template, canActivate: [permissionGuard] },
  { path: 'config-template/:id', data: { … }, component: ConfigTemplate, canActivate: [permissionGuard] },
] as Routes
```

`data.permission` là khoá `permissionGuard` đọc; `data.breadcrumb` chỉ để tra cứu, breadcrumb hiển thị do
**chính component** dựng (xem [03-components-state.md](03-components-state.md)).

Màn ký số vừa là `''` (trang chủ) vừa có nhánh `/ky-so` nạp lười — mở bằng đường nào cũng ra cùng component.

## authGuard — có token là chưa đủ

`shared/guard/auth-guard.ts`, hàm `async`:

1. Không có access token ⇒ trả `UrlTree` về `/auth/login?redirect_uri=<url đang vào>`.
2. Có token ⇒ **gọi `userService.getMe()`** rồi nạp `roles` + `permissions` vào `SharedService`, gọi
   `AppSessionService.init()`.
3. `status !== 1`, không có `data`, hoặc lời gọi hỏng ⇒ về màn đăng nhập.

Ba quyết định trong đó, đừng đảo:

- **Gọi `/me` chứ không chỉ đọc token.** Token còn hạn nhưng tài khoản có thể đã bị khoá hoặc đã đổi quyền.
- **Quyền lấy từ BE, không lấy từ claim trong token.** Tin claim là để người dùng tự quyết quyền của mình.
- **Trả `UrlTree` chứ không `navigate()` rồi `return true`.** `navigate` ở đây sinh hai lượt điều hướng chồng
  nhau.

Hệ quả: mỗi lần vào khung ứng dụng có **một** lời gọi `/me` chặn đường; đó là giá của việc quyền luôn tươi.

## permissionGuard — chặn theo từng màn

`shared/guard/permission-guard.ts` đọc `route.data['permission']`, cho qua nếu là super admin
(`SharedService.isSuperAdmin()`) hoặc có permission đó, còn lại điều hướng sang `auth/access`.

Guard này **đứng sau** `authGuard` về thời gian: nó chỉ đúng khi `SharedService` đã được nạp. Vì `authGuard`
nằm ở route cha nên thứ tự đó luôn được bảo đảm — đừng gắn `permissionGuard` cho route nằm ngoài khung.

## Menu ẩn theo quyền — hai chỗ, không phải một

`layout/component/app.menu.ts` dựng `model` trong `ngOnInit` với `visible: isGranted(PermissionKey)` cho từng
mục. Ẩn mục menu **không** phải chặn: người dùng gõ thẳng URL vẫn vào được. Vì vậy mọi màn có quyền riêng phải
khai **cả hai**:

| Chỗ | Việc |
|---|---|
| `app.menu.ts` → `visible` | Không hiện mục người dùng không có quyền |
| `<ten>.routes.ts` → `data.permission` + `permissionGuard` | Chặn thật khi gõ URL |

Menu dựng **một lần** trong `ngOnInit`, không phải signal: quyền đã nạp xong trước khi layout render, và trong
một phiên thì quyền không đổi. Đổi quyền phải đăng nhập lại — đã chấp nhận.

## Khoá permission

`shared/constants/permission.constants.ts` là **chỗ duy nhất** khai chuỗi permission. Khoá phải khớp từng ký tự
với `PermissionKeys` của BE (`ksts.be.shared/Constants/Auth`); lệch một ký tự thì màn hình lặng lẽ biến mất khỏi
menu mà không có lỗi nào.

⚠️ Quirk hai bên đang **cố tình khớp nhau**: `MenuUserManagementRole` được ghép thành `Menu.UserManagement_User`
(hậu tố `_User`, không phải `_Role`) ở **cả FE và BE**. Nghĩa là ai có quyền menu User thì cũng qua cổng menu
Role. Sửa một bên là hai bên lệch nhau và cổng quyền hỏng lặng lẽ — muốn sửa thì **sửa cả hai file cùng lúc**,
kèm dữ liệu permission đã gán trong DB.

> **Tiếp:** [02-http-services.md](02-http-services.md) — interceptor, khuôn service và cách nói chuyện với plugin.
