using ImageUploaderApi.Domain.Entities;
using ImageUploaderApi.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ImageUploaderApi.Persistence.Repositories
{
    public class ProjectYamlRepository : IProjectYamlRepository
    {
        private readonly AppDbContext _context;

        public ProjectYamlRepository(AppDbContext context)
        {
            _context = context;
        }

        // 查询方法实现
        public async Task<ProjectYaml> GetCurrentVersionAsync(string projectName)
        {
            return await _context.ProjectYamls
                .FirstOrDefaultAsync(p => p.ProjectName == projectName && p.IsCurrent);
        }

        public async Task<IEnumerable<ProjectYaml>> GetVersionHistoryAsync(string projectName)
        {
            return await _context.ProjectYamls
                .Where(p => p.ProjectName == projectName)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<ProjectYaml> GetByVersionAsync(string projectName, string version)
        {
            return await _context.ProjectYamls
                .FirstOrDefaultAsync(p => p.ProjectName == projectName && p.Version == version);
        }

        public async Task<IEnumerable<ProjectYaml>> GetAllAsync()
        {
            return await _context.ProjectYamls.ToListAsync();
        }

        public async Task<ProjectYaml> GetByIdAsync(int id)
        {
            return await _context.ProjectYamls.FindAsync(id);
        }

        // 新增方法实现
        public async Task AddAsync(ProjectYaml projectYaml)
        {
            await _context.ProjectYamls.AddAsync(projectYaml);
            await _context.SaveChangesAsync();
        }

        public async Task AddRangeAsync(IEnumerable<ProjectYaml> projectYamls)
        {
            await _context.ProjectYamls.AddRangeAsync(projectYamls);
            await _context.SaveChangesAsync();
        }

        // 修改方法实现
        public async Task UpdateAsync(ProjectYaml projectYaml)
        {
            _context.ProjectYamls.Update(projectYaml);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateRangeAsync(IEnumerable<ProjectYaml> projectYamls)
        {
            _context.ProjectYamls.UpdateRange(projectYamls);
            await _context.SaveChangesAsync();
        }

        // 删除方法实现
        public async Task DeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                await DeleteAsync(entity);
            }
        }

        public async Task DeleteAsync(ProjectYaml projectYaml)
        {
            _context.ProjectYamls.Remove(projectYaml);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteRangeAsync(IEnumerable<ProjectYaml> projectYamls)
        {
            _context.ProjectYamls.RemoveRange(projectYamls);
            await _context.SaveChangesAsync();
        }

        // 版本控制专用方法实现
        public async Task MarkAllAsNonCurrentAsync(string projectName)
        {
            await _context.ProjectYamls
                .Where(p => p.ProjectName == projectName && p.IsCurrent)
                .ForEachAsync(p => p.MarkAsNotCurrent());

            await _context.SaveChangesAsync();
        }
        public async Task<int> GetVersionCountAsync(string projectName)
        {
            return await _context.ProjectYamls
                .CountAsync(p => p.ProjectName == projectName);
        }

        public async Task<bool> VersionExistsAsync(string projectName, string version)
        {
            return await _context.ProjectYamls
                .AnyAsync(p => p.ProjectName == projectName && p.Version == version);
        }
    }
}
