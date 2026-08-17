import { IBaseRequestPaging } from "../shared/models/request-paging.base.models";

export interface ITemplatePosition {
  kind: number;
  pageNumber: number;
  xRatio: number;
  yRatio: number;
  widthRatio: number;
  heightRatio: number;
}

export interface IViewRowTemplate {
  id?: number;
  tenTemplate?: string;
  createdBy?: string | null;
  createdDate?: string;
}

export interface IViewTemplate extends IViewRowTemplate {
  thumbprint?: string;
  tenChungThu?: string | null;
  lyDoKy?: string | null;
  noiKy?: string | null;
  anhDauDoUrl?: string | null;
  anhChuKyTuoiUrl?: string | null;
  hienThiChuKySo?: boolean;
  nhoiChuKySoVaoAnh?: boolean;
  kyDe?: boolean;
  doDamDauDo?: number;
  doDamChuKyTuoi?: number;
  doDayNetChuKyTuoi?: number;
  mauChuKySo?: string;
  mauChuKyTuoi?: string | null;
  positions?: ITemplatePosition[];
}

export interface IViewFileMau {
  fileName?: string;
  exists?: boolean;
}

export interface IConfigTemplate {
  id: number;
  thumbprint: string;
  tenChungThu?: string | null;
  lyDoKy?: string | null;
  noiKy?: string | null;
  hienThiChuKySo: boolean;
  nhoiChuKySoVaoAnh: boolean;
  kyDe: boolean;
  doDamDauDo: number;
  doDamChuKyTuoi: number;
  doDayNetChuKyTuoi: number;
  mauChuKySo: string;
  mauChuKyTuoi: string | null;
  anhDauDo?: File | null;
  anhChuKyTuoi?: File | null;
  xoaAnhDauDo?: boolean;
  xoaAnhChuKyTuoi?: boolean;
  positions: ITemplatePosition[];
}

export interface ICreateTemplate {
  tenTemplate: string;
}

export interface IUpdateTemplate extends ICreateTemplate {
  id: number;
}

export interface IFindPagingTemplate extends IBaseRequestPaging {}
