const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub")
    .build();

connection.on("ReceiveMessage", function (senderId, message, sentAt) {
    const currentUserId = document.getElementById("currentUserId").value
    const isMe = senderId === currentUserId;

    const messageElement = document.createElement("li");
    messageElement.className = `d-flex mb-2 ${isMe ? "justify-content-end" : "justify-content-start"}`;

    // Форматируем время как в C#: "M/d/yyyy h:mm tt" (например, "10/5/2025 3:45 PM")
    const date = new Date(sentAt);
    const formattedTime = date.toLocaleString(); // Можно кастомизировать, если нужно

    const bubble = document.createElement("div");
    bubble.className = `message-bubble ${isMe ? "me" : "other"}`;
    bubble.innerHTML = `${message}<div class="message-time">${formattedTime}</div>`;

    messageElement.appendChild(bubble);
    document.getElementById("messages").appendChild(messageElement);

    // Прокрутка вниз
    scrollToBottom();
});

connection.start().then(function () {
    const chatId = document.getElementById("chatId").value;
    connection.invoke("JoinChat", chatId)
        .catch(err => console.error(err));
}).catch(err => console.error(err));

document.getElementById("messageInput").addEventListener("keypress", function(event) {
    if (event.key === "Enter") {
        event.preventDefault();
        sendMessage();
    }
});

function sendMessage() {
    const chatId = parseInt(document.getElementById("chatId").value);
    const user = document.getElementById("userName").value;
    const message = document.getElementById("messageInput").value;

    if (message.trim() === "") return;

    connection.invoke("SendMessage", chatId, user, message)
        .catch(err => {
            console.error(err);
            alert("Ошибка при отправке сообщения");
        });

    document.getElementById('messageInput').value = "";
}

function scrollToBottom() {
    const messagesContainer = document.querySelector("#messages");
    if (messagesContainer) {
        messagesContainer.parentElement.scrollTop = messagesContainer.parentElement.scrollHeight;
    }
}

// Прокрутка при первом открытии страницы
document.addEventListener("DOMContentLoaded", () => {
    scrollToBottom();
});