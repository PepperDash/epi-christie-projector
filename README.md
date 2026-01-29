# Crestron Christie Projector Plugin - Configuration Guide

Comprehensive power management, input control, and video mute functionality for Christie 4K25-RGB and 4K7-HS projectors. Integrates with Crestron Essentials for intelligent device-response-driven state management, real-time feedback, and safe power cycling with automatic warm-up and cool-down sequences.

---

<!-- START Minimum Essentials Framework Versions -->
### Minimum Essentials Framework Versions

- 2.5.1 (PepperDash Essentials)
<!-- END Minimum Essentials Framework Versions -->

---

<!-- START IMPORTANT -->
### ⚠️ IMPORTANT: Device-Response-Driven State Management

**Critical Power Management Behavior:**

The Christie projector plugin uses device feedback responses to manage power state transitions. The device returns specific response values that indicate its operational state:
- **PWR!00** = Power OFF (confirmed)
- **PWR!01** = Power ON (confirmed, fully warmed)
- **PWR!10** = Cooling Down (transition state - device is actively cooling)
- **PWR!11** = Warming Up (transition state - device is actively warming)

**Safety Features:**
- Minimum 30-second warm-up and cool-down periods are enforced (configurable, cannot be reduced below 30 seconds)
- Power commands sent during opposite state transitions are automatically queued and executed after the current transition completes
- Do NOT attempt to power on while device is warming, or power off while cooling - the plugin handles this automatically
- Device responses drive all state flags, not timers alone. Timers serve as safety fallbacks only.

**Example Safe Scenario:**
User presses PowerOn → Device responds PWR!11 (warming) → User immediately presses PowerOff → PowerOff queued automatically → Device responds PWR!01 (warmup complete) → Pending PowerOff automatically executes → Device responds PWR!10 (cooling) → Device responds PWR!00 (cooldown complete)

<!-- END IMPORTANT -->

---

<!-- START Supported Types -->
### Supported Types

**Device Types:**
- `christie4k7hsprojector` - Christie 4K7-HS Projector
- `christie4k25rgbprojector` - Christie 4K25-RGB Projector

Both devices support identical communication methods (RS-232 serial and TCP/Network) and feature sets.

<!-- END Supported Types -->

---

<!-- START Config Example -->
### Config Examples

#### TCP Configuration (Network)

```json
{
  "key": "roomA-projector",
  "uid": 1,
  "name": "Room A Christie 4K7-HS",
  "type": "christie4k7hsprojector",
  "group": "displays",
  "properties": {
    "control": {
      "method": "tcpIp",
      "tcpSshProperties": {
        "address": "192.168.1.100",
        "port": 3002,
        "username": "",
        "password": "",
        "autoReconnect": true,
        "autoReconnectIntervalMs": 5000
      }
    },
    "pollIntervalMs": 10000,
    "warmingTimeMs": 30000,
    "coolingTimeMs": 30000,
    "hasLamps": true,
    "hasScreen": false,
    "hasLift": false
  }
}
```
**Note:** Standard TCP/Network configuration for Christie projectors. Device IP address must be static. Port 3002 is the standard Christie serial-over-IP port. Poll interval minimum enforced at 10 seconds.

#### RS-232 Serial Configuration

```json
{
  "key": "conferenceRoom-projector",
  "uid": 2,
  "name": "Conference Christie 4K25-RGB",
  "type": "christie4k25rgbprojector",
  "group": "displays",
  "properties": {
    "control": {
      "method": "com",
      "controlPortNumber": 1,
      "comParams": {
        "baudRate": 9600,
        "dataBits": 8,
        "stopBits": 1,
        "parity": "None",
        "softwareHandshake": "None",
        "hardwareHandshake": "None",
        "protocol": "RS232"
      }
    },
    "pollIntervalMs": 10000,
    "warmingTimeMs": 30000,
    "coolingTimeMs": 30000,
    "hasLamps": true,
    "hasScreen": false,
    "hasLift": false
  }
}
```
**Note:** RS-232 serial configuration. Device connects to control processor serial port (typically 1-3 for CP4). Communication at 9600 baud, 8 data bits, no parity, 1 stop bit (device firmware fixed).

#### Multi-Device Configuration (Mixed)

