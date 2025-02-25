using Octokit;

namespace Managment.Interface
{
    public interface IGitHubCilentService
    {
        public GitHubClient GitHubClient { get; }
    }
}
