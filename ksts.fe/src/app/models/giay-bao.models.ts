export interface IExcelSheet {
    name: string;
    rowCount: number;
}

export interface IViewThiSinh {
    soVanBan: string;
    hoTen: string;
}

export type TrangThaiThiSinh = 'cho' | 'dangXuLy' | 'xong' | 'loi';

export interface IDongThiSinh extends IViewThiSinh {
    trangThai: TrangThaiThiSinh;
}

export interface IZipJob {
    jobId: string;
    taiToken: string;
    tongSo: number;
    daXong: number;
    soLoi: number;
    hoanTat: boolean;
    loiChung: string | null;
    dungLuong: number;

    /** Đẩy lên kho object — bộ đếm riêng, độc lập với khâu dựng và với việc tải zip về. */
    dangDayLenKho: boolean;
    daDayLenKho: number;
    soLoiDayLenKho: number;
    hoanTatDayLenKho: boolean;
    loiDayLenKho: string | null;
    tienToKho: string | null;
}
