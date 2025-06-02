using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;

namespace GradeBookApp.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class RequireRoleAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _roles;

        public RequireRoleAttribute(params string[] roles)
        {
            _roles = roles ?? Array.Empty<string>();
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            var logger = context.HttpContext.RequestServices.GetService(typeof(ILogger<RequireRoleAttribute>)) as ILogger;

            if (!user.Identity?.IsAuthenticated ?? true)
            {
                logger?.LogWarning("[RequireRole] Użytkownik nie jest uwierzytelniony.");
                context.Result = new UnauthorizedResult();
                return;
            }

            var userRoles = user.Claims
                .Where(c => c.Type == ClaimTypes.Role || c.Type.EndsWith("/role"))
                .Select(c => c.Value)
                .ToList();

            logger?.LogInformation("[RequireRole] Użytkownik: {Name}", user.Identity?.Name);
            logger?.LogInformation("[RequireRole] Role użytkownika: {Roles}", string.Join(", ", userRoles));
            logger?.LogInformation("[RequireRole] Role wymagane: {Required}", string.Join(", ", _roles));

            var hasRole = userRoles.Any(r => _roles.Contains(r, StringComparer.OrdinalIgnoreCase));

            if (!hasRole)
            {
                logger?.LogWarning("[RequireRole] Brak wymaganych ról. Dostęp zabroniony.");
                context.Result = new ForbidResult();
            }
            else
            {
                logger?.LogInformation("[RequireRole] Rola pasuje. Dostęp przyznany.");
            }
        }
    }
}