```json
[
  {
    "key": "main-display",
    "uid": 10,
    "name": "Main Room Projector",
    "type": "christie4k7hsprojector",
    "group": "displays",
    "properties": {
      "control": {
        "method": "tcpIp",
        "tcpSshProperties": {
          "address": "192.168.1.100",
          "port": 3002,
          "username": "",
          "password": "",
          "autoReconnect": true,
          "autoReconnectIntervalMs": 5000
        }
      },
      "pollIntervalMs": 10000,
      "warmingTimeMs": 30000,
      "coolingTimeMs": 30000,
      "hasLamps": true,
      "hasScreen": false,
      "hasLift": false
    }
  },
  {
    "key": "backup-display",
    "uid": 11,
    "name": "Backup Room Projector",
    "type": "christie4k25rgbprojector",
    "group": "displays",
    "properties": {
      "control": {
        "method": "com",
        "controlPortNumber": 2,
        "comParams": {
          "baudRate": 9600,
          "dataBits": 8,
          "stopBits": 1,
          "parity": "None",
          "softwareHandshake": "None",
          "hardwareHandshake": "None",
          "protocol": "RS232"
        }
      },
      "pollIntervalMs": 10000,
      "warmingTimeMs": 30000,
      "coolingTimeMs": 30000,
      "hasLamps": true,
      "hasScreen": false,
      "hasLift": false
    }
  }
]
```
**Note:** Multi-device configuration with mixed communication methods (TCP for main, serial for backup). UIDs must be unique across all devices.

<!-- END Config Example -->

---

<!-- START Device Series Capabilities -->
### Device Series Capabilities

| Feature | 4K7-HS | 4K25-RGB | Notes |
|---------|--------|----------|-------|
| Power On/Off | ✓ | ✓ | Managed with 30-second warm-up/cool-down |
| Input Selection | ✓ | ✓ | HDMI, DigitalLink, and other inputs |
| Video Mute | ✓ | ✓ | Blanks display output (SHU command) |
| Power States | ✓ | ✓ | OFF, Warming, Fully On, Cooling |
| Lamp Hours | ✓ | ✓ | Feedback-only, not adjustable |
| Communication | TCP/Serial | TCP/Serial | RS-232 at 9600 baud or TCP port 3002 |
| Polling | 10s minimum | 10s minimum | Configurable, enforced minimum |
| Feedback Responses | PWR, SIN, SHU, ILI | PWR, SIN, SHU, ILI | Four primary device response types |

<!-- END Device Series Capabilities -->

---

<!-- START Core Properties -->
### Core Properties

| Property | Type | Required | Default | Description |
|----------|------|----------|---------|-------------|
| `key` | string | ✓ | - | Unique device identifier |
| `uid` | integer | ✓ | - | Essentials system UID (must be unique) |
| `name` | string | ✓ | - | Device display name |
| `type` | string | ✓ | - | Device type (`christie4k7hsprojector` or `christie4k25rgbprojector`) |
| `group` | string | ✓ | - | Device grouping (e.g., `"displays"`, `"projectors"`) |
| `control` | object | ✓ | - | Communication control configuration (TCP or Serial) |
| `pollIntervalMs` | long | ✗ | 10000 | Status poll interval in milliseconds (minimum 10000) |
| `warmingTimeMs` | long | ✗ | 30000 | Device warm-up period in milliseconds (minimum 30000) |
| `coolingTimeMs` | long | ✗ | 30000 | Device cool-down period in milliseconds (minimum 30000) |
| `hasLamps` | bool | ✗ | false | Device has lamp indicator for feedback purposes |
| `hasScreen` | bool | ✗ | false | Device has screen for feedback purposes |
| `hasLift` | bool | ✗ | false | Device has motorized lift for feedback purposes |

<!-- END Core Properties -->

---

### Property Details

**Core Configuration:**

- **`key`:** Unique device identifier within the system. Used for device reference in routing and control logic. Examples: `"roomA-projector"`, `"main-display"`, `"backup-display"`.

- **`uid`:** Essentials system UID. Must be unique across all devices in the system. Critical for device communication and feedback routing. Range: 1-65535. Example: `1`, `10`, `100`.

- **`name`:** Device name displayed in the control system UI and used internally. Should be descriptive (e.g., `"Room A Christie 4K7-HS"`, `"Main Conference Projector"`).

- **`type`:** Device type identifier. Must be one of:
  - `"christie4k7hsprojector"` for Christie 4K7-HS models
  - `"christie4k25rgbprojector"` for Christie 4K25-RGB models

- **`group`:** Logical grouping category for device organization. Recommended values: `"displays"`, `"projectors"`, `"presentation"`.

