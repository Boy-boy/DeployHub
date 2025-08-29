using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace ImageUploaderApi.Domain.Entities
{
    // 部署配置实体
    public class ProjectDeploymentConfig
    {
        public Guid Id { get; private set; }
        public Guid ProjectId { get; private set; }
        public string Tag { get; private set; }  // 版本标识
        public string YamlContent { get; private set; }
        public bool IsCurrent { get; private set; } // 标记是否为当前部署版本
        public string Description { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        // 私有构造函数（供EF Core使用）
        private ProjectDeploymentConfig() { }

        // 公有构造函数
        public ProjectDeploymentConfig(
            Guid projectId,
            string yamlContent,
            string tag,
            string description)
        {
            ValidYaml(yamlContent);
            Id = Guid.NewGuid();
            ProjectId = projectId;
            YamlContent = yamlContent ?? throw new ArgumentNullException(nameof(yamlContent));
            Tag = tag ?? throw new ArgumentNullException(nameof(tag));
            IsCurrent = false;
            Description = description;
            CreatedAt = DateTime.UtcNow;
        }

        // 将目标版本标记为当前
        public void MarkAsCurrent() => IsCurrent = true;
        public void MarkAsNotCurrent() => IsCurrent = false;

        // 更新方法
        public void Update(string yamlContent, string description)
        {
            ValidYaml(yamlContent);
            YamlContent = yamlContent ?? throw new ArgumentNullException(nameof(yamlContent));
            Description = description;
            UpdatedAt = DateTime.UtcNow;
        }

        private void ValidYaml(string yamlContent)
        {
            if (string.IsNullOrWhiteSpace(yamlContent))
            {
                throw new DomainException("YAML 内容为空");
            }

            try
            {
                using var reader = new StringReader(yamlContent);
                var yamlStream = new YamlStream();
                yamlStream.Load(reader);

                if (yamlStream.Documents.Count == 0)
                {
                    throw new DomainException("YAML 文件不包含任何文档");
                }

                for (var i = 0; i < yamlStream.Documents.Count; i++)
                {
                    var document = yamlStream.Documents[i];

                    if (document.RootNode == null)
                    {
                        throw new DomainException($"第 {i + 1} 个文档为空");
                    }

                    if (document.RootNode is not YamlMappingNode and not YamlSequenceNode)
                    {
                        throw new DomainException($"第 {i + 1} 个文档根节点必须是映射或序列，当前类型: {document.RootNode.GetType().Name}");
                    }
                }
            }
            catch (YamlException ex)
            {
                var location = $"位置: Line {ex.Start.Line}, Column {ex.Start.Column} - ";
                throw new DomainException($"YAML 语法错误: {location}{ex.Message}");
            }
            catch (DomainException)
            {
                // 重新抛出业务异常
                throw;
            }
            catch (Exception ex)
            {
                throw new DomainException($"YAML 验证失败: {ex.Message}");
            }
        }
    }
}
