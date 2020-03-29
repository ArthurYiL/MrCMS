﻿using System.Collections.Generic;
using AutoMapper;
using MrCMS.Entities.Documents.Web;
using MrCMS.Web.Areas.Admin.Models.WebpageEdit;
using MrCMS.Web.Areas.Admin.Services;

namespace MrCMS.Web.Areas.Admin.Mapping
{
    public class FrontEndAllowedRoleMapper : IValueResolver<PermissionsTabViewModel, Webpage, IList<FrontEndAllowedRole>>
    {
        private readonly IDocumentRolesAdminService _documentRolesAdminService;

        public FrontEndAllowedRoleMapper(IDocumentRolesAdminService documentRolesAdminService)
        {
            _documentRolesAdminService = documentRolesAdminService;
        }

        public IList<FrontEndAllowedRole> Resolve(PermissionsTabViewModel source, Webpage destination, IList<FrontEndAllowedRole> destMember, ResolutionContext context)
        {
            // if should not be set
            if (!source.HasCustomPermissions || source.InheritFrontEndRolesFromParent || source.PermissionType != WebpagePermissionType.RoleBased)
            {
                // return empty collection
                return new List<FrontEndAllowedRole>();
            }
            return _documentRolesAdminService.GetFrontEndRoles(destination, source.FrontEndRoles, source.InheritFrontEndRolesFromParent);
        }
    }
}