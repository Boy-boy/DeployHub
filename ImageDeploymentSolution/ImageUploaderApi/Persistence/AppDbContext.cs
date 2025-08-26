using ImageUploaderApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ImageUploaderApi.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<ProjectYaml> ProjectYamls { get; set; }

        public DbSet<Project> Projects { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProjectYaml>()
                .HasIndex(p => new { p.ProjectName, p.Version })
                .IsUnique();

            modelBuilder.Entity<ProjectYaml>()
                .HasIndex(p => p.ProjectName);

            // 配置Project实体
            modelBuilder.Entity<Project>(entity =>
            {
                entity.ToTable("Projects");
                entity.HasKey(p => p.Id);

                entity.Property(p => p.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(p => p.Description)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(p => p.CreatedAt)
                    .IsRequired();

                entity.Property(p => p.UpdatedAt);

                // 配置一对多关系
                entity.HasMany(p => p.DeploymentConfigs)
                    .WithOne()
                    .HasForeignKey(c => c.ProjectId) // 使用显式ProjectId属性
                    .OnDelete(DeleteBehavior.Cascade);

                // 配置CurrentDeploymentConfigId作为普通属性
                entity.Property(p => p.CurrentDeploymentConfigId)
                    .IsRequired(false);

                entity.HasIndex(p => p.Name)
                    .IsUnique();
            });

            // 配置ProjectDeploymentConfig实体
            modelBuilder.Entity<ProjectDeploymentConfig>(entity =>
            {
                entity.ToTable("ProjectDeploymentConfigs");
                entity.HasKey(c => c.Id);

                entity.Property(c => c.Id).ValueGeneratedNever();

                // 配置ProjectId
                entity.Property(c => c.ProjectId)
                    .IsRequired();

                entity.Property(c => c.Tag)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(c => c.YamlContent)
                    .IsRequired();

                entity.Property(c => c.Description)
                    .HasMaxLength(500);

                entity.Property(c => c.CreatedAt)
                    .IsRequired();

                // 复合索引
                entity.HasIndex(c => new { c.ProjectId, c.Tag })
                    .IsUnique();
            });

        }
    }
}
