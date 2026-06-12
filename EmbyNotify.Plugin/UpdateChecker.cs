using System;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace EmbyNotify.Plugin
{
    public class UpdateCheckResult
    {
        public bool UpdateAvailable { get; set; }
        public string CurrentVersion { get; set; }
        public string LatestVersion { get; set; }
        public string DownloadUrl { get; set; }
        public string ReleaseNotes { get; set; }
        public string Error { get; set; }
    }

    public class InstallUpdateResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public static class UpdateChecker
    {
        private const string ApiUrl      = "https://api.github.com/repos/sftech13/embynotify/releases/latest";
        private const string DllAssetName = "EmbyNotify.Plugin.dll";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);

        private static UpdateCheckResult _cached;
        private static DateTime _cacheTime = DateTime.MinValue;
        private static readonly object _lock = new object();

        public static void InvalidateCache()
        {
            lock (_lock) { _cached = null; _cacheTime = DateTime.MinValue; }
        }

        public static async Task<UpdateCheckResult> CheckAsync()
        {
            lock (_lock)
            {
                if (_cached != null && (DateTime.UtcNow - _cacheTime) < CacheTtl)
                    return _cached;
            }

            var currentVersion = typeof(Plugin).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? typeof(Plugin).Assembly.GetName().Version?.ToString()
                ?? "0.0.0";

            UpdateCheckResult result;
            try
            {
                using (var http = new HttpClient())
                {
                    http.DefaultRequestHeaders.UserAgent.ParseAdd("EmbyNotify-Plugin/1.0");
                    http.Timeout = TimeSpan.FromSeconds(10);
                    var json = await http.GetStringAsync(ApiUrl).ConfigureAwait(false);
                    result = ParseAndCompare(currentVersion, json);
                }
            }
            catch (Exception ex)
            {
                result = new UpdateCheckResult { CurrentVersion = currentVersion, Error = ex.Message };
            }

            lock (_lock) { _cached = result; _cacheTime = DateTime.UtcNow; }
            return result;
        }

        private static UpdateCheckResult ParseAndCompare(string currentVersion, string json)
        {
            var result = new UpdateCheckResult { CurrentVersion = currentVersion };
            try
            {
                using (var doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
                    JsonElement el;
                    string tagName = null;
                    if (root.TryGetProperty("tag_name", out el)) tagName = el.GetString();
                    if (root.TryGetProperty("body", out el))     result.ReleaseNotes = el.GetString();

                    if (string.IsNullOrEmpty(tagName)) { result.Error = "No tag in release data"; return result; }

                    var versionStr = tagName.TrimStart('v');
                    result.LatestVersion = versionStr;

                    JsonElement assets;
                    if (root.TryGetProperty("assets", out assets))
                    {
                        foreach (var asset in assets.EnumerateArray())
                        {
                            JsonElement nameEl, urlEl;
                            if (asset.TryGetProperty("name", out nameEl) &&
                                string.Equals(nameEl.GetString(), DllAssetName, StringComparison.OrdinalIgnoreCase) &&
                                asset.TryGetProperty("browser_download_url", out urlEl))
                            {
                                result.DownloadUrl = urlEl.GetString();
                                break;
                            }
                        }
                    }

                    Version current, latest;
                    if (Version.TryParse(Normalize(currentVersion), out current) &&
                        Version.TryParse(Normalize(versionStr), out latest))
                    {
                        result.UpdateAvailable = latest > current;
                    }
                }
            }
            catch (Exception ex) { result.Error = ex.Message; }
            return result;
        }

        private static string Normalize(string v)
        {
            if (v == null) return "0.0";
            var dash = v.IndexOf('-');
            if (dash >= 0) v = v.Substring(0, dash);
            return v.Split('.').Length < 2 ? v + ".0" : v;
        }
    }
}
