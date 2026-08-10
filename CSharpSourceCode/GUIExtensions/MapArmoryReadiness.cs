using System;
using System.Collections.Generic;
using System.Xml;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using Bannerlord.UIExtenderEx.ViewModels;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapBar;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace DynamicTroopEquipmentReupload.GUIExtensions;

[ViewModelMixin(nameof(MapBarVM.OnRefresh), handleDerived: true)]
internal sealed class MapArmoryReadinessMixin : BaseViewModelMixin<MapBarVM> {
	private const float INDICATOR_HEIGHT = 152f;

	private readonly Dictionary<CharacterObject, int> _lastMainPartyTroopCounts = new();
	private MobileParty? _trackedMainParty;
	private int _lastArmoryVersion = -1;
	private int _lastMemberRosterVersion = -1;
	private bool _hasReadinessContextSnapshot;
	private bool _lastPreferDefaultEquipmentThenClosest;
	private bool _lastMainPartyAtSea;
	private bool _isArmoryReadinessVisible;
	private float _armoryReadinessFillHeight;
	private HintViewModel _armoryReadinessHint;

	public MapArmoryReadinessMixin(MapBarVM mapBar) : base(mapBar) {
		_armoryReadinessHint = new HintViewModel();
	}

	public override void OnRefresh() {
		var mapBar = ViewModel;
		var mainParty = Campaign.Current != null ? MobileParty.MainParty : null;
		var canOpenArmory = mapBar != null && CanOpenArmory(mapBar, mainParty);

		IsArmoryReadinessVisible = canOpenArmory;
		if (!canOpenArmory || mainParty == null)
			return;
		if (!ReferenceEquals(_trackedMainParty, mainParty)) {
			_trackedMainParty = mainParty;
			_lastArmoryVersion = -1;
			_lastMemberRosterVersion = -1;
			_hasReadinessContextSnapshot = false;
			_lastMainPartyTroopCounts.Clear();
		}

		var armoryVersion = ArmyArmory.Armory.VersionNo;
		var memberRosterVersion = mainParty.MemberRoster.VersionNo;
		var preferDefaultEquipmentThenClosest = ModSettings.Instance?.PreferDefaultEquipmentThenClosest ?? true;
		var mainPartyAtSea = mainParty.IsCurrentlyAtSea;
		var readinessContextChanged = !_hasReadinessContextSnapshot ||
			_lastPreferDefaultEquipmentThenClosest != preferDefaultEquipmentThenClosest ||
			_lastMainPartyAtSea != mainPartyAtSea;
		var troopCompositionChanged = false;

		if (_lastMemberRosterVersion != memberRosterVersion) {
			_lastMemberRosterVersion = memberRosterVersion;
			troopCompositionChanged = MainPartyTroopCompositionChanged(mainParty);
		}

		if (_lastArmoryVersion == armoryVersion && !troopCompositionChanged && !readinessContextChanged)
			return;

		_lastArmoryVersion = armoryVersion;
		_hasReadinessContextSnapshot = true;
		_lastPreferDefaultEquipmentThenClosest = preferDefaultEquipmentThenClosest;
		_lastMainPartyAtSea = mainPartyAtSea;

		var readiness = PartyEquipmentDistributor.MeasureMainPartyArmoryReadiness(!mainPartyAtSea);
		ArmoryReadinessFillHeight = INDICATOR_HEIGHT * readiness.FillRatio;

		var readinessText = new TextObject("{=armory_readiness_hint}Army Armory readiness: {PERCENT}% ({FILLED}/{EXPECTED} required equipment slots filled)");
		readinessText.SetTextVariable("PERCENT", readiness.Percentage);
		readinessText.SetTextVariable("FILLED", readiness.EquippedSlots);
		readinessText.SetTextVariable("EXPECTED", readiness.ExpectedSlots);

		TextObject shortageText;
		if (readiness.LargestShortageCount > 0) {
			shortageText = new TextObject("{=armory_readiness_shortage}Most needed equipment: {TYPE} ({COUNT} missing)");
			shortageText.SetTextVariable("TYPE", GetArmoryShortageName(readiness.LargestShortageType));
			shortageText.SetTextVariable("COUNT", readiness.LargestShortageCount);
		}
		else {
			shortageText = new TextObject("{=armory_readiness_no_shortage}No equipment shortages");
		}

		var hintText = new TextObject("{READINESS}\n{SHORTAGE}");
		hintText.SetTextVariable("READINESS", readinessText);
		hintText.SetTextVariable("SHORTAGE", shortageText);
		ArmoryReadinessHint = new HintViewModel(hintText);
	}

