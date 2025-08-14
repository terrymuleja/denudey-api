using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Denudey.Api.Domain.Entities;
using Denudey.Api.Services.Infrastructure.DbContexts;
using Denudey.Application.Interfaces;

namespace Denudey.Application.Services
{
    public class OrdersService(StatsDbContext context): IOrdersService
    {

        public async Task<IEnumerable<UserRequest>> GetOrdersForCreatorAsync(Guid creatorId)
        {
            var data = await context.UserRequests
                .Include(ur => ur.Creator)
                .Where(ur => ur.CreatorId == creatorId)
                .OrderByDescending(ur => ur.CreatedAt)
                .ToListAsync();
            return data;
        }
    }
}
