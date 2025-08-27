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
            // ���ӵ� Docker �ػ����̣�ͨ�� Unix �׽��֣�
            var client = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock"))
                .CreateClient();

            // ׼����������
            var buildParameters = new ImageBuildParameters
            {
                Tags = new List<string> { request.ImageTag }, // �����ǩ
                Dockerfile = "Dockerfile", // Dockerfile �ļ���
                Remove = true // �����ɹ���ɾ���м��
            };

            // �� MinIO �����ļ�
            var fileStream = await _minIoService.DownloadFileAsync("docker-image-upload-bucket", request.FileName);

            // ��ʼ��������
            await client.Images.BuildImageFromDockerfileAsync(
                buildParameters,
                fileStream,
                new List<AuthConfig>(), // ��ѡ����֤��Ϣ
                new Dictionary<string, string>(), // ��ѡ����������
                new Progress<JSONMessage>(),
                CancellationToken.None);

            // ��ȡ�ڵ� IP
            var nodeIp = Environment.GetEnvironmentVariable("NODE_IP");

            return Ok(new
            {
                Message = $"Image [{request.ImageTag}] build successful.",
                NodeIp = nodeIp
            });
        }
        catch (DockerApiException ex)
        {
            // ��ȡ�ڵ� IP
            var nodeIp = Environment.GetEnvironmentVariable("NODE_IP");

            return BadRequest(new
            {
                Message = $"Failed to build image [{request.ImageTag}]: {ex.Message}",
                NodeIp = nodeIp
            });
        }
        catch (Exception ex)
        {
            // ��ȡ�ڵ� IP
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
            // ���ӵ� Docker �ػ�����
            var client = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock"))
                .CreateClient();

            // ɾ������
            await client.Images.DeleteImageAsync(imageName, new ImageDeleteParameters { Force = true });

            // ��ȡ�ڵ� IP
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
        // ��ȡ�ڵ� IP
        var nodeIp = Environment.GetEnvironmentVariable("NODE_IP");

        try
        {
            // ���ӵ� Docker �ػ�����
            var client = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock"))
                .CreateClient();

            // ��ȡ�����б�
            var images = await client.Images.ListImagesAsync(new ImagesListParameters { All = true });

            // ���ؾ����б�
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
                Images = (object)null!, // �����б�Ϊ��
                NodeIp = nodeIp
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Success = false,
                Message = $"An error occurred while listing images: {ex.Message}",
                Images = (object)null!, // �����б�Ϊ��
                NodeIp = nodeIp
            });
        }
    }

    [HttpGet("inspect")]
    public async Task<IActionResult> InspectDockerImage([FromQuery] string imageName)
    {
        // ��ȡ�ڵ� IP
        var nodeIp = Environment.GetEnvironmentVariable("NODE_IP");

        try
        {
            // ���ӵ� Docker �ػ�����
            var client = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock"))
                .CreateClient();

            // ��ѯ��������
            var image = await client.Images.InspectImageAsync(imageName);

            // ���ؾ�������
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
                Image = (object)null!, // ��������Ϊ��
                NodeIp = nodeIp
            });
        }
        catch (DockerApiException ex)
        {
            return BadRequest(new
            {
                Success = false,
                Message = $"Failed to inspect image [{imageName}]: {ex.Message}",
                Image = (object)null!, // ��������Ϊ��
                NodeIp = nodeIp
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Success = false,
                Message = $"An error occurred while inspecting image [{imageName}]: {ex.Message}",
                Image = (object)null!, // ��������Ϊ��
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

// ������������ DTO
public class BuildDockerImageRequestDto
{
    public string ImageTag { get; set; } // �����ǩ���� my-image:latest��
    public string FileName { get; set; }
}

// ���������ģ��� Dockerfile �͹��������Ĵ��Ϊ tar.gz��
public class TarGzImageBuildContext : IDisposable
{
    private readonly string _tarGzFilePath;

    public TarGzImageBuildContext(string dockerFilePath)
    {
        _tarGzFilePath = Path.GetTempFileName(); // ������ʱ�ļ�
        CreateTarGz(dockerFilePath, _tarGzFilePath); // ���Ϊ tar.gz
    }

    public Stream GetStream()
    {
        return new FileStream(_tarGzFilePath, FileMode.Open, FileAccess.Read);
    }

    public void Dispose()
    {
        if (File.Exists(_tarGzFilePath))
        {
            File.Delete(_tarGzFilePath); // ɾ����ʱ�ļ�
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
        // ��ӵ�ǰĿ¼�µ������ļ�
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileInfo = new FileInfo(file);

            // ����ļ��Ƿ�Ϊѹ���ļ�
            if (IsCompressedFile(fileInfo))
            {
                // ��ѹѹ���ļ�
                var extractDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(extractDir);
                ExtractCompressedFile(fileInfo.FullName, extractDir);

                // ����ѹ���������ӵ� tar ��
                AddDirectoryToTar(tarWriter, extractDir, relativePath);

                // ������ʱ��ѹĿ¼
                Directory.Delete(extractDir, recursive: true);
            }
            else
            {
                // ��ͨ�ļ�ֱ����ӵ� tar ��
                var entryName = Path.Combine(relativePath, fileInfo.Name);
                tarWriter.WriteEntry(fileInfo.FullName, entryName);
            }
        }

        // �ݹ������Ŀ¼
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
                throw new NotSupportedException($"��֧�ֵ�ѹ���ļ���ʽ: {fileInfo.Extension}");
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
            // ȷ��Ŀ��·���ڽ�ѹĿ¼�ڣ���ֹ·������������
            var entryFullPath = Path.GetFullPath(Path.Combine(extractDir, entry.Name));
            if (!entryFullPath.StartsWith(Path.GetFullPath(extractDir)))
            {
                throw new InvalidOperationException("��ѹ·������ȫ�����ܰ���·������������");
            }

            if (entry.EntryType == TarEntryType.Directory)
            {
                // �����Ŀ¼������Ŀ¼
                Directory.CreateDirectory(entryFullPath);
            }
            else
            {
                // ������ļ��������ļ���д������
                Directory.CreateDirectory(Path.GetDirectoryName(entryFullPath) ?? string.Empty);
                using var entryStream = File.Create(entryFullPath);
                entry.DataStream?.CopyTo(entryStream);
            }
        }
    }
}