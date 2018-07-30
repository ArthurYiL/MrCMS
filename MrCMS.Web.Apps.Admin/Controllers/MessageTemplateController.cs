﻿using Microsoft.AspNetCore.Mvc;
using MrCMS.Helpers;
using MrCMS.Messages;
using MrCMS.Web.Apps.Admin.Helpers;
using MrCMS.Web.Apps.Admin.ModelBinders;
using MrCMS.Web.Apps.Admin.Services;
using MrCMS.Website.Controllers;

namespace MrCMS.Web.Apps.Admin.Controllers
{
    public class MessageTemplateController : MrCMSAdminController
    {
        private readonly IMessageTemplateAdminService _messageTemplateAdminService;

        public MessageTemplateController(IMessageTemplateAdminService messageTemplateAdminService)
        {
            _messageTemplateAdminService = messageTemplateAdminService;
        }

        [HttpGet]
        public ActionResult Index()
        {
            return View(_messageTemplateAdminService.GetAllMessageTemplateTypesWithDetails());
        }

        [HttpGet]
        public ActionResult AddSiteOverride(string type)
        {
            return View(_messageTemplateAdminService.GetNewOverride(type));
        }

        [HttpPost]
        [ActionName("AddSiteOverride")]
        public ActionResult AddSiteOverride_POST(
            [ModelBinder(typeof(MessageTemplateOverrideModelBinder))]
            MessageTemplate messageTemplate) 

        {
            if (messageTemplate != null)
            {
                _messageTemplateAdminService.AddOverride(messageTemplate);
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult DeleteSiteOverride(string type)
        {
            return View(_messageTemplateAdminService.GetOverride(type));
        }

        [HttpPost]
        [ActionName("DeleteSiteOverride")]
        public ActionResult DeleteSiteOverride_POST(
            [ModelBinder(typeof(DeleteMessageTemplateOverrideModelBinder))]
            MessageTemplate messageTemplate) 
        {
            if (messageTemplate != null)
            {
                _messageTemplateAdminService.DeleteOverride(messageTemplate);
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult Edit(string type)
        {
            ModelState.Clear();
            MessageTemplate template = _messageTemplateAdminService.GetTemplate(type);
            if (template != null)
            {
                return View(template);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ActionName("Edit")]
        public ActionResult Edit_POST(
            //[IoCModelBinder(typeof(MessageTemplateOverrideModelBinder))]
            MessageTemplate messageTemplate) // TODO: model-binding
        {
            if (messageTemplate != null)
            {
                _messageTemplateAdminService.Save(messageTemplate);
                TempData.SuccessMessages()
                    .Add(string.Format("{0} successfully edited", messageTemplate.GetType().Name.BreakUpString()));
                return RedirectToAction("Edit", new { type = messageTemplate.GetType().FullName });
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult ImportLegacyTemplate(string type)
        {
            return View((object)type);
        }

        [HttpPost, ActionName("ImportLegacyTemplate")]
        public ActionResult ImportLegacyTemplate_POST(string type)
        {
            _messageTemplateAdminService.ImportLegacyTemplate(type);
            return RedirectToAction("Index");
        }
    }
}