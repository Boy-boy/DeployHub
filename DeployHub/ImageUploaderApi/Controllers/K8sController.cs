using ImageUploaderApi.Infrastructure;
using k8s;
using k8s.Autorest;
using k8s.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using YamlDotNet.RepresentationModel;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class K8sController : ControllerBase
{
    private readonly IYamlSerializer _serializer;
    private readonly Dictionary<string, ResourceOperationHandlers> _resourceHandlers;
    private readonly IKubernetes _kubernetesClient;

    public K8sController(IYamlSerializer serializer)
    {
        _serializer = serializer;
        var config = KubernetesClientConfiguration.BuildDefaultConfig();
        _kubernetesClient = new Kubernetes(config);
        _resourceHandlers = new Dictionary<string, ResourceOperationHandlers>
            {
                { "Namespace", new ResourceOperationHandlers
                    {
                        CreateOrUpdateHandler = async (y, n, ns) => await HandleResourceOperation<V1Namespace>(
                            y, n, ns,
                            (patch, name, _) => _kubernetesClient.CoreV1.PatchNamespaceAsync(patch, name),
                            (resource, _) => _kubernetesClient.CoreV1.CreateNamespaceAsync(resource)),
                        DeleteHandler = async (kind,n, ns) => await HandleDeleteOperation<V1Status>(
                            kind,n, ns,
                            (name, _) => _kubernetesClient.CoreV1.DeleteNamespaceAsync(name))
                    }
                },
                { "Pod", new ResourceOperationHandlers
                    {
                        CreateOrUpdateHandler = async (y, n, ns) => await HandleResourceOperation<V1Pod>(
                            y, n, ns,
                            (patch, name, nsParam) => _kubernetesClient.CoreV1.PatchNamespacedPodAsync(patch, name, nsParam),
                            (resource, nsParam) => _kubernetesClient.CoreV1.CreateNamespacedPodAsync(resource, nsParam)),
                        DeleteHandler = async (kind,n, ns) => await HandleDeleteOperation<V1Pod>(
                            kind,n, ns,
                            (name, nsParam) => _kubernetesClient.CoreV1.DeleteNamespacedPodAsync(name, nsParam))
                    }
                },
                { "Deployment", new ResourceOperationHandlers
                    {
                        CreateOrUpdateHandler = async (y, n, ns) => await HandleResourceOperation<V1Deployment>(
                            y, n, ns,
                            (patch, name, nsParam) => _kubernetesClient.AppsV1.PatchNamespacedDeploymentAsync(patch, name, nsParam),
                            (resource, nsParam) => _kubernetesClient.AppsV1.CreateNamespacedDeploymentAsync(resource, nsParam)),
                        DeleteHandler = async (kind,n, ns) => await HandleDeleteOperation<V1Status>(
                            kind,n, ns,
                            (name, nsParam) => _kubernetesClient.AppsV1.DeleteNamespacedDeploymentAsync(name, nsParam))
                    }
                },
                { "Service", new ResourceOperationHandlers
                    {
                        CreateOrUpdateHandler = async (y, n, ns) => await HandleResourceOperation<V1Service>(
                            y, n, ns,
                            (patch, name, nsParam) => _kubernetesClient.CoreV1.PatchNamespacedServiceAsync(patch, name, nsParam),
                            (resource, nsParam) => _kubernetesClient.CoreV1.CreateNamespacedServiceAsync(resource, nsParam)),
                        DeleteHandler = async (kind,n, ns) => await HandleDeleteOperation<V1Service>(
                            kind,n, ns,
                            (name, nsParam) => _kubernetesClient.CoreV1.DeleteNamespacedServiceAsync(name, nsParam))
                    }
                },
                { "Ingress", new ResourceOperationHandlers
                    {
                        CreateOrUpdateHandler = async (y, n, ns) => await HandleResourceOperation<V1Ingress>(
                            y, n, ns,
                            (patch, name, nsParam) => _kubernetesClient.NetworkingV1.PatchNamespacedIngressAsync(patch, name, nsParam),
                            (resource, nsParam) => _kubernetesClient.NetworkingV1.CreateNamespacedIngressAsync(resource, nsParam)),
                        DeleteHandler = async (kind,n, ns) => await HandleDeleteOperation<V1Status>(
                            kind,n, ns,
                            (name, nsParam) => _kubernetesClient.NetworkingV1.DeleteNamespacedIngressAsync(name, nsParam))
                    }
                },
                { "ConfigMap", new ResourceOperationHandlers
                    {
                        CreateOrUpdateHandler = async (y, n, ns) => await HandleResourceOperation<V1ConfigMap>(
                            y, n, ns,
                            (patch, name, nsParam) => _kubernetesClient.CoreV1.PatchNamespacedConfigMapAsync(patch, name, nsParam),
                            (resource, nsParam) => _kubernetesClient.CoreV1.CreateNamespacedConfigMapAsync(resource, nsParam)),
                        DeleteHandler = async (kind,n, ns) => await HandleDeleteOperation<V1Status>(
                            kind,n, ns,
                            (name, nsParam) => _kubernetesClient.CoreV1.DeleteNamespacedConfigMapAsync(name, nsParam))
                    }
                },
                { "Secret", new ResourceOperationHandlers
                    {
                        CreateOrUpdateHandler = async (y, n, ns) => await HandleResourceOperation<V1Secret>(
                            y, n, ns,
                            (patch, name, nsParam) => _kubernetesClient.CoreV1.PatchNamespacedSecretAsync(patch, name, nsParam),
                            (resource, nsParam) => _kubernetesClient.CoreV1.CreateNamespacedSecretAsync(resource, nsParam)),
                        DeleteHandler = async (kind,n, ns) => await HandleDeleteOperation<V1Status>(
                            kind,n, ns,
                            (name, nsParam) => _kubernetesClient.CoreV1.DeleteNamespacedSecretAsync(name, nsParam))
                    }
                },
                { "PersistentVolume", new ResourceOperationHandlers
                    {
                        CreateOrUpdateHandler = async (y, n, ns) => await HandleResourceOperation<V1PersistentVolume>(
                            y, n, ns,
                            (patch, name, _) => _kubernetesClient.CoreV1.PatchPersistentVolumeAsync(patch, name),
                            (resource, _) => _kubernetesClient.CoreV1.CreatePersistentVolumeAsync(resource)),
                        DeleteHandler = async (kind,n, ns) => await HandleDeleteOperation<V1PersistentVolume>(
                            kind,n, ns,
                            (name, _) => _kubernetesClient.CoreV1.DeletePersistentVolumeAsync(name))
                    }
                },
                { "PersistentVolumeClaim", new ResourceOperationHandlers
                    {
                        CreateOrUpdateHandler = async (y, n, ns) => await HandleResourceOperation<V1PersistentVolumeClaim>(
                            y, n, ns,
                            (patch, name, nsParam) => _kubernetesClient.CoreV1.PatchNamespacedPersistentVolumeClaimAsync(patch, name, nsParam),
                            (resource, nsParam) => _kubernetesClient.CoreV1.CreateNamespacedPersistentVolumeClaimAsync(resource, nsParam)),
                        DeleteHandler = async (kind,n, ns) => await HandleDeleteOperation<V1PersistentVolumeClaim>(
                            kind,n, ns,
                            (name, nsParam) => _kubernetesClient.CoreV1.DeleteNamespacedPersistentVolumeClaimAsync(name, nsParam))
                    }
                },
                { "DaemonSet", new ResourceOperationHandlers
                    {
                        CreateOrUpdateHandler = async (y, n, ns) => await HandleResourceOperation<V1DaemonSet>(
                            y, n, ns,
                            (patch, name, nsParam) => _kubernetesClient.AppsV1.PatchNamespacedDaemonSetAsync(patch, name, nsParam),
                            (resource, nsParam) => _kubernetesClient.AppsV1.CreateNamespacedDaemonSetAsync(resource, nsParam)),
                        DeleteHandler = async (kind,n, ns) => await HandleDeleteOperation<V1Status>(
                            kind,n, ns,
                            (name, nsParam) => _kubernetesClient.AppsV1.DeleteNamespacedDaemonSetAsync(name, nsParam))
                    }
                },
                { "StatefulSet", new ResourceOperationHandlers
                    {
                        CreateOrUpdateHandler = async (y, n, ns) => await HandleResourceOperation<V1StatefulSet>(
                            y, n, ns,
                            (patch, name, nsParam) => _kubernetesClient.AppsV1.PatchNamespacedStatefulSetAsync(patch, name, nsParam),
                            (resource, nsParam) => _kubernetesClient.AppsV1.CreateNamespacedStatefulSetAsync(resource, nsParam)),
                        DeleteHandler = async (kind,n, ns) => await HandleDeleteOperation<V1Status>(
                            kind,n, ns,
                            (name, nsParam) => _kubernetesClient.AppsV1.DeleteNamespacedStatefulSetAsync(name, nsParam))
                    }
                },
                { "Job", new ResourceOperationHandlers
                    {
                        CreateOrUpdateHandler = async (y, n, ns) => await HandleResourceOperation<V1Job>(
                            y, n, ns,
                            (patch, name, nsParam) => _kubernetesClient.BatchV1.PatchNamespacedJobAsync(patch, name, nsParam),
                            (resource, nsParam) => _kubernetesClient.BatchV1.CreateNamespacedJobAsync(resource, nsParam)),
                        DeleteHandler = async (kind,n, ns) => await HandleDeleteOperation<V1Status>(
                            kind,n, ns,
                            (name, nsParam) => _kubernetesClient.BatchV1.DeleteNamespacedJobAsync(name, nsParam))
                    }
                },
                { "CronJob", new ResourceOperationHandlers
                    {
                        CreateOrUpdateHandler = async (y, n, ns) => await HandleResourceOperation<V1CronJob>(
                            y, n, ns,
                            (patch, name, nsParam) => _kubernetesClient.BatchV1.PatchNamespacedCronJobAsync(patch, name, nsParam),
                            (resource, nsParam) => _kubernetesClient.BatchV1.CreateNamespacedCronJobAsync(resource, nsParam)),
                        DeleteHandler = async (kind,n, ns) => await HandleDeleteOperation<V1Status>(
                            kind,n, ns,
                            (name, nsParam) => _kubernetesClient.BatchV1.DeleteNamespacedCronJobAsync(name, nsParam))
                    }
                },
                { "Role", new ResourceOperationHandlers
                    {
                        CreateOrUpdateHandler = async (y, n, ns) => await HandleResourceOperation<V1Role>(
                            y, n, ns,
                            (patch, name, nsParam) => _kubernetesClient.RbacAuthorizationV1.PatchNamespacedRoleAsync(patch, name, nsParam),
                            (resource, nsParam) => _kubernetesClient.RbacAuthorizationV1.CreateNamespacedRoleAsync(resource, nsParam)),
                        DeleteHandler = async (kind,n, ns) => await HandleDeleteOperation<V1Status>(
                            kind,n, ns,
                            (name, nsParam) => _kubernetesClient.RbacAuthorizationV1.DeleteNamespacedRoleAsync(name, nsParam))
                    }
                },
                { "RoleBinding", new ResourceOperationHandlers
                    {
                        CreateOrUpdateHandler = async (y, n, ns) => await HandleResourceOperation<V1RoleBinding>(
                            y, n, ns,
                            (patch, name, nsParam) => _kubernetesClient.RbacAuthorizationV1.PatchNamespacedRoleBindingAsync(patch, name, nsParam),
                            (resource, nsParam) => _kubernetesClient.RbacAuthorizationV1.CreateNamespacedRoleBindingAsync(resource, nsParam)),
                        DeleteHandler = async (kind,n, ns) => await HandleDeleteOperation<V1Status>(
                            kind,n, ns,
                            (name, nsParam) => _kubernetesClient.RbacAuthorizationV1.DeleteNamespacedRoleBindingAsync(name, nsParam))
                    }
                },
                { "ClusterRole", new ResourceOperationHandlers
                    {
                        CreateOrUpdateHandler = async (y, n, ns) => await HandleResourceOperation<V1ClusterRole>(
                            y, n, ns,
                            (patch, name, _) => _kubernetesClient.RbacAuthorizationV1.PatchClusterRoleAsync(patch, name),
                            (resource, _) => _kubernetesClient.RbacAuthorizationV1.CreateClusterRoleAsync(resource)),
                        DeleteHandler = async (kind,n, ns) => await HandleDeleteOperation<V1Status>(
                            kind,n, ns,
                            (name, _) => _kubernetesClient.RbacAuthorizationV1.DeleteClusterRoleAsync(name))
                    }
                },
                { "ClusterRoleBinding", new ResourceOperationHandlers
                    {
                        CreateOrUpdateHandler = async (y, n, ns) => await HandleResourceOperation<V1ClusterRoleBinding>(
                            y, n, ns,
                            (patch, name, _) => _kubernetesClient.RbacAuthorizationV1.PatchClusterRoleBindingAsync(patch, name),
                            (resource, _) => _kubernetesClient.RbacAuthorizationV1.CreateClusterRoleBindingAsync(resource)),
                        DeleteHandler = async (kind,n, ns) => await HandleDeleteOperation<V1Status>(
                            kind,n, ns,
                            (name, _) => _kubernetesClient.RbacAuthorizationV1.DeleteClusterRoleBindingAsync(name))
                    }
                },
                { "StorageClass", new ResourceOperationHandlers
                    {
                        CreateOrUpdateHandler = async (y, n, ns) => await HandleResourceOperation<V1StorageClass>(
                            y, n, ns,
                            (patch, name, _) => _kubernetesClient.StorageV1.PatchStorageClassAsync(patch, name),
                            (resource, _) => _kubernetesClient.StorageV1.CreateStorageClassAsync(resource)),
                        DeleteHandler = async (kind,n, ns) => await HandleDeleteOperation<V1StorageClass>(
                            kind,n, ns,
                            (name, _) => _kubernetesClient.StorageV1.DeleteStorageClassAsync(name))
                    }
                },
                { "IngressClass", new ResourceOperationHandlers
                    {
                        CreateOrUpdateHandler = async (y, n, ns) => await HandleResourceOperation<V1IngressClass>(
                            y, n, ns,
                            (patch, name, _) => _kubernetesClient.NetworkingV1.PatchIngressClassAsync(patch, name),
                            (resource, _) => _kubernetesClient.NetworkingV1.CreateIngressClassAsync(resource)),
                        DeleteHandler = async (kind,n, ns) => await HandleDeleteOperation<V1Status>(
                            kind,n, ns,
                            (name, _) => _kubernetesClient.NetworkingV1.DeleteIngressClassAsync(name))
                    }
                }
            };
    }

    /// <summary>
    /// 统一资源操作方法 (创建/更新/删除)
    /// </summary>
    [HttpPost]
    [Route("Deployment")]
    public async Task<IActionResult> Deployment([FromBody] KubernetesResourceOperationRequestDto request)
    {
        try
        {
            var yamlStream = new YamlStream();
            using (var reader = new StringReader(request.Yaml))
            {
                yamlStream.Load(reader);
            }

            var results = new List<object>();

            foreach (var document in yamlStream.Documents)
            {
                var yamlNode = (YamlMappingNode)document.RootNode;
                var kind = yamlNode.Children[new YamlScalarNode("kind")].ToString();
                var metadata = (YamlMappingNode)yamlNode.Children[new YamlScalarNode("metadata")];
                var @namespace = metadata.Children.ContainsKey(new YamlScalarNode("namespace"))
                    ? metadata.Children[new YamlScalarNode("namespace")].ToString()
                    : "default";
                var name = metadata.Children[new YamlScalarNode("name")].ToString();
                var yaml = _serializer.Serialize(document.RootNode);

                // 根据操作类型执行不同操作
                var result = request.Operation switch
                {
                    KubernetesOperation.CreateOrUpdate => await CreateOrUpdateResource(kind, @namespace, name, yaml),
                    KubernetesOperation.Delete => await DeleteResource(kind, @namespace, name),
                    _ => await CreateOrUpdateResource(kind, @namespace, name, yaml)
                };

                results.Add(result);
            }

            return Ok(results);
        }
        catch (HttpOperationException ex)
        {
            var errorResponse = JsonSerializer.Deserialize<JsonNode>(ex.Response.Content);
            var message = errorResponse?["message"]?.ToString();
            var reason = errorResponse?["reason"]?.ToString();
            var details = errorResponse?["details"]?.ToString();

            return StatusCode((int)ex.Response.StatusCode, new
            {
                Message = $"{message}",
                Reason = reason,
                Details = details
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Message = $"An error occurred: {ex.Message}"
            });
        }
    }

    #region 私有方法

    private async Task<object> CreateOrUpdateResource(
        string kind,
        string @namespace,
        string resourceName,
        string yaml)
    {
        if (_resourceHandlers.TryGetValue(kind, out var handlers) && handlers.CreateOrUpdateHandler != null)
        {
            return await handlers.CreateOrUpdateHandler(yaml, resourceName, @namespace);
        }
        throw new NotSupportedException($"Resource kind '{kind}' is not supported for create/update.");
    }

    private async Task<object> DeleteResource(
        string kind,
        string @namespace,
        string resourceName)
    {
        if (_resourceHandlers.TryGetValue(kind, out var handlers) && handlers.DeleteHandler != null)
        {
            return await handlers.DeleteHandler(kind, resourceName, @namespace);
        }
        throw new NotSupportedException($"Resource kind '{kind}' is not supported for deletion.");
    }

    private async Task<object> HandleResourceOperation<T>(
        string yaml,
        string resourceName,
        string @namespace,
        Func<V1Patch, string, string, Task<T>> patchFunc,
        Func<T, string, Task<T>> createFunc)
        where T : IKubernetesObject<V1ObjectMeta>
    {
        var resource = KubernetesYaml.Deserialize<T>(yaml);

        try
        {
            if (RequiresNamespace(typeof(T)) && !string.IsNullOrEmpty(@namespace))
            {
                await TryCreateNamespaceAsync(@namespace);
            }

            var patchResult = await patchFunc(
                new V1Patch(resource, V1Patch.PatchType.MergePatch),
                resourceName,
                @namespace
            );
            return CreateSuccessResult(resource.Kind, patchResult.Metadata.Name, patchResult.Metadata.NamespaceProperty, "updated");
        }
        catch (HttpOperationException ex) when (ex.Response.StatusCode == HttpStatusCode.NotFound)
        {
            var createResult = await createFunc(resource, @namespace);
            return CreateSuccessResult(resource.Kind, createResult.Metadata.Name, createResult.Metadata.NamespaceProperty, "created");
        }
        catch (Exception ex)
        {
            return CreateErrorResult(resource.Kind, ex.Message);
        }
    }

    private async Task<object> HandleDeleteOperation<T>(
        string kind,
        string resourceName,
        string @namespace,
        Func<string, string, Task<T>> deleteFunc)
    // where T : IKubernetesObject<V1ObjectMeta>
    {
        try
        {
            await deleteFunc(resourceName, @namespace);
            return CreateSuccessResult(kind, resourceName, @namespace, "deleted");
        }
        catch (HttpOperationException ex) when (ex.Response.StatusCode == HttpStatusCode.NotFound)
        {
            return CreateSuccessResult(kind, resourceName, @namespace, "not found (no deletion performed)");
        }
        catch (Exception ex)
        {
            return CreateErrorResult(kind, ex.Message);
        }
    }

    private object CreateSuccessResult(string kind, string name, string @namespace, string action)
    {
        return new
        {
            Kind = kind,
            Name = name,
            Namespace = @namespace,
            Message = $"{kind} {action} successfully."
        };
    }

    private object CreateErrorResult(string kind, string errorMessage)
    {
        return new
        {
            Kind = kind,
            Message = $"Failed to operate on {kind}: {errorMessage}"
        };
    }

    private bool RequiresNamespace(Type resourceType)
    {
        //判断资源类型是否需要 Namespace
        var clusterScopedTypes = new[]
        {
                typeof(V1PersistentVolume),          // 持久卷
                typeof(V1ClusterRole),               // 集群角色
                typeof(V1ClusterRoleBinding),        // 集群角色绑定
                typeof(V1Namespace),                 // 命名空间本身
                typeof(V1Node),                      // 节点
                typeof(V1StorageClass),              // 存储类
                typeof(V1IngressClass),              // Ingress 类
                typeof(V1CustomResourceDefinition),  // 自定义资源定义
                typeof(V1APIService),                // API 服务
                typeof(V1PriorityClass)              // 优先级类
            };
        return !clusterScopedTypes.Contains(resourceType);
    }

    private async Task TryCreateNamespaceAsync(string namespaceName)
    {
        try
        {
            await _kubernetesClient.CoreV1.CreateNamespaceAsync(new V1Namespace
            {
                Metadata = new V1ObjectMeta
                {
                    Name = namespaceName,
                    Labels = new Dictionary<string, string>
                        {
                            { "auto-created", "true" },
                            { "created-by", "CreateOrUpdateResource" }
                        }
                }
            });
        }
        catch (HttpOperationException ex) when (ex.Response.StatusCode == HttpStatusCode.Conflict)
        {
            // Namespace 已存在是正常情况，无需处理
        }
        catch (Exception ex)
        {
            // 记录日志但继续执行
            throw new Exception($"Failed to create namespace {namespaceName}");
        }
    }

    #endregion
}

// 资源操作处理器容器类
public class ResourceOperationHandlers
{
    public Func<string, string, string, Task<object>> CreateOrUpdateHandler { get; set; }
    public Func<string, string, string, Task<object>> DeleteHandler { get; set; }
}

public enum KubernetesOperation
{
    CreateOrUpdate,
    Delete
}

public class KubernetesResourceOperationRequestDto
{
    public string Yaml { get; set; }
    public KubernetesOperation Operation { get; set; } = KubernetesOperation.CreateOrUpdate;
}
