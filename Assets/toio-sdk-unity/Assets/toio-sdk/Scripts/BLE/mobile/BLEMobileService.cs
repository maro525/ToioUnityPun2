using System;
using UnityEngine;
using UniRx.Async;

namespace toio
{
    public class BLEMobileService : BLEServiceInterface
    {
        public void RequestDevice(Action<BLEDeviceInterface> action)
        {
            var devicescript = BluetoothLEHardwareInterface.Initialize(true, false, () =>
            {
                action(new BLEMobileDevice());
            }, (error) =>
            {
#if !RELEASE
            Debug.LogErrorFormat("[BLEMobileService.requestDevice]error = {0}", error);
#endif
        });
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

        public void DisconnectAll()
        {
            BluetoothLEHardwareInterface.DisconnectAll();
            BluetoothLEHardwareInterface.DeInitialize(null);
        }
    }
}