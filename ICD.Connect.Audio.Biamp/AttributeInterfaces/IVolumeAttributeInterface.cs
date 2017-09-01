﻿using System;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Properties;

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces
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
		void SetMute(bool mute);

		#endregion
	}
}
