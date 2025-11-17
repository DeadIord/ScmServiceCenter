const stockSummaryElement = document.getElementById('stock-summary');
const summaryState = {
    total: stockSummaryElement ? parseInt(stockSummaryElement.dataset.total || '0', 10) : 0,
    active: stockSummaryElement ? parseInt(stockSummaryElement.dataset.active || '0', 10) : 0,
    lowStock: stockSummaryElement ? parseInt(stockSummaryElement.dataset.low || '0', 10) : 0,
    totalValue: stockSummaryElement ? parseFloat(stockSummaryElement.dataset.value || '0') : 0
};

let currentPage = 1;
const itemsPerPage = 10;
let filteredRows = [];
let sortColumn = null;
let sortDirection = 'asc';

// ==================== СТАТИСТИКА ====================
function updateStatistics() {
    const statsValues = document.querySelectorAll('.stats-card .stats-value');
    if (statsValues.length < 4) {
        return;
    }

    const activePercentage = summaryState.total > 0
        ? Math.round((summaryState.active / summaryState.total) * 1000) / 10
        : 0;

    statsValues[0].textContent = summaryState.total;
    statsValues[1].textContent = summaryState.active;

    const activeChangeElement = statsValues[1].parentElement.querySelector('.stats-change');
    if (activeChangeElement) {
        activeChangeElement.textContent = `${activePercentage}%`;
    }

    statsValues[2].textContent = summaryState.lowStock;
    statsValues[3].textContent = summaryState.totalValue.toLocaleString('ru-RU', { maximumFractionDigits: 0 }) + ' ₽';
}

// ==================== СОРТИРОВКА ====================
function extractNumber(cell) {
    if (!cell) return 0;
    const text = cell.textContent || '';
    const cleaned = text.replace(/[^\d.,]/g, '');
    const normalized = cleaned.replace(',', '.');
    const number = parseFloat(normalized);
    return isNaN(number) ? 0 : number;
}

function applySort() {
    if (!sortColumn) return;

    filteredRows.sort((a, b) => {
        let aValue, bValue;

        switch(sortColumn) {
            case 'sku':
                const aCode = a.cells[1]?.querySelector('code');
                const bCode = b.cells[1]?.querySelector('code');
                aValue = aCode ? aCode.textContent.trim() : '';
                bValue = bCode ? bCode.textContent.trim() : '';

                const aMatch = aValue.match(/(.+?)-?(\d+)$/);
                const bMatch = bValue.match(/(.+?)-?(\d+)$/);

                if (aMatch && bMatch && aMatch[1] === bMatch[1]) {
                    return sortDirection === 'asc'
                        ? parseInt(aMatch[2]) - parseInt(bMatch[2])
                        : parseInt(bMatch[2]) - parseInt(aMatch[2]);
                }

                return sortDirection === 'asc'
                    ? aValue.localeCompare(bValue, 'ru')
                    : bValue.localeCompare(aValue, 'ru');

            case 'title':
                const aTitle = a.cells[2]?.querySelector('.product-info h6');
                const bTitle = b.cells[2]?.querySelector('.product-info h6');
                aValue = aTitle ? aTitle.textContent.trim() : '';
                bValue = bTitle ? bTitle.textContent.trim() : '';

                const aTitleMatch = aValue.match(/(.+?)\s*(\d+)$/);
                const bTitleMatch = bValue.match(/(.+?)\s*(\d+)$/);

                if (aTitleMatch && bTitleMatch && aTitleMatch[1] === bTitleMatch[1]) {
                    return sortDirection === 'asc'
                        ? parseInt(aTitleMatch[2]) - parseInt(bTitleMatch[2])
                        : parseInt(bTitleMatch[2]) - parseInt(aTitleMatch[2]);
                }

                return sortDirection === 'asc'
                    ? aValue.localeCompare(bValue, 'ru')
                    : bValue.localeCompare(aValue, 'ru');

            case 'price':
                aValue = extractNumber(a.cells[4]);
                bValue = extractNumber(b.cells[4]);
                break;

            case 'stock':
                aValue = extractNumber(a.cells[5]);
                bValue = extractNumber(b.cells[5]);
                break;

            case 'reorder':
                aValue = extractNumber(a.cells[6]);
                bValue = extractNumber(b.cells[6]);
                break;

            default:
                return 0;
        }

        if (typeof aValue === 'number' && typeof bValue === 'number') {
            return sortDirection === 'asc'
                ? aValue - bValue
                : bValue - aValue;
        }

        return 0;
    });

    const tbody = document.querySelector('.data-table tbody');
    filteredRows.forEach(row => tbody.appendChild(row));
}

