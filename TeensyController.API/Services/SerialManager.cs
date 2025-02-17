using System;
using System.Collections.Concurrent;
using System.IO.Ports;
using System.Text;
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
        // Holds the previous telemetry timestamp in milliseconds (initially -1 means “not set”).
        private long _previousTimestamp = -1;

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
            // Buffer to accumulate incoming data
            StringBuilder buffer = new StringBuilder();

            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Read all available data from the serial port.
                    string data = _serialPort.ReadExisting();
                    if (!string.IsNullOrEmpty(data))
                    {
                        // Append new data to the buffer.
                        buffer.Append(data);

                        // Process all complete telemetry lines (i.e. lines ending with '\n').
                        string allData = buffer.ToString();
                        int newlineIndex;
                        while ((newlineIndex = allData.IndexOf('\n')) != -1)
                        {
                            // Extract one complete line.
                            string completeLine = allData.Substring(0, newlineIndex).Trim();

                            // Only update LatestTelemetry if the line is not empty.
                            if (!string.IsNullOrEmpty(completeLine))
                            {
                                // Attempt to extract the timestamp from the telemetry.
                                long currentTimestamp = 0;
                                bool timestampParsed = false;
                                // Assuming the telemetry is formatted like "TS:1952962 | ARM:0 | ..."
                                var parts = completeLine.Split('|');
                                foreach (var part in parts)
                                {
                                    string trimmedPart = part.Trim();
                                    if (trimmedPart.StartsWith("TS:"))
                                    {
                                        string tsValue = trimmedPart.Substring(3).Trim();
                                        if (long.TryParse(tsValue, out currentTimestamp))
                                        {
                                            timestampParsed = true;
                                        }
                                        break;
                                    }
                                }

                                // If the timestamp was parsed, calculate the age (delta in ms)
                                if (timestampParsed)
                                {
                                    long age = 0;
                                    if (_previousTimestamp < 0)
                                    {
                                        // First telemetry received, so no previous timestamp exists.
                                        age = 0;
                                    }
                                    else
                                    {
                                        age = currentTimestamp - _previousTimestamp;
                                    }
                                    // Update _previousTimestamp with the current telemetry's timestamp.
                                    _previousTimestamp = currentTimestamp;

                                    // Append the telemetry age.
                                    completeLine += $" | AGE:{age}ms";
                                }

                                LatestTelemetry = completeLine;
                            }

                            // Remove the processed line (and the newline) from the data.
                            allData = allData.Substring(newlineIndex + 1);
                        }

                        // Clear the buffer and append any leftover incomplete data.
                        buffer.Clear();
                        buffer.Append(allData);
                    }
                }
                catch (TimeoutException)
                {
                    // Ignore timeouts.
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ReadLoop exception: " + ex.Message);
                }

                // Sleep briefly to reduce CPU usage.
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
