using System;

namespace ChristieProjectorPlugin
{
	public static class DebugExtension
	{
		public static uint Trace { get; set; }
		public static uint Notice { get; set; }
		public static uint Verbose { get; set; }


		public static void ResetDebugLevels()
		{
			Trace = 0;
			Notice = 1;
			Verbose = 2;
		}

		public static void SetDebugLevels(uint level)
		{
			Trace = level;
			Notice = level;
			Verbose = level;
		}

	}
}