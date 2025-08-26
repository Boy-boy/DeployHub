using ImageUploaderApi.AppServices;
using ImageUploaderApi.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ImageUploaderApi.Controllers
{
    //[ApiController]
    //[Route("api/projects/{projectName}/config")]
    //[Produces("application/json")]
    //public class ProjectConfigController : ControllerBase
    //{
    //    private readonly ProjectYamlAppService _service;
    //    private readonly ILogger<ProjectConfigController> _logger;

    //    public ProjectConfigController(
    //        ProjectYamlAppService service,
    //        ILogger<ProjectConfigController> logger)
    //    {
    //        _service = service;
    //        _logger = logger;
    //    }

    //    #region 配置管理端点

    //    /// <summary>
    //    /// 获取项目的当前配置
    //    /// </summary>
    //    /// <param name="projectName">项目名称</param>
    //    /// <returns>当前YAML配置</returns>
    //    [HttpGet("current")]
    //    [ProducesResponseType(StatusCodes.Status200OK)]
    //    [ProducesResponseType(StatusCodes.Status404NotFound)]
    //    public async Task<ActionResult<string>> GetCurrentConfig(string projectName)
    //    {
    //        try
    //        {
    //            var config = await _service.GetCurrentConfigAsync(projectName);
    //            if (config == null)
    //            {
    //                _logger.LogWarning("Current config not found for project {ProjectName}", projectName);
    //                return NotFound(new { Message = $"Current config not found for project {projectName}" });
    //            }

    //            return Ok(config);
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error getting current config for project {ProjectName}", projectName);
    //            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An error occurred while retrieving the config" });
    //        }
    //    }

    //    /// <summary>
    //    /// 更新项目的当前配置
    //    /// </summary>
    //    /// <param name="projectName">项目名称</param>
    //    /// <param name="request">更新请求</param>
    //    /// <returns>操作结果</returns>
    //    [HttpPut("current")]
    //    [ProducesResponseType(StatusCodes.Status200OK)]
    //    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    //    public async Task<IActionResult> UpdateCurrentConfig(
    //        string projectName,
    //        [FromBody] UpdateConfigRequest request)
    //    {
    //        if (!ModelState.IsValid)
    //        {
    //            return BadRequest(ModelState);
    //        }

    //        try
    //        {
    //            await _service.UpdateConfigAsync(
    //                projectName,
    //                request.Yaml,
    //                request.ChangeDescription);

    //            _logger.LogInformation("Updated current config for project {ProjectName}",
    //                projectName);

    //            return Ok(new { Message = "Config updated successfully" });
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error updating config for project {ProjectName}", projectName);
    //            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An error occurred while updating the config" });
    //        }
    //    }

    //    #endregion

    //    #region 版本管理端点

    //    /// <summary>
    //    /// 创建新版本配置
    //    /// </summary>
    //    /// <param name="projectName">项目名称</param>
    //    /// <param name="request">创建请求</param>
    //    /// <returns>新创建的版本信息</returns>
    //    [HttpPost("versions")]
    //    [ProducesResponseType(StatusCodes.Status201Created)]
    //    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    //    public async Task<ActionResult<ProjectYaml>> CreateVersion(
    //        string projectName,
    //        [FromBody] CreateVersionRequest request)
    //    {
    //        if (!ModelState.IsValid)
    //        {
    //            return BadRequest(ModelState);
    //        }

    //        try
    //        {
    //            var newVersion = await _service.CreateNewVersionAsync(
    //                projectName,
    //                request.Yaml,
    //                request.Version,
    //                request.ChangeDescription);

    //            _logger.LogInformation("Created new version {Version} for project {ProjectName}",
    //                newVersion.Version, projectName);

    //            return CreatedAtAction(
    //                nameof(GetVersion),
    //                new { projectName, version = newVersion.Version },
    //                newVersion);
    //        }
    //        catch (ArgumentException ex)
    //        {
    //            _logger.LogWarning(ex, "Invalid argument when creating version for project {ProjectName}", projectName);
    //            return BadRequest(new { ex.Message });
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error creating new version for project {ProjectName}", projectName);
    //            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An error occurred while creating the version" });
    //        }
    //    }

    //    /// <summary>
    //    /// 获取特定版本配置
    //    /// </summary>
    //    /// <param name="projectName">项目名称</param>
    //    /// <param name="version">版本号</param>
    //    /// <returns>特定版本的配置</returns>
    //    [HttpGet("versions/{version}")]
    //    [ProducesResponseType(StatusCodes.Status200OK)]
    //    [ProducesResponseType(StatusCodes.Status404NotFound)]
    //    public async Task<ActionResult<string>> GetVersion(
    //        string projectName,
    //        string version)
    //    {
    //        try
    //        {
    //            var config = await _service.GetConfigByVersionAsync(projectName, version);
    //            if (config == null)
    //            {
    //                _logger.LogWarning("Version {Version} not found for project {ProjectName}", version, projectName);
    //                return NotFound(new { Message = $"Version {version} not found for project {projectName}" });
    //            }

    //            return Ok(config);
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error getting version {Version} for project {ProjectName}", version, projectName);
    //            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An error occurred while retrieving the version" });
    //        }
    //    }

    //    /// <summary>
    //    /// 回滚到特定版本
    //    /// </summary>
    //    /// <param name="projectName">项目名称</param>
    //    /// <param name="version">要回滚到的版本号</param>
    //    /// <returns>操作结果</returns>
    //    [HttpPost("versions/{version}/rollback")]
    //    [ProducesResponseType(StatusCodes.Status200OK)]
    //    [ProducesResponseType(StatusCodes.Status404NotFound)]
    //    public async Task<IActionResult> RollbackToVersion(
    //        string projectName,
    //        string version)
    //    {
    //        try
    //        {
    //            var success = await _service.RollbackToVersionAsync(projectName, version);
    //            if (!success)
    //            {
    //                _logger.LogWarning("Rollback failed - version {Version} not found for project {ProjectName}", version, projectName);
    //                return NotFound(new { Message = $"Version {version} not found for project {projectName}" });
    //            }

    //            _logger.LogInformation("Rolled back project {ProjectName} to version {Version}", projectName, version);
    //            return Ok(new { Message = $"Successfully rolled back to version {version}" });
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error rolling back project {ProjectName} to version {Version}", projectName, version);
    //            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An error occurred during rollback" });
    //        }
    //    }

    //    /// <summary>
    //    /// 获取项目的版本历史
    //    /// </summary>
    //    /// <param name="projectName">项目名称</param>
    //    /// <returns>版本历史列表</returns>
    //    [HttpGet("versions")]
    //    [ProducesResponseType(StatusCodes.Status200OK)]
    //    public async Task<ActionResult<IEnumerable<ProjectYaml>>> GetVersionHistory(string projectName)
    //    {
    //        try
    //        {
    //            var history = await _service.GetVersionHistoryAsync(projectName);
    //            return Ok(history);
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error getting version history for project {ProjectName}", projectName);
    //            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An error occurred while retrieving version history" });
    //        }
    //    }

    //    #endregion

    //    #region 项目管理端点

    //    /// <summary>
    //    /// 获取所有项目名称
    //    /// </summary>
    //    /// <returns>项目名称列表</returns>
    //    [HttpGet("/api/projects")]
    //    [ProducesResponseType(StatusCodes.Status200OK)]
    //    public async Task<ActionResult<IEnumerable<string>>> GetAllProjects()
    //    {
    //        try
    //        {
    //            var projectNames = await _service.GetAllProjectNamesAsync();
    //            return Ok(projectNames);
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error getting all project names");
    //            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An error occurred while retrieving projects" });
    //        }
    //    }

    //    /// <summary>
    //    /// 删除项目及其所有版本
    //    /// </summary>
    //    /// <param name="projectName">项目名称</param>
    //    /// <returns>操作结果</returns>
    //    [HttpDelete]
    //    [ProducesResponseType(StatusCodes.Status200OK)]
    //    [ProducesResponseType(StatusCodes.Status404NotFound)]
    //    public async Task<IActionResult> DeleteProject(string projectName)
    //    {
    //        try
    //        {
    //            if (!await _service.ProjectExistsAsync(projectName))
    //            {
    //                _logger.LogWarning("Project {ProjectName} not found for deletion", projectName);
    //                return NotFound(new { Message = $"Project {projectName} not found" });
    //            }

    //            await _service.DeleteProjectAsync(projectName);
    //            _logger.LogInformation("Deleted project {ProjectName} and all its versions", projectName);
    //            return Ok(new { Message = $"Project {projectName} and all its versions have been deleted" });
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error deleting project {ProjectName}", projectName);
    //            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An error occurred while deleting the project" });
    //        }
    //    }

    //    /// <summary>
    //    /// 删除特定版本
    //    /// </summary>
    //    /// <param name="projectName">项目名称</param>
    //    /// <param name="version">版本号</param>
    //    /// <returns>操作结果</returns>
    //    [HttpDelete("versions/{version}")]
    //    [ProducesResponseType(StatusCodes.Status200OK)]
    //    [ProducesResponseType(StatusCodes.Status404NotFound)]
    //    public async Task<IActionResult> DeleteVersion(
    //        string projectName,
    //        string version)
    //    {
    //        try
    //        {
    //            if (!await _service.VersionExistsAsync(projectName, version))
    //            {
    //                _logger.LogWarning("Version {Version} not found for project {ProjectName}", version, projectName);
    //                return NotFound(new { Message = $"Version {version} not found for project {projectName}" });
    //            }

    //            await _service.DeleteVersionAsync(projectName, version);
    //            _logger.LogInformation("Deleted version {Version} for project {ProjectName}", version, projectName);
    //            return Ok(new { Message = $"Version {version} has been deleted" });
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error deleting version {Version} for project {ProjectName}", version, projectName);
    //            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An error occurred while deleting the version" });
    //        }
    //    }

    //    #endregion
    //}

    //#region 请求模型

    //public class CreateVersionRequest
    //{
    //    [Required]
    //    public string Yaml { get; set; }

    //    [Required]
    //    public string Version { get; set; }

    //    public string ChangeDescription { get; set; }
    //}

    //public class UpdateConfigRequest
    //{
    //    [Required]
    //    public string Yaml { get; set; }

    //    public string ChangeDescription { get; set; }
    //}

    //#endregion
}
