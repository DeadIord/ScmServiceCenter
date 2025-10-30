let currentPage = 1;
const itemsPerPage = 10;
let filteredRows = [];
let sortColumn = null;
let sortDirection = 'asc';

// ==================== СТАТИСТИКА ====================
function updateStatistics() {
    const allRows = document.querySelectorAll('.data-table tbody tr:not([style*="display: none"])');
    const visibleRows = Array.from(allRows).filter(row => !row.querySelector('.text-center.py-5'));
    
    const totalCount = visibleRows.length;
    document.querySelector('.stats-value').textContent = totalCount;
    
    let activeCount = 0;
    visibleRows.forEach(row => {
        const toggle = row.querySelector('.stock-toggle');
        if (toggle && toggle.classList.contains('active')) {
            activeCount++;
        }
    });
    const activeStatsValue = document.querySelectorAll('.stats-value')[1];
    activeStatsValue.textContent = activeCount;
    const activePercentage = totalCount > 0 ? Math.round((activeCount / totalCount) * 100 * 10) / 10 : 0;
    activeStatsValue.nextElementSibling.textContent = activePercentage + '%';
    
    let lowStockCount = 0;
    visibleRows.forEach(row => {
        const stockCell = row.cells[5];
        if (stockCell && stockCell.querySelector('.text-danger')) {
            lowStockCount++;
        }
    });
    document.querySelectorAll('.stats-value')[2].textContent = lowStockCount;
    
    let totalValue = 0;
    visibleRows.forEach(row => {
        const priceCell = row.cells[4];
        const stockCell = row.cells[5];
        if (priceCell && stockCell) {
            const priceText = priceCell.textContent.replace(/[^\d.,]/g, '').replace(',', '.');
            const stockText = stockCell.textContent.replace(/[^\d.,]/g, '').replace(',', '.');
            const price = parseFloat(priceText) || 0;
            const stock = parseFloat(stockText) || 0;
            totalValue += price * stock;
        }
    });
    document.querySelectorAll('.stats-value')[3].textContent = totalValue.toLocaleString('ru-RU', { maximumFractionDigits: 0 }) + ' ₽';
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
    updateStatistics();
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
        updateStatistics();
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
        });
    }
    
    filterTable();
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