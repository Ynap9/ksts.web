import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { concatMap, firstValueFrom, of } from 'rxjs';
import { environment } from '@/environments/environment';
import { Utils } from '@/app/shared/utils';
import { AuthConstants } from '@/app/shared/constants/auth.constants';
import { AppSessionService } from './app-session.service';
import { SharedService } from '../shared.service';
import { ChungThuSoDaChonService } from '../chung-thu-so-da-chon.service';
import { LoKyService } from '../lo-ky.service';

@Injectable({
    providedIn: 'root'
})
export class AuthService {
    private http = inject(HttpClient);
    private router = inject(Router);
    private appSession = inject(AppSessionService);
    private sharedService = inject(SharedService);
    private _chungThuSoDaChonService = inject(ChungThuSoDaChonService);
    private loKyService = inject(LoKyService);

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

    /**
     * Xoá sạch phiên làm việc rồi đưa về màn đăng nhập bằng ĐIỀU HƯỚNG CỨNG, không qua router: nạp lại cả
     * trang là cách duy nhất buông hết trạng thái đang giữ trong RAM của ứng dụng - chứng thư đã chọn, lô
     * đang theo dõi, dữ liệu màn hình - thay vì để chúng sống tiếp cho người đăng nhập kế tiếp trên cùng máy.
     */
    logout() {
        // Dừng lô ký còn dở TRƯỚC khi xoá phiên: sau đó không còn token để gọi, mà lô bỏ lại ở trạng thái
        // đang ký sẽ hiện lại như đang chạy ở lần đăng nhập sau — trong khi không ai mang chữ ký đi nữa.
        this.dungLoKyDangChay().finally(() => this.ketThucPhien());
    }

    /** Huỷ lô ký còn dở của người đang đăng nhập. Không có lô hoặc gọi hỏng đều không chặn việc đăng xuất. */
    async dungLoKyDangChay(): Promise<void> {
        try {
            const dangChay = await firstValueFrom(this.loKyService.loDangChay());
            if (dangChay?.status === 1 && dangChay.data) {
                await firstValueFrom(this.loKyService.huy(dangChay.data.id));
            }
        } catch {
            // Đăng xuất là việc của người dùng, không được phép hỏng vì dọn dẹp phía sau.
        }
    }

    ketThucPhien() {
        this.sharedService.clearAll();
        this.appSession.clear();
        this._chungThuSoDaChonService.clear();
        Utils.clearLocalStorage();
        Utils.clearSessionStorage();
        Utils.clearBrowserCache().finally(() => window.location.assign(AuthConstants.LOGIN_PATH));
    }
}
