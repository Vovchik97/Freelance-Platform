async function loadClientUnreadCount() {
    try {
        const resp = await fetch('/TeamProject/GetGroupUnreadCount');
        const data = resp.ok ? await resp.json() : { total: 0, byProject: {} };
        
        window._unreadByProject = cleanByProject(data.byProject);
        
        window.updateClientNavbarBadge();
    } catch (e) {
        console.error('loadClientUnreadCount error', e);
    }
}

window.updateClientNavbarBadge = function () {
    const total = Object.values(window._unreadByProject || {})
        .reduce((sum, n) => sum + n, 0);
    
    const badge = document.getElementById('clientUnreadCount');
    if (!badge) return;
    
    badge.innerHTML = '';
    
    if (total > 0) {
        badge.textContent = total;
        badge.style.display = 'inline';
    } else {
        badge.style.display = 'none';
    }
};

function cleanByProject(obj) {
    if (!obj || typeof obj !== 'object') return {};
    const result = {};
    for (const [key, value] of Object.entries(obj)) {
        if (key.startsWith('$')) continue;
        const num = parseInt(value);
        if (!isNaN(num) && num > 0) result[key] = num;
    }
    return result;
}

loadClientUnreadCount();
setInterval(loadClientUnreadCount, 60000);