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

window.initReportDesigner = function () {
    const container = document.getElementById('report-designer');
    if (!container || typeof axios === 'undefined') {
        return;
    }

    const metadataUrl = container.dataset.metadataUrl || '';
    const generateUrl = container.dataset.generateUrl || '';
    const sqlInput = document.getElementById('sql-text');
    const configInput = document.getElementById('builder-config');
    const manualFlagInput = document.getElementById('manual-sql-flag');
    const manualToggle = document.getElementById('manual-sql-toggle');
    const manualEditor = document.getElementById('manual-sql-editor');
    const sqlPreview = document.getElementById('designer-sql-preview');
    const parameterBadge = document.getElementById('designer-parameter-count');
    const tableList = document.getElementById('designer-table-list');
    const tableSearch = document.getElementById('designer-table-search');
    const tableCountBadge = document.getElementById('designer-selected-table-count');
    const columnsContainer = document.getElementById('designer-columns');
    const fieldsList = document.getElementById('designer-selected-fields');
    const relationsContainer = document.getElementById('designer-relations');
    const filtersContainer = document.getElementById('designer-filters');
    const addFilterButton = document.getElementById('designer-add-filter');
    const generateButton = document.getElementById('designer-generate');

    let metadata = null;
    let activeTableKey = null;

    const readConfig = () => {
        let parsed;
        try {
            parsed = JSON.parse(configInput?.value || '{}');
        } catch (error) {
            parsed = {};
        }

        const tables = Array.isArray(parsed.Tables) ? parsed.Tables : Array.isArray(parsed.tables) ? parsed.tables : [];
        const fields = Array.isArray(parsed.Fields) ? parsed.Fields : Array.isArray(parsed.fields) ? parsed.fields : [];
        const relations = Array.isArray(parsed.Relations) ? parsed.Relations : Array.isArray(parsed.relations) ? parsed.relations : [];
        const filters = Array.isArray(parsed.Filters) ? parsed.Filters : Array.isArray(parsed.filters) ? parsed.filters : [];
        const useDistinct = Object.prototype.hasOwnProperty.call(parsed, 'UseDistinct') ? Boolean(parsed.UseDistinct) : Boolean(parsed.useDistinct);
        const allowManualSql = Object.prototype.hasOwnProperty.call(parsed, 'AllowManualSql') ? Boolean(parsed.AllowManualSql) : Boolean(parsed.allowManualSql);

        return {
            Tables: tables,
            Fields: fields,
            Relations: relations,
            Filters: filters,
            UseDistinct: useDistinct,
            AllowManualSql: allowManualSql
        };
    };

    let config = readConfig();

    const saveConfig = () => {
        if (manualFlagInput) {
            manualFlagInput.value = config.AllowManualSql ? 'true' : 'false';
        }
        if (configInput) {
            configInput.value = JSON.stringify(config);
        }
    };

    const updateParameterBadge = () => {
        if (!parameterBadge) {
            return;
        }
        const count = config.Filters.filter(filter => typeof filter.ParameterName === 'string' && filter.ParameterName.trim().length > 0).length;
        parameterBadge.textContent = `Параметры: ${count}`;
    };

    const selectTable = (tableKey) => {
        activeTableKey = tableKey;
        renderTableList();
        renderColumns();
    };

    const ensureActiveTable = () => {
        if (activeTableKey && config.Tables.includes(activeTableKey)) {
            return;
        }
        activeTableKey = config.Tables.length > 0 ? config.Tables[0] : null;
    };

    const removeTableDependencies = (tableKey) => {
        config.Fields = config.Fields.filter(field => field.TableKey !== tableKey);
        config.Filters = config.Filters.filter(filter => filter.TableKey !== tableKey);
        config.Relations = config.Relations.filter(relationId => {
            if (!metadata) {
                return true;
            }
            const relation = metadata.relations.find(item => item.id === relationId);
            if (!relation) {
                return false;
            }
            return relation.fromTableKey !== tableKey && relation.toTableKey !== tableKey;
        });
    };

    const toggleTable = (tableKey) => {
        const exists = config.Tables.includes(tableKey);
        if (exists) {
            config.Tables = config.Tables.filter(key => key !== tableKey);
            removeTableDependencies(tableKey);
        } else {
            config.Tables.push(tableKey);
        }
        ensureActiveTable();
        saveConfig();
        renderTableList();
        renderColumns();
        renderRelations();
        renderFields();
        renderFilters();
        updateTableCount();
        updateParameterBadge();
    };

    const updateTableCount = () => {
        if (!tableCountBadge) {
            return;
        }
        tableCountBadge.textContent = String(config.Tables.length);
    };

    const renderTableList = () => {
        if (!tableList || !metadata) {
            return;
        }

        tableList.innerHTML = '';
        const searchTerm = (tableSearch?.value || '').toLowerCase();
        const filteredTables = metadata.tables.filter(table => {
            const name = `${table.displayName} ${table.key}`.toLowerCase();
            return name.includes(searchTerm);
        });

        filteredTables.forEach(table => {
            const wrapper = document.createElement('div');
            wrapper.className = 'form-check form-check-sm mb-1 designer-table-item';
            if (table.key === activeTableKey) {
                wrapper.classList.add('fw-semibold');
            }

            const checkbox = document.createElement('input');
            checkbox.className = 'form-check-input';
            checkbox.type = 'checkbox';
            checkbox.id = `tbl-${table.key}`;
            checkbox.checked = config.Tables.includes(table.key);
            checkbox.addEventListener('change', () => toggleTable(table.key));

            const label = document.createElement('label');
            label.className = 'form-check-label d-flex justify-content-between align-items-center';
            label.setAttribute('for', checkbox.id);
            label.innerHTML = `<span>${table.displayName}</span><small class="text-muted ms-2">${table.key}</small>`;
            label.addEventListener('click', () => selectTable(table.key));

            wrapper.appendChild(checkbox);
            wrapper.appendChild(label);
            tableList.appendChild(wrapper);
        });

        ensureActiveTable();
    };

    const addField = (tableKey, columnName) => {
        const exists = config.Fields.some(field => field.TableKey === tableKey && field.ColumnName === columnName);
        if (!exists) {
            config.Fields.push({ TableKey: tableKey, ColumnName: columnName, Alias: null });
            saveConfig();
            renderFields();
        }
    };

    const removeField = (index) => {
        if (index >= 0 && index < config.Fields.length) {
            config.Fields.splice(index, 1);
            saveConfig();
            renderFields();
        }
    };

    const renderColumns = () => {
        if (!columnsContainer) {
            return;
        }

        columnsContainer.innerHTML = '';
        if (!metadata || !activeTableKey) {
            columnsContainer.innerHTML = '<div class="text-muted small">Выберите таблицу, чтобы увидеть столбцы.</div>';
            return;
        }

        const table = metadata.tables.find(item => item.key === activeTableKey);
        if (!table) {
            columnsContainer.innerHTML = '<div class="text-muted small">Выбранная таблица недоступна.</div>';
            return;
        }

        table.columns.forEach(column => {
            const button = document.createElement('button');
            button.type = 'button';
            button.className = 'btn btn-sm btn-outline-secondary w-100 text-start mb-1';
            button.textContent = `${column.name}`;
            button.addEventListener('click', () => addField(table.key, column.name));
            columnsContainer.appendChild(button);
        });
    };

    const renderFields = () => {
        if (!fieldsList || !metadata) {
            return;
        }

        fieldsList.innerHTML = '';
        config.Fields.forEach((field, index) => {
            const table = metadata.tables.find(item => item.key === field.TableKey);
            const listItem = document.createElement('li');
            listItem.className = 'list-group-item d-flex align-items-center justify-content-between gap-2';
            const title = document.createElement('div');
            title.innerHTML = `<strong>${field.ColumnName}</strong><div class="text-muted small">${table ? table.displayName : field.TableKey}</div>`;

            const controls = document.createElement('div');
            controls.className = 'd-flex align-items-center gap-2';

            const aliasInput = document.createElement('input');
            aliasInput.type = 'text';
            aliasInput.className = 'form-control form-control-sm';
            aliasInput.placeholder = 'Псевдоним';
            aliasInput.value = field.Alias || '';
            aliasInput.addEventListener('input', () => {
                config.Fields[index].Alias = aliasInput.value || null;
                saveConfig();
            });

            const removeButton = document.createElement('button');
            removeButton.type = 'button';
            removeButton.className = 'btn btn-sm btn-outline-danger';
            removeButton.innerHTML = '<i class="bi bi-x"></i>';
            removeButton.addEventListener('click', () => removeField(index));

            controls.appendChild(aliasInput);
            controls.appendChild(removeButton);
            listItem.appendChild(title);
            listItem.appendChild(controls);
            fieldsList.appendChild(listItem);
        });
    };

    const toggleRelation = (relationId) => {
        if (!metadata) {
            return;
        }
        const exists = config.Relations.includes(relationId);
        if (exists) {
            config.Relations = config.Relations.filter(item => item !== relationId);
        } else {
            config.Relations.push(relationId);
        }
        saveConfig();
        renderRelations();
    };

    const renderRelations = () => {
        if (!relationsContainer || !metadata) {
            return;
        }

        relationsContainer.innerHTML = '';
        if (config.Tables.length < 2) {
            relationsContainer.innerHTML = '<div class="text-muted small">Добавьте ещё таблиц, чтобы выбрать связи.</div>';
            return;
        }

        const relevantRelations = metadata.relations.filter(relation => config.Tables.includes(relation.fromTableKey) && config.Tables.includes(relation.toTableKey));
        if (relevantRelations.length === 0) {
            relationsContainer.innerHTML = '<div class="text-muted small">Для выбранных таблиц нет явных связей. При необходимости добавьте их вручную.</div>';
            return;
        }

        relevantRelations.forEach(relation => {
            const relationDescription = `${relation.fromTableKey} ↔ ${relation.toTableKey}`;
            const relationId = relation.id;
            const wrapper = document.createElement('div');
            wrapper.className = 'form-check form-check-sm mb-1';

            const checkbox = document.createElement('input');
            checkbox.type = 'checkbox';
            checkbox.className = 'form-check-input';
            checkbox.id = `rel-${relationId}`;
            checkbox.checked = config.Relations.includes(relationId);
            checkbox.addEventListener('change', () => toggleRelation(relationId));

            const label = document.createElement('label');
            label.className = 'form-check-label';
            label.setAttribute('for', checkbox.id);
            label.innerHTML = `<span>${relationDescription}</span><div class="text-muted small">${relation.columns.map(pair => `${pair.fromColumn} = ${pair.toColumn}`).join(', ')}</div>`;

            wrapper.appendChild(checkbox);
            wrapper.appendChild(label);
            relationsContainer.appendChild(wrapper);
        });
    };

    const buildFilterRow = (filter, index) => {
        if (!metadata) {
            return document.createElement('div');
        }

        const row = document.createElement('div');
        row.className = 'border rounded p-2 mb-2';

        const tableSelect = document.createElement('select');
        tableSelect.className = 'form-select form-select-sm mb-2';
        metadata.tables.forEach(table => {
            if (config.Tables.includes(table.key)) {
                const option = document.createElement('option');
                option.value = table.key;
                option.textContent = table.displayName;
                option.selected = filter.TableKey === table.key;
                tableSelect.appendChild(option);
            }
        });
        tableSelect.addEventListener('change', () => {
            config.Filters[index].TableKey = tableSelect.value;
            config.Filters[index].ColumnName = '';
            saveConfig();
            renderFilters();
        });

        const columnSelect = document.createElement('select');
        columnSelect.className = 'form-select form-select-sm mb-2';
        const selectedTable = metadata.tables.find(item => item.key === (tableSelect.value || filter.TableKey));
        if (selectedTable) {
            selectedTable.columns.forEach(column => {
                const option = document.createElement('option');
                option.value = column.name;
                option.textContent = column.name;
                option.selected = filter.ColumnName === column.name;
                columnSelect.appendChild(option);
            });
        }
        columnSelect.addEventListener('change', () => {
            config.Filters[index].ColumnName = columnSelect.value;
            saveConfig();
        });

        const operatorSelect = document.createElement('select');
        operatorSelect.className = 'form-select form-select-sm mb-2';
        const operators = ['=', '<>', '>', '<', '>=', '<=', 'LIKE', 'NOT LIKE', 'ILIKE', 'NOT ILIKE'];
        operators.forEach(operator => {
            const option = document.createElement('option');
            option.value = operator;
            option.textContent = operator;
            option.selected = filter.Operator === operator;
            operatorSelect.appendChild(option);
        });
        operatorSelect.addEventListener('change', () => {
            config.Filters[index].Operator = operatorSelect.value;
            saveConfig();
            updateParameterBadge();
        });

        const parameterInput = document.createElement('input');
        parameterInput.type = 'text';
        parameterInput.className = 'form-control form-control-sm';
        parameterInput.placeholder = 'Имя параметра';
        parameterInput.value = filter.ParameterName || '';
        parameterInput.addEventListener('input', () => {
            config.Filters[index].ParameterName = parameterInput.value;
            saveConfig();
            updateParameterBadge();
        });

        const removeButton = document.createElement('button');
        removeButton.type = 'button';
        removeButton.className = 'btn btn-sm btn-outline-danger';
        removeButton.innerHTML = '<i class="bi bi-x"></i>';
        removeButton.addEventListener('click', () => {
            config.Filters.splice(index, 1);
            saveConfig();
            renderFilters();
            updateParameterBadge();
        });

        row.appendChild(tableSelect);
        row.appendChild(columnSelect);
        row.appendChild(operatorSelect);
        row.appendChild(parameterInput);
        row.appendChild(removeButton);
        return row;
    };

    const renderFilters = () => {
        if (!filtersContainer) {
            return;
        }

        filtersContainer.innerHTML = '';
        config.Filters.forEach((filter, index) => {
            const row = buildFilterRow(filter, index);
            filtersContainer.appendChild(row);
        });

        if (config.Filters.length === 0) {
            filtersContainer.innerHTML = '<div class="text-muted small">Фильтры не заданы.</div>';
        }
    };

    const addFilter = () => {
        const tableKey = config.Tables[0] || '';
        config.Filters.push({ TableKey: tableKey, ColumnName: '', Operator: '=', ParameterName: '' });
        saveConfig();
        renderFilters();
        updateParameterBadge();
    };

    const updateManualFlag = () => {
        if (!manualToggle || !manualEditor) {
            return;
        }
        manualToggle.checked = config.AllowManualSql;
        manualEditor.disabled = !config.AllowManualSql;
        if (!config.AllowManualSql && sqlPreview) {
            manualEditor.value = sqlPreview.textContent || '';
        }
        if (manualFlagInput) {
            manualFlagInput.value = config.AllowManualSql ? 'true' : 'false';
        }
        if (!config.AllowManualSql && sqlInput) {
            sqlInput.value = sqlPreview ? sqlPreview.textContent.trim() : sqlInput.value;
        }
    };

    const applySqlResult = (result) => {
        if (!result) {
            return;
        }

        const sqlText = typeof result.sql === 'string' ? result.sql : typeof result.Sql === 'string' ? result.Sql : '';
        const parameterList = Array.isArray(result.parameterNames)
            ? result.parameterNames
            : Array.isArray(result.ParameterNames)
                ? result.ParameterNames
                : [];

        if (!sqlText) {
            return;
        }

        if (sqlPreview) {
            sqlPreview.textContent = sqlText;
        }
        if (!config.AllowManualSql && manualEditor) {
            manualEditor.value = sqlText;
        }
        if (sqlInput) {
            sqlInput.value = config.AllowManualSql ? (manualEditor ? manualEditor.value : sqlText) : sqlText;
        }
        if (parameterBadge) {
            parameterBadge.textContent = `Параметры: ${parameterList.length}`;
        }
    };

    const showError = (message) => {
        if (sqlPreview) {
            sqlPreview.textContent = message || 'Не удалось сформировать SQL.';
            sqlPreview.classList.add('text-danger');
        }
    };

    const clearError = () => {
        if (sqlPreview) {
            sqlPreview.classList.remove('text-danger');
        }
    };

    const generateSql = () => {
        if (!generateUrl) {
            return;
        }
        clearError();
        saveConfig();
        axios.post(generateUrl, config)
            .then(response => {
                applySqlResult(response.data);
            })
            .catch(error => {
                const message = error?.response?.data?.error || 'Ошибка генерации SQL.';
                showError(message);
            });
    };

    const loadMetadata = () => {
        if (!metadataUrl) {
            return Promise.resolve();
        }
        return axios.get(metadataUrl)
            .then(response => {
                metadata = response.data;
                if (!metadata || typeof metadata !== 'object') {
                    metadata = { tables: [], relations: [] };
                }
                metadata.tables = Array.isArray(metadata.tables) ? metadata.tables : [];
                metadata.relations = Array.isArray(metadata.relations) ? metadata.relations : [];

                const tableExists = metadata.tables.some(table => config.Tables.includes(table.key));
                if (!tableExists) {
                    config.Tables = [];
                }
                saveConfig();
                renderTableList();
                renderColumns();
                renderFields();
                renderRelations();
                renderFilters();
                updateTableCount();
                updateParameterBadge();
            })
            .catch(() => {
                showError('Не удалось загрузить метаданные.');
            });
    };

    if (manualToggle) {
        manualToggle.addEventListener('change', () => {
            config.AllowManualSql = manualToggle.checked;
            saveConfig();
            updateManualFlag();
        });
    }

    if (manualEditor) {
        manualEditor.addEventListener('input', () => {
            if (sqlInput) {
                sqlInput.value = manualEditor.value;
            }
        });
    }

    if (tableSearch) {
        tableSearch.addEventListener('input', () => {
            renderTableList();
        });
    }

    if (addFilterButton) {
        addFilterButton.addEventListener('click', addFilter);
    }

    if (generateButton) {
        generateButton.addEventListener('click', () => {
            generateSql();
        });
    }

    saveConfig();
    updateManualFlag();
    loadMetadata().then(() => {
        if (config.Tables.length === 0 && metadata && Array.isArray(metadata.tables) && metadata.tables.length > 0) {
            activeTableKey = metadata.tables[0].key;
            renderTableList();
            renderColumns();
        }
        if (sqlPreview && !sqlPreview.textContent) {
            generateSql();
        }
    });
};
