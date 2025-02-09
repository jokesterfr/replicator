using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using es_replicator;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using es_replicator.Settings;
using EventStore.Replicator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
var isDebug     = Environment.GetEnvironmentVariable("REPLICATOR_DEBUG") != null;
var logConfig   = new LoggerConfiguration();
logConfig = isDebug ? logConfig.MinimumLevel.Debug() : logConfig.MinimumLevel.Information();

logConfig = logConfig
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Grpc", LogEventLevel.Error)
    .Enrich.FromLogContext();

logConfig = environment?.ToLower() == "development"
    ? logConfig.WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} <s:{SourceContext}>;{NewLine}{Exception}"
    )
    : logConfig.WriteTo.Console(new RenderedCompactJsonFormatter());
Log.Logger = logConfig.CreateLogger();
var fileInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
Log.Information("Starting replicator {Version}", fileInfo.ProductVersion);

var host = Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(
        webBuilder => {
            webBuilder.UseSerilog();
            webBuilder.UseStartup<Startup>();
        }
    )
    .ConfigureAppConfiguration(
        config => config
            .AddYamlFile("./config/appsettings.yaml", false, true)
            .AndEnvConfig()
    )
    .Build();
var restartOnFailure = host.Services.GetService<ReplicatorOptions>()?.RestartOnFailure == true;

try {
    host.Run();
    return 0;
}
catch (Exception ex) {
    Log.Fatal(ex, "Host terminated unexpectedly");
    if (restartOnFailure) return -1;

    while (true) {
        await Task.Delay(5000);
    }
}
finally {
    Log.CloseAndFlush();
}