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

// Global phone number input filter - digits only (no alphabets or special characters)
document.addEventListener('input', function (e) {
    const target = e.target;
    if (target && (target.type === 'tel' || target.name === 'Phone' || target.id === 'Phone' || target.getAttribute('asp-for') === 'Phone')) {
        target.value = target.value.replace(/[^0-9]/g, '');
    }
});

// Global AJAX Status Toggle
async function toggleStatus(controller, id, btnElement) {
    if (!id || id <= 0) return;

    btnElement.disabled = true;
    const originalContent = btnElement.innerHTML;
    btnElement.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>';

    try {
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        const token = tokenInput ? tokenInput.value : '';

        let response = await fetch(`/api/${controller}/toggle-status/${id}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest',
                'Accept': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify({ id: id })
        });

        // Graceful fallback to MVC controller endpoint if API returned 404/405
        if (!response.ok && (response.status === 404 || response.status === 405)) {
            response = await fetch(`/${controller}/ToggleStatus/${id}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest',
                    'Accept': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({ id: id })
            });
        }

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

// Native Browser Popup Interceptors: Route all alerts & confirms directly to UI notifications
window.alert = function (message) {
    if (message) {
        showToast(String(message), 'error');
    }
};

window.confirm = function () {
    // Prevent blocking browser confirm popups
    return true;
};

// Global Confirmation & Success Modals (Kept as non-modal resolution for backward compatibility)
function showConfirmDialog(title, message, confirmBtnText = 'Yes, Delete', cancelBtnText = 'Cancel') {
    return Promise.resolve(true);
}

function showSuccessDialog(title, message, okBtnText = 'OK') {
    return Promise.resolve(true);
}

// Global AJAX Row Delete with Non-Modal Inline UI Confirmation
async function deleteItem(controller, id, itemName, rowElement) {
    if (!id || id <= 0) return;

    if (!rowElement && typeof event !== 'undefined' && event && event.target) {
        rowElement = event.target.closest('tr');
    }

    const btn = (typeof event !== 'undefined' && event && event.target)
        ? (event.target.closest('button') || event.target)
        : null;

    if (!itemName && rowElement) {
        const cells = rowElement.querySelectorAll('td');
        if (cells.length >= 3) {
            itemName = cells[2]?.innerText?.trim();
        }
    }

    const entityType = controller ? (controller.charAt(0).toUpperCase() + controller.slice(1).toLowerCase()) : 'Record';
    const hasName = itemName && itemName.trim() && itemName.trim().toLowerCase() !== controller.toLowerCase();
    const cleanName = hasName ? itemName.trim() : '';
    const displayName = cleanName ? `${entityType} "${cleanName}"` : entityType;

    // Direct Inline UI Button Confirmation (No Modal, No Popups)
    if (btn && btn.tagName === 'BUTTON' && !btn.dataset.confirming) {
        btn.dataset.confirming = 'true';
        const originalHtml = btn.innerHTML;
        const originalClasses = btn.className;
        btn.className = 'btn btn-sm btn-danger text-nowrap';
        btn.innerHTML = '<i class="bi bi-check2 me-1"></i>Confirm?';

        const timer = setTimeout(() => {
            btn.innerHTML = originalHtml;
            btn.className = originalClasses;
            delete btn.dataset.confirming;
            delete btn.dataset.timerId;
        }, 4000);

        btn.dataset.timerId = timer;
        return;
    }

    if (btn && btn.dataset.timerId) {
        clearTimeout(parseInt(btn.dataset.timerId, 10));
        delete btn.dataset.timerId;
        delete btn.dataset.confirming;
        btn.disabled = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1" role="status"></span>';
    }

    try {
        const tokenInput = document.querySelector('#globalAntiForgeryForm input[name="__RequestVerificationToken"]')
            || document.querySelector('input[name="__RequestVerificationToken"]');
        const token = tokenInput ? tokenInput.value : '';

        let isSuccess = false;
        let message = '';

        try {
            const formData = new URLSearchParams();
            if (token) formData.append('__RequestVerificationToken', token);

            const response = await fetch(`/${controller}/Delete/${id}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token
                },
                body: formData.toString()
            });

            if (response.ok) {
                const json = await response.json().catch(() => null);
                if (json) {
                    isSuccess = json.success;
                    message = json.message;
                }
            }
        } catch {
            // fallback to API
        }

        if (!isSuccess) {
            const apiRes = await fetch(`/api/${controller}/delete-${controller.toLowerCase()}/${id}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest',
                    'Accept': 'application/json'
                },
                body: JSON.stringify({ id: id })
            });

            if (apiRes.ok) {
                const apiJson = await apiRes.json().catch(() => null);
                if (apiJson) {
                    isSuccess = apiJson.success !== undefined ? apiJson.success : (apiJson.Status && apiJson.Status.Code === 0);
                    message = apiJson.message || (apiJson.Data && apiJson.Data.Message) || (apiJson.Status && apiJson.Status.ReturnMessage);
                }
            }
        }

        if (isSuccess) {
            const successMsg = message || `${displayName} deleted successfully.`;

            if (rowElement) {
                rowElement.style.transition = 'all 0.3s ease';
                rowElement.style.opacity = '0';
                rowElement.style.transform = 'translateX(20px)';
                setTimeout(() => {
                    const tbody = rowElement.closest('tbody') || document.querySelector('table tbody');
                    rowElement.remove();

                    if (tbody) {
                        const rows = tbody.querySelectorAll('tr:not(#noRecordsRow)');
                        if (rows.length === 0) {
                            tbody.innerHTML = `
                                <tr>
                                    <td colspan="10" class="text-center py-4 text-muted">
                                        <em>No records found.</em>
                                    </td>
                                </tr>
                            `;
                        } else {
                            let startIdx = 1;
                            const pageSpan = document.getElementById('currentPageSpan');
                            if (pageSpan && pageSpan.innerText) {
                                const pNo = parseInt(pageSpan.innerText, 10);
                                if (pNo > 1) startIdx = (pNo - 1) * 10 + 1;
                            }
                            rows.forEach((r, idx) => {
                                const firstCell = r.querySelector('td:first-child');
                                if (firstCell) {
                                    firstCell.innerText = (startIdx + idx);
                                }
                            });
                        }
                    }
                }, 250);
            }

            showToast(successMsg, 'success');
        } else {
            showToast(message || 'Failed to delete.', 'error');
            if (btn) {
                btn.disabled = false;
                btn.innerText = 'Delete';
            }
        }
    } catch (err) {
        console.error('Error deleting item:', err);
        showToast('Error deleting item: ' + err.message, 'error');
        if (btn) {
            btn.disabled = false;
            btn.innerText = 'Delete';
        }
    }
}

