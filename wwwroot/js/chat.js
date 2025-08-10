document.addEventListener("DOMContentLoaded", function () {
    console.log("✅ DOM загружен");

    let selectedFiles = [];
    window.selectedFiles = selectedFiles; // для отладки в консоли

    // Проверка элементов
    const fileInput = document.getElementById("fileInput");
    const messageInput = document.getElementById("messageInput");
    const sendButton = document.getElementById("sendButton");
    const messagesList = document.getElementById("messages");
    const chatIdInput = document.getElementById("chatId");
    const userNameInput = document.getElementById("currentUserId");

    console.log("Элементы:", {
        fileInput, messageInput, sendButton, messagesList, chatIdInput, userNameInput
    });

    if (!fileInput || !messageInput || !sendButton || !messagesList || !chatIdInput || !userNameInput) {
        console.error("❌ Не все элементы найдены!");
        return;
    }

    // 1. Обработчик выбора файла
    fileInput.addEventListener("change", function (e) {
        console.log("🎯 change: файлы выбраны", e.target.files);
        const files = Array.from(e.target.files);
        if (files.length === 0) return;

        selectedFiles.push(...files);
        console.log("📂 selectedFiles:", selectedFiles);

        updateAttachmentsPreview();
    });

    // 2. Превью вложений
    function updateAttachmentsPreview() {
        const preview = document.getElementById("attachmentsPreview");
        if (!preview) {
            console.error("❌ attachmentsPreview не найден");
            return;
        }

        preview.innerHTML = "";

        selectedFiles.forEach((file, index) => {
            console.log("🖼️ Обработка файла:", file.name, file.type);

            const item = document.createElement("div");
            item.className = "attachment-item d-flex align-items-center bg-light p-1 rounded";
            item.style.gap = "8px";
            item.style.fontSize = "0.9em";

            if (file.type.startsWith("image/")) {
                const img = document.createElement("img");
                img.src = URL.createObjectURL(file);
                img.style.width = "40px";
                img.style.height = "40px";
                img.style.objectFit = "cover";
                img.style.borderRadius = "4px";
                item.appendChild(img);
            } else {
                const icon = document.createElement("i");
                icon.className = "fas fa-file text-muted";
                item.appendChild(icon);
            }

            const name = document.createElement("span");
            name.textContent = file.name.length > 15 ? file.name.substring(0, 12) + "..." : file.name;
            item.appendChild(name);

            const removeBtn = document.createElement("i");
            removeBtn.className = "fas fa-times text-danger";
            removeBtn.style.cursor = "pointer";
            removeBtn.onclick = () => {
                selectedFiles.splice(index, 1);
                updateAttachmentsPreview();
            };
            item.appendChild(removeBtn);

            preview.appendChild(item);
        });
    }

    // 3. Отправка
    async function sendMessage() {
        const chatId = parseInt(document.getElementById("chatId").value);
        const user = document.getElementById("userName").value;
        const messageInput = document.getElementById("messageInput");
        const message = messageInput.value.trim();

        // 1. Сначала отправь файлы (если есть)
        if (selectedFiles.length > 0) {
            const formData = new FormData();
            selectedFiles.forEach(f => formData.append("files", f));
            formData.append("chatId", chatId);

            try {
                const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
                formData.append("__RequestVerificationToken", token);

                const response = await fetch("/Chat/UploadAttachment", {
                    method: "POST",
                    body: formData
                });

                if (!response.ok) throw new Error("Ошибка загрузки");

                const attachments = await response.json();

                // Отправляем каждый файл как отдельное сообщение
                for (const att of attachments) {
                    await connection.invoke("SendAttachment", chatId, user, att.url, att.name, att.type);
                }
            } catch (err) {
                console.error("Ошибка отправки файла:", err);
                alert("Не удалось отправить файл");
                return;
            }
        }

        // 2. Потом отправь текст
        if (message) {
            try {
                await connection.invoke("SendMessage", chatId, user, message);
            } catch (err) {
                console.error("Ошибка отправки текста:", err);
                alert("Не удалось отправить сообщение");
                return;
            }
        }

        // 3. Очистка
        messageInput.value = "";
        selectedFiles = [];
        updateAttachmentsPreview();
    }

    // Привяжем функцию глобально
    window.sendMessage = sendMessage;

    // Кнопка
    sendButton.addEventListener("click", sendMessage);

    // Enter
    messageInput.addEventListener("keypress", function (e) {
        if (e.key === "Enter") {
            e.preventDefault();
            sendMessage();
        }
    });

    // 4. Подключение SignalR
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/chatHub")
        .build();

    connection.on("ReceiveMessage", function (senderId, message, sentAt, attachments) {
        console.log("📩 Получено сообщение:", { senderId, message, sentAt, attachments });

        const currentUserId = document.getElementById("currentUserId").value;
        const isMe = senderId === currentUserId;

        const messageElement = document.createElement("li");
        messageElement.className = `d-flex mb-2 ${isMe ? "justify-content-end" : "justify-content-start"}`;

        const bubble = document.createElement("div");
        bubble.className = `message-bubble ${isMe ? "me" : "other"}`;

        if (message) {
            bubble.innerHTML += message;
        }

        if (attachments && attachments.length > 0) {
            attachments.forEach(att => {
                const attDiv = document.createElement("div");
                attDiv.className = "attachment-preview mt-1";

                if (att.type?.startsWith("image/")) {
                    const img = document.createElement("img");
                    img.src = att.url;
                    img.style.maxWidth = "200px";
                    img.style.borderRadius = "8px";
                    attDiv.appendChild(img);
                } else {
                    const link = document.createElement("a");
                    link.href = att.url;
                    link.target = "_blank";
                    link.textContent = att.name || "Файл";
                    link.className = "file-link";
                    attDiv.appendChild(link);
                }
                bubble.appendChild(attDiv);
            });
        }

        const time = document.createElement("div");
        time.className = "message-time";
        time.textContent = new Date(sentAt).toLocaleString();
        bubble.appendChild(time);

        messageElement.appendChild(bubble);
        document.getElementById("messages").appendChild(messageElement);
        scrollToBottom();
    });

    connection.start()
        .then(() => {
            const chatId = chatIdInput.value;
            console.log("🔗 SignalR подключён, заходим в чат:", chatId);
            return connection.invoke("JoinChat", chatId);
        })
        .catch(err => console.error("❌ Ошибка SignalR:", err));

    function scrollToBottom() {
        const container = document.querySelector("#messages");
        if (container) {
            container.parentElement.scrollTop = container.parentElement.scrollHeight;
        }
    }

    scrollToBottom();
});