// Delivery Status Chart
$(document).ready(function() {
    const ctx = document.getElementById('deliveryChart');
    if (ctx && typeof deliveryData !== 'undefined') {
        try {
            new Chart(ctx, {
                type: 'doughnut',
                data: {
                    labels: ['Terminées', 'Retournées', 'En attente'],
                    datasets: [{
                        label: 'Statut des livraisons',
                        data: [
                            deliveryData.completed || 0,
                            deliveryData.returned || 0,
                            deliveryData.pending || 0
                        ],
                        backgroundColor: [
                            '#27ae60',  // Green for Terminées
                            '#e74c3c',  // Red for Retournées
                            '#95a5a6'   // Gray for En attente
                        ],
                        borderColor: [
                            '#27ae60',
                            '#e74c3c',
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
            console.error('Erreur lors de l\'initialisation du graphique:', error);
        }
    } else {
        console.warn('Canvas du graphique ou deliveryData non trouvé');
    }
});
