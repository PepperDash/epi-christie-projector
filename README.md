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