	private static TextObject GetArmoryShortageName(PartyEquipmentDistributor.ArmoryShortageType shortageType) {
		return shortageType switch {
			PartyEquipmentDistributor.ArmoryShortageType.BodyArmor => new TextObject("{=armory_shortage_body_armor}Body armor"),
			PartyEquipmentDistributor.ArmoryShortageType.LegArmor => new TextObject("{=armory_shortage_leg_armor}Leg armor"),
			PartyEquipmentDistributor.ArmoryShortageType.HeadArmor => new TextObject("{=armory_shortage_helmets}Helmets"),
			PartyEquipmentDistributor.ArmoryShortageType.HandArmor => new TextObject("{=armory_shortage_gloves}Gloves"),
			PartyEquipmentDistributor.ArmoryShortageType.Cape => new TextObject("{=armory_shortage_capes}Capes"),
			PartyEquipmentDistributor.ArmoryShortageType.Horse => new TextObject("{=armory_shortage_horses}Horses"),
			PartyEquipmentDistributor.ArmoryShortageType.HorseHarness => new TextObject("{=armory_shortage_horse_armor}Horse armor"),
			PartyEquipmentDistributor.ArmoryShortageType.MeleeWeapon => new TextObject("{=armory_shortage_melee_weapons}Melee weapons"),
			PartyEquipmentDistributor.ArmoryShortageType.Shield => new TextObject("{=armory_shortage_shields}Shields"),
			PartyEquipmentDistributor.ArmoryShortageType.Bow => new TextObject("{=armory_shortage_bows}Bows"),
			PartyEquipmentDistributor.ArmoryShortageType.Crossbow => new TextObject("{=armory_shortage_crossbows}Crossbows"),
			PartyEquipmentDistributor.ArmoryShortageType.Sling => new TextObject("{=armory_shortage_slings}Slings"),
			PartyEquipmentDistributor.ArmoryShortageType.Quiver => new TextObject("{=armory_shortage_quivers}Quivers"),
			PartyEquipmentDistributor.ArmoryShortageType.Ammunition => new TextObject("{=armory_shortage_ammunition}Ammunition"),
			PartyEquipmentDistributor.ArmoryShortageType.ThrowingWeapon => new TextObject("{=armory_shortage_throwing_weapons}Throwing weapons"),
			PartyEquipmentDistributor.ArmoryShortageType.Pistol => new TextObject("{=armory_shortage_pistols}Pistols"),
			PartyEquipmentDistributor.ArmoryShortageType.Musket => new TextObject("{=armory_shortage_muskets}Muskets"),
			PartyEquipmentDistributor.ArmoryShortageType.Banner => new TextObject("{=armory_shortage_banners}Banners"),
			_ => new TextObject("{=armory_shortage_other_equipment}Other equipment")
		};
	}

	private bool MainPartyTroopCompositionChanged(MobileParty mainParty) {
		var currentTroopCounts = new Dictionary<CharacterObject, int>();

		foreach (var rosterElement in mainParty.MemberRoster.GetTroopRoster()) {
			if (rosterElement.Character is CharacterObject { IsHero: false } character && rosterElement.Number > 0)
				currentTroopCounts[character] = rosterElement.Number;
		}

		if (currentTroopCounts.Count == _lastMainPartyTroopCounts.Count) {
			var compositionChanged = false;
			foreach (var troopCount in currentTroopCounts) {
				if (!_lastMainPartyTroopCounts.TryGetValue(troopCount.Key, out var previousCount) || previousCount != troopCount.Value) {
					compositionChanged = true;
					break;
				}
			}

			if (!compositionChanged)
				return false;
		}

		_lastMainPartyTroopCounts.Clear();
		foreach (var troopCount in currentTroopCounts)
			_lastMainPartyTroopCounts.Add(troopCount.Key, troopCount.Value);

		return true;
	}

