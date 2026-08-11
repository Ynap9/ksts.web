import { Routes } from "@angular/router";
import { ImportTuyenSinh } from "./import-tuyen-sinh";
import { PermissionConstants } from "@/app/shared/constants/permission.constants";
import { permissionGuard } from "@/app/shared/guard/permission-guard";

export default [
  { path: '', data: { breadcrumb: 'import-tuyen-sinh', permission: PermissionConstants.MenuImportTuyenSinh }, component: ImportTuyenSinh, canActivate: [permissionGuard] },
] as Routes
