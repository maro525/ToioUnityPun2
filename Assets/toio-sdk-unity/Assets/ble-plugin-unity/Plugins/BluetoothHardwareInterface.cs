using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

public class BluetoothLEHardwareInterface
{
  public enum CBCharacteristicProperties
  {
    CBCharacteristicPropertyBroadcast = 0x01,
    CBCharacteristicPropertyRead = 0x02,
    CBCharacteristicPropertyWriteWithoutResponse = 0x04,
    CBCharacteristicPropertyWrite = 0x08,
    CBCharacteristicPropertyNotify = 0x10,
    CBCharacteristicPropertyIndicate = 0x20,
    CBCharacteristicPropertyAuthenticatedSignedWrites = 0x40,
    CBCharacteristicPropertyExtendedProperties = 0x80,
    CBCharacteristicPropertyNotifyEncryptionRequired = 0x100,
    CBCharacteristicPropertyIndicateEncryptionRequired = 0x200,
  };


  public enum iOSProximity
  {
    Unknown = 0,
    Immediate = 1,
    Near = 2,
    Far = 3,
  }

  public struct iBeaconData
  {
    public string UUID;
    public int Major;
    public int Minor;
    public int RSSI;
    public int AndroidSignalPower;
    public iOSProximity iOSProximity;
  }

  public enum CBAttributePermissions
  {
    CBAttributePermissionsReadable = 0x01,
    CBAttributePermissionsWriteable = 0x02,
    CBAttributePermissionsReadEncryptionRequired = 0x04,
    CBAttributePermissionsWriteEncryptionRequired = 0x08,
  };

#if UNITY_IOS
	[DllImport ("__Internal")]
  private static extern void _uiOSCreateClient();

	[DllImport ("__Internal")]
  private static extern void _uiOSDestroyClient();

	[DllImport ("__Internal")]
  private static extern void _uiOSStartDeviceScan(string[] filteredUUIDs,  bool allowDuplicates);

	[DllImport ("__Internal")]
  private static extern void _uiOSStopDeviceScan();

	[DllImport ("__Internal")]
  private static extern void _uiOSConnectToDevice(string identifier);

	[DllImport ("__Internal")]
  private static extern void _uiOSCancelDeviceConnection(string identifier);

	[DllImport ("__Internal")]
  private static extern void _uiOSCancelDeviceConnectionAll();

	[DllImport ("__Internal")]
  private static extern void _uiOSReadCharacteristicForDevice(string identifier, string serviceUUID, string characteristicUUID);

	[DllImport ("__Internal")]
  private static extern void _uiOSWriteCharacteristicForDevice(string identifier, string serviceUUID, string characteristicUUID, string data, int length, bool withResponse);

	[DllImport ("__Internal")]
  private static extern void _uiOSMonitorCharacteristicForDevice(string identifier, string serviceUUID, string characteristicUUID);

	[DllImport ("__Internal")]
  private static extern void _uiOSUnMonitorCharacteristicForDevice(string identifier, string serviceUUID, string characteristicUUID);
#endif

  private static BluetoothDeviceScript bluetoothDeviceScript;

  public static void Log(string message)
  {

  }

  public static BluetoothDeviceScript Initialize(bool asCentral, bool asPeripheral, Action action, Action<string> errorAction)
  {
    bluetoothDeviceScript = null;

    GameObject bluetoothLEReceiver = GameObject.Find("BluetoothLEReceiver");
    if (bluetoothLEReceiver == null)
    {
      bluetoothLEReceiver = new GameObject("BluetoothLEReceiver");
    }

    if (bluetoothLEReceiver != null)
    {
      bluetoothDeviceScript = bluetoothLEReceiver.AddComponent<BluetoothDeviceScript>();
      if (bluetoothDeviceScript != null)
      {
        bluetoothDeviceScript.InitializedAction = action;
        bluetoothDeviceScript.ErrorAction = errorAction;
      }
    }

    GameObject.DontDestroyOnLoad(bluetoothLEReceiver);

    if (Application.isEditor)
    {
      if (bluetoothDeviceScript != null)
        bluetoothDeviceScript.SendMessage("OnBluetoothMessage", "Initialized");
    }
    else
    {
#if UNITY_IOS
      _uiOSCreateClient();
#endif
    }

    return bluetoothDeviceScript;
  }

