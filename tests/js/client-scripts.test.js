const vm = require("vm");
const fs = require("fs");
const assert = require("assert");
const path = require("path");

// A tiny browser: only what the two scripts use
function createBrowser({ traceparent = null, pathname = "/config", fetchBehaviour = "ok" } = {}) {
    const calls = [];
    const windowListeners = {};
    const documentListeners = {};
    const window = {
        addEventListener(type, handler) { (windowListeners[type] ||= []).push(handler); },
        location: { origin: "https://app.example.com" },
    };
    window.parent = window;   // not inside an iframe by default
    const document = {
        addEventListener(type, handler, capture) { (documentListeners[type] ||= []).push({ handler, capture }); },
        querySelector(selector) {
            if (selector === 'meta[name="traceparent"]' && traceparent)
                return { getAttribute: name => (name === "content" ? traceparent : null) };
            return null;
        },
    };
    const sandbox = {
        window, document, URL, console: { warn() { }, log() { } },
        location: { pathname },
        fetch(url, options) {
            calls.push({ url, options, body: JSON.parse(options.body) });
            if (fetchBehaviour === "throws") throw new Error("sync failure");
            if (fetchBehaviour === "rejects") return Promise.reject(new Error("network down"));
            return Promise.resolve({ ok: true });
        },
    };
    vm.createContext(sandbox);
    return {
        calls, window, sandbox,
        load(file) { vm.runInContext(fs.readFileSync(path.join(__dirname, "..", "..", "src", "ChessUI.Teams", "wwwroot", file), "utf8"), sandbox); },
        fireWindow(type, event) { (windowListeners[type] || []).forEach(h => h(event)); },
        fireDocument(type, event) { (documentListeners[type] || []).forEach(l => l.handler(event)); },
        documentListeners,
    };
}

const tests = [];
const test = (name, fn) => tests.push({ name, fn });

// ---------- client-errors.js ----------
test("an error event is sent as JSON to the server", () => {
    const b = createBrowser(); b.load("client-errors.js");
    b.fireWindow("error", { message: "boom", filename: "https://app.example.com/_framework/blazor.web.js?token=abc#x", lineno: 12, colno: 5 });
    assert.strictEqual(b.calls.length, 1);
    const c = b.calls[0];
    assert.strictEqual(c.url, "/client-errors");
    assert.strictEqual(c.options.method, "POST");
    assert.strictEqual(c.options.headers["Content-Type"], "application/json");
    assert.strictEqual(c.options.keepalive, true);
    assert.deepStrictEqual(c.body, { kind: "error", message: "boom", source: "/_framework/blazor.web.js", line: 12, column: 5, page: "/config" });
});
test("the page's trace id is sent as traceparent header", () => {
    const tp = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01";
    const b = createBrowser({ traceparent: tp }); b.load("client-errors.js");
    b.fireWindow("error", { message: "boom" });
    assert.strictEqual(b.calls[0].options.headers["traceparent"], tp);
});
test("without a trace id there is no traceparent header", () => {
    const b = createBrowser(); b.load("client-errors.js");
    b.fireWindow("error", { message: "boom" });
    assert.strictEqual("traceparent" in b.calls[0].options.headers, false);
});
test("an unhandled rejection reports the message of its reason", () => {
    const b = createBrowser(); b.load("client-errors.js");
    b.fireWindow("unhandledrejection", { reason: new Error("Failed to fetch") });
    assert.strictEqual(b.calls[0].body.kind, "unhandledrejection");
    assert.strictEqual(b.calls[0].body.message, "Failed to fetch");
});
test("a Teams style error shows its error code", () => {
    const b = createBrowser(); b.load("client-errors.js");
    b.fireWindow("unhandledrejection", { reason: { errorCode: 1000, message: "Consent required" } });
    assert.strictEqual(b.calls[0].body.message, "code 1000: Consent required");
});
test("a reason that is no object is shown as text", () => {
    const b = createBrowser(); b.load("client-errors.js");
    b.fireWindow("unhandledrejection", { reason: "plain text" });
    assert.strictEqual(b.calls[0].body.message, "plain text");
});
test("at most five reports are sent per page", () => {
    const b = createBrowser(); b.load("client-errors.js");
    for (let i = 0; i < 20; i++) b.fireWindow("error", { message: "loop " + i });
    assert.strictEqual(b.calls.length, 5);
});
test("a failed connection to the server is reported once per event", () => {
    const b = createBrowser(); b.load("client-errors.js");
    b.fireDocument("components-reconnect-state-changed", { detail: { state: "failed" } });
    b.fireDocument("components-reconnect-state-changed", { detail: { state: "rejected" } });
    assert.deepStrictEqual(b.calls.map(c => c.body.kind), ["circuit-failed", "circuit-rejected"]);
});
test("the other connection states are ignored", () => {
    const b = createBrowser(); b.load("client-errors.js");
    for (const state of ["show", "hide", "retrying", "paused"])
        b.fireDocument("components-reconnect-state-changed", { detail: { state } });
    b.fireDocument("components-reconnect-state-changed", {});
    assert.strictEqual(b.calls.length, 0);
});
test("the connection listener uses the capture phase", () => {
    const b = createBrowser(); b.load("client-errors.js");
    assert.strictEqual(b.documentListeners["components-reconnect-state-changed"][0].capture, true);
});
test("the query of an address in the message is removed", () => {
    const b = createBrowser(); b.load("client-errors.js");
    b.fireWindow("error", { message: "Failed to fetch https://app.example.com/api/x?token=abc123 now" });
    assert.strictEqual(b.calls[0].body.message, "Failed to fetch https://app.example.com/api/x now");
});
test("a long message is shortened to 500 characters", () => {
    const b = createBrowser(); b.load("client-errors.js");
    b.fireWindow("error", { message: "x".repeat(3000) });
    assert.strictEqual(b.calls[0].body.message.length, 500);
});
test("line and column that are no integers become null", () => {
    const b = createBrowser(); b.load("client-errors.js");
    b.fireWindow("error", { message: "boom", lineno: 12.5, colno: undefined });
    assert.strictEqual(b.calls[0].body.line, null);
    assert.strictEqual(b.calls[0].body.column, null);
});
test("a failing request never causes an error itself", async () => {
    for (const behaviour of ["rejects", "throws"]) {
        const b = createBrowser({ fetchBehaviour: behaviour }); b.load("client-errors.js");
        b.fireWindow("error", { message: "boom" });   // must not throw
        await new Promise(r => setImmediate(r));       // and must not leave an unhandled rejection
    }
});
test("other scripts can report through window.chessTeams.reportError", () => {
    const b = createBrowser(); b.load("client-errors.js");
    b.window.chessTeams.reportError("manual", "something happened");
    assert.deepStrictEqual([b.calls[0].body.kind, b.calls[0].body.message], ["manual", "something happened"]);
});

