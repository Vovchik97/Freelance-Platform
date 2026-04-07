document.addEventListener("DOMContentLoaded", function () {
    const projectIdEl = document.getElementById("projectId");
    const currentUserIdEl = document.getElementById("currentUserId");
    const messagesContainer = document.getElementById("groupMessages");
    const messageInput = document.getElementById("groupMessageInput");
    const sendBtn = document.getElementById("groupSendBtn");
    const fileInput = document.getElementById("groupFileInput");
    const attachmentsPreview = document.getElementById("groupAttachmentsPreview");

    if (!projectIdEl || !messagesContainer || !messageInput) return;

    const projectId = parseInt(projectIdEl.value);
    const currentUserId = currentUserIdEl?.value;
    let selectedFiles = [];

    // ===== SignalR =====
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/groupChatHub")
        .build();

    connection.on("ReceiveGroupMessage", function (
        id, senderId, senderName, text, sentAt, mentions, attachments
    ) {
        appendMessage(senderId, senderName, text, sentAt, attachments || []);
    });

    connection.on("TaskStatusUpdated", function (taskId, statusInt, statusText) {
        if (window.location.href.includes("tab=tasks")) {
            location.reload();
        }
    });

    connection.start()
        .then(() => connection.invoke("JoinProjectGroup", projectId.toString()))
        .catch(err => console.error(err));

    // ===== Файлы =====
    fileInput?.addEventListener("change", function () {
        selectedFiles.push(...Array.from(this.files));
        renderPreview();
        this.value = "";
    });

    // Drag & drop на поле ввода
    messageInput?.addEventListener("dragover", (e) => {
        e.preventDefault();
        messageInput.style.borderColor = "var(--primary)";
    });
    messageInput?.addEventListener("dragleave", () => {
        messageInput.style.borderColor = "";
    });
    messageInput?.addEventListener("drop", (e) => {
        e.preventDefault();
        messageInput.style.borderColor = "";
        selectedFiles.push(...Array.from(e.dataTransfer.files));
        renderPreview();
    });

    // Вставка из буфера
    messageInput?.addEventListener("paste", (e) => {
        if (e.clipboardData.files.length > 0) {
            e.preventDefault();
            selectedFiles.push(...Array.from(e.clipboardData.files));
            renderPreview();
        }
    });

    function renderPreview() {
        if (!attachmentsPreview) return;
        attachmentsPreview.innerHTML = "";

        if (selectedFiles.length === 0) {
            attachmentsPreview.style.display = "none";
            return;
        }

        attachmentsPreview.style.display = "flex";

        selectedFiles.forEach((file, index) => {
            const item = document.createElement("div");
            item.className = "attachment-item";

            if (file.type.startsWith("image/")) {
                const img = document.createElement("img");
                img.src = URL.createObjectURL(file);
                item.appendChild(img);
            } else {
                const icon = document.createElement("span");
                icon.style.fontSize = "24px";
                icon.textContent = "📄";
                item.appendChild(icon);
            }

            const name = document.createElement("span");
            name.textContent = file.name.length > 15
                ? file.name.substring(0, 12) + "..."
                : file.name;
            item.appendChild(name);

            const removeBtn = document.createElement("div");
            removeBtn.className = "remove-attachment";
            removeBtn.onclick = () => {
                selectedFiles.splice(index, 1);
                renderPreview();
            };
            item.appendChild(removeBtn);

            attachmentsPreview.appendChild(item);
        });
    }

    // ===== Отправка =====
    async function sendMessage() {
        const text = messageInput.value.trim();
        const filesToSend = [...selectedFiles];

        if (!text && filesToSend.length === 0) return;

        // Сразу сбрасываем UI
        messageInput.value = "";
        selectedFiles = [];
        renderPreview();

        let attachments = [];

        // Загружаем файлы через тот же UploadAttachment что в обычном чате
        if (filesToSend.length > 0) {
            const formData = new FormData();
            filesToSend.forEach(f => formData.append("files", f));

            const token = document.querySelector(
                'input[name="__RequestVerificationToken"]'
            )?.value;
            if (token) formData.append("__RequestVerificationToken", token);

            try {
                const response = await fetch("/Chat/UploadAttachment", {
                    method: "POST",
                    body: formData
                });
                if (!response.ok) throw new Error("Upload failed");
                attachments = await response.json();
            } catch {
                alert("Не удалось загрузить файлы");
                return;
            }
        }

        try {
            await connection.invoke("SendGroupMessage", projectId, text, attachments);
        } catch (e) {
            console.error(e);
            alert("Ошибка отправки сообщения");
        }
    }

    sendBtn?.addEventListener("click", sendMessage);
    messageInput?.addEventListener("keypress", function (e) {
        if (e.key === "Enter" && !e.shiftKey) {
            e.preventDefault();
            sendMessage();
        }
    });

    // ===== Рендер сообщения =====
    function appendMessage(senderId, senderName, text, sentAt, attachments) {
        const isMe = senderId === currentUserId;

        const li = document.createElement("li");
        li.className = `d-flex mb-2 ${isMe ? "justify-content-end" : "justify-content-start"}`;
        li.style.listStyle = "none";

        const bubble = document.createElement("div");
        bubble.className = `message-bubble ${isMe ? "me" : "other"}`;

        if (!isMe) {
            const nameEl = document.createElement("div");
            nameEl.className = "message-sender";
            nameEl.textContent = senderName;
            bubble.appendChild(nameEl);
        }

        // Текст с подсветкой mentions
        if (text) {
            const textEl = document.createElement("div");
            textEl.innerHTML = text.replace(
                /@(\w+)/g,
                '<strong style="color:var(--primary-light)">@$1</strong>'
            );
            bubble.appendChild(textEl);
        }

        // Вложения — та же логика что в chat.js
        if (attachments && attachments.length > 0) {
            const images = attachments.filter(a => a.type?.startsWith("image/"));
            const videos = attachments.filter(a => a.type?.startsWith("video/"));
            const audios = attachments.filter(a => a.type?.startsWith("audio/"));
            const files  = attachments.filter(a =>
                !a.type?.startsWith("image/") &&
                !a.type?.startsWith("video/") &&
                !a.type?.startsWith("audio/")
            );

            if (images.length > 0) {
                const grid = document.createElement("div");
                grid.className = "image-grid";
                images.forEach(img => {
                    const a = document.createElement("a");
                    a.href = img.url;
                    a.target = "_blank";
                    a.className = "image-link";
                    const imgEl = document.createElement("img");
                    imgEl.src = img.url;
                    imgEl.className = "message-image";
                    imgEl.loading = "lazy";
                    a.appendChild(imgEl);
                    grid.appendChild(a);
                });
                bubble.appendChild(grid);
            }

            if (videos.length > 0) {
                const grid = document.createElement("div");
                grid.className = "image-grid";
                videos.forEach(vid => {
                    const video = document.createElement("video");
                    video.src = vid.url;
                    video.controls = true;
                    video.className = "message-video";
                    video.style.width = "100%";
                    grid.appendChild(video);
                });
                bubble.appendChild(grid);
            }

            if (audios.length > 0) {
                const grid = document.createElement("div");
                grid.className = "audio-grid";
                audios.forEach(aud => {
                    const audio = document.createElement("audio");
                    audio.src = aud.url;
                    audio.controls = true;
                    audio.style.width = "100%";
                    grid.appendChild(audio);
                });
                bubble.appendChild(grid);
            }

            if (files.length > 0) {
                const grid = document.createElement("div");
                grid.className = "attachments-grid";
                files.forEach(file => {
                    const a = document.createElement("a");
                    a.href = file.url;
                    a.target = "_blank";
                    a.className = "file-attachment";
                    const ext = file.name.split('.').pop()?.toUpperCase() || "";
                    a.innerHTML = `
                        <div class="file-icon-box"><span>${ext}</span></div>
                        <div class="file-info">
                            <div class="file-name">${file.name}</div>
                        </div>`;
                    grid.appendChild(a);
                });
                bubble.appendChild(grid);
            }
        }

        const timeEl = document.createElement("div");
        timeEl.className = "message-time";
        timeEl.textContent = new Date(sentAt).toLocaleString();
        bubble.appendChild(timeEl);

        li.appendChild(bubble);
        messagesContainer.appendChild(li);
        messagesContainer.scrollTop = messagesContainer.scrollHeight;
    }

    // ===== Кнопки смены статуса задач =====
    document.querySelectorAll(".task-status-btn").forEach(btn => {
        btn.addEventListener("click", async function () {
            const taskId = parseInt(this.dataset.taskId);
            const status = parseInt(this.dataset.status);
            try {
                await connection.invoke("UpdateTaskStatus", projectId, taskId, status);
                setTimeout(() => location.reload(), 400);
            } catch (e) {
                console.error(e);
            }
        });
    });

    // Скролл вниз при открытии
    messagesContainer.scrollTop = messagesContainer.scrollHeight;
});