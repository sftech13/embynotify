using System;
using System.Collections.Generic;
using System.IO;
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

                var command = new MessageCommand
                {
                    Header = string.IsNullOrWhiteSpace(header) ? "Announcement" : header,
                    Text = text ?? "",
                    TimeoutMs = timeoutMs
                };

                foreach (var session in sessions)
                {
                    try
                    {
                        await sessionManager.SendMessageCommand(session.Id, session.Id, command, CancellationToken.None).ConfigureAwait(false);
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
    }

    public class BroadcastResult
    {
        public int SessionsMessaged { get; set; }
        public int SessionsFailed { get; set; }
        public string Error { get; set; }
    }
}
