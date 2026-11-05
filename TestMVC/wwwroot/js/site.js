/**
 * TestMVC - Smooth SPA Navigation & AJAX Client Handlers
 */

// Helper to get global Anti-Forgery Token
function getAntiForgeryToken() {
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenInput ? tokenInput.value : '';
}

// Toast Notification Manager
function showToast(message, type = 'success') {
    const container = document.getElementById('toastContainer');
    if (!container) return;

    const toast = document.createElement('div');
    toast.className = `custom-toast custom-toast-${type}`;
    toast.setAttribute('role', 'alert');
    toast.setAttribute('aria-live', 'assertive');
    toast.setAttribute('title', 'Click to dismiss');

    let iconSvg = '<svg width="14" height="14" viewBox="0 0 16 16" fill="currentColor"><path d="M13.485 1.929a1 1 0 0 1 1.414 1.414l-8 8a1 1 0 0 1-1.414 0l-4-4a1 1 0 1 1 1.414-1.414L6 9.086l7.485-7.157z"/></svg>';
    if (type === 'error') {
        iconSvg = '<svg width="14" height="14" viewBox="0 0 16 16" fill="currentColor"><path d="M4.646 4.646a.5.5 0 0 1 .708 0L8 7.293l2.646-2.647a.5.5 0 0 1 .708.708L8.707 8l2.647 2.646a.5.5 0 0 1-.708.708L8 8.707l-2.646 2.647a.5.5 0 0 1-.708-.708L7.293 8 4.646 5.354a.5.5 0 0 1 0-.708z"/></svg>';
    } else if (type === 'info') {
        iconSvg = '<svg width="14" height="14" viewBox="0 0 16 16" fill="currentColor"><path d="M8 15A7 7 0 1 1 8 1a7 7 0 0 1 0 14zm0 1A8 8 0 1 0 8 0a8 8 0 0 0 0 16z"/><path d="m8.93 6.588-2.29.287-.082.38.45.083c.294.07.352.176.288.469l-.738 3.468c-.194.897.105 1.319.808 1.319.545 0 1.178-.252 1.465-.598l.088-.416c-.2.176-.492.246-.686.246-.275 0-.375-.193-.304-.533L8.93 6.588zM9 4.5a1 1 0 1 1-2 0 1 1 0 0 1 2 0z"/></svg>';
    }

    toast.innerHTML = `
        <div class="d-flex align-items-center gap-2 flex-grow-1">
            <span class="custom-toast-icon">${iconSvg}</span>
            <span class="fw-medium">${escapeHtml(message)}</span>
        </div>
        <button type="button" class="btn-close btn-close-sm" style="font-size: 0.72rem;" aria-label="Close"></button>
        <div class="toast-progress-bar"></div>
    `;

    let isDismissed = false;
    let autoDismissTimer = null;

    function dismissToast() {
        if (isDismissed) return;
        isDismissed = true;

        if (autoDismissTimer) {
            clearTimeout(autoDismissTimer);
            autoDismissTimer = null;
        }

        // Apply exit animation class
        toast.classList.remove('show');
        toast.classList.add('hide');

        // Remove element from DOM once exit transition ends
        setTimeout(() => {
            if (toast.parentNode) {
                toast.remove();
            }
        }, 350);
    }

    // Disappear immediately when clicked anywhere on the toast
    toast.addEventListener('click', function () {
        dismissToast();
    });

    // Close button click handler
    const closeBtn = toast.querySelector('.btn-close');
    if (closeBtn) {
        closeBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            dismissToast();
        });
    }

    container.appendChild(toast);

    // Trigger smooth entrance animation
    requestAnimationFrame(() => {
        requestAnimationFrame(() => {
            toast.classList.add('show');
        });
    });

    // Auto dismiss after exactly 3 seconds (3000ms)
    autoDismissTimer = setTimeout(() => {
        dismissToast();
    }, 3000);
}

// HTML escape utility
function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.innerText = text;
    return div.innerHTML;
}

