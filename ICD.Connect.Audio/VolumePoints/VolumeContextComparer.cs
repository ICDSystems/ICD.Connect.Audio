using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;

namespace ICD.Connect.Audio.VolumePoints
{
	/// <summary>
	/// Comparer which determines the greater volume point based on contextual priority using flags.
	/// </summary>
	public sealed class VolumeContextComparer : IComparer<IVolumePoint>
	{
		private readonly eVolumePointContext m_Context;

		public VolumeContextComparer(eVolumePointContext context)
		{
			m_Context = context;
		}

		public int Compare(IVolumePoint x, IVolumePoint y)
		{
			foreach (eVolumePointContext flag in EnumUtils.GetFlagsExceptNone(m_Context))
			{
				bool xHasFlag = x.Context.HasFlag(flag);
				bool yHasFlag = y.Context.HasFlag(flag);

				if (xHasFlag && !yHasFlag)
					return 1;

				if (yHasFlag)
					return -1;
			}

			return 0;
		}
	}
}
