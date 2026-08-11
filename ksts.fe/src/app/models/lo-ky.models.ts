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

    /** Thư mục trên kho chứa bản đã ký; bản ký được đẩy lên ngay trong lúc ký. */
    tienToKho?: string | null;

    /** CHỈ file lỗi, để bảng tra nguyên nhân theo thứ tự dòng. */
    filesLoi: IViewFileKy[];
}

export interface IYeuCauKy {
    yeuCauId: string;
    duLieuBase64: string;
}

export interface IKetQuaKy {
    yeuCauId: string;
    chuKyBase64?: string | null;
    loi?: string | null;
}

export interface IMoPhienKetQua {
    thumbprint: string;
    commonName: string;
    chungThuBase64: string;
}
