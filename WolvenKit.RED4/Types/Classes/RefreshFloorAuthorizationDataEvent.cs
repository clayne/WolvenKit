using static WolvenKit.RED4.Types.Enums;

namespace WolvenKit.RED4.Types
{
	public partial class RefreshFloorAuthorizationDataEvent : redEvent
	{
		[Ordinal(0)] 
		[RED("passToEntity")] 
		public CBool PassToEntity
		{
			get => GetPropertyValue<CBool>();
			set => SetPropertyValue<CBool>(value);
		}

		public RefreshFloorAuthorizationDataEvent()
		{
			PostConstruct();
		}

		partial void PostConstruct();
	}
}
