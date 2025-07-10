using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;

namespace ChristieProjectorPlugin
{
    /// <summary>
    /// Bridge join map for Christie projector devices, extending the base DisplayControllerJoinMap
    /// with Christie-specific joins for lamp, screen, lift, video mute, and other functionality
    /// </summary>
    public class ChristieProjectorBridgeJoinMap : DisplayControllerJoinMap
    {
        #region Digitals

        //[JoinName("PowerOff")]
        //public JoinDataComplete PowerOff = new JoinDataComplete(
        //    new JoinData
        //    {
        //        JoinNumber = 1,
        //        JoinSpan = 1
        //    },
        //    new JoinMetadata
        //    {
        //        Description = "Power Off",
        //        JoinCapabilities = eJoinCapabilities.FromSIMPL,
        //        JoinType = eJoinType.Digital
        //    });

        //[JoinName("PowerOn")]
        //public JoinDataComplete PowerOn = new JoinDataComplete(
        //    new JoinData
        //    {
        //        JoinNumber = 2,
        //        JoinSpan = 1
        //    },
        //    new JoinMetadata
        //    {
        //        Description = "Power On",
        //        JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
        //        JoinType = eJoinType.Digital
        //    });

        //[JoinName("IsTwoWayDisplay")]
        //public JoinDataComplete IsTwoWayDisplay = new JoinDataComplete(
        //    new JoinData
        //    {
        //        JoinNumber = 3,
        //        JoinSpan = 1
        //    },
        //    new JoinMetadata
        //    {
        //        Description = "Is Two Way Display",
        //        JoinCapabilities = eJoinCapabilities.ToSIMPL,
        //        JoinType = eJoinType.Digital
        //    });

        //[JoinName("InputSelectOffset")]
        //public JoinDataComplete InputSelectOffset = new JoinDataComplete(
        //    new JoinData
        //    {
        //        JoinNumber = 11,
        //        JoinSpan = 10
        //    },
        //    new JoinMetadata
        //    {
        //        Description = "Input Select",
        //        JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
        //        JoinType = eJoinType.Digital
        //    });

        /// <summary>
        /// Digital join for indicating whether the device has lamps for feedback purposes
        /// </summary>
        [JoinName("HasLamps")]
        public JoinDataComplete HasLamps = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 31,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Has lamps feedback",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });

        /// <summary>
        /// Digital join for indicating whether the device has a screen for feedback purposes
        /// </summary>
        [JoinName("HasScreen")]
        public JoinDataComplete HasScreen = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 32,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Has screen feedback",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });

        /// <summary>
        /// Digital join for indicating whether the device has a lift mechanism for feedback purposes
        /// </summary>
        [JoinName("HasLift")]
        public JoinDataComplete HasLift = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 33,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Has lift feedback",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });

        /// <summary>
        /// Digital join for indicating when the device is in warming up state
        /// </summary>
        [JoinName("IsWarming")]
        public JoinDataComplete IsWarming = new JoinDataComplete(
                new JoinData
                {
                    JoinNumber = 36,
                    JoinSpan = 1
                },
                new JoinMetadata
                {
                    Description = "Device is warming feedback",
                    JoinCapabilities = eJoinCapabilities.ToSIMPL,
                    JoinType = eJoinType.Digital
                });

        /// <summary>
        /// Digital join for indicating when the device is in cooling down state
        /// </summary>
        [JoinName("IsCooling")]
        public JoinDataComplete IsCooling = new JoinDataComplete(
                new JoinData
                {
                    JoinNumber = 37,
                    JoinSpan = 1
                },
                new JoinMetadata
                {
                    Description = "Device is cooling feedback",
                    JoinCapabilities = eJoinCapabilities.ToSIMPL,
                    JoinType = eJoinType.Digital
                });

        /// <summary>
        /// Digital join for turning video mute on and providing feedback
        /// </summary>
        [JoinName("VideoMuteOn")]
        public JoinDataComplete VideoMuteOn = new JoinDataComplete(
                new JoinData
                {
                    JoinNumber = 38,
                    JoinSpan = 1
                },
                new JoinMetadata
                {
                    Description = "Video mute on and feedback",
                    JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                    JoinType = eJoinType.Digital
                });

