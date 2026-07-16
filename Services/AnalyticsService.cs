namespace FreelancePlatform.Services;

/// <summary>
/// Предоставляет методы аналитики и прогнозирования показателей платформы,
/// включая прогнозирование, поиск аномалий, анализ удержания пользователей
/// и расчет интегральных показателей эффективности.
/// </summary>
public class AnalyticsService
{
    /// <summary>
    /// Выполняет экспоненциальное сглаживание временного ряда и строит прогноз.
    /// </summary>
    /// <param name="data">Исходный временной ряд.</param>
    /// <param name="forecastSteps">Количество прогнозируемых периодов.</param>
    /// <param name="alpha">Коэффициент сглаживания.</param>
    /// <returns>Массив прогнозируемых значений.</returns>
    public decimal[] ExponentialSmoothing(decimal[] data, int forecastSteps, double alpha = 0.3)
    {
        if (data.Length == 0)
        {
            return new decimal[forecastSteps];
        }

        var smoothed = new decimal[data.Length];
        smoothed[0] = data[0];

        // Применяем формулу: S[t] = α × Y[t] + (1-α) × S[t-1]
        for (int i = 1; i < data.Length; i++)
        {
            smoothed[i] = (decimal)(alpha * (double)data[i]
                                    + (1 - alpha) * (double)smoothed[i - 1]);
        }

        var forecast = new decimal[forecastSteps];
        var lastSmoothed = smoothed[^1];

        decimal trend = 0;
        if (data.Length >= 3)
        {
            trend = (smoothed[^1] - smoothed[^3]) / 2;
        }

        for (int i = 0; i < forecastSteps; i++)
        {
            forecast[i] = lastSmoothed + trend * (i + 1);
            if (forecast[i] < 0)
            {
                forecast[i] = 0;
            }
        }

        return forecast;
    }
    
    /// <summary>
    /// Выполняет поиск аномальных значений методом Z-score.
    /// </summary>
    /// <param name="data">Исходные значения.</param>
    /// <param name="labels">Подписи значений.</param>
    /// <param name="threshold">Пороговое значение Z-score.</param>
    /// <returns>Коллекция найденных аномалий.</returns>
    public List<AnomalyPoint> DetectAnomalies(decimal[] data, string[] labels, double threshold = 2.0)
    {
        var anomalies = new List<AnomalyPoint>();

        if (data.Length < 3)
        {
            return anomalies;
        }

        var mean = data.Average(x => (double)x);

        var variance = data.Average(x => Math.Pow((double)x - mean, 2));
        var stdDev = Math.Sqrt(variance);

        if (stdDev < 0.001)
        {
            return anomalies;
        }

        for (int i = 0; i < data.Length; i++)
        {
            var zScore = Math.Abs(((double)data[i] - mean) / stdDev);

            if (zScore > threshold)
            {
                anomalies.Add(new AnomalyPoint
                {
                    Label = labels[i],
                    Value = data[i],
                    ZScore = Math.Round(zScore, 2),
                    IsSpike = (double)data[i] > mean,
                    Severity = zScore > 3.0 ? "Критическая" : "Умеренная"
                });
            }
        }

        return anomalies;
    }
    
    /// <summary>
    /// Рассчитывает показатели удержания и оттока пользователей.
    /// </summary>
    /// <param name="registrationsByMonth">Количество регистраций по месяцам.</param>
    /// <param name="activeByMonth">Количество активных пользователей по месяцам.</param>
    /// <returns>Результат расчета удержания пользователей.</returns>
    public RetentionData CalculateRetention(Dictionary<int, int> registrationsByMonth,
        Dictionary<int, int> activeByMonth)
    {
        var retentionRates = new List<double>();
        var churnRates = new List<double>();

        var months = registrationsByMonth.Keys.OrderBy(x => x).ToList();

        foreach (var month in months)
        {
            var registered = registrationsByMonth.GetValueOrDefault(month, 0);
            var active = activeByMonth.GetValueOrDefault(month, 0);

            if (registered > 0)
            {
                var retention = Math.Min(100.0, (double)active / registered * 100);
                retentionRates.Add(Math.Round(retention, 1));
                churnRates.Add(Math.Round(100 - retention, 1));
            }
            else
            {
                retentionRates.Add(0);
                churnRates.Add(0);
            }
        }

        var avgRetention = retentionRates.Any() ? retentionRates.Average() : 0;

        return new RetentionData
        {
            RetentionRates = retentionRates.ToArray(),
            ChurnRates = churnRates.ToArray(),
            AverageRetention = Math.Round(avgRetention, 1)
        };
    }
    
