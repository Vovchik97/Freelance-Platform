document.addEventListener("DOMContentLoaded", function () {
    let selectedFiles = [];

    const fileInput = document.getElementById("fileInput");
    const messageInput = document.getElementById("messageInput");
    const sendButton = document.getElementById("sendButton");
    const messagesList = document.getElementById("messages");
    const chatIdInput = document.getElementById("chatId");
    const userNameInput = document.getElementById("userName");
    const currentUserIdInput = document.getElementById("currentUserId");
    const attachmentsPreview = document.getElementById("attachmentsPreview");

    if (!fileInput || !messageInput || !sendButton || !messagesList || !chatIdInput || !userNameInput || !currentUserIdInput) {
        return;
    }

    const maxHeight = 150;

    function autoResizeTextarea() {
        messageInput.style.height = 'auto';
        if (messageInput.scrollHeight > maxHeight) {
            messageInput.style.height = maxHeight + 'px';
            messageInput.style.overflowY = 'auto';
        } else {
            messageInput.style.height = messageInput.scrollHeight + 'px';
            messageInput.style.overflowY = 'hidden';
        }
    }

    messageInput.style.resize = "none";
    messageInput.style.overflowY = 'hidden';
    messageInput.addEventListener('input', autoResizeTextarea);
    autoResizeTextarea();

    fileInput.addEventListener("change", function (e) {
        const files = Array.from(e.target.files);
        if (files.length === 0) return;

        selectedFiles.push(...files);
        updateAttachmentsPreview();
    });

    function updateAttachmentsPreview() {
        if (!attachmentsPreview) return;

        attachmentsPreview.innerHTML = "";

        if (selectedFiles.length === 0) {
            attachmentsPreview.className = "attachments-preview";
            attachmentsPreview.style.display = "none";
            return;
        }

        attachmentsPreview.style.display = "flex";
        attachmentsPreview.className = "attachments-preview d-flex flex-wrap gap-2 mb-2 p-2 rounded";

        selectedFiles.forEach((file, index) => {
            const item = document.createElement("div");
            item.className = "attachment-item";

            if (file.type.startsWith("image/")) {
                const img = document.createElement("img");
                img.src = URL.createObjectURL(file);
                item.appendChild(img);
            } else {
                const icon = document.createElement("i");
                icon.className = "fas fa-file file-icon";
                item.appendChild(icon);
            }

            const name = document.createElement("span");
            name.textContent = file.name.length > 15 ? file.name.substring(0, 12) + "..." : file.name;
            item.appendChild(name);

            const removeBtn = document.createElement("div");
            removeBtn.className = "remove-attachment";
            removeBtn.title = "Удалить вложение";
            removeBtn.onclick = () => {
                selectedFiles.splice(index, 1);
                updateAttachmentsPreview();
                fileInput.value = "";
            };
            item.appendChild(removeBtn);

            attachmentsPreview.appendChild(item);
        });
    }

    messageInput.addEventListener("dragover", (e) => {
        e.preventDefault();
        e.dataTransfer.dropEffect = "copy";
        messageInput.classList.add("dragover");
    });

    messageInput.addEventListener("dragleave", (e) => {
        e.preventDefault();
        messageInput.classList.remove("dragover");
    });

    messageInput.addEventListener("drop", (e) => {
        e.preventDefault();
        messageInput.classList.remove("dragover");

        const files = Array.from(e.dataTransfer.files);
        if (files.length === 0) return;

        selectedFiles.push(...files);
        updateAttachmentsPreview();
    });

    messageInput.addEventListener("paste", (e) => {
        if (e.clipboardData.files.length > 0) {
            e.preventDefault();
            const files = Array.from(e.clipboardData.files);
            selectedFiles.push(...files);
            updateAttachmentsPreview();
        }
    });

    document.body.addEventListener("click", function (e) {
        if (e.target.closest("a.image-link")) {
            e.preventDefault();
            const link = e.target.closest("a.image-link");
            const modal = document.getElementById("imageModal");
            const modalImg = document.getElementById("modalImage");
            modalImg.src = link.href;
            modal.style.display = "block";
        }
    });

    const modalClose = document.getElementById("modalClose");
    modalClose.onclick = function () {
        document.getElementById("imageModal").style.display = "none";
        document.getElementById("modalImage").src = "";
    };

    const modal = document.getElementById("imageModal");
    modal.onclick = function (e) {
        if (e.target === modal) {
            modal.style.display = "none";
            document.getElementById("modalImage").src = "";
        }
    };

    async function sendMessage() {
        const chatId = parseInt(chatIdInput.value);
        const userName = userNameInput.value;
        const message = messageInput.value.trim();

        const filesToSend = [...selectedFiles];

        messageInput.value = "";
        selectedFiles = [];
        updateAttachmentsPreview();
        fileInput.value = "";
        autoResizeTextarea();

        let attachments = [];

        if (filesToSend.length > 0) {
            const formData = new FormData();
            filesToSend.forEach(f => formData.append("files", f));
            formData.append("chatId", chatId);

            try {
                const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
                formData.append("__RequestVerificationToken", token);

                const response = await fetch("/Chat/UploadAttachment", {
                    method: "POST",
                    body: formData
                });

                if (!response.ok) throw new Error();

                attachments = await response.json();
            } catch {
                alert("Не удалось отправить файл");
                return;
            }
        }

        if (message || attachments.length > 0) {
            try {
                await connection.invoke("SendMessage", chatId, userName, message, attachments);
            } catch {
                alert("Не удалось отправить сообщение");
            }
        }
    }

    window.sendMessage = sendMessage;

    sendButton.addEventListener("click", sendMessage);

    messageInput.addEventListener("keypress", function (e) {
        if (e.key === "Enter" && !e.shiftKey) {
            e.preventDefault();
            sendMessage();
        }
    });

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/chatHub")
        .build();

    connection.on("ReceiveMessage", function (senderId, message, sentAt, attachments) {
        const currentUserId = currentUserIdInput.value;
        const isMe = senderId === currentUserId;

        const messageElement = document.createElement("li");
        messageElement.className = `d-flex mb-2 ${isMe ? "justify-content-end" : "justify-content-start"}`;

        const bubble = document.createElement("div");
        bubble.className = `message-bubble ${isMe ? "me" : "other"}`;

        if (message) {
            bubble.innerHTML += message;
        }

        if (attachments && attachments.length > 0) {
            const images = attachments.filter(a => a.type?.startsWith("image/"));
            const videos = attachments.filter(a => a.type?.startsWith("video/"));
            const audios = attachments.filter(a => a.type?.startsWith("audio/"));
            const files = attachments.filter(a => !a.type?.startsWith("image/") && !a.type?.startsWith("video/") && !a.type?.startsWith("audio/"));

            if (images.length > 0) {
                const imgGrid = document.createElement("div");
                imgGrid.className = "image-grid";

                images.forEach(imgAtt => {
                    const img = document.createElement("img");
                    img.src = imgAtt.url;
                    img.className = "message-image";
                    img.loading = "lazy";
                    img.onclick = () => window.open(imgAtt.url, "_blank");
                    imgGrid.appendChild(img);
                });

                bubble.appendChild(imgGrid);
            }

            if (videos.length > 0) {
                const vidGrid = document.createElement("div");
                vidGrid.className = "image-grid";

                videos.forEach(vidAtt => {
                    const video = document.createElement("video");
                    video.src = vidAtt.url;
                    video.controls = true;
                    video.className = "message-image";
                    video.onclick = () => window.open(vidAtt.url, "_blank");
                    vidGrid.appendChild(video);
                });

                bubble.appendChild(vidGrid);
            }

            if (audios.length > 0) {
                const audioGrid = document.createElement("div");
                audioGrid.className = "audio-grid";

                audios.forEach(audioAtt => {
                    const audio = document.createElement("audio");
                    audio.src = audioAtt.url;
                    audio.controls = true;
                    audio.style.width = "100%";
                    audioGrid.appendChild(audio);
                });

                bubble.appendChild(audioGrid);
            }

            if (files.length > 0) {
                const filesGrid = document.createElement("div");
                filesGrid.className = "attachments-grid";

                files.forEach(att => {
                    const fileItem = document.createElement("a");
                    fileItem.href = att.url;
                    fileItem.className = "file-attachment";
                    fileItem.target = "_blank";

                    const icon = document.createElement("div");
                    icon.className = "file-icon-box";
                    const ext = att.name.split('.').pop()?.toUpperCase() || "";
                    const extSpan = document.createElement("span");
                    extSpan.textContent = ext;
                    icon.appendChild(extSpan);

                    const info = document.createElement("div");
                    info.className = "file-info";

                    const name = document.createElement("div");
                    name.className = "file-name";
                    name.textContent = att.name;
                    info.appendChild(name);

                    if (att.size) {
                        const size = document.createElement("div");
                        size.className = "file-size";
                        size.textContent = formatFileSize(att.size);
                        info.appendChild(size);
                    }

                    fileItem.appendChild(icon);
                    fileItem.appendChild(info);
                    filesGrid.appendChild(fileItem);
                });

                bubble.appendChild(filesGrid);
            }
        }

        function formatFileSize(bytes) {
            const sizes = ['Б', 'КБ', 'МБ', 'ГБ'];
            if (!bytes) return '';
            const i = Math.floor(Math.log(bytes) / Math.log(1024));
            return (bytes / Math.pow(1024, i)).toFixed(1) + ' ' + sizes[i];
        }

        const time = document.createElement("div");
        time.className = "message-time";
        time.textContent = new Date(sentAt).toLocaleString();
        bubble.appendChild(time);

        messageElement.appendChild(bubble);
        messagesList.appendChild(messageElement);
        scrollToBottom();
    });

    connection.start()
        .then(() => {
            const chatId = chatIdInput.value;
            return connection.invoke("JoinChat", chatId);
        });

    function scrollToBottom() {
        const container = messagesList.parentElement;
        if (container) {
            container.scrollTop = container.scrollHeight;
        }
    }

    scrollToBottom();
});
