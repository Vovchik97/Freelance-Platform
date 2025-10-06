document.addEventListener("DOMContentLoaded", function () {
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/chatHub")
        .build();

    connection.start()
        .then(() => connection.invoke("JoinAdminsGroup"))
        .catch(err => console.error(err.toString()));

    document.querySelectorAll(".close-chat-btn").forEach(btn => {
        btn.addEventListener("click", async function () {
            const chatId = this.dataset.chatId;

            try {
                await connection.invoke("AdminLeaveChat", parseInt(chatId));
                location.reload();
            } catch (e) {
                console.error(e);
            }
        });
    });

    connection.on("SupportRequest", function (chatId, userId) {
        alert("Новый запрос поддержки: чат #" + chatId);
    });
});
