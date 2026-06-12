# EmbyNotify — Dev Log

This document captures the conversation and reasoning behind how EmbyNotify was built.

---

## Origin

EmbyNotify was spun out of [XC2EMBY](https://github.com/sftech13/EMBY-XC), an Emby plugin for Xtream-compatible IPTV providers.

XC2EMBY gained a feature in v1.1.98 where the plugin automatically broadcasts a popup to all active Emby sessions when the IPTV provider goes offline or comes back online. The broadcast uses Emby's native `ISessionManager.SendMessageCommand()` — the same mechanism as the "Send Message" button in the Emby admin dashboard.

The idea for EmbyNotify came from wanting that same broadcast capability as a standalone tool — one where an admin can type any message and push it to all connected users on demand, without it being tied to provider health events.

---

## What Was Built

A minimal Emby plugin with:

- A config page (`config.html`) with three fields:
  - **Header** — the popup title
  - **Message** — the popup body
  - **Auto-dismiss (seconds)** — `0` stays until dismissed, any other value auto-closes
- A **Send to All Sessions** button that POSTs to the plugin's own API endpoint
- `POST /EmbyNotify/Send` — admin-authenticated REST endpoint that calls `ISessionManager.SendMessageCommand()` for every active session
- Success/failure feedback with session count returned to the UI

---

## Technical Notes

### How Emby session broadcasting works

Emby's `ISessionManager` maintains a list of all active client sessions. `SendMessageCommand()` sends a `MessageCommand` to a specific session by ID. The command has three fields:

- `Header` — popup title text
- `Text` — popup body text
- `TimeoutMs` — auto-dismiss delay in milliseconds (`0` = no auto-dismiss)

EmbyNotify iterates all sessions and fires `SendMessageCommand` for each one. Sessions that fail (e.g., the client disconnected between the loop start and that iteration) are caught individually so one bad session doesn't abort the rest.

### Shared HttpClient lesson (from XC2EMBY)

XC2EMBY had two bugs where a shared `HttpClient` was being disposed — once in `ProviderHealthMonitor` (via a `using` block) and once in `XtreamLiveStream.Dispose()`. Both caused `ObjectDisposedException` on all subsequent HTTP calls. EmbyNotify has no HTTP client of its own, so this is not a concern here, but it's worth noting: in Emby plugins, `Plugin.CreateHttpClient()` returns a static shared instance and must never be wrapped in `using` or otherwise disposed.

### Plugin GUID

Every Emby plugin needs a stable unique `Guid` returned from `Plugin.Id`. EmbyNotify uses:

```
3c8f1e2a-4b7d-4e9f-a0c5-d6e7f8091b2c
```

This must never change after the plugin is installed on a server, as Emby uses it to identify the plugin across restarts and upgrades.

### Authentication

All API endpoints use `[Authenticated(Roles = "Admin")]` — only admin users can send broadcasts. Hitting the endpoint from a browser requires appending `?api_key=<your_api_key>` to the URL.

---

## File Structure

```
EmbyNotify/
  EmbyNotify.Plugin/
    Api/
      NotifyApi.cs          — REST endpoint: POST /EmbyNotify/Send
    Configuration/
      PluginConfiguration.cs — BasePluginConfiguration subclass (default header/timeout)
      Web/
        config.html          — Admin config page with send form
    Plugin.cs               — Plugin registration + BroadcastAsync()
    EmbyNotify.Plugin.csproj
  Directory.Build.props     — Keeps build artifacts out of the source tree
  .gitignore
  README.md
  CHANGELOG.md
  DEVLOG.md                 — This file
```

---

## Build & Deploy

```bash
dotnet build EmbyNotify.Plugin/EmbyNotify.Plugin.csproj -c Release
sudo cp artifacts/bin/Release/netstandard2.0/EmbyNotify.Plugin.dll /var/lib/emby/plugins/
sudo systemctl restart emby-server
```
