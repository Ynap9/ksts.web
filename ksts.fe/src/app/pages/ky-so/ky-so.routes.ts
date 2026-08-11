import { Routes } from '@angular/router';
import { PermissionConstants } from '@/app/shared/constants/permission.constants';
import { permissionGuard } from '@/app/shared/guard/permission-guard';
import { KySo } from './ky-so';

export default [
    {
        path: '',
        data: { breadcrumb: 'ky-so', permission: PermissionConstants.MenuKySo },
        component: KySo,
        canActivate: [permissionGuard]
    }
] as Routes;
