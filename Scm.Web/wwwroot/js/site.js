// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

const resources = document.body?.dataset ?? {};
const toastStatusUpdatedText = resources.toastStatusUpdated || 'Status updated';
const toastStatusErrorText = resources.toastStatusError || 'Could not update status';
const tableEmptyText = resources.tableEmpty || 'No records found';
const paramTypeStringText = resources.paramTypeString || 'String';
const paramTypeIntText = resources.paramTypeInt || 'Integer';
const paramTypeDecimalText = resources.paramTypeDecimal || 'Decimal';
const paramTypeDateText = resources.paramTypeDate || 'Date';
const paramTypeBoolText = resources.paramTypeBool || 'Boolean';

function updateKanbanColumnCounts(board) {
    if (!board) {
        return;
    }

    board.querySelectorAll('.kanban-column').forEach(column => {
        const header = column.previousElementSibling;
        const badge = header ? header.querySelector('.js-column-count') : null;
        const items = column.querySelectorAll('.kanban-item');
        const placeholder = column.querySelector('.kanban-empty');

        if (badge) {
            badge.textContent = items.length;
        }

        if (placeholder) {
            if (items.length) {
                placeholder.classList.add('d-none');
            } else {
                placeholder.classList.remove('d-none');
            }
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

        updateKanbanColumnCounts(board);

        const previousStatus = from.dataset.status;
        const newStatus = to.dataset.status;

        if (!previousStatus || !newStatus || previousStatus === newStatus) {
            return;
        }

        const orderId = item.dataset.id;
        if (!orderId) {
            return;
        }

        axios.post('/Orders/ChangeStatus', null, {
            params: { id: orderId, to: newStatus }
        }).then(() => {
            updateKanbanColumnCounts(board);
            if (typeof showToast === 'function') {
                showToast(toastStatusUpdatedText, 'bg-success');
            }
        }).catch(error => {
            const message = error?.response?.data?.message || toastStatusErrorText;
            if (typeof showToast === 'function') {
                showToast(message, 'bg-danger');
            }
            revertMove(evt);
        });
    };

    updateKanbanColumnCounts(board);

    const preventPlaceholderDrop = (evt) => {
        if (!evt || !evt.related || !evt.related.classList) {
            return true;
        }

        return !evt.related.classList.contains('kanban-empty');
    };

    board.querySelectorAll('.kanban-column').forEach(column => {
        Sortable.create(column, {
            group: groupName,
            animation: 220,
            ghostClass: 'kanban-ghost',
            dragClass: 'kanban-dragging',
            filter: '.kanban-empty',
            onMove: preventPlaceholderDrop,
            onStart: () => {
                board.classList.add('kanban-board-dragging');
            },
            onEnd: evt => {
                board.classList.remove('kanban-board-dragging');
                handleStatusChange(evt);
            }
        });
    });
};

window.initReportParametersEditor = function () {
    const tableBody = document.querySelector('#parameters-table tbody');
    if (!tableBody) {
        return;
    }

    const addButton = document.getElementById('add-parameter');

    const getRows = () => Array.from(tableBody.querySelectorAll('tr')).filter(row => !row.classList.contains('table-empty-row'));
    const removePlaceholder = () => {
        const placeholder = tableBody.querySelector('.table-empty-row');
        if (placeholder) {
            placeholder.remove();
        }
    };
    const ensurePlaceholder = () => {
        if (getRows().length) {
            removePlaceholder();
            return;
        }
        if (!tableBody.querySelector('.table-empty-row')) {
            const emptyRow = document.createElement('tr');
            emptyRow.className = 'table-empty-row';
            emptyRow.innerHTML = `<td colspan="4" class="text-center py-4 table-empty">${tableEmptyText}</td>`;
            tableBody.appendChild(emptyRow);
        }
    };
    const reindexRows = () => {
        getRows().forEach((row, index) => {
            row.querySelectorAll('[name^="Parameters["]').forEach(control => {
                const name = control.getAttribute('name');
                if (!name) {
                    return;
                }
                control.setAttribute('name', name.replace(/Parameters\[\d+\]/, `Parameters[${index}]`));
            });
        });
    };

    addButton?.addEventListener('click', () => {
        removePlaceholder();
        const index = getRows().length;
        const row = document.createElement('tr');
        row.innerHTML = `
            <td>
                <input class="form-control" name="Parameters[${index}].Name" placeholder="@example_param" />
            </td>
            <td>
                <select class="form-select" name="Parameters[${index}].Type">
                    <option value="string">${paramTypeStringText}</option>
                    <option value="int">${paramTypeIntText}</option>
                    <option value="decimal">${paramTypeDecimalText}</option>
                    <option value="date">${paramTypeDateText}</option>
                    <option value="bool">${paramTypeBoolText}</option>
                </select>
            </td>
            <td>
                <input class="form-control" name="Parameters[${index}].DefaultValue" />
            </td>
            <td class="text-end">
                <button type="button" class="btn btn-sm btn-outline-danger remove-parameter"><i class="bi bi-x"></i></button>
            </td>`;
        tableBody.appendChild(row);
        reindexRows();
    });

    tableBody.addEventListener('click', event => {
        if (!event.target.closest('.remove-parameter')) {
            return;
        }
        const row = event.target.closest('tr');
        if (row) {
            row.remove();
            reindexRows();
            ensurePlaceholder();
        }
    });

    ensurePlaceholder();
    reindexRows();
};
