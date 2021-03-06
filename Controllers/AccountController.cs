﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KaraYadak.Models;
using KaraYadak.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using DNTPersianUtils.Core;
using KaraYadak.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace KaraYadak.Controllers
{
    //[Authorize(Roles = "Admin")]
    //[Authorize(Roles = "User")]
    [Authorize(Roles = "Admin,User")]

    public class AccountController : Controller
    {

        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _signInManager = signInManager;
            _roleManager = roleManager;
            _userManager = userManager;
            _context = context;
        }
        public static string RandomString(int length)
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public async Task<IActionResult> CreateRole(string name)
        {
            await _roleManager.CreateAsync(new IdentityRole { Name = name, NormalizedName = name.Normalize() });

            return Content("role created: " + name);
        }
        public async Task<IActionResult> Action()
        {
            var user = _context.Users.FirstOrDefault(x => x.UserName == User.Identity.Name);
            //await _userManager.AddToRoleAsync(user, "Admin");
            return Json(await _userManager.IsInRoleAsync(user,"Admin"));
        }
        public async Task<IActionResult> CreateUser(string name)
        {
            var f = await _userManager.FindByNameAsync(name);
            var random = new Random();
            var pass = random.Next(1000000, 9999999).ToString();
            if (f == null)
            {
                await _userManager.CreateAsync(new ApplicationUser
                {
                    AvatarUrl = "",
                    Birthdate = "",
                    Email = name + "@kfc.ir",
                    NormalizedEmail = name + "@Name.ir".Normalize(),
                    EmailConfirmed = true,
                    FirstName = name,
                    LastName = "",
                    Gender = "",
                    NationalCode = "",
                    Nickname = name,
                    UserName = name + "@Name.ir",
                    NormalizedUserName = name.Normalize(),
                    Phone = "",
                    PhoneNumber = "",
                    RegistrationDateTime = DateTime.Now,
                    VerificationCode = "",
                }, "123456789");
            }
            return Content("user password: " + pass);
        }
        public IActionResult AccessDenied()
        {
            return Content("Access Denied");
        }

        public async Task<IActionResult> Index(string returnUrl)
        {
            returnUrl = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl;

            if (_signInManager.IsSignedIn(User))
            {
                return Redirect(returnUrl);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }
        [AllowAnonymous]
        public IActionResult Login(string returnUrl)
        {
            returnUrl = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl;

            if (_signInManager.IsSignedIn(User))
            {
                return Redirect(returnUrl);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }
        public async Task<IActionResult> ChangePassword()
        {
            var password = RandomString(6);
            var user = await _userManager.FindByNameAsync("Admin@KaraYadak.com");
            await _signInManager.SignInAsync(user, true);
            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, code, password);
            return Json(password);
        }
        [AllowAnonymous]

        public IActionResult Register(string returnUrl = "")
        {
            if (_signInManager.IsSignedIn(User)) return Redirect(returnUrl);
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]

        public async Task<IActionResult> SignIn(LoginViewModel input)
        {
            input.ReturnUrl = input.ReturnUrl ?? Url.Action("index", "home");

            if (!ModelState.IsValid)
            {
                var errors = new List<string>();
                foreach (var item in ModelState.Values)
                {
                    foreach (var err in item.Errors)
                    {
                        errors.Add(err.ErrorMessage);
                    }
                }
                return new JsonResult(new { Status = 0, Error = errors });
            }
            var user = await _userManager.FindByNameAsync(input.Username);
            if (user == null)
                return Json(new { status = "2", Error = "نام کاربری شما در سیستم موجود نمی باشد" });
            var result = await _signInManager.PasswordSignInAsync(user, input.Password, true, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                var isUser = await _userManager.IsInRoleAsync(user, "User");

                await _signInManager.SignInAsync(user, true);

                if (string.IsNullOrEmpty(input.ReturnUrl)||input.ReturnUrl=="/")
                {
                    if (isAdmin)
                    {
                        return new JsonResult(new { Status = 3, input.ReturnUrl, message = "/dashboard" });

                    }
                    else
                    {
                        return new JsonResult(new { Status = 3, input.ReturnUrl, message = "/" });

                    }
                }
                return new JsonResult(new { Status = 1, input.ReturnUrl ,message="خوش آمدید!"});
            }
            else
            {
                return new JsonResult(new { Status = 2, Error = "نام کاربری یا رمز عبور اشتباه است" });
            }
        }
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity.IsAuthenticated)
                return Redirect("/");

            return View();
        }

        [HttpPost]
        [AllowAnonymous]

        public async Task<IActionResult> Register(RegisterViewModel input)
        {
            input.ReturnUrl = input.ReturnUrl ?? Url.Action("index", "home");

            if (!ModelState.IsValid)
            {
                var errors = new List<string>();
                foreach (var item in ModelState.Values)
                {
                    foreach (var err in item.Errors)
                    {
                        errors.Add(err.ErrorMessage);
                    }
                }
                return new JsonResult(new { Status = 0, Error = errors });
            }
            var fullName = input.Nickname;
            var names = fullName.Split(" ");
            var fName = "";
            var lName = "";
            if (names[0].Length > 1)
            {
                fName = names[0];
            }
            if (names[1].Length > 1)
            {
                lName = names[1];
            }
            var result = await _userManager.CreateAsync(new ApplicationUser
            {
                AccessFailedCount = 0,
                AvatarUrl = "",
                Birthdate = "",
                Email = input.Email,
                EmailConfirmed = false,
                FirstName = fName,
                LastName = lName,
                Gender = "",
                Nickname = "",
                NormalizedUserName = input.Email.Normalize(),
                RegistrationDateTime = DateTime.Now,
                UserName = input.Email,
                Phone = "",
                PhoneNumber = ""
            }, input.Password);
           
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(await _userManager.FindByNameAsync(input.Email), "User");
                var signIn = await _signInManager.PasswordSignInAsync(input.Email, input.Password, isPersistent: true, lockoutOnFailure: false);
                if (signIn.Succeeded)
                    return new JsonResult(new { Status = 1, input.ReturnUrl, message = "ثبت نام با موفقیت انجام شد" });
                return new JsonResult(new { Status = 1, input.ReturnUrl, message = "ثبت نام با موفقیت انجام شد" });
            }
            else
            {
                if (result.Errors.Where(i => i.Code == "DuplicateUserName").Any())
                    return new JsonResult(new { Status = 2, Error = "نام کاربری از قبل ثبت شده است" });
                return new JsonResult(new { Status = 0, Error = result.Errors.First().Description });
            }

        }
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Logout(string returnUrl = null)
        {
            await _signInManager.SignOutAsync();
            returnUrl = returnUrl ?? Url.Content("/");
            return LocalRedirect(returnUrl);
        }

    }
}