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
                new Service { Title = "Создание лендинга", Description = "Сделаю современный одностраничник", Price = 1000, FreelancerId = "freelancer1", Status = ServiceStatus.Available },
                new Service { Title = "UX/UI дизайн", Description = "Прототипы и дизайн для веба", Price = 1200, FreelancerId = "freelancer2", Status = ServiceStatus.Available },
                new Service { Title = "Frontend разработка", Description = "Верстаю React / Vue интерфейсы", Price = 1500, FreelancerId = "freelancer3", Status = ServiceStatus.Available },
                new Service { Title = "Backend API", Description = "Разработка REST API на .NET", Price = 2000, FreelancerId = "freelancer4", Status = ServiceStatus.Available },
                new Service { Title = "Мобильная разработка", Description = "Android и iOS приложения", Price = 2500, FreelancerId = "freelancer5", Status = ServiceStatus.Available },
                new Service { Title = "SEO аудит", Description = "Анализ и рекомендации по SEO", Price = 800, FreelancerId = "freelancer1", Status = ServiceStatus.Available },
                new Service { Title = "Копирайтинг", Description = "Продающие тексты и статьи", Price = 700, FreelancerId = "freelancer2", Status = ServiceStatus.Available },
                new Service { Title = "Создание Telegram-бота", Description = "Бот под ваш сценарий", Price = 1100, FreelancerId = "freelancer3", Status = ServiceStatus.Available },
                new Service { Title = "Обработка данных", Description = "Python-скрипты, парсинг", Price = 1300, FreelancerId = "freelancer4", Status = ServiceStatus.Available },
                new Service { Title = "Анимация и видео", Description = "Моушн-дизайн и ролики", Price = 1600, FreelancerId = "freelancer5", Status = ServiceStatus.Available },
            };

            await context.Services.AddRangeAsync(services);
            await context.SaveChangesAsync();
        }
    }
}
