using static WolvenKit.RED4.Types.Enums;

namespace WolvenKit.RED4.Types
{
	public partial class questJournalPushPopQuestObjective_NodeType : questIJournal_NodeType
	{
		[Ordinal(0)] 
		[RED("path")] 
		public CHandle<gameJournalPath> Path
		{
			get => GetPropertyValue<CHandle<gameJournalPath>>();
			set => SetPropertyValue<CHandle<gameJournalPath>>(value);
		}

		[Ordinal(1)] 
		[RED("restore")] 
		public CBool Restore
		{
			get => GetPropertyValue<CBool>();
			set => SetPropertyValue<CBool>(value);
		}

		public questJournalPushPopQuestObjective_NodeType()
		{
			PostConstruct();
		}

		partial void PostConstruct();
	}
}
