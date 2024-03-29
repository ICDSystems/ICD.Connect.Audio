﻿using System;
using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces
{
	public interface IStateAttributeInterface : IAttributeInterface
	{
		event EventHandler<BoolEventArgs> OnStateChanged; 

		bool State { get; }

		void SetState(bool state);
	}
}
