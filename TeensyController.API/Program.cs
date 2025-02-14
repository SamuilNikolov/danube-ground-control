using TeensyController.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Retrieve the COM port from the command-line arguments.
// If no argument is provided, default to "COM5".
string portName = args.Length > 0 ? args[0] : "COM15";
int baudRate = 115200; // Fixed baud rate

Console.WriteLine($"Using COM port: {portName} at {baudRate} baud.");

// Create an instance of SerialManager using the specified COM port and baud rate,
// and register it as a singleton so that the same instance is shared across the app.
var serialManager = new SerialManager(portName, baudRate);
builder.Services.AddSingleton(serialManager);

// Register the hosted service that starts/stops the SerialManager when the app starts/stops.
builder.Services.AddHostedService<SerialManagerHostedService>();

// Add API controllers.
builder.Services.AddControllers();

var app = builder.Build();

// Enable serving static files from the wwwroot folder (e.g., index.html).
app.UseDefaultFiles(); // Looks for default files like index.html.
app.UseStaticFiles();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Run the application.
app.Run();
