import { Routes } from "@angular/router";
import { ChungThuSo } from "./chung-thu-so";
import { PermissionConstants } from "@/app/shared/constants/permission.constants";
import { permissionGuard } from "@/app/shared/guard/permission-guard";

export default [
  { path: '', data: { breadcrumb: 'chung-thu-so', permission: PermissionConstants.MenuChungThuSo }, component: ChungThuSo, canActivate: [permissionGuard] },
] as Routes
