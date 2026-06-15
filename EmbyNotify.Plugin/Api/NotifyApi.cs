using System.Collections.Generic;
using System.Threading.Tasks;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Services;

namespace EmbyNotify.Plugin.Api
{
    [Authenticated(Roles = "Admin")]
    [Route("/EmbyNotify/Send", "POST", Summary = "Broadcast a message to all active Emby sessions")]
    public class SendNotification : IReturn<SendNotificationResult>
    {
        public string Header { get; set; }
        public string Text { get; set; }
        public int TimeoutMs { get; set; }
    }

    public class SendNotificationResult
    {
        public int SessionsMessaged { get; set; }
        public int SessionsFailed { get; set; }
        public string Status { get; set; }
        public string Error { get; set; }
    }

    [Authenticated(Roles = "Admin")]
    [Route("/EmbyNotify/CheckUpdate", "POST", Summary = "Check GitHub for the latest plugin release")]
    public class CheckUpdate : IReturn<UpdateCheckResult> { }

    [Authenticated(Roles = "Admin")]
    [Route("/EmbyNotify/InstallUpdate", "POST", Summary = "Download and atomically install the latest plugin release")]
    public class InstallUpdate : IReturn<InstallUpdateResult> { }

    [Authenticated(Roles = "Admin")]
    [Route("/EmbyNotify/Notifications", "GET", Summary = "List all notifications with per-user delivery status")]
    public class GetNotifications : IReturn<List<PendingNotification>> { }

    [Authenticated(Roles = "Admin")]
    [Route("/EmbyNotify/Notifications/{Id}", "DELETE", Summary = "Dismiss a notification")]
    public class DismissNotification : IReturn<object>
    {
        public string Id { get; set; }
    }

    public class NotifyApi : IService, IRequiresRequest
    {
        public IRequest Request { get; set; }

        public async Task<object> Post(SendNotification request)
        {
            var result = await Plugin.Instance.BroadcastAsync(
                request.Header,
                request.Text,
                request.TimeoutMs).ConfigureAwait(false);

            return new SendNotificationResult
            {
                SessionsMessaged = result.SessionsMessaged,
                SessionsFailed   = result.SessionsFailed,
                Status = result.Error != null
                    ? $"Error: {result.Error}"
                    : result.SessionsMessaged > 0
                        ? $"Sent to {result.SessionsMessaged} session(s)"
                        : "No active sessions found",
                Error = result.Error
            };
        }

        public async Task<object> Post(CheckUpdate request)
        {
            UpdateChecker.InvalidateCache();
            return await UpdateChecker.CheckAsync().ConfigureAwait(false);
        }

        public async Task<object> Post(InstallUpdate request)
        {
            return await Plugin.Instance.InstallUpdateAsync().ConfigureAwait(false);
        }

        public object Get(GetNotifications request)
        {
            return Plugin.Instance.Store.GetAll();
        }

        public object Delete(DismissNotification request)
        {
            Plugin.Instance.Store.Dismiss(request.Id);
            return new { Success = true };
        }
    }
}
