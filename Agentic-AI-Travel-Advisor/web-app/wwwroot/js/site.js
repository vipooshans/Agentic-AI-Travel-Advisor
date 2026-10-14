document.addEventListener('DOMContentLoaded', () => {
    const sidebar = document.querySelector('.admin-sidebar');
    const backdrop = document.querySelector('.sidebar-backdrop');
    const toggle = document.getElementById('sidebarToggle');

    const closeSidebar = () => {
        sidebar?.classList.remove('open');
        backdrop?.classList.remove('show');
    };

    toggle?.addEventListener('click', () => {
        sidebar?.classList.toggle('open');
        backdrop?.classList.toggle('show');
    });
    backdrop?.addEventListener('click', closeSidebar);

    document.querySelectorAll('form').forEach((form) => {
        form.addEventListener('submit', () => {
            const btn = form.querySelector('button[type="submit"], button:not([type])');
            if (!btn || btn.disabled) return;
            btn.disabled = true;
            btn.setAttribute('aria-busy', 'true');
            const label = btn.textContent?.trim() || 'Please wait';
            btn.innerHTML = `<span class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span>${label}`;
        });
    });
});
