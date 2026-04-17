using Il2CppRUMBLE.Managers;
using Il2CppRUMBLE.Players;
using MelonLoader;
using RumbleModdingAPI.RMAPI;
using System.Collections;
using UnityEngine;
using UIFramework;

namespace SketchbookPlus
{
    public static class BuildInfo
    {
        public const string ModName = "SketchbookPlus";
        public const string ModVersion = "1.3.1";
        public const string Author = "UlvakSkillz";
    }

    public class main : MelonMod
    {
        private System.Random random = new System.Random(DateTime.Now.Millisecond * DateTime.Now.Second * DateTime.Now.Minute);
        private List<string> acceptedFiles = new List<string>();
        private List<string> failedFiles = new List<string>();
        private List<string> pendingFiles = new List<string>();
        private List<string> acceptedFilesTexts = new List<string>();
        private List<string> pendingFilesTexts = new List<string>();
        private List<List<int>> acceptedItems = new List<List<int>>();
        private bool randomizerInit = false;
        private int lastOutfit = -1;

        private void Log(string msg)
        {
            MelonLogger.Msg(msg);
        }

        private void debugLog(string msg)
        {
            if (Preferences.PrefDebugging.Value)
            {
                MelonLogger.Msg("-" + msg);
            }
        }

        public override void OnLateInitializeMelon()
        {
            if (!Directory.Exists(@"UserData\SketchbookPlus"))
            {
                Directory.CreateDirectory(@"UserData\SketchbookPlus");
            }
            if (!Directory.Exists(@"UserData\SketchbookPlus\Stored"))
            {
                Directory.CreateDirectory(@"UserData\SketchbookPlus\Stored");
            }
            if (!File.Exists(@"UserData\SketchbookPlus\BlackListedItems.txt"))
            {
                saveFile("", @"UserData\SketchbookPlus\BlackListedItems.txt", false);
            }
            Actions.onMapInitialized += mapInit;
            Actions.onPlayerSpawned += playerJoined;
            pendingFiles.Clear();
            acceptedFiles.Clear();
            failedFiles.Clear();
        }

        public override void OnInitializeMelon()
        {
            Preferences.InitPrefs();
            UI.Register((MelonBase)this, Preferences.SketchbookPlusCategory, Preferences.SaveLoadCategory, Preferences.RandomizerCategory).OnModSaved += Save;
        }

