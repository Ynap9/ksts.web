
import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { MenuItem } from 'primeng/api';
import { PaginatorState } from 'primeng/paginator';
import { Breadcrumb } from '@/app/shared/components/breadcrumb/breadcrumb';
import { Create } from './create/create';
import { TblAction, TblActionTypes } from './tbl-action/tbl-action';
import { BaseComponent } from '@/app/shared/components/base/base-component';
import { IViewRowUser, IFindPagingUser } from '@/app/models/user.models';
import { UserService } from '@/app/service/user.service';
import { DataTable } from '@/app/shared/components/data-table/data-table';
import { CellViewTypes } from '@/app/shared/constants/data-table.constants';
import { SharedImports } from '@/app/shared/import.shared';
import { IColumn } from '@/app/shared/models/data-table.models';

@Component({
    selector: 'app-user',
    imports: [SharedImports, DataTable, Breadcrumb],
    templateUrl: './user.html',
    styleUrl: './user.scss'
})
export class User extends BaseComponent {
    _userService = inject(UserService);

    searchForm: FormGroup = new FormGroup({
        search: new FormControl('')
    });

    breadcrumbHome: MenuItem = { icon: 'pi pi-home', routerLink: '/' };
    breadcrumbItems: MenuItem[] = [{ label: 'QL Tài khoản' }, { label: 'Người dùng' }];

    columns: IColumn[] = [
        { header: 'STT', cellViewType: CellViewTypes.INDEX, headerContainerStyle: 'width: 6rem' },
        { header: 'Tài khoản', field: 'userName', headerContainerStyle: 'min-width: 10rem' },
        { header: 'Họ tên', field: 'fullName', headerContainerStyle: 'min-width: 10rem' },
        { header: 'Email', field: 'email', headerContainerStyle: 'min-width: 10rem' },
        { header: 'SĐT', field: 'phoneNumber', headerContainerStyle: 'min-width: 10rem' },
        { header: 'Thao tác', headerContainerStyle: 'width: 6rem', cellViewType: CellViewTypes.CUSTOM_COMP, customComponent: TblAction }
    ];

    data = signal<IViewRowUser[]>([]);
    query: IFindPagingUser = {
        pageNumber: this.START_PAGE_NUMBER,
        pageSize: this.MAX_PAGE_SIZE
    };

    override ngOnInit(): void {
        this.getData();
    }

    onSearch() {
        this.getData();
    }

    onPageChanged($event: PaginatorState) {
        this.query.pageNumber = ($event.page ?? 0) + 1;
        this.getData();
    }

    getData() {
        this.loading.set(true);
        this._userService
            .findPaging({ ...this.query, keyword: this.searchForm.get('search')?.value })
            .subscribe({
                next: (res) => {
                    if (this.isResponseSucceed(res, false)) {
                        this.data.set(res.data.items);
                        this.totalRecords.set(res.data.totalItems);
                    }
                }
            })
            .add(() => {
                this.loading.set(false);
            });
    }

    onOpenCreate() {
        const ref = this._dialogService.open(Create, { header: 'Tạo tài khoản', closable: true, modal: true, styleClass: 'w-[700px]', focusOnShow: false });
        ref?.onClose.subscribe((result) => {
            if (result) {
                this.getData();
            }
        });
    }

    onOpenUpdate(data: IViewRowUser) {
        const ref = this._dialogService.open(Create, { header: 'Cập nhật tài khoản', closable: true, modal: true, styleClass: 'w-[700px]', focusOnShow: false, data });
        ref?.onClose.subscribe((result) => {
            if (result) {
                this.getData();
            }
        });
    }

    onCustomEmit(data: { type: string; data: IViewRowUser; field?: string }) {
        if (data.type === TblActionTypes.update) {
            this.onOpenUpdate(data.data);
        } else if (data.type === TblActionTypes.delete) {
        }
    }
}
