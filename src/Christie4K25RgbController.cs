using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Ssh;
using Crestron.SimplSharpPro.CrestronThread;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.Queues;
using PepperDash.Essentials.Core.Routing;
using PepperDash.Essentials.Devices.Displays;
using PepperDash.Essentials.Devices.Common.Displays;
using System.Text.RegularExpressions;

namespace ChristieProjectorPlugin
{
	public class Christie4K25RgbController : TwoWayDisplayBase, ICommunicationMonitor,
		IInputHdmi1, IInputHdmi2, IInputDisplayPort1,IBridgeAdvanced
	{
		
		private bool _isSerialComm;
		private bool HasLamps { get; set; }
		private bool HasScreen { get; set; }
		private bool HasLift { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="key"></param>
		/// <param name="name"></param>
		/// <param name="config"></param>
		/// <param name="comms"></param>
		public Christie4K25RgbController(string key, string name, ChristieProjectorPropertiesConfig config, IBasicCommunication comms)
			: base(key, name)
		{
			var props = config;
			if (props == null)
			{
				Debug.Console(0, this, Debug.ErrorLogLevel.Error, "{0} configuration must be included", key);
				return;
			}

			DebugExtension.ResetDebugLevels();

			LampHoursFeedback = new IntFeedback(() => LampHours);

			Communication = comms;

			_receiveQueue = new GenericQueue(key + "-queue");

			CommunicationGather = new CommunicationGather(Communication, GatherDelimiter);
            CommunicationGather.LineReceived += OnCommunicationGatherLineReceived;


			var socket = Communication as ISocketStatus;
			_isSerialComm = (socket == null);

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
		/// Initialize (override from PepperDash Essentials)
		/// </summary>
		public override void Initialize()
		{
			Communication.Connect();
			CommunicationMonitor.Start();
		}




		#region IBridgeAdvanced Members

		/// <summary>
		/// LinkToApi
		/// </summary>
		/// <param name="trilist"></param>
		/// <param name="joinStart"></param>
		/// <param name="joinMapKey"></param>
		/// <param name="bridge"></param>
		public void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
		{
			var joinMap = new ChristieProjectorBridgeJoinMap(joinStart);

			// This adds the join map to the collection on the bridge
			if (bridge != null)
			{
				bridge.AddJoinMap(Key, joinMap);
			}

			var customJoins = JoinMapHelper.TryGetJoinMapAdvancedForDevice(joinMapKey);
			if (customJoins != null)
			{
				joinMap.SetCustomJoinData(customJoins);
			}

			Debug.Console(0, this, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
			Debug.Console(0, this, "Linking to Bridge Type {0}", GetType().Name);

			// links to bridge
			// device name
			trilist.SetString(joinMap.Name.JoinNumber, Name);

			//var twoWayDisplay = this as TwoWayDisplayBase;
			//trilist.SetBool(joinMap.IsTwoWayDisplay.JoinNumber, twoWayDisplay != null);

			// lamp, screen, lift config outputs
			trilist.SetBool(joinMap.HasLamps.JoinNumber, HasLamps);
			trilist.SetBool(joinMap.HasScreen.JoinNumber, HasScreen);
			trilist.SetBool(joinMap.HasLift.JoinNumber, HasLift);

			if (CommunicationMonitor != null)
			{
				CommunicationMonitor.IsOnlineFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);
			}

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
					Debug.Console(DebugExtension.Verbose, this, "InputSelect Digital-'{0}'", inputIndex + 1);
					SetInput = inputIndex + 1;
				});

				trilist.StringInput[(ushort)(joinMap.InputNamesOffset.JoinNumber + inputIndex)].StringValue = GetInputNameForKey(input.Key);

				InputFeedback[inputIndex].LinkInputSig(trilist.BooleanInput[joinMap.InputSelectOffset.JoinNumber + (uint)inputIndex]);
			}

			// input (analog select)
			trilist.SetUShortSigAction(joinMap.InputSelect.JoinNumber, analogValue =>
			{
				Debug.Console(DebugExtension.Notice, this, "InputSelect Analog-'{0}'", analogValue);
				SetInput = analogValue;
			});

			// input (analog feedback)
			if (CurrentInputNumberFeedback != null)
				CurrentInputNumberFeedback.LinkInputSig(trilist.UShortInput[joinMap.InputSelect.JoinNumber]);

			if (CurrentInputFeedback != null)
				CurrentInputFeedback.OutputChange += (sender, args) => Debug.Console(DebugExtension.Notice, this, "CurrentInputFeedback: {0}", args.StringValue);

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

				if (CurrentInputFeedback != null)
					CurrentInputFeedback.FireUpdate();
				if (CurrentInputNumberFeedback != null)
					CurrentInputNumberFeedback.FireUpdate();

				for (var i = 0; i < InputPorts.Count; i++)
				{
					var inputIndex = i;
					if (InputFeedback != null)
						InputFeedback[inputIndex].FireUpdate();
				}

				LampHoursFeedback.FireUpdate();
			};
		}


		#endregion



		#region ICommunicationMonitor Members

		/// <summary>
		/// IBasicComminication object
		/// </summary>
		public IBasicCommunication Communication { get; private set; }

		/// <summary>
		/// Gather object
		/// </summary>
		public CommunicationGather CommunicationGather { get; private set; }

		/// <summary>
		/// Communication status monitor object
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
			if (args == null)
			{
				Debug.Console(DebugExtension.Notice, this, "OnCommunicationGatherLineReceived: args are null");
				return;
			}

			if (string.IsNullOrEmpty(args.Text))
			{
				Debug.Console(DebugExtension.Notice, this, "OnCommunicationGatherLineReceived: args.Text is null or empty");
				return;
			}

			try
			{
				Debug.Console(DebugExtension.Verbose, this, "OnCommunicationGatherLineReceived: args.Text-'{0}'", args.Text);
				_receiveQueue.Enqueue(new ProcessStringMessage(args.Text, ProcessResponse));
			}
			catch (Exception ex)
			{
				Debug.Console(DebugExtension.Notice, this, Debug.ErrorLogLevel.Error, "HandleLineReceived Exception: {0}", ex.Message);
				Debug.Console(DebugExtension.Verbose, this, Debug.ErrorLogLevel.Error, "HandleLineRecieved StackTrace: {0}", ex.StackTrace);
				if (ex.InnerException != null) Debug.Console(DebugExtension.Notice, this, Debug.ErrorLogLevel.Error, "HandleLineReceived InnerException: '{0}'", ex.InnerException);
			}
		}

		#endregion


		// TODO [ ] Update based on device responses
		private void ProcessResponse(string response)
		{
            if (string.IsNullOrEmpty(response)) return;


            var responseValue = 0;
            var responseType = "";
		    var responseData = "";
		
            try
            {

                // special case for lamp hour feedback
		        const string lampHourFb = "Lamp Hours = ";
                if (response.Contains(lampHourFb))
		        {
		            response = response.Substring(response.IndexOf(lampHourFb, System.StringComparison.Ordinal) + lampHourFb.Length, 5);
                  
                    if (response.Contains(":"))
		            {
		                var tokens = response.Split(':');
		                LampHours = Int32.Parse(tokens[0]);
                        Debug.Console(DebugExtension.Notice, this, "Lamp Hours: {0}", LampHours);
                    }
                    return;
		        }
                

                if (!response.Contains("!")) return;

			    Debug.Console(DebugExtension.Notice, this, "ProcessResponse: {0}", response);

                var pattern = new Regex(@"\((?<command>[^!]+)!(?<value>\d+)(?: ""(?<data>.+?)"")?", RegexOptions.None);
                var match = pattern.Match(response);
                responseType = match.Groups["command"].Value;
                var responseString = match.Groups["value"].Value;
                responseData = match.Groups["data"].Value;
                responseValue = Int32.Parse(responseString);
                
            }
		    catch (Exception e)
		    {
                Debug.Console(DebugExtension.Notice, this, "ProcessResponse error parsing, exception: {0}", e.Message);
            }
            
			Debug.Console(DebugExtension.Verbose, this, "ProcessResponse: responseType-'{0}', responseValue-'{1}'", responseType, responseValue);

			switch (responseType)
			{
				case "PWR":
					{
						PowerIsOn = responseValue == 1;
						break;
					}
		     	case "SIN":
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
						Debug.Console(DebugExtension.Verbose, this, "ProcessRespopnse: unknown response '{0}'", responseType);
						break;
					}
			}
            
		}

		/// <summary>
		/// Sends commands, adding delimiter
		/// </summary>
		/// <param name="cmd"></param>
		public void SendText(string cmd)
		{
			if (string.IsNullOrEmpty(cmd)) return;

			if (!Communication.IsConnected)
			{
				Debug.Console(DebugExtension.Notice, this, "SendText: device not connected");
				return;
			}

			var text = string.Format("({0})", cmd);
			Debug.Console(DebugExtension.Notice, this, "SendText: {0}", text);
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
			// tx format: "({cmd}{value})]"
			Communication.SendText(string.Format("({0}{1})", cmd, value));
		}

		/// <summary>
		/// Executes a switch, turning on display if necessary.
		/// </summary>
		/// <param name="selector"></param>
		public override void ExecuteSwitch(object selector)
		{
			if (PowerIsOn)
			{
				var action = selector as Action;
				Debug.Console(0, this, "ExecuteSwitch: action is {0}", action == null ? "null" : "not null");
				if (action != null)
				{
				    action();
					//CrestronInvoke.BeginInvoke(o => action());
				}
			}
			else // if power is off, wait until we get on FB to send it. 
			{
				// One-time event handler to wait for power on before executing switch
				EventHandler<FeedbackEventArgs> handler = null; // necessary to allow reference inside lambda to handler
				handler = (sender, args) =>
				{
					if (IsWarmingUp) return;

					IsWarmingUpFeedback.OutputChange -= handler;

					var action = selector as Action;
					Debug.Console(0, this, "ExecuteSwitch: action is {0}", action == null ? "null" : "not null");
					if (action != null)
					{
						CrestronInvoke.BeginInvoke(o => action());
					}
				};
				IsWarmingUpFeedback.OutputChange += handler; // attach and wait for on FB
				PowerOn();
			}
		}


		/// <summary>
		/// Starts the Poll Ring
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
		/// Power is on property
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
		/// Is warming property
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
		/// Is cooling property
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
		/// Set Power On For Device
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
		/// Set Power Off for Device
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
		/// Poll Power
		/// </summary>
		public void PowerGet()
		{
            SendText("PWR", "?");

		}


		/// <summary>
		/// Toggle current power state for device
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
		/// Input power on constant
		/// </summary>
		public const int InputPowerOn = 101;

		/// <summary>
		/// Input power off constant
		/// </summary>
		public const int InputPowerOff = 102;

		/// <summary>
		/// Input key list
		/// </summary>
		public static List<string> InputKeys = new List<string>();

		/// <summary>
		/// Input (digital) feedback
		/// </summary>
		public List<BoolFeedback> InputFeedback;

		/// <summary>
		/// Input number (analog) feedback
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
		/// Input number property
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
		/// Sets the requested input
		/// </summary>
		public int SetInput
		{
			set
			{
				if (value <= 0 || value > InputPorts.Count)
				{
					Debug.Console(DebugExtension.Notice, this, "SetInput: value-'{0}' is out of range (1 - {1})", value, InputPorts.Count);
					return;
				}

				Debug.Console(DebugExtension.Notice, this, "SetInput: value-'{0}'", value);

				// -1 to get actual input in list after 0d check
				var port = GetInputPort(value - 1);
				if (port == null)
				{
					Debug.Console(DebugExtension.Notice, this, "SetInput: failed to get input port");
					return;
				}

				Debug.Console(DebugExtension.Verbose, this, "SetInput: port.key-'{0}', port.Selector-'{1}', port.ConnectionType-'{2}', port.FeebackMatchObject-'{3}'",
					port.Key, port.Selector, port.ConnectionType, port.FeedbackMatchObject);

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
                case "hdmiIn3":
                    return "HDBaseT";
                case "displayPortIn1":
					return "Display Port 1";
                case "displayPortIn2":
					return "Display Port 2";
                case "rgbIn1":
                    return "SDI 1";
                case "rgbIn2":
                    return "SDI 2";
                case "vgaIn1":
                    return "SDI 3";
                case "hdmiIn4":
                    return "SDI 4";
                case "hdmiIn5":
                    return "Digital Link 1";
                case "hdmiIn6":
                    return "Digital Link 2";


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
					eRoutingPortConnectionType.Hdmi, new Action(InputHdmi1), this), 1);

			AddRoutingInputPort(
				new RoutingInputPort(RoutingPortNames.HdmiIn2, eRoutingSignalType.Audio | eRoutingSignalType.Video,
					eRoutingPortConnectionType.Hdmi, new Action(InputHdmi2), this), 2);

            AddRoutingInputPort(
            new RoutingInputPort(RoutingPortNames.HdmiIn3, eRoutingSignalType.Audio | eRoutingSignalType.Video,
                eRoutingPortConnectionType.Streaming, new Action(InputHdbaseT), this), 3);

            AddRoutingInputPort(
                new RoutingInputPort(RoutingPortNames.DisplayPortIn1, eRoutingSignalType.Audio | eRoutingSignalType.Video,
                    eRoutingPortConnectionType.DisplayPort, new Action(InputDisplayPort1), this), 4);

            AddRoutingInputPort(
                new RoutingInputPort(RoutingPortNames.DisplayPortIn2, eRoutingSignalType.Audio | eRoutingSignalType.Video,
                    eRoutingPortConnectionType.DisplayPort, new Action(InputDisplayPort2), this), 5);

			AddRoutingInputPort(
				new RoutingInputPort(RoutingPortNames.RgbIn1, eRoutingSignalType.Audio | eRoutingSignalType.Video,
					eRoutingPortConnectionType.Sdi, new Action(InputSdi1), this), 6);

            AddRoutingInputPort(
                new RoutingInputPort(RoutingPortNames.RgbIn2, eRoutingSignalType.Audio | eRoutingSignalType.Video,
                    eRoutingPortConnectionType.Sdi, new Action(InputSdi2), this), 7);

            AddRoutingInputPort(
                new RoutingInputPort(RoutingPortNames.VgaIn1, eRoutingSignalType.Audio | eRoutingSignalType.Video,
                    eRoutingPortConnectionType.Sdi, new Action(InputSdi3), this), 8);

            AddRoutingInputPort(
                new RoutingInputPort(RoutingPortNames.HdmiIn4, eRoutingSignalType.Audio | eRoutingSignalType.Video,
                    eRoutingPortConnectionType.Sdi, new Action(InputSdi4), this), 9);

     
            // RoutingPortNames does not contain Link 1, using HdmiIn5
			AddRoutingInputPort(
				new RoutingInputPort(RoutingPortNames.HdmiIn5, eRoutingSignalType.Audio | eRoutingSignalType.Video,
					eRoutingPortConnectionType.Streaming, new Action(InputDigitalLink1), this), 10);
            
            // RoutingPortNames does not contain Link 2, using HdmiIn6
			AddRoutingInputPort(
				new RoutingInputPort(RoutingPortNames.HdmiIn6, eRoutingSignalType.Audio | eRoutingSignalType.Video,
					eRoutingPortConnectionType.Streaming, new Action(InputDigitalLink2), this), 11);

			// initialize feedbacks after adding input ports
			_inputFeedback = new List<bool>();
			InputFeedback = new List<BoolFeedback>();

			for (var i = 0; i < InputPorts.Count; i++)
			{
				var input = i + 1;
				InputFeedback.Add(new BoolFeedback(() =>
				{
					Debug.Console(DebugExtension.Notice, this, "CurrentInput Number: {0}; input: {1};", CurrentInputNumber, input);
					return CurrentInputNumber == input;
				}));
			}

			CurrentInputNumberFeedback = new IntFeedback(() =>
			{
				Debug.Console(DebugExtension.Verbose, this, "CurrentInputNumberFeedback: {0}", CurrentInputNumber);
				return CurrentInputNumber;
			});
		}

		/// <summary>
		/// Lists available input routing ports
		/// </summary>
		public void ListRoutingInputPorts()
		{
			var index = 0;
			foreach (var inputPort in InputPorts)
			{
				Debug.Console(0, this, "ListRoutingInputPorts: index-'{0}' key-'{1}', connectionType-'{2}', feedbackMatchObject-'{3}'",
					index, inputPort.Key, inputPort.ConnectionType, inputPort.FeedbackMatchObject);
				index++;
			}
		}

		/// <summary>
		/// Select Hdmi 1
		/// </summary>
		public void InputHdmi1()
		{
			SendText("SIN", 1);
			Thread.Sleep(2000);
			InputGet();
		}

		/// <summary>
		/// Select Hdmi 2
		/// </summary>
		public void InputHdmi2()
		{
            SendText("SIN", 2);			
			Thread.Sleep(2000);
			InputGet();
		}

        /// <summary>
        /// Select Hdmi 3 > InputHDBaseT
        /// </summary>
        public void InputHdbaseT()
        {
            SendText("SIN", 3);
            Thread.Sleep(2000);
            InputGet();
        }


        /// <summary>
        /// Select DP 1
        /// </summary>
        public void InputDisplayPort1()
        {
            SendText("SIN", 4);
            Thread.Sleep(2000);
            InputGet();
        }

        /// <summary>
        /// Select DP 2
        /// </summary>
        public void InputDisplayPort2()
        {
            SendText("SIN", 5);
            Thread.Sleep(2000);
            InputGet();
        }

		/// <summary>
		/// SDI1
		/// </summary>
		public void InputSdi1()
		{
            SendText("SIN", 6);
			
			Thread.Sleep(2000);
			InputGet();
		}

        /// <summary>
        /// SDI1
        /// </summary>
        public void InputSdi2()
        {
            SendText("SIN", 7);

            Thread.Sleep(2000);
            InputGet();
        }

        /// <summary>
        /// SDI1
        /// </summary>
        public void InputSdi3()
        {
            SendText("SIN", 8);

            Thread.Sleep(2000);
            InputGet();
        }

        /// <summary>
        /// SDI1
        /// </summary>
        public void InputSdi4()
        {
            SendText("SIN", 9);

            Thread.Sleep(2000);
            InputGet();
        }


		/// <summary>
		/// Select input DVI terminal 1 (Input B)
		/// </summary>
		public void InputDigitalLink1()
		{
            SendText("SIN", 10);
			
			Thread.Sleep(2000);
			InputGet();
		}


        /// <summary>
        /// Select input DisplayPort
        /// </summary>
        public void InputDigitalLink2()
        {
            SendText("SIN", 11);
			
            Thread.Sleep(2000);
            InputGet();
        }

		
        /// <summary>
		/// Toggles the display input
		/// </summary>
		public void InputToggle()
		{
			throw new NotImplementedException("InputToggle is not supported");
		}

		/// <summary>
		/// Poll input
		/// </summary>
		public void InputGet()
		{
            SendText("SIN", "?");
			
		}

		/// <summary>
		/// Process Input Feedback from Response
		/// </summary>
		/// <param name="input">response from device</param>
		public void UpdateInputFb(int input)
		{
			var newInput = InputPorts.FirstOrDefault(i => i.FeedbackMatchObject.Equals(input));
			if (newInput == null) return;
			if (newInput == _currentInputPort)
			{
				Debug.Console(DebugExtension.Notice, this, "UpdateInputFb: _currentInputPort-'{0}' == newInput-'{1}'", _currentInputPort.Key, newInput.Key);
				return;
			}

			Debug.Console(DebugExtension.Notice, this, "UpdateInputFb: newInput key-'{0}', connectionType-'{1}', feedbackMatchObject-'{2}'",
				newInput.Key, newInput.ConnectionType, newInput.FeedbackMatchObject);

			_currentInputPort = newInput;
			CurrentInputFeedback.FireUpdate();

			Debug.Console(DebugExtension.Notice, this, "UpdateInputFb: _currentInputPort.key-'{0}'", _currentInputPort.Key);

			switch (_currentInputPort.Key)
			{
				case RoutingPortNames.HdmiIn1:
					CurrentInputNumber = 1;
					break;
				case RoutingPortNames.HdmiIn2:
					CurrentInputNumber = 2;
					break;
				case RoutingPortNames.HdmiIn3:
					CurrentInputNumber = 3;
					break;
				case RoutingPortNames.DisplayPortIn1:
					CurrentInputNumber = 4;
					break;
				case RoutingPortNames.DisplayPortIn2:
					CurrentInputNumber = 5;
					break;
				case RoutingPortNames.RgbIn1:
					CurrentInputNumber = 6;
					break;
                case RoutingPortNames.RgbIn2:
                    CurrentInputNumber = 7;
                    break;
                case RoutingPortNames.VgaIn1:
                    CurrentInputNumber = 8;
                    break;
                case RoutingPortNames.HdmiIn4:
                    CurrentInputNumber = 9;
                    break;
                case RoutingPortNames.HdmiIn5:
                    CurrentInputNumber = 10;
                    break;
                case RoutingPortNames.HdmiIn6:
                    CurrentInputNumber = 11;
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



		#region lmapHours

		/// <summary>
		/// Lamp hours feedback
		/// </summary>
		public IntFeedback LampHoursFeedback { get; set; }

		private int _lampHours;

		/// <summary>
		/// Lamp hours property
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
		/// Polls for lamp hours/laser runtime
		/// </summary>
		public void LampGet()
		{
            SendText("ILI", "?");
			
		}

		#endregion




		#region videoMute

		private bool _videoMuteIsOn;


		/// <summary>
		/// Video mute is on 
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
		/// Video mute feedback
		/// </summary>
		public BoolFeedback VideoMuteIsOnFeedback;

		public void VideoMuteGet()
		{
		    SendText("SHU", "?");
		}

		public void VideoMuteOn()
		{
            SendText("SHU", 1);
		    
            Thread.Sleep(25);
            VideoMuteGet();

		}

		public void VideoMuteOff()
		{
            SendText("SHU", 0);

            Thread.Sleep(25);
            VideoMuteGet();
		}

		public void VideoMuteToggle()
		{
			if(VideoMuteIsOn)
				VideoMuteOff();
			else 
				VideoMuteOn();
		}

		#endregion

	}
}