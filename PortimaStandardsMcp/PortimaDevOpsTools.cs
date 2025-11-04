using ModelContextProtocol.Server;
using System.ComponentModel;

namespace PortimaStandardsMcp
{
    [McpServerToolType]
    public class PortimaDevOpsTools
    {
        private readonly PortimaDevOpsService _portimaDevOpsService;

        public PortimaDevOpsTools(PortimaDevOpsService portimaDevOpsService)
        {
            _portimaDevOpsService = portimaDevOpsService;
        }

        [McpServerTool, Description("List file paths of a given repository and branch.")]
        public async Task<List<string>> ListRepositoryFiles(
            string projectName ,
            string repoName ,
            string branch )
        {
            return await _portimaDevOpsService.ListFilePathsAsync(projectName, repoName, branch);
        }

        [McpServerTool, Description("Get the content of a specific file in a given repository and branch.")]
        public async Task<string> GetRepositoryFileContent(
            string projectName ,
            string repoName ,
            string filePath ,
            string branch )
        {
            return await _portimaDevOpsService.GetFileFromBranchAsync(projectName, repoName, filePath, branch);
        }

        [McpServerTool, Description("Get all files content from a given repository and branch.")]
        public async Task<Dictionary<string, string>> GetAllRepositoryFilesContent(
            string projectName ,
            string repoName ,
            string branch )
        {
            var paths = await _portimaDevOpsService.ListFilePathsAsync(projectName, repoName, branch);
            var dict = new Dictionary<string, string>();
            var tasks = paths.Select(async path =>
            {
                var content = await _portimaDevOpsService.GetFileFromBranchAsync(projectName, repoName, path, branch);
                return (path, content);
            });

            var results = await Task.WhenAll(tasks);
            return results.ToDictionary(r => r.path, r => r.content);
        }

    }
}
