using System;
using System.Reflection;
using MrCMS.Data;
using MrCMS.Entities.Widget;
using MrCMS.Models;
using MrCMS.Website;

namespace MrCMS.Services
{
    public class GetWidgetCachingInfo : IGetWidgetCachingInfo
    {
        private readonly IGetCurrentPage _getCurrentPage;
        private readonly IGetCurrentUserGuid _getCurrentUserGuid;
        private readonly IRepository<Widget> _repository;

        public GetWidgetCachingInfo(IGetCurrentPage getCurrentPage, IGetCurrentUserGuid getCurrentUserGuid, IRepository<Widget> repository)
        {
            _getCurrentPage = getCurrentPage;
            _getCurrentUserGuid = getCurrentUserGuid;
            _repository = repository;
        }

        public CachingInfo Get(int id)
        {
            return Get(_repository.LoadSync(id));
        }

        public CachingInfo Get(Widget widget)
        {
            //using (MiniProfiler.Current.Step("Get caching info"))
            {
                if (widget == null)
                {
                    return CachingInfo.DoNotCache;
                }
                var attribute = widget.GetType().GetCustomAttribute<WidgetOutputCacheableAttribute>();
                if (attribute == null)
                {
                    return CachingInfo.DoNotCache;
                }
                var shouldCache = widget.Cache && widget.CacheLength > 0;
                return new CachingInfo(shouldCache, GetCacheKey(widget, attribute),
                    TimeSpan.FromSeconds(widget.CacheLength),
                    widget.CacheExpiryType);
            }
        }

        private string GetCacheKey(Widget widget, WidgetOutputCacheableAttribute attribute)
        {
            var cacheKey = "Widget." + widget.Id;
            if (attribute.PerPage)
            {
                var page = _getCurrentPage.GetPage();
                if (page != null)
                {
                    cacheKey += ".Page:" + page.Id;
                }
            }
            if (attribute.PerUser)
            {
                cacheKey += ".User:" + (_getCurrentUserGuid.Get());
            }
            return cacheKey;
        }

    }
}