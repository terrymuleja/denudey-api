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
                if (string.IsNullOrWhiteSpace(imageUrl))
                {
                    logger.LogWarning("DeleteImageFromCloudinary: imageUrl is null or empty.");
                    return false;
                }

                var publicId = ExtractPublicId(imageUrl);
                if (string.IsNullOrWhiteSpace(publicId))
                {
                    logger.LogWarning("DeleteImageFromCloudinary: Failed to extract publicId from URL: {ImageUrl}", imageUrl);
                    return false;
                }

                logger.LogInformation("Attempting to delete Cloudinary image with publicId: {PublicId}", publicId);

                var deletionParams = new DeletionParams(publicId);
                var result = await cloudinary.DestroyAsync(deletionParams);

                if (result.Result == "ok")
                {
                    logger.LogInformation("Successfully deleted image with publicId: {PublicId}", publicId);
                    return true;
                }

                logger.LogWarning("Cloudinary deletion failed for publicId: {PublicId}. Result: {Result}", publicId, result.Result);
                return false;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception occurred while deleting image from Cloudinary. URL: {ImageUrl}", imageUrl);
                return false;
            }
        }

        private string ExtractPublicId(string imageUrl)
        {
            try
            {
                var uri = new Uri(imageUrl);
                var path = uri.AbsolutePath;

                // Remove leading / and /upload/ prefix
                var parts = path.Split("/upload/");
                if (parts.Length != 2)
                    return null;

                var publicPart = parts[1];

                // Remove version prefix (v1234567890/)
                var versionAndRest = publicPart.Split('/');
                if (versionAndRest.Length < 2)
                    return null;

                var withoutVersion = string.Join('/', versionAndRest.Skip(1));
                var publicId = Path.ChangeExtension(withoutVersion, null); // removes .png

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
