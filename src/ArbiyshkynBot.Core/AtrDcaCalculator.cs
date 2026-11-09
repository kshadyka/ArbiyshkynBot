namespace ArbiyshkynBot.Core.Services;

public class AtrDcaCalculator
{
    /// <summary>
    /// Рассчитывает шаг следующего усреднения сетки DCA на основе коэффициента волатильности (ATR).
    /// На спокойных монетах сетка узкая, на штормовых — автоматически расширяется.
    /// </summary>
    public decimal CalculateNextGridStep(decimal currentPrice, decimal atrValue, int dcaStepIndex, decimal basePercentage = 0.015m)
    {
        // Динамический коэффициент расширения сетки в зависимости от индекса шага усреднения
        decimal stepMultiplier = 1.0m + (dcaStepIndex * 0.35m);
        
        // Учитываем текущий ATR (Average True Range)
        decimal volatilityFactor = atrValue / currentPrice;

        // Итоговый процент отступления для следующего ордера усреднения
        decimal effectivePercentage = (basePercentage * stepMultiplier) + (volatilityFactor * 0.5m);

        return currentPrice * (1.0m - effectivePercentage);
    }
}