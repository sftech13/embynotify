<p align="center">
  <h1 align="center">EmbyNotify</h1>
</p>

<p align="center">
  A lightweight Emby Server plugin for broadcasting custom popup messages to all active sessions — directly from the plugin config page.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Emby-4.8%2B-52B54B?style=flat-square&logo=emby" alt="Emby 4.8+" />
  <img src="https://img.shields.io/badge/.NET-Standard%202.0-512BD4?style=flat-square" alt=".NET Standard 2.0" />
  <img src="https://img.shields.io/badge/License-MIT-blue?style=flat-square" alt="MIT License" />
</p>

---

## What It Does

EmbyNotify lets you send a popup notification to every user currently active in Emby — from a simple config page, no scripting required.

You set the header, message body, and optional auto-dismiss timer. Hit **Send to All Sessions** and every connected client (web, Emby Theater, mobile, TV) gets the popup immediately.

---

## Installation

### Step 1 — Get the DLL

**Option A: Download a release**

Download `EmbyNotify.Plugin.dll` from the [latest release](../../releases/latest).

**Option B: Build from source**

Requires .NET SDK 8.0+.

```bash
git clone https://github.com/sftech13/EmbyNotify.git
cd EmbyNotify
dotnet build EmbyNotify.Plugin/EmbyNotify.Plugin.csproj -c Release
# Output: artifacts/bin/Release/netstandard2.0/EmbyNotify.Plugin.dll
```

### Step 2 — Install

Copy the DLL to your Emby plugins directory and restart Emby.

**Linux (systemd)**
```bash
sudo cp EmbyNotify.Plugin.dll /var/lib/emby/plugins/
sudo systemctl restart emby-server
```

**Docker**
```bash
docker cp EmbyNotify.Plugin.dll emby:/config/plugins/
docker restart emby
```

### Step 3 — Open the Config Page

Go to **Emby Dashboard → Plugins → EmbyNotify**.

---

## Usage

| Field | Description |
|---|---|
| **Header** | Short title shown at the top of the popup. Defaults to `Announcement`. |
| **Message** | The body text of the notification. Required. |
| **Auto-dismiss (seconds)** | `0` = stays until the user dismisses it. Any other value auto-closes after that many seconds. |

Click **Send to All Sessions**. The status line below the button confirms how many sessions received the message.

---

## API

All endpoints require admin authentication (`?api_key=<key>` or a valid session token).

### `POST /EmbyNotify/Send`

Broadcasts a message to all active Emby sessions.

**Request body (JSON):**

```json
{
  "Header": "Announcement",
  "Text": "Server maintenance starts in 10 minutes.",
  "TimeoutMs": 10000
}
```

| Field | Type | Description |
|---|---|---|
| `Header` | string | Popup title. Defaults to `Announcement` if blank. |
| `Text` | string | Message body. |
| `TimeoutMs` | int | Auto-dismiss in milliseconds. `0` = no auto-dismiss. |

**Response:**

```json
{
  "SessionsMessaged": 3,
  "SessionsFailed": 0,
  "Status": "Sent to 3 session(s)",
  "Error": null
}
```

---

## Building Releases

```bash
git tag v1.0.0
git push origin v1.0.0
```

GitHub Actions builds and publishes the release DLL automatically when a version tag is pushed.

---

## License

MIT
