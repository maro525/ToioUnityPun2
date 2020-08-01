using System;
using UniRx.Async;

namespace toio
{
    public class BLEMobileDevice : BLEDeviceInterface
    {
        public void Scan(String[] serviceUUIDs, bool rssiOnly, Action<BLEPeripheralInterface> action)
        {
            BluetoothLEHardwareInterface.ScanForPeripheralsWithServices(serviceUUIDs, null, (device_address, device_name, rssi, bytes) =>
            {
                action(new BLEMobilePeripheral(serviceUUIDs, device_address, device_name, rssi));
                // DeInitializeでperipheralListをクリアする前提で、clearPeripheralList=false
            }, rssiOnly, clearPeripheralList:false);
        }

        public void StopScan()
        {
            BluetoothLEHardwareInterface.StopScan();
        }

        public async UniTask Disconnect(Action action)
        {
            BluetoothLEHardwareInterface.DisconnectAll();
            BluetoothLEHardwareInterface.DeInitialize(null);
            await UniTask.Delay(500);
            action.Invoke();
        }

        public async UniTask Enable(bool enable, Action action)
        {
            BluetoothLEHardwareInterface.BluetoothEnable(enable);
#if !UNITY_EDITOR && UNITY_ANDROID
            await UniTask.Delay(1000);
#else
            await UniTask.Delay(1);
#endif
            action.Invoke();
        }
    }
}