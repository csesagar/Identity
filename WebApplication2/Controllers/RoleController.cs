using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Controllers
{
    public class RoleController : Controller
    {
        private readonly UserManager<IdentityUser> userManager;
        private readonly SignInManager<IdentityUser> signInManager;
        private readonly RoleManager<IdentityRole> roleManager;

        public RoleController(UserManager<IdentityUser> userManager , SignInManager<IdentityUser> signInManager , RoleManager<IdentityRole> roleManager)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.roleManager = roleManager;
        }
        #region Add role
        [HttpGet]
        public IActionResult AddRole()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddRole([Required] string Name)
        {
            if (ModelState.IsValid)
            {
                IdentityRole role = new IdentityRole(Name);
                var result = await roleManager.CreateAsync(role);
                if (result.Succeeded)
                {
                    return RedirectToAction("DisplayRoles");
                }
                else
                {
                    DisplayErrors(result.Errors);
                }
            }
            return View(Name);
        }

        public void DisplayErrors(IEnumerable<IdentityError> Errors)
        {
            foreach(var err in Errors)
            {
                ModelState.AddModelError("", err.Description);
            }
        }
        #endregion
    }
}