    /// <summary>
    /// Выполняет прогноз роста пользовательской базы с использованием
    /// нормального распределения приростов.
    /// </summary>
    /// <param name="usersByMonth">Количество пользователей по месяцам.</param>
    /// <param name="targetUsers">Целевое количество пользователей.</param>
    /// <returns>Результаты анализа роста пользователей.</returns>
    public GaussianGrowthAnalysis AnalyzeUserGrowthWithGaussian(int[] usersByMonth, int targetUsers = 500)
    {
        if (usersByMonth == null || usersByMonth.Length < 2)
        {
            return new GaussianGrowthAnalysis
            {
                CurrentUsers = usersByMonth?.Length == 1 ? usersByMonth[0] : 0,
                TargetUsers = targetUsers,
                NeededGrowth = targetUsers,
                ForecastBase = new int[3],
                ForecastHigh95 = new int[3],
                ForecastLow95 = new int[3],
                ForecastOptimist = new int[3],
                ForecastPessimist = new int[3],
                MonthAssessments = new List<MonthGrowthAssessment>(),
                TrendDirection = "➡️ Нет данных",
                AccelerationText = "Нет данных",
                MethodDescription = "Недостаточно данных (нужно минимум 2 месяца)"
            };
        }
        
        int n = usersByMonth.Length;
        int currentUsers = usersByMonth[^1];
        
        // 1. Вычисляем приросты - это наши наблюдения
        // Именно они моделируются нормальным распределением
        // а не абсолютные значения
        var changes = new double[n - 1];
        for (int i = 1; i < n; i++)
        {
            changes[i - 1] = usersByMonth[i] - usersByMonth[i - 1];
        }
        
        // 2. Параметры нормального распределения N(μ, σ²)
        //    μ — математическое ожидание прироста
        //    σ — стандартное отклонение (мера неопределённости)
        double mu = changes.Average();
        double sigma = StandardDeviation(changes);
        
        // 3. Прогноз на следующие месяцы
        //    Каждый прогнозируемый месяц: предыдущее значение + μ
        //    Неопределённость растёт с горизонтом: σ * sqrt(шаг)
        //    (дисперсии независимых шагов суммируются)
        int forecastSteps = 3;
        var forecastBase = new int[forecastSteps];
        var forecastHigh95 = new int[forecastSteps]; // μ + 2σ√t
        var forecastLow95 = new int[forecastSteps]; // μ - 2σ√t
        var forecastOptimist = new int[forecastSteps]; // μ + σ√t
        var forecastPessimist = new int[forecastSteps]; // μ - σ√t

        for (int i = 0; i < forecastSteps; i++)
        {
            // Неопределённость растёт пропорционально √t
            // Это стандартный результат для случайного блуждания
            double uncertainty = sigma * Math.Sqrt(i + 1);
            double baseValue = currentUsers + mu * (i + 1);

            forecastBase[i] = (int)Math.Max(0, Math.Round(baseValue));
            forecastHigh95[i] = (int)Math.Max(0, Math.Round(baseValue + 2 * uncertainty));
            forecastLow95[i] = (int)Math.Max(0, Math.Round(baseValue - 2 * uncertainty));
            forecastOptimist[i] = (int)Math.Max(0, Math.Round(baseValue + uncertainty));
            forecastPessimist[i] = (int)Math.Max(0, Math.Round(baseValue - uncertainty));
        }
        
        // 4. Оцениваем каждый прошедший месяц по Z-score
        //    |Z| <= 1 → норма (68% всех месяцев)
        //    |Z| <= 2 → умеренное отклонение (95%)
        //    |Z| >  2 → аномалия (выход за 95% интервал)
        var monthAssessments = new List<MonthGrowthAssessment>();
        for (int i = 0; i < changes.Length; i++)
        {
            double zScore = sigma > 0 ? (changes[i] - mu) / sigma : 0;

            string zone = Math.Abs(zScore) <= 1 ? "Норма (σ)" :
                Math.Abs(zScore) <= 2 ? "Умеренно (2σ)" : "Аномалия (>2σ)";
            
            monthAssessments.Add(new MonthGrowthAssessment
            {
                MonthIndex = i + 1,
                Change = (int)changes[i],
                ZScore = Math.Round(zScore, 2),
                Zone = zone,
                IsAnomaly = Math.Abs(zScore) > 2,
                IsSpike = zScore > 2,
                IsDrop = zScore < -2
            });
        }
        
        // 5. Сколько месяцев до целевого числа пользователей
        //    Базовый сценарий: растём по μ каждый месяц
        //    Оптимист: растём по μ + σ
        //    Пессимист: растём по μ - σ
        int neededGrowth = targetUsers - currentUsers;

        int monthsToTargetBase = mu > 0
            ? (int)Math.Ceiling(neededGrowth / mu)
            : -1;
        
        int monthsToTargetOptimistic = (mu + sigma) > 0
            ? (int)Math.Ceiling(neededGrowth / (mu + sigma))
            : -1;
        
        int monthsToTargetPessimistic = (mu - sigma) > 0
            ? (int)Math.Ceiling(neededGrowth / (mu - sigma))
            : -1;
        
        // 6. До удвоения аудитории
        int monthsToDouble = mu > 0
            ? (int)Math.Ceiling((double)currentUsers / mu)
            : -1;
        
        // 7. Тренд ускорения: сравниваем среднее первой и второй половины
        bool isAccelerating = false;
        if (changes.Length >= 2)
        {
            double firstHalfAvg = changes.Take(changes.Length / 2).Average();
            double secondHalfAvg = changes.Skip(changes.Length / 2).Average();
            isAccelerating = secondHalfAvg > firstHalfAvg;
        }
        
        int currentMonthGrowth = n >= 2 ? usersByMonth[^1] - usersByMonth[^2] : 0;
        
        // 8. Вероятность достижения цели за 1 месяц
        //    P(прирост >= neededGrowth) = 1 - Φ((neededGrowth - μ) / σ)
        double probOneMonth = sigma > 0
            ? 1.0 - GaussianCdf(neededGrowth, mu, sigma)
            : (mu >= neededGrowth ? 1.0 : 0.0);

        return new GaussianGrowthAnalysis
        {
            // --- Параметры распределения ---
            MeanGrowth = Math.Round(mu, 1),
            SigmaGrowth = Math.Round(sigma, 1),

            // --- Границы нормального роста ---
            Lower1Sigma = Math.Round(mu - sigma, 1),
            Upper1Sigma = Math.Round(mu + sigma, 1),
            Lower2Sigma = Math.Round(mu - 2 * sigma, 1),
            Upper2Sigma = Math.Round(mu + 2 * sigma, 1),

            // --- Прогноз ---
            CurrentUsers = currentUsers,
            ForecastBase = forecastBase,
            ForecastHigh95 = forecastHigh95,
            ForecastLow95 = forecastLow95,
            ForecastOptimist = forecastOptimist,
            ForecastPessimist = forecastPessimist,

            // --- Оценка месяцев ---
            MonthAssessments = monthAssessments,
            AnomalyCount = monthAssessments.Count(x => x.IsAnomaly),

            // --- Целевые показатели ---
            TargetUsers = targetUsers,
            NeededGrowth = neededGrowth,
            MonthsToTargetBase = monthsToTargetBase > 0 ? monthsToTargetBase : -1,
            MonthsToTargetOptimistic = monthsToTargetOptimistic > 0 ? monthsToTargetOptimistic : -1,
            MonthsToTargetPessimistic = monthsToTargetPessimistic > 0 ? monthsToTargetPessimistic : -1,
            MonthsToDouble = monthsToDouble > 0 ? monthsToDouble : -1,
            DoubleTarget = currentUsers * 2,

            // --- Тренд ---
            CurrentMonthGrowth = currentMonthGrowth,
            IsAccelerating = isAccelerating,
            AccelerationText = isAccelerating ? "Ускоряется ⚡" : "Замедляется 🐢",
            TrendDirection = currentMonthGrowth > 0 ? "📈 Растёт" :
                currentMonthGrowth < 0 ? "📉 Падает" : "➡️ Стабильно",
            Volatility = Math.Round(sigma, 1),

            // --- Вероятность ---
            ProbabilityOneMonth = Math.Round(probOneMonth * 100, 1),

            MethodDescription = "Нормальное распределение приростов N(μ, σ²). " +
                                "Доверительные интервалы: ±σ (68%), ±2σ (95%). " +
                                "Неопределённость прогноза растёт как σ·√t."
        };
    }
    
