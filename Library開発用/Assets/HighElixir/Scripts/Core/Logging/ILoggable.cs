namespace HighElixir.Loggings
{
    public interface ILoggable
    {
        ILogger Logger { get; set; }
        uint RequiredLoggerLevel { get; set; }
    }
}