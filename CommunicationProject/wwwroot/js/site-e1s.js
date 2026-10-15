(() => {
    "use strict";

    const container =
        document.getElementById("site-e1-container");

    const error =
        document.getElementById("site-e1-error");

    if (!container || !error) return;

    let pending = null;

    container.addEventListener("click", async (event) => {
        if (!(event.target instanceof Element)) return;

        const link = event.target.closest(
            ".site-e1-filters a, .site-e1-pagination a"
        );

        if (!link || !container.contains(link) ||
            event.defaultPrevented || event.button !== 0 ||
            event.ctrlKey || event.metaKey ||
            event.shiftKey || event.altKey) return;

        event.preventDefault();

        // Cancel older requests when filters are clicked quickly.
        pending?.abort();

        const controller = new AbortController();
        pending = controller;

        container.setAttribute("aria-busy", "true");
        error.hidden = true;

        const pageUrl = new URL(link.href);
        const requestUrl = new URL(pageUrl);

        requestUrl.searchParams.set("e1Only", "true");
        requestUrl.hash = "";

        try {
            const response = await fetch(requestUrl, {
                signal: controller.signal,
                credentials: "same-origin",
                redirect: "error"
            });

            if (!response.ok) {
                throw new Error("Request failed.");
            }

            const html = await response.text();

            if (controller.signal.aborted) return;

            const parsed = new DOMParser()
                .parseFromString(html, "text/html");

            const nextPanel =
                parsed.getElementById("site-e1s");

            const currentPanel =
                container.querySelector("#site-e1s");

            if (!nextPanel || !currentPanel) {
                throw new Error("E1 panel missing.");
            }

            nextPanel.open = currentPanel.open;
            currentPanel.replaceWith(nextPanel);

            // Keep the selected filter in the URL without navigation.
            history.replaceState(history.state, "", pageUrl);
        } catch (exception) {
            if (controller.signal.aborted) return;

            error.textContent =
                "Could not load E1s. Please try the filter again.";

            error.hidden = false;
        } finally {
            if (pending === controller) {
                container.removeAttribute("aria-busy");
                pending = null;
            }
        }
    });
})();