  public static void DeInitialize(Action action)
  {
    if (bluetoothDeviceScript != null)
    {
      bluetoothDeviceScript.DeinitializedAction = action;
    }

    if (Application.isEditor)
    {
      if (bluetoothDeviceScript != null)
        bluetoothDeviceScript.SendMessage("OnBluetoothMessage", "DeInitialized");
    }
    else
    {
#if UNITY_IOS
      _uiOSDestroyClient();
#endif
    }
  }

  public static void FinishDeInitialize()
  {
    GameObject bluetoothLEReceiver = GameObject.Find("BluetoothLEReceiver");
    if (bluetoothLEReceiver != null)
    {
      GameObject.Destroy(bluetoothLEReceiver);
    }
  }

  public static void BluetoothEnable(bool enable)
  {
  }

  // public static void PauseMessages(bool isPaused)
  // {
  //   if (!Application.isEditor)
  //   {
  //     _uiOSPauseMessages(isPaused);
  //   }
  // }

  public static void ScanForPeripheralsWithServices(string[] serviceUUIDs, Action<string, string> action, Action<string, string, int, byte[]> actionAdvertisingInfo = null, bool rssiOnly = false, bool clearPeripheralList = true, int recordType = 0xFF)
  {
    if (!Application.isEditor)
    {
      if (bluetoothDeviceScript != null)
      {
        bluetoothDeviceScript.DiscoveredPeripheralAction = action;
        bluetoothDeviceScript.DiscoveredPeripheralWithAdvertisingInfoAction = actionAdvertisingInfo;

        if (bluetoothDeviceScript.DiscoveredDeviceList != null)
          bluetoothDeviceScript.DiscoveredDeviceList.Clear();
      }

#if UNITY_IOS
      _uiOSStartDeviceScan(serviceUUIDs, false);
#endif
    }
  }

  public static void StopScan()
  {
    if (!Application.isEditor)
    {
#if UNITY_IOS
      _uiOSStopDeviceScan();
#endif
    }
  }

  public static void DisconnectAll()
  {
    if (!Application.isEditor)
    {
#if UNITY_IOS
      _uiOSCancelDeviceConnectionAll();
#endif
    }
  }

  public static void ConnectToPeripheral(string name, Action<string> connectAction, Action<string, string> serviceAction, Action<string, string, string> characteristicAction, Action<string> disconnectAction = null)
  {
    if (!Application.isEditor)
    {
      if (bluetoothDeviceScript != null)
      {
        bluetoothDeviceScript.ConnectedPeripheralAction = connectAction;
        bluetoothDeviceScript.DiscoveredServiceAction = serviceAction;
        bluetoothDeviceScript.DiscoveredCharacteristicAction = characteristicAction;
        bluetoothDeviceScript.ConnectedDisconnectPeripheralAction = disconnectAction;
      }

#if UNITY_IOS
      _uiOSConnectToDevice(name);
#endif
    }
  }

  public static void DisconnectPeripheral(string name, Action<string> action)
  {
    if (!Application.isEditor)
    {
      if (bluetoothDeviceScript != null)
        bluetoothDeviceScript.DisconnectedPeripheralAction = action;

#if UNITY_IOS
      _uiOSCancelDeviceConnection(name);
#endif
    }
  }

  public static void ReadCharacteristic(string name, string service, string characteristic, Action<string, byte[]> action)
  {
    if (!Application.isEditor)
    {
      if (bluetoothDeviceScript != null)
      {
        if (!bluetoothDeviceScript.DidUpdateCharacteristicValueAction.ContainsKey(name))
          bluetoothDeviceScript.DidUpdateCharacteristicValueAction[name] = new Dictionary<string, Action<string, byte[]>>();

        bluetoothDeviceScript.DidUpdateCharacteristicValueAction[name][characteristic] = action;
      }

#if UNITY_IOS
      _uiOSReadCharacteristicForDevice(name, service, characteristic);
#endif
    }
  }

