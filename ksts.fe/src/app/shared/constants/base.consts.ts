export type TagSeverity = 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast';

export enum EStatusResonse {
    SUCCESS = 1,
    ERROR = 0
}

export enum EFormatDate {
    DATE_TIME_SECOND = 'DD-MM-YYYY HH:mm:ss',
    DATE_TIME = 'DD-MM-YYYY HH:mm',
    DATE = 'DD-MM-YYYY'
}

export enum EFormatDateDisplay {
    DATE_TIME_SECOND = 'DD/MM/YYYY HH:mm:ss',
    DATE_TIME = 'DD/MM/YYYY HH:mm',
    DATE = 'DD/MM/YYYY'
}

export class BaseConsts {
    static readonly imageExtensions = ['jpg', 'jpeg', 'png'];
    static readonly imageExtensionStrings = '.jpg, .jpeg, .png';

    static readonly pageContentId = 'page-content';
    static readonly localStorageUser = 'userInfo';
    static readonly DEBOUNCE_TIME = 1200;

    static readonly messageOnUpload = 'Đang xử lý...';
    static readonly emptyMessage = 'Không có dữ liệu';

    static readonly maxSizeUpload = 5242880;
    static readonly PAGINATOR_MIN_LENGTH_SHOW = 25;
}

export class StatusResponseConst {
    public static RESPONSE_TRUE = 1;
    public static RESPONSE_FALSE = 0;
    public static HTTP_STATUS_OK = 200;
}
