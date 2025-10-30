document.getElementById('selectAll')?.addEventListener('change', function() {
    const checkboxes = document.querySelectorAll('.order-row input[type="checkbox"]');
    checkboxes.forEach(cb => cb.checked = this.checked);
});

document.querySelectorAll('.order-row input[type="checkbox"]').forEach(checkbox => {
    checkbox.addEventListener('change', function() {
        this.closest('tr').classList.toggle('table-active', this.checked);
    });
});