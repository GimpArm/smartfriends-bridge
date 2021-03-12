# MQTT Configuration

## Extra Configuration

The configuration file (appsettings.json) has the section "Mqtt" which needs to be configured.


## First Start

Recommended to use the HASSIO add-on. Add to the Supervisor Add-on store `https://github.com/GimpArm/hassio-addons`.

With Docker
```bash
docker run -d -v ${pwd}/appsettings.json:/app/appsettings.json gimparm/smartfriends-mqtt:latest
```

From the release binary
```bash
dotnet SmartFriends.Mqtt.dll
```

## Configuration

- Open the (appsettings.json) and change it accordingly:
```yaml
{
  "Mqtt": {
    "DataPath": "/config/smartfriends2mqtt", #---------------------> Directory path to where the deviceMap.json and typeTemplate.json files are stored.
    "BaseTopic": "smartfriends2mqtt", #---> Base MQTT topic to store device information. Ok to leave as is.
    "Server": "homeassistant", #----------> Broker server name or ip.
    "Port": 1883, #-----------------------> Broker port, most likely 1883.
    "User": "mqtt", #---------------------> Broker client username.
    "Password": "4mqPass!", #-------------> Broker client password.
    "UseSsl": false #--------------------> Flag whether the broker requires SSL or not.
  }
}
```

## Collect devices ID's
Device ID's **are important**, they **must** be added with a Type and optional Class to the `deviceMap.json` file in the DataPath folder. Each DeviceMap that is configured will available over MQTT. Any device not configured will be ignored.

When the MQTT client starts it will create a file in the specified DataPath folder `devices.json´

```json
[
  {
    "id": 13103,
    "name": "Blinds Dining",
    "room": "Dining Room",
    "controlValue": 0,
    "analogValue": 100,
	"state": "Stop",
    "commands": [
      "Stop",
      "Up",
      "Down"
    ],
    "min": 0,
    "max": 100,
    "stepSize": 1
  },
  {
    "id": 1323,
    "name": "Light",
    "room": "LivingRoom",
    "controlValue": 1,
	"state": "On",
    "commands": [
      "On",
      "Toggle",
      "Off"
    ]
  },
  ...
  ...
]
```

## DeviceMap

DeviceMap is a map of the Smart Friends Id to the HASSIO type/class with optional device specific overrides.

On start up the `deviceMap.json` file in the `DataPath` folder will be loaded. If no file exists then an empty file is created.

If there is no entry in the deviceMap.json file for a device then it will not be available over MQTT. This is required because mapping a device from the information the Smart Friends Box makes available to a HASSIO device cannot currently be done automatically.

### DeviceMap object definition
```yaml
{
  "Id": number, #-------> (required) The ´id´ from the above print out.
  "Type": string, #-----> (required) The HASSIO device type to be presented as.
  "Class": string, #----> (optional) The HASSIO device class, not all device types have classes.
  "Paramters": { #------> (optional) Key value override HASSIO settings and TypeTemplate for specific operation of the devices. See HASSIO MQTT device type specific documentation.
    "hassio_key1": value1,
    "hassio_key2": value2
  }
}
```


### Variables

There are currently 2 variables that can be useful for making templates that apply to all devices of a specific type.

- `{baseTopic}` is simply the value from the setting `BaseTopic` so the templates can remain generic.
- `{deviceId}` the MQTT unique ID for the device, currently it is in the form `"sf_" + DeviceMap.Id`.


### Examples

####  Schellenberg Rolladen
```json
{
  "Id": 13103,
  "Type": "cover",
  "Class": "shutter"
}
```

#### Zigbee Switch
*Set the icon to a lightbulb.*
```json
{
  "Id": 1323,
  "Type": "switch",
  "Parameters": {
    "icon": "hass:lightbulb"
  }
}
```

#### Zigbee Contact Door Sensor
```json
{
  "Id": 206,
  "Type": "binary_sensor",
  "Class": "door"
}
```


## TypeTemplates

TypeTemplates define templates to apply to all devices types/classes to override the basic default behavior. See https://www.home-assistant.io/docs/mqtt/discovery/

On start up the `typeTemplate.json` file in the `DataPath` folder will be loaded. If no file exists then a file with the below examples will be created.

TypeTemplates are optional but for proper control of most devices it is probably required. I include the example for the Schellenberg Rolladen and a generic Zigbee switch because that is all I own. For proper HASSIO integration you will need to do some trial and error and read the [MQTT Discovery documentation](https://www.home-assistant.io/docs/mqtt/discovery/).

### TypeTemplate object definition
```yaml
{
  "Type": string, #-------> (required) Device type to apply the template to.
  "Class": string, #------> (optional) Some device types have a class which you can use be even more specific about when to apply the template.
  "Parameters": { #-------> (required) Key value override HASSIO settings for specific operation of the devices. See HASSIO MQTT device type specific documentation.
    "hassio_key1": value1,
    "hassio_key2": value2
  }
}
```

### Variables

There are currently 2 variables that can be useful for making templates that apply to all devices of a specific type.

- `{baseTopic}` is simply the value from the setting `BaseTopic` so the templates can remain generic.
- `{deviceId}` the MQTT unique ID for the device, currently it is in the form `"sf_" + DeviceMap.Id`.

### Examples

#### Schellenberg Rolladen
```json
{
  "Type": "cover",
  "Class": "shutter",
  "Parameters": {
    "value_template": "{{ 100 - value_json.analogValue }}", 
    "position_topic": "{baseTopic}/{deviceId}",
    "set_position_topic": "{baseTopic}/{deviceId}/set",
    "set_position_template": "{{ 100 - position }}",
    "payload_stop": "Stop",
    "payload_open": "Up",
    "payload_close": "Down"
 }
},
```

#### Zigbee Switch
```json
{
  "Type": "switch",
  "Parameters": {
    "value_template": "{{ value_json.state }}"
  }
}
```

#### Zigbee Contact Sensor
```json
{
  "Type": "binary_sensor",
  "Parameters": {
    "value_template": "{{ value_json.state }}"
  }
}
```

