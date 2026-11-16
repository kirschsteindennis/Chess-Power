// Reports errors of the browser to the server, so they can appear in the log.
// It's loaded first, so that it also sees errors of the scripts that are loaded afterwards
(function () {
    "use strict";

    var endpoint = "/client-errors";
    var maxReportsPerPage = 5;          // a broken page can fail in a loop
    var maxMessageLength = 500;
    var sent = 0;

    // The page was created by the server inside a trace. Sending the same trace id with the report
    // connects the report with everything the server logged for this page
    function traceparent() {
        var meta = document.querySelector('meta[name="traceparent"]');
        return meta ? meta.getAttribute("content") : null;
    }

    // Only the path of an address: a query could hold a token or an id
    function pathOf(address) {
        if (!address) {
            return "";
        }

        var text = String(address);

        try {
            return new URL(text, "http://placeholder.invalid").pathname;
        } catch (error) {
            return text.split("?")[0].split("#")[0];
        }
    }

    // Messages often contain whole addresses, for example "Failed to fetch https://host/x?token=abc"
    function withoutQueries(text) {
        return text.replace(/(https?:\/\/[^\s?#]+)[?#]\S*/gi, "$1");
    }

    function describe(value) {
        if (value && value.message) {
            var code = value.errorCode !== undefined ? "code" + value.errorCode + ": " : "";
            return code + value.message;
        }

        return String(value);
    }

    function send(kind, message, source, line, column) {
        if (sent >= maxReportsPerPage) {
            return;
        }

        sent++;

        var text = message === undefined || message === null ? "" : String(message);

        var report = {
            kind: kind,
            message: withoutQueries(text).slice(0, maxMessageLength),
            source: pathOf(source),
            line: Number.isInteger(line) ? line : null,
            column: Number.isInteger(column) ? column : null,
            page: location.pathname
        };

        var headers = { "Content-Type": "application/json" };
        var parent = traceparent();

        if (parent) {
            headers["traceparent"] = parent;
        }

        try {
            // keepalive: the request also finishes when the page is closed right after the error
            fetch(endpoint, { method: "POST", headers: headers, body: JSON.stringify(report), keepalive: true })
                .catch(function () { /* reporting an error must never cause an error itself */ });
        } catch (error) {
            /* the same */
        }
    }

    window.addEventListener("error", function (event) {
        send("error", event.message, event.filename, event.lineno, event.colno);
    });

    window.addEventListener("unhandledrejection", function (event) {
        send("unhandledrejection", describe(event.reason));
    });

    // Blazor says when the connection to the server could not be restored.
    // Capture phase: it also works if the event does not bubble
    document.addEventListener("components-reconnect-state-changed", function (event) {
        var state = event.detail && event.detail.state;

        if (state === "failed") {
            send("circuit-failed", "The connection to the server could not be restored");
        } else if (state === "rejected") {
            send("circuit-rejected", "The server refused to resume the session");
        }
    }, true);

    window.chessTeams = window.chessTeams || {};

    // For other scripts: window.chessTeams.reportError("teams-sdk", error)
    window.chessTeams.reportError = function (kind, error) {
        send(kind, describe(error));
    };
})();