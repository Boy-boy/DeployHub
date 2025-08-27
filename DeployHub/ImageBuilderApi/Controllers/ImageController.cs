using Docker.DotNet;
using Docker.DotNet.Models;
using ImageBuilderApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Formats.Tar;
using System.IO.Compression;

[ApiController]
[Route("api/[controller]")]
public class ImageController : ControllerBase
{
    private readonly MinIOService _minIoService;

    public ImageController(MinIOService minIoService)
    {
        _minIoService = minIoService;
    }
    [HttpPost("build")]
    public async Task<IActionResult> BuildDockerImage([FromBody] BuildDockerImageRequestDto request)
    {
        try
        {
            // 连接到 Docker 守护进程（通过 Unix 套接字）
            var client = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock"))
                .CreateClient();

            // 准备构建参数
            var buildParameters = new ImageBuildParameters
            {
                Tags = new List<string> { request.ImageTag }, // 镜像标签
                Dockerfile = "Dockerfile", // Dockerfile 文件名
                Remove = true // 构建成功后删除中间层
            };

            // 从 MinIO 下载文件
            var fileStream = await _minIoService.DownloadFileAsync("docker-image-upload-bucket", request.FileName);

            // 开始构建镜像
            await client.Images.BuildImageFromDockerfileAsync(
                buildParameters,
                fileStream,
                new List<AuthConfig>(), // 可选：认证信息
                new Dictionary<string, string>(), // 可选：构建参数
                new Progress<JSONMessage>(),
                CancellationToken.None);

            // 获取节点 IP
            var nodeIp = Environment.GetEnvironmentVariable("NODE_IP");

            return Ok(new
            {
                Message = $"Image [{request.ImageTag}] build successful.",
                NodeIp = nodeIp
            });
        }
        catch (DockerApiException ex)
        {
            // 获取节点 IP
            var nodeIp = Environment.GetEnvironmentVariable("NODE_IP");

            return BadRequest(new
            {
                Message = $"Failed to build image [{request.ImageTag}]: {ex.Message}",
                NodeIp = nodeIp
            });
        }
        catch (Exception ex)
        {
            // 获取节点 IP
            var nodeIp = Environment.GetEnvironmentVariable("NODE_IP");

            return StatusCode(500, new
            {
                Message = $"An error occurred while building image [{request.ImageTag}]: {ex.Message}",
                NodeIp = nodeIp
            });
        }
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteDockerImage([FromQuery] string imageName)
    {
        try
        {
            // 连接到 Docker 守护进程
            var client = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock"))
                .CreateClient();

            // 删除镜像
            await client.Images.DeleteImageAsync(imageName, new ImageDeleteParameters { Force = true });

            // 获取节点 IP
            var nodeIp = Environment.GetEnvironmentVariable("NODE_IP");

            return Ok(new
            {
                Message = $"Image [{imageName}] deleted successfully.",
                NodeIp = nodeIp
            });
        }
        catch (DockerImageNotFoundException ex)
        {
            return NotFound(new
            {
                Message = $"Image [{imageName}] not found: {ex.Message}",
                NodeIp = Environment.GetEnvironmentVariable("NODE_IP")
            });
        }
        catch (DockerApiException ex)
        {
            return BadRequest(new
            {
                Message = $"Failed to delete image [{imageName}]: {ex.Message}",
                NodeIp = Environment.GetEnvironmentVariable("NODE_IP")
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Message = $"An error occurred while deleting image [{imageName}]: {ex.Message}",
                NodeIp = Environment.GetEnvironmentVariable("NODE_IP")
            });
        }
    }

    [HttpGet("list")]
    public async Task<IActionResult> ListDockerImages()
    {
        // 获取节点 IP
        var nodeIp = Environment.GetEnvironmentVariable("NODE_IP");

        try
        {
            // 连接到 Docker 守护进程
            var client = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock"))
                .CreateClient();

            // 获取镜像列表
            var images = await client.Images.ListImagesAsync(new ImagesListParameters { All = true });

            // 返回镜像列表
            var imageList = images.Where(image => image.RepoTags.Any())
                .Select(image => new
                {
                    Id = image.ID,
                    Tags = image.RepoTags,
                    Created = image.Created,
                    Size = image.Size
                }).ToList();

            return Ok(new
            {
                Success = true,
                Message = "Images listed successfully.",
                Images = imageList,
                NodeIp = nodeIp
            });
        }
        catch (DockerApiException ex)
        {
            return BadRequest(new
            {
                Success = false,
                Message = $"Failed to list images: {ex.Message}",
                Images = (object)null!, // 镜像列表为空
                NodeIp = nodeIp
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Success = false,
                Message = $"An error occurred while listing images: {ex.Message}",
                Images = (object)null!, // 镜像列表为空
                NodeIp = nodeIp
            });
        }
    }

    [HttpGet("inspect")]
    public async Task<IActionResult> InspectDockerImage([FromQuery] string imageName)
    {
        // 获取节点 IP
        var nodeIp = Environment.GetEnvironmentVariable("NODE_IP");

        try
        {
            // 连接到 Docker 守护进程
            var client = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock"))
                .CreateClient();

            // 查询镜像详情
            var image = await client.Images.InspectImageAsync(imageName);

            // 返回镜像详情
            var imageDetails = new
            {
                Id = image.ID,
                Tags = image.RepoTags,
                Created = image.Created,
                Size = image.Size,
                Architecture = image.Architecture,
                Os = image.Os,
                Config = image.Config
            };

            return Ok(new
            {
                Success = true,
                Message = "Image inspected successfully.",
                Image = imageDetails,
                NodeIp = nodeIp
            });
        }
        catch (DockerImageNotFoundException ex)
        {
            return NotFound(new
            {
                Success = false,
                Message = $"Image [{imageName}] not found: {ex.Message}",
                Image = (object)null!, // 镜像详情为空
                NodeIp = nodeIp
            });
        }
        catch (DockerApiException ex)
        {
            return BadRequest(new
            {
                Success = false,
                Message = $"Failed to inspect image [{imageName}]: {ex.Message}",
                Image = (object)null!, // 镜像详情为空
                NodeIp = nodeIp
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Success = false,
                Message = $"An error occurred while inspecting image [{imageName}]: {ex.Message}",
                Image = (object)null!, // 镜像详情为空
                NodeIp = nodeIp
            });
        }
    }
}

public class StreamImageBuildContext
{
    private readonly Stream _stream;

