# SmartFriends Bridge

## Description
This project is a simple bridge for the Smart Friends Box and devices (Schellenberg, ABUS, Paulmann, Steinel). There is a REST API and an MQTT client.

Tests have been carried out on on the Smart Friends Box but it probably also works on the Schellenberg SH1-Box.

You must have the [.Net 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) installed to use this or you must run as the docker/hassio-addon.

A device **must** be supported by the Smart Friends Box to be controllable. So if you paired a Zigbee or Z-Wave device and do not see it in the Smart Friends app, then is unlikely you be able to control it with this bridge even though you will see the device listed.

## Installing

Recommended to use the HASSIO add-on. Add to the Supervisor Add-on store
`https://github.com/GimpArm/hassio-addons`

See readme specific to service type for other install methods.

## How to use the bridge?

### MQTT or REST?
First decide if using MQTT or REST API.

MQTT will integrate into the [Home Assistant MQTT Integration](https://www.home-assistant.io/integrations/mqtt). The devices you setup and map will automatically be discovered by the integration creating devices and entities. It also takes advantage of the push notifications from the Smart Friends Box and relays them to the MQTT broker which informs Home Assistant. It is a more powerful interface but requires that you have configured a broker and the MQTT integration, along with some device mappings because the Smart Friends Box does not give enough information to accurately guess the what kind of device or how to control it in Home Assistant.

REST API is a more simple passive system. You must manually configure entities in Home Assistant to query the service along with polling for changes. This means there is usually a few seonc delay between manually operating a device and seeing its state change in Home Assistant.

### Configuration

**Both MQTT and REST API must be configured to talk to the Smart Friends Box.**

- Open the (appsettings.json) and change it accordingly:
```yaml
{
  "SmartFriends": {
    "Username": "", #---------------> Username (case sensitive)
    "Password": "", #---------------> Password
    "Host": "", #-------------------> IP of your Smart Friends Box
    "Port": 4300, #-----------------> Port of the box, generally 4300/tcp
    "CSymbol": "D19033", #----------> Extra param 1
    "CSymbolAddon": "i", #----------> Extra param 2
    "ShcVersion": "2.21.1", #-------> Extra param 3
    "ShApiVersion":  "2.20" #-------> Extra param 4
  },
  "Mqtt": {
    "Enabled": false, #-------------------> Flag whether to enabled or disable the MQTT client.
    "DataPath": "/config/smartfriends2mqtt", #---------------------> Directory path to where the deviceMap.json and typeTemplate.json files are stored.
    "BaseTopic": "smartfriends2mqtt", #---> Base MQTT topic to store device information. Ok to leave as is.
    "Server": "homeassistant", #----------> Broker server name or ip.
    "Port": 1883, #-----------------------> Broker port, most likely 1883.
    "User": "mqtt", #---------------------> Broker client username.
    "Password": "4mqPass!", #-------------> Broker client password.
    "UseSsl": false, #--------------------> Flag whether the broker requires SSL or not.
    "ProtocolVersion": "V500" #-----------> One of V310=3, V311=4, V500=5. If you have no problems, no need to change.
  }
}
```

**Extra API parameters**:
In order to find these values, simply open the Smart Friends App and go to the information page as illustrated:

![alt](https://raw.githubusercontent.com/GimpArm/hassio-addons/main/images/doc00.jpg)

### Collect devices ID's
Device ID's **are important**, they will be used to interact with the device itself.

After starting up in a browser go to:
```http://homeassistant:5001/list```

And you will see an output like this:

```json
[
  {
    "id": 13103,
    "name": "Blinds Dining",
    "room": "Dining Room",
    "gatewayDevice": "SmartFriendsBox",
    "kind": "RollingShutter",
    "manufacturer": "Alfred Schellenberg GmbH",
    "state": "Stop",
    "devices": {
      "rollingShutter": {
        "Id": 2973,
        "description": "SchellenbergBlind",
        "commands": {
          "Stop": 0,
          "Up": 1,
          "Down": 2
        },
        "currentValue": "Stop"
      },
      "position": {
        "Id": 3555,
        "description": "SchellenbergPosition_Blind",
        "max": 100,
        "min": 0,
        "precision": 0,
        "step": 1,
        "currentValue": 0
      },
      "stepper": {
        "Id": 14246,
        "description": "SchellenbergBlind.Steps",
        "commands": {
          "Up": 1,
          "Down": 2
        }
      },
      "default": {
        "Id": 4144,
        "description": "SchellenbergRssi",
        "currentValue": "SignalHigh"
      }
    }
  },
  {
    "id": 1323,
    "name": "Light",
    "room": "LivingRoom",
    "gatewayDevice": "SmartFriendsBox",
    "kind": "switchActuator",
    "manufacturer": "Zigbee Switch Device",
    "state": "Off",
    "devices": {
      "switchActuator": {
        "Id": 14246,
        "description": "Zigbee Switch Device",
        "commands": {
          "Off": 0,
          "On": 1,
          "Toggle": 2
        }
      }
    }
  },
  ...
  ...
]
```

# Using REST API
The service exposes a simple REST API which you can call to control your devices. Below are examples of the raw endpoints, followed by **Home Assistant examples using the `rest` integration** (REST sensors + `rest_command`) instead of `command_line` / `shell_command`.

## Examples: calling the REST API directly

- Open shutter:
  ```text
  http://127.0.0.1:5001/set/13103/rollingShutter/up
  ```
- Close shutter:
  ```text
  http://127.0.0.1:5001/set/13103/rollingShutter/down
  ```
- Stop shutter:
  ```text
  http://127.0.0.1:5001/set/13103/rollingShutter/stop
  ```
- Go to position 50%:
  ```text
  http://127.0.0.1:5001/set/13103/position/50
  ```
- Get current shutter position:
  ```text
  http://127.0.0.1:5001/get/13103/position
  ```
- Move shutter 1 step down:
  ```text
  http://127.0.0.1:5001/set/13103/stepper/down
  ```

Other devices can be controlled by using a command or setting a numeric value. Simple devices like a Zigbee switch can be controlled with:

- Turn device on:
  ```text
  http://127.0.0.1:5001/set/1323/on
  ```
- Turn device off:
  ```text
  http://127.0.0.1:5001/set/1323/off
  ```
- Read state:
  ```text
  http://127.0.0.1:5001/get/1323
  ```

---

## Home Assistant RESTful Sensor

This is an example of integrating to Home Assistant using the [RESTful Sensor](https://www.home-assistant.io/integrations/sensor.rest/).
The MQTT approach will yield better results but REST is still an option for people who do not want to use it.

### A) Cover (rolling shutter) using REST sensor + rest_command + template cover

This example creates three parts:

1. A **REST sensor** to poll the current shutter position
2. **REST commands** for up/down/stop/set-position
3. A **Template Cover** that binds (1) and (2) into a real `cover.*` entity

```yaml
# 1) Read current position (polling)
rest:
  - resource: "http://127.0.0.1:5001/get/10433/position"
    scan_interval: 5
    sensor:
      - name: "Shutter Position Office"
        unique_id: "shutter_position_office"
        unit_of_measurement: "%"
        value_template: "{{ 100 - (value_json.currentValue | int(0)) }}"

# 2) Control endpoints
rest_command:
  shutter_up:
    url: "http://127.0.0.1:5001/set/{{ device_id }}/rollingShutter/up"
    method: GET

  shutter_down:
    url: "http://127.0.0.1:5001/set/{{ device_id }}/rollingShutter/down"
    method: GET

  shutter_stop:
    url: "http://127.0.0.1:5001/set/{{ device_id }}/rollingShutter/stop"
    method: GET

  shutter_set_position:
    # Invert for devices that are opposite of Home Assistant
    url: "http://127.0.0.1:5001/set/{{ device_id }}/position/{{ 100 - position }}"
    method: GET

# 3) The cover entity
cover:
  - platform: template
    covers:
      shutter_office:
        friendly_name: "Shutter - Office"
        device_class: shutter
        position_template: "{{ states('sensor.shutter_position_office') | int(0) }}"
        open_cover:
          service: rest_command.shutter_up
          data:
            device_id: 10433
        close_cover:
          service: rest_command.shutter_down
          data:
            device_id: 10433
        stop_cover:
          service: rest_command.shutter_stop
          data:
            device_id: 10433
        set_cover_position:
          service: rest_command.shutter_set_position
          data:
            device_id: 10433
            position: "{{ position }}"
```

---

### B) Switch using REST sensor + rest_command + template switch

Example configuration:

```yaml
# 1) Read state
rest:
  - resource: "http://127.0.0.1:5001/get/14106"
    scan_interval: 5
    sensor:
      - name: "Office Light Raw State"
        unique_id: "office_light_raw_state"
        value_template: "{{ value_json.state }}"  # "On" or "Off"

# 2) Control endpoints
rest_command:
  device_on:
    url: "http://127.0.0.1:5001/set/{{ device_id }}/on"
    method: GET
  device_off:
    url: "http://127.0.0.1:5001/set/{{ device_id }}/off"
    method: GET

# 3) The switch entity
switch:
  - platform: template
    switches:
      office_light:
        friendly_name: "Office Light"
        # Convert the REST sensor string ("On"/"Off") into a boolean
        value_template: "{{ states('sensor.office_light_raw_state') | lower == 'on' }}"
        turn_on:
          service: rest_command.device_on
          data:
            device_id: 14106
        turn_off:
          service: rest_command.device_off
          data:
            device_id: 14106
```

---

### Notes

- This REST approach is **polling-based**, so state updates can lag by your `scan_interval`.


# Using MQTT

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
  "Parameters": { #------> (optional) Key value override HASSIO settings and TypeTemplate for specific operation of the devices. See HASSIO MQTT device type specific documentation.
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

TypeTemplates are optional but for proper control of most devices it is probably required. I include the example for the Schellenberg Rolladen because that is all I own. For proper HASSIO integration you will need to do some trial and error and read the [MQTT Discovery documentation](https://www.home-assistant.io/docs/mqtt/discovery/).

*For very basic devices like a Zigbee switch or Zigbee door/window sensor that only needs to read the state information and/or send a single sub device command no template is needed.*

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
    "command_topic": "{baseTopic}/{deviceId}/rollingShutter/set",
    "position_topic": "{baseTopic}/{deviceId}/position",
    "position_template": "{{ 100 - value | int }}",
    "set_position_topic": "{baseTopic}/{deviceId}/position/set",
    "set_position_template": "{{ 100 - position }}",
    "state_stopped": "Stop",
    "state_opening": "Up",
    "state_closing": "Down",
    "payload_stop": "Stop",
    "payload_open": "Up",
    "payload_close": "Down"
  }
},
```
## Acknowledgments
Special thanks to [LoPablo](https://github.com/LoPablo) for their work on figuring out how the Schellenberg/SmartFriends API functions.
