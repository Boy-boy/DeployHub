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
        "config/cdservice/appsettings.json", // ��Consul�д洢��key
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
                Console.WriteLine($"����Consul�����쳣: {exceptionContext.Exception}");
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

#region ��֤/��Ȩ
var authority = builder.Configuration.GetValue<string>("Jwt:Authority");
var audience = builder.Configuration.GetValue<string>("Jwt:Audience");
var requireHttpsMetadata = builder.Configuration.GetValue<bool>("Jwt:RequireHttpsMetadata");
JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = authority; // IdentityServer �ĵ�ַ
        options.Audience = audience;    // ��� API ��Դ����
        options.RequireHttpsMetadata = requireHttpsMetadata; // �Ƿ�Ҫ�� HTTPS

        // ���� JWT ��֤ѡ��
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,          // ��֤ Issuer
            ValidIssuer = authority,        // ��Ч�� Issuer
            ValidateAudience = true,        // ��֤ Audience
            ValidAudience = audience,        // ��Ч�� Audience
            ValidateLifetime = true,        // ��֤ Token ��Ч��
            ClockSkew = TimeSpan.Zero,      // ʱ��ƫ���Ϊ 0 ��ʾ�ϸ���֤��
            ValidateIssuerSigningKey = true // ��֤ǩ����Կ
        };
    });
builder.Services.AddAuthorization(options =>
{
    // ��ȫ�滻Ĭ�ϲ���
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

    // ���JWT��֤����
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

    // ȫ�ְ�ȫҪ��
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
