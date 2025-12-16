![PepperDash Essentials Pluign Logo](/images/essentials-plugin-blue.png)

# PepperDash Christie Projector Plugin (c) 2024

## Device Configuration

```json
{
	"key": "projector1",
	"uid": 1,
	"name": "Christie 4K7-HS Projector",
	"type": "christie4K7HsProjector",
	"group": "plugin",
	"properties": {
		"control": {
			"method": "tcpIp",
			"tcpSshProperties": {
				"address": "",
				"port": 3002,
				"username": "",
				"password": "",
				"autoReconnect": true,
				"autoReconnectIntervalMs": 5000,
			},
			"pollIntervalMs": 60000,
			"coolingTimeMs": 30000,
			"warmingTimeMs": 30000,
			"hasLamps": true,
			"hasScreen": false,
			"hasLift": false
		}
	}
}

{
	"key": "projector2",
	"uid": 1,
	"name": "Christie 4K25-RGB Projector",
	"type": "christie4K25RgbProjector",
	"group": "plugin",
	"properties": {
		"control": {
			"method": "tcpIp",
			"tcpSshProperties": {
				"address": "",
				"port": 3002,
				"username": "",
				"password": "",
				"autoReconnect": true,
				"autoReconnectIntervalMs": 5000,
			},
			"pollIntervalMs": 60000,
			"coolingTimeMs": 30000,
			"warmingTimeMs": 30000,
			"hasLamps": true,
			"hasScreen": false,
			"hasLift": false
		}
	}
}
```
**Notes**

`hasLamps`, `hasScreen`, `hasLift` are configuration options that are exposed via the bridge and can be leveraged by the developer in SIMPL.

## Bridge Configuration

```json
{
	"key": "plugin-bridge1",
	"uid": 3,
	"name": "Plugin Bridge",
	"group": "api",
	"type": "eiscApiAdvanced",
	"properties": {
		"control": {
			"tcpSshProperties": {
				"address": "127.0.0.2",
				"port": 0
			},
			"ipid": "B2",
			"method": "ipidTcp"
		},
		"devices": [
			{
				"deviceKey": "projector1",
				"joinStart": 1
			}
		]
	}
}
```

## Bridge Map

### Digitals



### Analogs



### Serials



<!-- START Minimum Essentials Framework Versions -->
### Minimum Essentials Framework Versions

- 2.5.1
<!-- END Minimum Essentials Framework Versions -->
<!-- START Config Example -->
### Config Example

```json
{
    "key": "GeneratedKey",
    "uid": 1,
    "name": "GeneratedName",
    "type": "ChristieProjectorProperties",
    "group": "Group",
    "properties": {
        "pollIntervalMs": 0,
        "coolingTimeMs": "SampleValue",
        "warmingTimeMs": "SampleValue",
        "hasLamps": true,
        "hasScreen": true,
        "hasLift": true
    }
}
```
<!-- END Config Example -->
<!-- START Supported Types -->

<!-- END Supported Types -->
<!-- START Join Maps -->

<!-- END Join Maps -->
<!-- START Interfaces Implemented -->
### Interfaces Implemented

- ISelectableItems<string>
- ICommunicationMonitor
- IBridgeAdvanced
- IHasInputs<string>
- IRoutingSinkWithSwitchingWithInputPort
<!-- END Interfaces Implemented -->
<!-- START Base Classes -->
### Base Classes

- TwoWayDisplayBase
- DisplayControllerJoinMap
<!-- END Base Classes -->
<!-- START Public Methods -->
### Public Methods

- public void Select()
- public void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
- public void SendText(string cmd)
- public void StatusGet()
- public void PowerGet()
- public void InputHdmi1()
- public void InputHdmi2()
- public void InputSlot1()
- public void InputSlot2()
- public void InputDvi1()
- public void InputDisplayPort1()
- public void InputToggle()
- public void InputGet()
- public void UpdateInputFb(int input)
- public void LampGet()
- public void VideoMuteGet()
- public void VideoMuteOn()
- public void VideoMuteOff()
- public void VideoMuteToggle()
- public void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
- public void SendText(string cmd)
- public void StatusGet()
- public void PowerGet()
- public void InputHdmi1()
- public void InputHdmi2()
- public void InputHdbaseT()
- public void InputDisplayPort1()
- public void InputDisplayPort2()
- public void InputSdi1()
- public void InputSdi2()
- public void InputSdi3()
- public void InputSdi4()
- public void InputDigitalLink1()
- public void InputDigitalLink2()
- public void InputToggle()
- public void InputGet()
- public void UpdateInputFb(int input)
- public void LampGet()
- public void VideoMuteGet()
- public void VideoMuteOn()
- public void VideoMuteOff()
- public void VideoMuteToggle()
<!-- END Public Methods -->
<!-- START Bool Feedbacks -->
### Bool Feedbacks

- VideoMuteIsOnFeedback
- VideoMuteIsOnFeedback
<!-- END Bool Feedbacks -->
<!-- START Int Feedbacks -->
### Int Feedbacks

- CurrentInputNumberFeedback
- LampHoursFeedback
- CurrentInputNumberFeedback
- LampHoursFeedback
<!-- END Int Feedbacks -->
<!-- START String Feedbacks -->

<!-- END String Feedbacks -->
