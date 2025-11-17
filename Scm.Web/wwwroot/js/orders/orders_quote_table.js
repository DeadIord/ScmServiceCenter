const selectAllQuoteLines = document.getElementById('selectAllQuoteLines');
if (selectAllQuoteLines) {
    selectAllQuoteLines.addEventListener('change', function () {
        const checkboxes = document.querySelectorAll('.quote-line-row input[type="checkbox"]');
        checkboxes.forEach(cb => cb.checked = this.checked);
    });
}

const addLineForm = document.getElementById('add-line-form');
if (addLineForm) {
    addLineForm.addEventListener('submit', function () {
        const qtyInput = addLineForm.querySelector('input[name="Qty"]');
        const priceInput = addLineForm.querySelector('input[name="Price"]');

        if (qtyInput) {
            qtyInput.value = normalizeDecimalValue(qtyInput.value);
        }

        if (priceInput) {
            priceInput.value = normalizeDecimalValue(priceInput.value);
        }
    });
}

function normalizeDecimalValue(rawValue) {
    const trimmedValue = (rawValue ?? '').toString().trim();
    if (trimmedValue === '') {
        return '';
    }

    const normalizedValue = trimmedValue.replace(/\./g, ',');
    return normalizedValue;
}

function deleteQuoteLine(lineId) {
    if (!confirm('Удалить эту позицию из сметы?')) {
        return;
    }

    axios.post('/Orders/DeleteQuoteLine', { lineId })
        .then(() => {
            showToast('Позиция удалена', 'bg-success');
            setTimeout(() => window.location.reload(), 400);
        })
        .catch(err => {
            const message = err.response?.data?.message ?? 'Ошибка удаления';
            showToast(message, 'bg-danger');
        });
}
