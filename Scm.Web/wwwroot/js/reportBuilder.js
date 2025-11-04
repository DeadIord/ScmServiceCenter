(function () {
    function ready(callback) {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', callback);
        } else {
            callback();
        }
    }

    function normalizeState(state) {
        const normalized = state && typeof state === 'object' ? state : {};

        normalized.tables = Array.isArray(normalized.tables) ? normalized.tables.map(function (table) {
            return {
                schema: table && table.schema ? table.schema : 'public',
                name: table && table.name ? table.name : '',
                alias: table && table.alias ? table.alias : '',
                joinType: table && table.joinType ? table.joinType : 'Inner',
                joinCondition: table && table.joinCondition ? table.joinCondition : ''
            };
        }) : [];

        normalized.columns = Array.isArray(normalized.columns) ? normalized.columns.map(function (column) {
            return {
                tableAlias: column && column.tableAlias ? column.tableAlias : '',
                columnName: column && column.columnName ? column.columnName : '',
                alias: column && column.alias ? column.alias : '',
                aggregate: column && column.aggregate ? column.aggregate : '',
                groupBy: Boolean(column && column.groupBy),
                sortDirection: column && column.sortDirection ? column.sortDirection : '',
                sortOrder: column && column.sortOrder !== null && column && column.sortOrder !== undefined ? parseInt(column.sortOrder, 10) : null
            };
        }) : [];

        normalized.filters = Array.isArray(normalized.filters) ? normalized.filters.map(function (filter, index) {
            return {
                connector: filter && filter.connector ? filter.connector : (index === 0 ? 'AND' : 'AND'),
                tableAlias: filter && filter.tableAlias ? filter.tableAlias : '',
                columnName: filter && filter.columnName ? filter.columnName : '',
                operator: filter && filter.operator ? filter.operator : '=',
                value: filter && filter.value ? filter.value : '',
                parameterName: filter && filter.parameterName ? filter.parameterName : ''
            };
        }) : [];

        normalized.sorts = Array.isArray(normalized.sorts) ? normalized.sorts.map(function (sort) {
            return {
                tableAlias: sort && sort.tableAlias ? sort.tableAlias : '',
                columnName: sort && sort.columnName ? sort.columnName : '',
                direction: sort && sort.direction ? sort.direction : 'ASC',
                order: sort && sort.order !== null && sort && sort.order !== undefined ? parseInt(sort.order, 10) : null
            };
        }) : [];

        return normalized;
    }

    function getEffectiveAlias(table) {
        return table.alias && table.alias.trim() ? table.alias.trim() : table.name;
    }

    function buildMetadataIndex(metadata) {
        const index = {};
        if (!metadata || !Array.isArray(metadata.tables)) {
            return index;
        }

        metadata.tables.forEach(function (table) {
            const key = table.schema + '.' + table.name;
            index[key] = table;
        });
        return index;
    }

    window.initReportQueryBuilder = function () {
        const root = document.getElementById('report-builder-root');
        if (!root) {
            return;
        }

        const metadataUrl = root.dataset.metadataUrl;
        const generateSqlUrl = root.dataset.generateSqlUrl;
        const definitionInput = document.getElementById('query-definition-json');
        const sqlInput = document.getElementById('sql-text-value');
        const sqlPreview = root.querySelector('[data-role="sql-preview"]');
        const legacyNotice = root.querySelector('[data-role="legacy-notice"]');
        const badge = root.querySelector('[data-role="builder-badge"]');

        const selectors = {
            tableSelector: root.querySelector('[data-role="table-selector"]'),
            tableAlias: root.querySelector('[data-role="table-alias"]'),
            tableJoinType: root.querySelector('[data-role="table-join-type"]'),
            tablesBody: root.querySelector('[data-role="selected-tables"] tbody'),
            columnsBody: root.querySelector('[data-role="selected-columns"] tbody'),
            filtersBody: root.querySelector('[data-role="selected-filters"] tbody'),
            sortsBody: root.querySelector('[data-role="selected-sorts"] tbody')
        };

        const form = root.closest('form');
        const antiforgeryInput = form ? form.querySelector('input[name="__RequestVerificationToken"]') : null;
        const antiforgeryToken = antiforgeryInput ? antiforgeryInput.value : null;

        let metadata = { tables: [] };
        let metadataIndex = {};
        let refreshHandle = null;

        let state;
        try {
            state = JSON.parse(definitionInput && definitionInput.value ? definitionInput.value : (root.dataset.initialDefinition || '{}'));
        } catch (error) {
            state = {};
        }

        state = normalizeState(state);

        function updateBadge() {
            if (!badge) {
                return;
            }

            if (state.tables.length === 0) {
                badge.textContent = 'Наследованный SQL';
                badge.classList.remove('bg-success');
                badge.classList.add('bg-light', 'text-dark');
            } else {
                badge.textContent = 'Конструктор активен';
                badge.classList.remove('bg-light', 'text-dark');
                badge.classList.add('bg-success');
            }
        }

        function setLegacyNotice() {
            if (!legacyNotice) {
                return;
            }

            if (state.tables.length === 0) {
                legacyNotice.removeAttribute('hidden');
            } else {
                legacyNotice.setAttribute('hidden', 'hidden');
            }
        }

        function persistState() {
            if (definitionInput) {
                definitionInput.value = JSON.stringify(state);
            }

            updateBadge();
            setLegacyNotice();
        }

        function scheduleSqlRefresh() {
            if (refreshHandle) {
                clearTimeout(refreshHandle);
            }

            refreshHandle = window.setTimeout(function () {
                refreshHandle = null;
                refreshSql();
            }, 400);
        }

        function refreshSql() {
            if (!sqlPreview || !sqlInput) {
                return;
            }

            if (state.tables.length === 0) {
                sqlPreview.textContent = sqlInput.value || 'SQL-запрос будет сгенерирован после выбора таблиц.';
                return;
            }

            const payload = JSON.stringify(state);
            fetch(generateSqlUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': antiforgeryToken || ''
                },
                body: payload
            })
                .then(function (response) {
                    if (!response.ok) {
                        return response.json().then(function (data) {
                            throw new Error(data && data.error ? data.error : 'Не удалось сгенерировать SQL.');
                        }).catch(function () {
                            throw new Error('Не удалось сгенерировать SQL.');
                        });
                    }

                    return response.json();
                })
                .then(function (data) {
                    const sql = data && data.sql ? data.sql : '';
                    sqlPreview.textContent = sql || 'SQL-запрос будет сгенерирован после выбора таблиц.';
                    sqlInput.value = sql;
                })
                .catch(function (error) {
                    sqlPreview.textContent = error && error.message ? error.message : 'Ошибка генерации SQL.';
                });
        }

        function createEmptyRow(colspan, text) {
            const row = document.createElement('tr');
            row.className = 'text-center text-muted';
            row.dataset.role = 'empty';

            const cell = document.createElement('td');
            cell.colSpan = colspan;
            cell.textContent = text;
            row.appendChild(cell);
            return row;
        }

        function renderTables() {
            const body = selectors.tablesBody;
            if (!body) {
                return;
            }

            body.innerHTML = '';

            if (state.tables.length === 0) {
                body.appendChild(createEmptyRow(6, 'Таблицы не выбраны'));
                return;
            }

            state.tables.forEach(function (table, index) {
                const row = document.createElement('tr');
                row.dataset.index = index.toString();

                const numberCell = document.createElement('td');
                numberCell.textContent = (index + 1).toString();
                row.appendChild(numberCell);

                const nameCell = document.createElement('td');
                nameCell.textContent = table.schema + '.' + table.name;
                row.appendChild(nameCell);

                const aliasCell = document.createElement('td');
                const aliasInput = document.createElement('input');
                aliasInput.type = 'text';
                aliasInput.className = 'form-control form-control-sm';
                aliasInput.value = table.alias;
                aliasInput.placeholder = table.name;
                aliasInput.addEventListener('input', function () {
                    const oldAlias = getEffectiveAlias(table);
                    table.alias = aliasInput.value.trim();
                    const newAlias = getEffectiveAlias(table);
                    if (oldAlias !== newAlias) {
                        replaceAlias(oldAlias, newAlias);
                    }
                    persistState();
                    renderColumns();
                    renderFilters();
                    renderSorts();
                    scheduleSqlRefresh();
                });
                aliasCell.appendChild(aliasInput);
                row.appendChild(aliasCell);

                const joinTypeCell = document.createElement('td');
                const joinSelect = document.createElement('select');
                joinSelect.className = 'form-select form-select-sm';
                ['Inner', 'Left', 'Right', 'Full', 'Cross'].forEach(function (value) {
                    const option = document.createElement('option');
                    option.value = value;
                    option.textContent = value.toUpperCase() + ' JOIN';
                    if (table.joinType === value) {
                        option.selected = true;
                    }
                    joinSelect.appendChild(option);
                });
                if (index === 0) {
                    joinSelect.disabled = true;
                }
                joinSelect.addEventListener('change', function () {
                    table.joinType = joinSelect.value;
                    persistState();
                    scheduleSqlRefresh();
                });
                joinTypeCell.appendChild(joinSelect);
                row.appendChild(joinTypeCell);

                const conditionCell = document.createElement('td');
                const conditionInput = document.createElement('input');
                conditionInput.type = 'text';
                conditionInput.className = 'form-control form-control-sm';
                conditionInput.placeholder = 'например, o."Id" = q."OrderId"';
                conditionInput.value = table.joinCondition || '';
                if (index === 0) {
                    conditionInput.disabled = true;
                }
                conditionInput.addEventListener('input', function () {
                    table.joinCondition = conditionInput.value;
                    persistState();
                    scheduleSqlRefresh();
                });
                conditionCell.appendChild(conditionInput);
                row.appendChild(conditionCell);

                const actionsCell = document.createElement('td');
                actionsCell.className = 'text-end';
                const removeButton = document.createElement('button');
                removeButton.type = 'button';
                removeButton.className = 'btn btn-outline-danger btn-sm';
                removeButton.innerHTML = '<i class="bi bi-x"></i>';
                removeButton.addEventListener('click', function () {
                    removeTable(index);
                });
                actionsCell.appendChild(removeButton);
                row.appendChild(actionsCell);

                body.appendChild(row);
            });
        }

        function renderColumns() {
            const body = selectors.columnsBody;
            if (!body) {
                return;
            }

            body.innerHTML = '';

            if (state.columns.length === 0) {
                body.appendChild(createEmptyRow(7, 'Колонки не выбраны'));
                return;
            }

            const aliases = state.tables.map(function (table) { return getEffectiveAlias(table); });

            state.columns.forEach(function (column, index) {
                const row = document.createElement('tr');
                row.dataset.index = index.toString();

                const aliasCell = document.createElement('td');
                const aliasSelect = document.createElement('select');
                aliasSelect.className = 'form-select form-select-sm';
                aliases.forEach(function (alias) {
                    const option = document.createElement('option');
                    option.value = alias;
                    option.textContent = alias;
                    if ((column.tableAlias || aliases[0]) === alias) {
                        option.selected = true;
                    }
                    aliasSelect.appendChild(option);
                });
                aliasSelect.addEventListener('change', function () {
                    column.tableAlias = aliasSelect.value;
                    renderColumns();
                    persistState();
                    scheduleSqlRefresh();
                });
                aliasCell.appendChild(aliasSelect);
                row.appendChild(aliasCell);

                const columnCell = document.createElement('td');
                const columnSelect = document.createElement('select');
                columnSelect.className = 'form-select form-select-sm';
                populateColumnOptions(columnSelect, column.tableAlias || aliases[0], column.columnName);
                columnSelect.addEventListener('change', function () {
                    column.columnName = columnSelect.value;
                    persistState();
                    scheduleSqlRefresh();
                });
                columnCell.appendChild(columnSelect);
                row.appendChild(columnCell);

                const aggregateCell = document.createElement('td');
                const aggregateSelect = document.createElement('select');
                aggregateSelect.className = 'form-select form-select-sm';
                ['', 'COUNT', 'SUM', 'AVG', 'MIN', 'MAX'].forEach(function (value) {
                    const option = document.createElement('option');
                    option.value = value;
                    option.textContent = value === '' ? 'Без агрегата' : value;
                    if ((column.aggregate || '').toUpperCase() === value) {
                        option.selected = true;
                    }
                    aggregateSelect.appendChild(option);
                });
                aggregateSelect.addEventListener('change', function () {
                    column.aggregate = aggregateSelect.value;
                    persistState();
                    scheduleSqlRefresh();
                });
                aggregateCell.appendChild(aggregateSelect);
                row.appendChild(aggregateCell);

                const aliasOutCell = document.createElement('td');
                const aliasOutInput = document.createElement('input');
                aliasOutInput.type = 'text';
                aliasOutInput.className = 'form-control form-control-sm';
                aliasOutInput.value = column.alias || '';
                aliasOutInput.placeholder = 'Название столбца';
                aliasOutInput.addEventListener('input', function () {
                    column.alias = aliasOutInput.value;
                    persistState();
                    scheduleSqlRefresh();
                });
                aliasOutCell.appendChild(aliasOutInput);
                row.appendChild(aliasOutCell);

                const groupCell = document.createElement('td');
                groupCell.className = 'text-center';
                const groupInput = document.createElement('input');
                groupInput.type = 'checkbox';
                groupInput.className = 'form-check-input';
                groupInput.checked = Boolean(column.groupBy);
                groupInput.addEventListener('change', function () {
                    column.groupBy = groupInput.checked;
                    persistState();
                    scheduleSqlRefresh();
                });
                groupCell.appendChild(groupInput);
                row.appendChild(groupCell);

                const sortCell = document.createElement('td');
                const sortWrapper = document.createElement('div');
                sortWrapper.className = 'input-group input-group-sm';

                const directionSelect = document.createElement('select');
                directionSelect.className = 'form-select form-select-sm';
                [{ value: '', label: 'Нет' }, { value: 'ASC', label: 'ASC' }, { value: 'DESC', label: 'DESC' }].forEach(function (optionData) {
                    const option = document.createElement('option');
                    option.value = optionData.value;
                    option.textContent = optionData.label;
                    if ((column.sortDirection || '').toUpperCase() === optionData.value) {
                        option.selected = true;
                    }
                    directionSelect.appendChild(option);
                });
                directionSelect.addEventListener('change', function () {
                    column.sortDirection = directionSelect.value;
                    persistState();
                    scheduleSqlRefresh();
                });

                const orderInput = document.createElement('input');
                orderInput.type = 'number';
                orderInput.className = 'form-control';
                orderInput.min = '1';
                orderInput.value = column.sortOrder !== null && column.sortOrder !== undefined ? column.sortOrder : '';
                orderInput.addEventListener('input', function () {
                    column.sortOrder = orderInput.value ? parseInt(orderInput.value, 10) : null;
                    persistState();
                    scheduleSqlRefresh();
                });

                sortWrapper.appendChild(directionSelect);
                sortWrapper.appendChild(orderInput);
                sortCell.appendChild(sortWrapper);
                row.appendChild(sortCell);

                const actionsCell = document.createElement('td');
                actionsCell.className = 'text-end';
                const removeButton = document.createElement('button');
                removeButton.type = 'button';
                removeButton.className = 'btn btn-outline-danger btn-sm';
                removeButton.innerHTML = '<i class="bi bi-x"></i>';
                removeButton.addEventListener('click', function () {
                    state.columns.splice(index, 1);
                    persistState();
                    renderColumns();
                    scheduleSqlRefresh();
                });
                actionsCell.appendChild(removeButton);
                row.appendChild(actionsCell);

                body.appendChild(row);
            });
        }

        function renderFilters() {
            const body = selectors.filtersBody;
            if (!body) {
                return;
            }

            body.innerHTML = '';

            if (state.filters.length === 0) {
                body.appendChild(createEmptyRow(6, 'Фильтры не заданы'));
                return;
            }

            const aliases = state.tables.map(function (table) { return getEffectiveAlias(table); });

            state.filters.forEach(function (filter, index) {
                const row = document.createElement('tr');
                row.dataset.index = index.toString();

                const connectorCell = document.createElement('td');
                const connectorSelect = document.createElement('select');
                connectorSelect.className = 'form-select form-select-sm';
                ['AND', 'OR'].forEach(function (value) {
                    const option = document.createElement('option');
                    option.value = value;
                    option.textContent = value;
                    if ((filter.connector || 'AND').toUpperCase() === value) {
                        option.selected = true;
                    }
                    connectorSelect.appendChild(option);
                });
                if (index === 0) {
                    connectorSelect.disabled = true;
                }
                connectorSelect.addEventListener('change', function () {
                    filter.connector = connectorSelect.value;
                    persistState();
                    scheduleSqlRefresh();
                });
                connectorCell.appendChild(connectorSelect);
                row.appendChild(connectorCell);

                const aliasCell = document.createElement('td');
                const aliasSelect = document.createElement('select');
                aliasSelect.className = 'form-select form-select-sm';
                aliases.forEach(function (alias) {
                    const option = document.createElement('option');
                    option.value = alias;
                    option.textContent = alias;
                    if ((filter.tableAlias || aliases[0]) === alias) {
                        option.selected = true;
                    }
                    aliasSelect.appendChild(option);
                });
                aliasSelect.addEventListener('change', function () {
                    filter.tableAlias = aliasSelect.value;
                    renderFilters();
                    persistState();
                    scheduleSqlRefresh();
                });
                aliasCell.appendChild(aliasSelect);
                row.appendChild(aliasCell);

                const columnCell = document.createElement('td');
                const columnSelect = document.createElement('select');
                columnSelect.className = 'form-select form-select-sm';
                populateColumnOptions(columnSelect, filter.tableAlias || aliases[0], filter.columnName);
                columnSelect.addEventListener('change', function () {
                    filter.columnName = columnSelect.value;
                    persistState();
                    scheduleSqlRefresh();
                });
                columnCell.appendChild(columnSelect);
                row.appendChild(columnCell);

                const operatorCell = document.createElement('td');
                const operatorSelect = document.createElement('select');
                operatorSelect.className = 'form-select form-select-sm';
                ['=', '<>', '>', '<', '>=', '<=', 'LIKE', 'NOT LIKE', 'IN', 'NOT IN', 'BETWEEN', 'IS NULL', 'IS NOT NULL'].forEach(function (value) {
                    const option = document.createElement('option');
                    option.value = value;
                    option.textContent = value;
                    if ((filter.operator || '=').toUpperCase() === value) {
                        option.selected = true;
                    }
                    operatorSelect.appendChild(option);
                });
                operatorSelect.addEventListener('change', function () {
                    filter.operator = operatorSelect.value;
                    persistState();
                    scheduleSqlRefresh();
                });
                operatorCell.appendChild(operatorSelect);
                row.appendChild(operatorCell);

                const valueCell = document.createElement('td');
                const valueWrapper = document.createElement('div');
                valueWrapper.className = 'row g-1';

                const parameterCol = document.createElement('div');
                parameterCol.className = 'col-6';
                const parameterInput = document.createElement('input');
                parameterInput.type = 'text';
                parameterInput.className = 'form-control form-control-sm';
                parameterInput.placeholder = '@параметр';
                parameterInput.value = filter.parameterName || '';
                parameterInput.addEventListener('input', function () {
                    filter.parameterName = parameterInput.value;
                    persistState();
                    scheduleSqlRefresh();
                });
                parameterCol.appendChild(parameterInput);

                const valueCol = document.createElement('div');
                valueCol.className = 'col-6';
                const valueInput = document.createElement('input');
                valueInput.type = 'text';
                valueInput.className = 'form-control form-control-sm';
                valueInput.placeholder = 'значение';
                valueInput.value = filter.value || '';
                valueInput.addEventListener('input', function () {
                    filter.value = valueInput.value;
                    persistState();
                    scheduleSqlRefresh();
                });
                valueCol.appendChild(valueInput);

                valueWrapper.appendChild(parameterCol);
                valueWrapper.appendChild(valueCol);
                valueCell.appendChild(valueWrapper);
                row.appendChild(valueCell);

                const actionsCell = document.createElement('td');
                actionsCell.className = 'text-end';
                const removeButton = document.createElement('button');
                removeButton.type = 'button';
                removeButton.className = 'btn btn-outline-danger btn-sm';
                removeButton.innerHTML = '<i class="bi bi-x"></i>';
                removeButton.addEventListener('click', function () {
                    state.filters.splice(index, 1);
                    persistState();
                    renderFilters();
                    scheduleSqlRefresh();
                });
                actionsCell.appendChild(removeButton);
                row.appendChild(actionsCell);

                body.appendChild(row);
            });
        }

        function renderSorts() {
            const body = selectors.sortsBody;
            if (!body) {
                return;
            }

            body.innerHTML = '';

            if (state.sorts.length === 0) {
                body.appendChild(createEmptyRow(5, 'Сортировка не задана'));
                return;
            }

            const aliases = state.tables.map(function (table) { return getEffectiveAlias(table); });

            state.sorts.forEach(function (sort, index) {
                const row = document.createElement('tr');
                row.dataset.index = index.toString();

                const aliasCell = document.createElement('td');
                const aliasSelect = document.createElement('select');
                aliasSelect.className = 'form-select form-select-sm';
                const emptyOption = document.createElement('option');
                emptyOption.value = '';
                emptyOption.textContent = '—';
                if (!sort.tableAlias) {
                    emptyOption.selected = true;
                }
                aliasSelect.appendChild(emptyOption);
                aliases.forEach(function (alias) {
                    const option = document.createElement('option');
                    option.value = alias;
                    option.textContent = alias;
                    if (sort.tableAlias === alias) {
                        option.selected = true;
                    }
                    aliasSelect.appendChild(option);
                });
                aliasSelect.addEventListener('change', function () {
                    sort.tableAlias = aliasSelect.value;
                    persistState();
                    scheduleSqlRefresh();
                });
                aliasCell.appendChild(aliasSelect);
                row.appendChild(aliasCell);

                const columnCell = document.createElement('td');
                const columnInput = document.createElement('input');
                columnInput.type = 'text';
                columnInput.className = 'form-control form-control-sm';
                columnInput.placeholder = 'Колонка или выражение';
                columnInput.value = sort.columnName || '';
                columnInput.addEventListener('input', function () {
                    sort.columnName = columnInput.value;
                    persistState();
                    scheduleSqlRefresh();
                });
                columnCell.appendChild(columnInput);
                row.appendChild(columnCell);

                const directionCell = document.createElement('td');
                const directionSelect = document.createElement('select');
                directionSelect.className = 'form-select form-select-sm';
                ['ASC', 'DESC'].forEach(function (value) {
                    const option = document.createElement('option');
                    option.value = value;
                    option.textContent = value;
                    if ((sort.direction || 'ASC').toUpperCase() === value) {
                        option.selected = true;
                    }
                    directionSelect.appendChild(option);
                });
                directionSelect.addEventListener('change', function () {
                    sort.direction = directionSelect.value;
                    persistState();
                    scheduleSqlRefresh();
                });
                directionCell.appendChild(directionSelect);
                row.appendChild(directionCell);

                const orderCell = document.createElement('td');
                const orderInput = document.createElement('input');
                orderInput.type = 'number';
                orderInput.className = 'form-control form-control-sm';
                orderInput.min = '1';
                orderInput.value = sort.order !== null && sort.order !== undefined ? sort.order : '';
                orderInput.addEventListener('input', function () {
                    sort.order = orderInput.value ? parseInt(orderInput.value, 10) : null;
                    persistState();
                    scheduleSqlRefresh();
                });
                orderCell.appendChild(orderInput);
                row.appendChild(orderCell);

                const actionsCell = document.createElement('td');
                actionsCell.className = 'text-end';
                const removeButton = document.createElement('button');
                removeButton.type = 'button';
                removeButton.className = 'btn btn-outline-danger btn-sm';
                removeButton.innerHTML = '<i class="bi bi-x"></i>';
                removeButton.addEventListener('click', function () {
                    state.sorts.splice(index, 1);
                    persistState();
                    renderSorts();
                    scheduleSqlRefresh();
                });
                actionsCell.appendChild(removeButton);
                row.appendChild(actionsCell);

                body.appendChild(row);
            });
        }

        function populateColumnOptions(select, alias, selectedColumn) {
            select.innerHTML = '';
            if (!alias) {
                const option = document.createElement('option');
                option.value = '';
                option.textContent = '—';
                select.appendChild(option);
                return;
            }

            const table = state.tables.find(function (tableItem) {
                return getEffectiveAlias(tableItem) === alias;
            });

            const placeholder = document.createElement('option');
            placeholder.value = '';
            placeholder.textContent = '—';
            select.appendChild(placeholder);

            if (!table) {
                return;
            }

            const metadataKey = table.schema + '.' + table.name;
            const metadataTable = metadataIndex[metadataKey];
            if (!metadataTable || !Array.isArray(metadataTable.columns)) {
                return;
            }

            metadataTable.columns.forEach(function (column) {
                const option = document.createElement('option');
                option.value = column.name;
                option.textContent = column.name + ' (' + column.dataType + ')';
                if (column.name === selectedColumn) {
                    option.selected = true;
                }
                select.appendChild(option);
            });
        }

        function replaceAlias(oldAlias, newAlias) {
            state.columns.forEach(function (column) {
                if (column.tableAlias === oldAlias) {
                    column.tableAlias = newAlias;
                }
            });

            state.filters.forEach(function (filter) {
                if (filter.tableAlias === oldAlias) {
                    filter.tableAlias = newAlias;
                }
            });

            state.sorts.forEach(function (sort) {
                if (sort.tableAlias === oldAlias) {
                    sort.tableAlias = newAlias;
                }
            });
        }

        function removeTable(index) {
            if (index < 0 || index >= state.tables.length) {
                return;
            }

            const table = state.tables[index];
            const alias = getEffectiveAlias(table);
            state.tables.splice(index, 1);

            state.columns = state.columns.filter(function (column) { return column.tableAlias !== alias; });
            state.filters = state.filters.filter(function (filter) { return filter.tableAlias !== alias; });
            state.sorts = state.sorts.filter(function (sort) { return sort.tableAlias !== alias; });

            persistState();
            renderTables();
            renderColumns();
            renderFilters();
            renderSorts();
            scheduleSqlRefresh();
        }

        function addTable() {
            const selector = selectors.tableSelector;
            if (!selector || selector.value === '') {
                return;
            }

            const selectedOption = selector.options[selector.selectedIndex];
            const schema = selectedOption.dataset.schema || 'public';
            const name = selectedOption.dataset.name || selector.value;
            const joinType = selectors.tableJoinType ? selectors.tableJoinType.value : 'Inner';
            const aliasValue = selectors.tableAlias ? selectors.tableAlias.value.trim() : '';

            state.tables.push({
                schema: schema,
                name: name,
                alias: aliasValue,
                joinType: state.tables.length === 0 ? 'Inner' : joinType,
                joinCondition: ''
            });

            if (selectors.tableAlias) {
                selectors.tableAlias.value = '';
            }

            persistState();
            renderTables();
            renderColumns();
            renderFilters();
            renderSorts();
            scheduleSqlRefresh();
        }

        function addColumn() {
            if (state.tables.length === 0) {
                return;
            }

            const firstAlias = getEffectiveAlias(state.tables[0]);
            state.columns.push({
                tableAlias: firstAlias,
                columnName: '',
                alias: '',
                aggregate: '',
                groupBy: false,
                sortDirection: '',
                sortOrder: null
            });

            persistState();
            renderColumns();
        }

        function addFilter() {
            if (state.tables.length === 0) {
                return;
            }

            const firstAlias = getEffectiveAlias(state.tables[0]);
            state.filters.push({
                connector: state.filters.length === 0 ? 'AND' : 'AND',
                tableAlias: firstAlias,
                columnName: '',
                operator: '=',
                value: '',
                parameterName: ''
            });

            persistState();
            renderFilters();
        }

        function addSort() {
            state.sorts.push({
                tableAlias: '',
                columnName: '',
                direction: 'ASC',
                order: null
            });

            persistState();
            renderSorts();
        }

        function populateTableSelector() {
            const selector = selectors.tableSelector;
            if (!selector) {
                return;
            }

            selector.innerHTML = '';

            const placeholder = document.createElement('option');
            placeholder.value = '';
            placeholder.textContent = 'Выберите таблицу';
            selector.appendChild(placeholder);

            metadata.tables.forEach(function (table) {
                const option = document.createElement('option');
                option.value = table.name;
                option.dataset.schema = table.schema;
                option.dataset.name = table.name;
                option.textContent = table.schema + '.' + table.name;
                selector.appendChild(option);
            });
        }

        if (root.querySelector('[data-action="add-table"]')) {
            root.querySelector('[data-action="add-table"]').addEventListener('click', addTable);
        }
        if (root.querySelector('[data-action="add-column"]')) {
            root.querySelector('[data-action="add-column"]').addEventListener('click', function () {
                addColumn();
                scheduleSqlRefresh();
            });
        }
        if (root.querySelector('[data-action="add-filter"]')) {
            root.querySelector('[data-action="add-filter"]').addEventListener('click', function () {
                addFilter();
                scheduleSqlRefresh();
            });
        }
        if (root.querySelector('[data-action="add-sort"]')) {
            root.querySelector('[data-action="add-sort"]').addEventListener('click', function () {
                addSort();
                scheduleSqlRefresh();
            });
        }

        persistState();
        renderTables();
        renderColumns();
        renderFilters();
        renderSorts();
        scheduleSqlRefresh();

        if (metadataUrl) {
            fetch(metadataUrl)
                .then(function (response) { return response.json(); })
                .then(function (data) {
                    metadata = data || { tables: [] };
                    metadataIndex = buildMetadataIndex(metadata);
                    populateTableSelector();
                    renderColumns();
                    renderFilters();
                })
                .catch(function () {
                    metadata = { tables: [] };
                    metadataIndex = {};
                    populateTableSelector();
                });
        }
    };

    ready(function () {
        if (typeof window.initReportQueryBuilder === 'function') {
            window.initReportQueryBuilder();
        }
    });
})();
