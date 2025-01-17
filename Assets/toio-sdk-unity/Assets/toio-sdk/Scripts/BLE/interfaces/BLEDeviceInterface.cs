using System;
using UniRx.Async;

namespace toio
{
    public interface BLEDeviceInterface
    {
        void Scan(String[] serviceUUIDs, bool rssiOnly, Action<BLEPeripheralInterface> action);
        void StopScan();
        UniTask Disconnect(Action action);
        UniTask Enable(bool enable, Action action);
    }
}