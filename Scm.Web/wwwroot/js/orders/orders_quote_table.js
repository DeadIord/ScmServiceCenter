document.getElementById('selectAllQuoteLines')?.addEventListener('change', function() {
    const checkboxes = document.querySelectorAll('.quote-line-row input[type="checkbox"]');
    checkboxes.forEach(cb => cb.checked = this.checked);
});

function deleteQuoteLine(lineId) {
    if (!confirm('Удалить эту позицию из сметы?')) return;
    
    axios.post('@Url.Action("DeleteQuoteLine", "Orders")', { lineId })
        .then(() => {
            showToast('Позиция удалена', 'bg-success');
            setTimeout(() => window.location.reload(), 400);
        })
        .catch(err => {
            showToast(err.response?.data?.message ?? 'Ошибка удаления', 'bg-danger');
        });
}

