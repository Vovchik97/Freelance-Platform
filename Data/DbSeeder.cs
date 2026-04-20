using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Data;
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        // Create roles
        string[] rolesName = { "Client", "Freelancer", "Admin" };
        foreach (var roleName in rolesName)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    
        // Create admin
        var adminEmail = "admin@example.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new IdentityUser { UserName = adminEmail, Email = adminEmail };
            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
        
        string defaultPasswordClient = "Client123!";
        string defaultPasswordFreelancer = "Freelancer123!";
        string[] clientIds = { "client1", "client2", "client3", "client4", "client5" };
        string[] freelancerIds = { "freelancer1", "freelancer2", "freelancer3", "freelancer4", "freelancer5" };
        
        // Добавление пользователей
        foreach (var id in clientIds)
        {
            if (await userManager.FindByIdAsync(id) == null)
            {
                var user = new IdentityUser
                {
                    Id = id,
                    UserName = id,
                    Email = $"{id}@example.com",
                    EmailConfirmed = false
                };
                await userManager.CreateAsync(user, defaultPasswordClient);
                await userManager.AddToRoleAsync(user, "Client");
            }
        }

        foreach (var id in freelancerIds)
        {
            if (await userManager.FindByIdAsync(id) == null)
            {
                var user = new IdentityUser
                {
                    Id = id,
                    UserName = id,
                    Email = $"{id}@example.com",
                    EmailConfirmed = false
                };
                await userManager.CreateAsync(user, defaultPasswordFreelancer);
                await userManager.AddToRoleAsync(user, "Freelancer");
            }
        }

        // Проекты
        if (!await context.Projects.AnyAsync())
        {
            var projects = new[]
            {
                new Project { Title = "Сайт-визитка", Description = "Создание сайта-визитки для бизнеса", Budget = 1000, Status = ProjectStatus.Open, ClientId = "client1", CreatedAt = DateTime.UtcNow },
                new Project { Title = "Интернет-магазин", Description = "Магазин с оплатой и корзиной", Budget = 2000, Status = ProjectStatus.Open, ClientId = "client2", CreatedAt = DateTime.UtcNow },
                new Project { Title = "Мобильное приложение", Description = "Приложение для Android", Budget = 3000, Status = ProjectStatus.Open, ClientId = "client3", CreatedAt = DateTime.UtcNow },
                new Project { Title = "Telegram-бот", Description = "Бот для ответов на вопросы", Budget = 800, Status = ProjectStatus.Open, ClientId = "client4", CreatedAt = DateTime.UtcNow },
                new Project { Title = "Система управления задачами", Description = "Веб-приложение для задач", Budget = 1500, Status = ProjectStatus.Open, ClientId = "client5", CreatedAt = DateTime.UtcNow },
                new Project { Title = "CRM-система", Description = "CRM для малого бизнеса", Budget = 2500, Status = ProjectStatus.Open, ClientId = "client1", CreatedAt = DateTime.UtcNow },
                new Project { Title = "Аналитическая панель", Description = "Dashboard для мониторинга", Budget = 1200, Status = ProjectStatus.Open, ClientId = "client2", CreatedAt = DateTime.UtcNow },
                new Project { Title = "Парсер сайтов", Description = "Сбор данных с сайтов", Budget = 900, Status = ProjectStatus.Open, ClientId = "client3", CreatedAt = DateTime.UtcNow },
                new Project { Title = "Блог-платформа", Description = "Сайт для публикаций", Budget = 1300, Status = ProjectStatus.Open, ClientId = "client4", CreatedAt = DateTime.UtcNow },
                new Project { Title = "Игра на Unity", Description = "Прототип 2D игры", Budget = 1800, Status = ProjectStatus.Open, ClientId = "client5", CreatedAt = DateTime.UtcNow },
            };

            await context.Projects.AddRangeAsync(projects);
            await context.SaveChangesAsync();
        }

        // Заявки
        if (!await context.Bids.AnyAsync())
        {
            var bids = new[]
            {
                new Bid { Amount = 900, Comment = "Готов взяться, есть опыт.", DurationInDays = 10, CreatedAt = DateTime.UtcNow, FreelancerId = "freelancer1", ProjectId = 1 },
                new Bid { Amount = 950, Comment = "Быстро и качественно", DurationInDays = 8, CreatedAt = DateTime.UtcNow, FreelancerId = "freelancer2", ProjectId = 1 },
                new Bid { Amount = 1900, Comment = "Опыт в e-commerce", DurationInDays = 20, CreatedAt = DateTime.UtcNow, FreelancerId = "freelancer3", ProjectId = 2 },
                new Bid { Amount = 1700, Comment = "Готов сделать дешевле", DurationInDays = 18, CreatedAt = DateTime.UtcNow, FreelancerId = "freelancer4", ProjectId = 2 },
                new Bid { Amount = 2900, Comment = "Хороший опыт в Android", DurationInDays = 30, CreatedAt = DateTime.UtcNow, FreelancerId = "freelancer5", ProjectId = 3 },
                new Bid { Amount = 850, Comment = "Есть похожие проекты", DurationInDays = 7, CreatedAt = DateTime.UtcNow, FreelancerId = "freelancer1", ProjectId = 4 },
                new Bid { Amount = 1450, Comment = "Работал с задачами", DurationInDays = 14, CreatedAt = DateTime.UtcNow, FreelancerId = "freelancer2", ProjectId = 5 },
                new Bid { Amount = 2400, Comment = "CRM на Laravel", DurationInDays = 15, CreatedAt = DateTime.UtcNow, FreelancerId = "freelancer3", ProjectId = 6 },
                new Bid { Amount = 1000, Comment = "React + Node.js", DurationInDays = 10, CreatedAt = DateTime.UtcNow, FreelancerId = "freelancer4", ProjectId = 7 },
                new Bid { Amount = 870, Comment = "Сделаю быстро", DurationInDays = 5, CreatedAt = DateTime.UtcNow, FreelancerId = "freelancer5", ProjectId = 8 },
                new Bid { Amount = 1250, Comment = "Пример есть в портфолио", DurationInDays = 12, CreatedAt = DateTime.UtcNow, FreelancerId = "freelancer1", ProjectId = 9 },
                new Bid { Amount = 1600, Comment = "Хорошо разбираюсь в Unity", DurationInDays = 25, CreatedAt = DateTime.UtcNow, FreelancerId = "freelancer2", ProjectId = 10 },
                new Bid { Amount = 950, Comment = "Есть опыт парсинга", DurationInDays = 9, CreatedAt = DateTime.UtcNow, FreelancerId = "freelancer3", ProjectId = 8 },
                new Bid { Amount = 880, Comment = "Сделаю качественно", DurationInDays = 6, CreatedAt = DateTime.UtcNow, FreelancerId = "freelancer4", ProjectId = 4 },
                new Bid { Amount = 2700, Comment = "Большой опыт в CRM", DurationInDays = 20, CreatedAt = DateTime.UtcNow, FreelancerId = "freelancer5", ProjectId = 6 },
                new Bid { Amount = 1100, Comment = "Удобный дизайн включён", DurationInDays = 10, CreatedAt = DateTime.UtcNow, FreelancerId = "freelancer1", ProjectId = 7 },
                new Bid { Amount = 1320, Comment = "Работаю с блогами", DurationInDays = 14, CreatedAt = DateTime.UtcNow, FreelancerId = "freelancer2", ProjectId = 9 },
                new Bid { Amount = 1800, Comment = "2D игры — мой профиль", DurationInDays = 18, CreatedAt = DateTime.UtcNow, FreelancerId = "freelancer3", ProjectId = 10 },
                new Bid { Amount = 1300, Comment = "React Native app", DurationInDays = 16, CreatedAt = DateTime.UtcNow, FreelancerId = "freelancer4", ProjectId = 3 },
                new Bid { Amount = 1400, Comment = "Все будет в срок", DurationInDays = 13, CreatedAt = DateTime.UtcNow, FreelancerId = "freelancer5", ProjectId = 5 },
            };

            await context.Bids.AddRangeAsync(bids);
            await context.SaveChangesAsync();
        }
        
        // Услуги
        if (!await context.Services.AnyAsync())
        {
            var services = new[]
            {
                new Service { Title = "Создание лендинга", Description = "Сделаю современный одностраничник", Price = 1000, FreelancerId = "freelancer1", Status = ServiceStatus.Available, CreatedAt = DateTime.UtcNow},
                new Service { Title = "UX/UI дизайн", Description = "Прототипы и дизайн для веба", Price = 1200, FreelancerId = "freelancer2", Status = ServiceStatus.Available, CreatedAt = DateTime.UtcNow },
                new Service { Title = "Frontend разработка", Description = "Верстаю React / Vue интерфейсы", Price = 1500, FreelancerId = "freelancer3", Status = ServiceStatus.Available, CreatedAt = DateTime.UtcNow },
                new Service { Title = "Backend API", Description = "Разработка REST API на .NET", Price = 2000, FreelancerId = "freelancer4", Status = ServiceStatus.Available, CreatedAt = DateTime.UtcNow },
                new Service { Title = "Мобильная разработка", Description = "Android и iOS приложения", Price = 2500, FreelancerId = "freelancer5", Status = ServiceStatus.Available, CreatedAt = DateTime.UtcNow },
                new Service { Title = "SEO аудит", Description = "Анализ и рекомендации по SEO", Price = 800, FreelancerId = "freelancer1", Status = ServiceStatus.Available, CreatedAt = DateTime.UtcNow },
                new Service { Title = "Копирайтинг", Description = "Продающие тексты и статьи", Price = 700, FreelancerId = "freelancer2", Status = ServiceStatus.Available, CreatedAt = DateTime.UtcNow },
                new Service { Title = "Создание Telegram-бота", Description = "Бот под ваш сценарий", Price = 1100, FreelancerId = "freelancer3", Status = ServiceStatus.Available, CreatedAt = DateTime.UtcNow },
                new Service { Title = "Обработка данных", Description = "Python-скрипты, парсинг", Price = 1300, FreelancerId = "freelancer4", Status = ServiceStatus.Available, CreatedAt = DateTime.UtcNow },
                new Service { Title = "Анимация и видео", Description = "Моушн-дизайн и ролики", Price = 1600, FreelancerId = "freelancer5", Status = ServiceStatus.Available, CreatedAt = DateTime.UtcNow },
            };

            await context.Services.AddRangeAsync(services);
            await context.SaveChangesAsync();
        }
        
        // Заказы
        if (!await context.Orders.AnyAsync())
        {
            var orders = new[]
            {
                new Order { ClientId = "client1", ServiceId = 1, Comment = "Нужен лендинг для продукта", DurationInDays = 5, CreatedAt = DateTime.UtcNow, Status = OrderStatus.Pending },
                new Order { ClientId = "client2", ServiceId = 2, Comment = "Дизайн для интернет-магазина", DurationInDays = 7, CreatedAt = DateTime.UtcNow, Status = OrderStatus.Pending },
                new Order { ClientId = "client3", ServiceId = 3, Comment = "Сайт на React", DurationInDays = 7, CreatedAt = DateTime.UtcNow, Status = OrderStatus.Pending },
                new Order { ClientId = "client4", ServiceId = 4, Comment = "API для авторизации", DurationInDays = 10, CreatedAt = DateTime.UtcNow, Status = OrderStatus.Pending },
                new Order { ClientId = "client5", ServiceId = 5, Comment = "Приложение под Android", DurationInDays = 15, CreatedAt = DateTime.UtcNow, Status = OrderStatus.Pending },
                new Order { ClientId = "client1", ServiceId = 6, Comment = "SEO аудит сайта", DurationInDays = 9, CreatedAt = DateTime.UtcNow, Status = OrderStatus.Pending },
                new Order { ClientId = "client2", ServiceId = 7, Comment = "Текст для блога", DurationInDays = 8, CreatedAt = DateTime.UtcNow, Status = OrderStatus.Pending },
                new Order { ClientId = "client3", ServiceId = 8, Comment = "Бот для Telegram", DurationInDays = 5, CreatedAt = DateTime.UtcNow, Status = OrderStatus.Pending },
                new Order { ClientId = "client4", ServiceId = 9, Comment = "Скрипт для обработки таблиц", DurationInDays = 6, CreatedAt = DateTime.UtcNow, Status = OrderStatus.Pending },
                new Order { ClientId = "client5", ServiceId = 10, Comment = "Видео-презентация", DurationInDays = 2, CreatedAt = DateTime.UtcNow, Status = OrderStatus.Pending },

                new Order { ClientId = "client1", ServiceId = 2, Comment = "Дизайн для лэндинга", DurationInDays = 5, CreatedAt = DateTime.UtcNow, Status = OrderStatus.Pending },
                new Order { ClientId = "client2", ServiceId = 3, Comment = "Frontend для CRM", DurationInDays = 7, CreatedAt = DateTime.UtcNow, Status = OrderStatus.Pending },
                new Order { ClientId = "client3", ServiceId = 4, Comment = "API на .NET 8", DurationInDays = 7, CreatedAt = DateTime.UtcNow, Status = OrderStatus.Pending },
                new Order { ClientId = "client4", ServiceId = 5, Comment = "Приложение для мероприятий", DurationInDays = 15, CreatedAt = DateTime.UtcNow, Status = OrderStatus.Pending },
                new Order { ClientId = "client5", ServiceId = 1, Comment = "Лендинг для продукта", DurationInDays = 6, CreatedAt = DateTime.UtcNow, Status = OrderStatus.Pending },
                new Order { ClientId = "client1", ServiceId = 7, Comment = "Описание для страницы", DurationInDays = 2, CreatedAt = DateTime.UtcNow, Status = OrderStatus.Pending },
                new Order { ClientId = "client2", ServiceId = 8, Comment = "Бот для автоответов", DurationInDays = 7, CreatedAt = DateTime.UtcNow, Status = OrderStatus.Pending },
                new Order { ClientId = "client3", ServiceId = 9, Comment = "Парсинг excel", DurationInDays = 3, CreatedAt = DateTime.UtcNow, Status = OrderStatus.Pending },
                new Order { ClientId = "client4", ServiceId = 10, Comment = "Видео-анимация логотипа", DurationInDays = 6, CreatedAt = DateTime.UtcNow, Status = OrderStatus.Pending },
                new Order { ClientId = "client5", ServiceId = 6, Comment = "Оптимизация под SEO", DurationInDays = 5, CreatedAt = DateTime.UtcNow, Status = OrderStatus.Pending },
            };

            await context.Orders.AddRangeAsync(orders);
            await context.SaveChangesAsync();
        }

        if (!await context.Categories.AnyAsync())
        {
            var categories = new[]
            {
                new Category { Name = "Веб-разработка", Description = "Создание сайтов и веб-приложений", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Category { Name = "Мобильная разработка", Description = "Разработка мобильных приложений", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Category { Name = "Дизайн", Description = "Графический и UI/UX дизайн", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Category { Name = "Маркетинг", Description = "Интернет-маркетинг и SMM", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Category { Name = "Копирайтинг", Description = "Написание текстов и контента", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Category { Name = "SEO", Description = "Поисковая оптимизация", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Category { Name = "Тестирование", Description = "QA и тестирование ПО", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Category { Name = "Администрирование", Description = "Системное администрирование и DevOps", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Category { Name = "Аналитика", Description = "Бизнес-аналитика и Data Science", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Category { Name = "Другое", Description = "Прочие услуги и проекты", IsActive = true, CreatedAt = DateTime.UtcNow }
            };
            
            await context.Categories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
        }

        if (!await context.TaskTemplates.AnyAsync())
        {
            var catWeb = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Веб-разработка");
            var catMobile = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Мобильная разработка");
            var catDesign = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Дизайн");
            var catCopy = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Копирайтинг");
            var catSeo = await context.Categories.FirstOrDefaultAsync(c => c.Name == "SEO");
            var catMarketing = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Маркетинг");
            var catTest = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Тестирование");
            var catAdmin = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Администрирование");
            var catAnalytics = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Аналитика");

            var templates = new List<TaskTemplate>
            {
                // ==================== ВЕБ-РАЗРАБОТКА (5) ====================
                new TaskTemplate
                {
                    Name = "Лендинг",
                    Description = "Быстрый и простой одностраничный сайт",
                    Categories = new List<Category>(new[] { catWeb }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Дизайн макета", Description = "Создать макет в Figma или Adobe XD с учетом брендинга и UX принципов", OrderIndex = 1 },
                        new() { Title = "Верстка HTML/CSS", Description = "Сверстать адаптивную страницу под различные устройства и браузеры", OrderIndex = 2 },
                        new() { Title = "Подключение форм", Description = "Настроить формы обратной связи, подписки и валидацию данных", OrderIndex = 3 },
                        new() { Title = "Тестирование", Description = "Протестировать работу на разных браузерах, устройствах и скоростях интернета", OrderIndex = 4 },
                        new() { Title = "Развёртывание", Description = "Залить сайт на хостинг, настроить домен и SSL-сертификат", OrderIndex = 5 }
                    }
                },
                new TaskTemplate
                {
                    Name = "Корпоративный сайт",
                    Description = "Многостраничный сайт для компании",
                    Categories = new List<Category>(new[] { catWeb }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Анализ требований", Description = "Провести интервью с заказчиком, определить цели и задачи сайта", OrderIndex = 1 },
                        new() { Title = "Дизайн системы", Description = "Создать дизайн-систему, определить стиль и компоненты", OrderIndex = 2 },
                        new() { Title = "Верстка всех страниц", Description = "Сверстать все страницы сайта согласно макетам", OrderIndex = 3 },
                        new() { Title = "CMS интеграция", Description = "Интегрировать систему управления контентом для удобного редактирования", OrderIndex = 4 },
                        new() { Title = "SEO оптимизация", Description = "Оптимизировать сайт для поисковых систем (мета-теги, структура, скорость)", OrderIndex = 5 },
                        new() { Title = "Мобильная адаптация", Description = "Адаптировать сайт под мобильные устройства и планшеты", OrderIndex = 6 },
                        new() { Title = "Тестирование", Description = "Комплексное тестирование функционала и кроссбраузерности", OrderIndex = 7 },
                        new() { Title = "Развёртывание", Description = "Развернуть сайт на производственном сервере и настроить домен", OrderIndex = 8 }
                    }
                },
                new TaskTemplate
                {
                    Name = "Интернет-магазин",
                    Description = "Полнофункциональный e-commerce проект",
                    Categories = new List<Category>(new[] { catWeb }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Анализ требований", Description = "Определить функционал магазина, способы оплаты и доставки", OrderIndex = 1 },
                        new() { Title = "Дизайн интерфейса", Description = "Создать дизайн каталога, карточек товаров и процесса покупки", OrderIndex = 2 },
                        new() { Title = "Верстка фронтенда", Description = "Сверстать все страницы магазина с адаптивным дизайном", OrderIndex = 3 },
                        new() { Title = "Backend разработка", Description = "Разработать серверную часть: каталог, корзина, заказы, пользователи", OrderIndex = 4 },
                        new() { Title = "Интеграция платежей", Description = "Подключить платежные системы (Stripe, PayPal, банковские карты)", OrderIndex = 5 },
                        new() { Title = "Интеграция доставки", Description = "Интегрировать сервисы доставки и расчет стоимости", OrderIndex = 6 },
                        new() { Title = "Admin панель", Description = "Создать панель администратора для управления товарами и заказами", OrderIndex = 7 },
                        new() { Title = "Система уведомлений", Description = "Настроить email-уведомления о заказах и SMS-информирование", OrderIndex = 8 },
                        new() { Title = "Тестирование", Description = "Протестировать весь процесс покупки и работу платежей", OrderIndex = 9 },
                        new() { Title = "Развёртывание", Description = "Развернуть магазин на сервере и настроить безопасность", OrderIndex = 10 }
                    }
                },
                new TaskTemplate
                {
                    Name = "SPA приложение",
                    Description = "Single Page Application на React/Vue",
                    Categories = new List<Category>(new[] { catWeb }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Архитектура приложения", Description = "Спроектировать структуру приложения и выбрать технологический стек", OrderIndex = 1 },
                        new() { Title = "Дизайн компонентов", Description = "Создать дизайн UI компонентов и интерактивных элементов", OrderIndex = 2 },
                        new() { Title = "Разработка фронтенда", Description = "Разработать пользовательский интерфейс на React или Vue.js", OrderIndex = 3 },
                        new() { Title = "API разработка", Description = "Создать RESTful API для взаимодействия фронтенда с сервером", OrderIndex = 4 },
                        new() { Title = "Аутентификация", Description = "Реализовать систему авторизации и аутентификации пользователей", OrderIndex = 5 },
                        new() { Title = "State management", Description = "Настроить управление состоянием приложения (Redux, Vuex)", OrderIndex = 6 },
                        new() { Title = "Unit тестирование", Description = "Написать unit-тесты для компонентов и функций", OrderIndex = 7 },
                        new() { Title = "E2E тестирование", Description = "Создать end-to-end тесты для пользовательских сценариев", OrderIndex = 8 },
                        new() { Title = "Оптимизация", Description = "Оптимизировать производительность и размер bundle'а", OrderIndex = 9 },
                        new() { Title = "Развёртывание", Description = "Настроить CI/CD и развернуть приложение на сервере", OrderIndex = 10 }
                    }
                },
                new TaskTemplate
                {
                    Name = "REST API",
                    Description = "Разработка backend API без фронтенда",
                    Categories = new List<Category>(new[] { catWeb }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Дизайн API", Description = "Спроектировать структуру API endpoints и формат данных", OrderIndex = 1 },
                        new() { Title = "Разработка endpoints", Description = "Создать все необходимые REST endpoints с правильными HTTP методами", OrderIndex = 2 },
                        new() { Title = "Работа с БД", Description = "Настроить подключение к базе данных и создать модели данных", OrderIndex = 3 },
                        new() { Title = "Аутентификация", Description = "Реализовать систему аутентификации (JWT токены, OAuth)", OrderIndex = 4 },
                        new() { Title = "Авторизация", Description = "Настроить права доступа и роли пользователей", OrderIndex = 5 },
                        new() { Title = "Валидация данных", Description = "Добавить валидацию входящих данных и обработку ошибок", OrderIndex = 6 },
                        new() { Title = "Кэширование", Description = "Настроить кэширование для повышения производительности", OrderIndex = 7 },
                        new() { Title = "Документация API", Description = "Создать документацию API с примерами использования (Swagger/OpenAPI)", OrderIndex = 8 },
                        new() { Title = "Unit тестирование", Description = "Написать unit и integration тесты для всех endpoints", OrderIndex = 9 },
                        new() { Title = "Развёртывание", Description = "Развернуть API на сервере и настроить мониторинг", OrderIndex = 10 }
                    }
                },

                // ==================== МОБИЛЬНАЯ РАЗРАБОТКА (5) ====================
                new TaskTemplate
                {
                    Name = "Simple Mobile App",
                    Description = "Простое приложение с основным функционалом",
                    Categories = new List<Category>(new[] { catMobile }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Дизайн UI/UX", Description = "Создать пользовательский интерфейс с учетом гайдлайнов платформы", OrderIndex = 1 },
                        new() { Title = "Разработка интерфейсов", Description = "Создать все экраны приложения и навигацию между ними", OrderIndex = 2 },
                        new() { Title = "Бизнес-логика", Description = "Реализовать основную функциональность и обработку данных", OrderIndex = 3 },
                        new() { Title = "Локальное хранилище", Description = "Настроить сохранение данных на устройстве пользователя", OrderIndex = 4 },
                        new() { Title = "Тестирование", Description = "Протестировать приложение на разных устройствах и версиях ОС", OrderIndex = 5 },
                        new() { Title = "Публикация", Description = "Подготовить и опубликовать приложение в App Store/Google Play", OrderIndex = 6 }
                    }
                },
                new TaskTemplate
                {
                    Name = "Cross-Platform App",
                    Description = "Приложение для iOS и Android",
                    Categories = new List<Category>(new[] { catMobile }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Дизайн UI/UX", Description = "Создать адаптивный дизайн для iOS и Android платформ", OrderIndex = 1 },
                        new() { Title = "Setup проекта", Description = "Настроить проект React Native/Flutter с необходимыми зависимостями", OrderIndex = 2 },
                        new() { Title = "Разработка компонентов", Description = "Создать переиспользуемые компоненты для обеих платформ", OrderIndex = 3 },
                        new() { Title = "API интеграция", Description = "Интегрировать приложение с backend API для обмена данными", OrderIndex = 4 },
                        new() { Title = "Локальная БД", Description = "Настроить локальную базу данных для оффлайн работы", OrderIndex = 5 },
                        new() { Title = "Native модули", Description = "Добавить платформо-специфичные функции (камера, GPS, уведомления)", OrderIndex = 6 },
                        new() { Title = "Тестирование iOS", Description = "Протестировать приложение на различных устройствах iOS", OrderIndex = 7 },
                        new() { Title = "Тестирование Android", Description = "Протестировать приложение на различных устройствах Android", OrderIndex = 8 },
                        new() { Title = "Публикация в App Store", Description = "Подготовить и опубликовать приложение в Apple App Store", OrderIndex = 9 },
                        new() { Title = "Публикация в Google Play", Description = "Подготовить и опубликовать приложение в Google Play Store", OrderIndex = 10 }
                    }
                },
                new TaskTemplate
                {
                    Name = "Advanced Mobile App",
                    Description = "Сложное приложение с расширенным функционалом",
                    Categories = new List<Category>(new[] { catMobile }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Архитектура приложения", Description = "Спроектировать масштабируемую архитектуру с учетом сложности", OrderIndex = 1 },
                        new() { Title = "Дизайн системы", Description = "Создать дизайн-систему и comprehensive UI kit", OrderIndex = 2 },
                        new() { Title = "Разработка фронтенда", Description = "Создать сложные пользовательские интерфейсы и анимации", OrderIndex = 3 },
                        new() { Title = "Backend интеграция", Description = "Интегрировать с multiple API endpoints и микросервисами", OrderIndex = 4 },
                        new() { Title = "WebSocket/Real-time", Description = "Реализовать real-time функциональность (чаты, уведомления)", OrderIndex = 5 },
                        new() { Title = "Оффлайн синхронизация", Description = "Настроить синхронизацию данных между устройством и сервером", OrderIndex = 6 },
                        new() { Title = "Геолокация", Description = "Интегрировать GPS функциональность и карты", OrderIndex = 7 },
                        new() { Title = "Push уведомления", Description = "Настроить систему push-уведомлений для обеих платформ", OrderIndex = 8 },
                        new() { Title = "Аналитика", Description = "Интегрировать системы аналитики и отслеживания поведения", OrderIndex = 9 },
                        new() { Title = "Юнит тесты", Description = "Написать comprehensive unit тесты для всех компонентов", OrderIndex = 10 },
                        new() { Title = "UI/UX тесты", Description = "Создать автоматизированные UI тесты и usability тестирование", OrderIndex = 11 },
                        new() { Title = "Оптимизация", Description = "Оптимизировать производительность, память и время загрузки", OrderIndex = 12 },
                        new() { Title = "Публикация", Description = "Развернуть приложение в production с настройкой CI/CD", OrderIndex = 13 }
                    }
                },
                new TaskTemplate
                {
                    Name = "Mobile Game",
                    Description = "Разработка мобильной игры",
                    Categories = new List<Category>(new[] { catMobile }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Game Design Document", Description = "Создать подробный документ с описанием геймплея и механик", OrderIndex = 1 },
                        new() { Title = "Концепт-арт", Description = "Разработать визуальный стиль игры и концепт-арт персонажей", OrderIndex = 2 },
                        new() { Title = "Разработка ядра игры", Description = "Создать основные игровые механики и game loop", OrderIndex = 3 },
                        new() { Title = "Graphics & Animation", Description = "Создать графические ассеты, анимации и визуальные эффекты", OrderIndex = 4 },
                        new() { Title = "Sound & Music", Description = "Добавить звуковые эффекты, фоновую музыку и аудио", OrderIndex = 5 },
                        new() { Title = "UI/UX интерфейс", Description = "Создать игровой интерфейс, меню и HUD элементы", OrderIndex = 6 },
                        new() { Title = "Gameplay mechanics", Description = "Реализовать все игровые механики, уровни и прогрессию", OrderIndex = 7 },
                        new() { Title = "Оптимизация", Description = "Оптимизировать производительность для мобильных устройств", OrderIndex = 8 },
                        new() { Title = "Beta тестирование", Description = "Провести beta-тестирование с реальными игроками", OrderIndex = 9 },
                        new() { Title = "Публикация", Description = "Опубликовать игру в App Store и Google Play", OrderIndex = 10 }
                    }
                },
                new TaskTemplate
                {
                    Name = "iOS App",
                    Description = "Native приложение для iOS",
                    Categories = new List<Category>(new[] { catMobile }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Дизайн интерфейса", Description = "Создать дизайн согласно Apple Human Interface Guidelines", OrderIndex = 1 },
                        new() { Title = "Swift разработка", Description = "Разработать приложение на Swift с использованием UIKit/SwiftUI", OrderIndex = 2 },
                        new() { Title = "Core Data", Description = "Настроить Core Data для локального хранения данных", OrderIndex = 3 },
                        new() { Title = "Network запросы", Description = "Реализовать сетевые запросы с обработкой ошибок", OrderIndex = 4 },
                        new() { Title = "UserDefaults", Description = "Настроить сохранение пользовательских настроек", OrderIndex = 5 },
                        new() { Title = "Notifications", Description = "Интегрировать локальные и push уведомления", OrderIndex = 6 },
                        new() { Title = "Unit тесты", Description = "Написать unit тесты с XCTest framework", OrderIndex = 7 },
                        new() { Title = "UI тесты", Description = "Создать UI тесты для автоматизации тестирования интерфейса", OrderIndex = 8 },
                        new() { Title = "Code review", Description = "Провести ревью кода и рефакторинг", OrderIndex = 9 },
                        new() { Title = "Отправка в App Store", Description = "Подготовить и отправить приложение на ревью в App Store", OrderIndex = 10 }
                    }
                },

                // ==================== ДИЗАЙН (5) ====================
                new TaskTemplate
                {
                    Name = "Логотип",
                    Description = "Разработка логотипа и фирменного стиля",
                    Categories = new List<Category>(new[] { catDesign }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Исследование конкурентов", Description = "Проанализировать конкурентов и тренды в индустрии", OrderIndex = 1 },
                        new() { Title = "Концепция и эскизы", Description = "Создать первичные концепции и эскизы логотипа", OrderIndex = 2 },
                        new() { Title = "Первые варианты", Description = "Разработать несколько детализированных вариантов логотипа", OrderIndex = 3 },
                        new() { Title = "Доработка по feedback", Description = "Внести изменения согласно отзывам заказчика", OrderIndex = 4 },
                        new() { Title = "Финальный вариант", Description = "Создать финальную версию логотипа в векторном формате", OrderIndex = 5 },
                        new() { Title = "Подготовка файлов", Description = "Подготовить логотип в различных форматах и размерах", OrderIndex = 6 }
                    }
                },
                new TaskTemplate
                {
                    Name = "UI/UX Design",
                    Description = "Дизайн интерфейса приложения или сайта",
                    Categories = new List<Category>(new[] { catDesign }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Анализ требований", Description = "Провести интервью с заказчиком и проанализировать потребности", OrderIndex = 1 },
                        new() { Title = "User Research", Description = "Исследовать целевую аудиторию и создать user personas", OrderIndex = 2 },
                        new() { Title = "Wireframes", Description = "Создать low-fidelity wireframes для структуры интерфейса", OrderIndex = 3 },
                        new() { Title = "High-Fidelity Mockups", Description = "Разработать детализированные mockups с финальным дизайном", OrderIndex = 4 },
                        new() { Title = "Design System", Description = "Создать дизайн-систему с компонентами и стилями", OrderIndex = 5 },
                        new() { Title = "Прототипирование", Description = "Создать интерактивный прототип для демонстрации UX flow", OrderIndex = 6 },
                        new() { Title = "Usability тестирование", Description = "Протестировать прототип с реальными пользователями", OrderIndex = 7 },
                        new() { Title = "Итерация дизайна", Description = "Внести улучшения на основе результатов тестирования", OrderIndex = 8 },
                        new() { Title = "Подготовка для разработки", Description = "Подготовить все ассеты и спецификации для разработчиков", OrderIndex = 9 }
                    }
                },
                new TaskTemplate
                {
                    Name = "Брендирование",
                    Description = "Полный брендинг компании",
                    Categories = new List<Category>(new[] { catDesign }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Brand Strategy", Description = "Разработать стратегию бренда и позиционирование", OrderIndex = 1 },
                        new() { Title = "Logo разработка", Description = "Создать логотип и различные его вариации", OrderIndex = 2 },
                        new() { Title = "Color Palette", Description = "Разработать фирменную цветовую палитру", OrderIndex = 3 },
                        new() { Title = "Typography", Description = "Выбрать и настроить фирменную типографику", OrderIndex = 4 },
                        new() { Title = "Brand Guidelines", Description = "Создать brand book с правилами использования", OrderIndex = 5 },
                        new() { Title = "Визуальные элементы", Description = "Разработать дополнительные графические элементы", OrderIndex = 6 },
                        new() { Title = "Фирменный стиль", Description = "Применить брендинг к различным носителям", OrderIndex = 7 },
                        new() { Title = "Mock-ups применения", Description = "Создать mockups применения бренда в реальности", OrderIndex = 8 }
                    }
                },
                new TaskTemplate
                {
                    Name = "Веб-дизайн",
                    Description = "Дизайн веб-сайта",
                    Categories = new List<Category>(new[] { catDesign }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Анализ конкурентов", Description = "Изучить дизайн конкурирующих сайтов и тренды", OrderIndex = 1 },
                        new() { Title = "Карта сайта (sitemap)", Description = "Создать структуру сайта и навигацию", OrderIndex = 2 },
                        new() { Title = "Wireframes", Description = "Разработать wireframes для всех страниц", OrderIndex = 3 },
                        new() { Title = "Дизайн главной страницы", Description = "Создать детализированный дизайн главной страницы", OrderIndex = 4 },
                        new() { Title = "Дизайн внутренних страниц", Description = "Разработать дизайн всех внутренних страниц", OrderIndex = 5 },
                        new() { Title = "Мобильная версия", Description = "Адаптировать дизайн под мобильные устройства", OrderIndex = 6 },
                        new() { Title = "Интерактивные элементы", Description = "Создать дизайн hover-эффектов и анимаций", OrderIndex = 7 },
                        new() { Title = "Feedback итерация", Description = "Внести изменения на основе отзывов заказчика", OrderIndex = 8 },
                        new() { Title = "Финальный макет", Description = "Подготовить финальные файлы для верстки", OrderIndex = 9 }
                    }
                },
                new TaskTemplate
                {
                    Name = "Графика и иллюстрации",
                    Description = "Создание графики и иллюстраций",
                    Categories = new List<Category>(new[] { catDesign }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Концепция", Description = "Разработать концепцию и стиль иллюстраций", OrderIndex = 1 },
                        new() { Title = "Эскизы", Description = "Создать первичные эскизы и наброски", OrderIndex = 2 },
                        new() { Title = "Линейные работы", Description = "Создать чистые линейные рисунки", OrderIndex = 3 },
                        new() { Title = "Цветовая обработка", Description = "Добавить цвет и освещение к иллюстрациям", OrderIndex = 4 },
                        new() { Title = "Детализация", Description = "Добавить детали и финальную обработку", OrderIndex = 5 },
                        new() { Title = "Финальная обработка", Description = "Провести финальную обработку и коррекцию", OrderIndex = 6 },
                        new() { Title = "Доработка по замечаниям", Description = "Внести правки согласно feedback заказчика", OrderIndex = 7 }
                    }
                },

                // ==================== МАРКЕТИНГ (4) ====================
                new TaskTemplate
                {
                    Name = "SMM кампания",
                    Description = "Продвижение в социальных сетях",
                    Categories = new List<Category>(new[] { catMarketing }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Стратегия", Description = "Разработать стратегию продвижения в социальных сетях", OrderIndex = 1 },
                        new() { Title = "Контент-план", Description = "Создать календарь публикаций и темы контента", OrderIndex = 2 },
                        new() { Title = "Создание контента", Description = "Написать тексты и подготовить материалы для публикаций", OrderIndex = 3 },
                        new() { Title = "Дизайн постов", Description = "Создать визуалы и графику для социальных сетей", OrderIndex = 4 },
                        new() { Title = "Публикация и расписание", Description = "Опубликовать контент согласно календарю", OrderIndex = 5 },
                        new() { Title = "Взаимодействие с аудиторией", Description = "Отвечать на комментарии и вести диалог с подписчиками", OrderIndex = 6 },
                        new() { Title = "Анализ результатов", Description = "Проанализировать метрики и эффективность кампании", OrderIndex = 7 }
                    }
                },
                new TaskTemplate
                {
                    Name = "Email маркетинг",
                    Description = "Email кампании и рассылки",
                    Categories = new List<Category>(new[] { catMarketing }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Стратегия", Description = "Разработать стратегию email маркетинга и цели кампании", OrderIndex = 1 },
                        new() { Title = "Сегментация аудитории", Description = "Разделить аудиторию на сегменты для персонализации", OrderIndex = 2 },
                        new() { Title = "Написание писем", Description = "Создать тексты писем с убедительными subject lines", OrderIndex = 3 },
                        new() { Title = "Дизайн писем", Description = "Создать HTML-шаблоны писем с адаптивным дизайном", OrderIndex = 4 },
                        new() { Title = "A/B тестирование", Description = "Провести A/B тесты subject lines и содержания", OrderIndex = 5 },
                        new() { Title = "Отправка", Description = "Настроить и запустить email рассылки", OrderIndex = 6 },
                        new() { Title = "Анализ метрик", Description = "Проанализировать open rate, click rate и конверсию", OrderIndex = 7 }
                    }
                },
                new TaskTemplate
                {
                    Name = "Контент маркетинг",
                    Description = "Создание контента для привлечения трафика",
                    Categories = new List<Category>(new[] { catMarketing }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Анализ ниши", Description = "Исследовать нишу, конкурентов и потребности аудитории", OrderIndex = 1 },
                        new() { Title = "Контент стратегия", Description = "Разработать стратегию контента и editorial calendar", OrderIndex = 2 },
                        new() { Title = "Написание статей", Description = "Создать качественные статьи и blog посты", OrderIndex = 3 },
                        new() { Title = "Создание видео", Description = "Снять и смонтировать видео контент для различных платформ", OrderIndex = 4 },
                        new() { Title = "Дизайн инфографики", Description = "Создать информативную и привлекательную инфографику", OrderIndex = 5 },
                        new() { Title = "SEO оптимизация", Description = "Оптимизировать контент под ключевые слова и поисковики", OrderIndex = 6 },
                        new() { Title = "Распределение контента", Description = "Распространить контент по различным каналам", OrderIndex = 7 },
                        new() { Title = "Анализ эффективности", Description = "Измерить эффективность контента и ROI", OrderIndex = 8 }
                    }
                },
                new TaskTemplate
                {
                    Name = "PPC кампании",
                    Description = "Запуск и управление платными кампаниями",
                    Categories = new List<Category>(new[] { catMarketing }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Анализ целевой аудитории", Description = "Исследовать целевую аудиторию и создать buyer personas", OrderIndex = 1 },
                        new() { Title = "Выбор платформы", Description = "Выбрать подходящие рекламные платформы (Google Ads, Facebook и др.)", OrderIndex = 2 },
                        new() { Title = "Создание объявлений", Description = "Написать compelling ad copy и создать креативы", OrderIndex = 3 },
                        new() { Title = "Setup кампании", Description = "Настроить таргетинг, бюджет и стратегию ставок", OrderIndex = 4 },
                        new() { Title = "Установка пикселей", Description = "Настроить пиксели отслеживания и conversion tracking", OrderIndex = 5 },
                        new() { Title = "Запуск кампании", Description = "Запустить кампанию и провести первичную проверку", OrderIndex = 6 },
                        new() { Title = "Оптимизация", Description = "Оптимизировать кампанию на основе данных performance", OrderIndex = 7 },
                        new() { Title = "Анализ результатов", Description = "Проанализировать ROI, CPC, conversion rate и другие метрики", OrderIndex = 8 }
                    }
                },

                // ==================== КОПИРАЙТИНГ (4) ====================
                new TaskTemplate
                {
                    Name = "Копирайтинг (малый)",
                    Description = "Написание текстов для малых проектов",
                    Categories = new List<Category>(new[] { catCopy }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Исследование", Description = "Изучить продукт, аудиторию и конкурентов", OrderIndex = 1 },
                        new() { Title = "Первый черновик", Description = "Написать первую версию текста", OrderIndex = 2 },
                        new() { Title = "Доработка по feedback", Description = "Внести правки согласно замечаниям заказчика", OrderIndex = 3 },
                        new() { Title = "Финальная версия", Description = "Подготовить итоговую версию текста", OrderIndex = 4 }
                    }
                },
                new TaskTemplate
                {
                    Name = "Копирайтинг (большой)",
                    Description = "Написание текстов для больших проектов",
                    Categories = new List<Category>(new[] { catCopy }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Брифинг и анализ", Description = "Провести детальный брифинг и анализ задачи", OrderIndex = 1 },
                        new() { Title = "Исследование целевой аудитории", Description = "Глубоко изучить целевую аудиторию и их потребности", OrderIndex = 2 },
                        new() { Title = "Разработка структуры", Description = "Создать логичную структуру и план текста", OrderIndex = 3 },
                        new() { Title = "Написание черновиков", Description = "Написать черновые версии всех разделов", OrderIndex = 4 },
                        new() { Title = "Корректировка", Description = "Провести самостоятельную корректировку и редактирование", OrderIndex = 5 },
                        new() { Title = "Feedback сессия 1", Description = "Обсудить первую версию с заказчиком", OrderIndex = 6 },
                        new() { Title = "Доработка", Description = "Внести изменения по результатам первого feedback", OrderIndex = 7 },
                        new() { Title = "Feedback сессия 2", Description = "Провести вторую сессию обратной связи", OrderIndex = 8 },
                        new() { Title = "Финальная доработка", Description = "Внести последние правки и улучшения", OrderIndex = 9 },
                        new() { Title = "Финальная версия", Description = "Подготовить итоговую отполированную версию", OrderIndex = 10 }
                    }
                },
                new TaskTemplate
                {
                    Name = "Продакшн-копирайтинг",
                    Description = "Копирайтинг для коммерческих предложений",
                    Categories = new List<Category>(new[] { catCopy }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Анализ продукта", Description = "Детально изучить продукт, его преимущества и особенности", OrderIndex = 1 },
                        new() { Title = "Анализ целевой аудитории", Description = "Исследовать боли, потребности и мотивации целевой аудитории", OrderIndex = 2 },
                        new() { Title = "USP определение", Description = "Сформулировать уникальное торговое предложение", OrderIndex = 3 },
                        new() { Title = "Написание заголовков", Description = "Создать цепляющие и продающие заголовки", OrderIndex = 4 },
                        new() { Title = "Написание основного текста", Description = "Написать убедительный основной текст с benefits", OrderIndex = 5 },
                        new() { Title = "Написание CTA", Description = "Создать призывы к действию и финальные предложения", OrderIndex = 6 },
                        new() { Title = "A/B версии", Description = "Подготовить альтернативные версии для тестирования", OrderIndex = 7 },
                        new() { Title = "Тестирование", Description = "Протестировать эффективность различных версий", OrderIndex = 8 },
                        new() { Title = "Оптимизация", Description = "Оптимизировать текст на основе результатов тестов", OrderIndex = 9 }
                    }
                },
                new TaskTemplate
                {
                    Name = "Техническое письмо",
                    Description = "Написание технической документации",
                    Categories = new List<Category>(new[] { catCopy }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Сбор информации", Description = "Собрать всю техническую информацию от экспертов", OrderIndex = 1 },
                        new() { Title = "Структурирование", Description = "Организовать информацию в логичную структуру", OrderIndex = 2 },
                        new() { Title = "Написание", Description = "Написать документацию понятным техническим языком", OrderIndex = 3 },
                        new() { Title = "Добавление диаграмм", Description = "Создать схемы, диаграммы и визуальные пояснения", OrderIndex = 4 },
                        new() { Title = "Проверка точности", Description = "Проверить техническую точность с экспертами", OrderIndex = 5 },
                        new() { Title = "Редактирование", Description = "Отредактировать текст для ясности и читаемости", OrderIndex = 6 },
                        new() { Title = "Финальный обзор", Description = "Провести финальную проверку и форматирование", OrderIndex = 7 }
                    }
                },

                // ==================== SEO (4) ====================
                new TaskTemplate
                {
                    Name = "SEO аудит",
                    Description = "Полный аудит сайта",
                    Categories = new List<Category>(new[] { catSeo }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Техническая проверка", Description = "Проверить скорость загрузки, мобильность, индексацию", OrderIndex = 1 },
                        new() { Title = "On-page анализ", Description = "Анализ контента, мета-тегов, структуры страниц", OrderIndex = 2 },
                        new() { Title = "Off-page анализ", Description = "Исследование ссылочного профиля и внешних факторов", OrderIndex = 3 },
                        new() { Title = "Конкурентный анализ", Description = "Анализ SEO стратегий основных конкурентов", OrderIndex = 4 },
                        new() { Title = "Анализ ключевых слов", Description = "Исследование текущих позиций и возможностей", OrderIndex = 5 },
                        new() { Title = "Отчет и рекомендации", Description = "Подготовить детальный отчет с планом улучшений", OrderIndex = 6 }
                    }
                },
                new TaskTemplate
                {
                    Name = "SEO оптимизация",
                    Description = "Оптимизация сайта для поисковиков",
                    Categories = new List<Category>(new[] { catSeo }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Исследование ключевых слов", Description = "Найти релевантные ключевые слова с хорошим потенциалом", OrderIndex = 1 },
                        new() { Title = "On-page оптимизация", Description = "Оптимизировать title, meta, заголовки и контент", OrderIndex = 2 },
                        new() { Title = "Technical SEO", Description = "Исправить технические проблемы и улучшить crawlability", OrderIndex = 3 },
                        new() { Title = "Content оптимизация", Description = "Оптимизировать существующий контент под ключевые слова", OrderIndex = 4 },
                        new() { Title = "Link building", Description = "Построить качественный ссылочный профиль", OrderIndex = 5 },
                        new() { Title = "Local SEO", Description = "Оптимизировать для локального поиска (если применимо)", OrderIndex = 6 },
                        new() { Title = "Schema markup", Description = "Внедрить структурированные данные", OrderIndex = 7 },
                        new() { Title = "Мониторинг и отчеты", Description = "Настроить отслеживание позиций и подготовить отчеты", OrderIndex = 8 }
                    }
                },
                new TaskTemplate
                {
                    Name = "Keyword Research",
                    Description = "Исследование ключевых слов",
                    Categories = new List<Category>(new[] { catSeo }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Анализ ниши", Description = "Изучить специфику ниши и особенности поискового поведения", OrderIndex = 1 },
                        new() { Title = "Сбор ключевых слов", Description = "Собрать максимальный список потенциальных ключевых слов", OrderIndex = 2 },
                        new() { Title = "Анализ конкурентов", Description = "Исследовать ключевые слова конкурентов", OrderIndex = 3 },
                        new() { Title = "Фильтрация и группировка", Description = "Отфильтровать и сгруппировать ключевые слова по темам", OrderIndex = 4 },
                        new() { Title = "Анализ сложности", Description = "Оценить сложность продвижения по каждому ключевому слову", OrderIndex = 5 },
                        new() { Title = "Анализ объема поиска", Description = "Проанализировать частоту запросов и сезонность", OrderIndex = 6 },
                        new() { Title = "Отчет с рекомендациями", Description = "Подготовить итоговый отчет с приоритетами", OrderIndex = 7 }
                    }
                },
                new TaskTemplate
                {
                    Name = "Link Building",
                    Description = "Построение ссылочного профиля",
                    Categories = new List<Category>(new[] { catSeo }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Анализ текущего профиля", Description = "Проанализировать существующие обратные ссылки", OrderIndex = 1 },
                        new() { Title = "Исследование конкурентов", Description = "Изучить ссылочные профили успешных конкурентов", OrderIndex = 2 },
                        new() { Title = "Поиск источников ссылок", Description = "Найти качественные сайты для размещения ссылок", OrderIndex = 3 },
                        new() { Title = "Outreach кампании", Description = "Связаться с владельцами сайтов для получения ссылок", OrderIndex = 4 },
                        new() { Title = "Создание контента для ссылок", Description = "Подготовить качественный контент для линкбилдинга", OrderIndex = 5 },
                        new() { Title = "Отслеживание прогресса", Description = "Мониторить получение новых ссылок", OrderIndex = 6 },
                        new() { Title = "Отчетность", Description = "Подготавливать регулярные отчеты по линкбилдингу", OrderIndex = 7 }
                    }
                },

                // ==================== ТЕСТИРОВАНИЕ (3) ====================
                new TaskTemplate
                {
                    Name = "QA Testing",
                    Description = "Тестирование приложения",
                    Categories = new List<Category>(new[] { catTest }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Изучение требований", Description = "Детально изучить техническое задание и требования", OrderIndex = 1 },
                        new() { Title = "Разработка тест-планов", Description = "Создать comprehensive тест-планы и test cases", OrderIndex = 2 },
                        new() { Title = "Функциональное тестирование", Description = "Протестировать все функции согласно требованиям", OrderIndex = 3 },
                        new() { Title = "Регрессионное тестирование", Description = "Убедиться, что новые изменения не сломали существующий функционал", OrderIndex = 4 },
                        new() { Title = "Баг репортинг", Description = "Документировать найденные баги с подробным описанием", OrderIndex = 5 },
                        new() { Title = "Performance тестирование", Description = "Протестировать производительность под нагрузкой", OrderIndex = 6 },
                        new() { Title = "Финальная проверка", Description = "Провести итоговую проверку перед релизом", OrderIndex = 7 }
                    }
                },
                new TaskTemplate
                {
                    Name = "Автоматизированное тестирование",
                    Description = "Разработка автотестов",
                    Categories = new List<Category>(new[] { catTest }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Анализ требований", Description = "Определить что именно нужно покрыть автотестами", OrderIndex = 1 },
                        new() { Title = "Выбор фреймворка", Description = "Выбрать подходящий framework для автотестирования", OrderIndex = 2 },
                        new() { Title = "Setup окружения", Description = "Настроить среду разработки и выполнения тестов", OrderIndex = 3 },
                        new() { Title = "Разработка тестов", Description = "Написать автоматизированные тесты для ключевого функционала", OrderIndex = 4 },
                        new() { Title = "Интеграция CI/CD", Description = "Интегрировать автотесты в pipeline CI/CD", OrderIndex = 5 },
                        new() { Title = "Maintenance тестов", Description = "Поддерживать актуальность тестов при изменениях", OrderIndex = 6 },
                        new() { Title = "Отчетность", Description = "Настроить детальную отчетность по результатам тестов", OrderIndex = 7 }
                    }
                },
                new TaskTemplate
                {
                    Name = "Тестирование безопасности",
                    Description = "Security тестирование",
                    Categories = new List<Category>(new[] { catTest }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Анализ угроз", Description = "Идентифицировать потенциальные угрозы безопасности", OrderIndex = 1 },
                        new() { Title = "Проверка уязвимостей", Description = "Сканировать приложение на известные уязвимости", OrderIndex = 2 },
                        new() { Title = "Penetration testing", Description = "Провести проникающее тестирование системы", OrderIndex = 3 },
                        new() { Title = "Анализ шифрования", Description = "Проверить правильность реализации шифрования", OrderIndex = 4 },
                        new() { Title = "Проверка аутентификации", Description = "Протестировать систему аутентификации и авторизации", OrderIndex = 5 },
                        new() { Title = "SQL injection тесты", Description = "Проверить защищенность от SQL инъекций", OrderIndex = 6 },
                        new() { Title = "Отчет с рекомендациями", Description = "Подготовить отчет с найденными проблемами и рекомендациями", OrderIndex = 7 }
                    }
                },

                // ==================== АДМИНИСТРИРОВАНИЕ (3) ====================
                new TaskTemplate
                {
                    Name = "DevOps/Deployment",
                    Description = "Развертывание и поддержка инфраструктуры",
                    Categories = new List<Category>(new[] { catAdmin }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Анализ требований", Description = "Определить требования к инфраструктуре и нагрузке", OrderIndex = 1 },
                        new() { Title = "Выбор инструментов", Description = "Выбрать подходящие инструменты для CI/CD и мониторинга", OrderIndex = 2 },
                        new() { Title = "Setup сервера", Description = "Настроить production сервера и окружение", OrderIndex = 3 },
                        new() { Title = "CI/CD настройка", Description = "Настроить автоматическое развертывание и интеграцию", OrderIndex = 4 },
                        new() { Title = "Database setup", Description = "Настроить базы данных и репликацию", OrderIndex = 5 },
                        new() { Title = "Безопасность", Description = "Настроить security конфигурации и firewall", OrderIndex = 6 },
                        new() { Title = "Мониторинг", Description = "Настроить мониторинг производительности и алерты", OrderIndex = 7 },
                        new() { Title = "Документация", Description = "Документировать инфраструктуру и процедуры", OrderIndex = 8 }
                    }
                },
                new TaskTemplate
                {
                    Name = "Cloud Infrastructure",
                    Description = "Настройка облачной инфраструктуры",
                    Categories = new List<Category>(new[] { catAdmin }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Выбор облачного провайдера", Description = "Выбрать подходящего cloud provider (AWS, Azure, GCP)", OrderIndex = 1 },
                        new() { Title = "Setup аккаунта", Description = "Настроить cloud аккаунт и billing", OrderIndex = 2 },
                        new() { Title = "Настройка VPC", Description = "Настроить виртуальную приватную сеть", OrderIndex = 3 },
                        new() { Title = "Deploy приложения", Description = "Развернуть приложение в облачной среде", OrderIndex = 4 },
                        new() { Title = "Database настройка", Description = "Настроить managed database сервисы", OrderIndex = 5 },
                        new() { Title = "Load balancing", Description = "Настроить балансировщики нагрузки", OrderIndex = 6 },
                        new() { Title = "Auto-scaling", Description = "Настроить автоматическое масштабирование", OrderIndex = 7 },
                        new() { Title = "Мониторинг и логирование", Description = "Настроить cloud-native мониторинг и логи", OrderIndex = 8 },
                        new() { Title = "Backup & Recovery", Description = "Настроить резервное копирование и disaster recovery", OrderIndex = 9 }
                    }
                },
                new TaskTemplate
                {
                    Name = "Server Administration",
                    Description = "Администрирование серверов",
                    Categories = new List<Category>(new[] { catAdmin }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Setup сервера", Description = "Настроить физические или виртуальные серверы", OrderIndex = 1 },
                        new() { Title = "Установка ОС", Description = "Установить и настроить операционную систему", OrderIndex = 2 },
                        new() { Title = "Настройка сети", Description = "Настроить сетевые подключения и конфигурации", OrderIndex = 3 },
                        new() { Title = "Установка ПО", Description = "Установить необходимое программное обеспечение", OrderIndex = 4 },
                        new() { Title = "Настройка безопасности", Description = "Настроить security политики и доступы", OrderIndex = 5 },
                        new() { Title = "Firewall конфигурация", Description = "Настроить firewall правила и защиту", OrderIndex = 6 },
                        new() { Title = "Backup стратегия", Description = "Реализовать стратегию резервного копирования", OrderIndex = 7 },
                        new() { Title = "Monitoring setup", Description = "Настроить мониторинг состояния сервера", OrderIndex = 8 },
                        new() { Title = "Техническая поддержка", Description = "Обеспечить ongoing техническую поддержку", OrderIndex = 9 }
                    }
                },

                // ==================== АНАЛИТИКА (3) ====================
                new TaskTemplate
                {
                    Name = "Data Analysis",
                    Description = "Анализ данных и составление отчетов",
                    Categories = new List<Category>(new[] { catAnalytics }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Определение KPI", Description = "Определить ключевые показатели эффективности для анализа", OrderIndex = 1 },
                        new() { Title = "Сбор данных", Description = "Собрать необходимые данные из различных источников", OrderIndex = 2 },
                        new() { Title = "Очистка данных", Description = "Очистить и подготовить данные для анализа", OrderIndex = 3 },
                        new() { Title = "Анализ", Description = "Провести статистический анализ и найти закономерности", OrderIndex = 4 },
                        new() { Title = "Визуализация", Description = "Создать наглядные графики и диаграммы", OrderIndex = 5 },
                        new() { Title = "Выводы и рекомендации", Description = "Сформулировать выводы и actionable рекомендации", OrderIndex = 6 },
                        new() { Title = "Отчет", Description = "Подготовить comprehensive отчет с результатами", OrderIndex = 7 }
                    }
                },
                new TaskTemplate
                {
                    Name = "Business Intelligence",
                    Description = "BI решения и dashboards",
                    Categories = new List<Category>(new[] { catAnalytics }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Анализ бизнес-процессов", Description = "Изучить текущие бизнес-процессы и потребности", OrderIndex = 1 },
                        new() { Title = "Определение метрик", Description = "Определить ключевые бизнес-метрики для отслеживания", OrderIndex = 2 },
                        new() { Title = "Выбор инструмента BI", Description = "Выбрать подходящую BI платформу (Tableau, Power BI и др.)", OrderIndex = 3 },
                        new() { Title = "Setup подключений", Description = "Настроить подключения к источникам данных", OrderIndex = 4 },
                        new() { Title = "Разработка dashboards", Description = "Создать интерактивные дашборды для мониторинга", OrderIndex = 5 },
                        new() { Title = "Создание отчетов", Description = "Разработать automated отчеты для stakeholders", OrderIndex = 6 },
                        new() { Title = "Setup автоматизации", Description = "Настроить автоматическое обновление данных", OrderIndex = 7 },
                        new() { Title = "Обучение пользователей", Description = "Обучить пользователей работе с BI инструментами", OrderIndex = 8 }
                    }
                },
                new TaskTemplate
                {
                    Name = "A/B Testing",
                    Description = "Проведение A/B тестов",
                    Categories = new List<Category>(new[] { catAnalytics }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Гипотеза формулировка", Description = "Сформулировать четкую гипотезу для тестирования", OrderIndex = 1 },
                        new() { Title = "Расчет размера выборки", Description = "Рассчитать необходимый размер выборки для статистической значимости", OrderIndex = 2 },
                        new() { Title = "Setup тестирования", Description = "Настроить инфраструктуру для проведения A/B теста", OrderIndex = 3 },
                        new() { Title = "Реализация вариантов", Description = "Создать и внедрить варианты A и B для тестирования", OrderIndex = 4 },
                        new() { Title = "Запуск теста", Description = "Запустить A/B тест и начать сбор данных", OrderIndex = 5 },
                        new() { Title = "Мониторинг результатов", Description = "Отслеживать прогресс и качество данных", OrderIndex = 6 },
                        new() { Title = "Статистический анализ", Description = "Провести статистический анализ результатов", OrderIndex = 7 },
                        new() { Title = "Выводы и рекомендации", Description = "Сделать выводы и дать рекомендации на основе результатов", OrderIndex = 8 }
                    }
                }
            };

            await context.TaskTemplates.AddRangeAsync(templates);
            await context.SaveChangesAsync();
        }
    }
}
