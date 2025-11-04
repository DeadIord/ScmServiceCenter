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
    if (!board) {
        return;
    }

    const revertMove = (moveContext) => {
        if (!moveContext) {
            return;
        }

        const { from, item, oldIndex, originalNextSibling } = moveContext;
        if (!from || !item) {
            return;
        }

        const reference = originalNextSibling || (typeof oldIndex === 'number' ? from.children[oldIndex] ?? null : null);
        if (reference) {
            from.insertBefore(item, reference);
        } else {
            from.appendChild(item);
        }

        updateKanbanColumnCounts(board);
    };

    const handleStatusChange = (moveContext) => {
        if (!moveContext) {
            return;
        }

        const { item, from, to } = moveContext;
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
            revertMove(moveContext);
        });
    };

    updateKanbanColumnCounts(board);

    const groupName = 'orders-board';

    const preventPlaceholderDrop = (evt) => {
        if (!evt || !evt.related || !evt.related.classList) {
            return true;
        }

        return !evt.related.classList.contains('kanban-empty');
    };

    if (typeof Sortable !== 'undefined') {
        board.querySelectorAll('.kanban-column').forEach(column => {
            Sortable.create(column, {
                group: groupName,
                animation: 220,
                ghostClass: 'kanban-ghost',
                dragClass: 'kanban-dragging',
                filter: '.kanban-empty',
                onMove: preventPlaceholderDrop,
                onStart: evt => {
                    board.classList.add('kanban-board-dragging');
                    evt.item.classList.add('kanban-dragging');
                    evt.item.__originalNextSibling = evt.item.nextElementSibling;
                },
                onEnd: evt => {
                    board.classList.remove('kanban-board-dragging');
                    evt.item.classList.remove('kanban-dragging');
                    handleStatusChange({
                        item: evt.item,
                        from: evt.from,
                        to: evt.to,
                        oldIndex: evt.oldIndex,
                        originalNextSibling: evt.item.__originalNextSibling || null
                    });
                    delete evt.item.__originalNextSibling;
                }
            });
        });

        return;
    }

    const dragState = {
        item: null,
        fromColumn: null,
        oldIndex: null,
        originalNextSibling: null,
        didDrop: false
    };

    const setDraggable = (item) => {
        item.setAttribute('draggable', 'true');
        item.addEventListener('dragstart', event => {
            dragState.item = item;
            dragState.fromColumn = item.closest('.kanban-column');
            const siblings = dragState.fromColumn ? Array.from(dragState.fromColumn.querySelectorAll('.kanban-item')) : [];
            dragState.oldIndex = siblings.indexOf(item);
            dragState.originalNextSibling = item.nextElementSibling;
            dragState.didDrop = false;

            board.classList.add('kanban-board-dragging');
            item.classList.add('kanban-dragging');

            if (event.dataTransfer) {
                event.dataTransfer.effectAllowed = 'move';
                event.dataTransfer.setData('text/plain', item.dataset.id || '');
            }
        });

        item.addEventListener('dragend', () => {
            board.classList.remove('kanban-board-dragging');
            item.classList.remove('kanban-dragging');

            if (!dragState.didDrop) {
                revertMove({
                    item: dragState.item,
                    from: dragState.fromColumn,
                    oldIndex: dragState.oldIndex,
                    originalNextSibling: dragState.originalNextSibling
                });
            }

            dragState.item = null;
            dragState.fromColumn = null;
            dragState.oldIndex = null;
            dragState.originalNextSibling = null;
            dragState.didDrop = false;
        });
    };

    const getDragAfterElement = (column, positionY) => {
        const siblings = Array.from(column.querySelectorAll('.kanban-item:not(.kanban-dragging)'));

        return siblings.reduce((closest, child) => {
            const box = child.getBoundingClientRect();
            const offset = positionY - box.top - box.height / 2;

            if (offset < 0 && offset > closest.offset) {
                return { offset, element: child };
            }

            return closest;
        }, { offset: Number.NEGATIVE_INFINITY, element: null }).element;
    };

    board.querySelectorAll('.kanban-item').forEach(setDraggable);

    board.querySelectorAll('.kanban-column').forEach(column => {
        column.addEventListener('dragover', event => {
            if (!dragState.item) {
                return;
            }

            event.preventDefault();
            if (event.dataTransfer) {
                event.dataTransfer.dropEffect = 'move';
            }
            const afterElement = getDragAfterElement(column, event.clientY);

            if (!afterElement) {
                column.appendChild(dragState.item);
                return;
            }

            if (afterElement !== dragState.item) {
                column.insertBefore(dragState.item, afterElement);
            }
        });

        column.addEventListener('drop', event => {
            if (!dragState.item) {
                return;
            }

            event.preventDefault();

            const moveContext = {
                item: dragState.item,
                from: dragState.fromColumn,
                to: column,
                oldIndex: dragState.oldIndex,
                originalNextSibling: dragState.originalNextSibling
            };

            dragState.didDrop = true;

            handleStatusChange(moveContext);
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
