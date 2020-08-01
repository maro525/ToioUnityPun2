#import "UnityBLE.h"
@import MultiplatformBleAdapter;

BleModule *_bleModule = nil;
NSMutableArray *uuids = nil;
int uniqueId = 0;

typedef void (^Rejection) (NSString *errorCode, NSString *errorMessage, NSError *error);

Rejection rejection = ^(NSString *errorCode, NSString *errorMessage, NSError *error) {
    NSString *message = [NSString stringWithFormat:@"Error~%@", error.description];
    UnitySendMessage ("BluetoothLEReceiver", "OnBluetoothMessage", [message UTF8String]);
    
#ifdef DEBUG
    NSLog(@"rejection: errorCode = %@", errorCode);
    NSLog(@"rejection: errorMessage = %@", errorMessage);
    NSLog(@"rejectino: error = %@", error);
#endif
};

// TODO: move this to csharp layer to use transactionId
NSString* nextUniqueId() {
    uniqueId += 1;
    return [NSString stringWithFormat:@"%d", uniqueId];
}

void _uiOSCreateClient() {
    _bleModule = [BleModule new];
    [_bleModule createClient];
    if (uuids != nil) {
        [uuids removeAllObjects];
    }
    uniqueId = 0;
}

void _uiOSDestroyClient() {
    if (_bleModule != nil) {
        [_bleModule destroyClient];
        _bleModule = nil;

        UnitySendMessage ("BluetoothLEReceiver", "OnBluetoothMessage", "DeInitialized");
    }
}

void _uiOSStartDeviceScan(const char** filteredUUIDs, BOOL allowDuplicates) {
    if (_bleModule != nil) {
        if (uuids == nil) {
            uuids = [[NSMutableArray alloc] init];
        }
        for (const char **p = filteredUUIDs; *p != NULL; p++) {
            [uuids addObject:[NSString stringWithCString:*p encoding:NSUTF8StringEncoding]];
        }
        NSDictionary *options = [NSDictionary dictionaryWithObject:[NSNumber numberWithBool:allowDuplicates] forKey:@"allowDuplicates"];
        [_bleModule startDeviceScan:uuids options:options];
    }
}

void _uiOSStopDeviceScan() {
    if (_bleModule != nil) {
        [_bleModule stopDeviceScan];
    }
}

void _uiOSConnectToDevice(const char* identifier) {
    if (_bleModule != nil) {
        [_bleModule connectToDevice:[NSString stringWithFormat:@"%s", identifier] options:nil resolver:^(NSDictionary *peripheral) {
            NSString *identifier = [peripheral valueForKey:@"id"];
            NSString *message = [NSString stringWithFormat:@"ConnectedPeripheral~%@", identifier];
            UnitySendMessage ("BluetoothLEReceiver", "OnBluetoothMessage", [message UTF8String]);
            
            // retrieve services and characteristics
            [_bleModule discoverAllServicesAndCharacteristicsForDevice:identifier transactionId:nextUniqueId() resolver:^(NSDictionary *peripheral) {
                
                [_bleModule servicesForDevice:identifier resolver:^(NSArray<NSDictionary *> *services) {
                    [services enumerateObjectsUsingBlock:^(NSDictionary * _Nonnull obj, NSUInteger idx, BOOL * _Nonnull stop) {
                        // device id and service uuid
                        NSString *message = [NSString stringWithFormat:@"DiscoveredService~%@~%@", [obj valueForKey:@"deviceID"], [obj valueForKey:@"uuid"]];
                        UnitySendMessage ("BluetoothLEReceiver", "OnBluetoothMessage", [message UTF8String]);
                                             
                        [_bleModule characteristicsForService:[obj valueForKey:@"id"] resolver:^(NSArray<NSDictionary *> *characteristics) {
                            [characteristics enumerateObjectsUsingBlock:^(NSDictionary * _Nonnull obj, NSUInteger idx, BOOL * _Nonnull stop) {
                                NSString *message = [NSString stringWithFormat:@"DiscoveredCharacteristic~%@~%@~%@", [obj valueForKey:@"deviceID"], [obj valueForKey:@"serviceUUID"], [obj valueForKey:@"uuid"]];
                                UnitySendMessage ("BluetoothLEReceiver", "OnBluetoothMessage", [message UTF8String]);
                            }];
                        } rejecter:rejection];
                    }];
                } rejecter:rejection];
            } rejecter:rejection];
        } rejecter:rejection];
    }
}

