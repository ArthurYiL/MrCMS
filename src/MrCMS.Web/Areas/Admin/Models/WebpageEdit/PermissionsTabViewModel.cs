﻿using MrCMS.Entities.Documents.Web;

namespace MrCMS.Web.Areas.Admin.Models.WebpageEdit
{
    public class PermissionsTabViewModel
    {
        public int Id { get; set; }
        public bool HasCustomPermissions { get; set; }
        public bool InheritFrontEndRolesFromParent { get; set; }
        public WebpagePermissionType PermissionType { get; set; }
        public string FrontEndRoles { get; set; }
        public string Password { get; set; }
    }

}