/**
 * TestMVC - Smooth SPA Navigation & AJAX Client Handlers
 */

// Helper to get global Anti-Forgery Token
function getAntiForgeryToken() {
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenInput ? tokenInput.value : '';
}

// Unified API Fetch helper ensuring consistent headers, credentials, JSON/FormData handling,
// and clean visibility in Chrome DevTools Network (Fetch/XHR) + Copy as cURL support.
window.apiFetch = async function (url, options = {}) {
    const token = getAntiForgeryToken();
    const method = (options.method || 'GET').toUpperCase();
    const headers = Object.assign({
        'Accept': 'application/json',
        'X-Requested-With': 'XMLHttpRequest'
    }, options.headers || {});

    if (token && method !== 'GET' && method !== 'HEAD') {
        headers['RequestVerificationToken'] = headers['RequestVerificationToken'] || token;
    }

    let body = options.body;
    if (body !== undefined && body !== null && !(body instanceof FormData) && typeof body !== 'string' && !(body instanceof URLSearchParams)) {
        if (!headers['Content-Type']) {
            headers['Content-Type'] = 'application/json';
        }
        body = JSON.stringify(body);
    }

    return fetch(url, {
        method,
        credentials: 'same-origin',
        headers,
        body
    });
};

// Reusable AJAX API Form Submission helper (keeps page in SPA so Network tab never clears)
window.submitApiForm = async function (form, config) {
    if (!form) return;

    if (window.jQuery && typeof window.jQuery(form).valid === 'function') {
        if (!window.jQuery(form).valid()) {
            return;
        }
    }

    const submitBtn = form.querySelector('button[type="submit"]');
    const origBtnHtml = submitBtn ? submitBtn.innerHTML : '';
    if (submitBtn) {
        submitBtn.disabled = true;
        submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status"></span>Saving...';
    }

    const existingAlert = form.querySelector('.api-form-error-alert');
    if (existingAlert) existingAlert.remove();

    try {
        const payload = config.buildPayload
            ? config.buildPayload(form)
            : (config.useFormData ? new FormData(form) : Object.fromEntries(new FormData(form).entries()));

        const res = await window.apiFetch(config.url, {
            method: config.method || 'POST',
            body: payload
        });

        const json = await res.json().catch(() => null);

        if (res.ok && json && json.success !== false) {
            if (typeof config.onSuccess === 'function') {
                await config.onSuccess(json, res);
            } else {
                showToast((json && json.message) || config.successMessage || 'Saved successfully.', 'success');
                const target = (json && json.redirectUrl) || config.redirectUrl;
                if (target) {
                    if (typeof spaNavigate === 'function') {
                        await spaNavigate(target, true);
                    } else {
                        window.location.href = target;
                    }
                }
            }
        } else {
            let firstValidationMsg = '';
            if (json && json.errors && typeof json.errors === 'object') {
                for (const [key, msgs] of Object.entries(json.errors)) {
                    const span = form.querySelector(`[data-valmsg-for="${key}"]`) || form.querySelector(`[data-valmsg-for="${key.charAt(0).toUpperCase() + key.slice(1)}"]`);
                    if (Array.isArray(msgs) && msgs.length > 0) {
                        if (!firstValidationMsg) firstValidationMsg = msgs[0];
                        if (span) {
                            span.innerText = msgs[0];
                            span.classList.remove('field-validation-valid');
                            span.classList.add('field-validation-error');
                        }
                    }
                }
            }

            const errMsg = (json && json.message) || firstValidationMsg || (json && json.title) || `Request failed with status ${res.status}.`;
            const alertDiv = document.createElement('div');
            alertDiv.className = 'alert alert-danger alert-dismissible fade show api-form-error-alert mb-3';
            alertDiv.setAttribute('role', 'alert');
            alertDiv.innerHTML = `<span>${escapeHtml(errMsg)}</span><button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>`;
            form.prepend(alertDiv);
            showToast(errMsg, 'error');
        }
    } catch (err) {
        console.error('API submit error:', err);
        showToast('Network error: ' + err.message, 'error');
    } finally {
        if (submitBtn) {
            submitBtn.disabled = false;
            submitBtn.innerHTML = origBtnHtml;
        }
    }
};

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

function getEntityApiPrefix(controller) {
    return (controller || '').trim().toLowerCase();
}

