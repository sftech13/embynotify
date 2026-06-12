# Changelog

All notable changes to EmbyNotify are listed here, newest first.

---

## v1.0.0

Initial release.

- Config page with Header, Message, and Auto-dismiss fields
- **Send to All Sessions** button broadcasts a `MessageCommand` popup to every active Emby session via `ISessionManager`
- Status feedback shows how many sessions received the message
- `POST /EmbyNotify/Send` API endpoint for scripted/external use (admin auth required)
- Auto-dismiss timer: `0` = stays until dismissed, any value in seconds auto-closes the popup
