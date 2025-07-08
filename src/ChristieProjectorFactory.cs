using System.Collections.Generic;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;

namespace ChristieProjectorPlugin
{
    /// <summary>
    /// Factory class for creating Christie projector device instances
    /// </summary>
    public class ChristieProjectorFactory : EssentialsPluginDeviceFactory<Christie4K7HsController>
    {
        /// <summary>
        /// Initializes a new instance of the ChristieProjectorFactory class
        /// </summary>
        public ChristieProjectorFactory()
        {
            TypeNames = new List<string> { "ChristieProjector", "Christie4k7hsProjector", "Christie4k25rgbProjector" };

            MinimumEssentialsFrameworkVersion = "2.5.1";
        }

        /// <summary>
        /// Builds a Christie projector device instance based on the provided device configuration
        /// </summary>
        /// <param name="dc">The device configuration containing connection and property settings</param>
        /// <returns>An EssentialsDevice instance of the appropriate Christie projector type, or null if creation fails</returns>
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