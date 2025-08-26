using ImageUploaderApi.Domain.Entities;
using ImageUploaderApi.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ImageUploaderApi.Persistence.Repositories
{
    // EF Core实现
    public class ProjectRepository : IProjectRepository
    {
        private readonly AppDbContext _context;

        public ProjectRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Project> GetByIdAsync(Guid id)
        {
            return await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<Project>> GetAllAsync()
        {
            return await _context.Projects
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Project> GetWithDeploymentConfigsAsync(Guid id)
        {
            return await _context.Projects
                .Include(p => p.DeploymentConfigs)
                .FirstOrDefaultAsync(p => p.Id == id);
        }


        public async Task<Project> GetByNameAsync(string name)
        {
            return await _context.Projects
                .FirstOrDefaultAsync(p => p.Name == name);
        }

        public async Task<bool> ExistsByNameAsync(string name)
        {
            return await _context.Projects.AnyAsync(p => p.Name == name);
        }

        public async Task AddAsync(Project entity)
        {
            await _context.Projects.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Project entity)
        {
            _context.Projects.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Project entity)
        {
            _context.Projects.Remove(entity);
            await _context.SaveChangesAsync();
        }



    }
}
