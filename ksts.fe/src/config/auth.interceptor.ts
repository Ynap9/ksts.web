import { HttpClient, HttpErrorResponse, HttpHandlerFn, HttpHeaders, HttpParams, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { environment } from '@/environments/environment';
import { Utils } from '@/app/shared/utils';
import { AuthConstants } from '@/app/shared/constants/auth.constants';

/**
 * Gắn access token và tiền tố apiUrl cho mọi request tương đối; gặp 401 thì đổi refresh token lấy token
 * mới rồi phát lại đúng request đó, hết đường thì dọn phiên và đưa về màn đăng nhập.
 *
 * Chỉ gắn Authorization KHI CÓ token: gắn "Bearer undefined" làm chính endpoint lấy token bị từ chối.
 */
export function authInterceptor(req: HttpRequest<unknown>, next: HttpHandlerFn) {
    const http = inject(HttpClient);
    const router = inject(Router);
    const token = Utils.getAccessToken();

    const buildUrl = (url: string) => (url.startsWith('http') ? url : `${environment.apiUrl}${url}`);

    // Token chỉ được gắn cho API của chính hệ thống. Plugin ký số chạy ở máy người dùng là tiến trình
    // khác, gửi Bearer sang đó là phát access token ra ngoài phạm vi cần thiết.
    const url = buildUrl(req.url);
    const isOwnApi = url.startsWith(environment.apiUrl);

    const newReq = req.clone({
        headers: token && isOwnApi ? req.headers.set('Authorization', `Bearer ${token}`) : req.headers,
        url
    });

    return next(newReq).pipe(
        catchError((error: HttpErrorResponse) => {
            if (error.status !== 401) {
                return throwError(() => error);
            }

            const refreshToken = Utils.getRefreshToken();
            if (!refreshToken) {
                Utils.clearLocalStorage();
                Utils.clearSessionStorage();
                router.navigate(['/auth/login']);
                return throwError(() => error);
            }

            const body = new HttpParams()
                .set('grant_type', 'refresh_token')
                .set('client_id', environment.authClientId)
                .set('client_secret', environment.authClientSecret ?? '')
                .set('refresh_token', refreshToken);

            const headers = new HttpHeaders({
                'Content-Type': 'application/x-www-form-urlencoded',
                Accept: 'application/json'
            });

            return http.post<any>(`${environment.apiUrl}/connect/token`, body.toString(), { headers }).pipe(
                switchMap((res) => {
                    Utils.setLocalStorage(AuthConstants.STORAGE_AUTH, {
                        accessToken: res.access_token,
                        refreshToken: res.refresh_token
                    });

                    return next(
                        req.clone({
                            setHeaders: { Authorization: `Bearer ${res.access_token}` },
                            url: buildUrl(req.url)
                        })
                    );
                }),
                catchError((refreshErr) => {
                    Utils.clearLocalStorage();
                    Utils.clearSessionStorage();
                    router.navigate(['/auth/login']);
                    return throwError(() => refreshErr);
                })
            );
        })
    );
}
