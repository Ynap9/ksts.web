import { Directive, inject, OnInit, signal } from '@angular/core';
import { AbstractControl, FormGroup } from '@angular/forms';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { ConfirmationService, MessageService } from 'primeng/api';
import { DialogService } from 'primeng/dynamicdialog';
import { IBaseResponse } from '../../models/request-paging.base.models';
import { Page } from '../../models/page.models';
import { Utils } from '../../utils';
import { AuthConstants } from '../../constants/auth.constants';
import { IUserLogin } from '../../../models/user.models';
import { filter } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Directive()
export abstract class BaseComponent implements OnInit {
    protected router = inject(Router);
    protected _activatedRoute = inject(ActivatedRoute);
    protected _messageService = inject(MessageService);
    protected _dialogService = inject(DialogService);
    protected _confirmationService = inject(ConfirmationService);

    ValidationMessages: Record<string, Record<string, string>> = {};
    form!: FormGroup;
    loading = signal<boolean>(false);
    onClickSubmit: boolean = false;
    page: Page = new Page();
    MAX_PAGE_SIZE = 10;
    START_PAGE_NUMBER = 1;
    totalRecords = signal<number>(0);
    constructor() {
        this.router.events.pipe(
            filter(event => event instanceof NavigationEnd),
            takeUntilDestroyed()
        ).subscribe((event: any) => {
            // Chỉ gọi onRouteActivated khi NavigationEnd đến đúng route của component này
            const activatedUrl = this._activatedRoute.snapshot.pathFromRoot
                .map(r => r.url.map(s => s.path).join('/'))
                .filter(p => p)
                .join('/');

            const currentUrl = event.urlAfterRedirects.split('?')[0].replace(/^\//, '');

            if (activatedUrl && currentUrl.startsWith(activatedUrl)) {
                this.onRouteActivated();
            }
        });
    }


    ngOnInit(): void { }
    onRouteActivated(): void { }

    /** Đánh dấu chạm toàn bộ control để lộ lỗi rồi báo form có hợp lệ không. */
    isFormInvalid(): boolean {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return true;
        }
        return false;
    }

    /** Thông tin người đang đăng nhập, ưu tiên bản đã lưu; không có thì suy từ token. */
    getUser(): IUserLogin {
        const userInfo = Utils.getLocalStorage(AuthConstants.STORAGE_USER);
        if (userInfo?.username) {
            return userInfo;
        }

        const payload = Utils.getDecodedJwtPayload();
        if (!payload) {
            return {};
        }

        return {
            id: payload.user_id ?? payload.sub,
            username: payload.username,
            fullName: payload.name,
            email: payload.email
        };
    }

    /**
     * Gate bắt buộc trước khi đọc res.data. Envelope của BE luôn trả HTTP 200, trạng thái thật nằm ở
     * res.status - không gate ở đây thì lỗi nghiệp vụ sẽ trôi vào màn hình như dữ liệu rỗng.
     */
    isResponseSucceed(res: IBaseResponse, isShowErrorMsg = true, successMsg = ''): boolean {
        if (res && res.status === 1) {
            if (successMsg) {
                this.messageSuccess(successMsg);
            }
            return true;
        }

        if (isShowErrorMsg) {
            this.messageError(res?.message || 'Có sự cố xảy ra. Vui lòng thử lại sau.');
        }
        return false;
    }

    /** Câu lỗi hiển thị cho một control, lấy từ bảng ValidationMessages của màn hình. */
    getErrorMessage(control: AbstractControl | null, fieldName: string): string | null {
        if (!control || !control.errors || !control.touched) {
            return null;
        }

        const messages = this.ValidationMessages[fieldName];
        if (!messages) {
            return null;
        }

        for (const errorKey of Object.keys(control.errors)) {
            if (messages[errorKey]) {
                return messages[errorKey];
            }
        }
        return null;
    }

    getError(field: string): string | null {
        return this.getErrorMessage(this.form.get(field), field);
    }

    messageSuccess(msg: string) {
        this._messageService.add({ closable: true, severity: 'success', detail: msg, life: 4000 });
    }

    messageWarning(msg: string) {
        this._messageService.add({ closable: true, severity: 'warn', detail: msg, life: 4000 });
    }

    messageError(msg: string) {
        this._messageService.add({
            closable: true,
            severity: 'error',
            detail: msg || 'Có sự cố xảy ra. Vui lòng thử lại sau.',
            life: 4000
        });
    }

    confirmDelete(opt: { header: string; message: string }, acceptCallback = () => { }) {
        this._confirmationService.confirm({
            message: opt.message,
            header: opt.header,
            closable: true,
            closeOnEscape: true,
            icon: 'pi pi-exclamation-triangle',
            rejectButtonProps: { label: 'Thoát', severity: 'secondary', outlined: true },
            acceptButtonProps: { label: 'Xoá', severity: 'danger' },
            accept: () => acceptCallback()
        });
    }

    confirmAction(opt: { header: string; message: string }, acceptCallback = () => { }) {
        this._confirmationService.confirm({
            message: opt.message,
            header: opt.header,
            closable: true,
            closeOnEscape: true,
            rejectButtonProps: { label: 'Thoát', severity: 'secondary', outlined: true },
            acceptButtonProps: { label: 'Tiếp tục' },
            accept: () => acceptCallback()
        });
    }
}