**Communication Configuration:**

- **`control.method`:** Communication method type:
  - `"tcpIp"` for TCP/Network connection to device
  - `"com"` for RS-232 serial connection

- **`control.tcpSshProperties`:** (TCP only) Network communication settings:
  - `"address"`: Device IP address (must be static)
  - `"port"`: Device port (typically 3002 for Christie serial-over-IP)

- **`control.controlPortNumber`:** (Serial only) Control processor's serial port number:
  - Typical range: 1-3 for CP4
  - Verify with system configuration for your processor

- **`control.comParams`:** (Serial only) Serial communication parameters (all values fixed for Christie devices):
  - `"baudRate"`: Must be `9600` (device firmware fixed)
  - `"dataBits"`: Must be `8`
  - `"stopBits"`: Must be `1`
  - `"parity"`: Must be `"None"`
  - `"softwareHandshake"`: Must be `"None"`
  - `"hardwareHandshake"`: Must be `"None"`
  - `"protocol"`: Must be `"RS232"`

**Monitoring & Timing:**

- **`pollIntervalMs`:** How often (in milliseconds) the system queries device status.
  - **Minimum enforced:** 10000 ms (10 seconds)
  - **Recommended:** 10000 ms for normal operation
  - **Lower is more responsive but consumes more bandwidth**
  - Values below 10000 ms will be forced to 10000 ms by the plugin

- **`warmingTimeMs`:** Duration (in milliseconds) to wait for device to complete power-on warm-up cycle.
  - **Minimum enforced:** 30000 ms (30 seconds)
  - **Must match actual device warm-up time:** Real Christie projectors require ~30 seconds to fully warm up
  - **Recommended:** 30000 ms (do not increase unless device specifically requires longer)
  - **Safety:** Commands cannot be sent to device during warm-up period
  - Values below 30000 ms will be forced to 30000 ms by the plugin

- **`coolingTimeMs`:** Duration (in milliseconds) to wait for device to complete power-off cool-down cycle.
  - **Minimum enforced:** 30000 ms (30 seconds)
  - **Must match actual device cool-down time:** Real Christie projectors require ~30 seconds to fully cool down
  - **Recommended:** 30000 ms (do not increase unless device specifically requires longer)
  - **Safety:** Commands cannot be sent to device during cool-down period
  - Values below 30000 ms will be forced to 30000 ms by the plugin

**Device Capabilities:**

- **`hasLamps`:** Boolean indicating whether the device has lamp indicators. Used for bridge feedback purposes (true if device reports lamp hours).

- **`hasScreen`:** Boolean indicating whether the device has a screen. Used for bridge feedback purposes.

- **`hasLift`:** Boolean indicating whether the device has a motorized lift. Used for bridge feedback purposes.

---

<!-- START Join Maps -->
### Join Maps

#### Digitals

| Join | Direction | Description |
|------|-----------|-------------|
| 1 | R | Is Online (Communication Status) |
| 2 | R/W | Power On / Power On Feedback |
| 3 | R/W | Power Off / Power Off Feedback |
| 4 | R | Power Toggle |
| 5 | R | Is Warming Up Feedback |
| 6 | R | Is Cooling Down Feedback |
| 11 | R/W | Input HDMI 1 Select / Feedback |
| 12 | R/W | Input HDMI 2 Select / Feedback |
| 13 | R/W | Input DigitalLink Select / Feedback |
| 14 | R/W | Input DVI-D Select / Feedback |
| 15 | R/W | Input DisplayPort Select / Feedback |
| 16 | R/W | Input SDI 1 Select / Feedback |
| 17 | R/W | Input SDI 2 Select / Feedback |
| 18 | R/W | Input SDI 3 Select / Feedback |
| 19 | R/W | Input SDI 4 Select / Feedback |
| 20 | R/W | Input USB Select / Feedback |
| 21 | R/W | Input Network Select / Feedback |
| 31 | R | Has Lamps (Device Capability Feedback) |
| 32 | R | Has Screen (Device Capability Feedback) |
| 33 | R | Has Lift (Device Capability Feedback) |
| 51 | R/W | Video Mute On / Video Mute On Feedback |
| 52 | R/W | Video Mute Off / Video Mute Off Feedback |
| 53 | R/W | Video Mute Toggle / Video Mute State |

#### Analogs