// Global AJAX Status Toggle
async function toggleStatus(controller, id, btnElement) {
    if (!id || id <= 0) return;

    btnElement.disabled = true;
    const originalContent = btnElement.innerHTML;
    btnElement.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>';

    try {
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        const token = tokenInput ? tokenInput.value : '';
        const prefix = getEntityApiPrefix(controller);

        let response = await window.apiFetch(`/api/${controller}/${prefix}_status_update`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
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
        btn.innerHTML = '<i class="bi bi-exclamation-triangle-fill me-1"></i>Confirm Delete?';
        showToast(`Click "Confirm Delete" to remove ${displayName}.`, 'warning');

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
        let isSuccess = false;
        let message = '';

        const prefix = getEntityApiPrefix(controller);
        let apiRes = await window.apiFetch(`/api/${controller}/${prefix}_delete?id=${id}`, {
            method: 'DELETE'
        });

        // Graceful fallback to MVC Delete route only if API route returned 404/405
        if (!apiRes.ok && (apiRes.status === 404 || apiRes.status === 405)) {
            const token = getAntiForgeryToken();
            const formData = new URLSearchParams();
            if (token) formData.append('__RequestVerificationToken', token);

            apiRes = await fetch(`/${controller}/Delete/${id}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token
                },
                body: formData.toString()
            });
        }

        const apiJson = await apiRes.json().catch(() => null);
        if (apiRes.ok && apiJson) {
            isSuccess = apiJson.success !== undefined ? apiJson.success : (apiJson.Status && apiJson.Status.Code === 0);
            message = apiJson.message || (apiJson.Data && apiJson.Data.Message) || (apiJson.Status && apiJson.Status.ReturnMessage);
            if (apiJson.stats && typeof window.updateExpenseUI === 'function') {
                window.updateExpenseUI(apiJson.stats);
            }
        } else if (apiJson && apiJson.message) {
            message = apiJson.message;
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
                                if (!r.hasAttribute('data-expense-id')) {
                                    const firstCell = r.querySelector('td:first-child');
                                    if (firstCell) {
                                        firstCell.innerText = (startIdx + idx);
                                    }
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
        btn.innerHTML = '<i class="bi bi-exclamation-triangle-fill me-1"></i>Confirm Delete?';
        showToast(`Click "Confirm Delete" to remove ${displayName}.`, 'warning');

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
        const response = await window.apiFetch(`/api/Customer/customer_delete?id=${id}`, {
            method: 'DELETE'
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
        btn.innerHTML = '<i class="bi bi-exclamation-triangle-fill me-1"></i>Confirm Delete?';
        showToast(`Click "Confirm Delete" to remove ${displayName}.`, 'warning');

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
        const response = await window.apiFetch(`/api/Supplier/supplier_delete?id=${id}`, {
            method: 'DELETE'
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
            // Clean up any active Bootstrap modal/backdrop left by the previous view before swapping DOM
            document.querySelectorAll('.modal.show').forEach(m => {
                if (window.bootstrap && window.bootstrap.Modal) {
                    const inst = window.bootstrap.Modal.getInstance(m);
                    if (inst) inst.hide();
                }
            });
            document.querySelectorAll('.modal-backdrop').forEach(b => b.remove());
            document.body.classList.remove('modal-open');
            document.body.style.removeProperty('overflow');
            document.body.style.removeProperty('padding-right');

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

            if (window.jQuery && window.jQuery.validator && window.jQuery.validator.unobtrusive) {
                window.jQuery.validator.unobtrusive.parse(mainContent);
            }

            // Execute page-specific @section Scripts inside #pageScripts so SPA-navigated pages have full API handlers
            const oldPageScripts = document.getElementById('pageScripts');
            const newPageScripts = doc.getElementById('pageScripts');
            if (oldPageScripts && newPageScripts) {
                oldPageScripts.innerHTML = '';
                const pageScriptNodes = Array.from(newPageScripts.querySelectorAll('script'));
                for (const s of pageScriptNodes) {
                    if (s.src) {
                        const absSrc = new URL(s.src, window.location.origin).href;
                        const alreadyLoaded = Array.from(document.querySelectorAll('script[src]')).some(existing => {
                            try { return new URL(existing.src, window.location.origin).href === absSrc; } catch { return false; }
                        });
                        if (alreadyLoaded) continue;
                        await new Promise((resolve) => {
                            const ns = document.createElement('script');
                            ns.src = absSrc;
                            ns.onload = resolve;
                            ns.onerror = resolve;
                            oldPageScripts.appendChild(ns);
                        });
                    } else {
                        const ns = document.createElement('script');
                        ns.textContent = s.textContent;
                        oldPageScripts.appendChild(ns);
                    }
                }
            }
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

// Reusable Lookup Master Modal Controller (Fetch List + Insert + Inline Update + Two-step Delete + Dropdown Sync)
window.initLookupManagerModal = function (config) {
    const openBtn = document.getElementById(config.openBtnId);
    const modalEl = document.getElementById(config.modalId);
    if (!openBtn || !modalEl) return;

    // Keep modal attached to body so page transition transform does not trap it under the backdrop
    document.querySelectorAll(`body > #${config.modalId}`).forEach(existing => {
        if (existing !== modalEl) existing.remove();
    });
    if (modalEl.parentElement !== document.body) {
        document.body.appendChild(modalEl);
    }

    const insertForm = document.getElementById(config.insertFormId);
    const newNameInput = document.getElementById(config.insertInputId);
    const insertBtn = document.getElementById(config.insertBtnId);
    const tbody = document.getElementById(config.tableBodyId);
    const countLabel = document.getElementById(config.countLabelId);
    const alertBox = document.getElementById(config.alertBoxId);

    let itemsCache = [];
    let editingId = null;

    function showModalAlert(message, type) {
        if (!alertBox) return;
        if (!message) {
            alertBox.className = 'd-none mb-3';
            alertBox.innerHTML = '';
            return;
        }
        const cls = type === 'success' ? 'alert-success' : 'alert-danger';
        alertBox.className = `alert ${cls} alert-dismissible fade show mb-3 py-2 px-3 small`;
        alertBox.innerHTML = `<span>${escapeHtml(message)}</span><button type="button" class="btn-close btn-sm" aria-label="Close"></button>`;
        const closeBtn = alertBox.querySelector('.btn-close');
        if (closeBtn) {
            closeBtn.addEventListener('click', function () {
                showModalAlert(null);
            });
        }
    }

    function syncDropdown(items, preferredId) {
        const selectEl = document.getElementById(config.dropdownId);
        if (!selectEl) return;

        const currentVal = selectEl.value;
        const targetVal = preferredId ? String(preferredId) : currentVal;

        selectEl.innerHTML = '';
        if (config.dropdownPlaceholder) {
            const ph = document.createElement('option');
            ph.value = '';
            ph.textContent = config.dropdownPlaceholder;
            selectEl.appendChild(ph);
        }

        let matched = false;
        (items || []).forEach(item => {
            const itemId = item[config.idField];
            const itemName = item[config.nameField];
            const opt = document.createElement('option');
            opt.value = String(itemId);
            opt.textContent = itemName;
            if (targetVal && String(itemId) === targetVal) {
                opt.selected = true;
                matched = true;
            }
            selectEl.appendChild(opt);
        });

        if (!matched && !config.dropdownPlaceholder && selectEl.options.length > 0) {
            selectEl.options[0].selected = true;
        }
    }

    function renderRows() {
        if (!tbody) return;
        if (countLabel) {
            const label = itemsCache.length === 1 ? config.singularLabel : config.pluralLabel;
            countLabel.innerText = `${itemsCache.length} ${label}`;
        }

        if (!itemsCache || itemsCache.length === 0) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="3" class="text-center py-4 text-muted">
                        <em>No ${escapeHtml(config.pluralLabel)} found. Add one above.</em>
                    </td>
                </tr>`;
            return;
        }

        tbody.innerHTML = itemsCache.map((item, idx) => {
            const id = item[config.idField];
            const name = item[config.nameField] || '';
            const isEditing = editingId === id;

            if (isEditing) {
                return `
                    <tr data-lookup-id="${id}">
                        <td class="text-center text-muted fw-semibold">${idx + 1}</td>
                        <td>
                            <input type="text"
                                   class="form-control form-control-sm lookup-edit-input"
                                   value="${escapeHtml(name)}"
                                   maxlength="100"
                                   data-id="${id}" />
                        </td>
                        <td class="text-center">
                            <div class="d-inline-flex gap-1">
                                <button type="button" class="btn btn-sm btn-success" data-action="save-edit" data-id="${id}">
                                    <i class="bi bi-check-lg me-1"></i>Save
                                </button>
                                <button type="button" class="btn btn-sm btn-outline-secondary" data-action="cancel-edit" data-id="${id}">
                                    Cancel
                                </button>
                            </div>
                        </td>
                    </tr>`;
            }

            return `
                <tr data-lookup-id="${id}">
                    <td class="text-center text-muted fw-semibold">${idx + 1}</td>
                    <td class="fw-semibold">${escapeHtml(name)}</td>
                    <td class="text-center">
                        <div class="d-inline-flex gap-1">
                            <button type="button" class="btn btn-sm btn-outline-primary" data-action="start-edit" data-id="${id}" title="Update ${escapeHtml(config.title)}">
                                <i class="bi bi-pencil-square me-1"></i>Update
                            </button>
                            <button type="button" class="btn btn-sm btn-outline-danger" data-action="delete" data-id="${id}" title="Delete ${escapeHtml(config.title)}">
                                <i class="bi bi-trash me-1"></i>Delete
                            </button>
                        </div>
                    </td>
                </tr>`;
        }).join('');

        if (editingId !== null) {
            const editInput = tbody.querySelector('.lookup-edit-input');
            if (editInput) {
                editInput.focus();
                editInput.select();
            }
        }
    }

    async function fetchItems(preferredSelectedId) {
        if (tbody) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="3" class="text-center py-4 text-muted">
                        <span class="spinner-border spinner-border-sm text-primary me-2" role="status"></span>Loading ${escapeHtml(config.pluralLabel)}...
                    </td>
                </tr>`;
        }

        try {
            const res = await window.apiFetch(config.listUrl, { method: 'GET' });
            const json = await res.json().catch(() => null);
            if (!res.ok || !json || !json.success) {
                const msg = (json && json.message) || `Failed to load ${config.pluralLabel} (HTTP ${res.status}).`;
                showModalAlert(msg, 'error');
                if (tbody) {
                    tbody.innerHTML = `<tr><td colspan="3" class="text-center py-4 text-danger">${escapeHtml(msg)}</td></tr>`;
                }
                return;
            }

            itemsCache = json.data || [];
            renderRows();
            syncDropdown(itemsCache, preferredSelectedId);
        } catch (err) {
            console.error(`Fetch ${config.pluralLabel} error:`, err);
            showModalAlert(`Error loading ${config.pluralLabel}: ${err.message}`, 'error');
        }
    }

    async function insertItem() {
        const name = newNameInput ? newNameInput.value.trim() : '';
        if (!name) {
            showModalAlert(`Please enter a ${config.singularLabel} name.`, 'error');
            if (newNameInput) newNameInput.focus();
            return;
        }

        const origHtml = insertBtn ? insertBtn.innerHTML : '';
        if (insertBtn) {
            insertBtn.disabled = true;
            insertBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1" role="status"></span>Saving...';
        }
        showModalAlert(null);

        try {
            const payload = { isActive: true };
            payload[config.nameField] = name;

            const res = await window.apiFetch(config.createUrl, {
                method: 'POST',
                body: payload
            });
            const json = await res.json().catch(() => null);

            if (!res.ok || !json || !json.success) {
                const errMsg = (json && json.message) || `Unable to add ${config.singularLabel} (HTTP ${res.status}).`;
                showModalAlert(errMsg, 'error');
                showToast(errMsg, 'error');
                return;
            }

            if (newNameInput) newNameInput.value = '';
            const selectEl = document.getElementById(config.dropdownId);
            const preferredId = (selectEl && !selectEl.value && json.data) ? json.data[config.idField] : null;
            const okMsg = json.message || `${config.title} added successfully.`;
            showModalAlert(okMsg, 'success');
            showToast(okMsg, 'success');
            editingId = null;
            await fetchItems(preferredId);
        } catch (err) {
            console.error(`Insert ${config.singularLabel} error:`, err);
            showModalAlert(`Error adding ${config.singularLabel}: ${err.message}`, 'error');
        } finally {
            if (insertBtn) {
                insertBtn.disabled = false;
                insertBtn.innerHTML = origHtml;
            }
        }
    }

    async function updateItem(id, updatedName, saveBtn) {
        const name = (updatedName || '').trim();
        if (!name) {
            showModalAlert(`${config.columnHeading} cannot be empty.`, 'error');
            return;
        }

        const origHtml = saveBtn ? saveBtn.innerHTML : '';
        if (saveBtn) {
            saveBtn.disabled = true;
            saveBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1" role="status"></span>Saving...';
        }
        showModalAlert(null);

        try {
            const payload = { isActive: true };
            payload[config.idField] = id;
            payload[config.nameField] = name;

            const res = await window.apiFetch(config.updateUrl, {
                method: 'PUT',
                body: payload
            });
            const json = await res.json().catch(() => null);

            if (!res.ok || !json || !json.success) {
                const errMsg = (json && json.message) || `Unable to update ${config.singularLabel} (HTTP ${res.status}).`;
                showModalAlert(errMsg, 'error');
                showToast(errMsg, 'error');
                if (saveBtn) {
                    saveBtn.disabled = false;
                    saveBtn.innerHTML = origHtml;
                }
                return;
            }

            editingId = null;
            const okMsg = json.message || `${config.title} updated successfully.`;
            showModalAlert(okMsg, 'success');
            showToast(okMsg, 'success');
            await fetchItems();
        } catch (err) {
            console.error(`Update ${config.singularLabel} error:`, err);
            showModalAlert(`Error updating ${config.singularLabel}: ${err.message}`, 'error');
            if (saveBtn) {
                saveBtn.disabled = false;
                saveBtn.innerHTML = origHtml;
            }
        }
    }

    async function deleteItemEntry(id, btn) {
        const item = itemsCache.find(c => c[config.idField] === id);
        const displayName = item ? `"${item[config.nameField]}"` : config.singularLabel;

        if (btn && !btn.dataset.confirming) {
            btn.dataset.confirming = 'true';
            const originalHtml = btn.innerHTML;
            const originalClasses = btn.className;
            btn.className = 'btn btn-sm btn-danger text-nowrap';
            btn.innerHTML = '<i class="bi bi-exclamation-triangle-fill me-1"></i>Confirm Delete?';
            showToast(`Click "Confirm Delete?" to remove ${displayName}.`, 'warning');

            const timer = setTimeout(() => {
                btn.innerHTML = originalHtml;
                btn.className = originalClasses;
                delete btn.dataset.confirming;
                delete btn.dataset.timerId;
            }, 4000);
            btn.dataset.timerId = String(timer);
            return;
        }

        if (btn && btn.dataset.timerId) {
            clearTimeout(parseInt(btn.dataset.timerId, 10));
            delete btn.dataset.timerId;
            delete btn.dataset.confirming;
            btn.disabled = true;
            btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1" role="status"></span>';
        }
        showModalAlert(null);

        try {
            const res = await window.apiFetch(`${config.deleteUrl}?id=${id}`, {
                method: 'DELETE'
            });
            const json = await res.json().catch(() => null);

            if (!res.ok || !json || !json.success) {
                const errMsg = (json && json.message) || `Unable to delete ${config.singularLabel} (HTTP ${res.status}).`;
                showModalAlert(errMsg, 'error');
                showToast(errMsg, 'error');
                renderRows();
                return;
            }

            if (editingId === id) editingId = null;
            const okMsg = json.message || `${config.title} deleted successfully.`;
            showModalAlert(okMsg, 'success');
            showToast(okMsg, 'success');
            await fetchItems();
        } catch (err) {
            console.error(`Delete ${config.singularLabel} error:`, err);
            showModalAlert(`Error deleting ${config.singularLabel}: ${err.message}`, 'error');
            renderRows();
        }
    }

    openBtn.addEventListener('click', function () {
        editingId = null;
        showModalAlert(null);
        if (window.bootstrap && window.bootstrap.Modal) {
            window.bootstrap.Modal.getOrCreateInstance(modalEl).show();
        }
        fetchItems();
    });

    if (insertForm) {
        insertForm.addEventListener('submit', function (e) {
            e.preventDefault();
            insertItem();
        });
    }

    if (tbody) {
        tbody.addEventListener('click', function (e) {
            const btn = e.target.closest('button[data-action]');
            if (!btn) return;
            const action = btn.getAttribute('data-action');
            const id = parseInt(btn.getAttribute('data-id'), 10) || 0;
            if (id <= 0) return;

            if (action === 'start-edit') {
                editingId = id;
                showModalAlert(null);
                renderRows();
            } else if (action === 'cancel-edit') {
                editingId = null;
                renderRows();
            } else if (action === 'save-edit') {
                const row = btn.closest('tr');
                const input = row ? row.querySelector('.lookup-edit-input') : null;
                updateItem(id, input ? input.value : '', btn);
            } else if (action === 'delete') {
                deleteItemEntry(id, btn);
            }
        });

        tbody.addEventListener('keydown', function (e) {
            const input = e.target.closest('.lookup-edit-input');
            if (!input) return;
            const id = parseInt(input.getAttribute('data-id'), 10) || 0;
            if (e.key === 'Enter') {
                e.preventDefault();
                const row = input.closest('tr');
                const saveBtn = row ? row.querySelector('button[data-action="save-edit"]') : null;
                updateItem(id, input.value, saveBtn);
            } else if (e.key === 'Escape') {
                e.preventDefault();
                editingId = null;
                renderRows();
            }
        });
    }
};