    /// <summary>
    /// Рассчитывает интегральный индекс здоровья платформы
    /// на основе ключевых бизнес-показателей.
    /// </summary>
    /// <param name="input">Исходные аналитические данные.</param>
    /// <returns>Индекс состояния платформы.</returns>
    public PlatformHealthIndex CalculateHealthIndex(HealthInputData input)
    {
        // --- Конверсия проектов (% завершённых от всех) ---
        double conversionScore = input.TotalProjects > 0
            ? Math.Min(100, (double)input.CompletedProjects / input.TotalProjects * 100)
            : 0;
        
        // --- Рост оборота (% изменение за последний месяц) ---
        double revenueGrowthScore = 50;
        if (input.PreviousMonthRevenue > 0)
        {
            var growthPct = (double)(input.CurrentMonthRevenue - input.PreviousMonthRevenue) / (double)input.PreviousMonthRevenue * 100;
            revenueGrowthScore = Math.Clamp(50 + growthPct * 2.5, 0, 100);
        }
        
        // --- Активность пользователей (заявки + заказы на пользователя) ---
        double activityScore = 0;
        if (input.TotalUsers > 0)
        {
            var activityPerUser = (double)(input.TotalBids + input.TotalOrders) / input.TotalUsers;
            activityScore = Math.Min(100, activityPerUser / 5.0 * 100);
        }
        
        // --- Retention (удержание) ---
        double retentionScore = input.RetentionRate;
        
        // --- Итоговый индекс (взвешенная сумма) ---
        double healthIndex = conversionScore * 0.30 
                             + revenueGrowthScore * 0.30 
                             + activityScore * 0.20
                             + retentionScore * 0.20;

        healthIndex = Math.Round(healthIndex, 1);

        return new PlatformHealthIndex
        {
            Score = healthIndex,
            Status = healthIndex >= 75 ? "Отлично" :
                healthIndex >= 50 ? "Хорошо" :
                healthIndex >= 25 ? "Требует внимания" : "Критично",
            StatusColor = healthIndex >= 75 ? "#28a745" :
                healthIndex >= 50 ? "#ffc107" :
                healthIndex >= 25 ? "#fd7e14" : "#dc3545",
            ConversionScore = Math.Round(conversionScore, 1),
            RevenueGrowthScore = Math.Round(revenueGrowthScore, 1),
            ActivityScore = Math.Round(activityScore, 1),
            RetentionScore = Math.Round(retentionScore, 1)
        };
    }
    
