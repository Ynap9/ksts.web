import { Injectable } from '@angular/core';
import { AuthConstants } from '../shared/constants/auth.constants';

/**
 * Giữ vai trò và quyền của người đang đăng nhập trong bộ nhớ của phiên làm việc.
 * Nạp một lần ở authGuard rồi dùng lại cho mọi màn hình - hỏi lại BE ở từng chỗ kiểm quyền là thừa.
 */
@Injectable({
    providedIn: 'root'
})
export class SharedService {
    private _permissions: string[] = [];
    private _roles: string[] = [];

    get permissions(): string[] {
        return this._permissions;
    }

    get roles(): string[] {
        return this._roles;
    }

    setPermissions(data: string[]) {
        this._permissions = data ?? [];
    }

    setRoles(data: string[]) {
        this._roles = data ?? [];
    }

    /** Admin đi qua mọi cổng kiểm quyền, không cần khai từng permission một. */
    isGranted(permission: string): boolean {
        return this.isSuperAdmin() || this._permissions.includes(permission);
    }

    isSuperAdmin(): boolean {
        return this._roles.includes(AuthConstants.SUPER_ADMIN_ROLE);
    }

    clearAll() {
        this.setRoles([]);
        this.setPermissions([]);
    }
}
