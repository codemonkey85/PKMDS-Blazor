var charts = new Object();
window.setupChart = (id, config) => {
    var ctx = document.getElementById(id).getContext('2d');
    if (typeof charts[id] !== 'undefined') {
        charts[id].destroy();
    }
    charts[id] = new Chart(ctx, config);
}