        /// <summary>
        /// Digital join for turning video mute off and providing feedback
        /// </summary>
        [JoinName("VideoMuteOff")]
        public JoinDataComplete VideoMuteOff = new JoinDataComplete(
                new JoinData
                {
                    JoinNumber = 39,
                    JoinSpan = 1
                },
                new JoinMetadata
                {
                    Description = "Video mute off and feedback",
                    JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                    JoinType = eJoinType.Digital
                });

        /// <summary>
        /// Digital join for toggling video mute state
        /// </summary>
        [JoinName("VideoMuteToggle")]
        public JoinDataComplete VideoMuteToggle = new JoinDataComplete(
                new JoinData
                {
                    JoinNumber = 40,
                    JoinSpan = 1
                },
                new JoinMetadata
                {
                    Description = "Video mute toggle",
                    JoinCapabilities = eJoinCapabilities.FromSIMPL,
                    JoinType = eJoinType.Digital
                });

        //[JoinName("ButtonVisibilityOffset")]
        //public JoinDataComplete ButtonVisibilityOffset = new JoinDataComplete(
        //    new JoinData
        //    {
        //        JoinNumber = 41,
        //        JoinSpan = 10
        //    },
        //    new JoinMetadata
        //    {
        //        Description = "Button Visibility Offset",
        //        JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
        //        JoinType = eJoinType.DigitalSerial
        //    });

        //[JoinName("IsOnline")]
        //public JoinDataComplete IsOnline = new JoinDataComplete(
        //    new JoinData
        //    {
        //        JoinNumber = 50,
        //        JoinSpan = 1
        //    },
        //    new JoinMetadata
        //    {
        //        Description = "Is Online",
        //        JoinCapabilities = eJoinCapabilities.ToSIMPL,
        //        JoinType = eJoinType.Digital
        //    });

        #endregion


        #region Analogs

        //[JoinName("InputSelect")]
        //public JoinDataComplete InputSelect = new JoinDataComplete(
        //    new JoinData
        //    {
        //        JoinNumber = 11,
        //        JoinSpan = 1
        //    },
        //    new JoinMetadata
        //    {
        //        Description = "Input Select",
        //        JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
        //        JoinType = eJoinType.Analog
        //    });

        /// <summary>
        /// Analog join for reporting current lamp hours
        /// </summary>
        [JoinName("LampHours")]
        public JoinDataComplete LampHours = new JoinDataComplete(
                new JoinData
                {
                    JoinNumber = 6,
                    JoinSpan = 1
                },
                new JoinMetadata
                {
                    Description = "Reports current lamp hours",
                    JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                    JoinType = eJoinType.Analog
                });

        #endregion


        #region Serials

        //[JoinName("Name")]
        //public JoinDataComplete Name = new JoinDataComplete(
        //    new JoinData
        //    {
        //        JoinNumber = 1,
        //        JoinSpan = 1
        //    },
        //    new JoinMetadata
        //    {
        //        Description = "Name",
        //        JoinCapabilities = eJoinCapabilities.ToSIMPL,
        //        JoinType = eJoinType.Serial
        //    });

        //[JoinName("InputNamesOffset")]
        //public JoinDataComplete InputNamesOffset = new JoinDataComplete(
        //    new JoinData
        //    {
        //        JoinNumber = 11,
        //        JoinSpan = 10
        //    },
        //    new JoinMetadata
        //    {
        //        Description = "Input Names Offset",
        //        JoinCapabilities = eJoinCapabilities.ToSIMPL,
        //        JoinType = eJoinType.Serial
        //    });

        #endregion


        /// <summary>
        /// Initializes a new instance of the ChristieProjectorBridgeJoinMap class with the specified starting join number
        /// </summary>
        /// <param name="joinStart">The starting join number for this join map</param>
        public ChristieProjectorBridgeJoinMap(uint joinStart)
            : base(joinStart, typeof(ChristieProjectorBridgeJoinMap))
        {

        }
    }
}