// ---------- teams-interop.js ----------
function teamsBrowser({ inTeams = true, initialize = () => Promise.resolve(), getContext = () => Promise.resolve({ chat: { id: "19:abc@thread.v2" } }),
    getAuthToken = () => Promise.resolve("the-token"), withErrorReporting = true } = {}) {
    const b = createBrowser();
    const notified = [];
    b.sandbox.microsoftTeams = {
        app: { initialize, getContext, notifyAppLoaded: () => notified.push("loaded"), notifySuccess: () => notified.push("success") },
        authentication: { getAuthToken },
    };
    if (inTeams) b.window.parent = {};   // another object than window: we are inside a frame
    if (withErrorReporting) b.load("client-errors.js");
    b.load("teams-interop.js");
    b.notified = notified;
    return b;
}
test("outside of Teams there is no context and the SDK is not touched", async () => {
    const b = teamsBrowser({ inTeams: false });
    assert.strictEqual(await b.window.chessTeams.getContext(), null);
    assert.deepStrictEqual(b.notified, []);
    assert.strictEqual(b.calls.length, 0);
});
test("inside Teams the context has the chat id and Teams is told that the app is loaded", async () => {
    const b = teamsBrowser();
    assert.strictEqual(JSON.stringify(await b.window.chessTeams.getContext()), JSON.stringify({ chatId: "19:abc@thread.v2" }));
    assert.deepStrictEqual(b.notified, ["loaded", "success"]);
    assert.strictEqual(b.calls.length, 0);
});
test("a personal tab has no chat id", async () => {
    const b = teamsBrowser({ getContext: () => Promise.resolve({}) });
    assert.strictEqual(JSON.stringify(await b.window.chessTeams.getContext()), JSON.stringify({ chatId: null }));
});
test("a failing initialisation gives null and is reported once as teams-sdk", async () => {
    const b = teamsBrowser({ initialize: () => Promise.reject({ errorCode: 4, message: "Not in Teams" }) });
    assert.strictEqual(await b.window.chessTeams.getContext(), null);
    assert.strictEqual(b.calls.length, 1);
    assert.deepStrictEqual([b.calls[0].body.kind, b.calls[0].body.message], ["teams-sdk", "code 4: Not in Teams"]);
});
test("the auth token is returned", async () => {
    const b = teamsBrowser();
    assert.strictEqual(await b.window.chessTeams.getAuthToken(), "the-token");
    assert.strictEqual(b.calls.length, 0);
});
test("a failing token request gives null and is reported as teams-auth with the error code", async () => {
    const b = teamsBrowser({ getAuthToken: () => Promise.reject({ errorCode: 1000, message: "resolve_consent_required" }) });
    assert.strictEqual(await b.window.chessTeams.getAuthToken(), null);
    assert.deepStrictEqual([b.calls[0].body.kind, b.calls[0].body.message], ["teams-auth", "code 1000: resolve_consent_required"]);
});
test("teams-interop keeps the error reporting of client-errors.js", () => {
    const b = teamsBrowser();
    assert.strictEqual(typeof b.window.chessTeams.reportError, "function");
    assert.strictEqual(typeof b.window.chessTeams.getContext, "function");
});
test("without client-errors.js a failure still gives null and does not throw", async () => {
    const b = teamsBrowser({ withErrorReporting: false, getAuthToken: () => Promise.reject(new Error("no")) });
    assert.strictEqual(await b.window.chessTeams.getAuthToken(), null);
});

(async () => {
    let passed = 0, failed = 0;
    for (const { name, fn } of tests) {
        try { await fn(); passed++; } catch (e) { failed++; console.log("FAIL", name, "\n   ", e.message.split("\n")[0]); }
    }
    console.log(`Passed ${passed}, failed ${failed}`);
    process.exit(failed ? 1 : 0);
})();