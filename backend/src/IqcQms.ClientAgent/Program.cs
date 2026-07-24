using IqcQms.ClientAgent;
using IqcQms.ClientAgent.Application.Config;
using IqcQms.ClientAgent.Application.Identity;
using IqcQms.ClientAgent.Application.Providers;
using IqcQms.ClientAgent.Infrastructure.Http;
using IqcQms.ClientAgent.Infrastructure.Identity;
using IqcQms.ClientAgent.Infrastructure.Providers;
using IqcQms.ClientAgent.Infrastructure.Queue;

var builder = Host.CreateApplicationBuilder(args);

// Support Windows Service hosting if running as Windows Service
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "IqcQmsClientAgent";
});

// Configure Options
builder.Services.Configure<AgentOptions>(builder.Configuration.GetSection("AgentOptions"));

var agentOptions = builder.Configuration.GetSection("AgentOptions").Get<AgentOptions>() ?? new AgentOptions();
var localDataPath = string.IsNullOrWhiteSpace(agentOptions.LocalStorePath)
    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IqcQmsAgent")
    : Path.GetFullPath(agentOptions.LocalStorePath);

Directory.CreateDirectory(localDataPath);

// Register Identity & Secure Store
builder.Services.AddSingleton<WindowsDpapiDeviceIdentityStore>(sp =>
    new WindowsDpapiDeviceIdentityStore(localDataPath, sp.GetRequiredService<ILogger<WindowsDpapiDeviceIdentityStore>>()));

builder.Services.AddSingleton<IDeviceIdentityStore>(sp => sp.GetRequiredService<WindowsDpapiDeviceIdentityStore>());
builder.Services.AddSingleton<ISecureCredentialStore>(sp => sp.GetRequiredService<WindowsDpapiDeviceIdentityStore>());

// Register Local Queue
builder.Services.AddSingleton<ILocalAgentQueue>(sp =>
    new SqliteLocalAgentQueue(localDataPath, agentOptions, sp.GetRequiredService<ILogger<SqliteLocalAgentQueue>>()));

// Register Providers
builder.Services.AddSingleton<IClientDataProvider, SyntheticClientDataProvider>();
builder.Services.AddSingleton<IClientDataProviderRegistry, ClientDataProviderRegistry>();

// Register Http API Client
builder.Services.AddHttpClient<IAgentApiClient, AgentApiClient>();

// Register Worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
