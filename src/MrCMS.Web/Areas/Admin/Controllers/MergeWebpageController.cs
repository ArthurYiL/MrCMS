using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MrCMS.Entities.Documents.Web;
using MrCMS.Web.Areas.Admin.Helpers;
using MrCMS.Web.Areas.Admin.Models;
using MrCMS.Web.Areas.Admin.Services;
using MrCMS.Website.Controllers;

namespace MrCMS.Web.Areas.Admin.Controllers
{
    public class MergeWebpageController : MrCMSAdminController
    {
        private readonly IMergeWebpageAdminService _mergeWebpageAdminService;

        public MergeWebpageController(IMergeWebpageAdminService mergeWebpageAdminService)
        {
            _mergeWebpageAdminService = mergeWebpageAdminService;
        }

        [HttpGet]
        public ViewResult Index(Webpage webpage)
        {
            ViewData["valid-parents"] = _mergeWebpageAdminService.GetValidParents(webpage);
            ViewData["webpage"] = webpage;

            return View(_mergeWebpageAdminService.GetModel(webpage));
        }

        [HttpPost]
        public async Task<RedirectToActionResult> Index(MergeWebpageModel model)
        {
            var validationResult = await _mergeWebpageAdminService.Validate(model);
            if (!validationResult.Success)
            {
                TempData.ErrorMessages().Add(validationResult.Message);
                return RedirectToAction("Index", new { id = model.Id });
            }

            return RedirectToAction("Confirm", model);
        }

        public ViewResult Confirm(MergeWebpageModel model)
        {
            ViewData["confirmation-model"] = _mergeWebpageAdminService.GetConfirmationModel(model);
            return View(model);
        }

        [HttpPost, ActionName("Confirm")]
        public async Task<RedirectToActionResult> Confirm_POST(MergeWebpageModel model)
        {
            var validationResult = await _mergeWebpageAdminService.Validate(model);
            if (!validationResult.Success)
            {
                TempData.ErrorMessages().Add(validationResult.Message);
                return RedirectToAction("Index", new { model.Id });
            }

            var confirmResult = await _mergeWebpageAdminService.Confirm(model);
            if (!confirmResult.Success)
            {
                TempData.ErrorMessages().Add(confirmResult.Message);
                return RedirectToAction("Index", new { model.Id });
            }

            TempData.SuccessMessages().Add(confirmResult.Message);
            return RedirectToAction("Edit", "Webpage", new { id = model.MergeIntoId });
        }
    }
}