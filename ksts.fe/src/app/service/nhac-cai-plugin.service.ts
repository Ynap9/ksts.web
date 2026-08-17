
import { Injectable, signal } from '@angular/core';
import { PLUGIN_KHONG_NHAC_LAI_KEY } from '../shared/constants/chung-thu-so.constants';
import { Utils } from '../shared/utils';

@Injectable({
    providedIn: 'root'
})
export class NhacCaiPluginService {
    daTat = signal<boolean>(Utils.getLocalStorage(PLUGIN_KHONG_NHAC_LAI_KEY) === true);

    tat() {
        this.daTat.set(true);
        Utils.setLocalStorage(PLUGIN_KHONG_NHAC_LAI_KEY, true);
    }
}
