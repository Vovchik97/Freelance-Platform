async function loadNotificationCount() {
    try {
        const [inviteResp, unreadResp] = await Promise.all([
            fetch('/TeamProject/GetInviteCount'),
            fetch('/TeamProject/GetGroupUnreadCount')
        ]);

        const inviteData = inviteResp.ok ? await inviteResp.json() : { count: 0 };
        const unreadData = unreadResp.ok ? await unreadResp.json() : { total: 0 };

        window._unreadByProject = cleanByProject(unreadData.byProject);
        window._pendingInvites  = parseInt(inviteData.count) || 0;

        window.updateNavbarBadge();
    } catch (e) {
        console.error('loadNotificationCount error', e);
    }
}

window.updateNavbarBadge = function () {
    const totalUnread = Object.values(window._unreadByProject || {})
        .reduce((sum, n) => sum + n, 0);
    const total = totalUnread + (window._pendingInvites || 0);

    const badge = document.getElementById('inviteCount');
    if (!badge) return;
    
    badge.textContent = '';

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

loadNotificationCount();
setInterval(loadNotificationCount, 60000);