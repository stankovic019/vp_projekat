using Common.Events;
using System;

namespace Common.Services
{
    public interface IBatteryEventPublisher
    {
        event EventHandler<TransferEventArgs> OnTransferStarted;
        event EventHandler<SampleEventArgs> OnSampleReceived;
        event EventHandler<TransferEventArgs> OnTransferCompleted;
        event EventHandler<WarningEventArgs> OnWarningRaised;
        event EventHandler<VoltageSpikeEventArgs> OnVoltageSpikeRaised;
        event EventHandler<ImpedanceJumpEventArgs> OnImpedanceJumpRaised;
        event EventHandler<OutOfBandWarningEventArgs> OnOutOfBandWarningRaised;

    }
}
