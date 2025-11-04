let currentPage = 1;
const itemsPerPage = 10;
let filteredRows = [];
let filteredMobileCards = [];

document.addEventListener('DOMContentLoaded', function() {
    const tableRows = document.querySelectorAll('.accounts-table tbody tr[data-type]');
    const mobileCards = document.querySelectorAll('.mobile-account-card[data-type]');
    const searchInput = document.querySelector('.search-input');

    function initPagination() {
        filteredRows = Array.from(tableRows);
        filteredMobileCards = Array.from(mobileCards);

        currentPage = 1;
        updatePagination();
        updateEmptyState();
    }

    function updateEmptyState() {
        const emptyRow = document.querySelector('.accounts-table tbody tr td[colspan]');
        if (emptyRow) {
            emptyRow.parentElement.style.display = filteredRows.length === 0 ? '' : 'none';
        }

        const mobileEmpty = document.querySelector('.mobile-accounts-grid .empty-state');
        if (mobileEmpty) {
            mobileEmpty.style.display = filteredMobileCards.length === 0 ? '' : 'none';
        }
    }

    if (searchInput) {
        searchInput.addEventListener('input', function() {
            const searchValue = this.value.toLowerCase();

            filteredRows = [];
            filteredMobileCards = [];

            tableRows.forEach(row => {
                const name = row.querySelector('.account-name')?.textContent.toLowerCase() || '';
                const inn = row.cells[2]?.textContent.toLowerCase() || '';

                if (name.includes(searchValue) || inn.includes(searchValue)) {
                    filteredRows.push(row);
                }
            });

            mobileCards.forEach(card => {
                const name = card.querySelector('.mobile-account-name')?.textContent.toLowerCase() || '';
                const inn = card.querySelector('.mobile-info-value')?.textContent.toLowerCase() || '';

                if (name.includes(searchValue) || inn.includes(searchValue)) {
                    filteredMobileCards.push(card);
                }
            });

            currentPage = 1;
            updatePagination();
            updateEmptyState();
        });
    }

    initPagination();
});

// ==================== Фильтрация ====================
function filterByType(type) {
    const rows = document.querySelectorAll('.accounts-table tbody tr[data-type]');
    const cards = document.querySelectorAll('.mobile-account-card[data-type]');

    filteredRows = [];
    filteredMobileCards = [];

    rows.forEach(row => {
        if (row.dataset.type === type) {
            filteredRows.push(row);
        }
    });

    cards.forEach(card => {
        if (card.dataset.type === type) {
            filteredMobileCards.push(card);
        }
    });

    currentPage = 1;
    updatePagination();
    updateEmptyState();
}

function filterByInn() {
    const rows = document.querySelectorAll('.accounts-table tbody tr[data-has-inn]');
    const cards = document.querySelectorAll('.mobile-account-card[data-has-inn]');

    filteredRows = [];
    filteredMobileCards = [];

    rows.forEach(row => {
        if (row.dataset.hasInn === 'True') {
            filteredRows.push(row);
        }
    });

    cards.forEach(card => {
        if (card.dataset.hasInn === 'True') {
            filteredMobileCards.push(card);
        }
    });

    currentPage = 1;
    updatePagination();
    updateEmptyState();
}

function resetFilters() {
    const tableRows = document.querySelectorAll('.accounts-table tbody tr[data-type]');
    const mobileCards = document.querySelectorAll('.mobile-account-card[data-type]');

    filteredRows = Array.from(tableRows);
    filteredMobileCards = Array.from(mobileCards);

    currentPage = 1;
    updatePagination();
    updateEmptyState();
}

// ==================== Пагинация ====================
function updatePagination() {
    const totalPages = Math.ceil(filteredRows.length / itemsPerPage);
    const startIndex = (currentPage - 1) * itemsPerPage;
    const endIndex = startIndex + itemsPerPage;

    let rowNumber = startIndex + 1;

    document.querySelectorAll('.accounts-table tbody tr[data-type]').forEach(row => {
        row.style.display = 'none';
    });

    filteredRows.forEach((row, index) => {
        if (index >= startIndex && index < endIndex) {
            row.style.display = '';
            const numberSpan = row.querySelector('.row-number');
            if (numberSpan) {
                numberSpan.textContent = rowNumber++;
            }
        } else {
            row.style.display = 'none';
        }
    });

    rowNumber = startIndex + 1;
    document.querySelectorAll('.mobile-account-card[data-type]').forEach(card => {
        card.style.display = 'none';
    });

    filteredMobileCards.forEach((card, index) => {
        if (index >= startIndex && index < endIndex) {
            card.style.display = '';
            const numberSpan = card.querySelector('.row-number');
            if (numberSpan) {
                numberSpan.textContent = rowNumber++;
            }
        } else {
            card.style.display = 'none';
        }
    });

    renderPaginationButtons(totalPages, '.table-pagination');
    renderPaginationButtons(totalPages, '.mobile-pagination');

    const tableCountText = document.querySelector('.table-pagination + .pagination-count');
    if (tableCountText) {
        const showing = filteredRows.length === 0 ? 0 : startIndex + 1;
        const to = Math.min(endIndex, filteredRows.length);
        tableCountText.textContent = `Показано от ${showing} до ${to} из ${filteredRows.length} записей`;
    }

    const mobileCountText = document.querySelector('.mobile-pagination + .pagination-count');
    if (mobileCountText) {
        const showing = filteredMobileCards.length === 0 ? 0 : startIndex + 1;
        const to = Math.min(endIndex, filteredMobileCards.length);
        mobileCountText.textContent = `Показано от ${showing} до ${to} из ${filteredMobileCards.length} записей`;
    }
}

function renderPaginationButtons(totalPages, containerSelector) {
    const paginationContainer = document.querySelector(containerSelector);
    if (!paginationContainer || totalPages === 0) return;

    paginationContainer.innerHTML = '';

    const prevBtn = createPageButton('<i class="bi bi-chevron-left"></i>');
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
        const btn = createPageButton('1');
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
        const btn = createPageButton(i);
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

        const btn = createPageButton(totalPages);
        btn.addEventListener('click', () => goToPage(totalPages));
        paginationContainer.appendChild(btn);
    }

    const nextBtn = createPageButton('<i class="bi bi-chevron-right"></i>');
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

function createPageButton(content) {
    const btn = document.createElement('button');
    btn.className = 'page-btn';
    btn.innerHTML = content;
    btn.type = 'button';
    return btn;
}

function goToPage(page) {
    currentPage = page;
    updatePagination();
    document.querySelector('.accounts-table-card')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
}

function updateEmptyState() {
    const emptyRow = document.querySelector('.accounts-table tbody tr td[colspan]');
    if (emptyRow) {
        emptyRow.parentElement.style.display = filteredRows.length === 0 ? '' : 'none';
    }

    const mobileEmpty = document.querySelector('.mobile-accounts-grid .empty-state');
    if (mobileEmpty) {
        mobileEmpty.style.display = filteredMobileCards.length === 0 ? '' : 'none';
    }
}
