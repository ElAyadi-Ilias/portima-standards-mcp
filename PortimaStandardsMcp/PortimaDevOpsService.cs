using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace PortimaStandardsMcp
{
    public class PortimaDevOpsService
    {
        private const string OrgUrl = "https://dev.azure.com/tfsportima";
        private readonly string _pat = "YourPersonalAccessToken"; 

        private GitHttpClient GetGitClient()
        {
            if (string.IsNullOrWhiteSpace(_pat))
                throw new Exception("La variable d'environnement AZURE_DEVOPS_EXT_PAT n'est pas définie.");

            var creds = new VssBasicCredential(string.Empty, _pat);
            var connection = new VssConnection(new Uri(OrgUrl), creds);
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
                    .Where(i => !i.IsFolder && (i.Path.EndsWith(".cs") || i.Path.EndsWith(".config"))) // ou extensions pertinentes
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
