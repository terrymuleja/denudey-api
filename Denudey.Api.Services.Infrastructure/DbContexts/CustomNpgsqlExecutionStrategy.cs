using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace Denudey.Api.Services.Infrastructure.DbContexts
{
    public class CustomNpgsqlExecutionStrategy : NpgsqlRetryingExecutionStrategy
    {
        public CustomNpgsqlExecutionStrategy(ExecutionStrategyDependencies dependencies)
            : base(dependencies)
        {
        }

        // Explicitly override this to ensure transaction support
        public override bool RetriesOnFailure => true;

        protected override bool ShouldRetryOn(Exception exception)
        {
            // Call the base implementation first (it has good defaults)
            if (base.ShouldRetryOn(exception))
                return true;

            // Then add your custom logic
            if (exception is NpgsqlException npgsqlException)
            {
                return npgsqlException.SqlState == "08000" ||
                       npgsqlException.SqlState == "08003" ||
                       npgsqlException.SqlState == "08006" ||
                       npgsqlException.SqlState == "57P01" ||
                       npgsqlException.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                       npgsqlException.Message.Contains("connection", StringComparison.OrdinalIgnoreCase);
            }

            return exception is SocketException || exception is TimeoutException;
        }
    }
}