	private static bool CanOpenArmory(MapBarVM mapBar, MobileParty? mainParty) {
		return Campaign.Current != null &&
			   mainParty != null &&
			   Hero.MainHero is { IsPrisoner: false } &&
			   mapBar.IsEnabled &&
			   mapBar.MapTimeControl is { IsInMap: true, IsCenterPanelEnabled: true } &&
			   !mainParty.IsTransitionInProgress &&
			   mainParty.MapEvent == null &&
			   PlayerEncounter.Current == null;
	}

	[DataSourceMethod]
	public void ExecuteOpenArmyArmory() {
		var mapBar = ViewModel;
		var mainParty = Campaign.Current != null ? MobileParty.MainParty : null;
		if (mapBar != null && CanOpenArmory(mapBar, mainParty))
			ArmyArmoryBehavior.OpenArmoryScreen();
	}

	[DataSourceProperty]
	public bool IsArmoryReadinessVisible {
		get => _isArmoryReadinessVisible;
		private set {
			if (_isArmoryReadinessVisible == value)
				return;

			_isArmoryReadinessVisible = value;
			OnPropertyChangedWithValue(value);
		}
	}

	[DataSourceProperty]
	public float ArmoryReadinessFillHeight {
		get => _armoryReadinessFillHeight;
		private set {
			if (_armoryReadinessFillHeight.Equals(value))
				return;

			_armoryReadinessFillHeight = value;
			OnPropertyChangedWithValue(value);
		}
	}

	[DataSourceProperty]
	public HintViewModel ArmoryReadinessHint {
		get => _armoryReadinessHint;
		private set {
			if (ReferenceEquals(_armoryReadinessHint, value))
				return;

			_armoryReadinessHint = value;
			OnPropertyChangedWithValue(value);
		}
	}
}

[PrefabExtension("MapBar", "descendant::ListPanel[@Id='MapBar']")]
internal sealed class MapArmoryReadinessPrefab : PrefabExtensionInsertPatch {
	private readonly XmlDocument _document;

	public MapArmoryReadinessPrefab() {
		_document = new XmlDocument();
		_document.LoadXml(@"
<ButtonWidget WidthSizePolicy='Fixed'
              HeightSizePolicy='Fixed'
              SuggestedWidth='84'
              SuggestedHeight='140'
              HorizontalAlignment='Left'
              VerticalAlignment='Bottom'
              PositionXOffset='8'
              PositionYOffset='-72'
              IsVisible='@IsArmoryReadinessVisible'
              UpdateChildrenStates='true'
              DoNotPassEventsToChildren='true'
              Command.Click='ExecuteOpenArmyArmory'>
  <Children>
    <Widget WidthSizePolicy='StretchToParent'
            HeightSizePolicy='StretchToParent'
            Sprite='dt_readiness_bg'
            DoNotAcceptEvents='true' />
    <Widget WidthSizePolicy='StretchToParent'
            HeightSizePolicy='Fixed'
            SuggestedHeight='@ArmoryReadinessFillHeight'
            VerticalAlignment='Bottom'
            ClipContents='true'
            DoNotAcceptEvents='true'>
      <Children>
        <Widget WidthSizePolicy='StretchToParent'
                HeightSizePolicy='Fixed'
                SuggestedHeight='140'
                VerticalAlignment='Bottom'
                Sprite='dt_readiness_fill'
                DoNotAcceptEvents='true' />
      </Children>
    </Widget>
    <HintWidget DataSource='{ArmoryReadinessHint}'
                WidthSizePolicy='StretchToParent'
                HeightSizePolicy='StretchToParent'
                Command.HoverBegin='ExecuteBeginHint'
                Command.HoverEnd='ExecuteEndHint' />
  </Children>
</ButtonWidget>");
	}

	public override InsertType Type => InsertType.Prepend;

	[PrefabExtensionXmlDocument]
	public XmlDocument GetPrefabExtension() => _document;
}
