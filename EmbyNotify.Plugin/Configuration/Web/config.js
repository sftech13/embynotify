define(['baseView'], function (BaseView) {
    'use strict';

    function View(view, params) {
        BaseView.apply(this, arguments);

        view.querySelector('.btnSend').addEventListener('click', function () {
            onSendClick(view);
        });

        view.querySelector('.btnCheckUpdate').addEventListener('click', function () {
            checkForUpdate(view);
        });

        view.querySelector('.btnInstallUpdate').addEventListener('click', function () {
            installUpdate(view);
        });
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
        }, function (err) {
            if (btn) btn.disabled = false;
            showStatus(view, 'Error ' + (err.status || '') + ': ' + (err.statusText || JSON.stringify(err)), true);
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
