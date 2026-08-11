import moment from 'moment';
import { jwtDecode } from 'jwt-decode';
import { FormGroup } from '@angular/forms';
import { IJwtPayload } from './models/jwt-payload.models';
import { AuthConstants } from './constants/auth.constants';

export class Utils {
    public static getLocalStorage(key: string) {
        try {
            return JSON.parse(localStorage.getItem(key) || '');
        } catch {
            return {};
        }
    }

    public static setLocalStorage(key: string, data: any) {
        localStorage.setItem(key, JSON.stringify(data));
    }

    public static removeLocalStorage(key: string) {
        localStorage.removeItem(key);
    }

    public static clearLocalStorage() {
        localStorage.clear();
    }

    public static setSessionStorage(key: string, data: any) {
        sessionStorage.setItem(key, data);
    }

    public static getSessionStorage(key: string) {
        return sessionStorage.getItem(key);
    }

    public static removeSessionStorage(key: string) {
        sessionStorage.removeItem(key);
    }

    public static clearSessionStorage() {
        sessionStorage.clear();
    }

    public static getAccessToken(): string | undefined {
        return this.getLocalStorage(AuthConstants.STORAGE_AUTH)?.accessToken;
    }

    public static getRefreshToken(): string | undefined {
        return this.getLocalStorage(AuthConstants.STORAGE_AUTH)?.refreshToken;
    }

    /**
     * Trả null khi chưa đăng nhập hoặc token hỏng, KHÔNG ném lỗi: hàm này được gọi trong guard,
     * ném ở đó là chặn cứng luồng điều hướng thay vì đẩy người dùng về màn đăng nhập.
     */
    public static getDecodedJwtPayload(): IJwtPayload | null {
        const token = this.getAccessToken();
        if (!token) {
            return null;
        }
        try {
            return jwtDecode<IJwtPayload>(token);
        } catch {
            return null;
        }
    }

    public static refreshData(data: any) {
        return JSON.parse(JSON.stringify(data));
    }

    public static replaceAll(str: string, find: string, replace: string) {
        const escapedFind = find.replace(/([.*+?^=!:${}()|\[\]\/\\])/g, '\\$1');
        return str.replace(new RegExp(escapedFind, 'g'), replace);
    }

    public static log(title: string, data?: any) {
        console.log(`%c ${title} `, 'background:black; color: red', data);
    }

    public static transformMoney(num: number, locales = 'vi-VN'): string {
        if (num === null || typeof num === 'undefined') {
            return '';
        }
        const result = new Intl.NumberFormat(locales).format(Number(num));
        return result === 'NaN' ? '' : result;
    }

    public static convertVietnameseToEng(str: string) {
        str = str.replace(/à|á|ạ|ả|ã|â|ầ|ấ|ậ|ẩ|ẫ|ă|ằ|ắ|ặ|ẳ|ẵ/g, 'a');
        str = str.replace(/è|é|ẹ|ẻ|ẽ|ê|ề|ế|ệ|ể|ễ/g, 'e');
        str = str.replace(/ì|í|ị|ỉ|ĩ/g, 'i');
        str = str.replace(/ò|ó|ọ|ỏ|õ|ô|ồ|ố|ộ|ổ|ỗ|ơ|ờ|ớ|ợ|ở|ỡ/g, 'o');
        str = str.replace(/ù|ú|ụ|ủ|ũ|ư|ừ|ứ|ự|ử|ữ/g, 'u');
        str = str.replace(/ỳ|ý|ỵ|ỷ|ỹ/g, 'y');
        str = str.replace(/đ/g, 'd');
        str = str.replace(/À|Á|Ạ|Ả|Ã|Â|Ầ|Ấ|Ậ|Ẩ|Ẫ|Ă|Ằ|Ắ|Ặ|Ẳ|Ẵ/g, 'A');
        str = str.replace(/È|É|Ẹ|Ẻ|Ẽ|Ê|Ề|Ế|Ệ|Ể|Ễ/g, 'E');
        str = str.replace(/Ì|Í|Ị|Ỉ|Ĩ/g, 'I');
        str = str.replace(/Ò|Ó|Ọ|Ỏ|Õ|Ô|Ồ|Ố|Ộ|Ổ|Ỗ|Ơ|Ờ|Ớ|Ợ|Ở|Ỡ/g, 'O');
        str = str.replace(/Ù|Ú|Ụ|Ủ|Ũ|Ư|Ừ|Ứ|Ự|Ử|Ữ/g, 'U');
        str = str.replace(/Ỳ|Ý|Ỵ|Ỷ|Ỹ/g, 'Y');
        str = str.replace(/Đ/g, 'D');
        return str;
    }

    public static formatDateCallApi(date: string | Date | null | undefined, format = 'YYYY-MM-DDTHH:mm:ss') {
        return moment(date).isValid() ? moment(date).format(format) : '';
    }

    public static formatDateView(date: string | Date | null | undefined, format = 'DD/MM/YYYY') {
        return moment(date).isValid() ? moment(date).format(format) : '';
    }

    public static trimFormGroup(formGroup: FormGroup): void {
        for (const controlName in formGroup.controls) {
            const control = formGroup.get(controlName);
            if (control && typeof control.value === 'string') {
                control.setValue(control.value.trim());
            }
        }
    }
}
