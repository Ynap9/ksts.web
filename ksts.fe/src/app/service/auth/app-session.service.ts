import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { Utils } from '@/app/shared/utils';
import { AuthConstants } from '@/app/shared/constants/auth.constants';
import { IUserLogin } from '@/app/models/user.models';

/**
 * Thông tin người dùng của phiên hiện tại, suy từ token và phát lại cho các thành phần giao diện.
 */
@Injectable({
    providedIn: 'root'
})
export class AppSessionService {
    private _user: IUserLogin = {};
    user$ = new BehaviorSubject<IUserLogin | null>(null);

    get user(): IUserLogin {
        return this._user;
    }

    getUserObs(): Observable<IUserLogin | null> {
        return this.user$.asObservable();
    }

    setUserObs(value: IUserLogin) {
        this.user$.next(value);
    }

    /** Đọc token, dựng lại thông tin người dùng và lưu để lần vào sau không phải giải mã lại. */
    init(): boolean {
        const payload = Utils.getDecodedJwtPayload();
        if (!payload) {
            return false;
        }

        this._user = {
            id: payload.user_id ?? payload.sub,
            username: payload.username,
            fullName: payload.name,
            email: payload.email
        };

        this.setUserObs(this._user);
        Utils.setLocalStorage(AuthConstants.STORAGE_USER, this._user);
        return true;
    }

    clear() {
        this._user = {};
        this.user$.next(null);
        Utils.removeLocalStorage(AuthConstants.STORAGE_USER);
    }
}