void _uiOSCancelDeviceConnection(const char* identifier) {
    if (_bleModule != nil) {
        [_bleModule cancelDeviceConnection:[NSString stringWithFormat:@"%s", identifier] resolver:^(NSDictionary *peripheral) {
            NSString *identifier = [peripheral valueForKey:@"id"];
            NSString *message = [NSString stringWithFormat:@"DisconnectedPeripheral~%@", identifier];
            UnitySendMessage ("BluetoothLEReceiver", "OnBluetoothMessage", [message UTF8String]);
        } rejecter:^(NSString *errorCode, NSString *errorMessage, NSError *error) {
            NSString *message = [NSString stringWithFormat:@"Error~%@", error.description];
            UnitySendMessage ("BluetoothLEReceiver", "OnBluetoothMessage", [message UTF8String]);
        }];
    }
}

void _uiOSCancelDeviceConnectionAll() {
    if (_bleModule != nil) {
        [_bleModule connectedDevices:uuids resolver:^(NSArray<NSDictionary *> *peripherals) {
            [peripherals enumerateObjectsUsingBlock:^(NSDictionary * _Nonnull obj, NSUInteger idx, BOOL * _Nonnull stop) {
                NSString *identifier = [obj valueForKey:@"id"];
                _uiOSCancelDeviceConnection([identifier UTF8String]);
            }];
        } rejecter:^(NSString *errorCode, NSString *errorMessage, NSError *error) {
            NSString *message = [NSString stringWithFormat:@"Error~%@", error.description];
            UnitySendMessage ("BluetoothLEReceiver", "OnBluetoothMessage", [message UTF8String]);
        }];
    }
}

void _uiOSReadCharacteristicForDevice(const char* identifier, const char* serviceUUID, const char* characteristicUUID) {
    if (_bleModule != nil) {
        [_bleModule readCharacteristicForDevice:[NSString stringWithUTF8String:identifier] serviceUUID:[NSString stringWithUTF8String:serviceUUID] characteristicUUID:[NSString stringWithUTF8String:characteristicUUID] transactionId:nextUniqueId() resolver:^(NSDictionary *characteristic) {
            NSString *message = [NSString stringWithFormat:@"DidUpdateValueForCharacteristic~%@~%@~%@", [characteristic valueForKey:@"deviceID"], [characteristic valueForKey:@"uuid"], [characteristic valueForKey:@"value"]];
            UnitySendMessage ("BluetoothLEReceiver", "OnBluetoothMessage", [message UTF8String]);
        } rejecter:rejection];
    }
}

void _uiOSWriteCharacteristicForDevice(const char* identifier, const char* serviceUUID, const char* characteristicUUID, const char* data, int length, BOOL withResponse) {
    if (_bleModule != nil) {
        [_bleModule writeCharacteristicForDevice:[NSString stringWithUTF8String:identifier] serviceUUID:[NSString stringWithUTF8String:serviceUUID] characteristicUUID:[NSString stringWithUTF8String:characteristicUUID] valueBase64:[NSString stringWithUTF8String:data] withResponse:withResponse transactionId:nextUniqueId() resolver:^(NSDictionary *characteristic) {
            NSString *message = [NSString stringWithFormat:@"DidWriteCharacteristic~%@", [characteristic valueForKey:@"uuid"]];
            UnitySendMessage ("BluetoothLEReceiver", "OnBluetoothMessage", [message UTF8String] );
        } rejecter:rejection];
    }
}

void _uiOSMonitorCharacteristicForDevice(const char* identifier, const char* serviceUUID, const char* characteristicUUID) {
    if (_bleModule != nil) {
        [_bleModule monitorCharacteristicForDevice:[NSString stringWithUTF8String:identifier] serviceUUID:[NSString stringWithUTF8String:serviceUUID] characteristicUUID:[NSString stringWithUTF8String:characteristicUUID] transactionID:nextUniqueId() resolver:^(id value) {
            // no op
        } rejecter:rejection];
    }
}

