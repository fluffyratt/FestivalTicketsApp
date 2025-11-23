$(document).ready(() => {
    let latitude = Number.parseFloat($("#hostLat").val().replace(',', '.'));
    let longitude = Number.parseFloat($("#hostLong").val().replace(',', '.'));

    let zoomValue = 15;

    let hostPosition = new google.maps.LatLng(latitude, longitude);

    let mapOptions = {
        center: hostPosition,
        zoom: zoomValue,
        minZoom: zoomValue,
        mapTypeId: google.maps.MapTypeId.ROADMAP
    };

    let mapCanvas = $("#hostPosition")[0];

    let map = new google.maps.Map(mapCanvas, mapOptions);

    let marker = new google.maps.Marker({
        position: hostPosition,
        animation: google.maps.Animation.BOUNCE,
    });

    marker.setMap(map);
});



