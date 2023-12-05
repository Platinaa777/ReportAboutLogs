namespace ReportApi;

/// <summary>
/// Информация об отчете
/// </summary>
public class ReportInfo
{
    /// <summary>
    /// Первый лог сервиса
    /// </summary>
    public DateTime DateOfFirstLog { get; set; } = DateTime.MaxValue;
    /// <summary>
    /// Последний лог сервиса
    /// </summary>
    public DateTime DateOfLastLog { get; set; } = DateTime.MinValue;
    /// <summary>
    /// Все Severity и их количество для каждого типа
    /// </summary>
    public Dictionary<string, ulong> Severities { get; set; } = new Dictionary<string, ulong>();
    /// <summary>
    /// Все Severity и их процентное соотношение для каждого типа
    /// </summary>
    public Dictionary<string, double> PercentageOfEachSeverity { get; set; } = new Dictionary<string, double>();
    /// <summary>
    /// Все Category и их количество для каждого типа
    /// </summary>
    public Dictionary<string, ulong> Categories { get; set; } = new Dictionary<string, ulong>();
    /// <summary>
    /// Все Category и их процентное соотношение для каждого типа
    /// </summary>
    public Dictionary<string, double> PercentageOfEachCategory { get; set; } = new Dictionary<string, double>();
    /// <summary>
    ///  Количество ротаций логов одного сервиса
    /// </summary>
    public ulong CountRotation { get; set; } = 0;
}