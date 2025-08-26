using ImageUploaderApi.Domain.Entities;
using ImageUploaderApi.Domain.Repositories;

namespace ImageUploaderApi.AppServices
{
    public class ProjectYamlAppService
    {
        private readonly IProjectYamlRepository _repository;
        private readonly ILogger<ProjectYamlAppService> _logger;

        public ProjectYamlAppService(
            IProjectYamlRepository repository,
            ILogger<ProjectYamlAppService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        #region 版本管理

        public async Task<ProjectYaml> CreateNewVersionAsync(
            string projectName,
            string yaml,
            string version,
            string changeDescription)
        {
            if (string.IsNullOrWhiteSpace(projectName))
                throw new ArgumentException("Project name cannot be empty", nameof(projectName));

            if (yaml == null)
                throw new ArgumentNullException(nameof(yaml));

            _logger.LogInformation("Creating new version {Version} for project {Project}", version, projectName);

            // 将当前版本标记为非当前
            await _repository.MarkAllAsNonCurrentAsync(projectName);

            var newVersion = new ProjectYaml(projectName, version, yaml, changeDescription);
            await _repository.AddAsync(newVersion);
            return newVersion;
        }

        public async Task<bool> RollbackToVersionAsync(string projectName, string version)
        {
            var targetVersion = await _repository.GetByVersionAsync(projectName, version);
            if (targetVersion == null)
            {
                _logger.LogWarning("Version {Version} not found for project {Project}", version, projectName);
                return false;
            }

            _logger.LogInformation("Rolling back project {Project} to version {Version}", projectName, version);

            // 将当前版本标记为非当前
            await _repository.MarkAllAsNonCurrentAsync(projectName);

            // 将目标版本标记为当前
            targetVersion.MarkAsCurrent();

            await _repository.UpdateAsync(targetVersion);
            return true;
        }

        #endregion

        #region 配置管理

        public async Task<string> GetCurrentConfigAsync(string projectName)
        {
            var currentVersion = await _repository.GetCurrentVersionAsync(projectName);
            if (currentVersion == null)
            {
                _logger.LogWarning("No current version found for project {Project}", projectName);
                return null;
            }

            try
            {
                return currentVersion.YamlContent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize YAML for project {Project}", projectName);
                throw new InvalidOperationException("Failed to deserialize YAML content", ex);
            }
        }

        public async Task<string> GetConfigByVersionAsync(string projectName, string version)
        {
            var projectVersion = await _repository.GetByVersionAsync(projectName, version);
            if (projectVersion == null)
            {
                _logger.LogWarning("Version {Version} not found for project {Project}", version, projectName);
                return null;
            }

            return projectVersion.YamlContent;
        }

        public async Task UpdateConfigAsync(string projectName, string yaml, string changeDescription)
        {
            var currentVersion = await _repository.GetCurrentVersionAsync(projectName);
            if (currentVersion == null)
            {
                _logger.LogWarning("No current version found for project {Project}, creating initial version", projectName);
                await CreateNewVersionAsync(projectName, yaml, "Latest", changeDescription);
                return;
            }
            currentVersion.UpdateInfo(yaml, changeDescription);

            await _repository.UpdateAsync(currentVersion);
            _logger.LogInformation("Updated current version for project {Project}", projectName);
        }

        #endregion

        #region 项目管理

        public async Task<IEnumerable<ProjectYaml>> GetVersionHistoryAsync(string projectName)
        {
            return await _repository.GetVersionHistoryAsync(projectName);
        }

        public async Task<IEnumerable<string>> GetAllProjectNamesAsync()
        {
            var allProjects = await _repository.GetAllAsync();
            return allProjects
                .Select(p => p.ProjectName)
                .Distinct()
                .OrderBy(name => name);
        }

        public async Task DeleteProjectAsync(string projectName)
        {
            var versions = (await _repository.GetVersionHistoryAsync(projectName)).ToList();
            if (versions.Any())
            {
                await _repository.DeleteRangeAsync(versions);
                _logger.LogInformation("Deleted all {Count} versions for project {Project}", versions.Count(), projectName);
            }
        }

        public async Task DeleteVersionAsync(string projectName, string version)
        {
            var projectVersion = await _repository.GetByVersionAsync(projectName, version);
            if (projectVersion == null)
            {
                _logger.LogWarning("Version {Version} not found for project {Project}", version, projectName);
                return;
            }

            // 如果要删除的是当前版本，需要重新指定一个当前版本
            if (projectVersion.IsCurrent)
            {
                var otherVersions = (await _repository.GetVersionHistoryAsync(projectName))
                    .Where(v => v.Version != version)
                    .OrderByDescending(v => v.CreatedAt)
                    .ToList();

                if (otherVersions.Any())
                {
                    var newCurrent = otherVersions.First();
                    newCurrent.MarkAsCurrent();
                    await _repository.UpdateAsync(newCurrent);
                    _logger.LogInformation("Set version {Version} as current for project {Project}", newCurrent.Version, projectName);
                }
            }

            await _repository.DeleteAsync(projectVersion);
            _logger.LogInformation("Deleted version {Version} for project {Project}", version, projectName);
        }

        #endregion

        #region 辅助方法

        public async Task<bool> ProjectExistsAsync(string projectName)
        {
            var currentVersion = await _repository.GetCurrentVersionAsync(projectName);
            return currentVersion != null;
        }

        public async Task<bool> VersionExistsAsync(string projectName, string version)
        {
            return await _repository.VersionExistsAsync(projectName, version);
        }

        public async Task<int> GetVersionCountAsync(string projectName)
        {
            return await _repository.GetVersionCountAsync(projectName);
        }

        #endregion
    }
}
