using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Services.Cloudinary.Interfaces
{
    public interface ICloudinaryService
    {
        Task<bool> DeleteImageFromCloudinary(string imageUrl);
    }
}
