﻿using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using MrCMS.ACL;
using MrCMS.Entities.Documents;
using MrCMS.Entities.Documents.Web;
using MrCMS.Models;
using MrCMS.Services;
using MrCMS.Web.Apps.Admin.Helpers;
using MrCMS.Web.Apps.Admin.ModelBinders;
using MrCMS.Web.Apps.Admin.Services;
using MrCMS.Website;
using MrCMS.Website.Controllers;
using MrCMS.Website.Filters;

namespace MrCMS.Web.Apps.Admin.Controllers
{
    public class WebpageController : MrCMSAdminController
    {
        private readonly IWebpageAdminService _webpageAdminService;
        private readonly IWebpageBaseViewDataService _webpageBaseViewDataService;
        private readonly IUrlValidationService _urlValidationService;

        public WebpageController(IWebpageAdminService webpageAdminService,
            IWebpageBaseViewDataService webpageBaseViewDataService, IUrlValidationService urlValidationService)
        {
            _webpageAdminService = webpageAdminService;
            _webpageBaseViewDataService = webpageBaseViewDataService;
            _urlValidationService = urlValidationService;
        }

        public ViewResult Index()
        {
            return View();
        }


        [HttpGet]
        [ActionName("Add")]
        public ViewResult Add_Get(int? id)
        {
            //Build list 
            var model = _webpageAdminService.GetAddModel(id);
            _webpageBaseViewDataService.SetAddPageViewData(ViewData, model?.Parent as Webpage);
            return View(model);
        }

        [HttpPost]
        [ForceImmediateLuceneUpdate]
        public ActionResult Add(
            [ModelBinder(typeof(AddWebpageModelBinder))]
            Webpage doc)
        {
            if (!_urlValidationService.UrlIsValidForWebpage(doc.UrlSegment, null))
            {
                _webpageBaseViewDataService.SetAddPageViewData(ViewData, doc.Parent as Webpage);
                return View(doc);
            }
            _webpageAdminService.Add(doc);
            TempData.SuccessMessages().Add(string.Format("{0} successfully added", doc.Name));
            return RedirectToAction("Edit", new {id = doc.Id});
        }

        [HttpGet]
        [ActionName("Edit")]
        [Acl(typeof(Webpage), TypeACLRule.Edit)]
        public ActionResult Edit_Get(Webpage doc)
        {
            _webpageBaseViewDataService.SetEditPageViewData(ViewData, doc);
            doc.SetAdminViewData(this);
            return View(doc);
        }

        [HttpPost]
        [Acl(typeof(Webpage), TypeACLRule.Edit)]
        [ForceImmediateLuceneUpdate]
        public ActionResult Edit(
            [ModelBinder(typeof(EditWebpageModelBinder))]
            Webpage doc)
        {
            _webpageAdminService.Update(doc);
            TempData.SuccessMessages().Add(string.Format("{0} successfully saved", doc.Name));
            return RedirectToAction("Edit", new {id = doc.Id});
        }

        [HttpGet]
        [Acl(typeof(Webpage), TypeACLRule.Delete)]
        [ActionName("Delete")]
        public ActionResult Delete_Get(Webpage document)
        {
            return PartialView(document);
        }

        [HttpPost]
        [Acl(typeof(Webpage), TypeACLRule.Delete)]
        [ForceImmediateLuceneUpdate]
        public ActionResult Delete(Webpage document)
        {
            _webpageAdminService.Delete(document);
            TempData.InfoMessages().Add(string.Format("{0} deleted", document.Name));
            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult Sort(
            //[IoCModelBinder(typeof(NullableEntityModelBinder))]
            Webpage parent) // TODO: model-binding
        {
            var sortItems = _webpageAdminService.GetSortItems(parent);

            return View(sortItems);
        }

        [HttpPost]
        public ActionResult Sort(
            //[IoCModelBinder(typeof(NullableEntityModelBinder))]
            Webpage parent, // TODO: model-binding
            List<SortItem> items)
        {
            _webpageAdminService.SetOrders(items);
            return RedirectToAction("Sort", parent == null ? null : new {id = parent.Id});
        }

        public ActionResult Show(Webpage document)
        {
            if (document == null)
                return RedirectToAction("Index");

            return View(document);
        }

        [HttpPost]
        public ActionResult PublishNow(Webpage webpage)
        {
            _webpageAdminService.PublishNow(webpage);

            return RedirectToAction("Edit", new {id = webpage.Id});
        }

        [HttpPost]
        public ActionResult Unpublish(Webpage webpage)
        {
            _webpageAdminService.Unpublish(webpage);

            return RedirectToAction("Edit", new {id = webpage.Id});
        }

        public ActionResult ViewChanges(DocumentVersion documentVersion)
        {
            if (documentVersion == null)
                return RedirectToAction("Index");

            return PartialView(documentVersion);
        }

        [HttpGet]
        public PartialViewResult FormProperties(Webpage webpage)
        {
            return PartialView(webpage);
        }

        /// <summary>
        ///     Finds out if the URL entered is valid for a webpage
        /// </summary>
        /// <param name="urlSegment">The URL Segment entered</param>
        /// <param name="id">The Id of the current document if it is set</param>
        /// <returns></returns>
        public ActionResult ValidateUrlIsAllowed(string urlSegment, int? id)
        {
            return !_urlValidationService.UrlIsValidForWebpage(urlSegment, id)
                ? Json("Please choose a different URL as this one is already used.")
                : Json(true);
        }

        /// <summary>
        ///     Returns server date used for publishing (can't use JS date as can be out compared to server date)
        /// </summary>
        /// <returns>Date</returns>
        public string GetServerDate()
        {
            return DateTime.Now.ToString(); // TODO: sort out dates and cultures
            //return CurrentRequestData.Now.ToString(CurrentRequestData.CultureInfo);
        }

        [HttpGet]
        public ActionResult AddProperties(
            [ModelBinder(typeof(AddPropertiesModelBinder))]
            Webpage webpage) 
        {
            if (webpage != null)
            {
                webpage.SetAdminViewData(this);
                return PartialView(webpage);
            }
            return new EmptyResult();
        }
    }
}