using System;
using UniRx.Async;

namespace toio
{
    public interface BLEServiceInterface
    {
        void RequestDevice(Action<BLEDeviceInterface> action);
        void DisconnectAll();
        UniTask Enable(bool enable, Action action);
    }
}