namespace FreelancePlatform.Models;

public enum ServiceStatus
{
    Available, // Услуга выставлена
    Requested, // Клиент заказал, ждёт ответа
    InProgress,
    Declined,
    Completed
}