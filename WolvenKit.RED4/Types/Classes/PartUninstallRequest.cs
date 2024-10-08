using static WolvenKit.RED4.Types.Enums;

namespace WolvenKit.RED4.Types
{
	public partial class PartUninstallRequest : gamePlayerScriptableSystemRequest
	{
		[Ordinal(1)] 
		[RED("itemID")] 
		public gameItemID ItemID
		{
			get => GetPropertyValue<gameItemID>();
			set => SetPropertyValue<gameItemID>(value);
		}

		[Ordinal(2)] 
		[RED("partID")] 
		public gameItemID PartID
		{
			get => GetPropertyValue<gameItemID>();
			set => SetPropertyValue<gameItemID>(value);
		}

		public PartUninstallRequest()
		{
			ItemID = new gameItemID();
			PartID = new gameItemID();

			PostConstruct();
		}

		partial void PostConstruct();
	}
}
