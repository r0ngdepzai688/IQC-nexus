# IQC Nexus Client Agent — Installation & Hosting Guide

## Development Execution

1. Build executable:
   ```bash
   dotnet build src/IqcQms.ClientAgent/IqcQms.ClientAgent.csproj
   ```
2. Run in Console Host mode:
   ```bash
   dotnet run --project src/IqcQms.ClientAgent/IqcQms.ClientAgent.csproj -- --AgentOptions:PairingCode=XXXXXX
   ```

## Windows Service Installation (Production Deployment)

1. Publish win-x64 binary:
   ```bash
   dotnet publish src/IqcQms.ClientAgent/IqcQms.ClientAgent.csproj -c Release -r win-x64 --self-contained false -o C:\Services\IqcQmsClientAgent
   ```
2. Create Windows Service using `sc.exe`:
   ```cmd
   sc.exe create IqcQmsClientAgent binPath= "C:\Services\IqcQmsClientAgent\IqcQms.ClientAgent.exe" start= auto
   sc.exe start IqcQmsClientAgent
   ```