// Global AJAX Status Toggle
async function toggleStatus(controller, id, btnElement) {
    if (!id || id <= 0) return;

    btnElement.disabled = true;
    const originalContent = btnElement.innerHTML;
    btnElement.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>';

    try {
        const response = await fetch(`/api/${controller}/toggle-status/${id}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest',
                'Accept': 'application/json'
            },
            body: JSON.stringify({ id: id })
        });

        if (!response.ok) {
            throw new Error(`Server returned error ${response.status}`);
        }

        const result = await response.json();

        // Handle both standard JSON and Data/Status dictionary formats
        const isSuccess = result.success !== undefined ? result.success : (result.Status && result.Status.Code === 0);
        const isActive = result.isActive !== undefined ? result.isActive : (result.Data && result.Data.IsActive);
        const message = result.message || (result.Data && result.Data.Message) || (result.Status && result.Status.ReturnMessage) || 'Status updated successfully.';

        if (isSuccess) {
            if (isActive) {
                btnElement.className = 'btn btn-sm btn-status-on px-2 py-1';
                btnElement.innerHTML = '<i class="bi bi-toggle-on me-1"></i> ON';
                btnElement.setAttribute('title', 'Click to turn OFF (Inactive)');
            } else {
                btnElement.className = 'btn btn-sm btn-status-off px-2 py-1';
                btnElement.innerHTML = '<i class="bi bi-toggle-off me-1"></i> OFF';
                btnElement.setAttribute('title', 'Click to turn ON (Active)');
            }

            showToast(message, 'success');
        } else {
            btnElement.innerHTML = originalContent;
            showToast(message || 'Failed to update status.', 'error');
        }
    } catch (err) {
        console.error('Error toggling status:', err);
        btnElement.innerHTML = originalContent;
        showToast('Error updating status: ' + err.message, 'error');
    } finally {
        btnElement.disabled = false;
    }
}

// Global AJAX Row Delete
async function deleteItem(controller, id, itemName, rowElement) {
    if (!id || id <= 0) return;

    if (!confirm(`Are you sure you want to delete '${itemName}'?`)) {
        return;
    }

    try {
        const response = await fetch(`/api/${controller}/delete-${controller.toLowerCase()}/${id}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest',
                'Accept': 'application/json'
            },
            body: JSON.stringify({ id: id })
        });

        if (!response.ok) {
            throw new Error(`Server returned error ${response.status}`);
        }

        const result = await response.json();
        const isSuccess = result.success !== undefined ? result.success : (result.Status && result.Status.Code === 0);
        const message = result.message || (result.Data && result.Data.Message) || (result.Status && result.Status.ReturnMessage) || `${itemName} deleted successfully.`;

        if (isSuccess) {
            showToast(message, 'success');

            if (rowElement) {
                rowElement.style.transition = 'all 0.3s ease';
                rowElement.style.opacity = '0';
                rowElement.style.transform = 'translateX(20px)';
                setTimeout(() => {
                    rowElement.remove();
                    // Check if table is now empty
                    const tbody = document.querySelector('table tbody');
                    if (tbody && tbody.querySelectorAll('tr').length === 0) {
                        tbody.innerHTML = `
                            <tr>
                                <td colspan="10" class="text-center py-4 text-muted">
                                    <em>No records found.</em>
                                </td>
                            </tr>
                        `;
                    }
                }, 300);
            }
        } else {
            showToast(message || 'Failed to delete.', 'error');
        }
    } catch (err) {
        console.error('Error deleting item:', err);
        showToast('Error deleting item: ' + err.message, 'error');
    }
}

