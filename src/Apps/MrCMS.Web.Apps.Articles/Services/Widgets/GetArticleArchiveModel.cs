﻿using System.Threading.Tasks;
using MrCMS.Services.Widgets;
using MrCMS.Web.Apps.Articles.Models;
using MrCMS.Web.Apps.Articles.Widgets;

namespace MrCMS.Web.Apps.Articles.Services.Widgets
{
    public class GetArticleArchiveModel : GetWidgetModelBase<ArticleArchive>
    {
        private readonly IArticleArchiveService _articleArchiveService;

        public GetArticleArchiveModel(IArticleArchiveService articleArchiveService)
        {
            _articleArchiveService = articleArchiveService;
        }

        public override async Task<object> GetModel(ArticleArchive widget)
        {
            var model = new ArticleArchiveModel
            {
                ArticleYearsAndMonths = await _articleArchiveService.GetMonthsAndYears(widget.ArticleList),
                ArticleList = widget.ArticleList,
                ArticleArchive = widget
            };

            return model;
        }
    }
}
