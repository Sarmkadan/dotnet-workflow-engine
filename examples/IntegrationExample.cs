using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DotNetWorkflowEngine.Configuration;

// Shows how to wire into ASP.NET Core DI
var builder = Host.CreateApplicationBuilder(args);

// Register the engine and necessary infrastructure
// This extension method handles internal service registration
builder.Services.AddWorkflowEngine(builder.Configuration);

// Infrastructure for data and background jobs (example wiring)
// builder.Services.AddDbContext<DatabaseContext>(...);
// builder.Services.AddHangfire(...);

using var host = builder.Build();

// Workflow engine is now available for injection in Controllers/Services
// Example usage in a controller:
// public class MyController : ControllerBase {
//     public MyController(IWorkflowExecutionService service) { ... }
// }
Console.WriteLine("Workflow Engine registered and ready for dependency injection.");
