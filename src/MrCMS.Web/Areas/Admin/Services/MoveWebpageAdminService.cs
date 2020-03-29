using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using MrCMS.Data;
using MrCMS.Entities.Documents;
using MrCMS.Entities.Documents.Web;
using MrCMS.Helpers;
using MrCMS.Models;
using MrCMS.Services;
using MrCMS.Services.Resources;
using MrCMS.Web.Areas.Admin.Models;
using MrCMS.Website;

namespace MrCMS.Web.Areas.Admin.Services
{
    public class MoveWebpageAdminService : IMoveWebpageAdminService
    {
        private readonly IRepository<Webpage> _webpageRepository;
        private readonly IStringResourceProvider _resourceProvider;
        private readonly IWebpageUrlService _webpageUrlService;
        private readonly ICreateUpdateUrlBatch _createUpdateUrlBatch;
        private readonly IActivePagesLoader _activePagesLoader;

        public MoveWebpageAdminService(IRepository<Webpage> webpageRepository, IStringResourceProvider resourceProvider, IWebpageUrlService webpageUrlService, ICreateUpdateUrlBatch createUpdateUrlBatch,
            IActivePagesLoader activePagesLoader)
        {
            _webpageRepository = webpageRepository;
            _resourceProvider = resourceProvider;
            _webpageUrlService = webpageUrlService;
            _createUpdateUrlBatch = createUpdateUrlBatch;
            _activePagesLoader = activePagesLoader;
        }

        public async Task<IEnumerable<SelectListItem>> GetValidParents(Webpage webpage)
        {
            var webpages = await GetValidParentWebpages(webpage);
            List<SelectListItem> result = webpages
                .BuildSelectItemList(page => string.Format("{0} ({1})", page.Name, page.GetMetadata().Name),
                    page => page.Id.ToString(),
                    webpage1 => webpage.Parent != null && webpage.ParentId == webpage1.Id, emptyItem: null);

            if (IsRootAllowed(webpage))
            {
                result.Insert(0, SelectListItemHelper.EmptyItem("Root"));
            }

            return result;
        }

        private async Task<IOrderedEnumerable<Webpage>> GetValidParentWebpages(Webpage webpage)
        {
            List<DocumentMetadata> validParentTypes = DocumentMetadataHelper.GetValidParentTypes(webpage);

            List<string> validParentTypeNames =
                validParentTypes.Select(documentMetadata => documentMetadata.Type.FullName).ToList();
            IList<Webpage> potentialParents =
                _webpageRepository.Query()
                    .Where(page => validParentTypeNames.Contains(page.DocumentClrType))
                    .ToList();

            var validParentWebpages = new List<Webpage>();
            foreach (var potentialParent in potentialParents)
            {
                var activePages = await _activePagesLoader.GetActivePages(potentialParent);
                if (activePages.Any(x => x.Id == potentialParent.Id))
                    validParentWebpages.Add(potentialParent);
            }
            return validParentWebpages.OrderBy(x => x.Name);
        }

        private async Task<bool> SetParent(Webpage webpage, Webpage parent)
        {
            if (webpage == null)
            {
                return false;
            }

            webpage.Parent = parent;

            await _webpageRepository.Update(webpage);
            return true;
        }

        public async Task<MoveWebpageResult> Validate(MoveWebpageModel moveWebpageModel)
        {
            var webpage = GetWebpage(moveWebpageModel);
            var parent = GetParent(moveWebpageModel);

            if (parent == webpage.Parent)
            {
                return new MoveWebpageResult
                {
                    Success = false,
                    Message = _resourceProvider.GetValue("The webpage already has this parent")
                };
            }

            var validParentWebpages = await GetValidParentWebpages(webpage);
            var valid = parent == null ? IsRootAllowed(webpage) : validParentWebpages.Contains(parent);

            return new MoveWebpageResult
            {
                Success = valid,
                Message = valid ? string.Empty : _resourceProvider.GetValue("Sorry, but you can't select that as a parent for this page.")
            };
        }

        private Webpage GetParent(MoveWebpageModel moveWebpageModel)
        {
            return moveWebpageModel.ParentId.HasValue
                ? _webpageRepository.LoadSync(moveWebpageModel.ParentId.Value)
                : null;
        }

        private Webpage GetWebpage(MoveWebpageModel moveWebpageModel)
        {
            return _webpageRepository.LoadSync(moveWebpageModel.Id);
        }

