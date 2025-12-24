// Chart.js rendering for Planning Poker results

let pieChartInstance = null;
let barChartInstance = null;

window.renderCharts = function (tally, average) {
    if (!tally || Object.keys(tally).length === 0) return;

    // Destroy existing charts
    if (pieChartInstance) {
        pieChartInstance.destroy();
        pieChartInstance = null;
    }
    if (barChartInstance) {
        barChartInstance.destroy();
        barChartInstance = null;
    }

    // Prepare data
    const labels = Object.keys(tally).sort();
    const data = labels.map(key => tally[key]);
    const colors = [
        '#FF6384', '#36A2EB', '#FFCE56', '#4BC0C0', '#9966FF',
        '#FF9F40', '#FF6384', '#C9CBCF', '#4BC0C0', '#FF6384'
    ];

    // Render Pie Chart
    const pieCtx = document.getElementById('pieChart');
    if (pieCtx) {
        pieChartInstance = new Chart(pieCtx, {
            type: 'pie',
            data: {
                labels: labels,
                datasets: [{
                    data: data,
                    backgroundColor: colors.slice(0, labels.length),
                    borderWidth: 2,
                    borderColor: '#fff'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: {
                            font: {
                                size: 14
                            },
                            padding: 15
                        }
                    },
                    title: {
                        display: true,
                        text: `Vote Distribution (Avg: ${average?.toFixed(2) || 'N/A'})`,
                        font: {
                            size: 16
                        }
                    }
                }
            }
        });
    }

    // Render Bar Chart
    const barCtx = document.getElementById('barChart');
    if (barCtx) {
        barChartInstance = new Chart(barCtx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Number of Votes',
                    data: data,
                    backgroundColor: '#0d6efd',
                    borderColor: '#0a58ca',
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            stepSize: 1
                        },
                        title: {
                            display: true,
                            text: 'Number of Votes'
                        }
                    },
                    x: {
                        title: {
                            display: true,
                            text: 'Vote Value'
                        }
                    }
                },
                plugins: {
                    legend: {
                        display: false
                    },
                    title: {
                        display: true,
                        text: 'Vote Distribution',
                        font: {
                            size: 16
                        }
                    }
                }
            }
        });
    }
};

// Cleanup on page unload
window.addEventListener('beforeunload', function () {
    if (pieChartInstance) pieChartInstance.destroy();
    if (barChartInstance) barChartInstance.destroy();
});
