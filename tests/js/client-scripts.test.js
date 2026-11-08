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
    window.parent = window;     // not in an iframe by default
    const document = {
        addEventListener(type, handler, capture) { (documentListeners[type] ||= []).push({ handler, capture }); },
        querySelector(selector) {
            if (selector === 'meta[name="traceparent"]' && traceparent)
                return { getAttribute: name => (name === "content" ? traceparent : null) };
            return null;
        },
    };
    const sandbox = {

    };
}