using Microsoft.Extensions.Options;
using StreakPlatform.Application.Common;

namespace StreakPlatform.Application.Services;

public class InviteUrlBuilder
{
    private readonly AppOptions _options;

    public InviteUrlBuilder(IOptions<AppOptions> options) => _options = options.Value;

    public string Build(string inviteCode) =>
        $"{_options.PublicBaseUrl.TrimEnd('/')}{_options.InvitePath}?code={Uri.EscapeDataString(inviteCode)}";
}
