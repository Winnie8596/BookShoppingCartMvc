using Microsoft.AspNetCore.Identity;

namespace BookShoppingCartMvcUI.Shared;

// the logged in user's id, so the repositories don't have to read HttpContext themselves.
// null for a guest (or outside a request)
public interface ICurrentUser
{
    string? UserId { get; }
}

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<IdentityUser> _userManager;

    public CurrentUser(IHttpContextAccessor httpContextAccessor, UserManager<IdentityUser> userManager)
    {
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
    }

    public string? UserId
    {
        get
        {
            var principal = _httpContextAccessor.HttpContext?.User;
            return principal is null ? null : _userManager.GetUserId(principal);
        }
    }
}
