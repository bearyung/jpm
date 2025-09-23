using System;

namespace JinPingMei.Game.Hosting;

public interface ITelnetServerMetrics
{
    void RecordAccepted();
    void RecordSessionEnded(bool faulted);
    void RecordRejected();
    void RecordInactivityTimeout();
    void RecordLifetimeLimit();
    void RecordCommand(TimeSpan duration, bool faulted);
}