function sortTable(column) {
    if (sortColumn === column) {
        sortDirection = sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
        sortColumn = column;
        sortDirection = 'asc';
    }

    document.querySelectorAll('.sortable').forEach(th => {
        th.classList.remove('asc', 'desc');
    });
    const activeHeader = document.querySelector(`.sortable[data-column="${column}"]`);
    if (activeHeader) {
        activeHeader.classList.add(sortDirection);
    }

    applySort();

    filteredRows.forEach(row => row.classList.add('sorting'));
    setTimeout(() => {
        filteredRows.forEach(row => row.classList.remove('sorting'));
    }, 300);

    currentPage = 1;
    updatePagination();
}

// ==================== ФИЛЬТРАЦИЯ ====================
document.addEventListener('DOMContentLoaded', function() {
    const statusFilter = document.querySelector('select[name="status"]');
    const categoryFilter = document.querySelector('select[name="category"]');
    const stockFilter = document.querySelector('select[name="stock"]');
    const searchInput = document.querySelector('input[name="q"]');
    const tableRows = document.querySelectorAll('.data-table tbody tr');

    document.querySelectorAll('.sortable').forEach(header => {
        header.addEventListener('click', function() {
            const column = this.dataset.column;
            sortTable(column);
            updateStatistics();
        });
    });

    function filterTable() {
        const statusValue = statusFilter.value;
        const stockValue = stockFilter.value;
        const searchValue = searchInput.value.toLowerCase();

        filteredRows = [];

        tableRows.forEach(row => {
            if (row.querySelector('.text-center.py-5')) return;

            let show = true;

            if (statusValue) {
                const toggle = row.querySelector('.stock-toggle');
                const isActive = toggle && toggle.classList.contains('active');
                if (statusValue === 'active' && !isActive) show = false;
                if (statusValue === 'inactive' && isActive) show = false;
            }

            if (stockValue && show) {
                const stockCell = row.cells[5];
                if (stockCell) {
                    const isLowStock = stockCell.querySelector('.text-danger');
                    if (stockValue === 'low' && !isLowStock) show = false;
                    if (stockValue === 'normal' && isLowStock) show = false;
                }
            }

            if (searchValue && show) {
                const sku = row.cells[1]?.textContent.toLowerCase() || '';
                const title = row.cells[2]?.textContent.toLowerCase() || '';
                if (!sku.includes(searchValue) && !title.includes(searchValue)) {
                    show = false;
                }
            }

            if (show) {
                filteredRows.push(row);
            }

            row.style.display = 'none';
        });

        applySort();

        currentPage = 1;
        updatePagination();
        updateEmptyState();
    }

    function updateEmptyState() {
        const emptyRow = document.querySelector('.data-table tbody tr td[colspan]');
        if (emptyRow) {
            emptyRow.parentElement.style.display = filteredRows.length === 0 ? '' : 'none';
        }
    }

    statusFilter?.addEventListener('change', filterTable);
    categoryFilter?.addEventListener('change', filterTable);
    stockFilter?.addEventListener('change', filterTable);
    searchInput?.addEventListener('input', filterTable);

    const resetBtn = document.querySelector('a[href*="Index"]');
    if (resetBtn && resetBtn.textContent.includes('Сбросить')) {
        resetBtn.addEventListener('click', function(e) {
            e.preventDefault();
            statusFilter.value = '';
            categoryFilter.value = '';
            stockFilter.value = '';
            searchInput.value = '';

            sortColumn = null;
            sortDirection = 'asc';
            document.querySelectorAll('.sortable').forEach(th => {
                th.classList.remove('asc', 'desc');
            });

            filterTable();
            updateStatistics();
        });
    }

    filterTable();
    updateStatistics();

    const receiveModalElement = document.getElementById('receiveModal');
    const receiveModal = receiveModalElement ? new bootstrap.Modal(receiveModalElement) : null;
    const receiveForm = receiveModalElement ? receiveModalElement.querySelector('form') : null;

    function fillReceiveForm(button) {
        if (!receiveForm) {
            return;
        }

        const skuInput = receiveForm.querySelector('input[name="Sku"]');
        const titleInput = receiveForm.querySelector('input[name="Title"]');
        const qtyInput = receiveForm.querySelector('input[name="Qty"]');
        const unitInput = receiveForm.querySelector('input[name="Unit"]');
        const priceInInput = receiveForm.querySelector('input[name="PriceIn"]');
        const priceOutInput = receiveForm.querySelector('input[name="PriceOut"]');

        if (skuInput) {
            skuInput.value = button.dataset.sku || '';
        }
        if (titleInput) {
            titleInput.value = button.dataset.title || '';
        }
        if (qtyInput) {
            qtyInput.value = qtyInput.value || '1';
        }
        if (unitInput) {
            unitInput.value = button.dataset.unit || '';
        }
        if (priceInInput) {
            priceInInput.value = button.dataset.pricein || '';
        }
        if (priceOutInput) {
            priceOutInput.value = button.dataset.priceout || '';
        }
    }

    document.querySelectorAll('.action-btn-edit').forEach(button => {
        button.addEventListener('click', function() {
            fillReceiveForm(this);
            receiveModal?.show();
        });
    });

    document.querySelectorAll('.action-btn-copy').forEach(button => {
        button.addEventListener('click', async function() {
            const sku = this.dataset.sku || '';
            if (!sku) {
                showNotification('SKU отсутствует', 'error');
                return;
            }

            try {
                await navigator.clipboard.writeText(sku);
                showNotification('SKU скопирован в буфер обмена', 'success');
            } catch (error) {
                console.error(error);
                showNotification('Не удалось скопировать SKU', 'error');
            }
        });
    });
});

