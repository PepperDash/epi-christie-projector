using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.CrestronThread;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Core.Logging;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.Queues;
using PepperDash.Essentials.Devices.Common.Displays;
using PepperDash.Essentials.Devices.Displays;

namespace ChristieProjectorPlugin
{
	/// <summary>
	/// Controller for Christie 4K7-HS projector providing two-way communication,
	/// input routing, power management, and video mute functionality
	/// </summary>
	public class Christie4K7HsController : TwoWayDisplayBase, ICommunicationMonitor,
		IInputHdmi1, IInputHdmi2, IInputHdmi4, IInputDisplayPort1, IBridgeAdvanced
	{

		private bool _isSerialComm;
		private bool HasLamps { get; set; }
		private bool HasScreen { get; set; }
		private bool HasLift { get; set; }

		/// <summary>
		/// Initializes a new instance of the Christie4K7HsController class
		/// </summary>
		/// <param name="key">Unique identifier for this device instance</param>
		/// <param name="name">Human-readable name for this device</param>
		/// <param name="config">Configuration properties specific to Christie projectors</param>
		/// <param name="comms">Communication interface for device connectivity</param>
		public Christie4K7HsController(string key, string name, ChristieProjectorPropertiesConfig config, IBasicCommunication comms)
			: base(key, name)
		{
			var props = config;
			if (props == null)
			{
				this.LogInformation("Configuration must be included");
				return;
			}

			LampHoursFeedback = new IntFeedback(() => LampHours);

			Communication = comms;

			_receiveQueue = new GenericQueue(key + "-queue");

			CommunicationGather = new CommunicationGather(Communication, GatherDelimiter);
			CommunicationGather.LineReceived += OnCommunicationGatherLineReceived;


			_isSerialComm = !(Communication is ISocketStatus socket);

			var pollIntervalMs = props.PollIntervalMs > 45000 ? props.PollIntervalMs : 45000;
			CommunicationMonitor = new GenericCommunicationMonitor(this, Communication, pollIntervalMs, 180000, 300000,
				StatusGet);

			CommunicationMonitor.StatusChange += OnCommunicationMonitorStatusChange;

			DeviceManager.AddDevice(CommunicationMonitor);

			VideoMuteIsOnFeedback = new BoolFeedback(() => VideoMuteIsOn);

			WarmupTime = props.WarmingTimeMs > 30000 ? props.WarmingTimeMs : 30000;
			CooldownTime = props.CoolingTimeMs > 30000 ? props.CoolingTimeMs : 30000;

			HasLamps = props.HasLamps;
			HasScreen = props.HasScreen;
			HasLift = props.HasLift;

			InitializeInputs();
		}


		/// <summary>
		/// Initializes the device by establishing communication connection and starting the communication monitor
		/// </summary>
		public override void Initialize()
		{
			Communication.Connect();
			CommunicationMonitor.Start();
		}

		#region IBridgeAdvanced Members

		/// <summary>
		/// Links the device to the EISC API bridge, configuring all join mappings for power, input, and video mute controls
		/// </summary>
		/// <param name="trilist">The BasicTriList device (EISC) to link to</param>
		/// <param name="joinStart">The starting join number for this device's join map</param>
		/// <param name="joinMapKey">The key for custom join map configuration</param>
		/// <param name="bridge">The EISC API advanced bridge instance</param>
		public void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
		{
			var joinMap = new ChristieProjectorBridgeJoinMap(joinStart);

			// This adds the join map to the collection on the bridge
			bridge?.AddJoinMap(Key, joinMap);

			var customJoins = JoinMapHelper.TryGetJoinMapAdvancedForDevice(joinMapKey);
			if (customJoins != null)
			{
				joinMap.SetCustomJoinData(customJoins);
			}

			this.LogInformation("Linking to Trilist '{ipId:X}'", trilist.ID.ToString("X"));

			// links to bridge
			// device name
			trilist.SetString(joinMap.Name.JoinNumber, Name);

			//var twoWayDisplay = this as TwoWayDisplayBase;
			//trilist.SetBool(joinMap.IsTwoWayDisplay.JoinNumber, twoWayDisplay != null);

			// lamp, screen, lift config outputs
			trilist.SetBool(joinMap.HasLamps.JoinNumber, HasLamps);
			trilist.SetBool(joinMap.HasScreen.JoinNumber, HasScreen);
			trilist.SetBool(joinMap.HasLift.JoinNumber, HasLift);

			CommunicationMonitor?.IsOnlineFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);

			// power off & is cooling
			trilist.SetSigTrueAction(joinMap.PowerOff.JoinNumber, PowerOff);
			PowerIsOnFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.PowerOff.JoinNumber]);
			IsCoolingDownFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsCooling.JoinNumber]);

			// power on & is warming
			trilist.SetSigTrueAction(joinMap.PowerOn.JoinNumber, PowerOn);
			PowerIsOnFeedback.LinkInputSig(trilist.BooleanInput[joinMap.PowerOn.JoinNumber]);
			IsWarmingUpFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsWarming.JoinNumber]);

			// input (digital select, digital feedback, names)
			for (var i = 0; i < InputPorts.Count; i++)
			{
				var inputIndex = i;
				var input = InputPorts.ElementAt(inputIndex);

				if (input == null) continue;

				trilist.SetSigTrueAction((ushort)(joinMap.InputSelectOffset.JoinNumber + inputIndex), () =>
				{
					this.LogVerbose("InputSelect Digital-{inputIndex}", inputIndex + 1);
					SetInput = inputIndex + 1;
				});

				trilist.StringInput[(ushort)(joinMap.InputNamesOffset.JoinNumber + inputIndex)].StringValue = GetInputNameForKey(input.Key);

				InputFeedback[inputIndex].LinkInputSig(trilist.BooleanInput[joinMap.InputSelectOffset.JoinNumber + (uint)inputIndex]);
			}

			// input (analog select)
			trilist.SetUShortSigAction(joinMap.InputSelect.JoinNumber, inputNumber =>
			{
				this.LogDebug("InputSelect Analog-{inputValue}", inputNumber);
				SetInput = inputNumber;
			});

			// input (analog feedback)
			CurrentInputNumberFeedback?.LinkInputSig(trilist.UShortInput[joinMap.InputSelect.JoinNumber]);

			if (CurrentInputFeedback != null)
				CurrentInputFeedback.OutputChange += (sender, args) => this.LogDebug("CurrentInputFeedback: {currentInput}", args.StringValue);

			// lamp hours feedback
			LampHoursFeedback.LinkInputSig(trilist.UShortInput[joinMap.LampHours.JoinNumber]);

			// video mute
			trilist.SetSigTrueAction(joinMap.VideoMuteOn.JoinNumber, VideoMuteOn);
			trilist.SetSigTrueAction(joinMap.VideoMuteOff.JoinNumber, VideoMuteOff);
			trilist.SetSigTrueAction(joinMap.VideoMuteToggle.JoinNumber, VideoMuteToggle);
			VideoMuteIsOnFeedback.LinkInputSig(trilist.BooleanInput[joinMap.VideoMuteOn.JoinNumber]);
			VideoMuteIsOnFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.VideoMuteOff.JoinNumber]);

			// bridge online change
			trilist.OnlineStatusChange += (sender, args) =>
			{
				if (!args.DeviceOnLine) return;

				// device name
				trilist.SetString(joinMap.Name.JoinNumber, Name);
				// lamp, screen, lift config outputs
				trilist.SetBool(joinMap.HasLamps.JoinNumber, HasLamps);
				trilist.SetBool(joinMap.HasScreen.JoinNumber, HasScreen);
				trilist.SetBool(joinMap.HasLift.JoinNumber, HasLift);

				PowerIsOnFeedback.FireUpdate();

				CurrentInputFeedback?.FireUpdate();
				CurrentInputNumberFeedback?.FireUpdate();

				for (var i = 0; i < InputPorts.Count; i++)
				{
					var inputIndex = i;
					InputFeedback?[inputIndex].FireUpdate();
				}

				LampHoursFeedback.FireUpdate();
			};
		}


		#endregion



		#region ICommunicationMonitor Members

		/// <summary>
		/// Gets the basic communication object used for device communication
		/// </summary>
		public IBasicCommunication Communication { get; private set; }

		/// <summary>
		/// Gets the communication gather object for parsing incoming messages
		/// </summary>
		public CommunicationGather CommunicationGather { get; private set; }

		/// <summary>
		/// Gets the communication status monitor for tracking connection health
		/// </summary>
		public StatusMonitorBase CommunicationMonitor { get; private set; }

		private const string GatherDelimiter = @"\)";

		private readonly GenericQueue _receiveQueue;

		private void OnCommunicationMonitorStatusChange(object sender, MonitorStatusChangeEventArgs args)
		{
			CommunicationMonitor.IsOnlineFeedback.FireUpdate();
		}

		private void OnCommunicationGatherLineReceived(object sender, GenericCommMethodReceiveTextArgs args)
		{
			try
			{
				this.LogVerbose("OnCommunicationGatherLineReceived: args.Text-'{0}'", args.Text);
				_receiveQueue.Enqueue(new ProcessStringMessage(args.Text, ProcessResponse));
			}
			catch (Exception ex)
			{
				this.LogError(ex, "Exception in OnCommunicationGatherLineReceived");
			}
		}

		#endregion


		// TODO [ ] Update based on device responses
		private void ProcessResponse(string response)
		{
			if (string.IsNullOrEmpty(response)) return;
			if (!response.Contains("!")) return;

			this.LogVerbose("ProcessResponse: {response}", response);

			var responseValue = 0;
			var responseType = "";
			try
			{

				var pattern = new Regex(@"\((?<command>[^!]+)!(?<value>\d+)(?: ""(?<data>.+?)"")?", RegexOptions.None);
				var match = pattern.Match(response);
				responseType = match.Groups["command"].Value;
				var responseString = match.Groups["value"].Value;
				string responseData = match.Groups["data"].Value;
				responseValue = int.Parse(responseString);

			}
			catch (Exception ex)
			{
				this.LogError(ex, "ProcessResponse exception");
			}

			this.LogVerbose("ProcessResponse: responseType-{responseType}, responseValue-{responseValue}", responseType, responseValue);

			switch (responseType)
			{
				case "PWR":
					{
						PowerIsOn = responseValue == 1;
						break;
					}
				case "ILI":
					{
						// TODO [ ] Verify feedback with actual device, not in api doc
						LampHours = responseValue;
						break;
					}
				case "SIN+MAIN":
					{
						UpdateInputFb(responseValue);
						break;
					}
				case "SHU":
					{
						VideoMuteIsOn = responseValue == 1;
						break;
					}
				default:
					{
						this.LogVerbose("ProcessResponse: unknown response {responseType}", responseType);
						break;
					}
			}
		}

		/// <summary>
		/// Sends commands to the projector, adding the proper delimiter format
		/// </summary>
		/// <param name="cmd">The command string to send to the device</param>
		public void SendText(string cmd)
		{
			if (string.IsNullOrEmpty(cmd)) return;

			if (!Communication.IsConnected)
			{
				this.LogWarning("SendText: device not connected");
				return;
			}

			var text = string.Format("({0})", cmd);

			Communication.SendText(text);
		}

		// formats outgoing message
		private void SendText(string cmd, string value)
		{
			var text = string.IsNullOrEmpty(value)
				? "?"
				: string.Format("({0}{1})", cmd, value);
			Communication.SendText(text);
		}

		// formats outgoing message
		private void SendText(string cmd, int value)
		{
			// tx format: "({cmd}{value})"
			Communication.SendText(string.Format("({0}{1})", cmd, value));
		}

		/// <summary>
		/// Executes a switch operation, turning on the display if necessary before switching inputs
		/// </summary>
		/// <param name="selector">The input selector action to execute</param>
		public override void ExecuteSwitch(object selector)
		{
			if (PowerIsOn)
			{
				(selector as Action)?.Invoke();
			}
			else // if power is off, wait until we get on FB to send it. 
			{
				// One-time event handler to wait for power on before executing switch
				EventHandler<FeedbackEventArgs> handler = null; // necessary to allow reference inside lambda to handler
				handler = (sender, args) =>
				{
					if (IsWarmingUp) return;

					IsWarmingUpFeedback.OutputChange -= handler;


					if (selector is Action action)
					{
						CrestronInvoke.BeginInvoke(o => action());
					}
				};
				IsWarmingUpFeedback.OutputChange += handler; // attach and wait for on FB
				PowerOn();
			}
		}


		/// <summary>
		/// Initiates the status polling sequence to get current device state
		/// </summary>
		public void StatusGet()
		{
			PowerGet();

			if (!PowerIsOn) return;

			CrestronEnvironment.Sleep(2000);
			InputGet();

			if (!HasLamps) return;

			CrestronEnvironment.Sleep(2000);
			LampGet();
		}




		#region Power

		private bool _isCoolingDown;
		private bool _isWarmingUp;
		private bool _powerIsOn;


		/// <summary>
		/// Gets or sets the power state of the projector
		/// </summary>
		public bool PowerIsOn
		{
			get { return _powerIsOn; }
			set
			{
				if (_powerIsOn == value)
				{
					return;
				}

				_powerIsOn = value;
				PowerIsOnFeedback.FireUpdate();
			}
		}

		/// <summary>
		/// Gets or sets whether the projector is currently warming up
		/// </summary>
		public bool IsWarmingUp
		{
			get { return _isWarmingUp; }
			set
			{
				_isWarmingUp = value;
				IsWarmingUpFeedback.FireUpdate();

				if (_isWarmingUp)
				{
					WarmupTimer = new CTimer(t =>
					{
						_isWarmingUp = false;
						IsWarmingUpFeedback.FireUpdate();
					}, WarmupTime);
				}
			}
		}

		/// <summary>
		/// Gets or sets whether the projector is currently cooling down
		/// </summary>
		public bool IsCoolingDown
		{
			get { return _isCoolingDown; }
			set
			{
				_isCoolingDown = value;
				IsCoolingDownFeedback.FireUpdate();

				if (_isCoolingDown)
				{
					CooldownTimer = new CTimer(t =>
					{
						_isCoolingDown = false;
						IsCoolingDownFeedback.FireUpdate();
					}, CooldownTime);
				}
			}
		}

		protected override Func<bool> PowerIsOnFeedbackFunc
		{
			get { return () => PowerIsOn; }
		}

		protected override Func<bool> IsCoolingDownFeedbackFunc
		{
			get { return () => IsCoolingDown; }
		}

		protected override Func<bool> IsWarmingUpFeedbackFunc
		{
			get { return () => IsWarmingUp; }
		}

		/// <summary>
		/// Powers on the projector and initiates the warming sequence
		/// </summary>
		public override void PowerOn()
		{
			if (IsWarmingUp || IsCoolingDown) return;

			if (PowerIsOn == false) IsWarmingUp = true;

			SendText("PWR", 1);

			Thread.Sleep(1500);

			PowerGet();

		}

		/// <summary>
		/// Powers off the projector and initiates the cooling sequence
		/// </summary>
		public override void PowerOff()
		{
			if (IsWarmingUp || IsCoolingDown) return;

			if (PowerIsOn == true) IsCoolingDown = true;

			SendText("PWR", 0);


			Thread.Sleep(50);

			PowerGet();

		}

		/// <summary>
		/// Polls the projector for current power status
		/// </summary>
		public void PowerGet()
		{
			SendText("PWR", "?");

		}


		/// <summary>
		/// Toggles the current power state of the projector (on to off, or off to on)
		/// </summary>
		public override void PowerToggle()
		{
			if (PowerIsOn)
			{
				PowerOff();
			}
			else
			{
				PowerOn();
			}
		}

		#endregion



		#region Inputs

		/// <summary>
		/// Constant representing the input power on state value
		/// </summary>
		public const int InputPowerOn = 101;

		/// <summary>
		/// Constant representing the input power off state value
		/// </summary>
		public const int InputPowerOff = 102;

		/// <summary>
		/// Static list of available input keys for the projector
		/// </summary>
		public static List<string> InputKeys = new List<string>();

		/// <summary>
		/// List of boolean feedback objects for digital input selection
		/// </summary>
		public List<BoolFeedback> InputFeedback;

		/// <summary>
		/// Integer feedback object for analog input number reporting
		/// </summary>
		public IntFeedback CurrentInputNumberFeedback;

		private RoutingInputPort _currentInputPort;

		protected override Func<string> CurrentInputFeedbackFunc
		{
			get { return () => _currentInputPort != null ? _currentInputPort.Key : string.Empty; }
		}

		private List<bool> _inputFeedback;
		private int _currentInputNumber;

		/// <summary>
		/// Gets or sets the current input number (1-based index)
		/// </summary>
		public int CurrentInputNumber
		{
			get { return _currentInputNumber; }
			private set
			{
				_currentInputNumber = value;
				CurrentInputNumberFeedback.FireUpdate();
				UpdateInputBooleanFeedback();
			}
		}

		/// <summary>
		/// Sets the active input by number (1-based index). Validates range and executes the input switch.
		/// </summary>
		public int SetInput
		{
			set
			{
				if (value <= 0 || value > InputPorts.Count)
				{
					this.LogError("SetInput: value-{inputValue} is out of range (1 - {inputPortsCount})", value, InputPorts.Count);
					return;
				}

				this.LogDebug("SetInput: value-{input}", value);

				// -1 to get actual input in list after 0d check
				var port = GetInputPort(value - 1);
				if (port == null)
				{
					this.LogWarning("SetInput: failed to get input port");
					return;
				}

				ExecuteSwitch(port.Selector);
			}
		}

		private RoutingInputPort GetInputPort(int input)
		{
			return InputPorts.ElementAt(input);
		}

		private string GetInputNameForKey(string key)
		{
			switch (key)
			{
				case "hdmiIn1":
					return "HDMI 1";
				case "hdmiIn2":
					return "HDMI 2";
				case "dviIn1":
					return "DVI 1";
				case "displayPortIn1":
					return "Display Port 1";
				case "hdmiIn4":
					return "Slot 1";
				case "hdmiIn5":
					return "Slot 2";
				default:
					return string.Empty;
			}
		}

		private void AddRoutingInputPort(RoutingInputPort port, int fbMatch)
		{
			port.FeedbackMatchObject = fbMatch;
			InputPorts.Add(port);
		}

		private void InitializeInputs()
		{
			AddRoutingInputPort(
				new RoutingInputPort(RoutingPortNames.HdmiIn1, eRoutingSignalType.Audio | eRoutingSignalType.Video,
					eRoutingPortConnectionType.Hdmi, new Action(InputHdmi1), this), 3);

			AddRoutingInputPort(
				new RoutingInputPort(RoutingPortNames.HdmiIn2, eRoutingSignalType.Audio | eRoutingSignalType.Video,
					eRoutingPortConnectionType.Hdmi, new Action(InputHdmi2), this), 4);

			AddRoutingInputPort(
				new RoutingInputPort(RoutingPortNames.DviIn1, eRoutingSignalType.Audio | eRoutingSignalType.Video,
					eRoutingPortConnectionType.Dvi, new Action(InputDvi1), this), 5);

			AddRoutingInputPort(
				new RoutingInputPort(RoutingPortNames.DisplayPortIn1, eRoutingSignalType.Audio | eRoutingSignalType.Video,
					eRoutingPortConnectionType.DisplayPort, new Action(InputDisplayPort1), this), 6);

			// RoutingPortNames does not contain and Slot1, using HdmiIn4
			AddRoutingInputPort(
				new RoutingInputPort(RoutingPortNames.HdmiIn4, eRoutingSignalType.Audio | eRoutingSignalType.Video,
					eRoutingPortConnectionType.Streaming, new Action(InputHdmi4), this), 13);

			// RoutingPortNames does not contain and Slot2, using HdmiIn5
			AddRoutingInputPort(
				new RoutingInputPort(RoutingPortNames.HdmiIn5, eRoutingSignalType.Audio | eRoutingSignalType.Video,
					eRoutingPortConnectionType.Streaming, new Action(InputHdmi5), this), 14);

			// initialize feedbacks after adding input ports
			_inputFeedback = new List<bool>();
			InputFeedback = new List<BoolFeedback>();

			for (var i = 0; i < InputPorts.Count; i++)
			{
				var input = i + 1;
				InputFeedback.Add(new BoolFeedback(() =>
				{
					return CurrentInputNumber == input;
				}));
			}

			CurrentInputNumberFeedback = new IntFeedback(() =>
			{
				return CurrentInputNumber;
			});
		}

		/// <summary>
		/// Selects HDMI input 1 on the projector using async task execution
		/// </summary>
		public void InputHdmi1()
		{
			Task.Run(() =>
			{
				SendText("SIN+MAIN", 3);
				Thread.Sleep(2000);
				InputGet();
			});
		}

		/// <summary>
		/// Selects HDMI input 2 on the projector using async task execution
		/// </summary>
		public void InputHdmi2()
		{
			Task.Run(() =>
			{
				SendText("SIN+MAIN", 4);
				Thread.Sleep(2000);
				InputGet();
			});
		}

		/// <summary>
		/// Selects HDMI input 4 (Slot 1) on the projector using async task execution
		/// </summary>
		public void InputHdmi4()
		{
			Task.Run(() =>
			{
				SendText("SIN+MAIN", 13);

				Thread.Sleep(2000);
				InputGet();
			});
		}

		/// <summary>
		/// Selects HDMI input 5 (Slot 2) on the projector using async task execution
		/// </summary>
		public void InputHdmi5()
		{
			Task.Run(() =>
			{
				SendText("SIN+MAIN", 14);

				Thread.Sleep(2000);
				InputGet();
			});
		}

		/// <summary>
		/// Selects DVI input 1 (Input B) on the projector using async task execution
		/// </summary>
		public void InputDvi1()
		{
			Task.Run(() =>
			{
				SendText("SIN+MAIN", 15);

				Thread.Sleep(2000);
				InputGet();
			});
		}


		/// <summary>
		/// Selects DisplayPort input 1 on the projector using async task execution
		/// </summary>
		public void InputDisplayPort1()
		{
			Task.Run(() =>
			{
				SendText("SIN+MAIN", 6);

				Thread.Sleep(2000);
				InputGet();
			});
		}


		/// <summary>
		/// Toggles between available inputs (not implemented for this device)
		/// </summary>
		/// <exception cref="NotImplementedException">Input toggle is not supported on this device</exception>
		public void InputToggle()
		{
			throw new NotImplementedException("InputToggle is not supported");
		}

		/// <summary>
		/// Polls the projector for current input selection status
		/// </summary>
		public void InputGet()
		{
			SendText("SIN+MAIN", "?");

		}

		/// <summary>
		/// Processes input feedback from the device response and updates current input state
		/// </summary>
		/// <param name="input">The input number returned from the device</param>
		public void UpdateInputFb(int input)
		{
			var newInput = InputPorts.FirstOrDefault(i => i.FeedbackMatchObject.Equals(input));
			if (newInput == null) return;
			if (newInput == _currentInputPort)
			{
				this.LogDebug("UpdateInputFb: _currentInputPort-'{currentInputPort}' == newInput-'{newInputPort}'", _currentInputPort.Key, newInput.Key);
				return;
			}

			_currentInputPort = newInput;
			CurrentInputFeedback.FireUpdate();

			switch (_currentInputPort.Key)
			{
				case RoutingPortNames.HdmiIn1:
					CurrentInputNumber = 1;
					break;
				case RoutingPortNames.HdmiIn2:
					CurrentInputNumber = 2;
					break;
				case RoutingPortNames.DviIn1:
					CurrentInputNumber = 3;
					break;
				case RoutingPortNames.DisplayPortIn1:
					CurrentInputNumber = 4;
					break;
				case RoutingPortNames.HdmiIn4:
					CurrentInputNumber = 5;
					break;
				case RoutingPortNames.HdmiIn5:
					CurrentInputNumber = 6;
					break;
			}
		}

		/// <summary>
		/// Updates Digital Route Feedback for Simpl EISC
		/// </summary>
		private void UpdateInputBooleanFeedback()
		{
			foreach (var item in InputFeedback)
			{
				item.FireUpdate();
			}
		}

		#endregion



		#region lampHours

		/// <summary>
		/// Gets or sets the feedback object for reporting lamp hours
		/// </summary>
		public IntFeedback LampHoursFeedback { get; set; }

		private int _lampHours;

		/// <summary>
		/// Gets or sets the current lamp hours value
		/// </summary>
		public int LampHours
		{
			get { return _lampHours; }
			set
			{
				_lampHours = value;
				LampHoursFeedback.FireUpdate();
			}
		}

		/// <summary>
		/// Polls the projector for current lamp hours or laser runtime
		/// </summary>
		public void LampGet()
		{
			SendText("ILI", "?");
		}

		#endregion

		#region videoMute

		private bool _videoMuteIsOn;


		/// <summary>
		/// Gets or sets the video mute state of the projector
		/// </summary>
		public bool VideoMuteIsOn
		{
			get { return _videoMuteIsOn; }
			set
			{
				if (_videoMuteIsOn == value)
				{
					return;
				}

				_videoMuteIsOn = value;
				VideoMuteIsOnFeedback.FireUpdate();
			}
		}

		/// <summary>
		/// Gets or sets the feedback object for video mute state
		/// </summary>
		public BoolFeedback VideoMuteIsOnFeedback;

		/// <summary>
		/// Polls the projector for current video mute status
		/// </summary>
		public void VideoMuteGet()
		{
			SendText("SHU", "?");
		}

		/// <summary>
		/// Turns on video mute (blanks the display output)
		/// </summary>
		public void VideoMuteOn()
		{
			SendText("SHU", 1);

			Thread.Sleep(25);
			VideoMuteGet();
		}

		/// <summary>
		/// Turns off video mute (restores the display output)
		/// </summary>
		public void VideoMuteOff()
		{
			SendText("SHU", 0);

			Thread.Sleep(25);
			VideoMuteGet();
		}

		/// <summary>
		/// Toggles the current video mute state (on to off, or off to on)
		/// </summary>
		public void VideoMuteToggle()
		{
			if (VideoMuteIsOn)
				VideoMuteOff();
			else
				VideoMuteOn();
		}

		#endregion
	}
}