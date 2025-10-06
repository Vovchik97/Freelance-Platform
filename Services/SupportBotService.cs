namespace FreelancePlatform.Services;

public class SupportBotService
{
    private readonly Dictionary<string, string> _faq = new(StringComparer.OrdinalIgnoreCase)
    {
        {
            "не могу оплатить",
            "Проверьте реквизиты карты и баланс. Если платёж отклонён — попробуйте другую карту или повторите оплату через 5 минут."
        },
        { 
            "ошибка входа", 
            "Попробуйте сбросить пароль через 'Забыли пароль'. Убедитесь, что почта подтверждена." 
        },
        {
            "не приходит письмо",
            "Проверьте папку 'Спам' и лимиты почты. Также можно попробовать указать другой email в профиле."
        },
        {
            "как добавить услугу",
            "Перейдите в 'Мои услуги' → 'Создать' и заполните форму: название, описание, цена. Затем опубликуйте."
        },
        {
            "как оставить отзыв",
            "Оставить отзыв можно на странице проекта после завершения заказа — кнопка 'Оставить отзыв'."
        }
    };

    public readonly List<string> _escalationPhrases = new()
    {
        "не помогло", "не решилось", "хочу с админом", "свяжите с админом", "перекинуть к админу", "надо админ"
    };

    public (string reply, bool escalated) GetReply(string userMessage)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return ("Опишите, пожалуйста, вашу проблему — я постараюсь помочь. Например: \\\"ошибка входа\\\", \\\"не приходит письмо\\\".", false);
        }

        var lower = userMessage.ToLowerInvariant();

        foreach (var kv in _faq)
        {
            if (lower.Contains(kv.Key.ToLowerInvariant()))
            {
                return (kv.Value, false);
            }
        }

        foreach (var phrase in _escalationPhrases)
        {
            if (lower.Contains(phrase))
            {
                return ("Похоже, что нужна помощь специалиста. Сейчас я попрошу администратора подключиться к чату.", true);
            }
        }

        var suggestions = string.Join("\n", _faq.Keys.Select(k => $"- {k}"));
        var replyText = "Я не уверен, что понимаю проблему. Часто помогают следующие запросы:\n" +
                        $"{suggestions}\n\nЕсли ничего не помогает - напишите \"не помогло\" или \"подключите админа\", и я вас переключу на живого администратора.";
        return (replyText, false);
    }
}