// ==================== ПАГИНАЦИЯ ====================
function updatePagination() {
    const totalPages = Math.ceil(filteredRows.length / itemsPerPage);
    const startIndex = (currentPage - 1) * itemsPerPage;
    const endIndex = startIndex + itemsPerPage;

    let rowNumber = startIndex + 1;

    filteredRows.forEach((row, index) => {
        if (index >= startIndex && index < endIndex) {
            row.style.display = '';

            const numberSpan = row.querySelector('td:first-child span');
            if (numberSpan) {
                numberSpan.textContent = rowNumber++;
            }
        } else {
            row.style.display = 'none';
        }
    });

    renderPaginationButtons(totalPages);

    const countText = document.querySelector('.pagination-custom + div');
    if (countText) {
        const showing = filteredRows.length === 0 ? 0 : startIndex + 1;
        const to = Math.min(endIndex, filteredRows.length);
        countText.textContent = `Показано от ${showing} до ${to} из ${filteredRows.length} записей`;
    }
}

function renderPaginationButtons(totalPages) {
    const paginationContainer = document.querySelector('.pagination-custom');
    if (!paginationContainer || totalPages === 0) return;

    paginationContainer.innerHTML = '';

    const prevBtn = createPageButton('prev', '<i class="bi bi-chevron-left"></i>');
    prevBtn.disabled = currentPage === 1;
    prevBtn.style.opacity = currentPage === 1 ? '0.5' : '1';
    prevBtn.addEventListener('click', () => {
        if (currentPage > 1) {
            currentPage--;
            updatePagination();
        }
    });
    paginationContainer.appendChild(prevBtn);

    const maxVisible = 7;
    let startPage = 1;
    let endPage = totalPages;

    if (totalPages > maxVisible) {
        if (currentPage <= 4) {
            endPage = maxVisible - 2;
        } else if (currentPage >= totalPages - 3) {
            startPage = totalPages - (maxVisible - 3);
        } else {
            startPage = currentPage - 2;
            endPage = currentPage + 2;
        }
    }

    if (startPage > 1) {
        const btn = createPageButton('page', '1');
        btn.addEventListener('click', () => goToPage(1));
        paginationContainer.appendChild(btn);

        if (startPage > 2) {
            const dots = document.createElement('span');
            dots.className = 'px-2 text-muted';
            dots.textContent = '...';
            paginationContainer.appendChild(dots);
        }
    }

    for (let i = startPage; i <= endPage; i++) {
        const btn = createPageButton('page', i);
        if (i === currentPage) {
            btn.classList.add('active');
        }
        btn.addEventListener('click', () => goToPage(i));
        paginationContainer.appendChild(btn);
    }

    if (endPage < totalPages) {
        if (endPage < totalPages - 1) {
            const dots = document.createElement('span');
            dots.className = 'px-2 text-muted';
            dots.textContent = '...';
            paginationContainer.appendChild(dots);
        }

        const btn = createPageButton('page', totalPages);
        btn.addEventListener('click', () => goToPage(totalPages));
        paginationContainer.appendChild(btn);
    }

    const nextBtn = createPageButton('next', '<i class="bi bi-chevron-right"></i>');
    nextBtn.disabled = currentPage === totalPages;
    nextBtn.style.opacity = currentPage === totalPages ? '0.5' : '1';
    nextBtn.addEventListener('click', () => {
        if (currentPage < totalPages) {
            currentPage++;
            updatePagination();
        }
    });
    paginationContainer.appendChild(nextBtn);
}

function createPageButton(type, content) {
    const btn = document.createElement('button');
    btn.className = 'page-btn';
    btn.innerHTML = content;
    btn.type = 'button';
    return btn;
}

function goToPage(page) {
    currentPage = page;
    updatePagination();
    document.querySelector('.table-card').scrollIntoView({ behavior: 'smooth', block: 'start' });
}
