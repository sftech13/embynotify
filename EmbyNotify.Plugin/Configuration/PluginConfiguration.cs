using MediaBrowser.Model.Plugins;

namespace EmbyNotify.Plugin.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public string DefaultHeader { get; set; } = "Announcement";
        public int DefaultTimeoutMs { get; set; } = 0;
    }
}
