using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Application.Interfaces
{
    public interface IDeliveryValidationService
    {
        Task TriggerValidationAsync(Guid requestId, string imageUrl, string expectedText);
    }
}
