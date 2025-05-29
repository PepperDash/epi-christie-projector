using System.Collections.Generic;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;

namespace ChristieProjectorPlugin
{
    public class ChristieProjectorFactory:EssentialsPluginDeviceFactory<EssentialsDevice>
    {
        public ChristieProjectorFactory()
        {
            TypeNames = new List<string> {"ChristieProjector", "Christie4k7hsProjector", "Christie4k25rgbProjector" };

            MinimumEssentialsFrameworkVersion = "2.5.1";
        }

        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
            var comms = CommFactory.CreateCommForDevice(dc);

            if (comms == null) return null;

			var config = dc.Properties.ToObject<ChristieProjectorPropertiesConfig>();
	        if (config == null)
	        {
		        return null;
	        }

	        switch (dc.Type.ToLower())
	        {
				case "christie4k7hsprojector":
					return new Christie4K7HsController(dc.Key, dc.Name, config, comms);
				case "christie4k25rgbprojector":
					return new Christie4K25RgbController(dc.Key, dc.Name, config, comms);
				default:
                    return new Christie4K7HsController(dc.Key, dc.Name, config, comms); ;
	        }
        }
    }
}