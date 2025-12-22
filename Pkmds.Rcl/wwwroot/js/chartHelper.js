window.chartHelper = {
    refreshChartColorsWithTheme: function (chartId, isDarkMode) {
        const canvas = document.getElementById(chartId);
        if (!canvas) {
            console.error('Canvas not found for refresh:', chartId);
            return;
        }

        const chart = Chart.getChart(canvas);
        if (!chart) {
            console.error('Chart not found for refresh:', chartId);
            return;
        }

        console.log('Refreshing chart colors with theme:', isDarkMode ? 'dark' : 'light');

        // Store the current theme on the chart object
        chart._isDarkMode = isDarkMode;

        // Update grid colors
        if (chart.options.scales?.r?.grid) {
            chart.options.scales.r.grid.color = isDarkMode ? 'rgba(255, 255, 255, 0.3)' : 'rgba(0, 0, 0, 0.2)';
            console.log('Grid color updated to:', chart.options.scales.r.grid.color);
        }

        // Update point label colors based on stored nature modifiers
        if (chart._natureModifiers && chart.options.scales?.r?.pointLabels) {
            const natureModifiers = chart._natureModifiers;
            // Recreate the color function with the new theme
            chart.options.scales.r.pointLabels.color = function (context) {
                // Read the current theme from the chart object each time
                const currentDarkMode = context.chart._isDarkMode || false;
                const modifier = natureModifiers && natureModifiers[context.index] ? natureModifiers[context.index] : 0;
                // Material Design color palette for better contrast
                if (modifier === 1) return currentDarkMode ? '#FF6B6B' : '#D32F2F';
                if (modifier === -1) return currentDarkMode ? '#4DABF7' : '#1976D2';
                return currentDarkMode ? '#E0E0E0' : '#424242';
            };
            console.log('Point label colors updated');
        }

        // Force a complete re-render by updating with 'active' mode
        chart.update('active');
        // Then do a full update
        setTimeout(() => {
            chart.update();
            console.log('Chart colors refreshed successfully');
        }, 0);
    },

    isDarkMode: function () {
        // Check MudBlazor's dark mode class on body or html
        const hasDarkClass = document.documentElement.classList.contains('mud-theme-dark') ||
            document.body.classList.contains('mud-theme-dark');
        const systemPreference = window.matchMedia('(prefers-color-scheme: dark)').matches;

        console.log('isDarkMode check:', {
            documentElement: document.documentElement.classList.contains('mud-theme-dark'),
            body: document.body.classList.contains('mud-theme-dark'),
            systemPreference: systemPreference,
            result: hasDarkClass || systemPreference
        });

        return hasDarkClass || systemPreference;
    },

    refreshChartColors: function (chartId) {
        const darkMode = this.isDarkMode();
        this.refreshChartColorsWithTheme(chartId, darkMode);
    },

    setRadarScale: function (chartId, min, max, stepSize) {
        // Find the chart canvas element by ID
        const canvas = document.getElementById(chartId);
        if (!canvas) {
            console.error('Canvas not found:', chartId);
            return;
        }

        // Get the Chart.js instance from the canvas
        const chart = Chart.getChart(canvas);
        if (!chart) {
            console.error('Chart not found for canvas:', chartId);
            return;
        }

        const darkMode = this.isDarkMode();

        // Store the initial theme on the chart
        chart._isDarkMode = darkMode;

        // Update the scale options
        if (!chart.options.scales) {
            chart.options.scales = {};
        }
        if (!chart.options.scales.r) {
            chart.options.scales.r = {};
        }

        chart.options.scales.r.min = min;
        chart.options.scales.r.max = max;

        if (!chart.options.scales.r.ticks) {
            chart.options.scales.r.ticks = {};
        }
        chart.options.scales.r.ticks.stepSize = stepSize;
        chart.options.scales.r.ticks.display = false; // Hide the tick labels

        // Configure grid lines to show only the outer border
        if (!chart.options.scales.r.grid) {
            chart.options.scales.r.grid = {};
        }
        chart.options.scales.r.grid.circular = false; // Use angular (hexagonal) grid
        chart.options.scales.r.grid.lineWidth = function (context) {
            // Show only the outermost line (at max value)
            return context.index === context.chart.scales.r.ticks.length - 1 ? 1 : 0;
        };
        // Set grid line color based on theme - darker in light mode for better visibility
        chart.options.scales.r.grid.color = darkMode ? 'rgba(255, 255, 255, 0.3)' : 'rgba(0, 0, 0, 0.2)';

        // Configure point labels (the stat names)
        if (!chart.options.scales.r.pointLabels) {
            chart.options.scales.r.pointLabels = {};
        }
        chart.options.scales.r.pointLabels.font = {
            size: 12,
            weight: 'bold'
        };

        // Hide legend
        if (!chart.options.plugins) {
            chart.options.plugins = {};
        }
        if (!chart.options.plugins.legend) {
            chart.options.plugins.legend = {};
        }
        chart.options.plugins.legend.display = false;

        // Update the chart to apply changes
        chart.update();
        console.log('Chart scale updated:', min, max, stepSize, 'dark mode:', darkMode);
    },

    updateLabelsWithValues: function (chartId, labels, values, natureModifiers) {
        const canvas = document.getElementById(chartId);
        if (!canvas) {
            console.error('Canvas not found:', chartId);
            return;
        }

        const chart = Chart.getChart(canvas);
        if (!chart) {
            console.error('Chart not found for canvas:', chartId);
            return;
        }

        const darkMode = this.isDarkMode();

        // Store theme on chart
        chart._isDarkMode = darkMode;

        // Store original labels for tooltip use
        const originalLabels = [...labels];

        // Create new labels with values on separate lines using array format
        // Add arrows for nature modifiers: 1 = increased (+), -1 = decreased (-), 0 = neutral
        const newLabels = labels.map((label, index) => {
            const modifier = natureModifiers && natureModifiers[index] ? natureModifiers[index] : 0;
            const arrow = modifier === 1 ? ' +' : modifier === -1 ? ' -' : '';
            return [label + arrow, values[index].toString()];
        });
        chart.data.labels = newLabels;

        // Apply color styling based on nature modifiers
        if (!chart.options.scales.r.pointLabels) {
            chart.options.scales.r.pointLabels = {};
        }

        chart.options.scales.r.pointLabels.font = {
            size: 12,
            weight: 'bold'
        };

        // Store nature modifiers for theme change handling
        chart._natureModifiers = natureModifiers;

        // Use colors with better contrast for both light and dark mode
        // Make this function read from the chart's stored theme
        chart.options.scales.r.pointLabels.color = function (context) {
            const currentDarkMode = context.chart._isDarkMode || false;
            const modifier = natureModifiers && natureModifiers[context.index] ? natureModifiers[context.index] : 0;
            // Material Design color palette for better contrast
            if (modifier === 1) return currentDarkMode ? '#FF6B6B' : '#D32F2F';
            if (modifier === -1) return currentDarkMode ? '#4DABF7' : '#1976D2';
            return currentDarkMode ? '#E0E0E0' : '#424242';
        };

        // Configure custom tooltip
        if (!chart.options.plugins) {
            chart.options.plugins = {};
        }
        if (!chart.options.plugins.tooltip) {
            chart.options.plugins.tooltip = {};
        }

        chart.options.plugins.tooltip.displayColors = false; // Remove the color box
        chart.options.plugins.tooltip.callbacks = {
            title: function () {
                return ''; // Remove title
            },
            label: function (context) {
                const index = context.dataIndex;
                const label = originalLabels[index];
                const value = values[index];
                return `${label}: ${value}`;
            }
        };

        chart.update();
        console.log('Labels updated with values and nature modifiers, dark mode:', darkMode);
    }
};
