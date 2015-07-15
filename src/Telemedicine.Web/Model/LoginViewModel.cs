﻿using System.ComponentModel.DataAnnotations;

namespace Telemedicine.Web.Model
{
    public class LoginViewModel
    {
        public string Email { get; set; }

        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }
}