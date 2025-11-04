using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace PortimaStandardsMcp
{
    public class PortimaDevOpsService
    {
        private readonly string _orgUrl;
        private readonly string _pat;

        public PortimaDevOpsService(IConfiguration configuration)
        {
            _orgUrl = configuration["AzureDevOps:OrganizationUrl"] 
                ?? throw new Exception("Azure DevOps organization URL is not configured in appsettings.json");
            _pat = configuration["AzureDevOps:PersonalAccessToken"] 
                ?? throw new Exception("Personal Access Token is not configured in appsettings.json");
        }

        private GitHttpClient GetGitClient()
        {
            var creds = new VssBasicCredential(string.Empty, _pat);
            var connection = new VssConnection(new Uri(_orgUrl), creds);
            return connection.GetClient<GitHttpClient>();
        }

        // Lists file paths (no content) for the given repo/branch
        public async Task<List<string>> ListFilePathsAsync(string projectName, string repoName, string branchName = "dev")
        {
            var gitClient = GetGitClient();
            var repo = await gitClient.GetRepositoryAsync(projectName, repoName);

            var items = await gitClient.GetItemsAsync(
                repo.Id,
                scopePath: "/",
                recursionLevel: VersionControlRecursionType.Full,
                versionDescriptor: new GitVersionDescriptor
                {
                    Version = branchName,
                    VersionType = GitVersionType.Branch
                });

                return items
                    .Where(i => !i.IsFolder && (i.Path.EndsWith(".cs") || i.Path.EndsWith(".config"))) // or other relevant extensions
                    .Select(i => i.Path)
                    .OrderBy(p => p)
                    .ToList();
        }

  public async Task<string> GetFileFromBranchAsync(string projectName, string repoName, string filePath, string branchName = "dev")
        {
            var gitClient = GetGitClient();
            var repo = await gitClient.GetRepositoryAsync(projectName, repoName);

            using (var stream = await gitClient.GetItemContentAsync(
                repo.Id,
                path: filePath,
                versionDescriptor: new GitVersionDescriptor
                {
                    Version = branchName,
                    VersionType = GitVersionType.Branch
                }))
            using (var reader = new StreamReader(stream))
            {
                return await reader.ReadToEndAsync();
            }
        }
    }
}
