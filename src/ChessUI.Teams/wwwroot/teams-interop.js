window.chessTeams = window.chessTeams || {};

// client-errors.js is loaded before this file. Without it, errors are only shown in the console.
function reportToServer(kind, error) {
    if (typeof window.chessTeams.reportError === "function") {
        window.chessTeams.reportError(kind, error);
    }
}

Object.assign(window.chessTeams, {

    // Returns { chatId }, or null if the app does not run inside Teams
    getContext: async function () {
        const inTeamsFrame = window.parent !== window || !!window.nativeInterface;

        if (!inTeamsFrame || typeof microsoftTeams === "undefined") {
            return null;
        }

        try {
            await microsoftTeams.app.initialize();
            microsoftTeams.app.notifyAppLoaded();
            microsoftTeams.app.notifySuccess();

            const context = await microsoftTeams.app.getContext();

            return { chatId: context.chat ? context.chat.id : null };
        } catch (error) {
            console.warn("Teams initialisation failed", error);
            reportToServer("teams-sdk", error);
            return null;
        }
    },

    // Asks Teams for the signed-in user's token (call after getContext)
    getAuthToken: async function () {
        try {
            return await microsoftTeams.authentication.getAuthToken();
        } catch (error) {
            console.warn("getAuthToken failed", error);
            reportToServer("teams-auth", error);
            return null;
        }
    },

    // Runs on the /config page: tells Teams which page to show as the tab
    registerConfig: async function () {
        await microsoftTeams.app.initialize();

        const origin = window.location.origin;

        microsoftTeams.pages.config.registerOnSaveHandler(function (saveEvent) {
            microsoftTeams.pages.config.setConfig({
                entityId: "chess-power",
                suggestedDisplayName: "Chess Power",
                contentUrl: origin + "/",
                websiteUrl: origin + "/"
            }).then(function () {
                saveEvent.notifySuccess();
            }).catch(function (error) {
                reportToServer("teams-sdk", error);
                saveEvent.notifyFailure("Could not save the tab.");
            });
        });

        microsoftTeams.pages.config.setValidityState(true);
        microsoftTeams.app.notifySuccess();
    }
});