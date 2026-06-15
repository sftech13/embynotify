using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Logging;

namespace EmbyNotify.Plugin
{
    public class DeliveryRecord
    {
        public string Username { get; set; }
        public DateTime DeliveredAt { get; set; }
    }

    public class PendingNotification
    {
        public string Id { get; set; }
        public string Header { get; set; }
        public string Text { get; set; }
        public int TimeoutMs { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool Active { get; set; }
        public Dictionary<string, DeliveryRecord> Deliveries { get; set; }

        public PendingNotification()
        {
            Id = Guid.NewGuid().ToString("N");
            CreatedAt = DateTime.UtcNow;
            Active = true;
            Deliveries = new Dictionary<string, DeliveryRecord>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public class NotificationStore
    {
        private readonly string _filePath;
        private readonly ILogger _logger;
        private readonly object _lock = new object();
        private List<PendingNotification> _notifications;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public NotificationStore(IApplicationPaths appPaths, ILogManager logManager)
        {
            _filePath = Path.Combine(appPaths.DataPath, "embynotify-notifications.json");
            _logger = logManager.GetLogger(nameof(NotificationStore));
            _notifications = Load();
        }

        private List<PendingNotification> Load()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    return JsonSerializer.Deserialize<List<PendingNotification>>(json, _jsonOpts)
                           ?? new List<PendingNotification>();
                }
            }
            catch (Exception ex)
            {
                _logger.Error("EmbyNotify: failed to load notification store: {0}", ex.Message);
            }
            return new List<PendingNotification>();
        }

        private void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(_notifications, _jsonOpts);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                _logger.Error("EmbyNotify: failed to save notification store: {0}", ex.Message);
            }
        }

        public PendingNotification Add(string header, string text, int timeoutMs)
        {
            var notification = new PendingNotification
            {
                Header    = header,
                Text      = text,
                TimeoutMs = timeoutMs
            };
            lock (_lock)
            {
                _notifications.Insert(0, notification);
                if (_notifications.Count > 100)
                    _notifications.RemoveRange(100, _notifications.Count - 100);
                Save();
            }
            return notification;
        }

        public void MarkDelivered(string notificationId, string userId, string username)
        {
            lock (_lock)
            {
                var n = _notifications.Find(x => x.Id == notificationId);
                if (n == null) return;
                n.Deliveries[userId] = new DeliveryRecord
                {
                    Username    = username ?? userId,
                    DeliveredAt = DateTime.UtcNow
                };
                Save();
            }
        }

        public void Dismiss(string notificationId)
        {
            lock (_lock)
            {
                var n = _notifications.Find(x => x.Id == notificationId);
                if (n == null) return;
                n.Active = false;
                Save();
            }
        }

        public List<PendingNotification> GetAll()
        {
            lock (_lock)
                return new List<PendingNotification>(_notifications);
        }

        public List<PendingNotification> GetActiveUndeliveredFor(string userId)
        {
            lock (_lock)
                return _notifications.FindAll(n =>
                    n.Active && !n.Deliveries.ContainsKey(userId));
        }
    }
}
