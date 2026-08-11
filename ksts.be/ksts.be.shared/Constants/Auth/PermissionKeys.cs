using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ksts.be.shared.Constants.Auth
{
     public static class PermissionKeys
    {
        public const string Menu = "Menu.";
        public const string Function = "Function.";


        public const string MenuKySo = Menu + "KySo";
        public const string MenuTemplate = Menu + "Template";
        public const string MenuChungThuSo = Menu + "ChungThuSo";
        public const string MenuImportTuyenSinh = Menu + "ImportTuyenSinh";
        public const string MenuUserManagement = Menu + "UserManagement";
        public const string MenuUserManagementUser = MenuUserManagement + "_User";
        public const string MenuUserManagementRole = MenuUserManagement + "_User";

       

        public const string CategoryUser = "QL User";
        public const string UserAdd = Function + "UserAdd";
        public const string UserUpdate = Function + "UserUpdate";
        public const string UserDelete = Function + "UserDelete";
        public const string UserView = Function + "UserView";
        public const string UserSetRoles = Function + "UserSetRoles";

        public const string CategoryRole = "QL Role";
        public const string RoleAdd = Function + "Add";
        public const string RoleUpdate = Function + "Update";
        public const string RoleDelete = Function + "Delete";
        public const string RoleView = Function + "View";



        public static readonly (string Key, string Name, string Category)[] All =
        {

            (MenuUserManagement, "Menu Quản lý User", "Menu"),
            (MenuUserManagementUser, "Menu Quản lý User - User", "Menu"),
            (MenuUserManagementRole, "Menu Quản lý User - Role", "Menu"),
            (MenuKySo, "Menu Ký số", "Menu"),
            (MenuTemplate, "Menu Quản lý Template Ký số","Menu"),
            (MenuChungThuSo, "Menu Chứng thư số", "Menu"),
            (MenuImportTuyenSinh, "Menu Import data tuyển sinh", "Menu"),






            (UserAdd, "Thêm user", CategoryUser),
            (UserUpdate, "Cập nhật User" , CategoryUser),
            (UserDelete, "Xoá User" , CategoryUser),
            (UserView, "Xem User" , CategoryUser),
            (UserSetRoles, "Gán role cho User" , CategoryUser),

            (RoleAdd, "Thêm Role", CategoryRole),
            (RoleUpdate, "Cập nhật Role", CategoryRole),
            (RoleDelete, "Xoá Role", CategoryRole),
            (RoleView, "Xem Role", CategoryRole),


        };



    }
}
