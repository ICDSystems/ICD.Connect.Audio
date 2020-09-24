using ICD.Common.Logging.Activities;
using ICD.Common.Utils.Services.Logging;

namespace ICD.Connect.Audio.Controls.Volume
{
	public static class VolumeDeviceControlActivities
	{
		public static Activity GetMutedActivity(bool isMuted)
		{
			return isMuted
				? new Activity(Activity.ePriority.Medium, "IsMuted", "Muted", eSeverity.Informational)
				: new Activity(Activity.ePriority.Low, "IsMuted", "Unmuted", eSeverity.Informational);
		}
	}
}
