namespace FreelancePlatform.Services;

public class AnalyticsService
{
    // ============================================================
    // 1. ЭКСПОНЕНЦИАЛЬНОЕ СГЛАЖИВАНИЕ (прогноз оборота)
    // alpha = 0.3 означает: 30% вес новых данных, 70% история
    // Чем меньше alpha — тем более "инертный" прогноз
    // ============================================================
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
    
    // ============================================================
    // 2. Z-SCORE АНОМАЛИЙ
    // Z = (значение - среднее) / стандартное_отклонение
    // |Z| > 2.0 = подозрительно (выходит за 95% доверительный интервал)
    // |Z| > 3.0 = аномалия (выходит за 99.7%)
    // ============================================================
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
    
    // ============================================================
    // 3. RETENTION RATE (удержание пользователей)
    // Считает % пользователей каждой когорты которые вернулись
    // Когорта = группа пользователей зарегистрировавшихся в одном месяце
    // ============================================================
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
    
    // ============================================================
    // 4. ЛИНЕЙНАЯ РЕГРЕССИЯ
    // Y = a + b×X
    // b = (N×ΣXY - ΣX×ΣY) / (N×ΣX² - (ΣX)²)
    // a = (ΣY - b×ΣX) / N
    // ============================================================
    public RegressionResult LinearRegression(double[] yValues)
    {
        int n = yValues.Length;
        if (n < 2)
        {
            return new RegressionResult();
        }

        var xValues = Enumerable.Range(0, n).Select(x => (double)x).ToArray();
        
        double sumX = xValues.Sum();
        double sumY = yValues.Sum();
        double sumXY = xValues.Zip(yValues, (x, y) => x * y).Sum();
        double sumX2 = xValues.Sum(x => x * x);
        
        double b = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
        double a = (sumY - b * sumX) / n;
        
        // R² — коэффициент детерминации (насколько хорошо регрессия описывает данные)
        // R² = 1 означает идеальное соответствие
        // R² = 0 означает регрессия бесполезна
        var meanY = sumY / n;
        var ssTot = yValues.Sum(y => Math.Pow(y - meanY, 2));
        var ssRes = xValues.Zip(yValues, (x, y) => Math.Pow(y - (a + b * x), 2)).Sum();
        var r2 = ssTot > 0 ? 1 - ssRes / ssTot : 0;

        var forecast = new double[3];
        for (int i = 0; i < 3; i++)
        {
            forecast[i] = Math.Max(0, a + b * (n + i));
        }

        var currentValue = yValues[^1];
        var targetValue = currentValue * 2;
        int monthsToDouble = -1;
        if (b > 0)
        {
            monthsToDouble = (int)Math.Ceiling((targetValue - a) / b) - n + 1;
        }

        return new RegressionResult
        {
            Slope = Math.Round(b, 2),
            Intercept = Math.Round(a, 2),
            R2 = Math.Round(r2, 3),
            Forecast = forecast.Select(x => Math.Round(x, 0)).ToArray(),
            MonthsToDouble = monthsToDouble > 0 ? monthsToDouble : -1,
            TrendDirection = b > 0.5 ? "Растёт" : b < -0.5 ? "Падает" : "Стабильно"
        };
    }
    
    // ============================================================
    // 5. ИНДЕКС ЗДОРОВЬЯ ПЛАТФОРМЫ
    // Взвешенная сумма нормализованных метрик
    // Каждая метрика нормализуется в диапазон 0-100
    // ============================================================
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
        double retenrionScore = input.RetentionRate;
        
        // --- Итоговый индекс (взвешенная сумма) ---
        double healthIndex = conversionScore * 0.30 + 
                             revenueGrowthScore * 0.30 + 
                             activityScore * 0.20 +
                             retenrionScore * 0.20;

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
            RetentionScore = Math.Round(retenrionScore, 1)
        };
    }
}

public class AnomalyPoint
{
    public string Label { get; set; } = "";
    public decimal Value { get; set; }
    public double ZScore { get; set; }
    public bool IsSpike { get; set; }
    public string Severity { get; set; } = "";
}

public class RetentionData
{
    public double[] RetentionRates { get; set; } = [];
    public double[] ChurnRates { get; set; } = [];
    public double AverageRetention { get; set; }
}

public class RegressionResult
{
    public double Slope { get; set; }
    public double Intercept { get; set; }
    public double R2 { get; set; }
    public double[] Forecast { get; set; } = [];
    public int MonthsToDouble { get; set; }
    public string TrendDirection { get; set; } = "";
}

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