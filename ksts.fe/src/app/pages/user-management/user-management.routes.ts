import { Routes } from "@angular/router";
import { User } from "./user/user";
import { Role } from "./role/role";
import { PermissionConstants } from "@/app/shared/constants/permission.constants";
import { permissionGuard } from "@/app/shared/guard/permission-guard";

export default [
  { path: 'user', data: { breadcrumb: 'user', permission: PermissionConstants.MenuUserManagementUser }, component: User, canActivate: [permissionGuard] },
  { path: 'role', data: { breadcrumb: 'role', permission: PermissionConstants.MenuUserManagementRole }, component: Role, canActivate: [permissionGuard] },
] as Routes