  public static void WriteCharacteristic(string name, string service, string characteristic, byte[] data, int length, bool withResponse, Action<string> action)
  {
    if (!Application.isEditor)
    {
      if (bluetoothDeviceScript != null)
        bluetoothDeviceScript.DidWriteCharacteristicAction = action;

#if UNITY_IOS
      _uiOSWriteCharacteristicForDevice(name, service, characteristic, Convert.ToBase64String(data), length, withResponse);
#endif
    }
  }

  public static void SubscribeCharacteristic(string name, string service, string characteristic, Action<string> notificationAction, Action<string, byte[]> action)
  {
    if (!Application.isEditor)
    {
      if (bluetoothDeviceScript != null)
      {
        name = name.ToUpper();
        service = service.ToUpper();
        characteristic = characteristic.ToUpper();

        if (!bluetoothDeviceScript.DidUpdateNotificationStateForCharacteristicAction.ContainsKey(name))
          bluetoothDeviceScript.DidUpdateNotificationStateForCharacteristicAction[name] = new Dictionary<string, Action<string>>();
        bluetoothDeviceScript.DidUpdateNotificationStateForCharacteristicAction[name][characteristic] = notificationAction;

        if (!bluetoothDeviceScript.DidUpdateCharacteristicValueAction.ContainsKey(name))
          bluetoothDeviceScript.DidUpdateCharacteristicValueAction[name] = new Dictionary<string, Action<string, byte[]>>();
        bluetoothDeviceScript.DidUpdateCharacteristicValueAction[name][characteristic] = action;
      }

#if UNITY_IOS
      _uiOSMonitorCharacteristicForDevice(name, service, characteristic);
#endif
    }
  }

  public static void SubscribeCharacteristicWithDeviceAddress(string name, string service, string characteristic, Action<string, string> notificationAction, Action<string, string, byte[]> action)
  {
    if (!Application.isEditor)
    {
      if (bluetoothDeviceScript != null)
      {
        name = name.ToUpper();
        service = service.ToUpper();
        characteristic = characteristic.ToUpper();

        if (!bluetoothDeviceScript.DidUpdateNotificationStateForCharacteristicWithDeviceAddressAction.ContainsKey(name))
          bluetoothDeviceScript.DidUpdateNotificationStateForCharacteristicWithDeviceAddressAction[name] = new Dictionary<string, Action<string, string>>();
        bluetoothDeviceScript.DidUpdateNotificationStateForCharacteristicWithDeviceAddressAction[name][characteristic] = notificationAction;

        if (!bluetoothDeviceScript.DidUpdateCharacteristicValueWithDeviceAddressAction.ContainsKey(name))
          bluetoothDeviceScript.DidUpdateCharacteristicValueWithDeviceAddressAction[name] = new Dictionary<string, Action<string, string, byte[]>>();
        bluetoothDeviceScript.DidUpdateCharacteristicValueWithDeviceAddressAction[name][characteristic] = action;
      }

#if UNITY_IOS
      _uiOSMonitorCharacteristicForDevice(name, service, characteristic);
#endif
    }
  }

  public static void UnSubscribeCharacteristic(string name, string service, string characteristic, Action<string> action)
  {
    if (!Application.isEditor)
    {
      if (bluetoothDeviceScript != null)
      {
        name = name.ToUpper();
        service = service.ToUpper();
        characteristic = characteristic.ToUpper();

        if (!bluetoothDeviceScript.DidUpdateNotificationStateForCharacteristicWithDeviceAddressAction.ContainsKey(name))
          bluetoothDeviceScript.DidUpdateNotificationStateForCharacteristicWithDeviceAddressAction[name] = new Dictionary<string, Action<string, string>>();
        bluetoothDeviceScript.DidUpdateNotificationStateForCharacteristicWithDeviceAddressAction[name][characteristic] = null;

        if (!bluetoothDeviceScript.DidUpdateNotificationStateForCharacteristicAction.ContainsKey(name))
          bluetoothDeviceScript.DidUpdateNotificationStateForCharacteristicAction[name] = new Dictionary<string, Action<string>>();
        bluetoothDeviceScript.DidUpdateNotificationStateForCharacteristicAction[name][characteristic] = null;
      }

#if UNITY_IOS
      _uiOSUnMonitorCharacteristicForDevice(name, service, characteristic);
#endif
    }
  }
}