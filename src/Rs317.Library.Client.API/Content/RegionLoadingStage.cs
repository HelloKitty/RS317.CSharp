using System;
using System.Collections.Generic;
using System.Text;

namespace Rs317
{
	public enum RegionLoadingStage
	{
		/// <summary>
		/// The default engine initialized value.
		/// </summary>
		Default = 0,

		/// <summary>
		/// Indicates a region is being loaded.
		/// </summary>
		LoadingRegion = 1,

		/// <summary>
		/// Indicates region is finished loaded.
		/// </summary>
		LoadingComplete = 2,
	}
}
