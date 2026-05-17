// Startup crash reporting.
//
// The Blazor ErrorBoundary handles in-app C# exceptions and routes them to
// the BugReportDialog. This file covers the complementary case: startup
// failures (WASM instantiation errors, AOT runtime crashes, unhandled promise
// rejections) where the .NET runtime never fully initialises and the only UI
// we have is the #blazor-error-ui bar.
//
// When a startup crash is detected we:
//   1. Inject the actual error message into the bar so users can see it.
//   2. Add a "Copy error details" button (clipboard).
//   3. Add a "Report this bug" link that pre-fills a GitHub issue with the
//      error message, stack trace, user agent, and timestamp.

(function () {
    'use strict';

    var injected = false;

    function buildReport(message, stack) {
        var lines = [
            '**User agent:** ' + navigator.userAgent,
            '**Time (UTC):** ' + new Date().toISOString(),
            '**URL:** ' + location.href,
            '',
            '## Error message',
            '```',
            message,
            '```',
        ];
        if (stack) {
            lines.push('', '## Stack trace', '```', stack, '```');
        }
        lines.push(
            '',
            '## Description',
            '<!-- Please describe what you were doing when the crash occurred. -->'
        );
        return lines.join('\n');
    }

    function buildIssueUrl(message, report) {
        var title = '[Crash] Startup error: ' + message.slice(0, 80);
        // GitHub caps issue body at ~65 535 chars in the URL; truncate generously.
        var body = report.length > 4000 ? report.slice(0, 4000) + '\n\n*(truncated)*' : report;
        return (
            'https://github.com/codemonkey85/PKMDS-Blazor/issues/new' +
            '?title=' + encodeURIComponent(title) +
            '&body=' + encodeURIComponent(body) +
            '&labels=' + encodeURIComponent('bug,crash')
        );
    }

    function injectErrorDetails(message, stack) {
        if (injected) return;
        injected = true;

        var ui = document.getElementById('blazor-error-ui');
        if (!ui) return;

        var report = buildReport(message, stack);
        var issueUrl = buildIssueUrl(message, report);

        // Error message line.
        var msgEl = document.createElement('p');
        msgEl.style.cssText = 'margin: 0.4rem 0 0; font-size: 0.8rem; font-family: monospace; word-break: break-all;';
        msgEl.textContent = message;
        ui.insertBefore(msgEl, ui.firstChild.nextSibling);

        // Collapsible stack trace.
        if (stack) {
            var details = document.createElement('details');
            details.style.cssText = 'margin-top: 0.4rem; font-size: 0.75rem;';
            var summary = document.createElement('summary');
            summary.textContent = 'Stack trace';
            summary.style.cursor = 'pointer';
            var pre = document.createElement('pre');
            pre.style.cssText = 'overflow: auto; max-height: 120px; background: rgba(0,0,0,0.08); padding: 0.4rem; border-radius: 3px; margin: 0.3rem 0 0; font-size: 0.7rem; white-space: pre-wrap; word-break: break-all;';
            pre.textContent = stack;
            details.appendChild(summary);
            details.appendChild(pre);
            ui.insertBefore(details, msgEl.nextSibling);
        }

        // Button row.
        var row = document.createElement('div');
        row.style.cssText = 'margin-top: 0.5rem; display: flex; gap: 0.5rem; flex-wrap: wrap; align-items: center;';

        var copyBtn = document.createElement('button');
        copyBtn.textContent = 'Copy error details';
        copyBtn.style.cssText = 'padding: 0.2rem 0.6rem; font-size: 0.78rem; cursor: pointer; border: 1px solid #888; background: #fff; border-radius: 3px;';
        copyBtn.addEventListener('click', function () {
            var done = function () {
                copyBtn.textContent = 'Copied!';
                setTimeout(function () { copyBtn.textContent = 'Copy error details'; }, 2000);
            };
            if (navigator.clipboard && navigator.clipboard.writeText) {
                navigator.clipboard.writeText(report).then(done).catch(done);
            } else {
                var ta = document.createElement('textarea');
                ta.value = report;
                ta.style.position = 'fixed';
                ta.style.opacity = '0';
                document.body.appendChild(ta);
                ta.focus();
                ta.select();
                try { document.execCommand('copy'); } catch (_) {}
                document.body.removeChild(ta);
                done();
            }
        });

        var reportLink = document.createElement('a');
        reportLink.href = issueUrl;
        reportLink.target = '_blank';
        reportLink.rel = 'noopener noreferrer';
        reportLink.textContent = 'Report this bug';
        reportLink.style.cssText = 'padding: 0.2rem 0.6rem; font-size: 0.78rem; background: #b91c1c; color: #fff; border-radius: 3px; text-decoration: none;';

        row.appendChild(copyBtn);
        row.appendChild(reportLink);

        var insertAfter = stack ? details : msgEl;
        ui.insertBefore(row, insertAfter.nextSibling);
    }

    // Unhandled promise rejections — covers WASM / AOT / Blazor bootstrap failures.
    window.addEventListener('unhandledrejection', function (e) {
        var reason = e.reason;
        if (!reason) return;
        var message = typeof reason.message === 'string' ? reason.message : String(reason);
        var stack = typeof reason.stack === 'string' ? reason.stack : null;
        injectErrorDetails(message, stack);
    });

    // Synchronous JS errors.
    window.addEventListener('error', function (e) {
        if (!e.message) return;
        var stack = e.error && typeof e.error.stack === 'string' ? e.error.stack : null;
        injectErrorDetails(e.message, stack);
    });
})();
