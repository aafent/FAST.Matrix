/**
 * FAST.Matrix JS Interop
 */
window.FastMatrix = {

    confirmNavigation: function (message) {
        return new Promise(function (resolve) {
            swal({
                title: 'Unsaved Changes',
                text: message,
                icon: 'warning',
                buttons: {
                    cancel:  { text: 'Stay',          value: false, visible: true, className: 'btn btn-secondary' },
                    confirm: { text: 'Leave & Discard', value: true, visible: true, className: 'btn btn-danger' }
                },
                dangerMode: true
            }).then(function (confirmed) {
                resolve(confirmed === true);
            });
        });
    },

    collapseSidebar: function () {
        document.body.classList.add('sidebar-collapse');
    },

    expandSidebar: function () {
        document.body.classList.remove('sidebar-collapse');
    },

    scrollContentToTop: function () {
        var wrapper = document.querySelector('.content-wrapper');
        if (wrapper) wrapper.scrollTop = 0;
    }
};
