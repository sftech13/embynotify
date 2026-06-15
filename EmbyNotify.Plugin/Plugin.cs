using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EmbyNotify.Plugin.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Session;

namespace EmbyNotify.Plugin
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages, IHasThumbImage
    {
        private readonly IServerApplicationHost _applicationHost;
        private readonly ILogger _logger;

        public static Plugin Instance { get; private set; }
        public NotificationStore Store { get; private set; }

        public Plugin(
            IApplicationPaths appPaths,
            IXmlSerializer xmlSerializer,
            IServerApplicationHost applicationHost,
            ILogManager logManager)
            : base(appPaths, xmlSerializer)
        {
            Instance = this;
            _applicationHost = applicationHost;
            _logger = logManager.GetLogger(nameof(Plugin));
            Store = new NotificationStore(appPaths, logManager);
        }

        public override string Name => "EmbyNotify";
        public override Guid Id => new Guid("3c8f1e2a-4b7d-4e9f-a0c5-d6e7f8091b2c");
        public override string Description => "Broadcast custom messages to all active Emby sessions from the plugin config page.";

        public Stream GetThumbImage() =>
            GetType().Assembly.GetManifestResourceStream("EmbyNotify.Plugin.thumb.png");

        public ImageFormat ThumbImageFormat => ImageFormat.Png;

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "EmbyNotify",
                    EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.Web.config.html",
                    IsMainConfigPage = true
                },
                new PluginPageInfo
                {
                    Name = "embynotifyconfig",
                    EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.Web.config.js"
                }
            };
        }

        internal async Task<BroadcastResult> BroadcastAsync(string header, string text, int timeoutMs)
        {
            var result = new BroadcastResult();
            try
            {
                var sessionManager = _applicationHost.Resolve<ISessionManager>();
                if (sessionManager == null)
                {
                    result.Error = "Session manager not available";
                    return result;
                }

                var sessions = sessionManager.Sessions;
                if (sessions == null)
                {
                    result.Error = "No session data";
                    return result;
                }

                // Record the notification — delivery tracking starts here
                var normalizedHeader = string.IsNullOrWhiteSpace(header) ? "Announcement" : header;
                var notification = Store.Add(normalizedHeader, text ?? "", timeoutMs);
                result.NotificationId = notification.Id;

                var command = new MessageCommand
                {
                    Header    = normalizedHeader,
                    Text      = text ?? "",
                    TimeoutMs = timeoutMs
                };

                foreach (var session in sessions)
                {
                    if (string.IsNullOrEmpty(session.UserId)) continue;
                    try
                    {
                        await sessionManager.SendMessageCommand(session.Id, session.Id, command, CancellationToken.None).ConfigureAwait(false);
                        Store.MarkDelivered(notification.Id, session.UserId, session.UserName ?? session.UserId);
                        result.SessionsMessaged++;
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug("EmbyNotify: failed to message session {0}: {1}", session.Id, ex.Message);
                        result.SessionsFailed++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("EmbyNotify BroadcastAsync error: {0}", ex.Message);
                result.Error = ex.Message;
            }

            return result;
        }

        internal async Task<InstallUpdateResult> InstallUpdateAsync()
        {
            var result = new InstallUpdateResult();
            try
            {
                UpdateChecker.InvalidateCache();
                var check = await UpdateChecker.CheckAsync().ConfigureAwait(false);

                if (!check.UpdateAvailable)
                {
                    result.Message = "No update available.";
                    return result;
                }

                if (string.IsNullOrEmpty(check.DownloadUrl))
                {
                    result.Message = "No download URL found in release.";
                    return result;
                }

                var currentDll = typeof(Plugin).Assembly.Location;
                if (string.IsNullOrEmpty(currentDll) || !File.Exists(currentDll))
                    currentDll = Path.Combine(ApplicationPaths.PluginsPath, "EmbyNotify.Plugin.dll");

                if (!File.Exists(currentDll))
                {
                    result.Message = "Could not locate plugin DLL.";
                    return result;
                }

                var tempPath = currentDll + ".temp";
                var bakPath  = currentDll + ".bak";

                byte[] dllBytes;
                using (var http = new HttpClient())
                {
                    http.DefaultRequestHeaders.UserAgent.ParseAdd("EmbyNotify-Plugin/1.0");
                    http.Timeout = TimeSpan.FromSeconds(60);
                    dllBytes = await http.GetByteArrayAsync(check.DownloadUrl).ConfigureAwait(false);
                }

                if (dllBytes.Length < 1024)
                {
                    result.Message = $"Downloaded file too small ({dllBytes.Length} bytes). Aborting.";
                    return result;
                }

                File.WriteAllBytes(tempPath, dllBytes);
                try
                {
                    if (File.Exists(bakPath)) File.Delete(bakPath);
                    File.Move(currentDll, bakPath);
                    File.Move(tempPath, currentDll);
                    try { File.Delete(bakPath); } catch { }
                }
                catch
                {
                    try { if (File.Exists(bakPath) && !File.Exists(currentDll)) File.Move(bakPath, currentDll); } catch { }
                    try { File.Delete(tempPath); } catch { }
                    throw;
                }

                UpdateChecker.InvalidateCache();

                try
                {
                    var notifyMethod = _applicationHost.GetType().GetMethod(
                        "NotifyPendingRestart",
                        BindingFlags.Public | BindingFlags.Instance);
                    notifyMethod?.Invoke(_applicationHost, null);
                }
                catch (Exception ex)
                {
                    _logger.Warn("EmbyNotify: NotifyPendingRestart failed: {0}", ex.Message);
                }

                result.Success = true;
                result.Message = $"Updated to v{check.LatestVersion} ({dllBytes.Length:N0} bytes). Restart Emby to apply.";
            }
            catch (Exception ex)
            {
                _logger.Error("EmbyNotify InstallUpdate failed: {0}", ex.Message);
                result.Message = "Install failed: " + ex.Message;
            }
            return result;
        }
    }

    public class BroadcastResult
    {
        public int SessionsMessaged { get; set; }
        public int SessionsFailed { get; set; }
        public string NotificationId { get; set; }
        public string Error { get; set; }
    }
}
