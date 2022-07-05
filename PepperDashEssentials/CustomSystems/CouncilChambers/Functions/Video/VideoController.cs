using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Room.Config;

using PepperDash.Essentials.DM;

namespace CI.Essentials.Video
{
    public class VideoController : IKeyed
    {
        string IKeyed.Key { get { return "VidController"; } }

        public IKeyed device { get; private set; }
        //IVideoPropertiesConfig config;
        //DmChassisController dm;

        public VideoController()
        {
            //var cs_ = DeviceManager.GetDeviceForKey("processor-avRouting");
            //Debug.Console(0, this, "controlsystem.type: {0}", cs_.ToString());

            //var config = DeviceManager.AllDevices.FindAll(x => x[x.Key].;
            foreach (var d in DeviceManager.AllDevices)
            {
                var dev_ = DeviceManager.GetDeviceForKey(d.Key);
                Debug.Console(1, this, "[{0}]", dev_.Key);
                if (dev_ is DmChassisController)
                {
                    Debug.Console(1, "[{0}] is DmChassisController", dev_.Key);
                    var dm_ = dev_ as DmChassisController;
                    foreach (var i in dm_.InputNames)
                    {
                        // "InputNames [1] VGA Input #1"
                        // "InputPorts key: inputCard5--HdmiIn5"
                        // "InputPorts Port.ToString: DMPS3-4K-150-C Input 5: HDMI: Hdmi Stream"
                        Debug.Console(1, this, "[{0}] input {1}: {2}", dm_.Name, i.Key, i.Value);
                    }
                    foreach (var o in dm_.OutputNames)
                    {
                        Debug.Console(1, this, "[{0}] output {1}: {2}", dm_.Name, o.Key, o.Value);
                    }
                    //dm_.VideoInputSyncFeedbacks
                }
                if (dev_ is DmpsRoutingController)
                {
                    Debug.Console(1, this, "[{0}] is DmpsRoutingController", dev_.Key);
                    var dmps_ = dev_ as DmpsRoutingController;
                    Debug.Console(0, this, "InputNames");
                    foreach (var i in dmps_.InputNames)
                        Debug.Console(1, this, "[{0}] input {1}: {2}", dmps_.Name, i.Key, i.Value);
                    Debug.Console(0, this, "OutputNames");
                    foreach (var o in dmps_.OutputNames)
                        Debug.Console(1, this, "[{0}] output {1}: {2}", dmps_.Name, o.Key, o.Value);
                    Debug.Console(0, this, "InputPorts");
                    foreach (var i in dmps_.InputPorts)
                    {
                        if (i.Key != null)
                        {
                            Debug.Console(0, this, "InputPorts key: {0}", i.Key);
                            Debug.Console(0, this, "InputPorts Port.ToString: {0}", i.Port.ToString());
                        }
                    }
                    Debug.Console(0, this, "OutputPorts");
                    foreach (var o in dmps_.OutputPorts)
                    {
                        if (o.Key != null)
                        {
                            Debug.Console(0, this, "OutputPorts key: {0}", o.Key);
                            Debug.Console(0, this, "OutputPorts Port.ToString: {0}", o.Port.ToString());
                        }
                    }
                    Debug.Console(0, this, "VolumeControls");
                    foreach (var o in dmps_.VolumeControls)
                    {
                        if (o.Key != null)
                        {
                            Debug.Console(0, this, "VolumeControls key: {0}", o.Key);
                            Debug.Console(0, this, "VolumeControls Output.Number: {0}", o.Value.Output.Number);
                            Debug.Console(0, this, "VolumeControls OutputVolume.Name: {0}", o.Value.Output.Volume.Name);
                            Debug.Console(0, this, "VolumeControls OutputVolume.Number: {0}", o.Value.Output.Volume.Number);
                        }
                    }
                    Debug.Console(0, this, "Microphones {0}", dmps_.Microphones);
                }
                if (dev_ is IRoutingNumericWithFeedback)
                {
                    Debug.Console(1, this, "[{0}] is IRoutingNumericWithFeedback", dev_.Key);
                    var switcher_ = dev_ as IRoutingNumericWithFeedback;
                    foreach (var i in switcher_.InputPorts)
                    {
                        Debug.Console(1, this, "[{0}] input {1}: {2}", switcher_.Name, i.Key, i.Port.ToString());
                    }
                }
            }
        }
    }

    public interface IVideoPropertiesConfig
    {
        string test { get; set; }
    }
}