﻿using System.Collections.Generic;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;

namespace ChristieProjectorPlugin
{
    public class ChristieProjectorFactory:EssentialsPluginDeviceFactory<Christie4K7HsController>
    {
        public ChristieProjectorFactory()
        {
            TypeNames = new List<string> {"ChristieProjector", "Christie4k7hsProjector", "Christie4k25rgbProjector" };

            MinimumEssentialsFrameworkVersion = "1.10.3";
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
				case "Christie4k7hsprojector":
					return new Christie4K7HsController(dc.Key, dc.Name, config, comms);
				//case "Christie4k25rgbcpprojector":
				//	return new Christie4k25rgbController(dc.Key, dc.Name, config, comms);
				default:
                    return new Christie4K7HsController(dc.Key, dc.Name, config, comms); ;
	        }
        }
    }
}