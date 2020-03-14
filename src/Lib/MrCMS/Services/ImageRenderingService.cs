using System.Drawing;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using MrCMS.DbConfiguration;
using MrCMS.Entities.Documents.Media;
using MrCMS.Helpers;
using MrCMS.Models;
using MrCMS.Services.Caching;
using MrCMS.Settings;
using MrCMS.Website.Caching;

namespace MrCMS.Services
{
    public class ImageRenderingService : IImageRenderingService
    {
        private readonly ICacheManager _cacheManager;
        private readonly IFileService _fileService;
        private readonly IConfigurationProvider _configurationProvider;
        private readonly IImageProcessor _imageProcessor;

        public ImageRenderingService(IImageProcessor imageProcessor, IFileService fileService,
            IConfigurationProvider configurationProvider, ICacheManager cacheManager)
        {
            _imageProcessor = imageProcessor;
            _fileService = fileService;
            _configurationProvider = configurationProvider;
            _cacheManager = cacheManager;
        }

        public async Task<ImageInfo> GetImageInfo(string imageUrl, Size targetSize)
        {
            var crop = await _imageProcessor.GetCrop(imageUrl);
            if (crop != null)
                return new ImageInfo
                {
                    Title = crop.Title,
                    Description = crop.Description,
                    ImageUrl = await GetCropImageUrl(crop, targetSize)
                };
            var image = await _imageProcessor.GetImage(imageUrl);
            if (image != null)
                return new ImageInfo
                {
                    Title = image.Title,
                    Description = image.Description,
                    ImageUrl = await GetFileImageUrl(image, targetSize)
                };
            return null;
        }


        public async Task<string> GetImageUrl(string imageUrl, Size targetSize)
        {
            var mediaSettings = await _configurationProvider.GetSiteSettings<MediaSettings>();
            var info = mediaSettings.GetImageUrlCachingInfo(imageUrl, targetSize);
            return await _cacheManager.GetOrCreate(info.CacheKey, async () => string.IsNullOrWhiteSpace(imageUrl)
                ? null
                : (await GetImageInfo(imageUrl, targetSize))?.ImageUrl, info.TimeToCache, info.ExpiryType);
        }

        public async Task<IHtmlContent> RenderImage(IHtmlHelper helper, string imageUrl, Size targetSize = default,
            string alt = null,
            string title = null, object attributes = null)
        {
            var mediaSettings = await _configurationProvider.GetSiteSettings<MediaSettings>();
            var cachingInfo = mediaSettings.GetImageTagCachingInfo(imageUrl, targetSize, alt, title, attributes);
            return await helper.GetCached(cachingInfo, async htmlHelper =>
            {
                {
                    if (string.IsNullOrWhiteSpace(imageUrl))
                        return HtmlString.Empty;

                    var imageInfo = await GetImageInfo(imageUrl, targetSize);
                    if (imageInfo == null)
                        return HtmlString.Empty;

                    return ReturnTag(imageInfo, alt, title, attributes);
                }
            });
        }

        private async Task<string> GetFileImageUrl(MediaFile image, Size targetSize)
        {
            return await _fileService.GetFileLocation(image, targetSize, true);
        }

        private async Task<string> GetCropImageUrl(Crop crop, Size targetSize)
        {
            return await _fileService.GetFileLocation(crop, targetSize, true);
        }


        private IHtmlContent ReturnTag(ImageInfo imageInfo, string alt, string title, object attributes)
        {
            var tagBuilder = new TagBuilder("img");
            tagBuilder.Attributes.Add("src", imageInfo.ImageUrl);
            tagBuilder.Attributes.Add("alt", alt ?? imageInfo.Title);
            tagBuilder.Attributes.Add("title", title ?? imageInfo.Description);
            if (attributes != null)
            {
                var routeValueDictionary = MrCMSHtmlHelperExtensions.AnonymousObjectToHtmlAttributes(attributes);
                foreach (var kvp in routeValueDictionary) tagBuilder.Attributes.Add(kvp.Key, kvp.Value.ToString());
            }

            tagBuilder.TagRenderMode = TagRenderMode.SelfClosing;
            return tagBuilder;
        }
    }
}