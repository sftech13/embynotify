# Changelog

All notable changes to EmbyNotify are listed here, newest first.

---

## v1.5.0

- **Notification history** added to the plugin config page — shows all active notifications with per-user delivery badges (green ✓ delivered, yellow pending) and a Dismiss button
- **Plugin Updates section** moved to the top of the config page, styled with a blue-accented card and Emby theme buttons
- History auto-loads on page open and refreshes after each send
- Message box clears automatically after a successful send

## v1.4.0

- Fixed: notification store errors no longer block the live send — store operations are now best-effort
- Fixed: sessions without a UserId were incorrectly skipped during broadcast
- Increased deferred delivery delay to 10 seconds to allow client UI to fully initialize before popup is sent
- Removed hardcoded "Announcement:" prefix from toast message body

## v1.3.0

- **Deferred delivery** — notifications are persisted to `embynotify-notifications.json` and automatically sent to users when they open Emby, even if they were offline when the message was broadcast
- Per-user delivery tracking: active sessions marked delivered immediately; offline users receive the notification on next login via `ISessionManager.SessionStarted`
- New endpoints: `GET /EmbyNotify/Notifications` (list with delivery status), `DELETE /EmbyNotify/Notifications/{id}` (dismiss)
- Standalone `emby.html` send page with notification history, delivery badges, and dismiss controls

## v1.2.0

- **Self-update mechanism** — `POST /EmbyNotify/CheckUpdate` polls GitHub Releases API and compares versions; `POST /EmbyNotify/InstallUpdate` downloads the latest DLL and atomically replaces the running plugin (temp → backup → move) with rollback on failure
- Calls `NotifyPendingRestart()` via reflection after install so Emby shows the restart prompt
- Check/Install Update buttons added to the plugin config page

## v1.1.0

- Rewrote config page for Emby 4.9 view controller architecture (`is="emby-scroller"`, AMD `define(['baseView'], ...)`)
- Fixed config page rendering behind plugin list (missing `class="view"` on root element)
- Fixed Send button doing nothing — moved all JS to a separate `config.js` AMD module
- Added plugin logo (256×256 PNG, `IHasThumbImage`)
- GitHub Actions release workflow with automatic `manifest.json` updates

## v1.0.0

Initial release.

- Config page with Header, Message, and Auto-dismiss fields
- **Send to All Sessions** button broadcasts a `MessageCommand` popup to every active Emby session via `ISessionManager`
- Status feedback shows how many sessions received the message
- `POST /EmbyNotify/Send` API endpoint for scripted/external use (admin auth required)
- Auto-dismiss timer: `0` = stays until dismissed, any value in seconds auto-closes the popup
