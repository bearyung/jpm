namespace JinPingMei.Game.Hosting;

public interface ITelnetServerMetrics
{
    void RecordAccepted();
    void RecordRejected();
    void RecordInactivityTimeout();
    void RecordLifetimeLimit();
}
