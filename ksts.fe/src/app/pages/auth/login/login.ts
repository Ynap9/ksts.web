import { Component, inject } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { SharedImports } from '@/app/shared/import.shared';
import { BaseComponent } from '@/app/shared/components/base/base-component';
import { Utils } from '@/app/shared/utils';
import { AuthConstants } from '@/app/shared/constants/auth.constants';
import { AuthService } from '@/app/service/auth/auth.service';

@Component({
    selector: 'app-login',
    imports: [...SharedImports],
    templateUrl: './login.html',
    styleUrl: './login.scss'
})
export class Login extends BaseComponent {
    private _authService = inject(AuthService);

    showPassword = false;

    override form = new FormGroup({
        username: new FormControl('', [Validators.required]),
        password: new FormControl('', [Validators.required])
    });

    override ValidationMessages: Record<string, Record<string, string>> = {
        username: { required: 'Không được bỏ trống' },
        password: { required: 'Không được bỏ trống' }
    };

    /** Nhớ trang người dùng định vào trước khi bị chặn, để đăng nhập xong quay lại đúng chỗ đó. */
    override ngOnInit(): void {
        this._activatedRoute.queryParamMap.subscribe((params) => {
            const redirectUri = params.get('redirect_uri') || '/';
            Utils.setSessionStorage(AuthConstants.REDIRECT_URI_AFTER_LOGIN, redirectUri);
        });
    }

    /** Gửi tài khoản/mật khẩu; điều hướng sau khi thành công do AuthService lo. */
    onSubmit() {
        if (this.isFormInvalid()) {
            return;
        }

        this.loading.set(true);
        this._authService
            .login(this.form.value.username!, this.form.value.password!)
            .subscribe({
                next: () => this.messageSuccess('Đăng nhập thành công!'),
                error: (err) => {
                    const msg = err?.error?.error_description || 'Sai tài khoản hoặc mật khẩu.';
                    this.messageError(msg);
                }
            })
            .add(() => {
                this.loading.set(false);
            });
    }

    focusPassword() {
        document.getElementById('password')?.focus();
    }

    focusLogin() {
        document.getElementById('login')?.focus();
    }

    hiddenPassword() {
        this.showPassword = !this.showPassword;
    }
}