void _uiOSUnMonitorCharacteristicForDevice(const char* identifier, const char* serviceUUID, const char* characteristicUUID) {
    // no op
}



@interface BleModule () <BleClientManagerDelegate>
@property(nonatomic) BleClientManager* manager;
@end

@implementation BleModule
{
    bool hasListeners;
}

- (void)dispatchEvent:(NSString * _Nonnull)name value:(id _Nonnull)value {
#ifdef DEBUG
    NSLog(@"[dispatchEvent]");
    NSLog(@"  name : %@", name);
    NSLog(@"  value: %@", value);
#endif    

    if ([name isEqualToString:@"StateChangeEvent"] && [value isEqualToString:@"PoweredOn"]) {
        UnitySendMessage("BluetoothLEReceiver", "OnBluetoothMessage", "Initialized");
        return;
    }
    
    if ([name isEqualToString:@"ScanEvent"]) {
        if ([value isKindOfClass:[NSArray class]]) {
            NSObject *item = [value objectAtIndex:1];
            NSString *identifier = [item valueForKey:@"id"];
            NSString *name = [item valueForKey:@"name"];
            NSString *rssi = [item valueForKey:@"rssi"];
            NSString *manufacturerData = [item valueForKey:@"manufacturerData"];

            NSString *message = nil;
            if ([manufacturerData isEqual:[NSNull null]]) {
                message = [NSString stringWithFormat:@"DiscoveredPeripheral~%@~%@~%@~", identifier, name, rssi];
            } else {
                message = [NSString stringWithFormat:@"DiscoveredPeripheral~%@~%@~%@~%@", identifier, name, rssi, manufacturerData];
            }
            
            UnitySendMessage ("BluetoothLEReceiver", "OnBluetoothMessage", [message UTF8String]);
        }
        return;
    }
    

    if ([name isEqualToString:@"RestoreStateEvent"]) {
        return;
    }
    
    if ([name isEqualToString:@"ConnectingEvent"]) {
        return;
    }

    if ([name isEqualToString:@"ConnectedEvent"]) {
        return;
    }
    
    if ([name isEqualToString:@"DisconnectionEvent"]) {
        return;
    }

    if ([name isEqualToString:@"ReadEvent"]) {
        if ([value isKindOfClass:[NSArray class]]) {
            NSObject *characteristic = [value objectAtIndex:1];
            NSString *identifier = [characteristic valueForKey:@"deviceID"];
            NSString *characteristicUUID = [characteristic valueForKey:@"uuid"];
            NSString *value = [characteristic valueForKey:@"value"];
            
            NSString *message = [NSString stringWithFormat:@"DidUpdateValueForCharacteristic~%@~%@~%@", identifier, characteristicUUID, value];
            UnitySendMessage ("BluetoothLEReceiver", "OnBluetoothMessage", [message UTF8String]);
        }
        return;
    }

}

- (void)startObserving {
    hasListeners = YES;
}

- (void)stopObserving {
    hasListeners = NO;
}

- (NSArray<NSString *> *)supportedEvents {
    return BleEvent.events;
}

- (NSDictionary<NSString *,id> *)constantsToExport {
    NSMutableDictionary* consts = [NSMutableDictionary new];
    for (NSString* event in BleEvent.events) {
        [consts setValue:event forKey:event];
    }
    return consts;
}

+ (BOOL)requiresMainQueueSetup {
    return YES;
}

- (void)createClient {
    _manager = [[BleClientManager alloc] initWithQueue:dispatch_get_main_queue()
                                  restoreIdentifierKey:nil];
    _manager.delegate = self;
}

- (void)destroyClient {
    [_manager invalidate];
    _manager = nil;
}

- (void)invalidate {
    [self destroyClient];
}


// Mark: Scanning ------------------------------------------------------------------------------------------------------

- (void)startDeviceScan:(NSArray*)filteredUUIDs
                options:(NSDictionary*)options {
     [_manager startDeviceScan:filteredUUIDs options:options];
 }

- (void)stopDeviceScan {
    [_manager stopDeviceScan];
}

// Mark: Device management ---------------------------------------------------------------------------------------------

