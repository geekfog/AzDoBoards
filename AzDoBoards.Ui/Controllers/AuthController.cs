using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;

namespace AzDoBoards.Ui.Controllers;

public class AuthController : Controller
{
    [Route("signout")]
    public new IActionResult SignOut()
    {
        return SignOut(
            new AuthenticationProperties { RedirectUri = "/" },
            OpenIdConnectDefaults.AuthenticationScheme,
            CookieAuthenticationDefaults.AuthenticationScheme
        );
    }

    [Route("switch-account")]
    public IActionResult SwitchAccount()
    {
        // Show intermediate page first, then redirect to sign out
        return Redirect("/switching-account");
    }

    [Route("perform-account-switch")]
    public IActionResult PerformAccountSwitch()
    {
        // First, sign out the current user
        return SignOut(
            new AuthenticationProperties
            {
                RedirectUri = Url.Action("PromptAccountSelection")
            },
            OpenIdConnectDefaults.AuthenticationScheme,
            CookieAuthenticationDefaults.AuthenticationScheme
        );
    }

    [Route("switch-to-work-account")]
    public IActionResult SwitchToWorkAccount()
    {
        // First, sign out the current user completely
        return SignOut(
            new AuthenticationProperties
            {
                RedirectUri = Url.Action("PromptWorkAccountSelection")
            },
            OpenIdConnectDefaults.AuthenticationScheme,
            CookieAuthenticationDefaults.AuthenticationScheme
        );
    }

    [Route("account-selection")]
    public IActionResult PromptAccountSelection()
    {
        // Force account selection by using prompt=select_account
        var properties = new AuthenticationProperties
        {
            RedirectUri = "/",
            Parameters =
            {
                ["prompt"] = "select_account"
            }
        };

        return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
    }

    [Route("work-account-selection")]
    public IActionResult PromptWorkAccountSelection()
    {
        // Force account selection with domain hint for work accounts and logout hint
        var properties = new AuthenticationProperties
        {
            RedirectUri = "/",
            Parameters =
            {
                ["prompt"] = "select_account",
                ["domain_hint"] = "organizations", // Prefer organizational accounts
                ["logout_hint"] = "true" // Hint to logout from all accounts first
            }
        };

        return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
    }

    [Route("force-logout")]
    public IActionResult ForceLogout()
    {
        // Complete logout with end session endpoint
        var properties = new AuthenticationProperties
        {
            RedirectUri = "/logged-out"
        };

        return SignOut(properties, OpenIdConnectDefaults.AuthenticationScheme, CookieAuthenticationDefaults.AuthenticationScheme);
    }
}