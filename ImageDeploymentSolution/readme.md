# Kubernetes 自动化部署平台

## 📋 概述

本项目提供了一套完整的 Kubernetes 自动化部署解决方案，包含 MinIO 对象存储、镜像上传服务和镜像构建服务。

## 🚀 部署说明

### 基础组件

- **Namespace**: `automated-deployment`
- **权限管理**: ClusterRole 和 ClusterRoleBinding
- **存储**: MinIO 对象存储服务
- **服务**: 镜像上传和构建服务

## 🔐 认证授权配置说明

### 企业级证书配置

项目中包含 `enterprise-root-ca-cert-secret` Secret，用于处理企业内部 SSL 证书认证。

#### 何时需要此配置：

- ✅ **使用认证授权**: 当 `imageuploaderapi` 服务需要与认证服务器（如 Identity Server）通信时
- ✅ **内部域名**: 使用企业内部颁发的 SSL 证书域名时
- ✅ **HTTPS 通信**: 需要建立安全的 HTTPS 连接时

#### 何时可以删除此配置：

- ❌ **无认证需求**: 服务间通信不需要认证授权
- ❌ **无内部证书**: 不使用企业内部颁发的 SSL 证书
- ❌ **HTTP 通信**: 使用 HTTP 或公共证书的场景

### 配置调整指南

#### 如果**需要认证授权**（默认配置）：

```yaml
# 保留以下配置
apiVersion: v1
kind: Secret
metadata:
  name: enterprise-root-ca-cert-secret
  namespace: automated-deployment
type: Opaque
data:
  ca.crt: #企业内部根证书
```

`imageuploaderapi` Deployment 中的证书挂载保持不变：

```yaml
  volumeMounts:
  - name: root-ca-cert
    mountPath: /etc/ssl/certs/ca.crt
    subPath: ca.crt
  - name: root-ca-cert
    mountPath: /usr/local/share/ca-certificates/ca.crt
    subPath: ca.crt
volumes:
- name: root-ca-cert
  secret:
    secretName: enterprise-root-ca-cert-secret
```

#### 如果**不需要认证授权**：

1. **删除 Secret 配置**：

```yaml

# 删除整个 enterprise-root-ca-cert-secret 配置块
# apiVersion: v1
# kind: Secret
# metadata:
#   name: enterprise-root-ca-cert-secret
#   namespace: automated-deployment
# ...

```

2. **修改 imageuploaderapi Deployment**：

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: imageuploaderapi
  namespace: automated-deployment
spec:
  # ... 其他配置保持不变
  template:
    spec:
      containers:
      - name: imageuploaderapi
        # 删除 lifecycle 中的证书更新命令
        # lifecycle:
        #   postStart:
        #     exec:
        #       command: ["sh", "-c", "update-ca-certificates"]
        # 删除证书挂载配置
        # volumeMounts:
        # - name: root-ca-cert
        #   mountPath: /etc/ssl/certs/ca.crt
        #   subPath: ca.crt
        # - name: root-ca-cert
        #   mountPath: /usr/local/share/ca-certificates/ca.crt
        #   subPath: ca.crt
      # 删除 volumes 中的证书配置
      # volumes:
      # - name: root-ca-cert
      #   secret:
      #     secretName: enterprise-root-ca-cert-secret
```

## 📁 目录结构

```
├── Namespace 配置
├── RBAC 权限配置
├── MinIO 存储服务
│   ├── PersistentVolumeClaim
│   ├── Deployment
│   └── Service
├── 证书 Secret（可选）
├── ImageUploaderAPI 服务
│   ├── Deployment
│   ├── Service
│   └── Ingress
└── ImageBuilderAPI 服务
    ├── DaemonSet
    ├── Service
```

## ⚙️ 服务说明

### MinIO 对象存储

- **用途**: 存储构建过程中的镜像文件和临时数据
- **存储**: 200Gi 持久化存储
- **访问**: 
  - API 端口: 9000
  - 控制台端口: 9090

### ImageUploaderAPI

- **用途**: 处理镜像文件上传请求
- **端口**: 5000
- **访问域名**: cd.zsfund.com

### ImageBuilderAPI

- **用途**: 构建 Docker 镜像
- **部署方式**: DaemonSet（每个节点一个实例）
- **特殊权限**: 需要访问 Docker Socket

## 🛡️ 安全注意事项

1. **默认密码**: MinIO 使用默认的 `minioadmin/minioadmin`，生产环境请修改
2. **权限控制**: ClusterRole 提供了广泛的 Kubernetes 操作权限
3. **网络安全**: 建议配置网络策略限制服务间访问
4. **证书管理**: 定期更新 SSL 证书

## 🚨 故障排除

### 证书相关问题

如果遇到 SSL 证书验证失败：

1. 确认是否需要企业证书配置
2. 检查 `enterprise-root-ca-cert-secret` 是否正确部署
3. 验证证书内容是否有效

### 部署问题

```sh

# 检查部署状态

kubectl get pods -n automated-deployment

# 查看服务日志

kubectl logs -n automated-deployment <pod-name>

# 验证权限配置

kubectl auth can-i create deployments --namespace automated-deployment
```

## 📝 版本信息

- **MinIO**: RELEASE.2025-02-07T23-21-09Z
- **Kubernetes API**: v1, apps/v1, rbac.authorization.k8s.io/v1

---

**注意**: 部署前请根据实际环境需求调整资源配置和安全设置。
