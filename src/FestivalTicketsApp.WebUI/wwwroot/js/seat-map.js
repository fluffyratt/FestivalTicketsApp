let seatsData = [];
let selectedSeatId = null;
let cart = [];

/* ------------------ LOAD DATA --------------------- */
function loadSeatMap(eventId) {
    fetch(`/Event/GetEventSeats/${eventId}`)
        .then(r => r.json())
        .then(seats => {
            seatsData = seats;

            assignCinemaColors(seatsData);
            assignSoldSeats(seatsData);

            renderSeatMap(seatsData);
        });
}

/* ---------- PRICE COLORS ---------- */
function assignCinemaColors(seats) {
    seats.forEach(s => {
        if (s.status === "Sold" || s.status === "InCart" || s.status === "Hold") return;

        if (s.row <= 3) s.fakePrice = 50;
        else if (s.row <= 7) s.fakePrice = 100;
        else s.fakePrice = 250;
    });
}

/* ---------- RANDOM SOLD ---------- */
function assignSoldSeats(seats) {
    const available = seats.filter(s => s.status !== "Sold" && s.status !== "InCart");

    for (let i = 0; i < 3 && i < available.length; i++)
        available[i].status = "Sold";
}

/* ---------- COLOR SELECTOR ---------- */
function getSeatColor(seat) {
    if (seat.status === "Sold") return "#9e9e9e";
    if (seat.status === "InCart") return "#ff4444";
    if (seat.status === "Hold") return "#4da6ff";    // 🔥 NEW COLOR

    if (seat.fakePrice === 50) return "#00c853";
    if (seat.fakePrice === 100) return "#ffeb3b";
    if (seat.fakePrice === 250) return "#ff00cc";
}

/* ---------- DRAW SEATS ---------- */
function renderSeatMap(seats) {

    const canvas = document.getElementById("seat-map");
    const ctx = canvas.getContext("2d");
    ctx.clearRect(0, 0, canvas.width, canvas.height);

    const padding = 40;

    const maxRow = Math.max(...seats.map(s => s.row));
    const rows = new Map();

    seats.forEach(s => {
        if (!rows.has(s.row)) rows.set(s.row, []);
        rows.get(s.row).push(s);
    });

    rows.forEach(r => r.sort((a, b) => a.seat - b.seat));

    const maxSeatsInRow = Math.max(...[...rows.values()].map(r => r.length));

    const availableWidth = canvas.width - padding * 2;
    const availableHeight = canvas.height - padding * 2;

    const stepX = availableWidth / (maxSeatsInRow + 1);
    const stepY = availableHeight / (maxRow + 1);

    const radius = Math.min(stepX * 0.35, stepY * 0.35);

    let rowIndex = 0;

    rows.forEach((rowSeats, rowNum) => {

        const seatsInThisRow = rowSeats.length;
        const offsetSeats = (maxSeatsInRow - seatsInThisRow) / 2;

        const y = padding + rowIndex * stepY;

        rowSeats.forEach((seatObj, idx) => {
            const col = offsetSeats + idx + 1;
            const x = padding + col * stepX;

            seatObj._x = x;
            seatObj._y = y;
            seatObj._r = radius;

            ctx.beginPath();
            ctx.arc(x, y, radius, 0, Math.PI * 2);

            ctx.fillStyle = getSeatColor(seatObj);
            ctx.fill();

            ctx.lineWidth = selectedSeatId === seatObj.id ? 4 : 2;
            ctx.strokeStyle = selectedSeatId === seatObj.id ? "#0066ff" : "#222";
            ctx.stroke();
        });

        rowIndex++;
    });
}

/* ---------- SELECT SEAT (WITH HOLD) ---------- */
document.addEventListener("click", function (e) {
    const canvas = document.getElementById("seat-map");
    const rect = canvas.getBoundingClientRect();
    const mx = e.clientX - rect.left;
    const my = e.clientY - rect.top;

    let found = null;
    seatsData.forEach(s => {
        if (Math.hypot(mx - s._x, my - s._y) <= s._r) found = s;
    });

    if (!found) return;
    if (found.status === "Sold") return;

    // ---------- CALL HOLD API ----------
    fetch(`/Event/HoldSeat?seatId=${found.id}`, { method: "POST" })
        .then(r => r.json())
        .then(data => {
            if (data.success) {
                found.status = "Hold";
                selectedSeatId = found.id;
                renderSeatMap(seatsData);
                showSeatInfo(found);
            } else {
                alert("Seat is reserved by another user!");
            }
        });
});

/* ---------- HOVER ---------- */
document.addEventListener("mousemove", function (e) {
    const canvas = document.getElementById("seat-map");
    const rect = canvas.getBoundingClientRect();
    const mx = e.clientX - rect.left;
    const my = e.clientY - rect.top;

    let hover = false;

    seatsData.forEach(s => {
        if (Math.hypot(mx - s._x, my - s._y) <= s._r)
            hover = true;
    });

    canvas.style.cursor = hover ? "pointer" : "default";
});

/* ---------- SHOW SELECTED SEAT ---------- */
function showSeatInfo(seat) {
    document.getElementById("seat-info").style.display = "block";
    document.getElementById("si-row").innerHTML = "Row: " + seat.row;
    document.getElementById("si-seat").innerHTML = "Seat: " + seat.seat;
    document.getElementById("si-price").innerHTML = "Price: " + seat.fakePrice + "$";

    document.getElementById("add-to-cart").onclick = () => addToCart(seat);
}

/* ---------- ADD TO CART ---------- */
function addToCart(seat) {
    if (seat.status === "InCart") return;

    seat.status = "InCart";
    cart.push(seat);

    updateCart();
    renderSeatMap(seatsData);
}

/* ---------- UPDATE CART ---------- */
function updateCart() {
    document.getElementById("cart").style.display = "block";

    let html = "";
    let total = 0;

    cart.forEach(s => {
        html += `<li>Row ${s.row}, Seat ${s.seat} — ${s.fakePrice}$</li>`;
        total += s.fakePrice;
    });

    document.getElementById("cart-items").innerHTML = html;
    document.getElementById("cart-total").innerText = total;
}
