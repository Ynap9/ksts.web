import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { Utils } from '../utils';
import { UserService } from '@/app/service/user.service';
import { SharedService } from '@/app/service/shared.service';
import { AppSessionService } from '@/app/service/auth/app-session.service';

/**
 * Chặn cửa mọi route trong khung ứng dụng: phải có token còn dùng được, và phải nạp xong vai trò/quyền
 * từ BE trước khi cho vào.
 *
 * Gọi /me chứ không chỉ đọc token: token có thể còn hạn nhưng tài khoản đã bị khoá hoặc đổi quyền, mà
 * quyền thì phải lấy từ BE - tin vào claim nằm sẵn trong token là để người dùng tự quyết quyền của mình.
 * Trả UrlTree thay vì gọi navigate rồi return true: navigate ở đây tạo ra hai lượt điều hướng chồng nhau.
 */
export const authGuard: CanActivateFn = async (route, state) => {
    const router = inject(Router);
    const userService = inject(UserService);
    const sharedService = inject(SharedService);
    const appSession = inject(AppSessionService);

    const loginUrlTree = router.createUrlTree(['/auth/login'], {
        queryParams: { redirect_uri: state.url }
    });

    if (!Utils.getAccessToken()) {
        return loginUrlTree;
    }

    try {
        const res = await firstValueFrom(userService.getMe());
        if (res?.status !== 1 || !res.data) {
            return loginUrlTree;
        }

        sharedService.setRoles(res.data.roles ?? []);
        sharedService.setPermissions(res.data.permissions ?? []);
        appSession.init();
        return true;
    } catch {
        return loginUrlTree;
    }
};
