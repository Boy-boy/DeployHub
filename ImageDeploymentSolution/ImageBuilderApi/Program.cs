using ImageBuilderApi.Infrastructure;
using Minio;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<MinIOService>();

var minioConfig = builder.Configuration.GetSection("MinIO");
builder.Services.AddSingleton(_ =>
    new MinioClient()
        .WithEndpoint(minioConfig["Endpoint"])
        .WithCredentials(minioConfig["AccessKey"], minioConfig["SecretKey"])
        .WithSSL(bool.Parse(minioConfig["UseSSL"] ?? "false"))
        .Build());
builder.Host.UseNLog();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthorization();
app.MapControllers();
app.Run();
