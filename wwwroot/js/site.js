document.addEventListener("DOMContentLoaded", () => {
    // 1. Плавное появление страницы
    document.body.style.opacity = 0;
    setTimeout(() => {
        document.body.style.transition = "opacity 0.6s ease-in-out";
        document.body.style.opacity = 1;
    }, 50);

    // 2. Кнопка "наверх"
    const btn = document.createElement("button");
    btn.textContent = "⬆";
    btn.id = "toTopBtn";
    Object.assign(btn.style, {
        position: "fixed",
        bottom: "30px",
        right: "30px",
        backgroundColor: "var(--primary)",
        color: "white",
        border: "none",
        borderRadius: "50%",
        width: "48px",
        height: "48px",
        fontSize: "1.5rem",
        display: "none",
        zIndex: 1000,
        cursor: "pointer",
        boxShadow: "0 0 10px rgba(0,0,0,0.3)"
    });
    document.body.appendChild(btn);

    window.addEventListener("scroll", () => {
        btn.style.display = window.scrollY > 300 ? "block" : "none";
    });

    btn.addEventListener("click", () => {
        window.scrollTo({ top: 0, behavior: "smooth" });
    });

    // 3. Подсветка активного пункта меню
    const links = document.querySelectorAll(".navbar .nav-link");
    links.forEach(link => {
        if (link.href === window.location.href) {
            link.classList.add("active");
            link.style.color = "var(--primary-light)";
        }
    });

    // 4. Анимация кнопок при наведении
    const buttons = document.querySelectorAll(".btn, .btn-primary, .btn-outline");
    buttons.forEach(btn => {
        btn.addEventListener("mouseenter", () => {
            btn.style.transform = "scale(1.05)";
        });
        btn.addEventListener("mouseleave", () => {
            btn.style.transform = "scale(1)";
        });
        btn.addEventListener("mousedown", () => {
            btn.style.transform = "scale(0.95)";
        });
        btn.addEventListener("mouseup", () => {
            btn.style.transform = "scale(1.05)";
        });
    });

    // 5. Подсветка карточек при наведении
    const cards = document.querySelectorAll(".project-card");
    cards.forEach(card => {
        card.addEventListener("mouseenter", () => {
            card.style.transform = "translateY(-2px) scale(1.01)";
            card.style.boxShadow = "0 10px 30px -10px rgba(139, 92, 246, 0.5)";
        });
        card.addEventListener("mouseleave", () => {
            card.style.transform = "none";
            card.style.boxShadow = "";
        });
    });

    // 6. Появление карточек при скролле (Intersection Observer)
    const observer = new IntersectionObserver(entries => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.style.opacity = 1;
                entry.target.style.transform = "translateY(0)";
                observer.unobserve(entry.target);
            }
        });
    }, { threshold: 0.1 });

    document.querySelectorAll(".project-card, .filters-card, .stat-card").forEach(el => {
        el.style.opacity = 0;
        el.style.transform = "translateY(30px)";
        el.style.transition = "opacity 0.6s ease-out, transform 0.6s ease-out";
        observer.observe(el);
    });

    // 7. Волна при нажатии на ссылки навигации
    links.forEach(link => {
        link.addEventListener("click", (e) => {
            const wave = document.createElement("span");
            wave.classList.add("ripple");
            Object.assign(wave.style, {
                position: "absolute",
                background: "rgba(255,255,255,0.3)",
                borderRadius: "50%",
                transform: "scale(0)",
                animation: "ripple 0.6s linear",
                pointerEvents: "none",
                width: "100px",
                height: "100px",
                top: `${e.offsetY - 50}px`,
                left: `${e.offsetX - 50}px`,
                zIndex: 1000
            });
            link.style.position = "relative";
            link.appendChild(wave);
            setTimeout(() => wave.remove(), 600);
        });
    });

    // 8. Анимация иконок в .stat-icon
    document.querySelectorAll(".stat-icon").forEach(icon => {
        icon.style.transition = "transform 0.3s ease";
        icon.addEventListener("mouseenter", () => {
            icon.style.transform = "scale(1.2) rotate(5deg)";
        });
        icon.addEventListener("mouseleave", () => {
            icon.style.transform = "scale(1) rotate(0deg)";
        });
    });

    // 9. Наведение на пункты преимуществ
    document.querySelectorAll(".filters-card li").forEach(li => {
        li.style.transition = "transform 0.2s ease, background 0.2s ease";
        li.addEventListener("mouseenter", () => {
            li.style.transform = "translateX(5px)";
            li.style.background = "rgba(255,255,255,0.03)";
        });
        li.addEventListener("mouseleave", () => {
            li.style.transform = "none";
            li.style.background = "transparent";
        });
    });

    // 10. <noscript> уведомление
    const noScript = document.createElement("noscript");
    noScript.innerHTML = `<div style="background: #dc2626; color: white; padding: 1rem; text-align: center;">
        JavaScript отключён. Некоторые функции сайта не будут работать.
    </div>`;
    document.body.appendChild(noScript);

    // 1. Убираем начальное скрытие body (для предотвращения мерцания)
    document.body.style.opacity = 1;

    // 2. Прелоадер (анимация загрузки при переходах)
    const loader = document.createElement("div");
    loader.id = "pageLoader";
    Object.assign(loader.style, {
        position: "fixed",
        top: 0,
        left: 0,
        width: "100%",
        height: "100%",
        background: "rgba(15, 15, 35, 0.9)",
        display: "none",
        zIndex: 9999,
        alignItems: "center",
        justifyContent: "center",
        color: "white",
        fontSize: "1.5rem",
        fontWeight: "bold",
    });
    loader.innerHTML = "Загрузка...";
    document.body.appendChild(loader);

    // 11. Переключение темы с анимацией
    const currentTheme = localStorage.getItem("theme") || "dark";
    document.documentElement.setAttribute("data-theme", currentTheme);

    const themeBtn = document.createElement("button");
    themeBtn.id = "btnThemeToggle";
    themeBtn.title = "Переключить тему";
    themeBtn.innerHTML = currentTheme === "dark" ? "🌞" : "🌙";
    Object.assign(themeBtn.style, {
        position: "fixed",
        bottom: "90px", // чуть выше кнопки "⬆"
        right: "30px",
        backgroundColor: "var(--surface)",
        color: "var(--text-primary)",
        border: "1px solid var(--border)",
        borderRadius: "50%",
        width: "48px",
        height: "48px",
        fontSize: "1.2rem",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        zIndex: 1000,
        cursor: "pointer",
        boxShadow: "0 0 10px rgba(0,0,0,0.3)",
        transition: "all 0.3s ease, transform 0.3s ease"
    });
    document.body.appendChild(themeBtn);

    // Добавим fade-анимацию для иконки темы
    themeBtn.style.transition = "background 0.3s ease, color 0.3s ease, opacity 0.3s ease";

    themeBtn.addEventListener("click", () => {
        const html = document.documentElement;
        const oldTheme = html.getAttribute("data-theme");
        const newTheme = oldTheme === "dark" ? "light" : "dark";

        // Смена темы
        html.setAttribute("data-theme", newTheme);
        localStorage.setItem("theme", newTheme);

        // Плавная смена иконки
        themeBtn.style.opacity = 0;
        setTimeout(() => {
            themeBtn.innerHTML = newTheme === "dark" ? "🌞" : "🌙";
            themeBtn.style.opacity = 1;
        }, 200);
    });


});

