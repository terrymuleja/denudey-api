using System.Net;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Denudey.Api.Services.Cloudinary.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Denudey.Api.Services.Cloudinary
{
    public class CloudinaryService(IOptions<CloudinarySettings> cloudinarySettings, ILogger<CloudinaryService> logger) : ICloudinaryService
    {
        private readonly CloudinaryDotNet.Cloudinary cloudinary = new(new Account(
            cloudinarySettings.Value.CloudName,
            cloudinarySettings.Value.ApiKey,
            cloudinarySettings.Value.ApiSecret));

        public async Task<bool> DeleteImageFromCloudinary(string imageUrl)
        {
            try
            {
                // Handle null, empty, or whitespace URLs gracefully
                if (string.IsNullOrWhiteSpace(imageUrl))
                {
                    logger.LogInformation("DeleteImageFromCloudinary: imageUrl is null or empty. No image to delete, considering operation successful.");
                    return true; // No image to delete is considered successful
                }

                var publicId = ExtractPublicId(imageUrl);
                if (string.IsNullOrWhiteSpace(publicId))
                {
                    logger.LogWarning("DeleteImageFromCloudinary: Failed to extract publicId from URL: {ImageUrl}. Considering deletion successful since URL is invalid.", imageUrl);
                    return true; // Invalid URL format, nothing to delete
                }

                // First, check if the image exists
                logger.LogInformation("Checking if Cloudinary image exists with publicId: {PublicId}", publicId);

                var resourceParams = new GetResourceParams(publicId)
                {
                    ResourceType = ResourceType.Image
                };

                try
                {
                    var resourceResult = await cloudinary.GetResourceAsync(resourceParams);

                    if (resourceResult == null || resourceResult.StatusCode == HttpStatusCode.NotFound)
                    {
                        logger.LogInformation("Image with publicId {PublicId} does not exist in Cloudinary. Considering deletion successful.", publicId);
                        return true; // Image doesn't exist, so deletion goal is achieved
                    }

                    logger.LogInformation("Image with publicId {PublicId} exists in Cloudinary. Proceeding with deletion.", publicId);
                }
                catch (Exception ex)
                {
                    // If we get a 404 or similar error, the image doesn't exist
                    if (ex.Message.Contains("404") || ex.Message.Contains("Not Found"))
                    {
                        logger.LogInformation("Image with publicId {PublicId} does not exist in Cloudinary (confirmed via exception). Considering deletion successful.", publicId);
                        return true;
                    }

                    // For other errors during existence check, log but continue with deletion attempt
                    logger.LogWarning("Error checking image existence for publicId {PublicId}: {Error}. Proceeding with deletion attempt.", publicId, ex.Message);
                }

                // Image exists, proceed with deletion
                logger.LogInformation("Attempting to delete Cloudinary image with publicId: {PublicId}", publicId);

                var deletionParams = new DeletionParams(publicId);
                var result = await cloudinary.DestroyAsync(deletionParams);

                if (result.Result == "ok")
                {
                    logger.LogInformation("Successfully deleted image with publicId: {PublicId}", publicId);
                    return true;
                }
                else if (result.Result == "not found")
                {
                    // Image was not found during deletion - this is also considered successful
                    logger.LogInformation("Image with publicId {PublicId} was not found during deletion. Considering deletion successful.", publicId);
                    return true;
                }
                else
                {
                    logger.LogWarning("Cloudinary deletion failed for publicId: {PublicId}. Result: {Result}", publicId, result.Result);
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception occurred while deleting Cloudinary image: {ImageUrl}", imageUrl);
                return false;
            }
        }

        private string ExtractPublicId(string imageUrl)
        {
            try
            {
                // Handle null or empty URLs
                if (string.IsNullOrWhiteSpace(imageUrl))
                {
                    logger.LogDebug("ExtractPublicId: imageUrl is null or empty");
                    return null;
                }

                // Validate URL format
                if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
                {
                    logger.LogWarning("ExtractPublicId: Invalid URL format: {ImageUrl}", imageUrl);
                    return null;
                }

                var path = uri.AbsolutePath;

                // Check if this is a Cloudinary URL
                if (!path.Contains("/upload/"))
                {
                    logger.LogWarning("ExtractPublicId: URL does not appear to be a Cloudinary URL (missing /upload/): {ImageUrl}", imageUrl);
                    return null;
                }

                // Remove leading / and /upload/ prefix
                var parts = path.Split("/upload/");
                if (parts.Length != 2)
                {
                    logger.LogWarning("ExtractPublicId: Unexpected URL structure: {ImageUrl}", imageUrl);
                    return null;
                }

                var publicPart = parts[1];

                // Remove version prefix (v1234567890/)
                var versionAndRest = publicPart.Split('/');
                if (versionAndRest.Length < 2)
                {
                    logger.LogWarning("ExtractPublicId: Missing version or path components in URL: {ImageUrl}", imageUrl);
                    return null;
                }

                var withoutVersion = string.Join('/', versionAndRest.Skip(1));
                var publicId = Path.ChangeExtension(withoutVersion, null); // removes .png

                logger.LogDebug("ExtractPublicId: Successfully extracted publicId '{PublicId}' from URL: {ImageUrl}", publicId, imageUrl);
                return publicId;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to extract publicId from imageUrl: {ImageUrl}", imageUrl);
                return null;
            }
        }
    }
}