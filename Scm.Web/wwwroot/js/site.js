// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

function updateKanbanColumnCounts(board) {
    if (!board) {
        return;
    }

    board.querySelectorAll('.kanban-column').forEach(column => {
        const header = column.previousElementSibling;
        const badge = header ? header.querySelector('.js-column-count') : null;
        if (badge) {
            badge.textContent = column.querySelectorAll('.kanban-item').length;
        }
    });
}

window.initKanbanBoard = function () {
    const board = document.getElementById('kanban-board');
    if (!board || typeof Sortable === 'undefined') {
        return;
    }

    const groupName = 'orders-board';

    const revertMove = (evt) => {
        const { from, item, oldIndex } = evt;
        if (!from || !item) {
            return;
        }

        const reference = from.children[oldIndex] ?? null;
        from.insertBefore(item, reference);
        updateKanbanColumnCounts(board);
    };

    const handleStatusChange = (evt) => {
        const { item, from, to } = evt;
        if (!item || !from || !to) {
            return;
        }

        const previousStatus = from.dataset.status;
        const newStatus = to.dataset.status;

        if (!previousStatus || !newStatus || previousStatus === newStatus) {
            updateKanbanColumnCounts(board);
            return;
        }

        const orderId = item.dataset.id;
        if (!orderId) {
            updateKanbanColumnCounts(board);
            return;
        }

        axios.post('/Orders/ChangeStatus', null, {
            params: { id: orderId, to: newStatus }
        }).then(() => {
            updateKanbanColumnCounts(board);
            if (typeof showToast === 'function') {
                showToast('Статус обновлён', 'bg-success');
            }
        }).catch(error => {
            const message = error?.response?.data?.message || 'Не удалось обновить статус';
            if (typeof showToast === 'function') {
                showToast(message, 'bg-danger');
            }
            revertMove(evt);
        });
    };

    board.querySelectorAll('.kanban-column').forEach(column => {
        Sortable.create(column, {
            group: groupName,
            animation: 150,
            ghostClass: 'kanban-ghost',
            dragClass: 'kanban-dragging',
            onEnd: handleStatusChange
        });
    });
};
