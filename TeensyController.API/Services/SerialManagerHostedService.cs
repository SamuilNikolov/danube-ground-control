using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace TeensyController.API.Services
{
    /// <summary>
    /// A hosted service that ensures the SerialManager starts with the application
    /// and stops when the application shuts down.
    /// </summary>
    public class SerialManagerHostedService : IHostedService
    {
        // The SerialManager instance injected via DI.
        private readonly SerialManager _serialManager;

        /// <summary>
        /// Constructor injecting the SerialManager.
        /// </summary>
        /// <param name="serialManager">The serial manager instance.</param>
        public SerialManagerHostedService(SerialManager serialManager)
        {
            _serialManager = serialManager;
        }

        /// <summary>
        /// Called when the host starts. Starts the SerialManager.
        /// </summary>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _serialManager.Start();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when the host is shutting down. Stops the SerialManager.
        /// </summary>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _serialManager.Stop();
            return Task.CompletedTask;
        }
    }
}
