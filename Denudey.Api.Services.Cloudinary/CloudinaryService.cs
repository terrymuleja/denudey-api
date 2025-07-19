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
            var uri = new Uri(imageUrl);
            var segments = uri.AbsolutePath.Split('/');
            var fileName = Path.GetFileNameWithoutExtension(segments.Last());
            var folderPath = string.Join("/", segments.Skip(segments.ToList().FindIndex(s => s == "upload") + 1).Take(segments.Length - 2));
            return string.IsNullOrEmpty(folderPath) ? fileName : $"{folderPath}/{fileName}";
        }


    }


}
