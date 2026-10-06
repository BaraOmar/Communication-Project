// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
document.addEventListener("DOMContentLoaded", function () {

    const sidebarToggle =
        document.getElementById("sidebarToggle");

    const mobileSidebarToggle =
        document.getElementById("mobileSidebarToggle");

    const sidebarBackdrop =
        document.getElementById("sidebarBackdrop");


    const STORAGE_KEY =
        "communication-sidebar-collapsed";


    /*
     * Restore desktop sidebar state.
     */
    const savedState =
        localStorage.getItem(STORAGE_KEY);


    if (savedState === "true") {
        document.body.classList.add(
            "sidebar-collapsed"
        );
    }


    /*
     * Desktop collapse / expand.
     */
    if (sidebarToggle) {

        sidebarToggle.addEventListener(
            "click",
            function () {

                document.body.classList.toggle(
                    "sidebar-collapsed"
                );


                const collapsed =
                    document.body.classList.contains(
                        "sidebar-collapsed"
                    );


                localStorage.setItem(
                    STORAGE_KEY,
                    collapsed.toString()
                );


                sidebarToggle.setAttribute(
                    "aria-label",
                    collapsed
                        ? "Expand sidebar"
                        : "Collapse sidebar"
                );


                sidebarToggle.setAttribute(
                    "title",
                    collapsed
                        ? "Expand sidebar"
                        : "Collapse sidebar"
                );

            }
        );

    }


    /*
     * Mobile sidebar.
     */
    if (mobileSidebarToggle) {

        mobileSidebarToggle.addEventListener(
            "click",
            function () {

                document.body.classList.toggle(
                    "sidebar-mobile-open"
                );

            }
        );

    }


    /*
     * Close mobile sidebar from backdrop.
     */
    if (sidebarBackdrop) {

        sidebarBackdrop.addEventListener(
            "click",
            function () {

                document.body.classList.remove(
                    "sidebar-mobile-open"
                );

            }
        );

    }

});