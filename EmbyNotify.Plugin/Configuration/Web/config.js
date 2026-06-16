define(['baseView'], function (BaseView) {
    'use strict';

    function View(view, params) {
        BaseView.apply(this, arguments);

        view.querySelector('.btnSend').addEventListener('click', function () {
            onSendClick(view);
        });

        view.querySelector('.btnRefreshHistory').addEventListener('click', function () {
            loadHistory(view);
        });

        view.querySelector('.btnCheckUpdate').addEventListener('click', function () {
            checkForUpdate(view);
        });

        view.querySelector('.btnInstallUpdate').addEventListener('click', function () {
            installUpdate(view);
        });

        loadHistory(view);
    }

    View.prototype = Object.create(BaseView.prototype);
    View.prototype.constructor = View;

    function showStatus(view, msg, isError) {
        var el = view.querySelector('.notifyStatus');
        if (!el) return;
        el.textContent = msg;
        el.style.display = 'block';
        el.style.background = isError ? 'rgba(180,30,30,0.25)' : 'rgba(30,140,30,0.25)';
        el.style.color      = isError ? '#ff6b6b' : '#7ddc7d';
        el.style.border     = isError ? '1px solid rgba(180,30,30,0.5)' : '1px solid rgba(30,140,30,0.5)';
    }

    function onSendClick(view) {
        var header  = view.querySelector('.txtHeader').value.trim() || 'Announcement';
        var text    = view.querySelector('.txtMessage').value.trim();
        var seconds = parseInt(view.querySelector('.txtTimeout').value, 10) || 0;

        if (!text) {
            showStatus(view, 'Please enter a message before sending.', true);
            return;
        }

        showStatus(view, 'Sending…', false);
        var btn = view.querySelector('.btnSend');
        if (btn) btn.disabled = true;

        ApiClient.ajax({
            type: 'POST',
            url: ApiClient.getUrl('EmbyNotify/Send'),
            data: JSON.stringify({ Header: header, Text: text, TimeoutMs: seconds * 1000 }),
            contentType: 'application/json',
            dataType: 'json'
        }).then(function (result) {
            if (btn) btn.disabled = false;
            showStatus(view, result.Status || 'Sent.', !!result.Error);
            view.querySelector('.txtMessage').value = '';
            setTimeout(function () { loadHistory(view); }, 400);
        }, function (err) {
            if (btn) btn.disabled = false;
            showStatus(view, 'Error ' + (err.status || '') + ': ' + (err.statusText || JSON.stringify(err)), true);
        });
    }

    function timeAgo(isoStr) {
        var diff = Math.floor((Date.now() - new Date(isoStr).getTime()) / 1000);
        if (diff < 60)   return diff + 's ago';
        if (diff < 3600) return Math.floor(diff / 60) + 'm ago';
        if (diff < 86400) return Math.floor(diff / 3600) + 'h ago';
        return Math.floor(diff / 86400) + 'd ago';
    }

    function esc(str) {
        return String(str || '')
            .replace(/&/g, '&amp;').replace(/</g, '&lt;')
            .replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    }

    function loadHistory(view) {
        ApiClient.ajax({
            type: 'GET',
            url: ApiClient.getUrl('EmbyNotify/Notifications'),
            dataType: 'json'
        }).then(function (data) {
            renderHistory(view, data);
        }, function () {
            var el = view.querySelector('.historyList');
            if (el) el.innerHTML = '<p style="opacity:.4;font-size:.85em;">Could not load history.</p>';
        });
    }

    function renderHistory(view, data) {
        var el = view.querySelector('.historyList');
        if (!el) return;

        var active = (data || []).filter(function (n) { return n.Active; });

        if (active.length === 0) {
            el.innerHTML = '<p style="opacity:.35;font-size:.85em;margin:0;">No active notifications.</p>';
            return;
        }

        el.innerHTML = '';
        active.forEach(function (n) {
            var deliveries  = n.Deliveries || {};
            var delivered   = Object.keys(deliveries);

            var badgeHtml = '';
            delivered.forEach(function (uid) {
                var rec = deliveries[uid];
                badgeHtml += '<span style="display:inline-block;font-size:.72em;padding:.2em .55em;border-radius:99px;'
                    + 'background:rgba(30,140,30,0.2);color:#7ddc7d;border:1px solid rgba(30,140,30,0.35);margin:.15em;" '
                    + 'title="' + esc(timeAgo(rec.DeliveredAt)) + '">'
                    + esc(rec.Username) + ' ✓</span>';
            });

            if (delivered.length === 0) {
                badgeHtml = '<span style="display:inline-block;font-size:.72em;padding:.2em .55em;border-radius:99px;'
                    + 'background:rgba(180,140,0,0.18);color:#e0b830;border:1px solid rgba(180,140,0,0.35);margin:.15em;">'
                    + 'No deliveries yet</span>';
            } else {
                badgeHtml += '<span style="display:inline-block;font-size:.72em;padding:.2em .55em;border-radius:99px;'
                    + 'background:rgba(180,140,0,0.18);color:#e0b830;border:1px solid rgba(180,140,0,0.35);margin:.15em;">'
                    + 'Pending offline users</span>';
            }

            var item = document.createElement('div');
            item.style.cssText = 'border:1px solid rgba(255,255,255,0.08);border-radius:6px;padding:.75em 1em;margin-bottom:.6em;background:rgba(0,0,0,0.2);';
            item.innerHTML =
                '<div style="display:flex;justify-content:space-between;align-items:flex-start;margin-bottom:.3em;">'
                + '<span style="font-weight:600;font-size:.95em;">' + esc(n.Header) + '</span>'
                + '<span style="font-size:.75em;opacity:.4;">' + esc(timeAgo(n.CreatedAt)) + '</span>'
                + '</div>'
                + '<div style="font-size:.88em;opacity:.75;margin-bottom:.5em;">' + esc(n.Text) + '</div>'
                + '<div style="margin-bottom:.5em;">' + badgeHtml + '</div>'
                + '<button class="dismissBtn" data-id="' + esc(n.Id) + '" '
                + 'style="font-size:.75em;color:rgba(255,255,255,0.3);background:none;border:1px solid rgba(255,255,255,0.1);'
                + 'border-radius:4px;padding:.2em .6em;cursor:pointer;">Dismiss</button>';

            el.appendChild(item);
        });

        el.querySelectorAll('.dismissBtn').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var id = btn.getAttribute('data-id');
                btn.disabled = true;
                btn.textContent = '…';
                ApiClient.ajax({
                    type: 'DELETE',
                    url: ApiClient.getUrl('EmbyNotify/Notifications/' + id)
                }).then(function () {
                    loadHistory(view);
                });
            });
        });
    }

    function showUpdateStatus(view, msg, isError) {
        var el = view.querySelector('.updateStatus');
        if (!el) return;
        el.textContent = msg;
        el.style.display = 'block';
        el.style.background = isError ? 'rgba(180,30,30,0.25)' : 'rgba(30,140,30,0.25)';
        el.style.color      = isError ? '#ff6b6b' : '#7ddc7d';
        el.style.border     = isError ? '1px solid rgba(180,30,30,0.5)' : '1px solid rgba(30,140,30,0.5)';
    }

    function checkForUpdate(view) {
        var btn = view.querySelector('.btnCheckUpdate');
        if (btn) btn.disabled = true;
        showUpdateStatus(view, 'Checking…', false);

        ApiClient.ajax({
            type: 'POST',
            url: ApiClient.getUrl('EmbyNotify/CheckUpdate'),
            dataType: 'json'
        }).then(function (result) {
            if (btn) btn.disabled = false;
            if (result.Error) {
                showUpdateStatus(view, 'Check failed: ' + result.Error, true);
                return;
            }
            if (result.UpdateAvailable) {
                showUpdateStatus(view, 'Update available: v' + result.LatestVersion + ' (current: v' + result.CurrentVersion + ')', false);
                var installBtn = view.querySelector('.btnInstallUpdate');
                if (installBtn) installBtn.style.display = '';
            } else {
                showUpdateStatus(view, 'Up to date (v' + result.CurrentVersion + ')', false);
            }
        }, function (err) {
            if (btn) btn.disabled = false;
            showUpdateStatus(view, 'Check error: ' + (err.statusText || JSON.stringify(err)), true);
        });
    }

    function installUpdate(view) {
        var btn = view.querySelector('.btnInstallUpdate');
        if (btn) btn.disabled = true;
        showUpdateStatus(view, 'Downloading and installing…', false);

        ApiClient.ajax({
            type: 'POST',
            url: ApiClient.getUrl('EmbyNotify/InstallUpdate'),
            dataType: 'json'
        }).then(function (result) {
            if (btn) btn.disabled = false;
            showUpdateStatus(view, result.Message, !result.Success);
            if (result.Success && btn) btn.style.display = 'none';
        }, function (err) {
            if (btn) btn.disabled = false;
            showUpdateStatus(view, 'Install error: ' + (err.statusText || JSON.stringify(err)), true);
        });
    }

    return View;
});
