using ICD.Connect.Audio.Controls.Mute;
using ICD.Connect.Audio.Controls.Volume;

namespace ICD.Connect.Audio.Biamp.Controls.Volume
{
	public interface IBiampTesiraVolumeDeviceControl : IBiampTesiraDeviceControl, 
													   IVolumeMuteFeedbackDeviceControl, 
													   IVolumeLevelDeviceControl
	{

	}
}