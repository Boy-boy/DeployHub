using Minio;
using Minio.DataModel.Args;

namespace ImageUploaderApi.Infrastructure
{
    public class MinIOService
    {
        private readonly IMinioClient _minioClient;

        public MinIOService(IMinioClient minioClient)
        {
            _minioClient = minioClient;
        }

        public async Task UploadFileAsync(string bucketName, string objectName, Stream fileStream)
        {
            if (string.IsNullOrEmpty(bucketName))
                throw new ArgumentNullException(nameof(bucketName));
            if (string.IsNullOrEmpty(objectName))
                throw new ArgumentNullException(nameof(objectName));
            if (fileStream == null || fileStream.Length == 0)
                throw new ArgumentNullException(nameof(fileStream));

            var beArgs = new BucketExistsArgs().WithBucket(bucketName);
            var found = await _minioClient.BucketExistsAsync(beArgs).ConfigureAwait(false);
            if (!found)
            {
                var mbArgs = new MakeBucketArgs().WithBucket(bucketName);
                await _minioClient.MakeBucketAsync(mbArgs).ConfigureAwait(false);
            }

            var putObjectArgs = new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithStreamData(fileStream)
                .WithObjectSize(fileStream.Length)
                .WithContentType("application/octet-stream");
            await _minioClient.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
        }

        public async Task<Stream> DownloadFileAsync(string bucketName, string objectName)
        {
            var memoryStream = new MemoryStream();
            var getObjectArgs = new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithCallbackStream(stream => stream.CopyTo(memoryStream));
            await _minioClient.GetObjectAsync(getObjectArgs).ConfigureAwait(false);
            memoryStream.Position = 0;
            return memoryStream;
        }
    }
}
