/**
 * FAST.Matrix JS Interop
 * Called from NavigationGuardService and overlay components via IJSRuntime.
 */
window.FastMatrix = {

    /**
     * Shows a SweetAlert confirmation dialog before allowing navigation away
     * from an applet with unsaved changes.
     * Called by NavigationGuardService.HandleAsync via IJSRuntime.InvokeAsync.
     * @param {string} message - The confirmation message to display.
     * @returns {Promise<boolean>} - True if user confirms, false if cancelled.
     */
    confirmNavigation: function (message) {
        return new Promise(function (resolve) {
            swal({
                title: 'Unsaved Changes',
                text: message,
                icon: 'warning',
                buttons: {
                    cancel: {
                        text: 'Stay',
                        value: false,
                        visible: true,
                        className: 'btn btn-secondary'
                    },
                    confirm: {
                        text: 'Leave & Discard',
                        value: true,
                        visible: true,
                        className: 'btn btn-danger'
                    }
                },
                dangerMode: true
            }).then(function (confirmed) {
                resolve(confirmed === true);
            });
        });
    },

    /**
     * Pushes AdminLTE sidebar to collapsed state programmatically.
     * Useful when overlays open to reclaim visual space.
     */
    collapseSidebar: function () {
        document.body.classList.add('sidebar-collapse');
    },

    /**
     * Restores AdminLTE sidebar to expanded state.
     */
    expandSidebar: function () {
        document.body.classList.remove('sidebar-collapse');
    },

    /**
     * Scrolls the main content wrapper to the top.
     * Called after applet route transitions.
     */
    scrollContentToTop: function () {
        var wrapper = document.querySelector('.content-wrapper');
        if (wrapper) wrapper.scrollTop = 0;
    }
};
