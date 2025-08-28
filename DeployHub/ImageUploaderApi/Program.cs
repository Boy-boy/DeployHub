using ImageUploaderApi.AppServices;
using ImageUploaderApi.Domain.Repositories;
using ImageUploaderApi.Infrastructure;
using ImageUploaderApi.Infrastructure.Authorizations;
using ImageUploaderApi.Models;
using ImageUploaderApi.Persistence;
using ImageUploaderApi.Persistence.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.OpenApi.Models;
using Minio;
using NLog.Web;
using Winton.Extensions.Configuration.Consul;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient();
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 5L * 1024 * 1024 * 1024; // 5G
});
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 5L * 1024 * 1024 * 1024; // 5G
});
builder.Host.UseNLog();


#region Consul
var enableConsul = builder.Configuration.GetValue<bool>("EnableConsul");
if (enableConsul)
{
    var consulOption = new ConsulOptions();
    builder.Configuration.GetSection("Consul").Bind(consulOption);
    builder.Configuration.AddConsul(
        "config/cdservice/appsettings.json", // 在Consul中存储的key
        options =>
        {
            options.ConsulConfigurationOptions = cco =>
            {
                cco.Address = new Uri(consulOption.Address);
                cco.Token = consulOption.Token;
            };
            options.Optional = consulOption.Optional;
            options.ReloadOnChange = consulOption.ReloadOnChange;
            options.OnLoadException = exceptionContext =>
            {
                exceptionContext.Ignore = consulOption.IgnoreException;
                Console.WriteLine($"加载Consul配置异常: {exceptionContext.Exception}");
            };
        }
    );
}
#endregion


builder.Services.AddScoped<MinIOService>();
var minioConfig = builder.Configuration.GetSection("MinIO");
builder.Services.AddSingleton(_ =>
    new MinioClient()
        .WithEndpoint(minioConfig["Endpoint"])
        .WithCredentials(minioConfig["AccessKey"], minioConfig["SecretKey"])
        .WithSSL(bool.Parse(minioConfig["UseSSL"] ?? "false"))
        .Build());

//builder.Services.AddDbContext<AppDbContext>(options =>
//    options.UseInMemoryDatabase("appDbContext"));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DbConnection")));

builder.Services.AddScoped<IProjectYamlRepository, ProjectYamlRepository>();
builder.Services.AddScoped<ProjectYamlAppService, ProjectYamlAppService>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<ProjectAppService, ProjectAppService>();

builder.Services.AddScoped<IYamlSerializer, YamlSerializer>();

#region 认证/授权
var authority = builder.Configuration.GetValue<string>("Jwt:Authority");
var audience = builder.Configuration.GetValue<string>("Jwt:Audience");
var requireHttpsMetadata = builder.Configuration.GetValue<bool>("Jwt:RequireHttpsMetadata");
JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = authority; // IdentityServer 的地址
        options.Audience = audience;    // 你的 API 资源名称
        options.RequireHttpsMetadata = requireHttpsMetadata; // 是否要求 HTTPS

        // 配置 JWT 验证选项
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,          // 验证 Issuer
            ValidIssuer = authority,        // 有效的 Issuer
            ValidateAudience = true,        // 验证 Audience
            ValidAudience = audience,        // 有效的 Audience
            ValidateLifetime = true,        // 验证 Token 有效期
            ClockSkew = TimeSpan.Zero,      // 时钟偏差（设为 0 表示严格验证）
            ValidateIssuerSigningKey = true // 验证签名密钥
        };
    });
builder.Services.AddAuthorization(options =>
{
    // 完全替换默认策略
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .AddRequirements(new UserWhitelistAuthorizationRequirement(builder.Configuration))
        .Build();
});
#endregion

#region swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CD API", Version = "v1" });

    // 添加JWT认证定义
    c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Example: \"Bearer {token}\"",
        Name = "Authorization",
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            Implicit = new OpenApiOAuthFlow
            {
                Scopes = new Dictionary<string, string> { { audience, "CD API" } },
                AuthorizationUrl = new Uri(authority + "/connect/authorize")
            }
        },
    });

    // 全局安全要求
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = JwtBearerDefaults.AuthenticationScheme
                }
            },
            new List<string>()
        }
    });
});
#endregion

var app = builder.Build();

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseSwagger(c =>
{
    c.RouteTemplate = "doc/{documentName}/swagger.json";
});
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/doc/v1/swagger.json", "CD API V1");
    c.RoutePrefix = "doc";
    c.OAuthClientId("web_api_swagger");
});
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
