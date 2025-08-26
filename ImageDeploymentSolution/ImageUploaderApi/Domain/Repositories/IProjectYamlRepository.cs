using ImageUploaderApi.Domain.Entities;

namespace ImageUploaderApi.Domain.Repositories
{
    public interface IProjectYamlRepository
    {
        // 查询方法
        Task<ProjectYaml> GetCurrentVersionAsync(string projectName);
        Task<IEnumerable<ProjectYaml>> GetVersionHistoryAsync(string projectName);
        Task<ProjectYaml> GetByVersionAsync(string projectName, string version);
        Task<IEnumerable<ProjectYaml>> GetAllAsync();
        Task<ProjectYaml> GetByIdAsync(int id);

        // 新增方法
        Task AddAsync(ProjectYaml projectYaml);
        Task AddRangeAsync(IEnumerable<ProjectYaml> projectYamls);

        // 修改方法
        Task UpdateAsync(ProjectYaml projectYaml);
        Task UpdateRangeAsync(IEnumerable<ProjectYaml> projectYamls);

        // 删除方法
        Task DeleteAsync(int id);
        Task DeleteAsync(ProjectYaml projectYaml);
        Task DeleteRangeAsync(IEnumerable<ProjectYaml> projectYamls);

        // 版本控制专用方法
        Task MarkAllAsNonCurrentAsync(string projectName);
        Task<int> GetVersionCountAsync(string projectName);
        Task<bool> VersionExistsAsync(string projectName, string version);
    }
}