- (void)connectedDevices:(NSArray<NSString*>*)serviceUUIDs
                resolver:(void (^)(NSArray<NSDictionary*>* peripherals))resolve
                rejecter:(void (^)(NSString* code, NSString* message, NSError* error))reject {
    [_manager connectedDevices:serviceUUIDs
                       resolve:resolve
                        reject:reject];
}

// Mark: Connection management -----------------------------------------------------------------------------------------

- (void)connectToDevice:(NSString*)deviceIdentifier
                options:(NSDictionary*)options
               resolver:(void (^)(NSDictionary* peripheral))resolve
               rejecter:(void (^)(NSString* code, NSString* message, NSError* error))reject {
    [_manager connectToDevice:deviceIdentifier
                      options:options
                      resolve:resolve
                       reject:reject];
}

- (void)cancelDeviceConnection:(NSString*)deviceIdentifier
                      resolver:(void (^)(NSDictionary* peripheral))resolve
                      rejecter:(void (^)(NSString* code, NSString* message, NSError* error))reject {
    [_manager cancelDeviceConnection:deviceIdentifier
                             resolve:resolve
                              reject:reject];
}

// Mark: Discovery -----------------------------------------------------------------------------------------------------

- (void)discoverAllServicesAndCharacteristicsForDevice:(NSString*)deviceIdentifier
                                         transactionId:(NSString*)transactionId
                                              resolver:(void (^)(NSDictionary* peripheral))resolve
                                              rejecter:(void (^)(NSString* code, NSString* message, NSError* error))reject {
    [_manager discoverAllServicesAndCharacteristicsForDevice:deviceIdentifier
                                               transactionId:transactionId
                                                     resolve:resolve
                                                      reject:reject];
}


// Mark: Service and characteristic getters ----------------------------------------------------------------------------

- (void)servicesForDevice:(NSString*)deviceIdentifier
                 resolver:(void (^)(NSArray<NSDictionary*>* services))resolve
                 rejecter:(void (^)(NSString* code, NSString* message, NSError* error))reject {
    [_manager servicesForDevice:deviceIdentifier
                        resolve:resolve
                         reject:reject];
}

- (void)characteristicsForDevice:(NSString*)deviceIdentifier
                     serviceUUID:(NSString*)serviceUUID
                        resolver:(void (^)(NSArray<NSDictionary*>* characteristics))resolve
                        rejecter:(void (^)(NSString* code, NSString* message, NSError* error))reject {
    [_manager characteristicsForDevice:deviceIdentifier
                           serviceUUID:serviceUUID
                               resolve:resolve
                                reject:reject];
}

- (void)characteristicsForService:(nonnull NSNumber*)serviceIdentifier
                         resolver:(void (^)(NSArray<NSDictionary*>* characteristics))resolve
                         rejecter:(void (^)(NSString* code, NSString* message, NSError* error))reject {
    [_manager characteristicsForService:serviceIdentifier.doubleValue
                                resolve:resolve
                                 reject:reject];
}

// Mark: Characteristics operations ------------------------------------------------------------------------------------

- (void)readCharacteristicForDevice:(NSString*)deviceIdentifier
                        serviceUUID:(NSString*)serviceUUID
                 characteristicUUID:(NSString*)characteristicUUID
                      transactionId:(NSString*)transactionId
                           resolver:(void (^)(NSDictionary* characteristic))resolve
                           rejecter:(void (^)(NSString* code, NSString* message, NSError* error))reject {
    [_manager readCharacteristicForDevice:deviceIdentifier
                              serviceUUID:serviceUUID
                       characteristicUUID:characteristicUUID
                            transactionId:transactionId
                                  resolve:resolve
                                   reject:reject];
}

- (void)readCharacteristicForService:(nonnull NSNumber*)serviceIdentifier
                  characteristicUUID:(NSString*)characteristicUUID
                       transactionId:(NSString*)transactionId
                            resolver:(void (^)(NSDictionary* characteristic))resolve
                            rejecter:(void (^)(NSString* code, NSString* message, NSError* error))reject {
    [_manager readCharacteristicForService:serviceIdentifier.doubleValue
                        characteristicUUID:characteristicUUID
                             transactionId:transactionId
                                   resolve:resolve
                                    reject:reject];
}

