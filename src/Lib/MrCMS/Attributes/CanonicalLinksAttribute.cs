﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using MrCMS.Entities.Documents.Web;
using MrCMS.Helpers;
using MrCMS.Models;
using MrCMS.Services;
using MrCMS.Services.Canonical;
using X.PagedList;

namespace MrCMS.Attributes
{
    public class CanonicalLinksAttribute : ActionFilterAttribute
    {
        private readonly string _pagedDataKey;
        private readonly PageInfoMethod _setPageInfo;
        public CanonicalLinksAttribute()
        {
            Order = 0;
        }

        public CanonicalLinksAttribute(string pagedDataKey = null, PageInfoMethod setPageInfo = PageInfoMethod.SetFromPage)
        {
            _pagedDataKey = pagedDataKey;
            _setPageInfo = setPageInfo;
            Order = 1;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var filterContext = await next();
            if (!(filterContext.Result is ViewResult viewResult))
            {
                return;
            }

            if (!(viewResult.Model is Webpage webpage))
            {
                return;
            }

            ViewDataDictionary viewData = viewResult.ViewData;
            var serviceProvider = filterContext.HttpContext.RequestServices;
            if (string.IsNullOrWhiteSpace(_pagedDataKey))
            {
                await SetCanonicalUrl(serviceProvider.GetRequiredService<IGetLiveUrl>(), viewData, webpage);
            }
            else
            {
                if (!viewData.ContainsKey(_pagedDataKey))
                {
                    return;
                }

                if (!(viewData[_pagedDataKey] is PagedListMetaData pagedListMetaData))
                {
                    return;
                }

                IGetPrevAndNextRelTags getTags = serviceProvider.GetRequiredService<IGetPrevAndNextRelTags>();
                await SetPrevAndNext(viewData, webpage, pagedListMetaData, getTags);

                if (_setPageInfo != PageInfoMethod.DoNothing && pagedListMetaData.PageNumber > 1)
                {
                    var title = viewData[MrCMSPageExtensions.PageTitleKey] as string;
                    var description = viewData[MrCMSPageExtensions.PageDescriptionKey] as string;

                    switch (_setPageInfo)
                    {
                        case PageInfoMethod.SetFromPage:
                            if (string.IsNullOrWhiteSpace(title))
                            {
                                viewData[MrCMSPageExtensions.PageTitleKey] = $"Page {pagedListMetaData.PageNumber} of {pagedListMetaData.PageCount} for { webpage.GetPageTitle()}";
                            }

                            if (string.IsNullOrWhiteSpace(description))
                            {
                                viewData[MrCMSPageExtensions.PageDescriptionKey] = string.Empty;
                            }

                            break;
                        case PageInfoMethod.SetFromViewData:
                            if (!string.IsNullOrWhiteSpace(title))
                            {
                                viewData[MrCMSPageExtensions.PageTitleKey] =
                                    $"Page {pagedListMetaData.PageNumber} of {pagedListMetaData.PageCount} for {title}";
                            }

                            if (string.IsNullOrWhiteSpace(description))
                            {
                                viewData[MrCMSPageExtensions.PageDescriptionKey] = string.Empty;
                            }

                            break;
                    }
                }
            }
        }

        private async Task SetCanonicalUrl(IGetLiveUrl getLiveUrl, ViewDataDictionary viewData, Webpage webpage)
        {
            var canonicalLink = await getLiveUrl.GetAbsoluteUrl(webpage);
            if (!string.IsNullOrWhiteSpace(webpage.ExplicitCanonicalLink))
            {
                canonicalLink = webpage.ExplicitCanonicalLink;
            }

            viewData.LinkTags().Add(LinkTag.Canonical, canonicalLink);
        }

        private async Task SetPrevAndNext(ViewDataDictionary viewData, Webpage webpage, PagedListMetaData metadata, IGetPrevAndNextRelTags getTags)
        {
            var prev = await getTags.GetPrev(webpage, metadata, viewData);
            if (!string.IsNullOrWhiteSpace(prev))
            {
                viewData.LinkTags().Add(LinkTag.Prev, prev);
            }

            var next = await getTags.GetNext(webpage, metadata, viewData);
            if (!string.IsNullOrWhiteSpace(next))
            {
                viewData.LinkTags().Add(LinkTag.Next, next);
            }
        }
    }
    public enum PageInfoMethod
    {
        SetFromPage,
        SetFromViewData,
        DoNothing
    }

}
