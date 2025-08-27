using System.ComponentModel.DataAnnotations.Schema;

namespace ImageUploaderApi.Domain.Entities
{
    /// <summary>
    /// 项目实体
    /// </summary>
    public class Project
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        // 当前部署配置（导航属性）
        public Guid? CurrentDeploymentConfigId { get; private set; }
        [NotMapped]
        public ProjectDeploymentConfig CurrentDeploymentConfig
        {
            get
            {
                return CurrentDeploymentConfigId == null
                    ? null
                    : DeploymentConfigs.FirstOrDefault(p => p.Id == CurrentDeploymentConfigId);
            }
        }

        // 所有部署配置
        public List<ProjectDeploymentConfig> DeploymentConfigs { get; private set; } = [];

        // 私有构造函数（供EF Core使用）
        private Project() { }

        // 公有构造函数
        public Project(string name, string description)
        {
            Id = Guid.NewGuid();
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description;
            CreatedAt = DateTime.UtcNow;
        }

        // 更新项目基本信息
        public void UpdateBasicInfo(string newName, string description = null)
        {
            if (string.IsNullOrWhiteSpace(newName))
                throw new DomainException("Project name cannot be empty");

            Name = newName;
            Description = description;
            UpdatedAt = DateTime.UtcNow;
        }

        // 添加部署配置
        public ProjectDeploymentConfig AddDeploymentConfig(
            string yamlContent,
            string tag,
            string description)
        {
            if (string.IsNullOrWhiteSpace(tag))
                throw new DomainException("Tag cannot be empty");

            if (DeploymentConfigs.Any(c => c.Tag == tag))
                throw new DomainException($"Tag '{tag}' already exists");

            var config = new ProjectDeploymentConfig(
                Id,
                yamlContent,
                tag,
                description);

            DeploymentConfigs.Add(config);
            //CurrentDeploymentConfigId = config.Id;
            UpdatedAt = DateTime.UtcNow;
            return config;
        }

        // 回滚到指定Tag
        public void RollbackToTag(string tag)
        {
            var targetConfig = DeploymentConfigs.FirstOrDefault(c => c.Tag == tag);
            if (targetConfig == null)
                throw new DomainException($"Config with tag '{tag}' not found");
            CurrentDeploymentConfigId = targetConfig.Id;
            UpdatedAt = DateTime.UtcNow;
        }

        // 更新当前部署配置
        public void UpdateCurrentConfig(string yamlContent, string changeDescription)
        {
            if (CurrentDeploymentConfig == null)
                throw new DomainException("No current deployment config");

            CurrentDeploymentConfig.Update(yamlContent, changeDescription);
            UpdatedAt = DateTime.UtcNow;
        }

        public void RemoveDeploymentConfig(ProjectDeploymentConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            // 如果要删除的是当前配置，需要清除引用
            if (CurrentDeploymentConfigId == config.Id)
            {
                CurrentDeploymentConfigId = null;
            }

            DeploymentConfigs.Remove(config);
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
