import { Routes } from "@angular/router";
import { Template } from "./template";
import { ConfigTemplate } from "./config-template/config-template";
import { PermissionConstants } from "@/app/shared/constants/permission.constants";
import { permissionGuard } from "@/app/shared/guard/permission-guard";

export default [
  { path: '', data: { breadcrumb: 'template', permission: PermissionConstants.MenuTemplate }, component: Template, canActivate: [permissionGuard] },
  { path: 'config-template/:id', data: { breadcrumb: 'config-template', permission: PermissionConstants.MenuTemplate }, component: ConfigTemplate, canActivate: [permissionGuard] },
] as Routes
