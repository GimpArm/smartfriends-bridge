# SmartFriends REST API

## Description
This project is a simple REST API that can be used to control all your SmartFriends devices (Schellenberg, ABUS, Paulmann, Steinel).

This is a re-write in C# .Net taking heavy inspiration from the NodeJS Schellenber API developed by [airthusiast](https://github.com/airthusiast/schellenberg-rest-api) and the Schellenberg API developed by [LoPablo](https://github.com/LoPablo).

Tests have been carried out on on the Smart Friends Box but it probably also works on the Schellenberg SH1-Box.

You must have the [.Net 5.0 Runtime](https://dotnet.microsoft.com/download/dotnet/5.0) installed to use this.

## How to use the REST API?
### Configuration and first start

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
  }
  ```
  **Extra API parameters**:
  In order to find these values, simply open the Smart Friends App and go to the information page as illustrated: 

  ![alt](images/doc00.jpg)

- Now the SmartFriends REST API:
  ```bash
  dotnet SmartFriends.Host.dll --urls=http://localhost:5001
  ```
### Collect devices ID's
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

### Using REST API

The service exposes a simple REST API, which can be called to control your devices.

#### Examples on how to use the REST API
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

## Acknowledgments
Special thanks to [LoPablo](https://github.com/LoPablo) and [AirThusiast](https://github.com/airthusiast) for their work on figuring out how the Schellenberg/SmartFriends API functions.