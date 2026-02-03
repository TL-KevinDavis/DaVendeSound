using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DaVendeSoundFunction;

public class DeleteThumbnail
{
 private readonly ILogger<DeleteThumbnail> _logger;

 public DeleteThumbnail(ILogger<DeleteThumbnail> logger)
 {
 _logger = logger;
 }

 [Function(nameof(DeleteThumbnail))]
 public async Task Run(
 [EventGridTrigger] EventGridEvent eventGridEvent)
 {
 try
 {
_logger.LogInformation("Event received: {EventType}", eventGridEvent.EventType);

 // Only process blob deleted events
 if (eventGridEvent.EventType != "Microsoft.Storage.BlobDeleted")
 {
 _logger.LogInformation("Ignoring event type: {EventType}", eventGridEvent.EventType);
 return;
 }

 // Parse the event data
 var blobDeletedData = eventGridEvent.Data.ToObjectFromJson<StorageBlobDeletedEventData>();
 
if (blobDeletedData == null)
 {
 _logger.LogWarning("Could not parse blob deleted event data");
 return;
 }

// Extract blob name from the URL
 // URL format: https://<account>.blob.core.windows.net/<container>/<blob-name>
 var blobUrl = blobDeletedData.Url;
 _logger.LogInformation("Blob deleted URL: {Url}", blobUrl);

 // Check if it's from the main images container
 if (!blobUrl.Contains("/davendesiteimages/"))
 {
 _logger.LogInformation("Blob not from davendesiteimages container, skipping");
 return;
 }

// Extract blob name
 var uri = new Uri(blobUrl);
 var segments = uri.Segments;
 
 // segments[0] = "/"
 // segments[1] = "davendesiteimages/"
 // segments[2...] = blob name (could be nested)
 if (segments.Length <3)
 {
 _logger.LogWarning("Could not extract blob name from URL: {Url}", blobUrl);
 return;
 }

 // Get blob name (everything after container name)
 var blobName = string.Join("", segments.Skip(2));
 blobName = Uri.UnescapeDataString(blobName);
 
 _logger.LogInformation("Attempting to delete thumbnail for: {BlobName}", blobName);

 // Get connection string
 string? connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

 if (string.IsNullOrEmpty(connectionString))
 {
 _logger.LogError("AzureWebJobsStorage connection string not found");
 return;
}

 // Create blob client for thumbnails container
 var blobServiceClient = new BlobServiceClient(connectionString);
 var containerClient = blobServiceClient.GetBlobContainerClient("davendesiteimages-thumbnails");

 // Determine expected thumbnail name
 string thumbnailName;
 string[] videoExtensions = { ".mp4", ".mov", ".avi", ".wmv", ".mkv", ".webm" };

 // If the deleted blob was a video, thumbnails are stored as the base filename + .jpg (see GenerateVideoThumbnail)
 if (videoExtensions.Any(ext => blobName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
 {
 thumbnailName = Path.GetFileNameWithoutExtension(blobName) + ".jpg";
 }
 else
 {
 // For images, GenerateThumbnail uses the same blob name when saving thumbnails
 thumbnailName = blobName;
 }

 _logger.LogInformation("Deleting thumbnail blob: {ThumbnailName}", thumbnailName);

 // Delete the corresponding thumbnail
 var thumbnailBlobClient = containerClient.GetBlobClient(thumbnailName);
 
 var response = await thumbnailBlobClient.DeleteIfExistsAsync();
 
 if (response.Value)
 {
 _logger.LogInformation("Thumbnail deleted successfully: {BlobName}", thumbnailName);
 }
else
 {
 _logger.LogInformation("Thumbnail did not exist: {BlobName}", thumbnailName);
 }
 }
 catch (Exception ex)
 {
 _logger.LogError(ex, "Error deleting thumbnail: {Message}", ex.Message);
 throw;
 }
 }
}
