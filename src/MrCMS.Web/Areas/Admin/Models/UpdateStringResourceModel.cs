﻿using System.ComponentModel.DataAnnotations;

namespace MrCMS.Web.Areas.Admin.Models
{
    public class UpdateStringResourceModel
    {
        public int Id { get; set; }
        [Required]
        public string Value { get; set; }
    }
}