using System;
using System.Collections.Concurrent;
using System.IO.Ports;
using System.Threading;

namespace TeensyController.API.Services
{
    /// <summary>
    /// Manages serial port communication with the Teensy device.
    /// This class handles sending commands and reading telemetry data on separate threads.
    /// </summary>
    public class SerialManager : IDisposable
    {
        // The SerialPort instance for communication.
        private readonly SerialPort _serialPort;
        // Threads for reading and writing.
        private Thread _readThread;
        private Thread _writeThread;
        // Thread-safe queue for commands waiting to be sent.
        private readonly ConcurrentQueue<string> _commandQueue;
        // Token source to signal cancellation to threads.
        private readonly CancellationTokenSource _cts;
        // Lock object to synchronize access to telemetry data.
        private readonly object _telemetryLock = new object();
        // Holds the most recent telemetry string.
        private string _latestTelemetry = "";

        /// <summary>
        /// Public property to get the latest telemetry data in a thread-safe manner.
        /// </summary>
        public string LatestTelemetry
        {
            get { lock (_telemetryLock) { return _latestTelemetry; } }
            private set { lock (_telemetryLock) { _latestTelemetry = value; } }
        }

        /// <summary>
        /// Constructor creates and configures the SerialPort, command queue, and cancellation token.
        /// </summary>
        /// <param name="portName">The COM port name (e.g., "COM5").</param>
        /// <param name="baudRate">The communication baud rate.</param>
        public SerialManager(string portName, int baudRate)
        {
            _serialPort = new SerialPort(portName, baudRate)
            {
                // Define newline character for ReadLine method.
                NewLine = "\n",
                // Set read timeout (milliseconds).
                ReadTimeout = 500
            };
            // Initialize the thread-safe command queue.
            _commandQueue = new ConcurrentQueue<string>();
            // Create a cancellation token source to manage thread cancellation.
            _cts = new CancellationTokenSource();
        }

        /// <summary>
        /// Starts the serial port communication and spawns the read/write threads.
        /// </summary>
        public void Start()
        {
            // Open the serial port.
            _serialPort.Open();

            // Create and start the reading thread (set as background so it ends when the process exits).
            _readThread = new Thread(ReadLoop) { IsBackground = true };
            // Create and start the writing thread.
            _writeThread = new Thread(WriteLoop) { IsBackground = true };

            // Start the threads, passing the cancellation token.
            _readThread.Start(_cts.Token);
            _writeThread.Start(_cts.Token);
        }

        /// <summary>
        /// Stops the serial communication by canceling the token and closing the port.
        /// </summary>
        public void Stop()
        {
            // Signal cancellation to both threads.
            _cts.Cancel();
            // If the port is still open, close it.
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }
        }

        /// <summary>
        /// The read loop runs in its own thread to continuously read telemetry data.
        /// </summary>
        /// <param name="obj">The cancellation token passed as an object.</param>
        private void ReadLoop(object obj)
        {
            CancellationToken token = (CancellationToken)obj;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Read all available data from the serial port.
                    string data = _serialPort.ReadExisting();
                    if (!string.IsNullOrEmpty(data))
                    {
                        // Split the data into lines.
                        string[] lines = data.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                        if (lines.Length > 0)
                        {
                            // Update with only the most recent telemetry.
                            LatestTelemetry = lines[lines.Length - 1];
                        }
                    }
                }
                catch (TimeoutException)
                {
                    // Occasional timeouts can happen; just ignore them.
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ReadLoop exception: " + ex.Message);
                }

                // Sleep briefly to avoid hogging the CPU.
                Thread.Sleep(10);
            }
        }

        /// <summary>
        /// The write loop runs in its own thread to continuously send commands from the queue.
        /// </summary>
        /// <param name="obj">The cancellation token passed as an object.</param>
        private void WriteLoop(object obj)
        {
            CancellationToken token = (CancellationToken)obj;
            // Continue looping until cancellation is requested.
            while (!token.IsCancellationRequested)
            {
                // Try to dequeue a command from the command queue.
                if (_commandQueue.TryDequeue(out string command))
                {
                    try
                    {
                        // Send the command to the serial port.
                        _serialPort.WriteLine(command);
                    }
                    catch (Exception ex)
                    {
                        // Log any exceptions encountered while writing.
                        Console.WriteLine("WriteLoop exception: " + ex.Message);
                    }
                }
                else
                {
                    // If no command is available, sleep briefly to reduce CPU usage.
                    Thread.Sleep(10);
                }
            }
        }

        /// <summary>
        /// Adds a command to the queue to be sent by the write loop.
        /// </summary>
        /// <param name="command">The command string to send.</param>
        public void SendCommand(string command)
        {
            _commandQueue.Enqueue(command);
        }

        /// <summary>
        /// Disposes of the SerialManager resources.
        /// </summary>
        public void Dispose()
        {
            // Stop any ongoing operations.
            Stop();
            // Dispose the serial port if it exists.
            _serialPort?.Dispose();
        }
    }
}
