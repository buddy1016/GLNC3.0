// Delivery Status Chart
$(document).ready(function() {
    const ctx = document.getElementById('deliveryChart');
    if (ctx && typeof deliveryData !== 'undefined') {
        try {
            new Chart(ctx, {
                type: 'doughnut',
                data: {
                    labels: ['Completed', 'In Progress', 'Pending'],
                    datasets: [{
                        label: 'Delivery Status',
                        data: [
                            deliveryData.completed || 0,
                            deliveryData.inProgress || 0,
                            deliveryData.pending || 0
                        ],
                        backgroundColor: [
                            '#27ae60',  // Green for Completed
                            '#f39c12',  // Orange for In Progress
                            '#95a5a6'   // Gray for Pending
                        ],
                        borderColor: [
                            '#27ae60',
                            '#f39c12',
                            '#95a5a6'
                        ],
                        borderWidth: 2
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: true,
                    plugins: {
                        legend: {
                            position: 'bottom',
                            labels: {
                                boxWidth: 12,
                                padding: 15,
                                font: {
                                    size: 12
                                }
                            }
                        },
                        tooltip: {
                            callbacks: {
                                label: function(context) {
                                    let label = context.label || '';
                                    if (label) {
                                        label += ': ';
                                    }
                                    const total = context.dataset.data.reduce(function(a, b) { return a + b; }, 0);
                                    const percentage = total > 0 ? ((context.parsed / total) * 100).toFixed(1) : 0;
                                    label += context.parsed + ' (' + percentage + '%)';
                                    return label;
                                }
                            }
                        }
                    }
                }
            });
        } catch (error) {
            console.error('Error initializing chart:', error);
        }
    } else {
        console.warn('Chart canvas or deliveryData not found');
    }
});
