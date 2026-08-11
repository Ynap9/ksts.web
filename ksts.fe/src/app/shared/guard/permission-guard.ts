import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { SharedService } from '@/app/service/shared.service';
import { AuthConstants } from '../constants/auth.constants';


export const permissionGuard: CanActivateFn = (route, state) => {

    const router = inject(Router);
    const _sharedService = inject(SharedService);

    const requiredPermission = route.data['permission'] as string;

    if (_sharedService.roles.includes(AuthConstants.SUPER_ADMIN_ROLE)) {
        return true;
    }
    if (_sharedService.permissions?.includes(requiredPermission)) {
        return true;
    }

    const uri = 'auth/access'
    router.navigate([uri]);
    return false;
};
