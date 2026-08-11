import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { concatMap, of } from 'rxjs';
import { environment } from '@/environments/environment';
import { Utils } from '@/app/shared/utils';
import { AuthConstants } from '@/app/shared/constants/auth.constants';
import { AppSessionService } from './app-session.service';
import { SharedService } from '../shared.service';

@Injectable({
    providedIn: 'root'
})
export class AuthService {
    private http = inject(HttpClient);
    private router = inject(Router);
    private appSession = inject(AppSessionService);
    private sharedService = inject(SharedService);

    /**
     * Đăng nhập bằng tài khoản/mật khẩu qua OpenIddict password flow, lưu token rồi điều hướng về trang
     * người dùng định vào trước khi bị chặn.
     */
    login(username: string, password: string) {
        const body = new HttpParams()
            .set('username', username)
            .set('password', password)
            .set('grant_type', environment.authGrantType)
            .set('client_id', environment.authClientId)
            .set('client_secret', environment.authClientSecret ?? '')
            .set('scope', environment.authScope);

        const headers = new HttpHeaders({
            'Content-Type': 'application/x-www-form-urlencoded',
            Accept: 'application/json'
        });

        return this.http.post<any>(`${environment.apiUrl}/connect/token`, body.toString(), { headers }).pipe(
            concatMap((res) => {
                Utils.setLocalStorage(AuthConstants.STORAGE_AUTH, {
                    accessToken: res.access_token,
                    refreshToken: res.refresh_token
                });
                this.appSession.init();

                const redirectUri = Utils.getSessionStorage(AuthConstants.REDIRECT_URI_AFTER_LOGIN) || '/';
                Utils.removeSessionStorage(AuthConstants.REDIRECT_URI_AFTER_LOGIN);
                this.router.navigateByUrl(redirectUri);

                return of(res);
            })
        );
    }

    /** Xoá sạch phiên làm việc rồi đưa về màn đăng nhập. */
    logout() {
        this.sharedService.clearAll();
        this.appSession.clear();
        Utils.clearLocalStorage();
        Utils.clearSessionStorage();
        this.router.navigate(['/auth/login']);
    }
}
