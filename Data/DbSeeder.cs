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
                        new() { Title = "Дизайн макета", OrderIndex = 1 },
                        new() { Title = "Верстка HTML/CSS", OrderIndex = 2 },
                        new() { Title = "Подключение форм", OrderIndex = 3 },
                        new() { Title = "Тестирование", OrderIndex = 4 },
                        new() { Title = "Развёртывание", OrderIndex = 5 }
                    }
                },
                new TaskTemplate
                {
                    Name = "Корпоративный сайт",
                    Description = "Многостраничный сайт для компании",
                    Categories = new List<Category>(new[] { catWeb }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Анализ требований", OrderIndex = 1 },
                        new() { Title = "Дизайн системы", OrderIndex = 2 },
                        new() { Title = "Верстка всех страниц", OrderIndex = 3 },
                        new() { Title = "CMS интеграция", OrderIndex = 4 },
                        new() { Title = "SEO оптимизация", OrderIndex = 5 },
                        new() { Title = "Мобильная адаптация", OrderIndex = 6 },
                        new() { Title = "Тестирование", OrderIndex = 7 },
                        new() { Title = "Развёртывание", OrderIndex = 8 }
                    }
                },
                new TaskTemplate
                {
                    Name = "Интернет-магазин",
                    Description = "Полнофункциональный e-commerce проект",
                    Categories = new List<Category>(new[] { catWeb }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Анализ требований", OrderIndex = 1 },
                        new() { Title = "Дизайн интерфейса", OrderIndex = 2 },
                        new() { Title = "Верстка фронтенда", OrderIndex = 3 },
                        new() { Title = "Backend разработка", OrderIndex = 4 },
                        new() { Title = "Интеграция платежей", OrderIndex = 5 },
                        new() { Title = "Интеграция доставки", OrderIndex = 6 },
                        new() { Title = "Admin панель", OrderIndex = 7 },
                        new() { Title = "Система уведомлений", OrderIndex = 8 },
                        new() { Title = "Тестирование", OrderIndex = 9 },
                        new() { Title = "Развёртывание", OrderIndex = 10 }
                    }
                },
                new TaskTemplate
                {
                    Name = "SPA приложение",
                    Description = "Single Page Application на React/Vue",
                    Categories = new List<Category>(new[] { catWeb }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Архитектура приложения", OrderIndex = 1 },
                        new() { Title = "Дизайн компонентов", OrderIndex = 2 },
                        new() { Title = "Разработка фронтенда", OrderIndex = 3 },
                        new() { Title = "API разработка", OrderIndex = 4 },
                        new() { Title = "Аутентификация", OrderIndex = 5 },
                        new() { Title = "State management", OrderIndex = 6 },
                        new() { Title = "Unit тестирование", OrderIndex = 7 },
                        new() { Title = "E2E тестирование", OrderIndex = 8 },
                        new() { Title = "Оптимизация", OrderIndex = 9 },
                        new() { Title = "Развёртывание", OrderIndex = 10 }
                    }
                },
                new TaskTemplate
                {
                    Name = "REST API",
                    Description = "Разработка backend API без фронтенда",
                    Categories = new List<Category>(new[] { catWeb }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Дизайн API", OrderIndex = 1 },
                        new() { Title = "Разработка endpoints", OrderIndex = 2 },
                        new() { Title = "Работа с БД", OrderIndex = 3 },
                        new() { Title = "Аутентификация", OrderIndex = 4 },
                        new() { Title = "Авторизация", OrderIndex = 5 },
                        new() { Title = "Валидация данных", OrderIndex = 6 },
                        new() { Title = "Кэширование", OrderIndex = 7 },
                        new() { Title = "Документация API", OrderIndex = 8 },
                        new() { Title = "Unit тестирование", OrderIndex = 9 },
                        new() { Title = "Развёртывание", OrderIndex = 10 }
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
                        new() { Title = "Дизайн UI/UX", OrderIndex = 1 },
                        new() { Title = "Разработка интерфейсов", OrderIndex = 2 },
                        new() { Title = "Бизнес-логика", OrderIndex = 3 },
                        new() { Title = "Локальное хранилище", OrderIndex = 4 },
                        new() { Title = "Тестирование", OrderIndex = 5 },
                        new() { Title = "Публикация", OrderIndex = 6 }
                    }
                },
                new TaskTemplate
                {
                    Name = "Cross-Platform App",
                    Description = "Приложение для iOS и Android",
                    Categories = new List<Category>(new[] { catMobile }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Дизайн UI/UX", OrderIndex = 1 },
                        new() { Title = "Setup проекта", OrderIndex = 2 },
                        new() { Title = "Разработка компонентов", OrderIndex = 3 },
                        new() { Title = "API интеграция", OrderIndex = 4 },
                        new() { Title = "Локальная БД", OrderIndex = 5 },
                        new() { Title = "Native модули", OrderIndex = 6 },
                        new() { Title = "Тестирование iOS", OrderIndex = 7 },
                        new() { Title = "Тестирование Android", OrderIndex = 8 },
                        new() { Title = "Публикация в App Store", OrderIndex = 9 },
                        new() { Title = "Публикация в Google Play", OrderIndex = 10 }
                    }
                },
                new TaskTemplate
                {
                    Name = "Advanced Mobile App",
                    Description = "Сложное приложение с расширенным функционалом",
                    Categories = new List<Category>(new[] { catMobile }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Архитектура приложения", OrderIndex = 1 },
                        new() { Title = "Дизайн системы", OrderIndex = 2 },
                        new() { Title = "Разработка фронтенда", OrderIndex = 3 },
                        new() { Title = "Backend интеграция", OrderIndex = 4 },
                        new() { Title = "WebSocket/Real-time", OrderIndex = 5 },
                        new() { Title = "Оффлайн синхронизация", OrderIndex = 6 },
                        new() { Title = "Геолокация", OrderIndex = 7 },
                        new() { Title = "Push уведомления", OrderIndex = 8 },
                        new() { Title = "Аналитика", OrderIndex = 9 },
                        new() { Title = "Юнит тесты", OrderIndex = 10 },
                        new() { Title = "UI/UX тесты", OrderIndex = 11 },
                        new() { Title = "Оптимизация", OrderIndex = 12 },
                        new() { Title = "Публикация", OrderIndex = 13 }
                    }
                },
                new TaskTemplate
                {
                    Name = "Mobile Game",
                    Description = "Разработка мобильной игры",
                    Categories = new List<Category>(new[] { catMobile }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Game Design Document", OrderIndex = 1 },
                        new() { Title = "Концепт-арт", OrderIndex = 2 },
                        new() { Title = "Разработка ядра игры", OrderIndex = 3 },
                        new() { Title = "Graphics & Animation", OrderIndex = 4 },
                        new() { Title = "Sound & Music", OrderIndex = 5 },
                        new() { Title = "UI/UX интерфейс", OrderIndex = 6 },
                        new() { Title = "Gameplay mechanics", OrderIndex = 7 },
                        new() { Title = "Оптимизация", OrderIndex = 8 },
                        new() { Title = "Beta тестирование", OrderIndex = 9 },
                        new() { Title = "Публикация", OrderIndex = 10 }
                    }
                },
                new TaskTemplate
                {
                    Name = "iOS App",
                    Description = "Native приложение для iOS",
                    Categories = new List<Category>(new[] { catMobile }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Дизайн интерфейса", OrderIndex = 1 },
                        new() { Title = "Swift разработка", OrderIndex = 2 },
                        new() { Title = "Core Data", OrderIndex = 3 },
                        new() { Title = "Network запросы", OrderIndex = 4 },
                        new() { Title = "UserDefaults", OrderIndex = 5 },
                        new() { Title = "Notifications", OrderIndex = 6 },
                        new() { Title = "Unit тесты", OrderIndex = 7 },
                        new() { Title = "UI тесты", OrderIndex = 8 },
                        new() { Title = "Code review", OrderIndex = 9 },
                        new() { Title = "Отправка в App Store", OrderIndex = 10 }
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
                        new() { Title = "Исследование конкурентов", OrderIndex = 1 },
                        new() { Title = "Концепция и эскизы", OrderIndex = 2 },
                        new() { Title = "Первые варианты", OrderIndex = 3 },
                        new() { Title = "Доработка по feedback", OrderIndex = 4 },
                        new() { Title = "Финальный вариант", OrderIndex = 5 },
                        new() { Title = "Подготовка файлов", OrderIndex = 6 }
                    }
                },
                new TaskTemplate
                {
                    Name = "UI/UX Design",
                    Description = "Дизайн интерфейса приложения или сайта",
                    Categories = new List<Category>(new[] { catDesign }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Анализ требований", OrderIndex = 1 },
                        new() { Title = "User Research", OrderIndex = 2 },
                        new() { Title = "Wireframes", OrderIndex = 3 },
                        new() { Title = "High-Fidelity Mockups", OrderIndex = 4 },
                        new() { Title = "Design System", OrderIndex = 5 },
                        new() { Title = "Прототипирование", OrderIndex = 6 },
                        new() { Title = "Usability тестирование", OrderIndex = 7 },
                        new() { Title = "Итерация дизайна", OrderIndex = 8 },
                        new() { Title = "Подготовка для разработки", OrderIndex = 9 }
                    }
                },
                new TaskTemplate
                {
                    Name = "Брендирование",
                    Description = "Полный брендинг компании",
                    Categories = new List<Category>(new[] { catDesign }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Brand Strategy", OrderIndex = 1 },
                        new() { Title = "Logo разработка", OrderIndex = 2 },
                        new() { Title = "Color Palette", OrderIndex = 3 },
                        new() { Title = "Typography", OrderIndex = 4 },
                        new() { Title = "Brand Guidelines", OrderIndex = 5 },
                        new() { Title = "Визуальные элементы", OrderIndex = 6 },
                        new() { Title = "Фирменный стиль", OrderIndex = 7 },
                        new() { Title = "Mock-ups применения", OrderIndex = 8 }
                    }
                },
                new TaskTemplate
                {
                    Name = "Веб-дизайн",
                    Description = "Дизайн веб-сайта",
                    Categories = new List<Category>(new[] { catDesign }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Анализ конкурентов", OrderIndex = 1 },
                        new() { Title = "Карта сайта (sitemap)", OrderIndex = 2 },
                        new() { Title = "Wireframes", OrderIndex = 3 },
                        new() { Title = "Дизайн главной страницы", OrderIndex = 4 },
                        new() { Title = "Дизайн внутренних страниц", OrderIndex = 5 },
                        new() { Title = "Мобильная версия", OrderIndex = 6 },
                        new() { Title = "Интерактивные элементы", OrderIndex = 7 },
                        new() { Title = "Feedback итерация", OrderIndex = 8 },
                        new() { Title = "Финальный макет", OrderIndex = 9 }
                    }
                },
                new TaskTemplate
                {
                    Name = "Графика и иллюстрации",
                    Description = "Создание графики и иллюстраций",
                    Categories = new List<Category>(new[] { catDesign }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Концепция", OrderIndex = 1 },
                        new() { Title = "Эскизы", OrderIndex = 2 },
                        new() { Title = "Линейные работы", OrderIndex = 3 },
                        new() { Title = "Цветовая обработка", OrderIndex = 4 },
                        new() { Title = "Детализация", OrderIndex = 5 },
                        new() { Title = "Финальная обработка", OrderIndex = 6 },
                        new() { Title = "Доработка по замечаниям", OrderIndex = 7 }
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
                        new() { Title = "Стратегия", OrderIndex = 1 },
                        new() { Title = "Контент-план", OrderIndex = 2 },
                        new() { Title = "Создание контента", OrderIndex = 3 },
                        new() { Title = "Дизайн постов", OrderIndex = 4 },
                        new() { Title = "Публикация и расписание", OrderIndex = 5 },
                        new() { Title = "Взаимодействие с аудиторией", OrderIndex = 6 },
                        new() { Title = "Анализ результатов", OrderIndex = 7 }
                    }
                },
                new TaskTemplate
                {
                    Name = "Email маркетинг",
                    Description = "Email кампании и рассылки",
                    Categories = new List<Category>(new[] { catMarketing }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Стратегия", OrderIndex = 1 },
                        new() { Title = "Сегментация аудитории", OrderIndex = 2 },
                        new() { Title = "Написание писем", OrderIndex = 3 },
                        new() { Title = "Дизайн писем", OrderIndex = 4 },
                        new() { Title = "A/B тестирование", OrderIndex = 5 },
                        new() { Title = "Отправка", OrderIndex = 6 },
                        new() { Title = "Анализ метрик", OrderIndex = 7 }
                    }
                },
                new TaskTemplate
                {
                    Name = "Контент маркетинг",
                    Description = "Создание контента для привлечения трафика",
                    Categories = new List<Category>(new[] { catMarketing }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Анализ ниши", OrderIndex = 1 },
                        new() { Title = "Контент стратегия", OrderIndex = 2 },
                        new() { Title = "Написание статей", OrderIndex = 3 },
                        new() { Title = "Создание видео", OrderIndex = 4 },
                        new() { Title = "Дизайн инфографики", OrderIndex = 5 },
                        new() { Title = "SEO оптимизация", OrderIndex = 6 },
                        new() { Title = "Распределение контента", OrderIndex = 7 },
                        new() { Title = "Анализ эффективности", OrderIndex = 8 }
                    }
                },
                new TaskTemplate
                {
                    Name = "PPC кампании",
                    Description = "Запуск и управление платными кампаниями",
                    Categories = new List<Category>(new[] { catMarketing }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Анализ целевой аудитории", OrderIndex = 1 },
                        new() { Title = "Выбор платформы", OrderIndex = 2 },
                        new() { Title = "Создание объявлений", OrderIndex = 3 },
                        new() { Title = "Setup кампании", OrderIndex = 4 },
                        new() { Title = "Установка пикселей", OrderIndex = 5 },
                        new() { Title = "Запуск кампании", OrderIndex = 6 },
                        new() { Title = "Оптимизация", OrderIndex = 7 },
                        new() { Title = "Анализ результатов", OrderIndex = 8 }
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
                        new() { Title = "Исследование", OrderIndex = 1 },
                        new() { Title = "Первый черновик", OrderIndex = 2 },
                        new() { Title = "Доработка по feedback", OrderIndex = 3 },
                        new() { Title = "Финальная версия", OrderIndex = 4 }
                    }
                },
                new TaskTemplate
                {
                    Name = "Копирайтинг (большой)",
                    Description = "Написание текстов для больших проектов",
                    Categories = new List<Category>(new[] { catCopy }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Брифинг и анализ", OrderIndex = 1 },
                        new() { Title = "Исследование целевой аудитории", OrderIndex = 2 },
                        new() { Title = "Разработка структуры", OrderIndex = 3 },
                        new() { Title = "Написание черновиков", OrderIndex = 4 },
                        new() { Title = "Корректировка", OrderIndex = 5 },
                        new() { Title = "Feedback сессия 1", OrderIndex = 6 },
                        new() { Title = "Доработка", OrderIndex = 7 },
                        new() { Title = "Feedback сессия 2", OrderIndex = 8 },
                        new() { Title = "Финальная доработка", OrderIndex = 9 },
                        new() { Title = "Финальная версия", OrderIndex = 10 }
                    }
                },
                new TaskTemplate
                {
                    Name = "Продакшн-копирайтинг",
                    Description = "Копирайтинг для коммерческих предложений",
                    Categories = new List<Category>(new[] { catCopy }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Анализ продукта", OrderIndex = 1 },
                        new() { Title = "Анализ целевой аудитории", OrderIndex = 2 },
                        new() { Title = "USP определение", OrderIndex = 3 },
                        new() { Title = "Написание заголовков", OrderIndex = 4 },
                        new() { Title = "Написание основного текста", OrderIndex = 5 },
                        new() { Title = "Написание CTA", OrderIndex = 6 },
                        new() { Title = "A/B версии", OrderIndex = 7 },
                        new() { Title = "Тестирование", OrderIndex = 8 },
                        new() { Title = "Оптимизация", OrderIndex = 9 }
                    }
                },
                new TaskTemplate
                {
                    Name = "Техническое письмо",
                    Description = "Написание технической документации",
                    Categories = new List<Category>(new[] { catCopy }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Сбор информации", OrderIndex = 1 },
                        new() { Title = "Структурирование", OrderIndex = 2 },
                        new() { Title = "Написание", OrderIndex = 3 },
                        new() { Title = "Добавление диаграмм", OrderIndex = 4 },
                        new() { Title = "Проверка точности", OrderIndex = 5 },
                        new() { Title = "Редактирование", OrderIndex = 6 },
                        new() { Title = "Финальный обзор", OrderIndex = 7 }
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
                        new() { Title = "Техническая проверка", OrderIndex = 1 },
                        new() { Title = "On-page анализ", OrderIndex = 2 },
                        new() { Title = "Off-page анализ", OrderIndex = 3 },
                        new() { Title = "Конкурентный анализ", OrderIndex = 4 },
                        new() { Title = "Анализ ключевых слов", OrderIndex = 5 },
                        new() { Title = "Отчет и рекомендации", OrderIndex = 6 }
                    }
                },
                new TaskTemplate
                {
                    Name = "SEO оптимизация",
                    Description = "Оптимизация сайта для поисковиков",
                    Categories = new List<Category>(new[] { catSeo }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Исследование ключевых слов", OrderIndex = 1 },
                        new() { Title = "On-page оптимизация", OrderIndex = 2 },
                        new() { Title = "Technical SEO", OrderIndex = 3 },
                        new() { Title = "Content оптимизация", OrderIndex = 4 },
                        new() { Title = "Link building", OrderIndex = 5 },
                        new() { Title = "Local SEO", OrderIndex = 6 },
                        new() { Title = "Schema markup", OrderIndex = 7 },
                        new() { Title = "Мониторинг и отчеты", OrderIndex = 8 }
                    }
                },
                new TaskTemplate
                {
                    Name = "Keyword Research",
                    Description = "Исследование ключевых слов",
                    Categories = new List<Category>(new[] { catSeo }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Анализ ниши", OrderIndex = 1 },
                        new() { Title = "Сбор ключевых слов", OrderIndex = 2 },
                        new() { Title = "Анализ конкурентов", OrderIndex = 3 },
                        new() { Title = "Фильтрация и группировка", OrderIndex = 4 },
                        new() { Title = "Анализ сложности", OrderIndex = 5 },
                        new() { Title = "Анализ объема поиска", OrderIndex = 6 },
                        new() { Title = "Отчет с рекомендациями", OrderIndex = 7 }
                    }
                },
                new TaskTemplate
                {
                    Name = "Link Building",
                    Description = "Построение ссылочного профиля",
                    Categories = new List<Category>(new[] { catSeo }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Анализ текущего профиля", OrderIndex = 1 },
                        new() { Title = "Исследование конкурентов", OrderIndex = 2 },
                        new() { Title = "Поиск источников ссылок", OrderIndex = 3 },
                        new() { Title = "Outreach кампании", OrderIndex = 4 },
                        new() { Title = "Создание контента для ссылок", OrderIndex = 5 },
                        new() { Title = "Отслеживание прогресса", OrderIndex = 6 },
                        new() { Title = "Отчетность", OrderIndex = 7 }
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
                        new() { Title = "Изучение требований", OrderIndex = 1 },
                        new() { Title = "Разработка тест-планов", OrderIndex = 2 },
                        new() { Title = "Функциональное тестирование", OrderIndex = 3 },
                        new() { Title = "Регрессионное тестирование", OrderIndex = 4 },
                        new() { Title = "Баг репортинг", OrderIndex = 5 },
                        new() { Title = "Performance тестирование", OrderIndex = 6 },
                        new() { Title = "Финальная проверка", OrderIndex = 7 }
                    }
                },
                new TaskTemplate
                {
                    Name = "Автоматизированное тестирование",
                    Description = "Разработка автотестов",
                    Categories = new List<Category>(new[] { catTest }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Анализ требований", OrderIndex = 1 },
                        new() { Title = "Выбор фреймворка", OrderIndex = 2 },
                        new() { Title = "Setup окружения", OrderIndex = 3 },
                        new() { Title = "Разработка тестов", OrderIndex = 4 },
                        new() { Title = "Интеграция CI/CD", OrderIndex = 5 },
                        new() { Title = "Maintenance тестов", OrderIndex = 6 },
                        new() { Title = "Отчетность", OrderIndex = 7 }
                    }
                },
                new TaskTemplate
                {
                    Name = "Тестирование безопасности",
                    Description = "Security тестирование",
                    Categories = new List<Category>(new[] { catTest }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Анализ угроз", OrderIndex = 1 },
                        new() { Title = "Проверка уязвимостей", OrderIndex = 2 },
                        new() { Title = "Penetration testing", OrderIndex = 3 },
                        new() { Title = "Анализ шифрования", OrderIndex = 4 },
                        new() { Title = "Проверка аутентификации", OrderIndex = 5 },
                        new() { Title = "SQL injection тесты", OrderIndex = 6 },
                        new() { Title = "Отчет с рекомендациями", OrderIndex = 7 }
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
                        new() { Title = "Анализ требований", OrderIndex = 1 },
                        new() { Title = "Выбор инструментов", OrderIndex = 2 },
                        new() { Title = "Setup сервера", OrderIndex = 3 },
                        new() { Title = "CI/CD настройка", OrderIndex = 4 },
                        new() { Title = "Database setup", OrderIndex = 5 },
                        new() { Title = "Безопасность", OrderIndex = 6 },
                        new() { Title = "Мониторинг", OrderIndex = 7 },
                        new() { Title = "Документация", OrderIndex = 8 }
                    }
                },
                new TaskTemplate
                {
                    Name = "Cloud Infrastructure",
                    Description = "Настройка облачной инфраструктуры",
                    Categories = new List<Category>(new[] { catAdmin }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Выбор облачного провайдера", OrderIndex = 1 },
                        new() { Title = "Setup аккаунта", OrderIndex = 2 },
                        new() { Title = "Настройка VPC", OrderIndex = 3 },
                        new() { Title = "Deploy приложения", OrderIndex = 4 },
                        new() { Title = "Database настройка", OrderIndex = 5 },
                        new() { Title = "Load balancing", OrderIndex = 6 },
                        new() { Title = "Auto-scaling", OrderIndex = 7 },
                        new() { Title = "Мониторинг и логирование", OrderIndex = 8 },
                        new() { Title = "Backup & Recovery", OrderIndex = 9 }
                    }
                },
                new TaskTemplate
                {
                    Name = "Server Administration",
                    Description = "Администрирование серверов",
                    Categories = new List<Category>(new[] { catAdmin }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Setup сервера", OrderIndex = 1 },
                        new() { Title = "Установка ОС", OrderIndex = 2 },
                        new() { Title = "Настройка сети", OrderIndex = 3 },
                        new() { Title = "Установка ПО", OrderIndex = 4 },
                        new() { Title = "Настройка безопасности", OrderIndex = 5 },
                        new() { Title = "Firewall конфигурация", OrderIndex = 6 },
                        new() { Title = "Backup стратегия", OrderIndex = 7 },
                        new() { Title = "Monitoring setup", OrderIndex = 8 },
                        new() { Title = "Техническая поддержка", OrderIndex = 9 }
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
                        new() { Title = "Определение KPI", OrderIndex = 1 },
                        new() { Title = "Сбор данных", OrderIndex = 2 },
                        new() { Title = "Очистка данных", OrderIndex = 3 },
                        new() { Title = "Анализ", OrderIndex = 4 },
                        new() { Title = "Визуализация", OrderIndex = 5 },
                        new() { Title = "Выводы и рекомендации", OrderIndex = 6 },
                        new() { Title = "Отчет", OrderIndex = 7 }
                    }
                },
                new TaskTemplate
                {
                    Name = "Business Intelligence",
                    Description = "BI решения и dashboards",
                    Categories = new List<Category>(new[] { catAnalytics }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Анализ бизнес-процессов", OrderIndex = 1 },
                        new() { Title = "Определение метрик", OrderIndex = 2 },
                        new() { Title = "Выбор инструмента BI", OrderIndex = 3 },
                        new() { Title = "Setup подключений", OrderIndex = 4 },
                        new() { Title = "Разработка dashboards", OrderIndex = 5 },
                        new() { Title = "Создание отчетов", OrderIndex = 6 },
                        new() { Title = "Setup автоматизации", OrderIndex = 7 },
                        new() { Title = "Обучение пользователей", OrderIndex = 8 }
                    }
                },
                new TaskTemplate
                {
                    Name = "A/B Testing",
                    Description = "Проведение A/B тестов",
                    Categories = new List<Category>(new[] { catAnalytics }.Where(c => c != null)!),
                    Items = new List<TaskTemplateItem>
                    {
                        new() { Title = "Гипотеза формулировка", OrderIndex = 1 },
                        new() { Title = "Расчет размера выборки", OrderIndex = 2 },
                        new() { Title = "Setup тестирования", OrderIndex = 3 },
                        new() { Title = "Реализация вариантов", OrderIndex = 4 },
                        new() { Title = "Запуск теста", OrderIndex = 5 },
                        new() { Title = "Мониторинг результатов", OrderIndex = 6 },
                        new() { Title = "Статистический анализ", OrderIndex = 7 },
                        new() { Title = "Выводы и рекомендации", OrderIndex = 8 }
                    }
                }
            };

            await context.TaskTemplates.AddRangeAsync(templates);
            await context.SaveChangesAsync();
        }
    }
}
