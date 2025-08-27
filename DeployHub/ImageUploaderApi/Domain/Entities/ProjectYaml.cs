using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using YamlDotNet.RepresentationModel;

namespace ImageUploaderApi.Domain.Entities
{
    public class ProjectYaml
    {
        private ProjectYaml() { } // For EF Core
        public ProjectYaml(string projectName,
            string version,
            string yamlContent,
            string changeDescription,
            bool isCurrent = true)
        {
            Id = Guid.NewGuid();
            ProjectName = projectName ?? throw new ArgumentNullException(nameof(projectName));
            Version = version ?? throw new ArgumentNullException(nameof(version));
            IsCurrent = isCurrent;
            YamlContent = yamlContent ?? throw new ArgumentNullException(nameof(yamlContent));
            ChangeDescription = changeDescription;
            CreatedAt = DateTime.Now;
        }

        public Guid Id { get; private set; }
        public string ProjectName { get; private set; }
        public string Version { get; private set; } // 版本标识 (如"latest"， "1.0", "2.0")
        public bool IsCurrent { get; private set; } // 标记是否为当前版本
        public string YamlContent { get; private set; }
        public string ChangeDescription { get; private set; } // 变更描述
        public DateTime CreatedAt { get; private set; }

        public void MarkAsCurrent() => IsCurrent = true;
        public void MarkAsNotCurrent() => IsCurrent = false;

        public void UpdateInfo(string yamlContent,
            string changeDescription)
        {
            YamlContent = yamlContent ?? throw new ArgumentNullException(nameof(yamlContent));
            ChangeDescription = changeDescription;
        }
    }
}
