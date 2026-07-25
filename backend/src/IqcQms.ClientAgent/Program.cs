using IqcQms.ClientAgent;
using IqcQms.ClientAgent.Application.Config;
using IqcQms.ClientAgent.Application.Identity;
using IqcQms.ClientAgent.Application.Nasca;
using IqcQms.ClientAgent.Application.Providers;
using IqcQms.ClientAgent.Application.Runtime;
using IqcQms.ClientAgent.Application.Storage;
using IqcQms.ClientAgent.Application.Startup;
using IqcQms.ClientAgent.Infrastructure.Http;
using IqcQms.ClientAgent.Infrastructure.Identity;
using IqcQms.ClientAgent.Infrastructure.Nasca;
using IqcQms.ClientAgent.Infrastructure.Providers;
using IqcQms.ClientAgent.Infrastructure.Queue;
using IqcQms.ClientAgent.Infrastructure.Runtime;
using IqcQms.ClientAgent.Infrastructure.Storage;
using IqcQms.ClientAgent.Infrastructure.Startup;

var builder = Host.CreateApplicationBuilder(args);

// Configure Options
builder.Services.Configure<AgentOptions>(builder.Configuration.GetSection("AgentOptions"));
builder.Services.Configure<NascaOptions>(builder.Configuration.GetSection("NascaOptions"));

var agentOptions = builder.Configuration.GetSection("AgentOptions").Get<AgentOptions>() ?? new AgentOptions();
agentOptions.Validate(builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing"));

var nascaOptions = builder.Configuration.GetSection("NascaOptions").Get<NascaOptions>() ?? new NascaOptions();
nascaOptions.Validate(builder.Environment.IsProduction());

// Isolate Windows Service mode behind explicit compatibility flag (disabled by default)
if (agentOptions.EnableCompatibilityWindowsService)
{
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "IqcQmsClientAgent";
    });
}

// 1. User-scoped Path Resolver & Security Validator
var pathResolver = new AgentPathResolver(agentOptions.AgentProfile, agentOptions.LocalStorePath);
builder.Services.AddSingleton<IAgentPathResolver>(pathResolver);
builder.Services.AddSingleton<IAllowedInputPathValidator, AllowedInputPathValidator>();

// 2. Identity & DPAPI Store
builder.Services.AddSingleton<WindowsDpapiDeviceIdentityStore>(sp =>
    new WindowsDpapiDeviceIdentityStore(pathResolver, sp.GetRequiredService<ILogger<WindowsDpapiDeviceIdentityStore>>()));

builder.Services.AddSingleton<IDeviceIdentityStore>(sp => sp.GetRequiredService<WindowsDpapiDeviceIdentityStore>());
builder.Services.AddSingleton<ISecureCredentialStore>(sp => sp.GetRequiredService<WindowsDpapiDeviceIdentityStore>());

// 3. Local Queue
builder.Services.AddSingleton<ILocalAgentQueue>(sp =>
    new SqliteLocalAgentQueue(pathResolver.RootDataDirectory, agentOptions, sp.GetRequiredService<ILogger<SqliteLocalAgentQueue>>()));

// 4. Single Instance Lock
builder.Services.AddSingleton<ISingleInstanceLock>(sp =>
    new WindowsSingleInstanceLock(pathResolver.ProfileName, sp.GetRequiredService<ILogger<WindowsSingleInstanceLock>>()));

// 5. Startup Registration
builder.Services.AddSingleton<IUserStartupRegistration, WindowsHkcuRunStartupRegistration>();

// 6. Providers, NASCA Inspector & Disabled Runner, HTTP Client
builder.Services.AddSingleton<IClientDataProvider, SyntheticClientDataProvider>();
builder.Services.AddSingleton<IClientDataProviderRegistry, ClientDataProviderRegistry>();
builder.Services.AddSingleton<INascaInstallationInspector, NascaInstallationInspector>();
builder.Services.AddSingleton<INascaJobRunner, NascaJobRunnerNotConfigured>();
builder.Services.AddHttpClient<IAgentApiClient, AgentApiClient>();

// 7. Hosted Worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
