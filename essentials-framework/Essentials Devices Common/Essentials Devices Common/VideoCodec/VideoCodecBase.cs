﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharp.Ssh;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Core.Intersystem;
using PepperDash.Core.Intersystem.Tokens;
using PepperDash.Core.WebApi.Presets;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core.Devices;
using PepperDash.Essentials.Core.DeviceTypeInterfaces;
using PepperDash.Essentials.Core.Routing;
using PepperDash.Essentials.Devices.Common.Cameras;
using PepperDash.Essentials.Devices.Common.Codec;
using PepperDash.Essentials.Devices.Common.VideoCodec.Interfaces;
using PepperDash.Essentials.Core.Bridges.JoinMaps;
using Feedback = PepperDash.Essentials.Core.Feedback;

namespace PepperDash.Essentials.Devices.Common.VideoCodec
{
	public abstract class VideoCodecBase : ReconfigurableDevice, IRoutingInputsOutputs,
		IUsageTracking, IHasDialer, IHasContentSharing, ICodecAudio, iVideoCodecInfo, IBridgeAdvanced
	{
		private const int XSigEncoding = 28591;
        protected const int MaxParticipants = 50;
		private readonly byte[] _clearBytes = XSigHelpers.ClearOutputs();
		protected VideoCodecBase(DeviceConfig config)
			: base(config)
		{

			StandbyIsOnFeedback = new BoolFeedback(StandbyIsOnFeedbackFunc);
			PrivacyModeIsOnFeedback = new BoolFeedback(PrivacyModeIsOnFeedbackFunc);
			VolumeLevelFeedback = new IntFeedback(VolumeLevelFeedbackFunc);
			MuteFeedback = new BoolFeedback(MuteFeedbackFunc);
			SharingSourceFeedback = new StringFeedback(SharingSourceFeedbackFunc);
			SharingContentIsOnFeedback = new BoolFeedback(SharingContentIsOnFeedbackFunc);

            // TODO [ ] hotfix/videocodecbase-max-meeting-xsig-set
            MeetingsToDisplayFeedback = new IntFeedback(() => MeetingsToDisplay);

			InputPorts = new RoutingPortCollection<RoutingInputPort>();
			OutputPorts = new RoutingPortCollection<RoutingOutputPort>();

			ActiveCalls = new List<CodecActiveCallItem>();
		}

		public IBasicCommunication Communication { get; protected set; }

		/// <summary>
		/// An internal pseudo-source that is routable and connected to the osd input
		/// </summary>
		public DummyRoutingInputsDevice OsdSource { get; protected set; }

		public BoolFeedback StandbyIsOnFeedback { get; private set; }

		protected abstract Func<bool> PrivacyModeIsOnFeedbackFunc { get; }
		protected abstract Func<int> VolumeLevelFeedbackFunc { get; }
		protected abstract Func<bool> MuteFeedbackFunc { get; }
		protected abstract Func<bool> StandbyIsOnFeedbackFunc { get; }

		public List<CodecActiveCallItem> ActiveCalls { get; set; }

		public bool ShowSelfViewByDefault { get; protected set; }

        public bool SupportsCameraOff { get; protected set; }
        public bool SupportsCameraAutoMode { get; protected set; }

		public bool IsReady { get; protected set; }

		public virtual List<Feedback> Feedbacks
		{
			get
			{
				return new List<Feedback>
                {
                    PrivacyModeIsOnFeedback,
                    SharingSourceFeedback
                };
			}
		}

		protected abstract Func<string> SharingSourceFeedbackFunc { get; }
		protected abstract Func<bool> SharingContentIsOnFeedbackFunc { get; }

		#region ICodecAudio Members

		public abstract void PrivacyModeOn();
		public abstract void PrivacyModeOff();
		public abstract void PrivacyModeToggle();
		public BoolFeedback PrivacyModeIsOnFeedback { get; private set; }


		public BoolFeedback MuteFeedback { get; private set; }

		public abstract void MuteOff();

		public abstract void MuteOn();

		public abstract void SetVolume(ushort level);

		public IntFeedback VolumeLevelFeedback { get; private set; }

		public abstract void MuteToggle();

		public abstract void VolumeDown(bool pressRelease);


		public abstract void VolumeUp(bool pressRelease);

		#endregion

		#region IHasContentSharing Members

		public abstract void StartSharing();
		public abstract void StopSharing();

		public bool AutoShareContentWhileInCall { get; protected set; }

		public StringFeedback SharingSourceFeedback { get; private set; }
		public BoolFeedback SharingContentIsOnFeedback { get; private set; }

		#endregion

		#region IHasDialer Members

		/// <summary>
		/// Fires when the status of any active, dialing, or incoming call changes or is new
		/// </summary>
		public event EventHandler<CodecCallStatusItemChangeEventArgs> CallStatusChange;

		/// <summary>
		/// Returns true when any call is not in state Unknown, Disconnecting, Disconnected
		/// </summary>
		public bool IsInCall
		{
			get
			{
				var value = ActiveCalls != null && ActiveCalls.Any(c => c.IsActiveCall);
				return value;
			}
		}

		public abstract void Dial(string number);
		public abstract void EndCall(CodecActiveCallItem call);
		public abstract void EndAllCalls();
		public abstract void AcceptCall(CodecActiveCallItem call);
		public abstract void RejectCall(CodecActiveCallItem call);
		public abstract void SendDtmf(string s);

		#endregion

		#region IRoutingInputsOutputs Members

		public RoutingPortCollection<RoutingInputPort> InputPorts { get; private set; }

		public RoutingPortCollection<RoutingOutputPort> OutputPorts { get; private set; }

		#endregion

		#region IUsageTracking Members

		/// <summary>
		/// This object can be added by outside users of this class to provide usage tracking
		/// for various services
		/// </summary>
		public UsageTracking UsageTracker { get; set; }

		#endregion

		#region iVideoCodecInfo Members

		public VideoCodecInfo CodecInfo { get; protected set; }

		#endregion

		public event EventHandler<EventArgs> IsReadyChange;
		public abstract void Dial(Meeting meeting);

		public virtual void Dial(IInvitableContact contact)
		{
		}

		public abstract void ExecuteSwitch(object selector);

		/// <summary>
		/// Helper method to fire CallStatusChange event with old and new status
		/// </summary>
		protected void SetNewCallStatusAndFireCallStatusChange(eCodecCallStatus newStatus, CodecActiveCallItem call)
		{
			call.Status = newStatus;

			OnCallStatusChange(call);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="previousStatus"></param>
		/// <param name="newStatus"></param>
		/// <param name="item"></param>
		protected virtual void OnCallStatusChange(CodecActiveCallItem item)
		{
			var handler = CallStatusChange;
			if (handler != null)
			{
				handler(this, new CodecCallStatusItemChangeEventArgs(item));
			}

			if (AutoShareContentWhileInCall)
			{
				StartSharing();
			}

			if (UsageTracker != null)
			{
				if (IsInCall && !UsageTracker.UsageTrackingStarted)
				{
					UsageTracker.StartDeviceUsage();
				}
				else if (UsageTracker.UsageTrackingStarted && !IsInCall)
				{
					UsageTracker.EndDeviceUsage();
				}
			}
		}

		/// <summary>
		/// Sets IsReady property and fires the event. Used for dependent classes to sync up their data.
		/// </summary>
		protected void SetIsReady()
		{
			CrestronInvoke.BeginInvoke((o) =>
				{
					try
					{
						IsReady = true;
						var h = IsReadyChange;
						if (h != null)
						{
							h(this, new EventArgs());
						}
					}
					catch (Exception e)
					{
						Debug.Console(2, this, "Error in SetIsReady() : {0}", e);
					}
				});
		}

		// **** DEBUGGING THINGS ****
		/// <summary>
		/// 
		/// </summary>
		public virtual void ListCalls()
		{
			var sb = new StringBuilder();
			foreach (var c in ActiveCalls)
			{
				sb.AppendFormat("{0} {1} -- {2} {3}\n", c.Id, c.Number, c.Name, c.Status);
			}
			Debug.Console(1, this, "\n{0}\n", sb.ToString());
		}

		public abstract void StandbyActivate();

		public abstract void StandbyDeactivate();

		#region Implementation of IBridgeAdvanced

		public abstract void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge);

		/// <summary>
		/// Use this method when using a plain VideoCodecControllerJoinMap
		/// </summary>
		/// <param name="codec"></param>
		/// <param name="trilist"></param>
		/// <param name="joinStart"></param>
		/// <param name="joinMapKey"></param>
		/// <param name="bridge"></param>
		protected void LinkVideoCodecToApi(VideoCodecBase codec, BasicTriList trilist, uint joinStart, string joinMapKey,
			EiscApiAdvanced bridge)
		{
			var joinMap = new VideoCodecControllerJoinMap(joinStart);

			var customJoins = JoinMapHelper.TryGetJoinMapAdvancedForDevice(joinMapKey);

			if (customJoins != null)
			{
				joinMap.SetCustomJoinData(customJoins);
			}

			if (bridge != null)
			{
				bridge.AddJoinMap(Key, joinMap);
			}

			LinkVideoCodecToApi(codec, trilist, joinMap);

		    trilist.OnlineStatusChange += (device, args) =>
		    {
		        if (!args.DeviceOnLine) return;
		    };
		}

		/// <summary>
		/// Use this method when you need to pass in a join map that extends VideoCodecControllerJoinMap
		/// </summary>
		/// <param name="codec"></param>
		/// <param name="trilist"></param>
		/// <param name="joinMap"></param>
		protected void LinkVideoCodecToApi(VideoCodecBase codec, BasicTriList trilist, VideoCodecControllerJoinMap joinMap)
		{
			Debug.Console(1, this, "Linking to Trilist {0}", trilist.ID.ToString("X"));

			LinkVideoCodecDtmfToApi(trilist, joinMap);

			LinkVideoCodecCallControlsToApi(trilist, joinMap);

			LinkVideoCodecContentSharingToApi(trilist, joinMap);

			LinkVideoCodecPrivacyToApi(trilist, joinMap);

			LinkVideoCodecVolumeToApi(trilist, joinMap);

			if (codec is ICommunicationMonitor)
			{
				LinkVideoCodecCommMonitorToApi(codec as ICommunicationMonitor, trilist, joinMap);
			}

			if (codec is IHasCodecCameras)
			{
				LinkVideoCodecCameraToApi(codec as IHasCodecCameras, trilist, joinMap);
			}

			if (codec is IHasCodecSelfView)
			{
				LinkVideoCodecSelfviewToApi(codec as IHasCodecSelfView, trilist, joinMap);
			}

			if (codec is IHasCameraAutoMode)
			{
				trilist.SetBool(joinMap.CameraSupportsAutoMode.JoinNumber, SupportsCameraAutoMode);
				LinkVideoCodecCameraModeToApi(codec as IHasCameraAutoMode, trilist, joinMap);
			}

			if (codec is IHasCameraOff)
			{
				trilist.SetBool(joinMap.CameraSupportsOffMode.JoinNumber, SupportsCameraOff);
				LinkVideoCodecCameraOffToApi(codec as IHasCameraOff, trilist, joinMap);
			}

			if (codec is IHasCodecLayouts)
			{
				LinkVideoCodecCameraLayoutsToApi(codec as IHasCodecLayouts, trilist, joinMap);
			}

			if (codec is IHasSelfviewPosition)
			{
				LinkVideoCodecSelfviewPositionToApi(codec as IHasSelfviewPosition, trilist, joinMap);
			}

			if (codec is IHasDirectory)
			{
				LinkVideoCodecDirectoryToApi(codec as IHasDirectory, trilist, joinMap);
			}

			if (codec is IHasScheduleAwareness)
			{
				LinkVideoCodecScheduleToApi(codec as IHasScheduleAwareness, trilist, joinMap);
			}

			if (codec is IHasParticipants)
			{
				LinkVideoCodecParticipantsToApi(codec as IHasParticipants, trilist, joinMap);
			}

			if (codec is IHasFarEndContentStatus)
			{
				(codec as IHasFarEndContentStatus).ReceivingContent.LinkInputSig(trilist.BooleanInput[joinMap.RecievingContent.JoinNumber]);
			}

			if (codec is IHasPhoneDialing)
			{
				LinkVideoCodecPhoneToApi(codec as IHasPhoneDialing, trilist, joinMap);
			}

			trilist.OnlineStatusChange += (device, args) =>
			{
				if (!args.DeviceOnLine) return;

				if (codec is IHasDirectory)
				{
					(codec as IHasDirectory).SetCurrentDirectoryToRoot();
				}

				if (codec is IHasScheduleAwareness)
				{
					(codec as IHasScheduleAwareness).GetSchedule();
				}

				if (codec is IHasParticipants)
				{
					UpdateParticipantsXSig((codec as IHasParticipants).Participants.CurrentParticipants);
				}

				if (codec is IHasCameraAutoMode)
				{
					trilist.SetBool(joinMap.CameraSupportsAutoMode.JoinNumber, true);

					(codec as IHasCameraAutoMode).CameraAutoModeIsOnFeedback.FireUpdate();
				}

				if (codec is IHasCodecSelfView)
				{
					(codec as IHasCodecSelfView).SelfviewIsOnFeedback.FireUpdate();
				}

				if (codec is IHasCameraAutoMode)
				{
					(codec as IHasCameraAutoMode).CameraAutoModeIsOnFeedback.FireUpdate();
				}

				if (codec is IHasCameraOff)
				{
					(codec as IHasCameraOff).CameraIsOffFeedback.FireUpdate();
				}

				if (codec is IHasPhoneDialing)
				{
					(codec as IHasPhoneDialing).PhoneOffHookFeedback.FireUpdate();
				}

				SharingContentIsOnFeedback.FireUpdate();

				trilist.SetBool(joinMap.HookState.JoinNumber, IsInCall);

				trilist.SetString(joinMap.CurrentCallData.JoinNumber, UpdateCallStatusXSig());
			};
		}

		private void LinkVideoCodecPhoneToApi(IHasPhoneDialing codec, BasicTriList trilist, VideoCodecControllerJoinMap joinMap)
		{
			codec.PhoneOffHookFeedback.LinkInputSig(trilist.BooleanInput[joinMap.PhoneHookState.JoinNumber]);

			trilist.SetSigFalseAction(joinMap.DialPhone.JoinNumber,
				() => codec.DialPhoneCall(trilist.StringOutput[joinMap.PhoneDialString.JoinNumber].StringValue));

			trilist.SetSigFalseAction(joinMap.HangUpPhone.JoinNumber, codec.EndPhoneCall);
		}

		private void LinkVideoCodecSelfviewPositionToApi(IHasSelfviewPosition codec, BasicTriList trilist, VideoCodecControllerJoinMap joinMap)
		{
			trilist.SetSigFalseAction(joinMap.SelfviewPosition.JoinNumber, codec.SelfviewPipPositionToggle);

			codec.SelfviewPipPositionFeedback.LinkInputSig(trilist.StringInput[joinMap.SelfviewPositionFb.JoinNumber]);
		}

		private void LinkVideoCodecCameraOffToApi(IHasCameraOff codec, BasicTriList trilist, VideoCodecControllerJoinMap joinMap)
		{
			trilist.SetSigFalseAction(joinMap.CameraModeOff.JoinNumber, codec.CameraOff);

			codec.CameraIsOffFeedback.OutputChange += (o, a) =>
			{
				if (a.BoolValue)
				{
					trilist.SetBool(joinMap.CameraModeOff.JoinNumber, true);
					trilist.SetBool(joinMap.CameraModeManual.JoinNumber, false);
					trilist.SetBool(joinMap.CameraModeAuto.JoinNumber, false);
					return;
				}

				trilist.SetBool(joinMap.CameraModeOff.JoinNumber, false);

				var autoCodec = codec as IHasCameraAutoMode;

				if (autoCodec == null) return;

				trilist.SetBool(joinMap.CameraModeAuto.JoinNumber, autoCodec.CameraAutoModeIsOnFeedback.BoolValue);
				trilist.SetBool(joinMap.CameraModeManual.JoinNumber, !autoCodec.CameraAutoModeIsOnFeedback.BoolValue);
			};

			if (codec.CameraIsOffFeedback.BoolValue)
			{
				trilist.SetBool(joinMap.CameraModeOff.JoinNumber, true);
				trilist.SetBool(joinMap.CameraModeManual.JoinNumber, false);
				trilist.SetBool(joinMap.CameraModeAuto.JoinNumber, false);
				return;
			}

			trilist.SetBool(joinMap.CameraModeOff.JoinNumber, false);

			var autoModeCodec = codec as IHasCameraAutoMode;

			if (autoModeCodec == null) return;

			trilist.SetBool(joinMap.CameraModeAuto.JoinNumber, autoModeCodec.CameraAutoModeIsOnFeedback.BoolValue);
			trilist.SetBool(joinMap.CameraModeManual.JoinNumber, !autoModeCodec.CameraAutoModeIsOnFeedback.BoolValue);
		}

		private void LinkVideoCodecVolumeToApi(BasicTriList trilist, VideoCodecControllerJoinMap joinMap)
		{
			MuteFeedback.LinkInputSig(trilist.BooleanInput[joinMap.VolumeMuteOn.JoinNumber]);
			MuteFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.VolumeMuteOff.JoinNumber]);