// Seamless SPA Dynamic Navigation
async function spaNavigate(url, pushState = true) {
    const progressBar = document.getElementById('appProgressBar');
    const mainContent = document.getElementById('mainContent');

    if (progressBar) {
        progressBar.style.opacity = '1';
        progressBar.style.width = '30%';
    }

    try {
        const response = await fetch(url, {
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        if (progressBar) progressBar.style.width = '70%';

        if (!response.ok) {
            window.location.href = url;
            return;
        }

        const htmlText = await response.text();
        const parser = new DOMParser();
        const doc = parser.parseFromString(htmlText, 'text/html');

        // If returned content is the Login Page (user logged out or session expired), force full-page reload
        if (doc.querySelector('.login-wrapper') || doc.querySelector('.login-card') || doc.querySelector('#loginForm')) {
            window.location.href = '/Account/Login';
            return;
        }

        const newMain = doc.querySelector('#mainContent') || doc.querySelector('main');
        const newTitle = doc.querySelector('title');

        if (newMain && mainContent) {
            // Apply crossfade transition
            mainContent.classList.remove('page-fade-in');
            void mainContent.offsetWidth; // Trigger reflow

            mainContent.innerHTML = newMain.innerHTML;
            mainContent.classList.add('page-fade-in');

            if (newTitle) {
                document.title = newTitle.innerText;
            }

            if (pushState) {
                window.history.pushState({ path: url }, '', url);
            }

            updateActiveNav(url);
            executeInlineScripts(mainContent);
        } else {
            window.location.href = url;
        }
    } catch (err) {
        console.error('SPA navigation failed, falling back to standard load:', err);
        window.location.href = url;
    } finally {
        if (progressBar) {
            progressBar.style.width = '100%';
            setTimeout(() => {
                progressBar.style.opacity = '0';
                setTimeout(() => { progressBar.style.width = '0%'; }, 200);
            }, 150);
        }
    }
}

// Re-execute scripts inside newly injected dynamic content
function executeInlineScripts(container) {
    const scripts = container.querySelectorAll('script');
    scripts.forEach(script => {
        const newScript = document.createElement('script');
        if (script.src) {
            newScript.src = script.src;
        } else {
            newScript.textContent = script.textContent;
        }
        document.body.appendChild(newScript);
        newScript.remove();
    });
}

// Update Active Navbar Link
function updateActiveNav(url) {
    const currentPath = new URL(url, window.location.origin).pathname.toLowerCase();
    const navLinks = document.querySelectorAll('#mainNavBar .sidebar-nav-link');

    navLinks.forEach(link => {
        const href = link.getAttribute('href');
        if (!href) return;
        const linkPath = new URL(href, window.location.origin).pathname.toLowerCase();

        let isMatch = false;
        if (linkPath === '/' || linkPath === '/home' || linkPath === '/home/index') {
            isMatch = (currentPath === '/' || currentPath === '/home' || currentPath === '/home/index');
        } else if (linkPath.startsWith('/brand')) {
            isMatch = currentPath.startsWith('/brand');
        } else if (linkPath.startsWith('/category')) {
            isMatch = currentPath.startsWith('/category');
        } else if (linkPath.startsWith('/product')) {
            isMatch = currentPath.startsWith('/product');
        } else {
            isMatch = (linkPath === currentPath);
        }

        if (isMatch) {
            link.classList.add('active');
        } else {
            link.classList.remove('active');
        }
    });
}

// Global Password Visibility Toggle
window.togglePasswordVisibility = function (inputId, btn) {
    const input = document.getElementById(inputId);
    if (!input) return;

    if (input.type === 'password') {
        input.type = 'text';
        if (btn) {
            btn.innerHTML = '🙈';
            btn.setAttribute('title', 'Hide Password');
            btn.setAttribute('aria-label', 'Hide Password');
        }
    } else {
        input.type = 'password';
        if (btn) {
            btn.innerHTML = '👁️';
            btn.setAttribute('title', 'Show Password');
            btn.setAttribute('aria-label', 'Show Password');
        }
    }
};

// Global User Approval Toggle
window.toggleUserApproval = async function (userId, btn) {
    if (!userId || userId <= 0) return;
    btn.disabled = true;
    const originalText = btn.innerHTML;
    btn.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>';

    try {
        const response = await fetch(`/api/Auth/toggle-approval/${userId}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest',
                'Accept': 'application/json'
            }
        });

        if (!response.ok) throw new Error(`HTTP ${response.status}`);
        const result = await response.json();

        if (result.success) {
            showToast(result.message || 'User approval updated.', 'success');
            // Smoothly refresh current SPA view
            spaNavigate(window.location.href, false);
        } else {
            showToast(result.message || 'Failed to update approval.', 'error');
            btn.innerHTML = originalText;
        }
    } catch (err) {
        console.error('Error toggling approval:', err);
        showToast('Error updating approval: ' + err.message, 'error');
        btn.innerHTML = originalText;
    } finally {
        btn.disabled = false;
    }
};

// Delegated click listener for password visibility toggling
document.addEventListener('click', function (e) {
    const toggleBtn = e.target.closest('[data-toggle-password], .toggle-password-btn');
    if (toggleBtn) {
        const targetId = toggleBtn.getAttribute('data-toggle-password') || toggleBtn.getAttribute('data-target');
        if (targetId) {
            e.preventDefault();
            window.togglePasswordVisibility(targetId, toggleBtn);
        }
    }
});

// Mobile Sidebar Toggle Handlers
document.addEventListener('click', function (e) {
    const toggleBtn = e.target.closest('#mobileSidebarToggle');
    const sidebar = document.getElementById('appSidebar');

    if (toggleBtn && sidebar) {
        sidebar.classList.toggle('show-mobile');
    } else if (sidebar && sidebar.classList.contains('show-mobile') && !e.target.closest('#appSidebar')) {
        sidebar.classList.remove('show-mobile');
    }
});

// Intercept SPA navigation link clicks site-wide
document.addEventListener('click', function (e) {
    const link = e.target.closest('a');
    if (!link) return;

    const href = link.getAttribute('href');
    if (!href || href.startsWith('#') || href.startsWith('javascript:') || href.startsWith('mailto:') || href.startsWith('tel:') || link.target === '_blank') {
        return;
    }

    // Check if it's an internal application link
    try {
        const linkUrl = new URL(href, window.location.origin);
        if (linkUrl.origin === window.location.origin) {
            const path = linkUrl.pathname.toLowerCase();
            // Do NOT intercept Logout, Login, or any links marked with data-no-spa
            if (link.hasAttribute('data-no-spa') || path.includes('/account/logout') || path.includes('/account/login')) {
                return;
            }

            e.preventDefault();
            // Close mobile sidebar if open
            const sidebar = document.getElementById('appSidebar');
            if (sidebar) sidebar.classList.remove('show-mobile');
            
            spaNavigate(linkUrl.href, true);
        }
    } catch (ex) {
        // Normal fallback
    }
});

// Handle Browser Back / Forward History navigation
window.addEventListener('popstate', function (e) {
    spaNavigate(window.location.href, false);
});

// Global Theme Switcher (Light / Dark Mode)
function initThemeSwitcher() {
    const savedTheme = localStorage.getItem('theme') || 'light';
    applyTheme(savedTheme);

    document.addEventListener('click', function (e) {
        const toggleBtn = e.target.closest('#themeToggleBtn, .theme-toggle-btn');
        if (toggleBtn) {
            e.preventDefault();
            const currentTheme = document.documentElement.getAttribute('data-theme') || 'light';
            const nextTheme = currentTheme === 'dark' ? 'light' : 'dark';
            applyTheme(nextTheme);
        }
    });
}

function applyTheme(theme) {
    document.documentElement.setAttribute('data-theme', theme);
    localStorage.setItem('theme', theme);

    document.querySelectorAll('.theme-mode-label').forEach(el => {
        el.innerText = theme === 'dark' ? 'Dark Mode' : 'Light Mode';
    });
}

// Initialize on first load
document.addEventListener('DOMContentLoaded', function () {
    initThemeSwitcher();
    updateActiveNav(window.location.href);
});