    public StreamImageBuildContext(Stream stream)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
    }

    public Stream GetStream()
    {
        return _stream;
    }

    public void Dispose()
    {
        _stream.Dispose();
    }
}

// 构建镜像请求 DTO
public class BuildDockerImageRequestDto
{
    public string ImageTag { get; set; } // 镜像标签（如 my-image:latest）
    public string FileName { get; set; }
}

// 构建上下文（将 Dockerfile 和构建上下文打包为 tar.gz）
public class TarGzImageBuildContext : IDisposable
{
    private readonly string _tarGzFilePath;

    public TarGzImageBuildContext(string dockerFilePath)
    {
        _tarGzFilePath = Path.GetTempFileName(); // 创建临时文件
        CreateTarGz(dockerFilePath, _tarGzFilePath); // 打包为 tar.gz
    }

    public Stream GetStream()
    {
        return new FileStream(_tarGzFilePath, FileMode.Open, FileAccess.Read);
    }

    public void Dispose()
    {
        if (File.Exists(_tarGzFilePath))
        {
            File.Delete(_tarGzFilePath); // 删除临时文件
        }
    }

    private void CreateTarGz(string sourceDir, string outputFilePath)
    {
        using var outputStream = File.Create(outputFilePath);
        using var tarWriter = new TarWriter(outputStream);
        AddDirectoryToTar(tarWriter, sourceDir, string.Empty);
    }

    private void AddDirectoryToTar(TarWriter tarWriter, string sourceDir, string relativePath)
    {
        // 添加当前目录下的所有文件
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileInfo = new FileInfo(file);

            // 检查文件是否为压缩文件
            if (IsCompressedFile(fileInfo))
            {
                // 解压压缩文件
                var extractDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(extractDir);
                ExtractCompressedFile(fileInfo.FullName, extractDir);

                // 将解压后的内容添加到 tar 中
                AddDirectoryToTar(tarWriter, extractDir, relativePath);

                // 清理临时解压目录
                Directory.Delete(extractDir, recursive: true);
            }
            else
            {
                // 普通文件直接添加到 tar 中
                var entryName = Path.Combine(relativePath, fileInfo.Name);
                tarWriter.WriteEntry(fileInfo.FullName, entryName);
            }
        }

        // 递归添加子目录
        foreach (var directory in Directory.GetDirectories(sourceDir))
        {
            var directoryInfo = new DirectoryInfo(directory);
            var entryName = Path.Combine(relativePath, directoryInfo.Name);
            AddDirectoryToTar(tarWriter, directory, entryName);
        }
    }

    private bool IsCompressedFile(FileInfo fileInfo)
    {
        var compressedExtensions = new[] { ".tar", ".zip", ".gz", ".tgz" };
        return compressedExtensions.Contains(fileInfo.Extension.ToLower());
    }
    private void ExtractCompressedFile(string compressedFilePath, string extractDir)
    {
        var fileInfo = new FileInfo(compressedFilePath);

        switch (fileInfo.Extension.ToLower())
        {
            case ".tar":
                ExtractTarFile(compressedFilePath, extractDir);
                break;
            case ".zip":
                ExtractZipFile(compressedFilePath, extractDir);
                break;
            case ".gz":
            case ".tgz":
                ExtractGzFile(compressedFilePath, extractDir);
                break;
            default:
                throw new NotSupportedException($"不支持的压缩文件格式: {fileInfo.Extension}");
        }
    }

    private void ExtractTarFile(string tarFilePath, string extractDir)
    {
        using var tarFile = File.OpenRead(tarFilePath);
        TarFile.ExtractToDirectory(tarFile, extractDir, overwriteFiles: true);
    }

    private void ExtractZipFile(string zipFilePath, string extractDir)
    {
        ZipFile.ExtractToDirectory(zipFilePath, extractDir);
    }

    private void ExtractGzFile(string gzFilePath, string extractDir)
    {
        using var gzFile = File.OpenRead(gzFilePath);
        using var gzStream = new GZipStream(gzFile, CompressionMode.Decompress);
        using var tarReader = new TarReader(gzStream);

        TarEntry entry;
        while ((entry = tarReader.GetNextEntry()) != null)
        {
            // 确保目标路径在解压目录内（防止路径遍历攻击）
            var entryFullPath = Path.GetFullPath(Path.Combine(extractDir, entry.Name));
            if (!entryFullPath.StartsWith(Path.GetFullPath(extractDir)))
            {
                throw new InvalidOperationException("解压路径不安全，可能包含路径遍历攻击。");
            }

            if (entry.EntryType == TarEntryType.Directory)
            {
                // 如果是目录，创建目录
                Directory.CreateDirectory(entryFullPath);
            }
            else
            {
                // 如果是文件，创建文件并写入内容
                Directory.CreateDirectory(Path.GetDirectoryName(entryFullPath) ?? string.Empty);
                using var entryStream = File.Create(entryFullPath);
                entry.DataStream?.CopyTo(entryStream);
            }
        }
    }
}