namespace Denudey.Api.Controllers
{
    public class UpdateProfileDto
    {
        public string CountryCode { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; }
    }

}