    /// <summary>
    /// Вычисляет стандартное отклонение для набора значений.
    /// </summary>
    /// <param name="data">Массив числовых значений.</param>
    /// <returns>Стандартное отклонение выборки.</returns>
    private double StandardDeviation(double[] data)
    {
        if (data.Length < 2)
        {
            return 0;
        }

        double mean = data.Average();
        double variance = data.Average(x => Math.Pow(x - mean, 2));
        return Math.Sqrt(variance);
    }
    
    /// <summary>
    /// Вычисляет значение функции распределения (CDF) нормального распределения.
    /// </summary>
    /// <param name="x">Исследуемое значение.</param>
    /// <param name="mu">Математическое ожидание распределения.</param>
    /// <param name="sigma">Стандартное отклонение распределения.</param>
    /// <returns>Вероятность того, что случайная величина не превышает указанное значение.</returns>
    private double GaussianCdf(double x, double mu, double sigma)
    {
        if (sigma < 1e-10)
        {
            return x >= mu ? 1.0 : 0.0;
        }
        
        double z = (x - mu) / (sigma * Math.Sqrt(2));
        return 0.5 * (1.0 + Erf(z));
    }

    /// <summary>
    /// Вычисляет значение функции ошибок (Erf),
    /// используемой при расчете функции нормального распределения.
    /// </summary>
    /// <param name="x">Аргумент функции.</param>
    /// <returns>Значение функции ошибок.</returns>
    private double Erf(double x)
    {
        double t = 1.0 / (1.0 + 0.3275911 * Math.Abs(x));
        double y = 1.0 - (((((1.061405429 * t)
                             - 1.453152027) * t
                            + 1.421413741) * t
                           - 0.284496736) * t
                          + 0.254829592) * t * Math.Exp(-x * x);
        return x >= 0 ? y : -y;

    }
}

