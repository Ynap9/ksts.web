import { IBaseRequestPaging } from "../shared/models/request-paging.base.models";

export interface IViewRowRole {
  id?: string;
  name?: string;
  permissionKey?: string[];
}

export interface ICreateRole {
  name: string,
  permissionKey: string[]
}

export interface IUpdateRole extends ICreateRole {
  id: string
}

export interface IFindPagingRole extends IBaseRequestPaging {}