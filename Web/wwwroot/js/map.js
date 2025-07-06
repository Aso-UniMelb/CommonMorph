$('.map').html(`<h2>Languages</h2>
  <div id="mapid" style="height: 500px; z-index: 2;"></div>`);

var map = L.map('mapid', { minZoom: 1, maxZoom: 9 });
map.setView([20, 0], 2);
L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
  attribution:
    '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
}).addTo(map);

$.ajax({
  url: '/Lang/mapdata',
  type: 'GET',
  success: function (data) {
    data.forEach(function (item) {
      L.marker([item.latitude, item.longitude])
        .addTo(map)
        .bindPopup(item.title + ' [' + item.code + ']');
    });
  },
  error: function (data) {
    console.log(data);
  },
});

$(document).ready(function () {});
