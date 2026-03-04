// site.js → Dashboard için SignalR bağlantısı ve tablo güncelleme

// Sayfadaki hidden <h6 id="ip">@Model</h6> içinden IP al
const IpElement = document.getElementById("ip");
const Ip = IpElement ? IpElement.textContent : "localhost";

// SignalR bağlantısı kur
const connection = new signalR.HubConnectionBuilder()
    .withUrl(`http://${Ip}:5199/hubs/phone?phone=dashboard`) // Program.cs -> app.MapHub<CallHub>("/hubs/phone")
    .configureLogging(signalR.LogLevel.Information)
    .build();

// Sunucudan kullanıcı listesi geldiğinde
connection.on("UserList", (users) => {
    console.log("📡 UserList alındı:", users);
    updateUserTable(users);
});

// Opsiyonel: mesaj event'i
connection.on("ReceiveMessage", (phone, message) => {
    console.log(`💬 [${phone}] -> ${message}`);
});

// Bağlantıyı başlat
connection.start()
    .then(() => {
        console.log("✅ Dashboard SignalR connected.");
        // İstersen ilk bağlanınca server'a haber verebilirsin:
        // connection.invoke("SendMessage", "dashboard", "Dashboard joined");
    })
    .catch(err => console.error("❌ Connection error:", err));

// Kullanıcı tablosunu güncelle
function updateUserTable(users) {
    const tbody = document.querySelector("#userTable tbody");
    const countBadge = document.querySelector("#userCount");
    tbody.innerHTML = "";

    if (!users || users.length === 0) {
        tbody.innerHTML = `<tr><td colspan="9" class="text-center text-muted py-3">Hiç kullanıcı yok</td></tr>`;
        countBadge.textContent = "0 Kullanıcı";
        return;
    }

    countBadge.textContent = `${users.length} Kullanıcı`;

    users.forEach(u => {
        const row = document.createElement("tr");
        row.id = u?.callerId || `row-${Math.random().toString(36).substr(2, 9)}`;

        row.innerHTML = `
            <td>${u?.callerId || "-"}</td>
            <td>${u?.userId || "-"}</td>
            <td>${u?.productId || "-"}</td>
            <td>${u?.firstName || "-"}</td>
            <td>${u?.lastName || "-"}</td>
            <td>${u?.phone || "-"}</td>
            <td>${u?.email || "-"}</td>
            <td>${u?.host || "-"}</td>
            <td>
                <span class="badge ${u?.status === "Online" ? "bg-success" : "bg-secondary"}">
                    ${u?.status || "Offline"}
                </span>
            </td>
        `;

        tbody.appendChild(row);
    });
}


