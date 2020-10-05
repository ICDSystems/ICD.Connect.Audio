using ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces;
using ICD.Connect.Settings;

namespace ICD.Connect.Audio.Biamp.Tesira.Devices
{
	public abstract class AbstractTesiraChildAttributeInterfaceDevice<TAttributeInterface, TSettings> : AbstractTesiraChildDevice<TSettings>
		where TSettings : ITesiraChildAttributInterfaceDeviceSettings, new()
		where TAttributeInterface : AbstractAttributeInterface
	{
		private TAttributeInterface m_AttributInterface;

		protected TAttributeInterface AttributeInterface {get { return m_AttributInterface; }}

		#region Settings

		protected override void ClearSettingsFinal()
		{
			SetAttributeInterface((TAttributeInterface)null);

			base.ClearSettingsFinal();
		}

		protected override void CopySettingsFinal(TSettings settings)
		{
			base.CopySettingsFinal(settings);
			
			settings.InstanceTag = AttributeInterface == null ? null : AttributeInterface.InstanceTag;
		}

		protected override void ApplySettingsFinal(TSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			SetAttributeInterface(settings.InstanceTag);
		}

		protected void SetAttributeInterface(string instanceTag)
		{
			if (Biamp == null || instanceTag == null)
				SetAttributeInterface((TAttributeInterface) null);
			else
				SetAttributeInterface(Biamp.AttributeInterfaces.LazyLoadAttributeInterface<TAttributeInterface>(instanceTag));
		}

		protected virtual void SetAttributeInterface(TAttributeInterface attributeInterface)
		{
			Unsubscribe(m_AttributInterface);
			m_AttributInterface = attributeInterface;
			Subscribe(m_AttributInterface);

			UpdateCachedOnlineStatus();
		}

		#endregion



		#region AttributInterface Callbacks

		protected virtual void Subscribe(TAttributeInterface attributeInterface)
		{
			
		}

		protected virtual void Unsubscribe(TAttributeInterface attributeInterface)
		{
			
		}

		#endregion
	}
}