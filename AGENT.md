# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Run Commands
- Build project: `dotnet build`
- Run application: `dotnet run`
- Publish for Windows: `dotnet publish -c Release -r win-x64 --self-contained`

## Architecture and Structure
The project is a .NET 10 Windows application designed to prevent "JBL Go 4" speakers from automatically powering off by emitting a near-silent audio heartbeat.

### Core Components
- **Entry Point (`Program.cs`)**: Bootstraps a .NET Generic Host and launches a Windows Forms `TrayApplicationContext`.
- **Background Worker (`Worker.cs`)**: A `BackgroundService` that monitors audio endpoints using `NAudio`. It detects the connection/disconnection of a "JBL Go 4" device and manages the audio signal generation.
- **Status Service (`JblStatusService.cs`)**: A singleton that maintains the current connection state and provides an event (`OnStatusChanged`) to notify other components.
- **Tray UI (`TrayApplicationContext.cs`)**: Handles the system tray icon, updating its icon and tooltip based on the `JblStatusService` state.
- **Notification Client (`AudioNotificationClient.cs`)**: Implements the Windows audio endpoint notification callback to trigger immediate status updates when devices are plugged/unplugged.

### Key Dependencies
- `NAudio`: Used for audio device enumeration and signal generation.
- `Microsoft.Extensions.Hosting`: Provides the background service framework.
- `System.Windows.Forms`: Used for the system tray integration.
