﻿using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Xml;
using ICD.Connect.Audio.QSys.Devices.QSysCore.Controls;

namespace ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedComponents
{
	public sealed class CameraNamedComponent : AbstractNamedComponent
	{
		public const string CONTROL_PAN_CURRENT = "pan.curr";
		public const string CONTROL_PAN_LEFT = "pan.left";
		public const string CONTROL_PAN_NEXT = "pan.next";
		public const string CONTROL_PAN_RIGHT = "pan.right";

		public const string CONTROL_TILT_CURRENT = "tilt.curr";
		public const string CONTROL_TILT_DOWN = "tilt.down";
		public const string CONTROL_TILT_NEXT = "tilt.next";
		public const string CONTROL_TILT_UP = "tilt.up";

		public const string CONTROL_ZOOM_CURRENT = "zoom.curr";
		public const string CONTROL_ZOOM_IN = "zoom.in";
		public const string CONTROL_ZOOM_NEXT = "zoom.next";
		public const string CONTROL_ZOOM_OUT = "zoom.out";

		public const string CONTROL_PRESET_HOME_LOAD = "preset.home.load";
		public const string CONTROL_PRESET_HOME_LOAD_STATE = "preset.home.load.state";
		public const string CONTROL_PRESET_HOME_LOAD_TRIGGER = "preset.home.load.trigger";
		public const string CONTROL_PRESET_HOME_PAN = "preset.home.pan";
		public const string CONTROL_PRESET_HOME_SAVE_TRIGGER = "preset.home.save.trigger";
		public const string CONTROL_PRESET_HOME_TILT = "preset.home.tilt";
		public const string CONTROL_PRESET_HOME_ZOOM = "preset.home.zoom";

		public const string CONTROL_PRESET_PRIVATE_LOAD = "preset.private.load";
		public const string CONTROL_PRESET_PRIVATE_LOAD_STATE = "preset.private.load.state";
		public const string CONTROL_PRESET_PRIVATE_LOAD_TRIGGER = "preset.private.load.trigger";
		public const string CONTROL_PRESET_PRIVATE_PAN = "preset.private.pan";
		public const string CONTROL_PRESET_PRIVATE_SAVE_TRIGGER = "preset.private.save.trigger";
		public const string CONTROL_PRESET_PRIVATE_TILT = "preset.private.tilt";
		public const string CONTROL_PRESET_PRIVATE_ZOOM = "preset.private.zoom";

		public const string CONTROL_TOGGLE_PRIVACY = "toggle.privacy";

		private static readonly IcdHashSet<string> s_Controls =
			new IcdHashSet<string>
			{
				CONTROL_PAN_CURRENT,
				CONTROL_PAN_LEFT,
				CONTROL_PAN_NEXT,
				CONTROL_PAN_RIGHT,
				CONTROL_TILT_CURRENT,
				CONTROL_TILT_DOWN,
				CONTROL_TILT_NEXT,
				CONTROL_TILT_UP,
				CONTROL_ZOOM_CURRENT,
				CONTROL_ZOOM_IN,
				CONTROL_ZOOM_NEXT,
				CONTROL_ZOOM_OUT,
				CONTROL_PRESET_HOME_LOAD,
				CONTROL_PRESET_HOME_LOAD_STATE,
				CONTROL_PRESET_HOME_LOAD_TRIGGER,
				CONTROL_PRESET_HOME_PAN,
				CONTROL_PRESET_HOME_SAVE_TRIGGER,
				CONTROL_PRESET_HOME_TILT,
				CONTROL_PRESET_HOME_ZOOM,
				CONTROL_PRESET_PRIVATE_LOAD,
				CONTROL_PRESET_PRIVATE_LOAD_STATE,
				CONTROL_PRESET_PRIVATE_LOAD_TRIGGER,
				CONTROL_PRESET_PRIVATE_PAN,
				CONTROL_PRESET_PRIVATE_SAVE_TRIGGER,
				CONTROL_PRESET_PRIVATE_TILT,
				CONTROL_PRESET_PRIVATE_ZOOM,
				CONTROL_TOGGLE_PRIVACY
			};

		private static readonly IcdHashSet<string> s_FeedbackControls =
			new IcdHashSet<string>
			{
				CONTROL_TOGGLE_PRIVACY
			};

		/// <summary>
		/// Constructor for Explicitly defined component
		/// </summary>
		/// <param name="id"></param>
		/// <param name="friendlyName"></param>
		/// <param name="context"></param>
		/// <param name="xml"></param>
		[UsedImplicitly]
		public CameraNamedComponent(int id, string friendlyName, CoreElementsLoadContext context, string xml)
			: base(context.QSysCore, friendlyName, id)
		{
			string componentName = XmlUtils.TryReadChildElementContentAsString(xml, "ComponentName");

			// If we don't have a component name, bail out
			if (string.IsNullOrEmpty(componentName))
				throw new InvalidOperationException(
					string.Format("Tried to create CameraNamedComponent {0}:{1} without component name", id, friendlyName));

			ComponentName = componentName;
			AddControls(s_Controls);
			SetupInitialChangeGroups(context, Enumerable.Empty<int>());
		}

		/// <summary>
		/// Constructor for Implicitly built component
		/// </summary>
		/// <param name="id"></param>
		/// <param name="context"></param>
		/// <param name="componentName"></param>
		[UsedImplicitly]
		public CameraNamedComponent(int id, CoreElementsLoadContext context, string componentName)
			: base(context.QSysCore, string.Format("Implicit:{0}", componentName), id)
		{
			ComponentName = componentName;
			AddControls(s_Controls);
			SetupInitialChangeGroups(context, Enumerable.Empty<int>());
		}

		protected override IEnumerable<INamedComponentControl> GetControlsForSubscribe()
		{
			return GetControls().Where(c => s_FeedbackControls.Contains(c.Name));
		}

		#region Console

		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		public override string ConsoleName { get { return string.Format("CameraComponent:{0}", Name); } }

		#endregion
	}
}
