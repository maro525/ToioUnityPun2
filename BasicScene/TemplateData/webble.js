// device
var deviceCount = 0;
var deviceTable = {};
var deviceEventTable = {};
// server
var serverCount = 0;
var serverTable = {};
// service
var serviceCount = 0;
var serviceTable = {};
// peripheral
var peripheralCount = 0;
var peripheralTable = {};
// characteristic
var characteristicCount = 0;
var characteristicTable = {};
var characteristicNotificationTable = {};

// callback(int deviceID, string deviceUUID, string deviceName)
function bluetooth_requestDevice(SERVICE_UUID, callback, errorCallback)
{
    let options = {};
    options.filters = [ {services: [SERVICE_UUID]}, ];
    navigator.bluetooth.requestDevice(options)
    .then(device => {
        let id = deviceCount;
        deviceTable[id] = device;
        deviceCount++;
        callback(id, device.id, device.name);
    })
    .catch(error => {
        errorCallback(error.message);
    });
}

// callback(int deviceID, int serverID, int serviceID, string serviceUUID)
// disconnectCallback(int deviceID)
function server_connect(deviceID, SERVICE_UUID, callback, disconnectCallback)
{
    deviceTable[deviceID].gatt.connect()
    .then(server => {
        let ondisconnected = () => {
            deviceTable[deviceID].removeEventListener('gattserverdisconnected', deviceEventTable[deviceID]);
            delete deviceEventTable[deviceID];
            disconnectCallback(deviceID);
        }
        deviceEventTable[deviceID] = ondisconnected;
        deviceTable[deviceID].addEventListener('gattserverdisconnected', ondisconnected);
        let id = serverCount;
        serverTable[id] = server;
        serverCount++;
        server_getPrimaryService(id, SERVICE_UUID, (serviceID, serviceUUID) => { callback(deviceID, id, serviceID, serviceUUID); });
    });
}

// callback(int serviceID, string serviceUUID)
function server_getPrimaryService(serverID, SERVICE_UUID, callback)
{
    serverTable[serverID].getPrimaryService(SERVICE_UUID)
    .then(service => {
        let id = serviceCount;
        serviceTable[id] = service;
        serviceCount++;
        callback(id, service.uuid);
    });
}

function server_disconnect(serverID)
{
    serverTable[serverID].disconnect();
}

function service_getCharacteristic(serviceID, characteristicUUID, callback)
{
    serviceTable[serviceID].getCharacteristic(characteristicUUID)
    .then(chara => {
        let id = characteristicCount;
        characteristicTable[id] = chara;
        characteristicCount++;
        callback(serviceID, id, characteristicUUID);
    });
}

function service_getCharacteristics(serviceID, callback)
{
    serviceTable[serviceID].getCharacteristics()
    .then(charas => {

        for(let i = 0; i < charas.length; i++)
        {
            let id = characteristicCount;
            characteristicTable[id] = charas[i];
            callback(serviceID, charas.length, i, id, charas[i].uuid);
            characteristicCount++;
        }
    });
}

function characteristic_writeValue(characteristicID, bytes)
{
    characteristicTable[characteristicID].writeValue(bytes);
}

function characteristic_readValue(characteristicID, callback)
{
    characteristicTable[characteristicID].readValue()
    .then(response => {
        callback(characteristicID, response.buffer);
    });
}

// callback(int characteristicID, byte[] data)
function characteristic_startNotifications(characteristicID, callback)
{
    characteristicTable[characteristicID].startNotifications().then(char => {
        console.log('notifications started');
        let onchanged = (event) => {
            callback(characteristicID, event.target.value.buffer);
        };
        characteristicNotificationTable[characteristicID] = onchanged;
        char.addEventListener('characteristicvaluechanged', onchanged);
    });
}

function characteristic_stopNotifications(characteristicID)
{
    characteristicTable[characteristicID].stopNotifications().then(char => {
        char.removeEventListener('characteristicvaluechanged', characteristicNotificationTable[characteristicID]);
        delete characteristicNotificationTable[characteristicID];
    });
}

function ab2str(buf)
{
    return String.fromCharCode.apply(null, new Uint8Array(buf));
}