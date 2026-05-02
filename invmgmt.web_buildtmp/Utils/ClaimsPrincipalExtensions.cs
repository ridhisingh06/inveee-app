using System.Security.Claims;

namespace invmgmt.web.Utils;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue("UserId");
        if (string.IsNullOrWhiteSpace(raw) || !int.TryParse(raw, out var id))
        {
            throw new InvalidOperationException("Invalid or missing UserId claim.");
        }

        return id;
    }
}

