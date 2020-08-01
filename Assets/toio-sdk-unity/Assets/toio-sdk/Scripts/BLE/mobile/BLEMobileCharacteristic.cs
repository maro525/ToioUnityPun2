using System;

namespace toio
{
    public class BLEMobileCharacteristic : BLECharacteristicInterface
    {
        public string deviceAddress { get; private set; }
        public string serviceUUID { get; private set; }
        public string characteristicUUID { get; private set; }

        public BLEMobileCharacteristic(string deviceAddress, string serviceUUID, string characteristicUUID)
        {
            this.deviceAddress = deviceAddress;
            this.serviceUUID = serviceUUID.ToUpper();
            this.characteristicUUID = characteristicUUID.ToUpper();
        }

        public void ReadValue(Action<string, byte[]> action)
        {
            BluetoothLEHardwareInterface.ReadCharacteristic(this.deviceAddress, this.serviceUUID, this.characteristicUUID, action);
        }
        public void WriteValue(byte[] data, bool withResponse)
        {
            BluetoothLEHardwareInterface.WriteCharacteristic(this.deviceAddress, this.serviceUUID, this.characteristicUUID, data, data.Length, withResponse, null);
        }
        public void StartNotifications(Action<byte[]> action)
        {
            BluetoothLEHardwareInterface.SubscribeCharacteristicWithDeviceAddress(this.deviceAddress, this.serviceUUID, this.characteristicUUID, null, (address, characteristicUUID, bytes) =>
            {
                action(bytes);
            });
        }
        public void StopNotifications()
        {
            BluetoothLEHardwareInterface.UnSubscribeCharacteristic(this.deviceAddress, this.serviceUUID, this.characteristicUUID, null);
        }
    }
}