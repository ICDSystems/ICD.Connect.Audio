using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces
{
	public interface IVolumeAttributeInterface : IAttributeInterface
	{
		[PublicAPI]
		event EventHandler<FloatEventArgs> OnLevelChanged;

		[PublicAPI]
		event EventHandler<BoolEventArgs> OnMuteChanged;

		#region Properties

		[PublicAPI]
		float Level { get; }

		[PublicAPI]
		float MinLevel { get; }

		[PublicAPI]
		float MaxLevel { get; }

		[PublicAPI]
		bool Mute { get; }

		#endregion

		#region Methods

		[PublicAPI]
		void SetLevel(float level);

		[PublicAPI]
		void IncrementLevel();

		[PublicAPI]
		void DecrementLevel();

		[PublicAPI]
		void IncrementLevel(float incrementValue);

		[PublicAPI]
		void DecrementLevel(float decrementValue);

		[PublicAPI]
		void SetMute(bool mute);

		#endregion
	}
}