/// <summary>
/// Представляет информацию об обнаруженной аномалии.
/// </summary>
public class AnomalyPoint
{
    public string Label { get; set; } = "";
    public decimal Value { get; set; }
    public double ZScore { get; set; }
    public bool IsSpike { get; set; }
    public string Severity { get; set; } = "";
}

/// <summary>
/// Содержит результаты анализа удержания пользователей.
/// </summary>
public class RetentionData
{
    public double[] RetentionRates { get; set; } = [];
    public double[] ChurnRates { get; set; } = [];
    public double AverageRetention { get; set; }
}

/// <summary>
/// Содержит результаты анализа роста пользовательской базы.
/// </summary>
public class GaussianGrowthAnalysis
{
    // --- Параметры нормального распределения приростов ---
    public double MeanGrowth { get; set; } // μ — средний прирост
    public double SigmaGrowth { get; set; } // σ — стандартный отклонение
    
    // --- Границы нормального роста ---
    public double Lower1Sigma { get; set; } // μ - σ
    public double Upper1Sigma { get; set; } // μ + σ
    public double Lower2Sigma { get; set; } // μ - 2σ
    public double Upper2Sigma { get; set; } // μ + 2σ
    
    // --- Прогноз на 3 месяца ---
    public int[] ForecastBase { get; set; } = []; // μ на каждый шаг
    public int[] ForecastHigh95 { get; set; } = []; // μ + 2σ√t
    public int[] ForecastLow95 { get; set; } = []; // μ - 2σ√t
    public int[] ForecastOptimist { get; set; } = []; // μ + σ√t
    public int[] ForecastPessimist { get; set; } = []; // μ - σ√t
    
    // --- Оценка прошедших месяцев ---
    public List<MonthGrowthAssessment> MonthAssessments { get; set; } = new();
    public int AnomalyCount { get; set; }
    
    // --- Текущее состояние ---
    public int CurrentUsers { get; set; }
    public int CurrentMonthGrowth { get; set; }
    public string TrendDirection { get; set; } = "";
    public bool IsAccelerating { get; set; }
    public string AccelerationText { get; set; } = "";
    public double Volatility { get; set; }
    
    // --- Целевые показатели ---
    public int TargetUsers { get; set; }
    public int NeededGrowth { get; set; }
    public int MonthsToTargetBase { get; set; }
    public int MonthsToTargetOptimistic { get; set; }
    public int MonthsToTargetPessimistic { get; set; }
    public int MonthsToDouble { get; set; }
    public int DoubleTarget { get; set; }
    
    // --- Вероятность ---
    public double ProbabilityOneMonth { get; set; }
    
    public string MethodDescription { get; set; } = "";
}

/// <summary>
/// Представляет оценку изменения пользовательской базы за отдельный период.
/// </summary>
public class MonthGrowthAssessment
{
    public int MonthIndex { get; set; }
    public int Change { get; set; }
    public double ZScore { get; set; }
    public string Zone { get; set; } = "";
    public bool IsAnomaly { get; set; }
    public bool IsSpike { get; set; }
    public bool IsDrop { get; set; }
}

/// <summary>
/// Содержит входные данные для расчета индекса здоровья платформы.
/// </summary>
public class HealthInputData
{
    public int TotalProjects { get; set; }
    public int CompletedProjects { get; set; }
    public decimal CurrentMonthRevenue { get; set; }
    public decimal PreviousMonthRevenue { get; set; }
    public int TotalUsers { get; set; }
    public int TotalBids { get; set; }
    public int TotalOrders { get; set; }
    public double RetentionRate { get; set; }
}

/// <summary>
/// Представляет рассчитанный индекс состояния платформы.
/// </summary>
public class PlatformHealthIndex
{
    public double Score { get; set; }
    public string Status { get; set; } = "";
    public string StatusColor { get; set; } = "";
    public double ConversionScore { get; set; }
    public double RevenueGrowthScore { get; set; }
    public double ActivityScore { get; set; }
    public double RetentionScore { get; set; }
}