			trilist.SetSigFalseAction(joinMap.VolumeMuteOn.JoinNumber, MuteOn);
			trilist.SetSigFalseAction(joinMap.VolumeMuteOff.JoinNumber, MuteOff);
			trilist.SetSigFalseAction(joinMap.VolumeMuteToggle.JoinNumber, MuteToggle);

			VolumeLevelFeedback.LinkInputSig(trilist.UShortInput[joinMap.VolumeLevel.JoinNumber]);

			trilist.SetBoolSigAction(joinMap.VolumeUp.JoinNumber, VolumeUp);
			trilist.SetBoolSigAction(joinMap.VolumeDown.JoinNumber, VolumeDown);

			trilist.SetUShortSigAction(joinMap.VolumeLevel.JoinNumber, SetVolume);

		}

		private void LinkVideoCodecPrivacyToApi(BasicTriList trilist, VideoCodecControllerJoinMap joinMap)
		{
			PrivacyModeIsOnFeedback.LinkInputSig(trilist.BooleanInput[joinMap.MicMuteOn.JoinNumber]);
			PrivacyModeIsOnFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.MicMuteOff.JoinNumber]);

			trilist.SetSigFalseAction(joinMap.MicMuteOn.JoinNumber, PrivacyModeOn);
			trilist.SetSigFalseAction(joinMap.MicMuteOff.JoinNumber, PrivacyModeOff);
			trilist.SetSigFalseAction(joinMap.MicMuteToggle.JoinNumber, PrivacyModeToggle);
		}

		private void LinkVideoCodecCommMonitorToApi(ICommunicationMonitor codec, BasicTriList trilist, VideoCodecControllerJoinMap joinMap)
		{
			codec.CommunicationMonitor.IsOnlineFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);
		}

		private void LinkVideoCodecParticipantsToApi(IHasParticipants codec, BasicTriList trilist, VideoCodecControllerJoinMap joinMap)
		{
            // make sure to update the values when the EISC comes online
            trilist.OnlineStatusChange += (sender, args) =>
                {
                    if (sender.IsOnline)
                    {
                        UpdateParticipantsXSig(codec, trilist, joinMap);
                    }
                };

            // set actions and update the values when the list changes
			codec.Participants.ParticipantsListHasChanged += (sender, args) =>
			{
                SetParticipantActions(trilist, joinMap, codec.Participants.CurrentParticipants);

                UpdateParticipantsXSig(codec, trilist, joinMap);
			};

            trilist.OnlineStatusChange += (device, args) =>
            {
                if (!args.DeviceOnLine) return;

                // TODO [ ] Issue #868
                trilist.SetString(joinMap.CurrentParticipants.JoinNumber, "\xFC");
                UpdateParticipantsXSig(codec, trilist, joinMap);
            };
		}

        private void UpdateParticipantsXSig(IHasParticipants codec, BasicTriList trilist, VideoCodecControllerJoinMap joinMap)
        {
			string participantsXSig;

			if (codec.Participants.CurrentParticipants.Count == 0)
			{
				participantsXSig = Encoding.GetEncoding(XSigEncoding).GetString(_clearBytes, 0, _clearBytes.Length);
				trilist.SetString(joinMap.CurrentParticipants.JoinNumber, participantsXSig);
				trilist.SetUshort(joinMap.ParticipantCount.JoinNumber, (ushort)codec.Participants.CurrentParticipants.Count);
				return;
			}

			participantsXSig = UpdateParticipantsXSig(codec.Participants.CurrentParticipants);

			trilist.SetString(joinMap.CurrentParticipants.JoinNumber, participantsXSig);

			trilist.SetUshort(joinMap.ParticipantCount.JoinNumber, (ushort)codec.Participants.CurrentParticipants.Count);
        }

        /// <summary>
        /// Sets the actions for each participant in the list
        /// </summary>
        private void SetParticipantActions(BasicTriList trilist, VideoCodecControllerJoinMap joinMap, List<Participant> currentParticipants)
        {
            uint index = 0; // track the index of the participant in the 

            foreach (var participant in currentParticipants)
            {
                var p = participant;
                if (index > MaxParticipants) break;

                var audioMuteCodec = this as IHasParticipantAudioMute;
                if (audioMuteCodec != null)
                {
                    trilist.SetSigFalseAction(joinMap.ParticipantAudioMuteToggleStart.JoinNumber + index,
                        () => audioMuteCodec.ToggleAudioForParticipant(p.UserId));

                    trilist.SetSigFalseAction(joinMap.ParticipantVideoMuteToggleStart.JoinNumber + index,
                        () => audioMuteCodec.ToggleVideoForParticipant(p.UserId));
                }

                var pinCodec = this as IHasParticipantPinUnpin;
                if (pinCodec != null)
                {
                    trilist.SetSigFalseAction(joinMap.ParticipantPinToggleStart.JoinNumber + index,
                        () => pinCodec.ToggleParticipantPinState(p.UserId, pinCodec.ScreenIndexToPinUserTo));
                }

                index++;
            }

            // Clear out any previously set actions
            while (index < MaxParticipants)
            {
                trilist.ClearBoolSigAction(joinMap.ParticipantAudioMuteToggleStart.JoinNumber + index);
                trilist.ClearBoolSigAction(joinMap.ParticipantVideoMuteToggleStart.JoinNumber + index);
                trilist.ClearBoolSigAction(joinMap.ParticipantPinToggleStart.JoinNumber + index);

                index++;
            }
        }

		private string UpdateParticipantsXSig(List<Participant> currentParticipants)
		{
			const int maxParticipants = MaxParticipants;
			const int maxDigitals = 7;
			const int maxStrings = 1;
			const int maxAnalogs = 1;
			const int offset = maxDigitals + maxStrings + maxAnalogs; // 9
			var digitalIndex = (maxStrings + maxAnalogs) * maxParticipants; // 100
			var stringIndex = 0;
			var analogIndex = stringIndex + maxParticipants;
			var meetingIndex = 0;

			var tokenArray = new XSigToken[maxParticipants * offset];

			foreach (var participant in currentParticipants)
			{
				if (meetingIndex >= maxParticipants * offset) break;

                Debug.Console(2, this,
@"Updating Participant on xsig:
Name: {0} (s{9})
AudioMute: {1} (d{10})
VideoMute: {2} (d{11})
CanMuteVideo: {3} (d{12})
CanUMuteVideo: {4} (d{13})
IsHost: {5} (d{14})
HandIsRaised: {6} (d{15})
IsPinned: {7} (d{16})
ScreenIndexIsPinnedTo: {8} (a{17})
",
 participant.Name,
 participant.AudioMuteFb,
 participant.VideoMuteFb,
 participant.CanMuteVideo,
 participant.CanUnmuteVideo,
 participant.IsHost,
 participant.HandIsRaisedFb,
 participant.IsPinnedFb,
 participant.ScreenIndexIsPinnedToFb,
 stringIndex + 1,
 digitalIndex + 1,
 digitalIndex + 2,
 digitalIndex + 3,
 digitalIndex + 4,
 digitalIndex + 5,
 digitalIndex + 6,
 digitalIndex + 7,
 analogIndex + 1
 );


				//digitals
				tokenArray[digitalIndex] = new XSigDigitalToken(digitalIndex + 1, participant.AudioMuteFb);
				tokenArray[digitalIndex + 1] = new XSigDigitalToken(digitalIndex + 2, participant.VideoMuteFb);
				tokenArray[digitalIndex + 2] = new XSigDigitalToken(digitalIndex + 3, participant.CanMuteVideo);
				tokenArray[digitalIndex + 3] = new XSigDigitalToken(digitalIndex + 4, participant.CanUnmuteVideo);
				tokenArray[digitalIndex + 4] = new XSigDigitalToken(digitalIndex + 5, participant.IsHost);
                tokenArray[digitalIndex + 5] = new XSigDigitalToken(digitalIndex + 6, participant.HandIsRaisedFb);
                tokenArray[digitalIndex + 6] = new XSigDigitalToken(digitalIndex + 7, participant.IsPinnedFb);

                Debug.Console(2, this, "Index: {0} byte value: {1}", digitalIndex + 7, ComTextHelper.GetEscapedText(tokenArray[digitalIndex + 6].GetBytes()));

				//serials
				tokenArray[stringIndex] = new XSigSerialToken(stringIndex + 1, participant.Name);

				//analogs
				tokenArray[analogIndex] = new XSigAnalogToken(analogIndex + 1, (ushort)participant.ScreenIndexIsPinnedToFb);

				digitalIndex += maxDigitals;
				meetingIndex += offset;
				stringIndex += maxStrings;
				analogIndex += maxAnalogs;
			}

			while (meetingIndex < maxParticipants * offset)
			{
				//digitals
				tokenArray[digitalIndex] = new XSigDigitalToken(digitalIndex + 1, false);
				tokenArray[digitalIndex + 1] = new XSigDigitalToken(digitalIndex + 2, false);
				tokenArray[digitalIndex + 2] = new XSigDigitalToken(digitalIndex + 3, false);
				tokenArray[digitalIndex + 3] = new XSigDigitalToken(digitalIndex + 4, false);
				tokenArray[digitalIndex + 4] = new XSigDigitalToken(digitalIndex + 5, false);
				tokenArray[digitalIndex + 5] = new XSigDigitalToken(digitalIndex + 6, false);
				tokenArray[digitalIndex + 6] = new XSigDigitalToken(digitalIndex + 7, false);

				//serials
				tokenArray[stringIndex] = new XSigSerialToken(stringIndex + 1, String.Empty);

				//analogs
				tokenArray[analogIndex] = new XSigAnalogToken(analogIndex + 1, 0);

				digitalIndex += maxDigitals;
				meetingIndex += offset;
				stringIndex += maxStrings;
				analogIndex += maxAnalogs;
			}

            var returnString = GetXSigString(tokenArray);

            //Debug.Console(2, this, "{0}", ComTextHelper.GetEscapedText(Encoding.GetEncoding(28591).GetBytes(returnString)));


            return returnString;
		}

		private void LinkVideoCodecContentSharingToApi(BasicTriList trilist, VideoCodecControllerJoinMap joinMap)
		{
			SharingContentIsOnFeedback.LinkInputSig(trilist.BooleanInput[joinMap.SourceShareStart.JoinNumber]);
			SharingContentIsOnFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.SourceShareEnd.JoinNumber]);

			SharingSourceFeedback.LinkInputSig(trilist.StringInput[joinMap.CurrentSource.JoinNumber]);

			trilist.SetSigFalseAction(joinMap.SourceShareStart.JoinNumber, StartSharing);
			trilist.SetSigFalseAction(joinMap.SourceShareEnd.JoinNumber, StopSharing);

			trilist.SetBoolSigAction(joinMap.SourceShareAutoStart.JoinNumber, (b) => AutoShareContentWhileInCall = b);
		}

		private List<Meeting> _currentMeetings = new List<Meeting>();

		private void LinkVideoCodecScheduleToApi(IHasScheduleAwareness codec, BasicTriList trilist, VideoCodecControllerJoinMap joinMap)
		{
			trilist.SetSigFalseAction(joinMap.UpdateMeetings.JoinNumber, codec.GetSchedule);

			trilist.SetUShortSigAction(joinMap.MinutesBeforeMeetingStart.JoinNumber, (i) =>
			{
				codec.CodecSchedule.MeetingWarningMinutes = i;
			});

			trilist.SetSigFalseAction(joinMap.DialMeeting1.JoinNumber, () =>
			{
				var mtg = 1;
				var index = mtg - 1;
				Debug.Console(1, this, "Meeting {0} Selected (EISC dig-o{1}) > _currentMeetings[{2}].Id: {3}, Title: {4}",
					mtg, joinMap.DialMeeting1.JoinNumber, index, _currentMeetings[index].Id, _currentMeetings[index].Title);
				if (_currentMeetings[index] != null)
					Dial(_currentMeetings[index]);
			});
			
			trilist.SetSigFalseAction(joinMap.DialMeeting2.JoinNumber, () =>
			{
				var mtg = 2;
				var index = mtg - 1;
				Debug.Console(1, this, "Meeting {0} Selected (EISC dig-o{1}) > _currentMeetings[{2}].Id: {3}, Title: {4}",
					mtg, joinMap.DialMeeting2.JoinNumber, index, _currentMeetings[index].Id, _currentMeetings[index].Title);
				if (_currentMeetings[index] != null)
					Dial(_currentMeetings[index]);
			});
			
			trilist.SetSigFalseAction(joinMap.DialMeeting3.JoinNumber, () =>
			{
				var mtg = 3;
				var index = mtg - 1;
				Debug.Console(1, this, "Meeting {0} Selected (EISC dig-o{1}) > _currentMeetings[{2}].Id: {3}, Title: {4}",
					mtg, joinMap.DialMeeting3.JoinNumber, index, _currentMeetings[index].Id, _currentMeetings[index].Title);
				if (_currentMeetings[index] != null)
					Dial(_currentMeetings[index]);
			});

			codec.CodecSchedule.MeetingsListHasChanged += (sender, args) => UpdateMeetingsList(codec, trilist, joinMap);
			codec.CodecSchedule.MeetingEventChange += (sender, args) =>
				{
					if (args.ChangeType == eMeetingEventChangeType.MeetingStartWarning)
					{
						UpdateMeetingsList(codec, trilist, joinMap);
					}
				};

            // TODO [ ] hotfix/videocodecbase-max-meeting-xsig-set
            trilist.SetUShortSigAction(joinMap.MeetingsToDisplay.JoinNumber, m => MeetingsToDisplay = m);
            MeetingsToDisplayFeedback.LinkInputSig(trilist.UShortInput[joinMap.MeetingsToDisplay.JoinNumber]);

            trilist.OnlineStatusChange += (device, args) =>
            {
                if (!args.DeviceOnLine) return;

                // TODO [ ] Issue #868
                trilist.SetString(joinMap.Schedule.JoinNumber, "\xFC");
                UpdateMeetingsList(codec, trilist, joinMap);
                // TODO [ ] hotfix/videocodecbase-max-meeting-xsig-set
                MeetingsToDisplayFeedback.LinkInputSig(trilist.UShortInput[joinMap.MeetingsToDisplay.JoinNumber]);
            };
        }

		private void UpdateMeetingsList(IHasScheduleAwareness codec, BasicTriList trilist, VideoCodecControllerJoinMap joinMap)
		{
			var currentTime = DateTime.Now;

			_currentMeetings = codec.CodecSchedule.Meetings.Where(m => m.StartTime >= currentTime || m.EndTime >= currentTime).ToList();

            if (_currentMeetings.Count == 0)
            {
                var emptyXSigByteArray = XSigHelpers.ClearOutputs();
                var emptyXSigString = Encoding.GetEncoding(XSigEncoding)
                    .GetString(emptyXSigByteArray, 0, emptyXSigByteArray.Length);

                trilist.SetString(joinMap.Schedule.JoinNumber, emptyXSigString);
                return;
            }

			var meetingsData = UpdateMeetingsListXSig(_currentMeetings);
			trilist.SetString(joinMap.Schedule.JoinNumber, meetingsData);
			trilist.SetUshort(joinMap.MeetingCount.JoinNumber, (ushort)_currentMeetings.Count);

            trilist.OnlineStatusChange += (device, args) =>
            {
                if (!args.DeviceOnLine) return;

                // TODO [ ] Issue #868
                trilist.SetString(joinMap.Schedule.JoinNumber, "\xFC");
                UpdateMeetingsListXSig(_currentMeetings);
            };
		}


        // TODO [ ] hotfix/videocodecbase-max-meeting-xsig-set
	    private int _meetingsToDisplay = 3;
        // TODO [ ] hotfix/videocodecbase-max-meeting-xsig-set
	    protected int MeetingsToDisplay
	    {
	        get { return _meetingsToDisplay; }
	        set {
                _meetingsToDisplay = (ushort) (value == 0 ? 3 : value);
                MeetingsToDisplayFeedback.FireUpdate();
	        }
	    }

        // TODO [ ] hotfix/videocodecbase-max-meeting-xsig-set
        public IntFeedback MeetingsToDisplayFeedback { get; set; }

		private string UpdateMeetingsListXSig(List<Meeting> meetings)
		{
            // TODO [ ] hotfix/videocodecbase-max-meeting-xsig-set
            //const int _meetingsToDisplay = 3;            
			const int maxDigitals = 2;
			const int maxStrings = 7;
			const int offset = maxDigitals + maxStrings;
			var digitalIndex = maxStrings * _meetingsToDisplay; //15
			var stringIndex = 0;
			var meetingIndex = 0;

			var tokenArray = new XSigToken[_meetingsToDisplay * offset];
			/* 
			 * Digitals
			 * IsJoinable - 1
			 * IsDialable - 2
			 * 
			 * Serials
			 * Organizer - 1
			 * Title - 2
			 * Start Date - 3
			 * Start Time - 4
			 * End Date - 5
			 * End Time - 6
			 * Id - 7
			*/


			foreach (var meeting in meetings)
			{
				var currentTime = DateTime.Now;

				if (meeting.StartTime < currentTime && meeting.EndTime < currentTime) continue;

				if (meetingIndex >= _meetingsToDisplay * offset)
				{
					Debug.Console(2, this, "Max Meetings reached");
					break;
				}

				//digitals
				tokenArray[digitalIndex] = new XSigDigitalToken(digitalIndex + 1, meeting.Joinable);
				tokenArray[digitalIndex + 1] = new XSigDigitalToken(digitalIndex + 2, meeting.Id != "0");

				//serials
				tokenArray[stringIndex] = new XSigSerialToken(stringIndex + 1, meeting.Organizer);
				tokenArray[stringIndex + 1] = new XSigSerialToken(stringIndex + 2, meeting.Title);
                tokenArray[stringIndex + 2] = new XSigSerialToken(stringIndex + 3, meeting.StartTime.ToString("t", Global.Culture));
				tokenArray[stringIndex + 3] = new XSigSerialToken(stringIndex + 4, meeting.StartTime.ToString("t", Global.Culture));
                tokenArray[stringIndex + 4] = new XSigSerialToken(stringIndex + 5, meeting.EndTime.ToString("t", Global.Culture));
				tokenArray[stringIndex + 5] = new XSigSerialToken(stringIndex + 6, meeting.EndTime.ToString("t", Global.Culture));
				tokenArray[stringIndex + 6] = new XSigSerialToken(stringIndex + 7, meeting.Id);


				digitalIndex += maxDigitals;
				meetingIndex += offset;
				stringIndex += maxStrings;
			}

			while (meetingIndex < _meetingsToDisplay * offset)
			{
				Debug.Console(2, this, "Clearing unused data. Meeting Index: {0} MaxMeetings * Offset: {1}",
					meetingIndex, _meetingsToDisplay * offset);

				//digitals
				tokenArray[digitalIndex] = new XSigDigitalToken(digitalIndex + 1, false);
				tokenArray[digitalIndex + 1] = new XSigDigitalToken(digitalIndex + 2, false);

				//serials
				tokenArray[stringIndex] = new XSigSerialToken(stringIndex + 1, String.Empty);
				tokenArray[stringIndex + 1] = new XSigSerialToken(stringIndex + 2, String.Empty);
				tokenArray[stringIndex + 2] = new XSigSerialToken(stringIndex + 3, String.Empty);
				tokenArray[stringIndex + 3] = new XSigSerialToken(stringIndex + 4, String.Empty);
				tokenArray[stringIndex + 4] = new XSigSerialToken(stringIndex + 5, String.Empty);
				tokenArray[stringIndex + 5] = new XSigSerialToken(stringIndex + 6, String.Empty);
				tokenArray[stringIndex + 6] = new XSigSerialToken(stringIndex + 7, String.Empty);

				digitalIndex += maxDigitals;
				meetingIndex += offset;
				stringIndex += maxStrings;
			}

			return GetXSigString(tokenArray);
		}

		private void LinkVideoCodecDirectoryToApi(IHasDirectory codec, BasicTriList trilist, VideoCodecControllerJoinMap joinMap)
		{
			codec.CurrentDirectoryResultIsNotDirectoryRoot.LinkComplementInputSig(
				trilist.BooleanInput[joinMap.DirectoryIsRoot.JoinNumber]);

			trilist.SetSigFalseAction(joinMap.DirectoryRoot.JoinNumber, codec.SetCurrentDirectoryToRoot);

			trilist.SetStringSigAction(joinMap.DirectorySearchString.JoinNumber, codec.SearchDirectory);

			trilist.SetUShortSigAction(joinMap.DirectorySelectRow.JoinNumber, (i) => SelectDirectoryEntry(codec, i));

			trilist.SetSigFalseAction(joinMap.DirectoryRoot.JoinNumber, codec.SetCurrentDirectoryToRoot);

			trilist.SetSigFalseAction(joinMap.DirectoryFolderBack.JoinNumber, codec.GetDirectoryParentFolderContents);

			codec.DirectoryResultReturned += (sender, args) =>
			{
				trilist.SetUshort(joinMap.DirectoryRowCount.JoinNumber, (ushort)args.Directory.CurrentDirectoryResults.Count);

				var clearBytes = XSigHelpers.ClearOutputs();

				trilist.SetString(joinMap.DirectoryEntries.JoinNumber,
					Encoding.GetEncoding(XSigEncoding).GetString(clearBytes, 0, clearBytes.Length));
				var directoryXSig = UpdateDirectoryXSig(args.Directory, !codec.CurrentDirectoryResultIsNotDirectoryRoot.BoolValue);

				trilist.SetString(joinMap.DirectoryEntries.JoinNumber, directoryXSig);
			};

            trilist.OnlineStatusChange += (device, args) =>
            {
                if (!args.DeviceOnLine) return;

                // TODO [ ] Issue #868
                trilist.SetString(joinMap.DirectoryEntries.JoinNumber, "\xFC");
                UpdateDirectoryXSig(codec.CurrentDirectoryResult,
                    !codec.CurrentDirectoryResultIsNotDirectoryRoot.BoolValue);
            };
		}

		private void SelectDirectoryEntry(IHasDirectory codec, ushort i)
		{
			var entry = codec.CurrentDirectoryResult.CurrentDirectoryResults[i - 1];

			if (entry is DirectoryFolder)
			{
				codec.GetDirectoryFolderContents(entry.FolderId);
				return;
			}

			var dialableEntry = entry as IInvitableContact;

			if (dialableEntry != null)
			{
				Dial(dialableEntry);
				return;
			}

			var entryToDial = entry as DirectoryContact;

			if (entryToDial == null) return;

			Dial(entryToDial.ContactMethods[0].Number);
		}

		private string UpdateDirectoryXSig(CodecDirectory directory, bool isRoot)
		{
			var contactIndex = 1;
			var tokenArray = new XSigToken[directory.CurrentDirectoryResults.Count];

			foreach (var entry in directory.CurrentDirectoryResults)
			{
				var arrayIndex = contactIndex - 1;

				if (entry is DirectoryFolder && entry.ParentFolderId == "root")
				{
					tokenArray[arrayIndex] = new XSigSerialToken(contactIndex, String.Format("[+] {0}", entry.Name));

					contactIndex++;

					continue;
				}

				if (isRoot && String.IsNullOrEmpty(entry.FolderId)) continue;

				tokenArray[arrayIndex] = new XSigSerialToken(contactIndex, entry.Name);

				contactIndex++;
			}

			return GetXSigString(tokenArray);
		}

		private void LinkVideoCodecCallControlsToApi(BasicTriList trilist, VideoCodecControllerJoinMap joinMap)
		{
			trilist.SetSigFalseAction(joinMap.ManualDial.JoinNumber,
				() => Dial(trilist.StringOutput[joinMap.CurrentDialString.JoinNumber].StringValue));

			//End All calls for now
			trilist.SetSigFalseAction(joinMap.EndCall.JoinNumber, EndAllCalls);

			trilist.SetBool(joinMap.HookState.JoinNumber, IsInCall);

			CallStatusChange += (sender, args) =>
			{
				trilist.SetBool(joinMap.HookState.JoinNumber, IsInCall);

				Debug.Console(1, this, "Call Direction: {0}", args.CallItem.Direction);
				Debug.Console(1, this, "Call is incoming: {0}", args.CallItem.Direction == eCodecCallDirection.Incoming);
				trilist.SetBool(joinMap.IncomingCall.JoinNumber, args.CallItem.Direction == eCodecCallDirection.Incoming && args.CallItem.Status == eCodecCallStatus.Ringing);

				if (args.CallItem.Direction == eCodecCallDirection.Incoming)
				{
					trilist.SetSigFalseAction(joinMap.IncomingAnswer.JoinNumber, () => AcceptCall(args.CallItem));
					trilist.SetSigFalseAction(joinMap.IncomingReject.JoinNumber, () => RejectCall(args.CallItem));
				}

				trilist.SetString(joinMap.CurrentCallData.JoinNumber, UpdateCallStatusXSig());
			};

            trilist.OnlineStatusChange += (device, args) =>
            {
                if (!args.DeviceOnLine) return;

                // TODO [ ] Issue #868
                trilist.SetString(joinMap.CurrentCallData.JoinNumber, "\xFC");
                UpdateCallStatusXSig();
            };
		}

		private string UpdateCallStatusXSig()
		{
			const int maxCalls = 8;
			const int maxStrings = 5;
			const int offset = 6;
			var stringIndex = 0;
			var digitalIndex = maxStrings * maxCalls;
			var arrayIndex = 0;

			var tokenArray = new XSigToken[maxCalls * offset]; //set array size for number of calls * pieces of info

			foreach (var call in ActiveCalls)
			{
				if (arrayIndex >= maxCalls * offset)
					break;
				//digitals
				tokenArray[arrayIndex] = new XSigDigitalToken(digitalIndex + 1, call.IsActiveCall);

				//serials
				tokenArray[arrayIndex + 1] = new XSigSerialToken(stringIndex + 1, call.Name ?? String.Empty);
				tokenArray[arrayIndex + 2] = new XSigSerialToken(stringIndex + 2, call.Number ?? String.Empty);
				tokenArray[arrayIndex + 3] = new XSigSerialToken(stringIndex + 3, call.Direction.ToString());
				tokenArray[arrayIndex + 4] = new XSigSerialToken(stringIndex + 4, call.Type.ToString());
				tokenArray[arrayIndex + 5] = new XSigSerialToken(stringIndex + 5, call.Status.ToString());

				arrayIndex += offset;
				stringIndex += maxStrings;
				digitalIndex++;
			}
			while (digitalIndex < maxCalls)
			{
				//digitals
				tokenArray[arrayIndex] = new XSigDigitalToken(digitalIndex + 1, false);

				//serials
				tokenArray[arrayIndex + 1] = new XSigSerialToken(stringIndex + 1, String.Empty);
				tokenArray[arrayIndex + 2] = new XSigSerialToken(stringIndex + 2, String.Empty);
				tokenArray[arrayIndex + 3] = new XSigSerialToken(stringIndex + 3, String.Empty);
				tokenArray[arrayIndex + 4] = new XSigSerialToken(stringIndex + 4, String.Empty);
				tokenArray[arrayIndex + 5] = new XSigSerialToken(stringIndex + 5, String.Empty);

				arrayIndex += offset;
				stringIndex += maxStrings;
				digitalIndex++;
			}

			return GetXSigString(tokenArray);
		}

		private void LinkVideoCodecDtmfToApi(BasicTriList trilist, VideoCodecControllerJoinMap joinMap)
		{
			trilist.SetSigFalseAction(joinMap.Dtmf0.JoinNumber, () => SendDtmf("0"));
			trilist.SetSigFalseAction(joinMap.Dtmf1.JoinNumber, () => SendDtmf("1"));
			trilist.SetSigFalseAction(joinMap.Dtmf2.JoinNumber, () => SendDtmf("2"));
			trilist.SetSigFalseAction(joinMap.Dtmf3.JoinNumber, () => SendDtmf("3"));
			trilist.SetSigFalseAction(joinMap.Dtmf4.JoinNumber, () => SendDtmf("4"));
			trilist.SetSigFalseAction(joinMap.Dtmf5.JoinNumber, () => SendDtmf("5"));
			trilist.SetSigFalseAction(joinMap.Dtmf6.JoinNumber, () => SendDtmf("6"));
			trilist.SetSigFalseAction(joinMap.Dtmf7.JoinNumber, () => SendDtmf("7"));
			trilist.SetSigFalseAction(joinMap.Dtmf8.JoinNumber, () => SendDtmf("8"));
			trilist.SetSigFalseAction(joinMap.Dtmf9.JoinNumber, () => SendDtmf("9"));
			trilist.SetSigFalseAction(joinMap.DtmfStar.JoinNumber, () => SendDtmf("*"));
			trilist.SetSigFalseAction(joinMap.DtmfPound.JoinNumber, () => SendDtmf("#"));
		}

		private void LinkVideoCodecCameraLayoutsToApi(IHasCodecLayouts codec, BasicTriList trilist, VideoCodecControllerJoinMap joinMap)
		{
			trilist.SetSigFalseAction(joinMap.CameraLayout.JoinNumber, codec.LocalLayoutToggle);

			codec.LocalLayoutFeedback.LinkInputSig(trilist.StringInput[joinMap.CameraLayoutStringFb.JoinNumber]);
		}

		private void LinkVideoCodecCameraModeToApi(IHasCameraAutoMode codec, BasicTriList trilist, VideoCodecControllerJoinMap joinMap)
		{
			trilist.SetSigFalseAction(joinMap.CameraModeAuto.JoinNumber, codec.CameraAutoModeOn);
			trilist.SetSigFalseAction(joinMap.CameraModeManual.JoinNumber, codec.CameraAutoModeOff);

			codec.CameraAutoModeIsOnFeedback.OutputChange += (o, a) =>
			{
				var offCodec = codec as IHasCameraOff;

				if (offCodec != null)
				{
					if (offCodec.CameraIsOffFeedback.BoolValue)
					{
						trilist.SetBool(joinMap.CameraModeAuto.JoinNumber, false);
						trilist.SetBool(joinMap.CameraModeManual.JoinNumber, false);
						trilist.SetBool(joinMap.CameraModeOff.JoinNumber, true);
						return;
					}

					trilist.SetBool(joinMap.CameraModeAuto.JoinNumber, a.BoolValue);
					trilist.SetBool(joinMap.CameraModeManual.JoinNumber, !a.BoolValue);
					trilist.SetBool(joinMap.CameraModeOff.JoinNumber, false);
					return;
				}

				trilist.SetBool(joinMap.CameraModeAuto.JoinNumber, a.BoolValue);
				trilist.SetBool(joinMap.CameraModeManual.JoinNumber, !a.BoolValue);
				trilist.SetBool(joinMap.CameraModeOff.JoinNumber, false);
			};

			var offModeCodec = codec as IHasCameraOff;

			if (offModeCodec != null)
			{
				if (offModeCodec.CameraIsOffFeedback.BoolValue)
				{
					trilist.SetBool(joinMap.CameraModeAuto.JoinNumber, false);
					trilist.SetBool(joinMap.CameraModeManual.JoinNumber, false);
					trilist.SetBool(joinMap.CameraModeOff.JoinNumber, true);
					return;
				}

				trilist.SetBool(joinMap.CameraModeAuto.JoinNumber, codec.CameraAutoModeIsOnFeedback.BoolValue);
				trilist.SetBool(joinMap.CameraModeManual.JoinNumber, !codec.CameraAutoModeIsOnFeedback.BoolValue);
				trilist.SetBool(joinMap.CameraModeOff.JoinNumber, false);
				return;
			}

			trilist.SetBool(joinMap.CameraModeAuto.JoinNumber, codec.CameraAutoModeIsOnFeedback.BoolValue);
			trilist.SetBool(joinMap.CameraModeManual.JoinNumber, !codec.CameraAutoModeIsOnFeedback.BoolValue);
			trilist.SetBool(joinMap.CameraModeOff.JoinNumber, false);
		}

		private void LinkVideoCodecSelfviewToApi(IHasCodecSelfView codec, BasicTriList trilist,
			VideoCodecControllerJoinMap joinMap)
		{
			trilist.SetSigFalseAction(joinMap.CameraSelfView.JoinNumber, codec.SelfViewModeToggle);

			codec.SelfviewIsOnFeedback.LinkInputSig(trilist.BooleanInput[joinMap.CameraSelfView.JoinNumber]);
		}

		private void LinkVideoCodecCameraToApi(IHasCodecCameras codec, BasicTriList trilist, VideoCodecControllerJoinMap joinMap)
		{
			//Camera PTZ
			trilist.SetBoolSigAction(joinMap.CameraTiltUp.JoinNumber, (b) =>
			{
				if (codec.SelectedCamera == null) return;
				var camera = codec.SelectedCamera as IHasCameraPtzControl;

				if (camera == null) return;

				if (b) camera.TiltUp();
				else camera.TiltStop();
			});

			trilist.SetBoolSigAction(joinMap.CameraTiltDown.JoinNumber, (b) =>
			{
				if (codec.SelectedCamera == null) return;
				var camera = codec.SelectedCamera as IHasCameraPtzControl;

				if (camera == null) return;

				if (b) camera.TiltDown();
				else camera.TiltStop();
			});
			trilist.SetBoolSigAction(joinMap.CameraPanLeft.JoinNumber, (b) =>
			{
				if (codec.SelectedCamera == null) return;
				var camera = codec.SelectedCamera as IHasCameraPtzControl;

				if (camera == null) return;

				if (b) camera.PanLeft();
				else camera.PanStop();
			});
			trilist.SetBoolSigAction(joinMap.CameraPanRight.JoinNumber, (b) =>
			{
				if (codec.SelectedCamera == null) return;
				var camera = codec.SelectedCamera as IHasCameraPtzControl;

				if (camera == null) return;

				if (b) camera.PanRight();
				else camera.PanStop();
			});

			trilist.SetBoolSigAction(joinMap.CameraZoomIn.JoinNumber, (b) =>
			{
				if (codec.SelectedCamera == null) return;
				var camera = codec.SelectedCamera as IHasCameraPtzControl;

				if (camera == null) return;

				if (b) camera.ZoomIn();
				else camera.ZoomStop();
			});

			trilist.SetBoolSigAction(joinMap.CameraZoomOut.JoinNumber, (b) =>
			{
				if (codec.SelectedCamera == null) return;
				var camera = codec.SelectedCamera as IHasCameraPtzControl;

				if (camera == null) return;

				if (b) camera.ZoomOut();
				else camera.ZoomStop();
			});

			//Camera Select
			trilist.SetUShortSigAction(joinMap.CameraNumberSelect.JoinNumber, (i) =>
			{
				if (codec.SelectedCamera == null) return;

				codec.SelectCamera(codec.Cameras[i].Key);
			});

			codec.CameraSelected += (sender, args) =>
			{
				var i = (ushort)codec.Cameras.FindIndex((c) => c.Key == args.SelectedCamera.Key);

				if (codec is IHasCodecRoomPresets)
				{
					return;
				}

				if (!(args.SelectedCamera is IHasCameraPresets))
				{
					return;
				}

				var cam = args.SelectedCamera as IHasCameraPresets;
				SetCameraPresetNames(cam.Presets);

				(args.SelectedCamera as IHasCameraPresets).PresetsListHasChanged += (o, eventArgs) => SetCameraPresetNames(cam.Presets);

				trilist.SetUShortSigAction(joinMap.CameraPresetSelect.JoinNumber,
						(a) =>
						{
							cam.PresetSelect(a);
							trilist.SetUshort(joinMap.CameraPresetSelect.JoinNumber, a);
						});

				trilist.SetSigFalseAction(joinMap.CameraPresetSave.JoinNumber,
					() =>
					{
						cam.PresetStore(trilist.UShortOutput[joinMap.CameraPresetSelect.JoinNumber].UShortValue,
							String.Empty);
						trilist.PulseBool(joinMap.CameraPresetSave.JoinNumber, 3000);
					});
			};

			if (!(codec is IHasCodecRoomPresets)) return;

			var presetCodec = codec as IHasCodecRoomPresets;

			presetCodec.CodecRoomPresetsListHasChanged +=
				(sender, args) => SetCameraPresetNames(presetCodec.NearEndPresets);

			//Camera Presets
			trilist.SetUShortSigAction(joinMap.CameraPresetSelect.JoinNumber, (i) =>
			{
				presetCodec.CodecRoomPresetSelect(i);

				trilist.SetUshort(joinMap.CameraPresetSelect.JoinNumber, i);
			});

			trilist.SetSigFalseAction(joinMap.CameraPresetSave.JoinNumber,
					() =>
					{
						presetCodec.CodecRoomPresetStore(
							trilist.UShortOutput[joinMap.CameraPresetSelect.JoinNumber].UShortValue, String.Empty);
						trilist.PulseBool(joinMap.CameraPresetSave.JoinNumber, 3000);
					});

            trilist.OnlineStatusChange += (device, args) =>
            {
                if (!args.DeviceOnLine) return;

                // TODO [ ] Issue #868
                trilist.SetString(joinMap.CameraPresetNames.JoinNumber, "\xFC");
                SetCameraPresetNames(presetCodec.NearEndPresets);
            };
		}

		private string SetCameraPresetNames(IEnumerable<CodecRoomPreset> presets)
		{
			return SetCameraPresetNames(presets.Select(p => p.Description).ToList());
		}

		private string SetCameraPresetNames(IEnumerable<CameraPreset> presets)
		{
			return SetCameraPresetNames(presets.Select(p => p.Description).ToList());
		}

		private string SetCameraPresetNames(ICollection<string> presets)
		{
			var i = 1; //start index for xsig;

			var tokenArray = new XSigToken[presets.Count];

			foreach (var preset in presets)
			{
				var cameraPreset = new XSigSerialToken(i, preset);
				tokenArray[i - 1] = cameraPreset;
				i++;
			}

			return GetXSigString(tokenArray);
		}

		private string GetXSigString(XSigToken[] tokenArray)
		{
			string returnString;
			using (var s = new MemoryStream())
			{
				using (var tw = new XSigTokenStreamWriter(s, true))
				{
					tw.WriteXSigData(tokenArray);
				}

				var xSig = s.ToArray();

				returnString = Encoding.GetEncoding(XSigEncoding).GetString(xSig, 0, xSig.Length);
			}

			return returnString;
		}

		#endregion
	}


	/// <summary>
	/// Used to track the status of syncronizing the phonebook values when connecting to a codec or refreshing the phonebook info
	/// </summary>
	public class CodecPhonebookSyncState : IKeyed
	{
		private bool _InitialSyncComplete;

		public CodecPhonebookSyncState(string key)
		{
			Key = key;

			CodecDisconnected();
		}

		public bool InitialSyncComplete
		{
			get { return _InitialSyncComplete; }
			private set
			{
				if (value == true)
				{
					var handler = InitialSyncCompleted;
					if (handler != null)
					{
						handler(this, new EventArgs());
					}
				}
				_InitialSyncComplete = value;
			}
		}

		public bool InitialPhonebookFoldersWasReceived { get; private set; }

		public bool NumberOfContactsWasReceived { get; private set; }

		public bool PhonebookRootEntriesWasRecieved { get; private set; }

		public bool PhonebookHasFolders { get; private set; }

		public int NumberOfContacts { get; private set; }

		#region IKeyed Members

		public string Key { get; private set; }

		#endregion

		public event EventHandler<EventArgs> InitialSyncCompleted;

		public void InitialPhonebookFoldersReceived()
		{
			InitialPhonebookFoldersWasReceived = true;

			CheckSyncStatus();
		}

		public void PhonebookRootEntriesReceived()
		{
			PhonebookRootEntriesWasRecieved = true;

			CheckSyncStatus();
		}

		public void SetPhonebookHasFolders(bool value)
		{
			PhonebookHasFolders = value;

			Debug.Console(1, this, "Phonebook has folders: {0}", PhonebookHasFolders);
		}

		public void SetNumberOfContacts(int contacts)
		{
			NumberOfContacts = contacts;
			NumberOfContactsWasReceived = true;

			Debug.Console(1, this, "Phonebook contains {0} contacts.", NumberOfContacts);

			CheckSyncStatus();
		}

		public void CodecDisconnected()
		{
			InitialPhonebookFoldersWasReceived = false;
			PhonebookHasFolders = false;
			NumberOfContacts = 0;
			NumberOfContactsWasReceived = false;
		}

		private void CheckSyncStatus()
		{
			if (InitialPhonebookFoldersWasReceived && NumberOfContactsWasReceived && PhonebookRootEntriesWasRecieved)
			{
				InitialSyncComplete = true;
				Debug.Console(1, this, "Initial Phonebook Sync Complete!");
			}
			else
			{
				InitialSyncComplete = false;
			}
		}
	}
}