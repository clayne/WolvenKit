using static WolvenKit.RED4.Types.Enums;

namespace WolvenKit.RED4.Types
{
	public partial class UIInGameNotificationQueue : gameuiGenericNotificationGameController
	{
		[Ordinal(4)] 
		[RED("duration")] 
		public CFloat Duration
		{
			get => GetPropertyValue<CFloat>();
			set => SetPropertyValue<CFloat>(value);
		}

		public UIInGameNotificationQueue()
		{
			Duration = 5.000000F;

			PostConstruct();
		}

		partial void PostConstruct();
	}
}
