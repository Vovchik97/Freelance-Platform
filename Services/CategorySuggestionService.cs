using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Services;

public class CategorySuggestionService
{
    private readonly AppDbContext _context;

    
    private static readonly Dictionary<string, string[]> CategoryKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Веб-разработка"] = new[]
        {
            "сайт", "веб", "web", "frontend", "backend", "фронтенд", "бэкенд",
            "html", "css", "javascript", "react", "angular", "vue", "asp.net",
            "django", "laravel", "php", "wordpress", "лендинг", "landing",
            "верстка", "fullstack", "фулстек", "node.js", "typescript"
        },
        ["Мобильная разработка"] = new[]
        {
            "мобильн", "android", "ios", "flutter", "react native", "swift",
            "kotlin", "приложение для телефона", "app", "мобайл", "mobile",
            "xamarin", "maui", "cordova", "ionic"
        },
        ["Дизайн"] = new[]
        {
            "дизайн", "design", "figma", "photoshop", "illustrator", "ui", "ux",
            "макет", "логотип", "logo", "баннер", "banner", "иконк", "график",
            "брендинг", "branding", "прототип", "wireframe", "sketch", "canva"
        },
        ["Маркетинг"] = new[]
        {
            "маркетинг", "marketing", "smm", "реклама", "таргет", "target",
            "продвижение", "контекстная реклама", "google ads", "яндекс директ",
            "instagram", "facebook", "tiktok", "воронка продаж", "email рассылка",
            "лидогенерация", "lead", "пиар", "pr"
        },
        ["Копирайтинг"] = new[]
        {
            "копирайт", "copywriting", "текст", "статья", "контент", "content",
            "рерайт", "rewriting", "блог", "blog", "описание товар", "сценарий",
            "редактирование текст", "корректура", "перевод", "translation"
        },
        ["SEO"] = new[]
        {
            "seo", "поисковая оптимизация", "ключевые слова", "семантическое ядро",
            "ссылочная масса", "backlink", "поисковое продвижение", "выдача",
            "serp", "мета-теги", "meta", "индексация", "google search console",
            "яндекс вебмастер"
        },
        ["Тестирование"] = new[]
        {
            "тестирование", "testing", "qa", "тест", "баг", "bug", "автотест",
            "selenium", "unit test", "ручное тестирование", "нагрузочное тестирование",
            "test case", "тест-кейс", "postman", "quality assurance"
        },
        ["Администрирование"] = new[]
        {
            "администрирование", "devops", "сервер", "server", "linux", "docker",
            "kubernetes", "k8s", "ci/cd", "nginx", "aws", "azure", "облако",
            "cloud", "деплой", "deploy", "инфраструктура", "terraform", "ansible",
            "мониторинг", "monitoring", "бэкап", "backup"
        },
        ["Аналитика"] = new[]
        {
            "аналитик", "analytics", "data science", "данные", "data", "bi",
            "power bi", "tableau", "sql", "python", "машинное обучение",
            "machine learning", "нейросеть", "neural", "статистика", "отчёт",
            "report", "dashboard", "дашборд", "парсинг", "parsing", "excel"
        }
    };

    public CategorySuggestionService(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<int>> SuggestCategoryIdsAsync(string? title, string? description)
    {
        var text = $"{title} {description}".ToLowerInvariant();
        
        var matchedCategoryNames = CategoryKeywords
            .Where(kvp => kvp.Value.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            .Select(kvp => kvp.Key)
            .ToList();

        if (!matchedCategoryNames.Any())
        {
            matchedCategoryNames.Add("Другое");
        }
        
        var suggestedIds = await _context.Categories
            .Where(c => c.IsActive && matchedCategoryNames.Contains(c.Name))
            .Select(c => c.Id)
            .ToListAsync();

        return suggestedIds;
    }
}