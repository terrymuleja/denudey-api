using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Domain.DTOs
{
    public static class Mappings
    {
        public static ProductSummaryDto ToSummary(this ProductDetailsDto productDetailsDto)
        {
            return new ProductSummaryDto { 
                ProductName = productDetailsDto.ProductName,
                MainPhotoUrl = productDetailsDto.MainPhotoUrl,
                BodyPart = productDetailsDto.BodyPart,
                Tags = productDetailsDto.Tags,

                CreatorUsername = productDetailsDto.CreatorUsername,
                ModifiedAt = productDetailsDto.ModifiedAt,                
                CreatedAt = productDetailsDto.CreatedAt,
                
                 };
        }
    }
}
