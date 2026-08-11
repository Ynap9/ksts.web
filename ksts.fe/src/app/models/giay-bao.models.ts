export interface IExcelSheet {
    name: string;
    rowCount: number;
}

export interface IViewThiSinh {
    soVanBan: string;
    hoTen: string;
}

export interface IDongLoi {
    thuTu: number;
    lyDo: string;
}

export interface IZipJob {
    jobId: string;
    taiToken: string;
    tongSo: number;
    daXong: number;
    soLoi: number;
    hoanTat: boolean;
    loiChung: string | null;

    /** Tổng dung lượng đã đẩy lên kho, không phải dung lượng file nén — file nén chỉ dựng lúc tải. */
    dungLuong: number;

    /** Thư mục trên kho chứa giấy báo của lô; giấy báo được đẩy lên ngay trong lúc dựng. */
    tienToKho: string | null;

    /** CHỈ dòng lỗi, để bảng tra nguyên nhân theo thứ tự dòng. */
    dongLoi: IDongLoi[];
}