        private void Save()
        {
            debugLog("Main Method Running");
            PlayerVisualData pvd = PlayerManager.instance.localPlayer.Data.VisualData;
            PlayerVisualData tempPVD = new PlayerVisualData(pvd);
            int[] outfit = new int[acceptedItems.Count];
            //if Reseting Files was true
            if (Preferences.PrefUpdateFilesTexts.Value)
            {
                debugLog("Clearing Known Files");
                //turn Reset Files button to false
                Preferences.PrefUpdateFilesTexts.ResetToDefault();
                pendingFiles.Clear();
                acceptedFiles.Clear();
                failedFiles.Clear();
            }
            debugLog("Rechecking Files");
            recheckFiles();
            debugLog("Done Rechecking Files");
            if (!Preferences.PrefSavedOutfitName.Value.ToLower().EndsWith(".txt"))
            {
                Preferences.PrefSavedOutfitName.Value += ".txt";
            }
            if (!Preferences.PrefOutfitNameToLoad.Value.ToLower().EndsWith(".txt"))
            {
                Preferences.PrefOutfitNameToLoad.Value += ".txt";
            }
            if (Preferences.PrefOutfitTypeToLoad.Value is OutfitTypes.RandomizerOutfit)
            {
                debugLog("Starting Randomizer");
                string[] originalOutfit = getOutfitString(pvd).Split(",");
                debugLog("Got Original Outfit");
                int[] identitySpots = new int[] { 0, 1, 2, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 58, 59, 60, 61, 62, 63, 64 };
                int[] armorColorSpots = new int[] { 3, 4, 5, 6, 7, 8 };
                int[] markingsSpots = new int[] { 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57 };
                int[] armorItemsSpots = new int[] { 19, 20, 21, 22, 23, 24, 25, 26, 27 };
                string[] newOutfit = new string[identitySpots.Length + armorColorSpots.Length + markingsSpots.Length + armorItemsSpots.Length];
                debugLog("Set PVD Variable Spots");
                for (int i = 0; i < originalOutfit.Length; i++)
                {
                    newOutfit[i] = originalOutfit[i];
                }
                debugLog("Updated New PVD Variables");
                for (int x = 0; x < identitySpots.Length + armorColorSpots.Length + markingsSpots.Length + armorItemsSpots.Length; x++)
                {
                    if ((Preferences.PrefRandomizerIdentity.Value && identitySpots.Contains(x)) || (Preferences.PrefRandomizerArmorColors.Value && armorColorSpots.Contains(x))
                        || (Preferences.PrefArmorItems.Value && armorItemsSpots.Contains(x)) || (Preferences.PrefRandomizerMarkings.Value && markingsSpots.Contains(x)))
                    {
                        newOutfit[x] = acceptedItems[x][random.Next(0, acceptedItems[x].Count)].ToString();
                    }
                }
                debugLog("Outfit Ready to Set");
                setPVD(pvd, newOutfit);
                Log("Randomizer Outfit Set: " + getOutfitString(pvd));
            }
            else if (Preferences.PrefSaveOutfit.Value)
            {
                //reset Save Button to False
                Preferences.PrefSaveOutfit.ResetToDefault();
                debugLog(@"Saving Outfit: UserData\SketchbookPlus\" + Preferences.PrefSavedOutfitName.Value);
                string textToSave = getOutfitString(PlayerManager.instance.localPlayer.Data.VisualData);
                debugLog("File Text: " + textToSave);
                saveFile(textToSave, @"UserData\SketchbookPlus\" + Preferences.PrefSavedOutfitName.Value);
            }
            else if (Calls.Scene.GetSceneName() == "Gym")
            {
                if ((Preferences.PrefOutfitTypeToLoad.Value is OutfitTypes.RandomFromFilesOutfit) && (acceptedFiles.Count > 0))
                {
                    int thisOutfit = random.Next(0, acceptedFiles.Count);
                    while ((thisOutfit == lastOutfit) && (acceptedFiles.Count > 1))
                    {
                        thisOutfit = random.Next(0, acceptedFiles.Count);
                    }
                    debugLog("Loading Random: " + acceptedFiles[thisOutfit]);
                    loadOutfit(acceptedFiles[thisOutfit]);
                    lastOutfit = thisOutfit;
                }
                else if (Preferences.PrefOutfitTypeToLoad.Value is OutfitTypes.SavedOutfit)
                {
                    debugLog("Loading Selected: " + Preferences.PrefOutfitNameToLoad.Value);
                    loadOutfit(@"UserData\SketchbookPlus\" + Preferences.PrefOutfitNameToLoad.Value);
                }
            }
        }

        private void recheckFiles()
        {
            string[] files = Directory.GetFiles(@"UserData\SketchbookPlus");
            foreach (string file in files)
            {
                if ((file == @"UserData\SketchbookPlus\config.cfg") || (file == @"UserData\SketchbookPlus\BlackListedItems.txt") || failedFiles.Contains(file) || acceptedFiles.Contains(file))
                {
                    continue;
                }
                else
                {
                    debugLog("Pending File: " + file);
                    pendingFiles.Add(file);
                }
            }
            loadFileTexts();
            while (pendingFiles.Count > 0)
            {
                if (!CheckFileValidity(pendingFiles[0]))
                {
                    Log("Removing Bad File: " + pendingFiles[0]);
                    Log("Removing Bad File's Text: " + pendingFilesTexts[0]);
                    failedFiles.Add(pendingFiles[0]);
                    pendingFiles.Remove(pendingFiles[0]);
                    pendingFilesTexts.RemoveAt(0);
                }
                else
                {
                    debugLog("Accepting file: " + pendingFiles[0]);
                    acceptedFiles.Add(pendingFiles[0]);
                    acceptedFilesTexts.Add(pendingFilesTexts[0]);
                    pendingFiles.Remove(pendingFiles[0]);
                    pendingFilesTexts.RemoveAt(0);
                }
            }
        }

        private void saveFile(string textToSave, string file, bool addAcceptedFile = true)
        {
            if (!File.Exists(file))
            {
                acceptedFiles.Add(file);
            }
            FileStream fs = File.Create(file);
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(textToSave);
            fs.Write(bytes);
            fs.Close();
            fs.Dispose();
            Log("Saving " + file + " Complete");
        }

        private void playerJoined(Player player)
        {
            if (Preferences.PrefStoreOthersOutfits.Value && (player.Controller.controllerType == ControllerType.Remote))
            {
                MelonCoroutines.Start(checkPlayer(player));
            }
        }

        private IEnumerator checkPlayer(Player player)
        {
            debugLog("Storing Player");
            yield return new WaitForSeconds(1);
            string visuals = getOutfitString(player.Data.VisualData);
            string name = player.Data.GeneralData.PublicUsername;
            if (name.Contains("<") || name.Contains(">") || name.Contains(":"))
            {
                name = name.Replace("<", "_");
                name = name.Replace(">", "_");
                name = name.Replace(":", "_");
            }
            if (!File.Exists(@"UserData\SketchbookPlus\Stored\" + name + ".txt"))
            {
                debugLog($"Saving {name}'s Model");
            }
            else
            {
                debugLog($"Found Existing {name}'s Model, Overriding Text");
            }
            saveFile(visuals, @"UserData\SketchbookPlus\Stored\" + name + ".txt");
            yield break;
        }

        private void setupRandomizer()
        {
            debugLog("Setting Up Randomizer");
            int maxSlotsToCheck = 50;
            PlayerVisualData pvd = PlayerManager.instance.localPlayer.Data.VisualData;
            PlayerVisualData tempPVD = new PlayerVisualData(pvd);
            setPVD(tempPVD, pvd);
            string originalOutfit = getOutfitString(pvd);
            string[] originalOutfitSplit = originalOutfit.Split(",");
            int[] currentOutfit = new int[originalOutfitSplit.Length];
            //resets current outfit
            for(int i = 0; i < originalOutfitSplit.Length; i++)
            {
                currentOutfit[i] = int.Parse(originalOutfitSplit[i]);
            }
            //for every cosmetic option
            for(int i = 0; i < originalOutfitSplit.Length; i++)
            {
                List<int> acceptedList = new List<int>();
                string debugMsg = "Spot " + i + ": ";
                //for every max for each cosmetic
                for (int x = 0; x <= maxSlotsToCheck; x++)
                {
                    //set parsed number to try
                    currentOutfit[i] = x;
                    //sets up outfitString from int[] to string[]
                    string[] outfitString = new string[currentOutfit.Length];
                    for (int j = 0; j < outfitString.Length; j++)
                    {
                        outfitString[j] = currentOutfit[j].ToString();
                    }
                    //sets tempPvd with the string to check
                    setPVD(tempPVD, outfitString);
                    //if accepted item
                    if (checkItems(tempPVD.ToPlayfabDataString(), true))
                    {
                        //add to list
                        acceptedList.Add(x);
                        debugMsg += x + ", ";
                    }
                }
                acceptedItems.Add(acceptedList);
                debugLog(debugMsg);
                //resets outfit
                for (int x = 0; x < originalOutfitSplit.Length; x++)
                {
                    currentOutfit[x] = int.Parse(originalOutfitSplit[x]);
                }
            }
            setupBlackList();
            debugLog("Randomizer Setup");
            randomizerInit = true;
        }

        private void setupBlackList()
        {
            string[] fileText = File.ReadAllLines(@"UserData\SketchbookPlus\BlackListedItems.txt");
            try
            {
                foreach (string line in fileText)
                {
                    string[] item = line.Split(",");
                    if (acceptedItems[int.Parse(item[0])].Remove(int.Parse(item[1])))
                    {
                        Log("Removed Item: " + line);
                    }
                }
            }
            catch
            {
                MelonLogger.Error("Error Reading BlackList File");
            }
        }

        private string getOutfitString(PlayerVisualData pvd)
        {
            string outfitString = "";
            for (int i = 0; i < pvd.ColorationIndexes.Count; i++)
            {
                outfitString += pvd.ColorationIndexes[i] + ",";
            }
            for (int i = 0; i < pvd.CustomizationPartIndexes.Count; i++)
            {
                outfitString += pvd.CustomizationPartIndexes[i] + ",";
            }
            for (int i = 0; i < pvd.OtherCustomizationIndexes.Count; i++)
            {
                outfitString += pvd.OtherCustomizationIndexes[i] + ",";
            }
            for (int i = 0; i < pvd.TextureCustomizationIndexes.Count; i++)
            {
                outfitString += pvd.TextureCustomizationIndexes[i] + ",";
            }
            for (int i = 0; i < pvd.TextureOpacityIndexes.Count; i++)
            {
                outfitString += pvd.TextureOpacityIndexes[i] + ",";
            }
            for (int i = 0; i < pvd.WeightAdjustementIndexes.Count; i++)
            {
                outfitString += pvd.WeightAdjustementIndexes[i];
                if (i != pvd.WeightAdjustementIndexes.Count - 1)
                {
                    outfitString += ",";
                }
            }
            return outfitString;
        }

        private void loadOutfit(string fileName)
        {
            if (acceptedFiles.IndexOf(fileName) == -1)
            {
                MelonLogger.Error("Not An Accepted File: " + fileName);
                return;
            }
            Log("Text: " + acceptedFilesTexts[acceptedFiles.IndexOf(fileName)]);
            string[] data = acceptedFilesTexts[acceptedFiles.IndexOf(fileName)].Split(',');
            PlayerVisualData pvd = PlayerManager.instance.localPlayer.Data.VisualData;
            PlayerVisualData tempPVD = new PlayerVisualData(pvd);
            setPVD(tempPVD, data);
            if (checkItems(tempPVD.ToPlayfabDataString()))
            {
                debugLog("Setting PVD Now");
                setPVD(pvd, tempPVD);
                Log("Loaded Outfit for Next Scene: " + fileName);
            }
        }

        private bool CheckFileValidity(string fileName)
        {
            string[] data = pendingFilesTexts[pendingFiles.IndexOf(fileName)].Split(',');
            PlayerVisualData tempPVD = new PlayerVisualData(PlayerManager.instance.localPlayer.Data.VisualData);
            try
            {
                setPVD(tempPVD, data);
            }
            catch
            {
                Log("Invalid File Text");
                return false;
            }
            return checkItems(tempPVD.ToPlayfabDataString());
        }

        private void setPVD(PlayerVisualData pvd, string[] data)
        {
            int j = 0;
            for (int i = 0; i < pvd.ColorationIndexes.Count; i++)
            {
                pvd.ColorationIndexes[i] = short.Parse(data[j]);
                j++;
            }
            for (int i = 0; i < pvd.CustomizationPartIndexes.Count; i++)
            {
                pvd.CustomizationPartIndexes[i] = short.Parse(data[j]);
                j++;
            }
            for (int i = 0; i < pvd.OtherCustomizationIndexes.Count; i++)
            {
                pvd.OtherCustomizationIndexes[i] = short.Parse(data[j]);
                j++;
            }
            for (int i = 0; i < pvd.TextureCustomizationIndexes.Count; i++)
            {
                pvd.TextureCustomizationIndexes[i] = short.Parse(data[j]);
                j++;
            }
            for (int i = 0; i < pvd.TextureOpacityIndexes.Count; i++)
            {
                pvd.TextureOpacityIndexes[i] = short.Parse(data[j]);
                j++;
            }
            for (int i = 0; i < pvd.WeightAdjustementIndexes.Count; i++)
            {
                pvd.WeightAdjustementIndexes[i] = short.Parse(data[j]);
                j++;
            }
        }

        private void setPVD(PlayerVisualData pvdTo, PlayerVisualData pvdFrom)
        {
            for (int i = 0; i < pvdTo.ColorationIndexes.Count; i++)
            {
                pvdTo.ColorationIndexes[i] = pvdFrom.ColorationIndexes[i];
            }
            for (int i = 0; i < pvdTo.CustomizationPartIndexes.Count; i++)
            {
                pvdTo.CustomizationPartIndexes[i] = pvdFrom.CustomizationPartIndexes[i];
            }
            for (int i = 0; i < pvdTo.OtherCustomizationIndexes.Count; i++)
            {
                pvdTo.OtherCustomizationIndexes[i] = pvdFrom.OtherCustomizationIndexes[i];
            }
            for (int i = 0; i < pvdTo.TextureCustomizationIndexes.Count; i++)
            {
                pvdTo.TextureCustomizationIndexes[i] = pvdFrom.TextureCustomizationIndexes[i];
            }
            for (int i = 0; i < pvdTo.TextureOpacityIndexes.Count; i++)
            {
                pvdTo.TextureOpacityIndexes[i] = pvdFrom.TextureOpacityIndexes[i];
            }
            for (int i = 0; i < pvdTo.WeightAdjustementIndexes.Count; i++)
            {
                pvdTo.WeightAdjustementIndexes[i] = pvdFrom.WeightAdjustementIndexes[i];
            }
        }

        private bool checkItems(string tempPVDEconomyDataString, bool isSilent = false)
        {
            string[] tempPVDEconomyData = tempPVDEconomyDataString.Split(':');
            bool goodList = true;
            for (int i = 0; i < tempPVDEconomyData.Length; i++)
            {
                if ((i != 0) && (i != 5))
                {
                    string[] individualEconomyData = tempPVDEconomyData[i].Split(",");
                    foreach (string economyItem in individualEconomyData)
                    {
                        if (!PlayerManager.instance.localPlayer.Data.EconomyData.ReceivedItems.Contains(economyItem))
                        {
                            goodList = false;
                            if (!isSilent)
                            {
                                Log("Item Not Redeemed");
                            }
                            break;
                        }
                    }
                    if (!goodList)
                    {
                        break;
                    }
                }
            }
            if (goodList)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void mapInit(string map)
        {
            debugLog("RMAPI triggered MapInit");
            if (map == "Gym")
            {
                debugLog("Map is Gym");
                if (!randomizerInit)
                {
                    debugLog("Setting up Randomizer");
                    setupRandomizer();
                }
                Save();
            }
        }

        private void loadFileTexts()
        {
            foreach (string pendingFile in pendingFiles)
            {
                string fileText = File.ReadAllText(pendingFile);
                pendingFilesTexts.Add(fileText);
            }
        }
    }
}
