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

    let icon = '✓';
    if (type === 'error') icon = '✕';
    if (type === 'info') icon = 'ℹ';

    toast.innerHTML = `
        <div class="d-flex align-items-center gap-2">
            <span class="fw-bold">${icon}</span>
            <span>${escapeHtml(message)}</span>
        </div>
        <button type="button" class="btn-close btn-close-sm" style="font-size: 0.75rem;" onclick="this.parentElement.remove()"></button>
    `;

    container.appendChild(toast);

    // Trigger animation
    setTimeout(() => {
        toast.classList.add('show');
    }, 10);

    // Auto dismiss after 3.5 seconds
    setTimeout(() => {
        toast.classList.remove('show');
        setTimeout(() => toast.remove(), 300);
    }, 3500);
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

// Initialize active state on first load
document.addEventListener('DOMContentLoaded', function () {
    updateActiveNav(window.location.href);
});


