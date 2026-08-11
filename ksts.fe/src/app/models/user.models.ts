import { IBaseRequestPaging } from "../shared/models/request-paging.base.models";

export interface IViewRowUser {
  id?: string;
  userName?: string;
  email?: string;
  msAccount?: string;
  phoneNumber?: string | null;
  fullName?: string;
  emailConfirmed?: boolean;
  phoneNumberConfirmed?: boolean;
  createdAt?: string; // or Date if you plan to parse it
  roles?: string[];
}

export interface IViewUser extends IViewRowUser {
  passwordRandom?: string
  permissions?: string[]
}

export interface ICreateUser {
  userName?: string,
  email?: string,
  phoneNumber?: string,
  password?: string,
  fullName?: string,
  roleNames?: string[],
  msAccount?: string
}

export interface IUpdateUser extends ICreateUser {
  id: string
}

export interface IGetListUsers {
  id: string,
  userName: string,
  email: string,
  fullName: string,
}

export interface IFindPagingUser extends IBaseRequestPaging {}

export interface IUserLogin {
    id?: string;
    username?: string;
    fullName?: string;
    email?: string;
}
