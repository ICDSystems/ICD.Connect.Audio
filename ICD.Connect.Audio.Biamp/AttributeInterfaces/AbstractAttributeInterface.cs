using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Biamp.Controls;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces
{
	/// <summary>
	/// Base class for all classes that communicate with the BiampTesiraDevice.
	/// </summary>
	public abstract class AbstractAttributeInterface: IConsoleNode, IDisposable, IAttributeInterface
	{
		private readonly string m_InstanceTag;
		private readonly BiampTesiraDevice m_Device;

		#region Properties

		/// <summary>
		/// Gets the instance tag for this component.
		/// </summary>
		public string InstanceTag { get { return m_InstanceTag; } }

		/// <summary>
		/// Gets the parent device for this component.
		/// </summary>
		public BiampTesiraDevice Device { get { return m_Device; } }

		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		public string ConsoleName { get { return InstanceTag; } }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public virtual string ConsoleHelp { get { return string.Empty; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		protected AbstractAttributeInterface(BiampTesiraDevice device, string instanceTag)
		{
			m_InstanceTag = instanceTag;
			m_Device = device;

			Subscribe(m_Device);
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public virtual void Dispose()
		{
			Unsubscribe(m_Device);
		}

		/// <summary>
		/// Gets the child attribute interface at the given path.
		/// </summary>
		/// <param name="channelType"></param>
		/// <param name="indices"></param>
		/// <returns></returns>
		public virtual IAttributeInterface GetAttributeInterface(eChannelType channelType, params int[] indices)
		{
			switch (channelType)
			{
				default:
					string message = string.Format("{0} has no channel type {1}", GetType().Name, channelType);
					throw new ArgumentOutOfRangeException("channelType", message);
			}
		}

		#endregion

		#region Protected Methods

		/// <summary>
		/// Logs the message with the device configured logger.
		/// </summary>
		/// <param name="severity"></param>
		/// <param name="message"></param>
		/// <param name="args"></param>
		public virtual void Log(eSeverity severity, string message, params object[] args)
		{
			message = string.Format("{0} - {1}", InstanceTag, message);
			Device.Log(severity, message, args);
		}

		/// <summary>
		/// Override to request initial values from the device, and subscribe for feedback.
		/// </summary>
		public virtual void Initialize()
		{
		}

	    public virtual void Deinitialize()
	    {
	    }

		/// <summary>
		/// Builds an attribute code and sends it to the device.
		/// </summary>
		/// <param name="callback"></param>
		/// <param name="command"></param>
		/// <param name="attribute"></param>
		/// <param name="value"></param>
		/// <param name="indices"></param>
		protected void RequestAttribute(BiampTesiraDevice.SubscriptionCallback callback, AttributeCode.eCommand command,
										string attribute, IValue value, params int[] indices)
		{
			AttributeCode code;

			switch (command)
			{
				// One-off command can be sent to the device as-is
				case AttributeCode.eCommand.Get:
					code = AttributeCode.Get(m_InstanceTag, attribute, indices);
					m_Device.SendData(callback, code);
					break;
				case AttributeCode.eCommand.Set:
					code = AttributeCode.Set(m_InstanceTag, attribute, value, indices);
					m_Device.SendData(callback, code);
					break;
				case AttributeCode.eCommand.Increment:
					code = AttributeCode.Increment(m_InstanceTag, attribute, indices);
					m_Device.SendData(callback, code);
					break;
				case AttributeCode.eCommand.Decrement:
					code = AttributeCode.Decrement(m_InstanceTag, attribute, indices);
					m_Device.SendData(callback, code);
					break;
				case AttributeCode.eCommand.Toggle:
					code = AttributeCode.Toggle(m_InstanceTag, attribute, indices);
					m_Device.SendData(callback, code);
					break;

				// Subscriptions are registered with the device for later lookup
				case AttributeCode.eCommand.Subscribe:
					m_Device.SubscribeAttribute(callback, m_InstanceTag, attribute, indices);
					break;
				case AttributeCode.eCommand.Unsubscribe:
					m_Device.UnsubscribeAttribute(callback, m_InstanceTag, attribute, indices);
					break;

				default:
					throw new ArgumentOutOfRangeException("command");
			}

			// Query the state of the attribute if we are modifying it.
			switch (command)
			{
				case AttributeCode.eCommand.Set:
				case AttributeCode.eCommand.Increment:
				case AttributeCode.eCommand.Decrement:
				case AttributeCode.eCommand.Toggle:
				case AttributeCode.eCommand.Subscribe:
					RequestAttribute(callback, AttributeCode.eCommand.Get, attribute, null, indices);
					break;
			}
		}

		/// <summary>
		/// Builds a service code and sends it to the device.
		/// </summary>
		/// <param name="service"></param>
		/// <param name="value"></param>
		/// <param name="indices"></param>
		protected void RequestService(string service, IValue value, params int[] indices)
		{
			ServiceCode code = new ServiceCode(m_InstanceTag, service, value, indices.Cast<object>().ToArray());
			m_Device.SendData(code);
		}

		#endregion

		#region Device Callbacks

		/// <summary>
		/// Subscribes to the codec events.
		/// </summary>
		/// <param name="device"></param>
		private void Subscribe(BiampTesiraDevice device)
		{
			device.OnInitializedChanged += DeviceOnInitializedChanged;
		}

		/// <summary>
		/// Unsubscribes from the codec events.
		/// </summary>
		/// <param name="device"></param>
		private void Unsubscribe(BiampTesiraDevice device)
		{
			device.OnInitializedChanged -= DeviceOnInitializedChanged;
		}

		/// <summary>
		/// Called when the device initializes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void DeviceOnInitializedChanged(object sender, BoolEventArgs args)
		{
			if (args.Data)
				Initialize();
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public virtual IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			yield break;
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public virtual void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			addRow("InstanceTag", m_InstanceTag);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public virtual IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield return new ConsoleCommand("Initialize", "Updates to the latest values from the device", () => Initialize());
		}

		#endregion
	}
}