| Join | Direction | Description |
|------|-----------|-------------|
| 1 | R | Communication Status (0=Online, 1=Warning, 2=Error) |
| 2 | R | Current Input Selection (1-11 for input number) |
| 3 | R | Lamp Hours (Read-only feedback) |
| 4 | W | Input Select Command (1-11 to switch input) |

#### Serials

| Join | Direction | Description |
|------|-----------|-------------|
| 1 | R | Device Name |
| 2 | R | Current Input Name (String feedback) |

<!-- END Join Maps -->

---

### Join Details

**Communication & Status:**

- **1 (Is Online):** Device online status feedback. True = device is responding and communication is healthy. False = device offline or communication failure detected.

- **Communication Status (Analog 1):** Numeric communication status: 0 = Online (healthy), 1 = Warning (degraded), 2 = Error (offline).

**Power Control:**

- **2 (Power On):** Power on control and feedback. Write high to trigger power-on sequence. Reads true when device is powered on or warming up.

- **3 (Power Off):** Power off control and feedback. Write high to trigger power-off sequence. Reads true when device is powered off or cooling down.

- **4 (Power Toggle):** Bidirectional power toggle. Write high to toggle current power state.

- **5 (Is Warming Up):** Read-only feedback. True when device is in warm-up sequence (PWR!11 response received). Use to display "Warming..." indicator in UI.

- **6 (Is Cooling Down):** Read-only feedback. True when device is in cool-down sequence (PWR!10 response received). Use to display "Cooling..." indicator in UI.

**Input Selection:**

- **11-21 (Input Select):** Digital joins for each input type (HDMI1, HDMI2, DigitalLink, DVI-D, DisplayPort, SDI 1-4, USB, Network). Write high to select input. Reads true when that input is currently active.

- **Analog 2 (Current Input):** Numeric input selection feedback. Values 1-11 corresponding to each input type.

- **Analog 4 (Input Select Command):** Write analog value 1-11 to switch input via analog command.

- **Serial 2 (Input Name):** String feedback showing current input name (e.g., "HDMI 1", "DigitalLink").

**Device Capabilities:**

- **31 (Has Lamps):** Device capability feedback. True if device reports lamp hour capability.

- **32 (Has Screen):** Device capability feedback. True if device has motorized screen.

- **33 (Has Lift):** Device capability feedback. True if device has motorized lift.

- **Analog 3 (Lamp Hours):** Read-only numeric feedback displaying cumulative lamp operating hours.

**Video Mute:**

- **51 (Video Mute On):** Video mute control and feedback. Write high to blank display. Reads true when mute is active.

- **52 (Video Mute Off):** Video mute off control and feedback. Write high to show display. Reads true when video is visible.

- **53 (Video Mute Toggle):** Toggle video mute state. Write high to switch between mute on/off.

**Device Information:**

- **Serial 1 (Device Name):** String feedback with configured device name (from config file).

---

<!-- START Interfaces Implemented -->
### Interfaces Implemented

- `ITwoWayDisplayWithAudio` - Two-way display control interface (base for all power/input/mute methods)
- `IOnline` - Online status feedback interface
- `ICommunicationMonitor` - Communication monitoring and error detection
- `IBridgeAdvanced` - Advanced bridge support for EISC API integration

<!-- END Interfaces Implemented -->

---

<!-- START Base Classes -->
### Base Classes

**Device Base Classes:**
- `TwoWayDisplayBase` - Base class for two-way display devices with power, input, and mute control

**Communication & Monitoring:**
- `CommunicationGather` - Serial message gathering and parsing (accumulates data until delimiter found)
- `GenericCommunicationMonitor` - Communication health monitoring with online/offline/error state tracking
- `IBasicCommunication` - Core communication interface abstraction

**Bridge & Join Mapping:**
- `DisplayControllerJoinMap` - Base join map for display control devices
- `ChristieProjectorBridgeJoinMap` - Christie-specific join map extending DisplayControllerJoinMap

**Device Management:**
- `DeviceManager` - Essentials device registry and lifecycle management
- `EssentialsPluginDeviceFactory<T>` - Base factory for creating plugin devices

<!-- END Base Classes -->

---

<!-- START Routing Framework -->
### Routing Framework & Architecture

**State Management Architecture:**

The Christie projector plugin implements a device-response-driven state machine that prioritizes actual device feedback over timer-based assumptions.

**Power State Flow:**

