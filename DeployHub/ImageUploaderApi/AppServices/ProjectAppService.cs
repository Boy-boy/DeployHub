using ImageUploaderApi.Domain.Entities;
using ImageUploaderApi.Domain.Repositories;

namespace ImageUploaderApi.AppServices
{
    public class ProjectAppService
    {
        private readonly IProjectRepository _projectRepository;
        private readonly ILogger<ProjectAppService> _logger;

        public ProjectAppService(
            IProjectRepository projectRepository,
            ILogger<ProjectAppService> logger)
        {
            _projectRepository = projectRepository;
            _logger = logger;
        }

        #region 项目管理
        public async Task<IEnumerable<Project>> GetAllProjectsAsync()
        {
            return await _projectRepository.GetAllAsync();
        }

        // 创建项目
        public async Task<Project> CreateProjectAsync(string name, string description = null)
        {
            if (await _projectRepository.ExistsByNameAsync(name))
                throw new ApplicationException($"Project '{name}' already exists");

            var project = new Project(name, description);
            await _projectRepository.AddAsync(project);

            _logger.LogInformation("Created new project {ProjectName}", name);
            return project;
        }

        public async Task UpdateProjectAsync(
            Guid projectId,
            string newName,
            string description = null)
        {
            var project = await _projectRepository.GetByIdAsync(projectId);
            if (project == null)
                throw new ApplicationException("Project not found");

            // 检查名称是否已被其他项目使用
            if (await _projectRepository.ExistsByNameAsync(newName) &&
                project.Name != newName)
            {
                throw new ApplicationException($"Project name '{newName}' already in use");
            }

            project.UpdateBasicInfo(newName, description);
            await _projectRepository.UpdateAsync(project);

            _logger.LogInformation("Updated basic info for project {ProjectId}", projectId);
        }

        //删除项目
        public async Task DeleteProjectAsync(Guid projectId)
        {
            var project = await _projectRepository.GetByIdAsync(projectId);
            if (project == null)
                throw new ApplicationException("Project not found");

            await _projectRepository.DeleteAsync(project);
            _logger.LogInformation("Deleted project {ProjectId}", projectId);
        }
        #endregion

        #region 部署配置操作
        // 添加部署配置
        public async Task<ProjectDeploymentConfig> AddDeploymentConfigAsync(
            Guid projectId,
            string yamlContent,
            string tag,
            string description)
        {
            var project = await _projectRepository.GetWithDeploymentConfigsAsync(projectId);
            if (project == null)
                throw new ApplicationException("Project not found");

            var config = project.AddDeploymentConfig(yamlContent, tag, description);
            await _projectRepository.UpdateAsync(project);

            _logger.LogInformation(
                "Added deployment config {Tag} to project {ProjectName}",
                tag, project.Name);

            return config;
        }

        //更新当前部署配置
        public async Task UpdateCurrentConfigAsync(
            Guid projectId,
            string yamlContent,
            string changeDescription)
        {
            var project = await _projectRepository.GetWithDeploymentConfigsAsync(projectId);
            if (project == null)
                throw new ApplicationException("Project not found");

            if (project.CurrentDeploymentConfig == null)
                throw new ApplicationException("No current deployment config");

            project.UpdateCurrentConfig(yamlContent, changeDescription);
            await _projectRepository.UpdateAsync(project);

            _logger.LogInformation("Updated current config for project {ProjectName}",
                project.Name);
        }
        #endregion

        #region 版本查询
        public async Task<IReadOnlyList<ProjectDeploymentConfig>> GetDeploymentHistoryAsync(
            Guid projectId)
        {
            var project = await _projectRepository.GetWithDeploymentConfigsAsync(projectId);
            if (project == null)
                throw new ApplicationException("Project not found");

            return project.DeploymentConfigs
                .OrderByDescending(c => c.IsCurrent)
                .ThenByDescending(c => c.CreatedAt)
                .ToList();
        }

        public async Task<ProjectDeploymentConfig> GetDeploymentByTagAsync(
            Guid projectId,
            string tag)
        {
            var project = await _projectRepository.GetWithDeploymentConfigsAsync(projectId);
            if (project == null)
                throw new ApplicationException("Project not found");

            return project.DeploymentConfigs.FirstOrDefault(c => c.Tag == tag)
                   ?? throw new ApplicationException("Deployment config not found");
        }

        public async Task<ProjectDeploymentConfig> GetCurrentDeploymentAsync(Guid projectId)
        {
            var project = await _projectRepository.GetWithDeploymentConfigsAsync(projectId);
            if (project == null)
                throw new ApplicationException("Project not found");

            return project.CurrentDeploymentConfig
                   ?? throw new ApplicationException("No current deployment config");
        }
        #endregion

        #region 版本管理
        public async Task RollbackToTagAsync(Guid projectId, string tag)
        {
            var project = await _projectRepository.GetWithDeploymentConfigsAsync(projectId);
            if (project == null)
                throw new ApplicationException("Project not found");

            project.RollbackToTag(tag);
            await _projectRepository.UpdateAsync(project);

            _logger.LogInformation("Rolled back project {ProjectName} to tag {Tag}",
                project.Name, tag);
        }

        public async Task DeleteDeploymentConfigAsync(Guid projectId, string tag)
        {
            var project = await _projectRepository.GetWithDeploymentConfigsAsync(projectId);
            if (project == null)
                throw new ApplicationException("Project not found");

            var configToRemove = project.DeploymentConfigs.FirstOrDefault(c => c.Tag == tag);
            if (configToRemove == null)
                throw new ApplicationException("Deployment config not found");

            project.RemoveDeploymentConfig(configToRemove);
            await _projectRepository.UpdateAsync(project);

            _logger.LogInformation("Deleted deployment config {Tag} from project {ProjectName}",
                tag, project.Name);
        }
        #endregion


        // 辅助方法
        public async Task<bool> ProjectExistsAsync(Guid projectId)
        {
            return await _projectRepository.GetByIdAsync(projectId) != null;
        }

        public async Task<bool> TagExistsAsync(Guid projectId, string tag)
        {
            var project = await _projectRepository.GetWithDeploymentConfigsAsync(projectId);
            return project?.DeploymentConfigs.Any(c => c.Tag == tag) ?? false;
        }
    }
}
