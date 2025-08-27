#nullable enable
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ImageUploaderApi.Infrastructure.Authorizations
{
    /// <summary>
    /// 授权需求：白名单用户访问控制
    /// </summary>
    public class UserWhitelistAuthorizationRequirement : AuthorizationHandler<UserWhitelistAuthorizationRequirement>, IAuthorizationRequirement
    {
        private readonly IConfiguration _config;

        public UserWhitelistAuthorizationRequirement(IConfiguration config)
        {
            _config = config;
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            UserWhitelistAuthorizationRequirement requirement)
        {
            // 从配置中获取允许访问的用户名列表（忽略空值）
            var allowedUsers = _config.GetSection("Security:WhitelistedUsers").Get<string[]?>()
                ?.Where(user => !string.IsNullOrWhiteSpace(user))
                .ToArray() ?? [];

            // 检查是否允许所有用户访问（不区分大小写）
            if (allowedUsers.Length == 0 ||
                allowedUsers.Any(user => "all".Equals(user, StringComparison.OrdinalIgnoreCase)))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // 获取当前用户身份（支持多种claim类型）
            var currentUser = context.User.FindFirstValue("loginname")
                              ?? context.User.FindFirstValue(ClaimTypes.Name)
                              ?? context.User.Identity?.Name;

            // 精确匹配验证
            if (currentUser != null && allowedUsers.Contains(currentUser))
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail(new AuthorizationFailureReason(this,
                    $"用户 '{currentUser}' 不在白名单中。允许的用户: {string.Join(", ", allowedUsers)}"));
            }

            return Task.CompletedTask;
        }
    }
}
