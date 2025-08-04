using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Denudey.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LogTestController : ControllerBase
    {
        private readonly ILogger<LogTestController> _logger;
        private readonly ILoggerFactory _loggerFactory;

        public LogTestController(ILogger<LogTestController> logger, ILoggerFactory loggerFactory)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            var timestamp = DateTime.UtcNow;

            // Force console output first
            Console.WriteLine($"🎯🎯🎯 DIRECT CONSOLE: LogTestController.Ping hit at {timestamp} 🎯🎯🎯");

            // Test all log levels
            _logger.LogTrace("🔍 TRACE: LogTestController.Ping hit at {time}", timestamp);
            _logger.LogDebug("🐛 DEBUG: LogTestController.Ping hit at {time}", timestamp);
            _logger.LogInformation("ℹ️ INFO: LogTestController.Ping hit at {time}", timestamp);
            _logger.LogWarning("⚠️ WARNING: LogTestController.Ping hit at {time}", timestamp);
            _logger.LogError("🔥 ERROR: LogTestController.Ping hit at {time}", timestamp);
            _logger.LogCritical("💀 CRITICAL: LogTestController.Ping hit at {time}", timestamp);

            Console.WriteLine("🎯 CONSOLE: All log levels attempted");

            return Ok(new
            {
                message = "pong",
                timestamp = timestamp,
                note = "Check console for log output - if you see console messages but not logger messages, there's a logging config issue"
            });
        }

        [HttpGet("debug")]
        public IActionResult Debug()
        {
            Console.WriteLine("🔍 CONSOLE: Debug endpoint hit");

            try
            {
                // Test if we can create loggers manually
                var manualLogger = _loggerFactory.CreateLogger("MANUAL_TEST");
                manualLogger.LogError("🧪 MANUAL LOGGER TEST: This is a manually created logger");

                Console.WriteLine("🧪 CONSOLE: Manual logger test completed");

                // Get logging configuration info
                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

                _logger.LogError("🔍 DEBUG INFO:");
                _logger.LogError("Environment: {env}", environment);
                _logger.LogError("Current time: {time}", DateTime.UtcNow);

                Console.WriteLine($"🔍 CONSOLE DEBUG - Environment: {environment}");

                return Ok(new
                {
                    environment,
                    timestamp = DateTime.UtcNow,
                    manualLoggerCreated = true,
                    message = "Debug info logged - check console and logs"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🚨 CONSOLE ERROR in Debug: {ex.Message}");
                _logger.LogError(ex, "Error in debug endpoint");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("force-console")]
        public IActionResult ForceConsole()
        {
            // Just pure console output to verify console works
            Console.WriteLine("==========================================");
            Console.WriteLine("🎯 FORCE CONSOLE TEST");
            Console.WriteLine($"🕐 Time: {DateTime.UtcNow}");
            Console.WriteLine("🌐 This endpoint only uses Console.WriteLine");
            Console.WriteLine("📍 If you see this, console output is working");
            Console.WriteLine("🔧 If you don't see logger output in other endpoints,");
            Console.WriteLine("   there's definitely a logging configuration issue");
            Console.WriteLine("==========================================");

            return Ok(new
            {
                message = "Console test completed",
                instruction = "Check your console/logs for the test output above"
            });
        }
    }
}