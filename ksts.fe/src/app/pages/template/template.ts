
import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { MenuItem } from 'primeng/api';
import { PaginatorState } from 'primeng/paginator';
import { Breadcrumb } from '@/app/shared/components/breadcrumb/breadcrumb';
import { Create } from './create/create';
import { TblAction, TblActionTypes } from './tbl-action/tbl-action';
import { BaseComponent } from '@/app/shared/components/base/base-component';
import { IViewRowTemplate, IFindPagingTemplate } from '@/app/models/template.models';
import { TemplateService } from '@/app/service/template.service';
import { DataTable } from '@/app/shared/components/data-table/data-table';
import { CellViewTypes } from '@/app/shared/constants/data-table.constants';
import { SharedImports } from '@/app/shared/import.shared';
import { IColumn } from '@/app/shared/models/data-table.models';

@Component({
    selector: 'app-template',
    imports: [SharedImports, DataTable, Breadcrumb],
    templateUrl: './template.html',
    styleUrl: './template.scss'
})
export class Template extends BaseComponent {
    _templateService = inject(TemplateService);

    searchForm: FormGroup = new FormGroup({
        search: new FormControl('')
    });

    breadcrumbHome: MenuItem = { icon: 'pi pi-home', routerLink: '/' };
    breadcrumbItems: MenuItem[] = [{ label: 'Template' }];

    columns: IColumn[] = [
        { header: 'STT', cellViewType: CellViewTypes.INDEX, headerContainerStyle: 'width: 6rem' },
        { header: 'Tên template', field: 'tenTemplate', headerContainerStyle: 'min-width: 14rem', clickable: true, cellClass: 'text-blue-600 font-medium' },
        { header: 'Ngày tạo', field: 'createdDate', cellViewType: CellViewTypes.DATE, dateFormat: 'dd/MM/yyyy HH:mm', headerContainerStyle: 'min-width: 10rem' },
        { header: 'Người tạo', field: 'createdBy', headerContainerStyle: 'min-width: 10rem' },
        { header: 'Thao tác', headerContainerStyle: 'width: 6rem', cellViewType: CellViewTypes.CUSTOM_COMP, customComponent: TblAction }
    ];

    data = signal<IViewRowTemplate[]>([]);
    query: IFindPagingTemplate = {
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
        this._templateService
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
        const ref = this._dialogService.open(Create, { header: 'Tạo template', closable: true, modal: true, styleClass: 'w-[700px]', focusOnShow: false });
        ref?.onClose.subscribe((result) => {
            if (result) {
                this.getData();
            }
        });
    }

    onOpenUpdate(data: IViewRowTemplate) {
        const ref = this._dialogService.open(Create, { header: 'Cập nhật template', closable: true, modal: true, styleClass: 'w-[700px]', focusOnShow: false, data });
        ref?.onClose.subscribe((result) => {
            if (result) {
                this.getData();
            }
        });
    }

    onDelete(data: IViewRowTemplate) {
        this.confirmDelete({ header: 'Xoá template', message: `Bạn có chắc chắn muốn xoá template "${data.tenTemplate}"?` }, () => {
            this._templateService.delete(data.id!).subscribe({
                next: (res) => {
                    if (this.isResponseSucceed(res, true, 'Xoá template thành công.')) {
                        this.getData();
                    }
                }
            });
        });
    }

    onOpenChiTiet(data: IViewRowTemplate) {
        this.router.navigate(['/template/config-template', data.id]);
    }

    onCustomEmit(data: { type: string; data: IViewRowTemplate; field?: string }) {
        if (data.type === TblActionTypes.view) {
            this.onOpenChiTiet(data.data);
        } else if (data.type === 'cellClick') {
            this.onOpenChiTiet(data.data);
        } else if (data.type === TblActionTypes.update) {
            this.onOpenUpdate(data.data);
        } else if (data.type === TblActionTypes.delete) {
            this.onDelete(data.data);
        }
    }
}
