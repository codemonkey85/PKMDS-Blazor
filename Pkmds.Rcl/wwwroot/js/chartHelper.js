window.chartHelper = {
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
        console.log('Chart scale updated:', min, max, stepSize);
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

        chart.options.scales.r.pointLabels.color = function (context) {
            const modifier = natureModifiers && natureModifiers[context.index] ? natureModifiers[context.index] : 0;
            if (modifier === 1) return '#FF0000'; // Red for increased
            if (modifier === -1) return '#0000FF'; // Blue for decreased
            return '#666'; // Default gray for neutral
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
        console.log('Labels updated with values and nature modifiers');
    }
};