        private bool IsRootAllowed(Webpage webpage)
        {
            return !webpage.GetMetadata().RequiresParent;
        }

        public async Task<MoveWebpageConfirmationModel> GetConfirmationModel(MoveWebpageModel model)
        {
            var webpage = GetWebpage(model);
            var parent = GetParent(model);


            return new MoveWebpageConfirmationModel
            {
                Webpage = webpage,
                Parent = parent,
                ChangedPages = await GetChangedPages(model, webpage, parent)
            };
        }

        private async Task<List<MoveWebpageChangedPageModel>> GetChangedPages(MoveWebpageModel model, Webpage webpage, Webpage parent)
        {
            var webpageHierarchy = GetWebpageHierarchy(webpage).ToList();

            var parentActivePages = (await _activePagesLoader.GetActivePages(parent)) ?? new List<Webpage>();
            parentActivePages.Reverse();

            List<MoveWebpageChangedPageModel> models = new List<MoveWebpageChangedPageModel>();
            foreach (var page in webpageHierarchy)
            {
                var activePages = await _activePagesLoader.GetActivePages(page);
                var indexOf = activePages.IndexOf(webpage);
                var childActivePages = activePages.Take(indexOf + 1).ToList();
                activePages.Reverse();
                var immediateParent = childActivePages.ElementAtOrDefault(1);
                childActivePages.Reverse();
                var newUrl = await GetNewUrl(model, parent, page, immediateParent, models.FirstOrDefault(x => x.Id == immediateParent?.Id));
                models.Add(new MoveWebpageChangedPageModel
                {
                    Id = page.Id,
                    ParentId = immediateParent?.Id,
                    OldUrl = page.UrlSegment,
                    NewUrl = newUrl,
                    OldHierarchy = GetHierarchy(activePages),
                    NewHierarchy = GetHierarchy(parentActivePages.Concat(childActivePages))
                });
            }
            return models;
        }

        private async Task<string> GetNewUrl(MoveWebpageModel model, Webpage parent, Webpage page, Webpage immediateParent,
            MoveWebpageChangedPageModel parentModel)
        {
            if (!model.UpdateUrls)
            {
                return page.UrlSegment;
            }

            if (immediateParent == null)
            {
                return await _webpageUrlService.Suggest(new SuggestParams
                {
                    DocumentType = page.DocumentClrType,
                    PageName = page.Name,
                    Template = page.PageTemplate?.Id,
                    UseHierarchy = true,
                    ParentId = parent?.Id,
                    WebpageId = page.Id
                });
            }

            return await _webpageUrlService.Suggest(
                new SuggestParams
                {
                    DocumentType = page.DocumentClrType,
                    PageName = $"{parentModel.NewUrl}/{page.Name}",
                    Template = page.PageTemplate?.Id,
                    UseHierarchy = false,
                    ParentId = immediateParent.Id,
                    WebpageId = page.Id
                });
        }

        private string GetHierarchy(IEnumerable<Webpage> webpages)
        {
            return string.Join(" > ", webpages.Select(x => x.Name));
        }

        private IEnumerable<Webpage> GetWebpageHierarchy(Webpage webpage)
        {
            yield return webpage;
            var descendants = _webpageRepository.Query().Where(x => x.Parent.Id == webpage.Id).OrderBy(x => x.DisplayOrder)
                .ToList();
            foreach (var descendant in descendants)
            {
                foreach (var child in GetWebpageHierarchy(descendant))
                {
                    yield return child;
                }
            }
        }

        public async Task<MoveWebpageResult> Confirm(MoveWebpageModel model)
        {
            var confirmationModel = await GetConfirmationModel(model);

            var success = await SetParent(confirmationModel.Webpage, confirmationModel.Parent);
            if (!success)
            {
                return new MoveWebpageResult
                {
                    Success = false,
                    Message = _resourceProvider.GetValue("There was an issue setting the parent to the new value")
                };
            }

            success = await _createUpdateUrlBatch.CreateBatch(confirmationModel);

            return new MoveWebpageResult
            {
                Success = success,
                Message = _resourceProvider.GetValue(success
                    ? "The page has been moved successfully"
                    : "There was an issue creating the batch to update the page URLs")
            };
        }

        public MoveWebpageModel GetModel(Webpage webpage)
        {
            return new MoveWebpageModel
            {
                Id = webpage.Id
            };
        }
    }
}