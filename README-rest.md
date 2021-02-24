# REST API Configuration

## First Start

Recommended to use the HASSIO add-on. Add to the Supervisor Add-on store `https://github.com/GimpArm/hassio-addons`.

With Docker
```bash
docker run -d -p 5001:80 -v ${pwd}/appsettings.json:/app/appsettings.json gimparm/smartfriends-rest-api:latest
```

From the release binary
```bash
dotnet SmartFriends.Host.dll --urls=http://localhost:5001
```

## Collect devices ID's
Device ID's **are important**, they will be used to interact with the device itself.

This .Net version makes this process easier. In a browser go to:
```http://127.0.0.1:5001/list```

And you will see an output like this:

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

## Using REST API

The service exposes a simple REST API, which can be called to control your devices.

### Examples on how to use the REST API
- Open Shutter: 

  ```http://127.0.0.1:5001/set/13103/open```
- Close shutter:

  ```http://127.0.0.1:5001/set/13103/close```
- Stop shutter:
  
  ```http://127.0.0.1:5001/set/13103/stop```
- Go to position 50%:
  
  ```http://127.0.0.1:5001/set/13103/50```
- Get current shutter position:
  
  ```http://127.0.0.1:5001/get/13103```
  
Other devices can be controlled by using a command or setting a numeric value. I have only tested Zigbee switched but others should also work.

- Turn light on:

```http://127.0.0.1:5001/set/on```

As it can be seen, actions are send using the device ID.