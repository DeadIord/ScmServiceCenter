let orderId = null;

document.addEventListener('DOMContentLoaded', function() {
    const mainBody = document.querySelector('.main-body');
    if (mainBody) {
        orderId = mainBody.dataset.orderId;
    }

    const messagesContainer = document.getElementById('messages-container');
    if (messagesContainer) {
        messagesContainer.scrollTop = messagesContainer.scrollHeight;
    }
    
    initializeEventListeners();
});

function initializeEventListeners() {
    const modalChangeBtn = document.getElementById('modal-change-status');
    if (modalChangeBtn) {
        modalChangeBtn.addEventListener('click', function() {
            updateStatus('modal-status-select');
        });
    }
    

    const submitButtons = [
        document.getElementById('submit-quote'),
        document.getElementById('action-submit-quote')
    ];
    
    submitButtons.forEach(btn => {
        if (btn) {
            btn.addEventListener('click', handleSubmitQuote);
        }
    });

    const messageForm = document.getElementById('message-form');
    if (messageForm) {
        messageForm.addEventListener('submit', handleMessageSubmit);
    }
}

function copyToClipboard() {
    const link = document.getElementById('clientLink');
    if (!link) return;
    
    link.select();
    link.setSelectionRange(0, 99999); 
    
    try {
        document.execCommand('copy');
        showToast('Ссылка скопирована', 'bg-success');
    } catch (err) {
        console.error('Failed to copy:', err);
        showToast('Не удалось скопировать', 'bg-danger');
    }
}

function updateStatus(selectId) {
    if (!orderId) {
        showToast('Ошибка: ID заказа не найден', 'bg-danger');
        return;
    }
    
    const selectElement = document.getElementById(selectId);
    if (!selectElement) return;
    
    const status = selectElement.value;
    const form = new URLSearchParams();
    form.append('id', orderId);
    form.append('to', status);
    
    axios.post('/Orders/ChangeStatus', form, {
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' }
    })
    .then(() => {
        showToast('Статус обновлён', 'bg-success');
        setTimeout(() => window.location.reload(), 600);
    })
    .catch(err => {
        console.error('Status update error:', err);
        showToast(err.response?.data?.message ?? 'Ошибка обновления статуса', 'bg-danger');
    });
}

function handleSubmitQuote() {
    if (!orderId) {
        showToast('Ошибка: ID заказа не найден', 'bg-danger');
        return;
    }
    
    const form = new URLSearchParams();
    form.append('orderId', orderId);
    
    axios.post('/Orders/SubmitQuote', form, {
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' }
    })
    .then(() => {
        showToast('Смета отправлена на согласование', 'bg-success');
        setTimeout(() => window.location.reload(), 600);
    })
    .catch(err => {
        console.error('Submit quote error:', err);
        showToast(err.response?.data?.message ?? 'Ошибка отправки сметы', 'bg-danger');
    });
}

function handleMessageSubmit(e) {
    e.preventDefault();
    
    if (!orderId) {
        showToast('Ошибка: ID заказа не найден', 'bg-danger');
        return;
    }
    
    const formData = new FormData(this);
    const text = formData.get('Text');
    
    if (!text || text.trim() === '') {
        showToast('Введите сообщение', 'bg-warning');
        return;
    }
    
    const payload = {
        orderId: orderId,
        text: text.trim()
    };
    
    axios.post('/Messages/Add', payload, {
        headers: { 'Content-Type': 'application/json' }
    })
    .then(response => {
        const message = response.data;
        appendMessage(message);
        this.reset();
        showToast('Сообщение отправлено', 'bg-success');
    })
    .catch(err => {
        console.error('Send message error:', err);
        console.error('Error response:', err.response?.data);
        showToast(err.response?.data?.message ?? 'Не удалось отправить сообщение', 'bg-danger');
    });
}

function appendMessage(message) {
    const container = document.getElementById('messages-container');
    if (!container) return;

    const wrapper = document.createElement('div');
    wrapper.className = `message-wrapper message-${message.fromClient ? 'start' : 'end'}`;

    const bubble = document.createElement('div');
    bubble.className = `message-bubble ${message.fromClient ? 'message-bubble-client' : 'message-bubble-staff'}`;

    const header = document.createElement('div');
    header.className = 'message-header';

    const author = document.createElement('span');
    author.className = 'message-author';
    author.textContent = message.fromClient ? 'Клиент' : 'Сотрудник';

    const timestamp = document.createElement('span');
    timestamp.className = 'message-time';
    const date = new Date(message.atUtc);
    timestamp.textContent = date.toLocaleString('ru-RU', { 
        month: 'short', 
        day: 'numeric', 
        hour: '2-digit', 
        minute: '2-digit' 
    });

    header.appendChild(author);
    header.appendChild(timestamp);

    const text = document.createElement('div');
    text.className = 'message-text';
    text.textContent = message.text;

    bubble.appendChild(header);
    bubble.appendChild(text);
    wrapper.appendChild(bubble);
    container.appendChild(wrapper);

    container.scrollTop = container.scrollHeight;
}

window.copyToClipboard = copyToClipboard;