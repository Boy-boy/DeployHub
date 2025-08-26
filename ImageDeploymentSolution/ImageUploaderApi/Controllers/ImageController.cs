using ImageUploaderApi.Infrastructure;
using k8s;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace ImageUploaderApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ImageController : ControllerBase
    {
        private readonly MinIOService _minIoService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ImageController> _logger;

        public ImageController(MinIOService minIoService,
            IHttpClientFactory httpClientFactory,
            ILogger<ImageController> logger)
        {
            _minIoService = minIoService;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadAsync([FromForm] ImageUploadRequestDto request)
        {
            try
            {
                if (request.ImageFile.Length == 0)
                {
                    return BadRequest("image file must be uploaded ");
                }

                // 检查文件扩展名
                var fileExtension = Path.GetExtension(request.ImageFile.FileName).ToLower();
                if (fileExtension != ".tar")
                {
                    return BadRequest("Only .tar files are allowed.");
                }

                await using var stream = request.ImageFile.OpenReadStream();
                stream.Position = 0;
                var fileName = request.ImageFile.FileName;
                var objectName = fileName + "_" + request.ImageTag;
                await _minIoService.UploadFileAsync("docker-image-upload-bucket", objectName, stream);

                // 获取Pod IP 列表
                var podIPs = await GetPodIPs();

                var client = _httpClientFactory.CreateClient();

                var requestData = new
                {
                    ImageTag = request.ImageTag,
                    FileName = objectName
                };

                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(requestData),
                    Encoding.UTF8,
                    "application/json"
                );

                // 构造请求 URI
                var requestUri = "/api/image/build";

                // 并发调用所有 Pod
                var tasks = podIPs.Select(podIp =>
                {
                    // 将 Pod IP 转换为 DNS 格式（例如 10-244-1-2）
                    var podDns = podIp.Replace('.', '-');
                    // 构造 Pod 的完整域名
                    var podUrl = $"http://{podDns}.imagebuilderapi.automated-deployment.svc.cluster.local:5000{requestUri}";
                    return client.PostAsync(podUrl, jsonContent);
                }).ToList();

                // 等待所有任务完成并收集响应
                var responses = await Task.WhenAll(tasks);

                // 处理响应并返回统一的结果
                var results = responses.Select(response => response.Content.ReadFromJsonAsync<object>().GetAwaiter().GetResult()).ToList();

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"image file uploaded failed，message：{ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteDockerImage([FromQuery] string imageName)
        {
            try
            {
                // 获取Pod IP 列表
                var podIPs = await GetPodIPs();

                // 创建 HTTP 客户端
                var client = _httpClientFactory.CreateClient();

                // 构造请求内容
                var requestUri = "/api/image/delete";

                // 并发调用所有 Pod
                var tasks = podIPs.Select(podIp =>
                {
                    // 将 Pod IP 转换为 DNS 格式（例如 10-244-1-2）
                    var podDns = podIp.Replace('.', '-');
                    // 构造 Pod 的完整域名
                    var podUrl = $"http://{podDns}.imagebuilderapi.automated-deployment.svc.cluster.local:5000{requestUri}?imageName={imageName}";
                    return client.DeleteAsync(podUrl);
                }).ToList();
                var responses = await Task.WhenAll(tasks);

                // 收集响应结果
                var results = responses.Select(response => response.Content.ReadFromJsonAsync<object>().GetAwaiter().GetResult()).ToList();

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting image [{imageName}]: {ex.Message}");
                return StatusCode(500, new { Message = $"Error deleting image [{imageName}]: {ex.Message}" });
            }
        }

        [HttpGet("list")]
        public async Task<IActionResult> ListDockerImages()
        {
            try
            {
                // 获取Pod IP 列表
                var podIPs = await GetPodIPs();

                // 创建 HTTP 客户端
                var client = _httpClientFactory.CreateClient();

                // 构造请求 URI
                var requestUri = "/api/image/list";

                // 并发调用所有 Pod
                var tasks = podIPs.Select(async podIp =>
                {
                    try
                    {
                        // 将 Pod IP 转换为 DNS 格式（例如 10-244-1-2）
                        var podDns = podIp.Replace('.', '-');
                        // 构造 Pod 的完整域名
                        var podUrl = $"http://{podDns}.imagebuilderapi.automated-deployment.svc.cluster.local:5000{requestUri}";
                        var response = await client.GetAsync(podUrl);

                        if (response.IsSuccessStatusCode)
                        {
                            var result = await response.Content.ReadFromJsonAsync<ImageListResponse>();
                            return new PodResult { PodIp = podIp, Result = result, Success = result.Success };
                        }
                        _logger.LogWarning($"Failed to get images from pod {podIp}, status: {response.StatusCode}");
                        return new PodResult { PodIp = podIp, Result = (ImageListResponse)null, Success = false };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error getting images from pod {podIp}: {ex.Message}");
                        return new PodResult { PodIp = podIp, Result = (ImageListResponse)null, Success = false };
                    }
                }).ToList();
                var results = await Task.WhenAll(tasks);

                // 合并相同镜像
                var mergedImages = MergeImagesByNode(results.Where(r => r.Success).ToList());

                return Ok(mergedImages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error listing images: {ex.Message}");
                return StatusCode(500, new { Message = $"Error listing images: {ex.Message}" });
            }
        }

        [HttpGet("inspect")]
        public async Task<IActionResult> InspectDockerImage([FromQuery] string imageName)
        {
            try
            {
                // 获取Pod IP 列表
                var podIPs = await GetPodIPs();

                // 创建 HTTP 客户端
                var client = _httpClientFactory.CreateClient();

                // 构造请求 URI
                var requestUri = $"/api/image/inspect?imageName={imageName}";

                // 并发调用所有 Pod
                var tasks = podIPs.Select(podIp =>
                {
                    // 将 Pod IP 转换为 DNS 格式（例如 10-244-1-2）
                    var podDns = podIp.Replace('.', '-');
                    // 构造 Pod 的完整域名
                    var podUrl = $"http://{podDns}.imagebuilderapi.automated-deployment.svc.cluster.local:5000{requestUri}";
                    return client.GetAsync(podUrl);
                }).ToList();
                var responses = await Task.WhenAll(tasks);

                // 收集响应结果
                var results = responses.Select(response => response.Content.ReadFromJsonAsync<object>().GetAwaiter().GetResult()).ToList();

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error inspecting image [{imageName}]: {ex.Message}");
                return StatusCode(500, new { Message = $"Error inspecting image [{imageName}]: {ex.Message}" });
            }
        }

        private async Task<List<string>> GetPodIPs()
        {
            try
            {
                // 加载 Kubernetes 配置
                var config = KubernetesClientConfiguration.BuildDefaultConfig();
                var client = new Kubernetes(config);

                // 获取所有 Pod
                var pods = await client.ListNamespacedPodAsync("automated-deployment", labelSelector: "app=imagebuilderapi");
                if (pods == null || pods.Items == null || !pods.Items.Any())
                {
                    _logger.LogWarning("No pods found in the Kubernetes cluster.");
                    return new List<string>();
                }

                // 提取 Pod 的 IP 地址
                var podIPs = pods.Items
                    .Where(pod => !string.IsNullOrEmpty(pod.Status.PodIP))
                    .Select(pod => pod.Status.PodIP)
                    .ToList();

                // 日志记录
                if (podIPs.Any())
                {
                    _logger.LogInformation($"Kubernetes pod IPs: {string.Join(";", podIPs)}");
                }
                else
                {
                    _logger.LogWarning("No valid pod IPs found.");
                }

                return podIPs!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to retrieve Kubernetes pod IPs: {ex.Message}");
                return new List<string>(); // 返回空列表
            }
        }

        // 合并镜像的辅助方法
        private List<MergedDockerImage> MergeImagesByNode(List<PodResult> podResults)
        {
            var imageDictionary = new Dictionary<string, MergedDockerImage>();

            foreach (var result in podResults)
            {
                var imageResult = result.Result;

                if (imageResult?.Images == null) continue;

                var nodeIp = imageResult.NodeIp;
                foreach (var image in imageResult.Images)
                {
                    if (image.Tags == null || !image.Tags.Any()) continue;

                    foreach (var repoTag in image.Tags)
                    {
                        // 解析镜像名和标签
                        var (imageName, tag) = ParseImageReference(repoTag);
                        var key = $"{imageName}:{tag}";

                        if (!imageDictionary.ContainsKey(key))
                        {
                            imageDictionary[key] = new MergedDockerImage
                            {
                                FullName = key,
                                ImageName = imageName,
                                Tag = tag,
                                NodeCount = 0
                            };
                        }

                        var mergedImage = imageDictionary[key];

                        // 添加节点IP（避免重复）
                        if (!mergedImage.NodeIps.Contains(nodeIp))
                        {
                            mergedImage.NodeIps.Add(nodeIp);
                            mergedImage.NodeCount++;
                        }
                    }
                }
            }

            return imageDictionary.Values
                .OrderByDescending(img => img.NodeCount)
                .ThenBy(img => img.ImageName)
                .ToList();
        }

        // 解析镜像引用的辅助方法
        private (string imageName, string tag) ParseImageReference(string imageRef)
        {
            if (string.IsNullOrEmpty(imageRef))
                return (string.Empty, "latest");

            //为什么要检查是否包含 /？
            //因为冒号: 在 Docker 镜像引用中有两种用途：
            //标签分隔符: nginx: latest(冒号后是标签)
            //端口号分隔符: myregistry.com:5000/myapp(冒号后是端口号)
            // 例子说明:
            //"nginx:latest"           // 冒号后是 "latest" (不包含/) → 标签分隔符 ✓
            //"myregistry.com:5000/myapp"  // 冒号后是 "5000/myapp" (包含/) → 端口号分隔符 ✗
            var tagSeparatorIndex = imageRef.LastIndexOf(':');
            if (tagSeparatorIndex > 0 && !imageRef.Substring(tagSeparatorIndex + 1).Contains('/'))
            {
                var tag = imageRef.Substring(tagSeparatorIndex + 1);
                var imageName = imageRef.Substring(0, tagSeparatorIndex);
                return (imageName, tag);
            }

            return (imageRef, "latest");
        }
    }

    public class ImageUploadRequestDto
    {
        public IFormFile ImageFile { get; set; }

        public string ImageTag { get; set; }
    }

    public class PodResult
    {
        public string PodIp { get; set; }
        public ImageListResponse Result { get; set; }
        public bool Success { get; set; }
    }

    public class ImageListResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<DockerImageInfo> Images { get; set; }
        public string NodeIp { get; set; }
    }

    // 定义镜像信息类
    public class DockerImageInfo
    {
        public string Id { get; set; }
        public List<string> Tags { get; set; }
        public DateTime Created { get; set; }
        public long Size { get; set; }
    }

    // 定义合并后的镜像信息类
    public class MergedDockerImage
    {
        public string FullName { get; set; }
        public string ImageName { get; set; }
        public string Tag { get; set; }
        public List<string> NodeIps { get; set; } = new List<string>();
        public int NodeCount { get; set; }
    }
}
