(() => {
    "use strict";

    function selectedEndpoints(dialog) {
        return Array.from(
            dialog.querySelectorAll('input[name="E1Ids"]:checked')
        );
    }

    function updateSelection(dialog) {
        dialog.querySelector("[data-path-status-count]").textContent =
            `${selectedEndpoints(dialog).length} selected`;

        dialog.querySelector("[data-path-status-error]").hidden = true;
    }

    function updateDescription(dialog) {
        dialog.querySelector("[data-path-status-description]").required =
            dialog.querySelector("[data-path-new-status]").value === "Connected";
    }

    const triggers = Array.from(
        document.querySelectorAll("[data-path-status-dialog]")
    );

    triggers.forEach(trigger => {
        trigger.addEventListener("click", () => {
            const dialog = document.getElementById(
                trigger.dataset.pathStatusDialog
            );

            if (!dialog) return;

            dialog.querySelector("form").reset();

            dialog.querySelectorAll('input[name="E1Ids"]').forEach(checkbox => {
                checkbox.checked =
                    checkbox.value === trigger.dataset.pathStatusE1;
            });

            const selected = selectedEndpoints(dialog)[0];
            const status = selected?.dataset.currentStatus;

            dialog.querySelector("[data-path-new-status]").value =
                ["Available", "Wrong", "Connected"].includes(status)
                    ? status
                    : "";

            updateSelection(dialog);
            updateDescription(dialog);
            dialog.showModal();
        });
    });

    document.querySelectorAll(".path-status-dialog").forEach(dialog => {
        dialog.querySelectorAll("[data-path-status-close]").forEach(button => {
            button.addEventListener("click", () => dialog.close());
        });

        dialog.querySelector("[data-path-status-select-all]")
            .addEventListener("click", () => {
                dialog.querySelectorAll('input[name="E1Ids"]')
                    .forEach(checkbox => checkbox.checked = true);

                updateSelection(dialog);
            });

        dialog.querySelector("[data-path-status-clear]")
            .addEventListener("click", () => {
                dialog.querySelectorAll('input[name="E1Ids"]')
                    .forEach(checkbox => checkbox.checked = false);

                updateSelection(dialog);
            });

        dialog.querySelectorAll('input[name="E1Ids"]').forEach(checkbox => {
            checkbox.addEventListener(
                "change",
                () => updateSelection(dialog)
            );
        });

        dialog.querySelector("[data-path-new-status]")
            .addEventListener("change", () => updateDescription(dialog));

        dialog.querySelector("form").addEventListener("submit", event => {
            const selected = selectedEndpoints(dialog);
            const error = dialog.querySelector("[data-path-status-error]");

            if (selected.length === 0) {
                event.preventDefault();
                error.textContent = "Select at least one E1 endpoint.";
                error.hidden = false;
                return;
            }

            const newStatus =
                dialog.querySelector("[data-path-new-status]").value;

            const confirmation = newStatus === "Available"
                ? "Release the selected physical E1 pair(s) from this customer's path? Both endpoints and their LTU assignments will be released."
                : `Change all ${selected.length} selected E1 endpoints to ${newStatus}?`;

            if (
                (newStatus === "Available" || selected.length > 1) &&
                !window.confirm(confirmation)
            ) {
                event.preventDefault();
            }
        });
    });

    // Expand the same path after saving.
    if (window.location.hash.startsWith("#path-body-")) {
        const pathBody = document.getElementById(
            window.location.hash.slice(1)
        );

        if (pathBody) {
            pathBody.hidden = false;

            const toggle = document.querySelector(
                `[data-path-toggle="${pathBody.id}"]`
            );

            if (toggle) {
                toggle.setAttribute("aria-expanded", "true");

                const text = toggle.querySelector(".path-toggle-text");
                const icon = toggle.querySelector(".path-toggle-icon");

                if (text) text.textContent = "Hide Path";
                if (icon) icon.textContent = "↑";
            }
        }
    }

    // Open the selected E1 when redirected from the old status page.
    const focusId = new URLSearchParams(window.location.search)
        .get("focusE1Id");

    if (focusId) {
        const trigger = triggers.find(
            button => button.dataset.pathStatusE1 === focusId
        );

        if (trigger) {
            const pathBody = trigger.closest(".path-result-body");

            if (pathBody) pathBody.hidden = false;

            trigger.click();
        }

        // Prevent the dialog from reopening automatically after saving.
        document.querySelectorAll(
            '.path-status-dialog input[name="ReturnUrl"]'
        ).forEach(input => {
            const url = new URL(input.value, window.location.origin);

            url.searchParams.delete("focusE1Id");

            input.value = url.pathname + url.search + url.hash;
            input.defaultValue = input.value;
        });
    }
})();