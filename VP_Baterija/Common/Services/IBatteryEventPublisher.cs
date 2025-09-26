using Common.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Services
{
    public interface IBatteryEventPublisher
    {
        event EventHandler<TransferEventArgs> OnTransferStarted;
        event EventHandler<SampleEventArgs> OnSampleReceived;
        event EventHandler<TransferEventArgs> OnTransferCompleted;
        event EventHandler<WarningEventArgs> OnWarningRaised;
    }
}
