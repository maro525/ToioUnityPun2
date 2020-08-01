using System;
using System.Collections.Generic;
using UnityEngine;

namespace toio
{
    public class BLEMobilePeripheral : BLEPeripheralInterface
    {
        public string[] serviceUUIDs { get; private set; }
        public string device_address { get; private set; }
        public string device_name { get; private set; }
        public float rssi { get; private set; }
        public bool isConnected { get; private set; }

        // BluetoothLEHardwareInterface.ConnectToPeripheralに登録出来る切断コールバックが1つの関数のみのため、static辞書でperipheralを管理
        private static Dictionary<string, BLEMobilePeripheral> peripheralDatabase = new Dictionary<string, BLEMobilePeripheral>();
        private TCallbackProvider<BLEPeripheralInterface> callback;

        public BLEMobilePeripheral(string[] serviceUUIDs, string device_address, string device_name, float rssi)
        {
            device_address = device_address.ToUpper();
#if !RELEASE
            if (peripheralDatabase.ContainsKey(device_address) && peripheralDatabase[device_address].isConnected)
            {
                Debug.LogWarning("有効なPeripheralが既に存在しています。");
            }
#endif

            this.serviceUUIDs = serviceUUIDs;
            this.device_address = device_address;
            this.device_name = device_name;
            this.rssi = rssi;
            this.callback = new TCallbackProvider<BLEPeripheralInterface>();
            this.isConnected = false;

            // staticな辞書に追加
            if (!peripheralDatabase.ContainsKey(device_address))
            {
                peripheralDatabase.Add(this.device_address, this);
            }
        }

        /// <summary>
        /// peripheralに接続
        /// [peripheral:1 - characteristic:多]の関係なので、characteristicActionが複数回呼び出される
        /// </summary>
        public void Connect(Action<BLECharacteristicInterface> characteristicAction)
        {
            BluetoothLEHardwareInterface.ConnectToPeripheral(this.device_address, OnConnected, null, (address, serviceUUID, characteristicUUID) =>
            {
                //Debug.Log("address=" + address + ". uuid=" + serviceUUID + ". chara=" + characteristicUUID);
                characteristicAction(new BLEMobileCharacteristic(this.device_address, serviceUUID, characteristicUUID));
            }, OnDisconnected);
        }

        /// <summary>
        /// 接続/切断コールバックを登録
        /// </summary>
        public void AddConnectionListener(string key, Action<BLEPeripheralInterface> action)
        {
            this.callback.AddListener(key, action);
        }

        /// <summary>
        /// 接続/切断コールバックを解除
        /// </summary>
        public void RemoveConnectionListener(string key)
        {
            this.callback.RemoveListener(key);
        }

        /// <summary>
        /// 接続/切断コールバックを呼び出し
        /// </summary>
        public void ConnectionNotify(BLEPeripheralInterface peri)
        {
            this.callback.Notify(peri);
        }

        /// <summary>
        /// 通信コールバック(接続時)
        /// </summary>
        private static void OnConnected(string device_address)
        {
            device_address = device_address.ToUpper();
            if (peripheralDatabase.ContainsKey(device_address))
            {
                var instance = peripheralDatabase[device_address];
                instance.isConnected = true;
                instance.ConnectionNotify(instance);
            }
        }

        /// <summary>
        /// 通信コールバック(切断時)
        /// </summary>
        private static void OnDisconnected(string device_address)
        {
            device_address = device_address.ToUpper();

            var instance = peripheralDatabase[device_address];
            instance.isConnected = false;
            instance.ConnectionNotify(instance);
        }
    }
}