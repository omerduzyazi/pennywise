/**
 * PennyWise — Core Application Logic
 * SPA navigation, authentication, and API communication.
 */

const API_BASE = '/api';

// ═══════════════════════════════════════════════════════════════
// Auth State Management
// ═══════════════════════════════════════════════════════════════

function getToken() {
    return localStorage.getItem('pw_token');
}

function getUser() {
    const data = localStorage.getItem('pw_user');
    return data ? JSON.parse(data) : null;
}

function setAuth(token, user) {
    localStorage.setItem('pw_token', token);
    localStorage.setItem('pw_user', JSON.stringify(user));
}

function clearAuth() {
    localStorage.removeItem('pw_token');
    localStorage.removeItem('pw_user');
}

function isAuthenticated() {
    return !!getToken();
}

function authHeaders() {
    const token = getToken();
    return token ? { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' } : { 'Content-Type': 'application/json' };
}

// ═══════════════════════════════════════════════════════════════
// UI State
// ═══════════════════════════════════════════════════════════════

function showAuthModal() {
    document.getElementById('auth-overlay').classList.remove('hidden');
}

document.getElementById('auth-overlay').addEventListener('click', (e) => {
    if (e.target.id === 'auth-overlay') hideAuthModal();
});

function hideAuthModal() {
    document.getElementById('auth-overlay').classList.add('hidden');
}

function updateUIForAuth() {
    if (isAuthenticated()) {
        hideAuthModal();
        const user = getUser();
        if (user) {
            document.getElementById('greeting').textContent = `SYS.USR: ${user.fullName}`;
            
            // Show Admin Panel if role is Admin
            if (user.role === 'Admin') {
                document.getElementById('nav-admin').style.display = 'flex';
            } else {
                document.getElementById('nav-admin').style.display = 'none';
            }
        }
    } else {
        showAuthModal();
        document.getElementById('greeting').textContent = '';
        document.getElementById('header-balance-container').style.display = 'none';
    }
}

function getGreetingText() {
    const hour = new Date().getHours();
    if (hour < 12) return 'Good morning';
    if (hour < 18) return 'Good afternoon';
    return 'Good evening';
}

// ═══════════════════════════════════════════════════════════════
// Auth Form Handlers
// ═══════════════════════════════════════════════════════════════

// Toggle between login and register
document.getElementById('show-register').addEventListener('click', (e) => {
    e.preventDefault();
    document.getElementById('login-form').classList.remove('active');
    document.getElementById('register-form').classList.add('active');
    document.getElementById('login-error').textContent = '';
});

document.getElementById('show-login').addEventListener('click', (e) => {
    e.preventDefault();
    document.getElementById('register-form').classList.remove('active');
    document.getElementById('login-form').classList.add('active');
    document.getElementById('register-error').textContent = '';
});

// Login
document.getElementById('login-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    const btn = document.getElementById('login-btn');
    const errorEl = document.getElementById('login-error');
    errorEl.textContent = '';

    const email = document.getElementById('login-email').value.trim();
    const password = document.getElementById('login-password').value;

    if (!email || !password) {
        errorEl.textContent = 'Please fill in all fields.';
        return;
    }

    btn.disabled = true;
    btn.textContent = 'Signing in...';

    try {
        const res = await fetch(`${API_BASE}/auth/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, password })
        });

        if (res.ok) {
            const data = await res.json();
            setAuth(data.token, { email: data.email, fullName: data.fullName, role: data.role });
            updateUIForAuth();
        } else {
            const err = await res.json();
            errorEl.textContent = err.error || 'Invalid credentials.';
        }
    } catch {
        errorEl.textContent = 'Cannot connect to the server.';
    } finally {
        btn.disabled = false;
        btn.textContent = 'Sign In';
    }
});

// Register
document.getElementById('register-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    const btn = document.getElementById('register-btn');
    const errorEl = document.getElementById('register-error');
    errorEl.textContent = '';

    const fullName = document.getElementById('register-fullname').value.trim();
    const email = document.getElementById('register-email').value.trim();
    const password = document.getElementById('register-password').value;

    if (!fullName || !email || !password) {
        errorEl.textContent = 'Please fill in all fields.';
        return;
    }

    if (password.length < 6) {
        errorEl.textContent = 'Password must be at least 6 characters.';
        return;
    }

    btn.disabled = true;
    btn.textContent = 'Creating account...';

    try {
        const res = await fetch(`${API_BASE}/auth/register`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, password, fullName })
        });

        if (res.ok || res.status === 201) {
            const data = await res.json();
            setAuth(data.token, { email: data.email, fullName: data.fullName, role: data.role });
            updateUIForAuth();
        } else {
            const err = await res.json();
            errorEl.textContent = err.error || 'Registration failed.';
        }
    } catch {
        errorEl.textContent = 'Cannot connect to the server.';
    } finally {
        btn.disabled = false;
        btn.textContent = 'Create Account';
    }
});

// Logout
document.getElementById('btn-logout').addEventListener('click', () => {
    clearAuth();
    updateUIForAuth();
});

// ═══════════════════════════════════════════════════════════════
// Navigation
// ═══════════════════════════════════════════════════════════════

document.querySelectorAll('.nav-item').forEach(item => {
    item.addEventListener('click', (e) => {
        e.preventDefault();
        navigateTo(item.dataset.page);
    });
});

function navigateTo(pageName) {
    document.querySelectorAll('.nav-item').forEach(n => n.classList.remove('active'));
    const activeNav = document.querySelector(`[data-page="${pageName}"]`);
    if (activeNav) activeNav.classList.add('active');

    document.querySelectorAll('.page').forEach(p => p.classList.remove('active'));
    const activePage = document.getElementById(`page-${pageName}`);
    if (activePage) activePage.classList.add('active');

    const title = pageName.charAt(0).toUpperCase() + pageName.slice(1);
    document.getElementById('page-title').textContent = title === 'Admin' ? 'SYSTEM.ADMIN_PANEL' : title;

    if (pageName === 'admin') fetchAdminUsers();

    document.getElementById('sidebar').classList.remove('open');
}

// Mobile menu
document.getElementById('menu-toggle').addEventListener('click', () => {
    document.getElementById('sidebar').classList.toggle('open');
});

// ═══════════════════════════════════════════════════════════════
// API Health Check
// ═══════════════════════════════════════════════════════════════

async function checkApiHealth() {
    const dot = document.querySelector('.status-dot');
    const text = document.querySelector('.status-text');

    try {
        const res = await fetch(`${API_BASE}/health`);
        if (res.ok) {
            const data = await res.json();
            dot.className = 'status-dot online';
            text.textContent = `API ${data.status} — v${data.version}`;
        } else {
            dot.className = 'status-dot offline';
            text.textContent = 'API Unreachable';
        }
    } catch {
        dot.className = 'status-dot offline';
        text.textContent = 'API Offline';
    }
}

// ═══════════════════════════════════════════════════════════════
// Transactions API & UI
// ═══════════════════════════════════════════════════════════════

async function fetchTransactions() {
    if (!isAuthenticated()) return;
    try {
        const res = await fetch(`${API_BASE}/transactions?pageSize=50`, { headers: authHeaders() });
        if (res.ok) {
            const data = await res.json();
            renderTransactions(data.items);
            renderRecentTransactions(data.items.slice(0, 5));
        }
    } catch (e) {
        console.error('Failed to fetch transactions:', e);
    }
}

function renderTransactions(transactions) {
    const tbody = document.getElementById('transactions-list');
    if (!transactions || transactions.length === 0) {
        tbody.innerHTML = '<tr><td colspan="6" class="empty-state">No transactions found. Add one!</td></tr>';
        return;
    }

    tbody.innerHTML = transactions.map(tx => {
        const date = new Date(tx.transactionDate).toLocaleDateString();
        const typeBadge = tx.type === 0 
            ? '<span class="badge income">Income</span>' 
            : '<span class="badge expense">Expense</span>';
        const amountClass = tx.type === 0 ? 'text-success' : 'text-danger';
        const sign = tx.type === 0 ? '+' : '-';
        
        return `
            <tr>
                <td>${date}</td>
                <td>${tx.description || '-'}</td>
                <td>${tx.category}</td>
                <td>${typeBadge}</td>
                <td style="font-weight: 600" class="${amountClass}">${sign}₺${tx.amount.toFixed(2)}</td>
                <td>
                    <button class="btn-danger btn-sm" onclick="deleteTransaction('${tx.id}')">Delete</button>
                </td>
            </tr>
        `;
    }).join('');
}

function renderRecentTransactions(transactions) {
    const container = document.getElementById('recent-transactions-list');
    if (!transactions || transactions.length === 0) {
        container.innerHTML = '<p class="empty-state">No transactions yet. Add your first one!</p>';
        return;
    }
    
    // We reuse the data-table styling for the recent list
    let html = '<table class="data-table"><tbody>';
    html += transactions.map(tx => {
        const date = new Date(tx.transactionDate).toLocaleDateString();
        const sign = tx.type === 0 ? '+' : '-';
        const color = tx.type === 0 ? 'color: var(--color-success)' : 'color: var(--color-danger)';
        return `
            <tr>
                <td><div><strong>${tx.category}</strong><div style="font-size: 0.75rem; color: var(--color-text-muted)">${date}</div></div></td>
                <td>${tx.description}</td>
                <td style="text-align: right; font-weight: 600; ${color}">${sign}₺${tx.amount.toFixed(2)}</td>
            </tr>
        `;
    }).join('');
    html += '</tbody></table>';
    container.innerHTML = html;
}

async function deleteTransaction(id) {
    if (!confirm('Are you sure you want to delete this transaction?')) return;
    try {
        const res = await fetch(`${API_BASE}/transactions/${id}`, {
            method: 'DELETE',
            headers: authHeaders()
        });
        if (res.ok) {
            refreshData();
        } else {
            alert('Failed to delete transaction.');
        }
    } catch (e) {
        console.error(e);
    }
}

// ═══════════════════════════════════════════════════════════════
// Budgets API & UI
// ═══════════════════════════════════════════════════════════════

async function fetchBudgets() {
    if (!isAuthenticated()) return;
    try {
        const res = await fetch(`${API_BASE}/budgets/status`, { headers: authHeaders() });
        if (res.ok) {
            const data = await res.json();
            renderBudgets(data);
            renderBudgetOverview(data.slice(0, 3));
        }
    } catch (e) {
        console.error('Failed to fetch budgets:', e);
    }
}

function renderBudgets(budgets) {
    const container = document.getElementById('budgets-list');
    if (!budgets || budgets.length === 0) {
        container.innerHTML = '<p class="empty-state" style="grid-column: 1 / -1">No budgets configured for this month.</p>';
        return;
    }

    container.innerHTML = budgets.map(bg => {
        const percent = Math.min(bg.percentUsed, 100);
        let colorClass = '';
        if (percent >= 90) colorClass = 'danger';
        else if (percent >= 75) colorClass = 'warning';

        return `
            <div class="budget-card">
                <div class="budget-header">
                    <span class="budget-category">${bg.category}</span>
                    <div class="budget-actions">
                        <button onclick="deleteBudget('${bg.id}')" title="Delete">
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="3 6 5 6 21 6"></polyline><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path></svg>
                        </button>
                    </div>
                </div>
                <div class="budget-stats">
                    <span>₺${bg.spentAmount.toFixed(2)} spent</span>
                    <span>₺${bg.limitAmount.toFixed(2)} limit</span>
                </div>
                <div class="progress-bar">
                    <div class="progress-fill ${colorClass}" style="width: ${percent}%"></div>
                </div>
                <div style="text-align: right; margin-top: 8px; font-size: 0.75rem; color: var(--color-text-muted)">
                    ${bg.percentUsed}% used
                </div>
            </div>
        `;
    }).join('');
}

function renderBudgetOverview(budgets) {
    const container = document.getElementById('budget-overview-list');
    if (!budgets || budgets.length === 0) {
        container.innerHTML = '<p class="empty-state">No budgets configured.</p>';
        return;
    }
    
    let html = '<div style="display: flex; flex-direction: column; gap: 16px;">';
    html += budgets.map(bg => {
        const percent = Math.min(bg.percentUsed, 100);
        let colorClass = '';
        if (percent >= 90) colorClass = 'danger';
        else if (percent >= 75) colorClass = 'warning';

        return `
            <div>
                <div style="display: flex; justify-content: space-between; font-size: 0.85rem; margin-bottom: 6px;">
                    <strong>${bg.category}</strong>
                    <span style="color: var(--color-text-muted)">₺${bg.spentAmount.toFixed(0)} / ₺${bg.limitAmount.toFixed(0)}</span>
                </div>
                <div class="progress-bar">
                    <div class="progress-fill ${colorClass}" style="width: ${percent}%"></div>
                </div>
            </div>
        `;
    }).join('');
    html += '</div>';
    container.innerHTML = html;
}

async function deleteBudget(id) {
    if (!confirm('Are you sure you want to delete this budget?')) return;
    try {
        const res = await fetch(`${API_BASE}/budgets/${id}`, {
            method: 'DELETE',
            headers: authHeaders()
        });
        if (res.ok) {
            refreshData();
        } else {
            alert('Failed to delete budget.');
        }
    } catch (e) {
        console.error(e);
    }
}

// ═══════════════════════════════════════════════════════════════
// Dashboard Summary
// ═══════════════════════════════════════════════════════════════

async function fetchSummary() {
    if (!isAuthenticated()) return;
    try {
        const res = await fetch(`${API_BASE}/transactions/summary`, { headers: authHeaders() });
        if (res.ok) {
            const data = await res.json();
            document.getElementById('stat-income').textContent = `₺${data.totalIncome.toFixed(2)}`;
            document.getElementById('stat-expense').textContent = `₺${data.totalExpenses.toFixed(2)}`;
            document.getElementById('stat-balance').textContent = `₺${data.netBalance.toFixed(2)}`;
            
            // Update header balance
            document.getElementById('header-balance-val').textContent = `₺${data.netBalance.toFixed(2)}`;
            document.getElementById('header-balance-container').style.display = 'block';
        }
    } catch (e) {
        console.error('Failed to fetch summary:', e);
    }
}

// ═══════════════════════════════════════════════════════════════
// Portfolio & Analytics API & UI
// ═══════════════════════════════════════════════════════════════

let currentPortfolioId = null;

async function fetchPortfolios() {
    if (!isAuthenticated()) return;
    try {
        const res = await fetch(`${API_BASE}/portfolios`, { headers: authHeaders() });
        if (res.ok) {
            const portfolios = await res.json();
            renderPortfoliosList(portfolios);
            
            // Calculate total portfolio value for dashboard
            let totalVal = 0;
            for (let pf of portfolios) {
                const anRes = await fetch(`${API_BASE}/portfolios/${pf.id}/analytics`, { headers: authHeaders() });
                if (anRes.ok) {
                    const an = await anRes.json();
                    totalVal += an.totalValue;
                }
            }
            document.getElementById('stat-portfolio').textContent = `₺${totalVal.toFixed(2)}`;
        }
    } catch (e) {
        console.error('Failed to fetch portfolios:', e);
    }
}

function renderPortfoliosList(portfolios) {
    const container = document.getElementById('portfolios-list');
    if (!portfolios || portfolios.length === 0) {
        container.innerHTML = '<p class="empty-state">No portfolios yet.</p>';
        return;
    }
    
    container.innerHTML = portfolios.map(pf => `
        <div style="padding: 16px; border: 1px solid var(--border-color); border-radius: 8px; cursor: pointer; display: flex; justify-content: space-between; align-items: center; transition: background 0.2s;" 
             onclick="selectPortfolio('${pf.id}', '${pf.name}')"
             onmouseover="this.style.background='var(--color-bg-card-hover)'"
             onmouseout="this.style.background='transparent'">
            <span style="font-weight: 600;">${pf.name}</span>
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="var(--color-text-muted)" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
        </div>
    `).join('');
}

async function selectPortfolio(id, name) {
    currentPortfolioId = id;
    document.getElementById('pf-detail-title').textContent = name;
    document.getElementById('portfolio-details').style.display = 'block';
    await refreshPortfolioDetails();
}

async function refreshPortfolioDetails() {
    if (!currentPortfolioId) return;
    
    try {
        // Fetch Analytics
        const anRes = await fetch(`${API_BASE}/portfolios/${currentPortfolioId}/analytics`, { headers: authHeaders() });
        if (anRes.ok) {
            const an = await anRes.json();
            document.getElementById('pf-detail-value').textContent = `₺${an.totalValue.toFixed(2)}`;
            
            const absEl = document.getElementById('pf-detail-abs');
            absEl.textContent = `${an.absoluteReturnAmount >= 0 ? '+' : ''}₺${an.absoluteReturnAmount.toFixed(2)}`;
            absEl.style.color = an.absoluteReturnAmount >= 0 ? 'var(--color-success)' : 'var(--color-danger)';
            
            const twrEl = document.getElementById('pf-detail-twr');
            twrEl.textContent = `${an.twrPercentage >= 0 ? '+' : ''}${an.twrPercentage.toFixed(2)}%`;
            twrEl.style.color = an.twrPercentage >= 0 ? 'var(--color-success)' : 'var(--color-danger)';
        }

        // Fetch Holdings
        const hRes = await fetch(`${API_BASE}/portfolios/${currentPortfolioId}/holdings`, { headers: authHeaders() });
        if (hRes.ok) {
            const holdings = await hRes.json();
            renderHoldings(holdings);
        }
    } catch (e) {
        console.error(e);
    }
}

function renderHoldings(holdings) {
    const tbody = document.getElementById('holdings-list');
    if (!holdings || holdings.length === 0) {
        tbody.innerHTML = '<tr><td colspan="7" class="empty-state">No holdings in this portfolio.</td></tr>';
        return;
    }

    tbody.innerHTML = holdings.map(h => {
        const totalVal = h.currentPrice * h.quantity;
        const pnl = totalVal - (h.purchasePrice * h.quantity);
        const pnlColor = pnl >= 0 ? 'var(--color-success)' : 'var(--color-danger)';
        const pnlSign = pnl >= 0 ? '+' : '';
        
        return `
            <tr>
                <td style="font-weight: bold;">${h.symbol}</td>
                <td>${h.name}</td>
                <td>${h.quantity.toFixed(4)}</td>
                <td>₺${h.purchasePrice.toFixed(2)}</td>
                <td>
                    <div style="display: flex; gap: 8px; align-items: center;">
                        ₺${h.currentPrice.toFixed(2)}
                        <button style="background:none; border:none; color:var(--color-accent-primary); cursor:pointer;" onclick="updatePrice('${h.id}', ${h.currentPrice})">
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 20h9"></path><path d="M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4L16.5 3.5z"></path></svg>
                        </button>
                    </div>
                </td>
                <td>
                    ₺${totalVal.toFixed(2)}
                    <div style="font-size: 0.75rem; color: ${pnlColor}">${pnlSign}₺${pnl.toFixed(2)}</div>
                </td>
                <td>
                    <button class="btn-danger btn-sm" onclick="deleteHolding('${h.id}')">Sell/Del</button>
                </td>
            </tr>
        `;
    }).join('');
}

async function updatePrice(holdingId, currentPrice) {
    const newPrice = prompt('Enter new current price (₺):', currentPrice);
    if (newPrice === null || isNaN(parseFloat(newPrice))) return;
    
    try {
        const res = await fetch(`${API_BASE}/holdings/${holdingId}/price`, {
            method: 'PUT',
            headers: authHeaders(),
            body: JSON.stringify({ currentPrice: parseFloat(newPrice) })
        });
        if (res.ok) {
            refreshPortfolioDetails();
            fetchPortfolios(); // Update dashboard total
        }
    } catch(e) { console.error(e); }
}

async function deleteHolding(holdingId) {
    if (!confirm('Are you sure you want to remove this holding?')) return;
    try {
        const res = await fetch(`${API_BASE}/holdings/${holdingId}`, {
            method: 'DELETE',
            headers: authHeaders()
        });
        if (res.ok) {
            refreshPortfolioDetails();
            fetchPortfolios(); // Update dashboard total
        }
    } catch(e) { console.error(e); }
}

// ═══════════════════════════════════════════════════════════════
// Modals & Form Submissions (Continued)
// ═══════════════════════════════════════════════════════════════

const pfModal = document.getElementById('portfolio-modal-overlay');
const hModal = document.getElementById('holding-modal-overlay');

document.getElementById('btn-add-portfolio').addEventListener('click', () => pfModal.classList.remove('hidden'));
document.getElementById('close-portfolio-modal').addEventListener('click', () => pfModal.classList.add('hidden'));

document.getElementById('btn-add-holding').addEventListener('click', () => {
    document.getElementById('h-date').valueAsDate = new Date();
    hModal.classList.remove('hidden');
});
document.getElementById('close-holding-modal').addEventListener('click', () => hModal.classList.add('hidden'));

// Close modals when clicking outside
[pfModal, hModal].forEach(modal => {
    modal.addEventListener('click', (e) => {
        if (e.target === modal) modal.classList.add('hidden');
    });
});

document.getElementById('portfolio-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    const btn = document.getElementById('pf-submit-btn');
    const errorEl = document.getElementById('pf-error');
    errorEl.textContent = '';
    
    btn.disabled = true;
    try {
        const res = await fetch(`${API_BASE}/portfolios`, {
            method: 'POST',
            headers: authHeaders(),
            body: JSON.stringify({ name: document.getElementById('pf-name').value })
        });
        if (res.ok) {
            pfModal.classList.add('hidden');
            e.target.reset();
            fetchPortfolios();
        } else {
            errorEl.textContent = 'Failed to create portfolio.';
        }
    } catch {
        errorEl.textContent = 'Server error.';
    } finally {
        btn.disabled = false;
    }
});

document.getElementById('holding-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    if (!currentPortfolioId) return;
    
    const btn = document.getElementById('h-submit-btn');
    const errorEl = document.getElementById('h-error');
    errorEl.textContent = '';
    
    const payload = {
        symbol: document.getElementById('h-symbol').value,
        name: document.getElementById('h-name').value,
        instrumentType: parseInt(document.getElementById('h-type').value),
        purchasePrice: parseFloat(document.getElementById('h-price').value),
        quantity: parseFloat(document.getElementById('h-qty').value),
        purchaseDate: document.getElementById('h-date').value
    };

    btn.disabled = true;
    try {
        const res = await fetch(`${API_BASE}/portfolios/${currentPortfolioId}/holdings`, {
            method: 'POST',
            headers: authHeaders(),
            body: JSON.stringify(payload)
        });
        if (res.ok) {
            hModal.classList.add('hidden');
            e.target.reset();
            refreshPortfolioDetails();
            fetchPortfolios();
        } else {
            const err = await res.json();
            errorEl.textContent = err.error || 'Failed to add holding.';
        }
    } catch {
        errorEl.textContent = 'Server error.';
    } finally {
        btn.disabled = false;
    }
});

function refreshData() {
    fetchSummary();
    fetchTransactions();
    fetchBudgets();
    fetchPortfolios();
}

// ═══════════════════════════════════════════════════════════════
// Init
// ═══════════════════════════════════════════════════════════════

const txModal = document.getElementById('transaction-modal-overlay');
const bgModal = document.getElementById('budget-modal-overlay');

document.getElementById('btn-add-transaction').addEventListener('click', () => {
    document.getElementById('tx-date').valueAsDate = new Date();
    txModal.classList.remove('hidden');
});
document.getElementById('close-transaction-modal').addEventListener('click', () => txModal.classList.add('hidden'));

document.getElementById('btn-add-budget').addEventListener('click', () => {
    const now = new Date();
    document.getElementById('bg-month').value = now.getMonth() + 1;
    document.getElementById('bg-year').value = now.getFullYear();
    bgModal.classList.remove('hidden');
});
document.getElementById('close-budget-modal').addEventListener('click', () => bgModal.classList.add('hidden'));

// Close modals when clicking outside
[txModal, bgModal].forEach(modal => {
    modal.addEventListener('click', (e) => {
        if (e.target === modal) modal.classList.add('hidden');
    });
});

document.getElementById('transaction-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    const btn = document.getElementById('tx-submit-btn');
    const errorEl = document.getElementById('tx-error');
    errorEl.textContent = '';
    
    const payload = {
        type: parseInt(document.getElementById('tx-type').value),
        amount: parseFloat(document.getElementById('tx-amount').value),
        category: document.getElementById('tx-category').value,
        description: document.getElementById('tx-description').value,
        transactionDate: document.getElementById('tx-date').value
    };

    btn.disabled = true;
    try {
        const res = await fetch(`${API_BASE}/transactions`, {
            method: 'POST',
            headers: authHeaders(),
            body: JSON.stringify(payload)
        });
        if (res.ok) {
            txModal.classList.add('hidden');
            e.target.reset();
            refreshData();
        } else {
            const err = await res.json();
            errorEl.textContent = err.error || 'Failed to save transaction.';
        }
    } catch {
        errorEl.textContent = 'Server error.';
    } finally {
        btn.disabled = false;
    }
});

document.getElementById('budget-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    const btn = document.getElementById('bg-submit-btn');
    const errorEl = document.getElementById('bg-error');
    errorEl.textContent = '';
    
    const payload = {
        category: document.getElementById('bg-category').value,
        limitAmount: parseFloat(document.getElementById('bg-amount').value),
        month: parseInt(document.getElementById('bg-month').value),
        year: parseInt(document.getElementById('bg-year').value)
    };

    btn.disabled = true;
    try {
        const res = await fetch(`${API_BASE}/budgets`, {
            method: 'POST',
            headers: authHeaders(),
            body: JSON.stringify(payload)
        });
        if (res.ok) {
            bgModal.classList.add('hidden');
            e.target.reset();
            refreshData();
        } else {
            const err = await res.json();
            errorEl.textContent = err.error || 'Failed to save budget.';
        }
    } catch {
        errorEl.textContent = 'Server error.';
    } finally {
        btn.disabled = false;
    }
});

const originalUpdateUIForAuth = updateUIForAuth;
updateUIForAuth = function() {
    originalUpdateUIForAuth();
    if (isAuthenticated()) {
        refreshData();
    }
};

// ═══════════════════════════════════════════════════════════════
// Init
// ═══════════════════════════════════════════════════════════════
// ═══════════════════════════════════════════════════════════════
// Admin API & UI
// ═══════════════════════════════════════════════════════════════

async function fetchAdminUsers() {
    if (!isAuthenticated()) return;
    const user = getUser();
    if (!user || user.role !== 'Admin') return;
    
    try {
        const res = await fetch(`${API_BASE}/admin/users`, { headers: authHeaders() });
        if (res.ok) {
            const users = await res.json();
            const tbody = document.getElementById('admin-users-list');
            if (!users || users.length === 0) {
                tbody.innerHTML = '<tr><td colspan="5" class="empty-state">No users found.</td></tr>';
                return;
            }
            
            tbody.innerHTML = users.map(u => `
                <tr>
                    <td style="font-family: var(--font-mono); font-size: 0.7rem; color: var(--color-text-muted)">${u.id}</td>
                    <td>${u.email}</td>
                    <td>${u.fullName}</td>
                    <td>
                        <span class="badge ${u.role === 'Admin' ? 'income' : 'expense'}">${u.role}</span>
                    </td>
                    <td>${new Date(u.createdAt).toLocaleDateString()}</td>
                </tr>
            `).join('');
        } else {
            console.error('Failed to fetch admin users', await res.text());
        }
    } catch (e) {
        console.error('Error fetching admin users:', e);
    }
}

updateUIForAuth();
checkApiHealth();
setInterval(checkApiHealth, 30000);
