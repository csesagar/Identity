using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using System.Security.Claims;
using System.Text;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> userManager;
        private readonly SignInManager<IdentityUser> signInManager;
        public AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
        {
            this.userManager = userManager; 
            this.signInManager = signInManager;
        }
        #region Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Register(UserModel user)
        {
            if (ModelState.IsValid)
            {
                IdentityUser identityUser = new IdentityUser
                {
                    UserName = user.Name,
                    Email = user.Email,
                    PhoneNumber = user.Mobile
                };
                var result = await userManager.CreateAsync(identityUser, user.Password);
                if (result.Succeeded)
                {
                    var token = await userManager.GenerateEmailConfirmationTokenAsync(identityUser);    // token generation
                    var confirmationUrlLink = Url.Action("ConfirmEmail", "Account", new { UserId = identityUser.Id, Token = token }, Request.Scheme);   // confirmation link 
                    SendMail(identityUser, confirmationUrlLink, "Email Confirmation Link");
                    TempData["Title"] = "Email Confirmation Link";
                    TempData["Message"] = "A confirm email link has been sent to your registered mail, click on it to confirm.";
                    return View("DisplayMessages");

                    //Performing a Sign-In into the appliction
                    //await signInManager.SignInAsync(identityUser, false);   // it will manage signin and signout  // false :- not signin when close the browser
                    //return RedirectToAction("Index", "Home");
                }
                else
                {
                    foreach (var Error in result.Errors)
                    {
                        //Displaying error details to the user
                        ModelState.AddModelError("", Error.Description);
                    }
                }
            }
            return View(user);
        }

        #endregion

        #region login
        public IActionResult Login()
        {
            return View();
        }

    

        [HttpPost]
        public async Task<IActionResult> Login(LoginModel loginModel, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                //code to check whethere email is confirmed or not 
                var User = await userManager.FindByNameAsync(loginModel.Name);
                if (User != null && User.EmailConfirmed == false && (await userManager.CheckPasswordAsync(User, loginModel.Password)))
                {
                    ModelState.AddModelError("", "your email is not confirmed");
                    return View(loginModel);
                }
                var result = await signInManager.PasswordSignInAsync(loginModel.Name, loginModel.Password, loginModel.RememberMe, false);
                if (result.Succeeded)
                {
                    if (string.IsNullOrEmpty(returnUrl))
                    {
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        return LocalRedirect(returnUrl);   // connot allow remote redirect for safety we use it 
                    }

                }
                else
                {
                    ModelState.AddModelError("", "Invalid login credentials.");
                }
            }
            return View(loginModel);
        }


        #endregion

        #region Logout

        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Login");

        }

        #endregion

        #region sendmail
        public void SendMail(IdentityUser identityUser, string requestLink, string subject)
        {
            StringBuilder mailBody = new StringBuilder();
            mailBody.Append("Hello " + identityUser.UserName + "<br /><br />");
            if (subject == "Email Confirmation Link")
            {
                mailBody.Append("Click on the link below to confirm your email:");
            }
            else if (subject == "Change Password Link")
            {
                mailBody.Append("Click on the link below to reset your password:");
            }
            mailBody.Append("<br />");
            mailBody.Append(requestLink);
            mailBody.Append("<br /><br />");
            mailBody.Append("Regards");
            mailBody.Append("<br /><br />");
            mailBody.Append("Customer Support.");
            BodyBuilder bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = mailBody.ToString();
            MailboxAddress fromAddress = new MailboxAddress("Customer Support", "csesagar065@gmail.com");
            MailboxAddress toAddress = new MailboxAddress(identityUser.UserName, identityUser.Email);
            MimeMessage mailMessage = new MimeMessage();
            mailMessage.From.Add(fromAddress);
            mailMessage.To.Add(toAddress);
            mailMessage.Subject = subject;
            mailMessage.Body = bodyBuilder.ToMessageBody();
            SmtpClient smtpClient = new SmtpClient();
            smtpClient.Connect("smtp.gmail.com", 465, true);
            smtpClient.Authenticate("csesagar065@gmail.com", "12345Abcde@#");
            smtpClient.Send(mailMessage);
        }
        #endregion

        #region ConfirmEmail
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
{
            if (userId != null && token != null)
            {
                var User = await userManager.FindByIdAsync(userId);
                if (User != null)
                {
                    var result = await userManager.ConfirmEmailAsync(User, token);
                    if (result.Succeeded)
                    {
                        TempData["Title"] = "Email Confirmation Success.";
                        TempData["Message"] = "Email confirmation is completed. You can now login into the application.";
                        return View("DisplayMessages");
                    }
                    else
                    {
                        StringBuilder Errors = new StringBuilder();
                        foreach (var Error in result.Errors)
                        {
                            Errors.Append(Error.Description + ". ");
                        }
                        TempData["Title"] = "Confirmation Email Failure";
                        TempData["Message"] = Errors.ToString(); return View("DisplayMessages");
                    }
                }
                else
                {
                    TempData["Title"] = "Invalid User Id.";
                    TempData["Message"] = "User Id which is present in confirm email link is in-valid.";
                    return View("DisplayMessages");
                }
            }
            else
            {
                TempData["Title"] = "Invalid Email Confirmation Link.";
                TempData["Message"] = "Email confirmation link is invalid, either missing the User Id or Confirmation Token.";
                return View("DisplayMessages");
            }
        }
        #endregion


        #region Forgot Password

        public IActionResult ForgotPassword()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordModel model)
{
            if (ModelState.IsValid)
            {
                var User = await userManager.FindByNameAsync(model.Name);
                if (User != null && await userManager.IsEmailConfirmedAsync(User))
                {
                    var token = await userManager.GeneratePasswordResetTokenAsync(User);
                    var confirmationUrlLink = Url.Action("ForgotPassword", "Account", new { UserId = User.Id, Token = token },Request.Scheme);
                    SendMail(User, confirmationUrlLink, "Change Password Link");
                    TempData["Title"] = "Change Password Link";
                    TempData["Message"] = "Change password link has been sent to your mail, click on it and change password.";
                    return View("DisplayMessages");
                }
                else
                {
                    TempData["Title"] = "Change Password Mail Generation Failed.";
                    TempData["Message"] = "Either the Username you have entered is in-valid or your email is not confirmed.";
                    return View("DisplayMessages");
                }
            }
            return View(model);
        }
        #endregion

        #region Reset password

        public IActionResult ChangePassword()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult>ChangePassword(ResetPasswordModel model)
        {
            if (ModelState.IsValid)
            {
                var User = await userManager.FindByIdAsync(model.UserId);
                if (User != null)
                {
                    var result = await userManager.ResetPasswordAsync(User, model.Token, model.Password);
                    if (result.Succeeded)
                    {
                        TempData["Title"] = "Reset Password Success";
                        TempData["Message"] = "Your password has been reset successfully.";
                        return View("DisplayMessages");
                    }
                    else
                    {
                        foreach (var Error in result.Errors)
                            ModelState.AddModelError("", Error.Description);
                    }
                }
                else
                {
                    TempData["Title"] = "Invalid User";
                    TempData["Message"] = "No user exists with the given User Id.";
                    return View("DisplayMessages");
                }
            }
            return View(model);
        }

        #endregion

        #region authentication facebook
        public IActionResult ExternalLogin(string returnUrl, string Provider)
        {
            var url = Url.Action("CallBack", "Account", new { ReturnUrl = returnUrl });
            var properties = signInManager.ConfigureExternalAuthenticationProperties(Provider, url);   // it check whether ur app has external login property
            return new ChallengeResult(Provider, properties);   // redirect to google sign in page
        }
        public async Task<IActionResult> CallBack(string returnUrl)
        {
            if (string.IsNullOrEmpty(returnUrl))
            {
                returnUrl = "~/";
            }
            LoginModel model = new LoginModel();
            var info = await signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ModelState.AddModelError("", "Error loading external login information.");
                return View("Login", model);
            }
            var signInResult = await signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, false, true);
            if (signInResult.Succeeded)
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                if (email != null)
                {
                    var user = await userManager.FindByEmailAsync(email);
                    if (user == null)
                    {
                        user = new IdentityUser
                        {
                            UserName = info.Principal.FindFirstValue(ClaimTypes.Email),
                            Email = info.Principal.FindFirstValue(ClaimTypes.Email),
                            PhoneNumber = info.Principal.FindFirstValue(ClaimTypes.MobilePhone),
                        };
                        var identityResult = await userManager.CreateAsync(user);
                    }
                    await userManager.AddLoginAsync(user, info);
                    await signInManager.SignInAsync(user, false);
                    return LocalRedirect(returnUrl);
                }
                TempData["Title"] = "Error";
                TempData["Message"] = "Email claim not received from third party provided.";
                return RedirectToAction("DisplayMessages");
            }
        }

        #endregion

    }
    }
