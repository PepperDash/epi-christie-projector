using System;

namespace ChristieProjectorPlugin
{
	/// <summary>
	/// Static class providing debug level configuration for Christie projector plugin
	/// </summary>
	public static class DebugExtension
	{
		/// <summary>
		/// Gets or sets the debug level for trace messages
		/// </summary>
		public static uint Trace { get; set; }

		/// <summary>
		/// Gets or sets the debug level for notice messages
		/// </summary>
		public static uint Notice { get; set; }

		/// <summary>
		/// Gets or sets the debug level for verbose messages
		/// </summary>
		public static uint Verbose { get; set; }


		/// <summary>
		/// Resets all debug levels to their default values (Trace=0, Notice=1, Verbose=2)
		/// </summary>
		public static void ResetDebugLevels()
		{
			Trace = 0;
			Notice = 1;
			Verbose = 2;
		}

		/// <summary>
		/// Sets all debug levels to the specified value
		/// </summary>
		/// <param name="level">The debug level to apply to all debug categories</param>
		public static void SetDebugLevels(uint level)
		{
			Trace = level;
			Notice = level;
			Verbose = level;
		}

	}
}