// Global alias for deleteRecord
window.deleteRecord = function (controller, id, element) {
    const row = element ? (element.closest ? element.closest('tr') : element) : null;
    let name = '';
    if (row) {
        const nameCell = row.querySelector('.badge-brand, .badge-category, td:nth-child(3)');
        if (nameCell) name = nameCell.innerText.trim();
    }
    return deleteItem(controller, id, name, row);
};

// Dedicated Customer Deletion Handler with Direct Inline Button Confirmation & Animated Toast Notification
window.deleteCustomer = async function (id, name, rowElement) {
    if (!id || id <= 0) return;

    if (!rowElement && typeof event !== 'undefined' && event && event.target) {
        rowElement = event.target.closest('tr');
    }

    const btn = (typeof event !== 'undefined' && event && event.target)
        ? (event.target.closest('button') || event.target)
        : null;

    const cleanName = (name || '').trim();
    const displayName = cleanName ? `"${cleanName}"` : 'Customer';

    // Direct Inline UI Button Confirmation (No Modal, No Popups)
    if (btn && btn.tagName === 'BUTTON' && !btn.dataset.confirming) {
        btn.dataset.confirming = 'true';
        const originalHtml = btn.innerHTML;
        const originalClasses = btn.className;
        btn.className = 'btn btn-sm btn-danger text-nowrap';
        btn.innerHTML = '<i class="bi bi-check2 me-1"></i>Confirm?';

        const timer = setTimeout(() => {
            btn.innerHTML = originalHtml;
            btn.className = originalClasses;
            delete btn.dataset.confirming;
            delete btn.dataset.timerId;
        }, 4000);

        btn.dataset.timerId = timer;
        return;
    }

    if (btn && btn.dataset.timerId) {
        clearTimeout(parseInt(btn.dataset.timerId, 10));
        delete btn.dataset.timerId;
        delete btn.dataset.confirming;
        btn.disabled = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1" role="status"></span>';
    }

    try {
        const tokenInput = document.querySelector('#customerAntiForgeryForm input[name="__RequestVerificationToken"]')
            || document.querySelector('input[name="__RequestVerificationToken"]');
        const token = tokenInput ? tokenInput.value : '';

        const formData = new URLSearchParams();
        formData.append('id', id);
        if (token) formData.append('__RequestVerificationToken', token);

        const response = await fetch(`/Customer/Delete/${id}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'X-Requested-With': 'XMLHttpRequest',
                'RequestVerificationToken': token
            },
            body: formData.toString()
        });

        const json = await response.json().catch(() => null);

        if (response.ok && json && json.success) {
            if (rowElement) {
                rowElement.style.transition = 'all 0.3s ease';
                rowElement.style.opacity = '0';
                rowElement.style.transform = 'translateX(20px)';
                setTimeout(() => {
                    const tbody = rowElement.closest('tbody') || document.querySelector('#customerTable tbody');
                    rowElement.remove();

                    if (tbody) {
                        const rows = tbody.querySelectorAll('tr:not(#noRecordsRow)');
                        if (rows.length === 0) {
                            tbody.innerHTML = `
                                <tr id="noRecordsRow">
                                    <td colspan="8" class="text-center py-5 text-muted">
                                        <i class="bi bi-people fs-1 d-block mb-2 text-muted"></i>
                                        <em>No customers registered yet.</em>
                                    </td>
                                </tr>
                            `;
                        } else {
                            rows.forEach((r, idx) => {
                                const firstCell = r.querySelector('td:first-child');
                                if (firstCell) {
                                    firstCell.innerText = (idx + 1);
                                }
                            });
                        }
                    }
                }, 250);
            }

            const successMsg = cleanName
                ? `Customer "${cleanName}" deleted successfully.`
                : 'Customer deleted successfully.';
            showToast(successMsg, 'success');
        } else {
            const errorMsg = (json && json.message) ? json.message : 'Failed to delete customer.';
            showToast(errorMsg, 'error');
            if (btn) {
                btn.disabled = false;
                btn.innerHTML = '<i class="bi bi-trash me-1"></i>Delete';
            }
        }
    } catch (err) {
        console.error('Error deleting customer:', err);
        showToast('Error deleting customer: ' + err.message, 'error');
        if (btn) {
            btn.disabled = false;
            btn.innerHTML = '<i class="bi bi-trash me-1"></i>Delete';
        }
    }
};

// Dedicated Supplier Deletion Handler with Direct Inline Button Confirmation & Animated Toast Notification
window.deleteSupplier = async function (id, name, rowElement) {
    if (!id || id <= 0) return;

    if (!rowElement && typeof event !== 'undefined' && event && event.target) {
        rowElement = event.target.closest('tr');
    }

    const btn = (typeof event !== 'undefined' && event && event.target)
        ? (event.target.closest('button') || event.target)
        : null;

    const cleanName = (name || '').trim();
    const displayName = cleanName ? `"${cleanName}"` : 'Supplier';

    // Direct Inline UI Button Confirmation (No Modal, No Popups)
    if (btn && btn.tagName === 'BUTTON' && !btn.dataset.confirming) {
        btn.dataset.confirming = 'true';
        const originalHtml = btn.innerHTML;
        const originalClasses = btn.className;
        btn.className = 'btn btn-sm btn-danger text-nowrap';
        btn.innerHTML = '<i class="bi bi-check2 me-1"></i>Confirm?';

        const timer = setTimeout(() => {
            btn.innerHTML = originalHtml;
            btn.className = originalClasses;
            delete btn.dataset.confirming;
            delete btn.dataset.timerId;
        }, 4000);

        btn.dataset.timerId = timer;
        return;
    }

    if (btn && btn.dataset.timerId) {
        clearTimeout(parseInt(btn.dataset.timerId, 10));
        delete btn.dataset.timerId;
        delete btn.dataset.confirming;
        btn.disabled = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1" role="status"></span>';
    }

    try {
        const tokenInput = document.querySelector('#supplierAntiForgeryForm input[name="__RequestVerificationToken"]')
            || document.querySelector('input[name="__RequestVerificationToken"]');
        const token = tokenInput ? tokenInput.value : '';

        const formData = new URLSearchParams();
        formData.append('id', id);
        if (token) formData.append('__RequestVerificationToken', token);

        const response = await fetch(`/Supplier/Delete/${id}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'X-Requested-With': 'XMLHttpRequest',
                'RequestVerificationToken': token
            },
            body: formData.toString()
        });

        const json = await response.json().catch(() => null);

        if (response.ok && json && json.success) {
            if (rowElement) {
                rowElement.style.transition = 'all 0.3s ease';
                rowElement.style.opacity = '0';
                rowElement.style.transform = 'translateX(20px)';
                setTimeout(() => {
                    const tbody = rowElement.closest('tbody') || document.querySelector('#supplierTable tbody');
                    rowElement.remove();

                    if (tbody) {
                        const rows = tbody.querySelectorAll('tr:not(#noRecordsRow)');
                        if (rows.length === 0) {
                            tbody.innerHTML = `
                                <tr id="noRecordsRow">
                                    <td colspan="9" class="text-center py-5 text-muted">
                                        <i class="bi bi-truck fs-1 d-block mb-2 text-muted"></i>
                                        <em>No suppliers registered yet.</em>
                                    </td>
                                </tr>
                            `;
                        } else {
                            rows.forEach((r, idx) => {
                                const firstCell = r.querySelector('td:first-child');
                                if (firstCell) {
                                    firstCell.innerText = (idx + 1);
                                }
                            });
                        }
                    }
                }, 250);
            }

            const successMsg = cleanName
                ? `Supplier "${cleanName}" deleted successfully.`
                : 'Supplier deleted successfully.';
            showToast(successMsg, 'success');
        } else {
            const errorMsg = (json && json.message) ? json.message : 'Failed to delete supplier.';
            showToast(errorMsg, 'error');
            if (btn) {
                btn.disabled = false;
                btn.innerHTML = '<i class="bi bi-trash me-1"></i>Delete';
            }
        }
    } catch (err) {
        console.error('Error deleting supplier:', err);
        showToast('Error deleting supplier: ' + err.message, 'error');
        if (btn) {
            btn.disabled = false;
            btn.innerHTML = '<i class="bi bi-trash me-1"></i>Delete';
        }
    }
};

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

    const eyeOff = btn ? btn.querySelector('#eyeOffIcon') : document.getElementById('eyeOffIcon');
    const eyeOn = btn ? btn.querySelector('#eyeOnIcon') : document.getElementById('eyeOnIcon');

    if (input.type === 'password') {
        input.type = 'text';
        if (eyeOff && eyeOn) {
            eyeOff.style.display = 'none';
            eyeOn.style.display = 'inline';
        }
        if (btn) {
            btn.setAttribute('title', 'Hide Password');
            btn.setAttribute('aria-label', 'Hide Password');
        }
    } else {
        input.type = 'password';
        if (eyeOff && eyeOn) {
            eyeOff.style.display = 'inline';
            eyeOn.style.display = 'none';
        }
        if (btn) {
            btn.setAttribute('title', 'Show Password');
            btn.setAttribute('aria-label', 'Show Password');
        }
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
    const savedTheme = localStorage.getItem('theme') || 'dark';
    applyTheme(savedTheme);

    document.addEventListener('click', function (e) {
        const toggleBtn = e.target.closest('#themeToggleBtn, .theme-toggle-btn');
        if (toggleBtn) {
            e.preventDefault();
            const currentTheme = document.documentElement.getAttribute('data-theme') || 'dark';
            const nextTheme = currentTheme === 'dark' ? 'light' : 'dark';
            applyTheme(nextTheme);
        }
    });
}

function applyTheme(theme) {
    document.documentElement.setAttribute('data-theme', theme);
    document.documentElement.setAttribute('data-bs-theme', theme);
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



// Global Image Upload Live Preview Handler
function previewSelectedImage(inputElement, previewContainerId, previewImgId) {
    if (!inputElement || !inputElement.files || !inputElement.files[0]) {
        return;
    }
    const file = inputElement.files[0];
    const container = document.getElementById(previewContainerId);
    const img = document.getElementById(previewImgId);

    if (file && img && container) {
        const reader = new FileReader();
        reader.onload = function (e) {
            img.src = e.target.result;
            container.style.display = 'block';
        };
        reader.readAsDataURL(file);
    }
}
