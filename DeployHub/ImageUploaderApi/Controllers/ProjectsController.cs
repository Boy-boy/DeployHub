using ImageUploaderApi.AppServices;
using ImageUploaderApi.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ImageUploaderApi.Controllers
{
    [ApiController]
    [Route("api/projects")]
    [Authorize]
    public class ProjectsController : ControllerBase
    {
        private readonly ProjectAppService _projectAppService;
        private readonly ILogger<ProjectsController> _logger;

        public ProjectsController(
            ProjectAppService projectAppService,
            ILogger<ProjectsController> logger)
        {
            _projectAppService = projectAppService;
            _logger = logger;
        }

        #region 项目管理
        /// <summary>
        /// 获取所有项目
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllProjects()
        {
            var projects = await _projectAppService.GetAllProjectsAsync();
            return Ok(projects.Select(p => new ProjectDto(p)));
        }

        /// <summary>
        /// 创建新项目
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest request)
        {
            try
            {
                var project = await _projectAppService.CreateProjectAsync(request.Name, request.Description);
                return Ok(new ProjectDto(project));
            }
            catch (ApplicationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating project");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// 更新项目信息
        /// </summary>
        [HttpPut("{projectId}")]
        public async Task<IActionResult> UpdateProject(
            Guid projectId,
            [FromBody] UpdateProjectRequest request)
        {
            try
            {
                await _projectAppService.UpdateProjectAsync(
                    projectId,
                    request.NewName,
                    request.Description);

                return NoContent();
            }
            catch (ApplicationException ex) when (ex.Message.Contains("not found"))
            {
                return NotFound();
            }
            catch (ApplicationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating project {ProjectId}", projectId);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// 删除项目
        /// </summary>
        [HttpDelete("{projectId}")]
        public async Task<IActionResult> DeleteProject(Guid projectId)
        {
            try
            {
                await _projectAppService.DeleteProjectAsync(projectId);
                return NoContent();
            }
            catch (ApplicationException ex) when (ex.Message.Contains("not found"))
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting project {ProjectId}", projectId);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        #endregion

        #region 部署配置管理

        /// <summary>
        /// 获取项目所有部署配置历史
        /// </summary>
        [HttpGet("{projectId}/deployments")]
        public async Task<IActionResult> GetDeploymentHistory(Guid projectId)
        {
            try
            {
                var configs = await _projectAppService.GetDeploymentHistoryAsync(projectId);
                return Ok(configs.Select(c => new DeploymentConfigDto(c)));
            }
            catch (ApplicationException ex) when (ex.Message.Contains("not found"))
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting deployment history for project {ProjectId}", projectId);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// 获取当前部署配置
        /// </summary>
        [HttpGet("{projectId}/deployments/current")]
        public async Task<IActionResult> GetCurrentDeployment(Guid projectId)
        {
            try
            {
                var config = await _projectAppService.GetCurrentDeploymentAsync(projectId);
                return Ok(new DeploymentConfigDto(config));
            }
            catch (ApplicationException ex) when (ex.Message.Contains("not found"))
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current deployment for project {ProjectId}", projectId);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// 添加部署配置
        /// </summary>
        [HttpPost("{projectId}/deployments")]
        public async Task<IActionResult> AddDeploymentConfig(
            Guid projectId,
            [FromBody] AddDeploymentConfigRequest request)
        {
            try
            {
                var config = await _projectAppService.AddDeploymentConfigAsync(
                    projectId,
                    request.YamlContent,
                    request.Tag,
                    request.Description);

                return Ok(new DeploymentConfigDto(config));
            }
            catch (ApplicationException ex) when (ex.Message.Contains("not found"))
            {
                return NotFound();
            }
            catch (ApplicationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding deployment config to project {ProjectId}", projectId);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// 更新当前部署配置
        /// </summary>
        [HttpPut("{projectId}/deployments/current")]
        public async Task<IActionResult> UpdateCurrentDeployment(
            Guid projectId,
            [FromBody] UpdateDeploymentConfigRequest request)
        {
            try
            {
                await _projectAppService.UpdateCurrentConfigAsync(
                    projectId,
                    request.YamlContent,
                    request.ChangeDescription);

                return NoContent();
            }
            catch (ApplicationException ex) when (ex.Message.Contains("not found"))
            {
                return NotFound();
            }
            catch (ApplicationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating current deployment for project {ProjectId}", projectId);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// 回滚到指定版本
        /// </summary>
        [HttpPost("{projectId}/deployments/{tag}/rollback")]
        public async Task<IActionResult> RollbackToTag(Guid projectId, string tag)
        {
            try
            {
                await _projectAppService.RollbackToTagAsync(projectId, tag);
                return NoContent();
            }
            catch (ApplicationException ex) when (ex.Message.Contains("not found"))
            {
                return NotFound();
            }
            catch (ApplicationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rolling back project {ProjectId} to tag {Tag}", projectId, tag);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// 删除部署配置
        /// </summary>
        [HttpDelete("{projectId}/deployments/{tag}")]
        public async Task<IActionResult> DeleteDeploymentConfig(Guid projectId, string tag)
        {
            try
            {
                await _projectAppService.DeleteDeploymentConfigAsync(projectId, tag);
                return NoContent();
            }
            catch (ApplicationException ex) when (ex.Message.Contains("not found"))
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting deployment {Tag} from project {ProjectId}", tag, projectId);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        #endregion

        #region DTOs

        public class ProjectDto
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }

            public ProjectDto(Project project)
            {
                Id = project.Id;
                Name = project.Name;
                Description = project.Description;
                CreatedAt = project.CreatedAt;
                UpdatedAt = project.UpdatedAt;
            }
        }

        public class DeploymentConfigDto
        {
            public Guid Id { get; set; }
            public string Tag { get; set; }
            public string Yaml { get; set; }
            public string Description { get; set; }
            public DateTime CreatedAt { get; set; }

            public DeploymentConfigDto(ProjectDeploymentConfig config)
            {
                Id = config.Id;
                Tag = config.Tag;
                Yaml = config.YamlContent;
                Description = config.Description;
                CreatedAt = config.CreatedAt;
            }
        }

        public class CreateProjectRequest
        {
            [Required]
            [StringLength(200)]
            public string Name { get; set; }

            [StringLength(1000)]
            public string Description { get; set; }
        }

        public class UpdateProjectRequest
        {
            [Required]
            [StringLength(200)]
            public string NewName { get; set; }

            [StringLength(1000)]
            public string Description { get; set; }
        }

        public class AddDeploymentConfigRequest
        {
            [Required]
            public string YamlContent { get; set; }

            [Required]
            [StringLength(100)]
            public string Tag { get; set; }

            [StringLength(500)]
            public string Description { get; set; }
        }

        public class UpdateDeploymentConfigRequest
        {
            [Required]
            public string YamlContent { get; set; }

            [StringLength(500)]
            public string ChangeDescription { get; set; }
        }

        #endregion
    }
}
