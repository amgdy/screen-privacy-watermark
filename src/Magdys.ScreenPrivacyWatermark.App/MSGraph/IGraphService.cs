using Microsoft.Graph.Models;

namespace Magdys.ScreenPrivacyWatermark.App.MSGraph;

public interface IGraphService
{
    ValueTask<User> GetCurrentUserDataAsync(params string[] properties);

    ValueTask<HashSet<Guid>> GetCurrentUserGroupIdsAsync();
}