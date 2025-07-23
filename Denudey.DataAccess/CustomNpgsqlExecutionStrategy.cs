using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Denudey.DataAccess
{
    public class CustomNpgsqlExecutionStrategy : ExecutionStrategy
    {
        public CustomNpgsqlExecutionStrategy(ExecutionStrategyDependencies dependencies)
            : base(dependencies, maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10))
        {
        }

        protected override bool ShouldRetryOn(Exception exception)
        {
            // Add retry logic for transient PostgreSQL exceptions if needed
            if (exception is NpgsqlException npgsqlException)
            {
                // Check for specific transient error codes
                return npgsqlException.SqlState == "08000" || // Connection exception
                       npgsqlException.SqlState == "08003" || // Connection does not exist
                       npgsqlException.SqlState == "08006" || // Connection failure
                       npgsqlException.SqlState == "57P01" || // Admin shutdown
                       npgsqlException.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                       npgsqlException.Message.Contains("connection", StringComparison.OrdinalIgnoreCase);
            }

            // Also retry on socket exceptions and timeouts
            return exception is SocketException ||
                   exception is TimeoutException;
        }
    }
}