```
User Action
    ↓
PowerOn()/PowerOff() method
    ├─ Check current state flags
    ├─ Apply guards (skip duplicate, queue conflicting)
    └─ Send command to device
         ↓
Device responds with PWR!xx
    ↓
ProcessResponse() receives value
    ├─ PWR!11 (Warming): Set IsWarmingUp=true
    ├─ PWR!10 (Cooling): Set IsCoolingDown=true
    ├─ PWR!01 (ON confirmed): Clear IsWarmingUp, execute pending PowerOff if queued
    └─ PWR!00 (OFF confirmed): Clear IsCoolingDown, execute pending PowerOn if queued
         ↓
Timers act as safety fallback (25s expiry)
```

**Command Queuing:**

- All control commands (PWR, SIN, SHU) are queued in `_commandQueue` (generic Queue<string>)
- Query commands (PWR?, SIN?, SHU?) bypass queue and execute immediately
- Queue processes with 100ms minimum send interval to prevent device overload
- No queue blocking on state flags - conflicting commands are handled by PowerOn/PowerOff guard clauses

**Guard Clause Logic:**

| Method | Current State | Action |
|--------|---------------|--------|
| PowerOn() | IsWarmingUp=true | Return (skip) - already warming |
| PowerOn() | IsCoolingDown=true | Set _pendingPowerOn=true, return (queue) |
| PowerOn() | Idle | Send (PWR1), set IsWarmingUp=true |
| PowerOff() | IsCoolingDown=true | Return (skip) - already cooling |
| PowerOff() | IsWarmingUp=true | Set _pendingPowerOff=true, return (queue) |
| PowerOff() | Idle | Send (PWR0), set IsCoolingDown=true |

**Pending Command Execution:**

When device reaches confirmed state:
- If `_pendingPowerOn=true` after PWR!00: Automatically call PowerOn()
- If `_pendingPowerOff=true` after PWR!01: Automatically call PowerOff()
- Prevents manual queuing by developer - happens automatically in ProcessResponse()

**Communication Protocol:**

- **Outbound:** Commands in format `(CMD)` or `(CMD n)` (e.g., `(PWR1)`, `(SIN11)`)
- **Inbound:** Responses in format `(CMD!nn)` (e.g., `(PWR!01)`, `(SIN!04)`)
- **Delimiter:** Line feed (`\n`) terminates responses for parsing
- **Polling:** StatusGet() sends `(PWR?)`, `(SIN?)`, `(SHU?)` queries on configured interval

**Response Value Meanings:**

- PWR: 0=OFF, 1=ON (fully warmed), 10=Cooling Down, 11=Warming Up
- SIN: 1-11 (input selection)
- SHU: 0=Video On, 1=Video Mute
- ILI: Lamp hours (numeric, read-only)

<!-- END Routing Framework -->

---

<!-- START Configuration Best Practices -->
### Configuration Best Practices

**Communication Setup:**

- **TCP Configuration:**
  - Verify device IP address is static and accessible from control processor
  - Confirm port 3002 is open and device is listening
  - Test connectivity with ping before deploying config: `ping 192.168.x.x`
  - Document IP address and port for maintenance

- **Serial Configuration:**
  - Identify correct COM port on control processor (typically 1-3 for CP4)
  - Verify COM port is available (not in use by other devices)
  - Serial parameters are fixed by device firmware (9600 baud, 8N1)
  - Test connectivity using terminal utility before final deployment
  - Use serial port labels that match physical control processor ports

**Device Configuration:**

- Use unique, descriptive device keys: `"roomA-projector"`, `"main-display"`, NOT `"proj1"` or `"display"`
- Assign UIDs sequentially for easy reference: 1, 2, 3 or 10, 11, 12 (avoid random numbers)
- Group devices logically: `"displays"`, `"projectors"`, `"presentation-equipment"`
- Set `hasLamps`, `hasScreen`, `hasLift` to match actual device capabilities (for accurate feedback)

**Timing Configuration:**

- **Warming Time:** Must be ≥ 30000 ms to allow device full warm-up cycle
- **Cooling Time:** Must be ≥ 30000 ms to allow device full cool-down cycle
- **Poll Interval:** Minimum 10000 ms; higher intervals reduce network traffic but increase feedback latency
- **Never modify minimum values** - they are enforced by device firmware and safety requirements

**Production Deployment Checklist:**

