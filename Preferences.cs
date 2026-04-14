using MelonLoader;
using System.ComponentModel.DataAnnotations;

namespace SketchbookPlus
{
    internal enum OutfitTypes
    {
        [Display(Name = "None")]
        None,
        [Display(Name = "Outfit From Selected File")]
        SavedOutfit,
        [Display(Name = "Random Outfit From Files")]
        RandomFromFilesOutfit,
        [Display(Name = "Randomizer Outfit")]
        RandomizerOutfit
    }

    public class Preferences
	{
		private const string CONFIG_FILE = "config.cfg";
		private const string USER_DATA = "UserData/SketchbookPlus/";
        internal static Dictionary<MelonPreferences_Entry, object> LastSavedValues = new();

        internal static MelonPreferences_Category SketchbookPlusCategory;
        internal static MelonPreferences_Entry<OutfitTypes> PrefOutfitTypeToLoad;
        internal static MelonPreferences_Entry<bool> PrefStoreOthersOutfits;
        internal static MelonPreferences_Entry<bool> PrefUpdateFilesTexts;
        internal static MelonPreferences_Entry<bool> PrefDebugging;

        internal static MelonPreferences_Category SaveLoadCategory;
		internal static MelonPreferences_Entry<bool> PrefSaveOutfit;
		internal static MelonPreferences_Entry<string> PrefSavedOutfitName;
		internal static MelonPreferences_Entry<string> PrefOutfitNameToLoad;
		

        internal static MelonPreferences_Category RandomizerCategory;
		internal static MelonPreferences_Entry<bool> PrefRandomizerIdentity;
		internal static MelonPreferences_Entry<bool> PrefRandomizerArmorColors;
		internal static MelonPreferences_Entry<bool> PrefArmorItems;
		internal static MelonPreferences_Entry<bool> PrefRandomizerMarkings;

        internal static void InitPrefs()
		{
			if (!Directory.Exists(USER_DATA)) { Directory.CreateDirectory(USER_DATA); }

            //General settings
            SketchbookPlusCategory = MelonPreferences.CreateCategory("SketchbookPlus", "Settings");
            SketchbookPlusCategory.SetFilePath(Path.Combine(USER_DATA, CONFIG_FILE));

            PrefOutfitTypeToLoad = SketchbookPlusCategory.CreateEntry("OutfitTypeToLoad", OutfitTypes.None, "Outfit Type to Load", "Selects what type of Outfit to Load");
            PrefStoreOthersOutfits = SketchbookPlusCategory.CreateEntry("StoreOthersOutfits", false, "Store Others Outfits", "Set to True to Store Other Player's Cosmetics to UserData/SketchBookPlus/Stored");
            PrefUpdateFilesTexts = SketchbookPlusCategory.CreateEntry("UpdateFilesTexts", false, "Reload Text Files", "Set to True to refresh the Mod's Data from Files");
            PrefDebugging = SketchbookPlusCategory.CreateEntry("Debugging", false, "Enable Debugging Logs", "Set to True to Show Logs");

            //Save & Load Category settings
            SaveLoadCategory = MelonPreferences.CreateCategory("SaveAndLoad", "Save & Load");
            SaveLoadCategory.SetFilePath(Path.Combine(USER_DATA, CONFIG_FILE));

            PrefSaveOutfit = SaveLoadCategory.CreateEntry("SaveOutfit", false, "Save Outfit", "Set to True and Save to Store the Equipped Outfit");
            PrefSavedOutfitName = SaveLoadCategory.CreateEntry("SavedOutfitName", "DefaultName", "Saved Outfit Name", "File Name To Save Under");
            PrefOutfitNameToLoad = SaveLoadCategory.CreateEntry("OutfitNameToLoad", "DefaultName", "Outfit to Load from File", "File Name To Load");
            
            
            //Randomizer Category settings
            RandomizerCategory = MelonPreferences.CreateCategory("Randomizer", "Randomizer");
            RandomizerCategory.SetFilePath(Path.Combine(USER_DATA, CONFIG_FILE));
            
            PrefRandomizerIdentity = RandomizerCategory.CreateEntry("RandomizerIdentity", false, "Identity", "(Sets in Gym) Sets the Player's Identity to a Randomized Selection");
            PrefRandomizerArmorColors = RandomizerCategory.CreateEntry("RandomizerArmorColors", true, "Armor Colors", "(Sets in Gym) Sets the Player's Armor Colors to a Randomized Selection");
            PrefArmorItems = RandomizerCategory.CreateEntry("RandomizerArmorItems", false, "Armor Items", "(Sets in Gym) Sets the Player's Armor Items to a Randomized Selection");
            PrefRandomizerMarkings = RandomizerCategory.CreateEntry("RandomizerMarkings", false, "Markings", "(Sets in Gym) Sets the Player's Markings to a Randomized Selection");

            PrefSaveOutfit.ResetToDefault(); //Ignore saved setting to emulate ModUI DoNotSave tag;
            PrefUpdateFilesTexts.ResetToDefault(); //Ignore saved setting to emulate ModUI DoNotSave tag;
            StoreLastSavedPrefs();
		}

		internal static void StoreLastSavedPrefs()
		{
			List<MelonPreferences_Entry> prefs = new();
			prefs.AddRange(SketchbookPlusCategory.Entries);
			prefs.AddRange(SaveLoadCategory.Entries);
			prefs.AddRange(RandomizerCategory.Entries);

			foreach (MelonPreferences_Entry entry in  prefs) { LastSavedValues[entry] = entry.BoxedValue; }
		}

		public static bool AnyPrefsChanged()
		{
			foreach (KeyValuePair<MelonPreferences_Entry, object> pair in LastSavedValues)
			{
				if (!pair.Key.BoxedValue.Equals(pair.Value)) { return true; }
			}
			return false;
		}

		public static bool IsPrefChanged(MelonPreferences_Entry entry)
		{
			if (LastSavedValues.TryGetValue(entry, out object? lastValue)) { return !entry.BoxedValue.Equals(lastValue); }
			return false;
		}
	}
}