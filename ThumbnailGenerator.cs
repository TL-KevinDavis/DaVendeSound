using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace DaVendeSoundThumbnailFunction
{
    public class GenerateThumbnail
    {
        private readonly ILogger<GenerateThumbnail> _logger;
        private const int ThumbnailWidth = 250;
        private const int ThumbnailHeight = 150;

        public GenerateThumbnail(ILogger<GenerateThumbnail> logger)
        {
            _logger = logger;
        }

        [Function(nameof(GenerateThumbnail))]
        public async Task Run(
            [BlobTrigger("davendesiteimages/{name}", Connection = "AzureWebJobsStorage")] 
            Stream imageStream,
            string name)
        {
            try
            {
                // Skip if already a thumbnail
                if (name.Contains("_thumb"))
                {
                    _logger.LogInformation($"Skipping thumbnail file: {name}");
                    return;
                }

                // Skip non-image files
                string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                if (!imageExtensions.Any(ext => name.ToLower().EndsWith(ext)))
                {
                    _logger.LogInformation($"Skipping non-image file: {name}");
                    return;
                }

                _logger.LogInformation($"Processing image: {name}");

                // Load image from stream
                using (Image image = await Image.LoadAsync(imageStream))
                {
                    // Resize to thumbnail dimensions (250x150)
                    image.Mutate(x => x
                        .Resize(new ResizeOptions
                        {
                            Size = new Size(ThumbnailWidth, ThumbnailHeight),
                            Mode = ResizeMode.Crop
                        }));

                    // Prepare thumbnail filename
                    string fileExtension = Path.GetExtension(name);
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(name);
                    string thumbnailName = $"{fileNameWithoutExtension}{fileExtension}";

                    // Get connection string
                    string? connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                    
                    // Create blob client for thumbnails container using connection string
                    var blobServiceClient = new BlobServiceClient(connectionString);
                    var containerClient = blobServiceClient.GetBlobContainerClient("davendesiteimages-thumbnails");
   
                    // Ensure container exists
                    await containerClient.CreateIfNotExistsAsync();

                    // Save thumbnail to blob storage
                    using (MemoryStream thumbnailStream = new MemoryStream())
                    {
                        // Save as JPEG for optimal compression
                        await image.SaveAsJpegAsync(thumbnailStream);
                        thumbnailStream.Position = 0;

                        var thumbnailBlobClient = containerClient.GetBlobClient(thumbnailName);
                        await thumbnailBlobClient.UploadAsync(thumbnailStream, overwrite: true);

                        _logger.LogInformation($"Thumbnail created successfully: {thumbnailName}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing image {name}: {ex.Message}");
                throw;
            }
        }
    }
}