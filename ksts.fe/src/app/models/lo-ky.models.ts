export type TrangThaiFileKy = 'cho' | 'dangKy' | 'xong' | 'loi';

export interface IViewLoKy {
    id: number;
    templateId: number;
    thumbprint?: string | null;
    taiToken: string;
    trangThai: string;
    tongSo: number;
    daXong: number;
    soLoi: number;
    createdDate?: string;
}

export interface IViewFileKy {
    id: number;
    thuTu: number;
    tenFile: string;
    trangThai: TrangThaiFileKy;
    lyDoLoi?: string | null;
    thoiGianKy?: string | null;
    dauThoiGian?: string | null;
}

export interface IViewTienDoLoKy {
    id: number;
    trangThai: string;
    taiToken: string;
    tongSo: number;
    daXong: number;
    soLoi: number;
    dangChay: boolean;
    hoanTat: boolean;
    loiChung?: string | null;

    /** Tiến độ đẩy bản đã ký lên kho — bộ đếm riêng, độc lập với tiến độ ký. */
    dangDayLenKho: boolean;
    daDayLenKho: number;
    soLoiDayLenKho: number;
    hoanTatDayLenKho: boolean;
    loiDayLenKho?: string | null;
    tienToKho?: string | null;

    files: IViewFileKy[];
}
