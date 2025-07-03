Toast System
window.showModernToast = function (message, type = 'info', duration = 5000) {
    const container = document.getElementById('toast-container') || createToastContainer();

    const toast = document.createElement('div');
    toast.className = `toast ${type}`;

    const iconMap = {
        success: 'fas fa-check-circle',
        error: 'fas fa-exclamation-circle',
        warning: 'fas fa-exclamation-triangle',
        info: 'fas fa-info-circle'
    };

    toast.innerHTML = `
        <i class="toast-icon ${iconMap[type] || iconMap.info}"></i>
        <span class="toast-message">${message}</span>
        <button class="toast-close" onclick="this.parentElement.remove()">
            <i class="fas fa-times"></i>
        </button>
    `;

    container.appendChild(toast);

    // Auto remove
    setTimeout(() => {
        if (toast.parentElement) {
            toast.style.animation = 'toastSlideOut 0.3s cubic-bezier(0.4, 0, 0.2, 1) forwards';
            setTimeout(() => toast.remove(), 300);
        }
    }, duration);
};

function createToastContainer() {
    const container = document.createElement('div');
    container.id = 'toast-container';
    container.className = 'toast-container';
    document.body.appendChild(container);
    return container;
}

// Theme Management
window.modernTheme = {
    set: function (isDark) {
        document.documentElement.setAttribute('data-theme', isDark ? 'dark' : 'light');
        localStorage.setItem('modernTheme', isDark ? 'dark' : 'light');
    },

    get: function () {
        return localStorage.getItem('modernTheme') === 'dark';
    },

    toggle: function () {
        const isDark = !this.get();
        this.set(isDark);
        return isDark;
    },

    init: function () {
        const isDark = this.get();
        this.set(isDark);
    }
};

// Initialize theme on load
document.addEventListener('DOMContentLoaded', function () {
    window.modernTheme.init();
});

// Smooth scrolling for navigation
document.addEventListener('click', function (e) {
    if (e.target.matches('a[href^="#"]')) {
        e.preventDefault();
        const target = document.querySelector(e.target.getAttribute('href'));
        if (target) {
            target.scrollIntoView({ behavior: 'smooth' });
        }
    }
});

// Enhanced chart configurations
window.modernCharts = {
    getDefaultOptions: function (isDark = false) {
        return {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'top',
                    labels: {
                        usePointStyle: true,
                        padding: 20,
                        color: isDark ? '#e5e7eb' : '#374151',
                        font: {
                            family: 'Inter, system-ui, sans-serif',
                            size: 12,
                            weight: '500'
                        }
                    }
                },
                tooltip: {
                    backgroundColor: isDark ? '#1f2937' : '#ffffff',
                    titleColor: isDark ? '#f9fafb' : '#111827',
                    bodyColor: isDark ? '#e5e7eb' : '#374151',
                    borderColor: isDark ? '#374151' : '#e5e7eb',
                    borderWidth: 1,
                    cornerRadius: 12,
                    padding: 16,
                    boxPadding: 8,
                    usePointStyle: true,
                    font: {
                        family: 'Inter, system-ui, sans-serif'
                    }
                }
            },
            scales: {
                x: {
                    grid: {
                        color: isDark ? '#374151' : '#f3f4f6',
                        borderColor: isDark ? '#4b5563' : '#d1d5db'
                    },
                    ticks: {
                        color: isDark ? '#9ca3af' : '#6b7280',
                        font: {
                            family: 'Inter, system-ui, sans-serif',
                            size: 11
                        }
                    }
                },
                y: {
                    grid: {
                        color: isDark ? '#374151' : '#f3f4f6',
                        borderColor: isDark ? '#4b5563' : '#d1d5db'
                    },
                    ticks: {
                        color: isDark ? '#9ca3af' : '#6b7280',
                        font: {
                            family: 'Inter, system-ui, sans-serif',
                            size: 11
                        }
                    }
                }
            },
            elements: {
                point: {
                    radius: 4,
                    hoverRadius: 6,
                    borderWidth: 2
                },
                line: {
                    tension: 0.4,
                    borderWidth: 3
                }
            }
        };
    }
};