- (void)readCharacteristic:(nonnull NSNumber*)characteristicIdentifier
             transactionId:(NSString*)transactionId
                  resolver:(void (^)(NSDictionary* characteristic))resolve
                  rejecter:(void (^)(NSString* code, NSString* message, NSError* error))reject {
    [_manager readCharacteristic:characteristicIdentifier.doubleValue
                   transactionId:transactionId
                         resolve:resolve
                          reject:reject];
}

- (void)writeCharacteristicForDevice:(NSString*)deviceIdentifier
                         serviceUUID:(NSString*)serviceUUID
                  characteristicUUID:(NSString*)characteristicUUID
                         valueBase64:(NSString*)valueBase64
                        withResponse:(BOOL)response
                       transactionId:(NSString*)transactionId
                            resolver:(void (^)(NSDictionary* characteristic))resolve
                            rejecter:(void (^)(NSString* code, NSString* message, NSError* error))reject {
    [_manager writeCharacteristicForDevice:deviceIdentifier
                               serviceUUID:serviceUUID
                        characteristicUUID:characteristicUUID
                               valueBase64:valueBase64
                                  response:response
                             transactionId:transactionId
                                   resolve:resolve
                                    reject:reject];
}

- (void)writeCharacteristicForService:(nonnull NSNumber*)serviceIdentifier
                   characteristicUUID:(NSString*)characteristicUUID
                          valueBase64:(NSString*)valueBase64
                         withResponse:(BOOL)response
                        transactionId:(NSString*)transactionId
                             resolver:(void (^)(NSDictionary* characteristic))resolve
                             rejecter:(void (^)(NSString* code, NSString* message, NSError* error))reject {
    [_manager writeCharacteristicForService:serviceIdentifier.doubleValue
                         characteristicUUID:characteristicUUID
                                valueBase64:valueBase64
                                   response:response
                              transactionId:transactionId
                                    resolve:resolve
                                     reject:reject];
}

- (void)writeCharacteristic:(nonnull NSNumber*)characteristicIdentifier
                valueBase64:(NSString*)valueBase64
               withResponse:(BOOL)response
              transactionId:(NSString*)transactionId
                   resolver:(void (^)(NSDictionary* characteristic))resolve
                   rejecter:(void (^)(NSString* code, NSString* message, NSError* error))reject {
    [_manager writeCharacteristic:characteristicIdentifier.doubleValue
                      valueBase64:valueBase64
                         response:response
                    transactionId:transactionId
                          resolve:resolve
                           reject:reject];
}

- (void)monitorCharacteristicForDevice:(NSString*)deviceIdentifier
                           serviceUUID:(NSString*)serviceUUID
                    characteristicUUID:(NSString*)characteristicUUID
                         transactionID:(NSString*)transactionId
                              resolver:(void (^)(id))resolve
                              rejecter:(void (^)(NSString* code, NSString* message, NSError* error))reject {
    [_manager monitorCharacteristicForDevice:deviceIdentifier
                                 serviceUUID:serviceUUID
                          characteristicUUID:characteristicUUID
                               transactionId:transactionId
                                     resolve:resolve
                                      reject:reject];
}

- (void)monitorCharacteristicForService:(nonnull NSNumber*)serviceIdentifier
                     characteristicUUID:(NSString*)characteristicUUID
                          transactionID:(NSString*)transactionId
                               resolver:(void (^)(id))resolve
                               rejecter:(void (^)(NSString* code, NSString* message, NSError* error))reject {
    [_manager monitorCharacteristicForService:serviceIdentifier.doubleValue
                           characteristicUUID:characteristicUUID
                                transactionId:transactionId
                                      resolve:resolve
                                       reject:reject];
}

- (void)monitorCharacteristic:(nonnull NSNumber*)characteristicIdentifier
                transactionID:(NSString*)transactionId
                     resolver:(void (^)(id))resolve
                     rejecter:(void (^)(NSString* code, NSString* message, NSError* error))reject {
    [_manager monitorCharacteristic:characteristicIdentifier.doubleValue
                      transactionId:transactionId
                            resolve:resolve
                             reject:reject];
}

- (void)cancelTransaction:(NSString*)transactionId {
    [_manager cancelTransaction:transactionId];
}

@end
