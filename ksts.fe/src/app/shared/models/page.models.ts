export class Page {
    pageSizeAll = -1;

    perPageOptions: number[] = [25, 50, 100, 200];

    pageSize: number = this.perPageOptions[0];

    totalItems: number = 0;

    totalPages: number = 0;

    pageNumber: number = 0;

    keyword: string = '';

    getPageNumber() {
        return this.pageNumber + 1;
    }

    getPageSize() {
        return this.pageSize;
    }
}
