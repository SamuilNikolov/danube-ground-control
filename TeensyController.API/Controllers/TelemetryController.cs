using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TeensyController.API.Services;

namespace TeensyController.API.Controllers
{
    /// <summary>
    /// API controller to handle telemetry data retrieval and sending commands.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class TelemetryController : ControllerBase
    {
        // Reference to the SerialManager for communication.
        private readonly SerialManager _serialManager;

        /// <summary>
        /// Constructor with dependency injection for the SerialManager.
        /// </summary>
        /// <param name="serialManager">Injected SerialManager instance.</param>
        public TelemetryController(SerialManager serialManager)
        {
            _serialManager = serialManager;
        }

        /// <summary>
        /// GET endpoint that returns the latest telemetry data from the Teensy.
        /// Example: GET /Telemetry
        /// </summary>
        /// <returns>A JSON object containing the telemetry data.</returns>
        [HttpGet]
        public IActionResult GetTelemetry()
        {
            return Ok(new { telemetry = _serialManager.LatestTelemetry });
        }

        /// <summary>
        /// POST endpoint to send a generic command to the Teensy.
        /// Example: POST /Telemetry/command with JSON payload { "Command": "a" }
        /// </summary>
        /// <param name="request">The command request payload.</param>
        /// <returns>A JSON response confirming the command was sent.</returns>
        [HttpPost("command")]
        public IActionResult SendCommand([FromBody] CommandRequest request)
        {
            // Validate that the command string is not null or empty.
            if (string.IsNullOrWhiteSpace(request.Command))
            {
                return BadRequest("Command is required.");
            }
            // Enqueue the command to be sent to the Teensy.
            _serialManager.SendCommand(request.Command);
            return Ok(new { status = "Command sent", command = request.Command });
        }

        /// <summary>
        /// POST endpoint to trigger a millisecond-precise command sequence.
        /// This sequence performs the following:
        /// 1. Activates solenoid 1 immediately.
        /// 2. Waits 50 ms (non-blocking) then activates solenoid 2.
        /// 3. Holds both solenoids active for 4 seconds.
        /// 4. Deactivates both solenoids in quick succession.
        /// 
        /// Example: POST /Telemetry/precise
        /// </summary>
        [HttpGet("precise")]
        public IActionResult RunPreciseCommandSequence()
        {
            // Fire and forget the precise command sequence on a background task.
            Task.Run(async () =>
            {
                // 1. Activate solenoid 1 immediately ("s11" means solenoid 1 ON).
                _serialManager.SendCommand("s11");

                // 2. Wait 50 milliseconds before activating solenoid 2.
                await Task.Delay(50);

                // 3. Activate solenoid 2 ("s21" means solenoid 2 ON).
                _serialManager.SendCommand("s21");                // 2. Wait 50 milliseconds before activating solenoid 2.
                await Task.Delay(50);

                // 3. Activate solenoid 2 ("s21" means solenoid 2 ON).
                _serialManager.SendCommand("s31");                // 2. Wait 50 milliseconds before activating solenoid 2.
                await Task.Delay(50);

                // 3. Activate solenoid 2 ("s21" means solenoid 2 ON).
                _serialManager.SendCommand("s41");                // 2. Wait 50 milliseconds before activating solenoid 2.
                await Task.Delay(50);

                // 3. Activate solenoid 2 ("s21" means solenoid 2 ON).
                _serialManager.SendCommand("s51");                // 2. Wait 50 milliseconds before activating solenoid 2.
                await Task.Delay(50);

                // 3. Activate solenoid 2 ("s21" means solenoid 2 ON).
                _serialManager.SendCommand("s61");                // 2. Wait 50 milliseconds before activating solenoid 2.
                await Task.Delay(50);

                // 3. Activate solenoid 2 ("s21" means solenoid 2 ON).
                _serialManager.SendCommand("s71");                // 2. Wait 50 milliseconds before activating solenoid 2.
                await Task.Delay(50);

                // 3. Activate solenoid 2 ("s21" means solenoid 2 ON).
                _serialManager.SendCommand("s81");                // 2. Wait 50 milliseconds before activating solenoid 2.
                await Task.Delay(50);

                // 3. Activate solenoid 2 ("s21" means solenoid 2 ON).
                _serialManager.SendCommand("s91");                // 2. Wait 50 milliseconds before activating solenoid 2.
                await Task.Delay(50);

                // 3. Activate solenoid 2 ("s21" means solenoid 2 ON).
                _serialManager.SendCommand("s101");
                await Task.Delay(50);

                // 3. Activate solenoid 2 ("s21" means solenoid 2 ON).
                _serialManager.SendCommand("s111");
                await Task.Delay(50);

                // 3. Activate solenoid 2 ("s21" means solenoid 2 ON).
                _serialManager.SendCommand("s121");
                await Task.Delay(50);

                // 3. Activate solenoid 2 ("s21" means solenoid 2 ON).
                _serialManager.SendCommand("s131");
                await Task.Delay(50);

                // 3. Activate solenoid 2 ("s21" means solenoid 2 ON).
                _serialManager.SendCommand("s141");
                await Task.Delay(50);

                // 3. Activate solenoid 2 ("s21" means solenoid 2 ON).
                _serialManager.SendCommand("s151");
                await Task.Delay(50);

                // 3. Activate solenoid 2 ("s21" means solenoid 2 ON).
                _serialManager.SendCommand("s161");

                // 4. Hold both solenoids active for 4 seconds.
                await Task.Delay(4000);

                // 5. Deactivate both solenoids. ("s10" and "s20" mean OFF for solenoid 1 and 2 respectively.)
                _serialManager.SendCommand("s10");
                _serialManager.SendCommand("s20");
                _serialManager.SendCommand("s30");
                _serialManager.SendCommand("s40");
                _serialManager.SendCommand("s50");
                _serialManager.SendCommand("s60");
                _serialManager.SendCommand("s70");
                _serialManager.SendCommand("s80");
                _serialManager.SendCommand("s90");
                _serialManager.SendCommand("s100");
                _serialManager.SendCommand("s110");
                _serialManager.SendCommand("s120");
                _serialManager.SendCommand("s130");
                _serialManager.SendCommand("s140");
                _serialManager.SendCommand("s150");
                _serialManager.SendCommand("s160");
            });

            // Immediately return a response so that the main API remains responsive.
            return Ok("Precise command sequence started");
        }

        [HttpGet("sequencer")]
        public IActionResult RunSequencer()
        {
            // Fire-and-forget the sequencer task so that the API remains responsive.
            Task.Run(async () =>
            {
                const int solenoidCount = 16;
                for (int j = 0; j < 5; j++)
                {
                    for (int i = 1; i <= solenoidCount; i++)
                    {
                        _serialManager.SendCommand($"s{i}1");
                        await Task.Delay(20);
                    }

                    // (Optional) Hold all solenoids on for a desired period.
                    // await Task.Delay(4000);

                    // Turn off solenoids in reverse order with a 50ms delay.
                    for (int i = solenoidCount; i >= 1; i--)
                    {
                        _serialManager.SendCommand($"s{i}0");
                        await Task.Delay(20);
                    }
                }
                // Turn on solenoids sequentially with a 50ms delay.
               
            });

            return Ok("Sequencer command started");
        }

    }

    /// <summary>
    /// Represents the payload for sending a command.
    /// </summary>
    public class CommandRequest
    {
        /// <summary>
        /// The command to be sent to the Teensy (e.g., "a", "d", "s51").
        /// </summary>
        public string Command { get; set; }
    }
}
