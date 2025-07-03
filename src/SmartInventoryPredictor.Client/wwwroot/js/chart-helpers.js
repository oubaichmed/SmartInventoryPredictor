// Clean Chart Helpers for Smart Inventory Predictor
window.chartHelpers = {
    createChart: function (canvasId, config) {
        const ctx = document.getElementById(canvasId);
        if (ctx) {
            // Apply clean defaults
            this.applyCleanDefaults(config);
            return new Chart(ctx, config);
        }
        return null;
    },

    updateChart: function (chart, data) {
        if (chart) {
            chart.data = data;
            chart.update('none');
        }
    },

    destroyChart: function (chart) {
        if (chart) {
            chart.destroy();
        }
    },

    applyCleanDefaults: function (config) {
        // Clean color palette
        const colors = {
            primary: '#2563eb',
            secondary: '#6b7280',
            success: '#10b981',
            warning: '#f59e0b',
            danger: '#ef4444'
        };

        // Clean default options
        const cleanDefaults = {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    labels: {
                        font: {
                            family: 'Inter, system-ui, sans-serif',
                            size: 12,
                            weight: 500
                        },
                        color: '#374151',
                        usePointStyle: true,
                        padding: 16
                    }
                },
                tooltip: {
                    backgroundColor: '#ffffff',
                    titleColor: '#111827',
                    bodyColor: '#374151',
                    borderColor: '#e5e7eb',
                    borderWidth: 1,
                    cornerRadius: 8,
                    padding: 12,
                    titleFont: {
                        family: 'Inter, system-ui, sans-serif',
                        size: 13,
                        weight: 600
                    },
                    bodyFont: {
                        family: 'Inter, system-ui, sans-serif',
                        size: 12
                    }
                }
            },
            scales: {
                x: {
                    grid: {
                        color: '#f3f4f6',
                        borderColor: '#e5e7eb'
                    },
                    ticks: {
                        color: '#6b7280',
                        font: {
                            family: 'Inter, system-ui, sans-serif',
                            size: 11
                        }
                    }
                },
                y: {
                    grid: {
                        color: '#f3f4f6',
                        borderColor: '#e5e7eb'
                    },
                    ticks: {
                        color: '#6b7280',
                        font: {
                            family: 'Inter, system-ui, sans-serif',
                            size: 11
                        }
                    }
                }
            },
            elements: {
                point: {
                    radius: 3,
                    hoverRadius: 5,
                    borderWidth: 2
                },
                line: {
                    tension: 0.4,
                    borderWidth: 2
                }
            }
        };

        // Deep merge defaults with config
        this.deepMerge(config.options, cleanDefaults);

        // Apply clean colors to datasets
        if (config.data && config.data.datasets) {
            config.data.datasets.forEach((dataset, index) => {
                if (!dataset.backgroundColor && !dataset.borderColor) {
                    const colorKeys = Object.keys(colors);
                    const colorKey = colorKeys[index % colorKeys.length];
                    const color = colors[colorKey];

                    if (config.type === 'line') {
                        dataset.borderColor = color;
                        dataset.backgroundColor = color + '20'; // 20% opacity
                    } else if (config.type === 'doughnut' || config.type === 'pie') {
                        dataset.backgroundColor = [
                            colors.primary,
                            colors.secondary,
                            colors.success,
                            colors.warning,
                            colors.danger
                        ];
                    } else {
                        dataset.backgroundColor = color;
                        dataset.borderColor = color;
                    }
                }
            });
        }
    },

    deepMerge: function (target, source) {
        for (const key in source) {
            if (source[key] && typeof source[key] === 'object' && !Array.isArray(source[key])) {
                if (!target[key]) target[key] = {};
                this.deepMerge(target[key], source[key]);
            } else if (target[key] === undefined) {
                target[key] = source[key];
            }
        }
    },

    // Dark theme support
    applyDarkTheme: function (config) {
        const darkColors = {
            text: '#f9fafb',
            grid: '#374151',
            border: '#4b5563',
            background: '#1f2937'
        };

        if (config.options.plugins.legend) {
            config.options.plugins.legend.labels.color = darkColors.text;
        }

        if (config.options.plugins.tooltip) {
            config.options.plugins.tooltip.backgroundColor = darkColors.background;
            config.options.plugins.tooltip.titleColor = darkColors.text;
            config.options.plugins.tooltip.bodyColor = darkColors.text;
            config.options.plugins.tooltip.borderColor = darkColors.border;
        }

        if (config.options.scales) {
            Object.keys(config.options.scales).forEach(scaleKey => {
                const scale = config.options.scales[scaleKey];
                if (scale.grid) {
                    scale.grid.color = darkColors.grid;
                    scale.grid.borderColor = darkColors.border;
                }
                if (scale.ticks) {
                    scale.ticks.color = darkColors.text;
                }
            });
        }
    }
};

// Utility functions
window.downloadFile = function (filename, base64Data, contentType) {
    const linkSource = `data:${contentType};base64,${base64Data}`;
    const downloadLink = document.createElement("a");
    downloadLink.href = linkSource;
    downloadLink.download = filename;
    downloadLink.style.display = "none";
    document.body.appendChild(downloadLink);
    downloadLink.click();
    document.body.removeChild(downloadLink);
};

window.showToast = function (message, type = 'info') {
    const container = document.getElementById('toast-container') || createToastContainer();

    const toast = document.createElement('div');
    toast.className = `toast ${type}`;

    const icons = {
        success: 'fas fa-check-circle',
        error: 'fas fa-exclamation-circle',
        warning: 'fas fa-exclamation-triangle',
        info: 'fas fa-info-circle'
    };

    toast.innerHTML = `
        <i class="${icons[type] || icons.info}" style="color: var(--${type === 'error' ? 'danger' : type === 'warning' ? 'warning' : type === 'success' ? 'success' : 'primary'});"></i>
        <span>${message}</span>
        <button onclick="this.parentElement.remove()" style="margin-left: auto; background: none; border: none; cursor: pointer; color: var(--gray-400);">
            <i class="fas fa-times"></i>
        </button>
    `;

    container.appendChild(toast);

    setTimeout(() => {
        if (toast.parentElement) {
            toast.style.opacity = '0';
            toast.style.transform = 'translateX(100%)';
            setTimeout(() => toast.remove(), 300);
        }
    }, 5000);
};

function createToastContainer() {
    const container = document.createElement('div');
    container.id = 'toast-container';
    container.className = 'toast-container';
    document.body.appendChild(container);
    return container;
}