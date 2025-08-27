using ImageUploaderApi.Domain.Entities;

namespace ImageUploaderApi.Domain.Repositories
{
    public interface IProjectRepository : IRepository<Project>
    {
        Task<Project> GetWithDeploymentConfigsAsync(Guid id);

        Task<Project> GetByNameAsync(string name);
        Task<bool> ExistsByNameAsync(string name);
    }
}