- [ ] Device IP address is static (for TCP) or COM port is correct (for serial)
- [ ] Device is powered on and accessible before starting Essentials
- [ ] Configuration UIDs are unique across all devices
- [ ] Warming and cooling times set to minimum 30000 ms
- [ ] Poll interval configured (default 10000 ms is recommended)
- [ ] Test power on: Should see PWR!11 (warming), then PWR!01 (on)
- [ ] Test power off: Should see PWR!10 (cooling), then PWR!00 (off)
- [ ] Test input selection: Should see SIN response with correct value
- [ ] Test video mute: Should see SHU response changing between 0 and 1
- [ ] Verify online status feedback transitions to "Online" after 10-15 seconds
- [ ] Check lamp hours feedback displays current value (if applicable)

**Common Pitfalls to Avoid:**

- ❌ Setting warm/cool times below 30000 ms (will be forced up by plugin)
- ❌ Using dynamic IP addresses for TCP connections (must be static)
- ❌ Duplicate UIDs across devices (causes device identification conflicts)
- ❌ Sending power commands more frequently than 100 ms (queue throttling prevents damage)
- ❌ Assuming immediate power transitions (device responds with transition states 11 and 10)
- ❌ Ignoring IsWarmingUp/IsCoolingDown feedback in UI (should display "Warming..." / "Cooling...")

<!-- END Configuration Best Practices -->

---

### Bool Feedbacks

- `PowerIsOn` - Device power on state (true when fully powered on)
- `IsWarmingUp` - Device warming up state (true during PWR!11 transition)
- `IsCoolingDown` - Device cooling down state (true during PWR!10 transition)
- `IsOnline` - Device online/communication status
- `VideoMuteIsOn` - Video mute state (true when display is blanked)
- `HasLamps` - Device has lamp indicator capability
- `HasScreen` - Device has screen capability
- `HasLift` - Device has lift capability

---

### Int Feedbacks

- `CommunicationMonitor.Status` - Communication status (0=Online, 1=Warning, 2=Error)
- `CurrentInputNumber` - Current input selection (1-11)
- `LampHours` - Lamp operating hours (read-only)

---

### String Feedbacks

- `Name` - Device name
- `CurrentInput` - Current input name string (e.g., "HDMI 1", "DigitalLink")

---

### Public Methods

**Power Control:**
- `PowerOn()` - Initiate power-on sequence (handles warm-up automatically)
- `PowerOff()` - Initiate power-off sequence (handles cool-down automatically)
- `PowerToggle()` - Toggle between on and off states

**Input Selection:**
- `SetInput(uint input)` - Select input by number (1-11)
- `InputHdmi1()` through `InputDigitalLink2()` - Select specific input by method

**Video Mute:**
- `VideoMuteOn()` - Blank display output
- `VideoMuteOff()` - Show display output
- `VideoMuteToggle()` - Toggle video mute state

**Status & Polling:**
- `PowerGet()` - Poll current power status (sends PWR? query)
- `StatusGet()` - Poll all device status (powers, input, mute, lamp hours)
- `VideoMuteGet()` - Poll video mute status (sends SHU? query)

**Bridge Integration:**
- `LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)` - Link device to EISC API bridge

---

### Architecture Overview

**Core Device Class Hierarchy:**

```
TwoWayDisplayBase
    ↓
Christie4K7HsController / Christie4K25RgbController
    ├─ Implements: ITwoWayDisplayWithAudio, IOnline, ICommunicationMonitor, IBridgeAdvanced
    ├─ Uses: CommunicationGather, GenericCommunicationMonitor
    ├─ Commands: Power (PWR), Input Select (SIN), Video Mute (SHU), Lamp Hours (ILI)
    └─ State Management: Warm-up/Cool-down flags, Pending command flags, 100ms throttling
```

**Data Flow:**

1. User action → PowerOn()/SetInput()/VideoMuteOn() methods
2. Guard clauses check state → Send command or queue/skip
3. Command queued in `_commandQueue` with 100ms throttling
4. Device receives command and responds
5. CommunicationGather buffers response until `\n` delimiter
6. ProcessResponse() parses response and updates feedbacks
7. State machine executes pending commands if transition complete
8. Feedbacks update bridge automatically

**Thread Safety:**

- Command queue uses `_sendLock` (object) for thread-safe enqueue/dequeue operations
- All state flags (IsWarmingUp, IsCoolingDown) are accessed safely within lock context
- Timer callbacks execute on Essentials thread pool (safe for state updates)

---

**Generated:** January  28, 2026  
**Framework Version:** PepperDash Essentials 2.5.1+  
**Plugin Version:** 1.0.0  
**Methodology:** SOURCE-FIRST EXTRACTION per COPILOT_README_PROMPTS.md
