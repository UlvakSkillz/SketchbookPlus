using Il2CppRUMBLE.Managers;
using Il2CppRUMBLE.Players;
using MelonLoader;
using RumbleModdingAPI;
using RumbleModUI;
using System.Collections;
using UnityEngine;

namespace SketchbookPlus
{
    public static class BuildInfo
    {
        public const string ModName = "SketchbookPlus";
        public const string ModVersion = "1.1.1";
        public const string Author = "UlvakSkillz";
    }
    public class main : MelonMod
    {
        public static Mod SketchbookPlus = new Mod();
        private string currentScene = "Loader";
        private System.Random random = new System.Random(DateTime.Now.Millisecond * DateTime.Now.Second * DateTime.Now.Minute);
        private List<string> acceptedFiles = new List<string>();
        private List<string> failedFiles = new List<string>();
        private List<string> pendingFiles = new List<string>();
        private List<string> acceptedFilesTexts = new List<string>();
        private List<string> pendingFilesTexts = new List<string>();
        private List<List<int>> acceptedItems = new List<List<int>>();
        private bool randomizerInit = false;
        private int lastOutfit = -1;
        private bool debug = false;

        private void Log(string msg)
        {
            MelonLogger.Msg(msg);
        }

        private void debugLog(string msg)
        {
            if (debug)
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
            if (!File.Exists(@"UserData\SketchbookPlus\BlackListedItems.txt"))
            {
                saveFile("", @"UserData\SketchbookPlus\BlackListedItems.txt", false);
            }
            SketchbookPlus.ModName = "SketchbookPlus";
            SketchbookPlus.ModVersion = BuildInfo.ModVersion;
            SketchbookPlus.SetFolder("SketchbookPlus");
            SketchbookPlus.AddToList("Save Outfit", false, 0, "Set to True and Save to Store the Equipped Outfit", new Tags { DoNotSave = true });
            SketchbookPlus.AddToList("Saved Outfit Name", "DefaultName", "File Name To Save Under", new Tags { });
            SketchbookPlus.AddToList("Load Outfit", false, 1, "(Sets in Gym) Set to True and Save to Load the Selected Outfit", new Tags { });
            SketchbookPlus.AddToList("Outfit Name to Load", "DefaultName", "Outfit to Load from File", new Tags { });
            SketchbookPlus.AddToList("Random Outfit from Files", false, 1, "(Sets in Gym) Selects a Random Outfit from Saved Outfit Files", new Tags { });
            SketchbookPlus.AddToList("Randomizer: Identity", false, 0, "On/Off Toggle for Randomizing Identity", new Tags { });
            SketchbookPlus.AddToList("Randomizer: Armor Colors", true, 0, "On/Off Toggle for Randomizing Armor Colors", new Tags { });
            SketchbookPlus.AddToList("Randomizer: Armor Items", false, 0, "On/Off Toggle for Randomizing Armor Items", new Tags { });
            SketchbookPlus.AddToList("Randomizer: Markings", false, 0, "On/Off Toggle for Randomizing Markings", new Tags { });
            SketchbookPlus.AddToList("Set Random Outfit", false, 1, "(Sets in Gym) Sets the Player to a Randomized Outfit from the Randomizer Selections", new Tags {});
            SketchbookPlus.AddToList("Update Files Texts", false, 0, "Set to True to refresh the Mod's Data from Files", new Tags { DoNotSave = true });
            SketchbookPlus.GetFromFile();
            UI.instance.UI_Initialized += UIInit;
            SketchbookPlus.ModSaved += Save;
            Calls.onMapInitialized += mapInit;
            pendingFiles.Clear();
            acceptedFiles.Clear();
            failedFiles.Clear();
            Calls.onPlayerSpawned += playerJoined; //REMOVE
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            currentScene = sceneName;
            debugLog("Loading Scene: " + currentScene);
        }

        private void UIInit()
        {
            UI.instance.AddMod(SketchbookPlus);
        }

        private void Save()
        {
            debugLog("Main Method Running");
            bool doSaveOutfit = (bool)SketchbookPlus.Settings[0].SavedValue;
            string saveOutfitName = (string)SketchbookPlus.Settings[1].SavedValue;
            bool doLoadOutfit = (bool)SketchbookPlus.Settings[2].SavedValue;
            string loadOutfitName = (string)SketchbookPlus.Settings[3].SavedValue;
            bool randomOutfit = (bool)SketchbookPlus.Settings[4].SavedValue;
            bool randomizeIdentity = (bool)SketchbookPlus.Settings[5].SavedValue;
            bool randomizeArmorColor = (bool)SketchbookPlus.Settings[6].SavedValue;
            bool randomizeArmorItems = (bool)SketchbookPlus.Settings[7].SavedValue;
            bool randomizeMarkings = (bool)SketchbookPlus.Settings[8].SavedValue;
            bool selectRandomOutfit = (bool)SketchbookPlus.Settings[9].SavedValue;
            PlayerVisualData pvd = PlayerManager.instance.localPlayer.Data.VisualData;
            PlayerVisualData tempPVD = new PlayerVisualData(pvd);
            int[] outfit = new int[acceptedItems.Count];
            //reset Save Button to False
            SketchbookPlus.Settings[0].Value = false;
            SketchbookPlus.Settings[0].SavedValue = false;
            //if Reseting Files was true
            if ((bool)SketchbookPlus.Settings[10].SavedValue)
            {
                debugLog("Clearing Known Files");
                //turn Reset Files button to false
                SketchbookPlus.Settings[10].Value = false;
                SketchbookPlus.Settings[10].SavedValue = false;
                pendingFiles.Clear();
                acceptedFiles.Clear();
                failedFiles.Clear();
            }
            debugLog("Rechecking Files");
            recheckFiles();
            debugLog("Done Rechecking Files");
            if (!saveOutfitName.ToLower().EndsWith(".txt"))
            {
                saveOutfitName += ".txt";
            }
            if (!loadOutfitName.ToLower().EndsWith(".txt"))
            {
                loadOutfitName += ".txt";
            }
            if (selectRandomOutfit)
            {
                debugLog("Starting Randomizer");
                string[] originalOutfit = getOutfitString(pvd).Split(",");
                debugLog("Got Original Outfit");
                string[] newOutfit = new string[originalOutfit.Length];
                int[] identitySpots = new int[] { 0, 1, 2, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 58, 59, 60, 61, 62, 63, 64 };
                int[] armorColorSpots = new int[] { 3, 4, 5, 6, 7, 8 };
                int[] markingsSpots = new int[] { 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57 };
                int[] armorItemsSpots = new int[] { 19, 20, 21, 22, 23, 24, 25, 26, 27 };
                for (int i = 0; i < originalOutfit.Length; i++)
                {
                    newOutfit[i] = originalOutfit[i];
                }
                for (int x = 0; x < identitySpots.Length + armorColorSpots.Length + markingsSpots.Length + armorItemsSpots.Length; x++)
                {
                    if ((randomizeIdentity && identitySpots.Contains(x)) || (randomizeArmorColor && armorColorSpots.Contains(x))
                        || (randomizeArmorItems && armorItemsSpots.Contains(x)) || (randomizeMarkings && markingsSpots.Contains(x)))
                    {
                        newOutfit[x] = acceptedItems[x][random.Next(0, acceptedItems[x].Count)].ToString();
                    }
                }
                setPVD(pvd, newOutfit);
                Log("Randomizer Outfit Set: " + getOutfitString(pvd));
            }
            else if (doSaveOutfit)
            {
                debugLog(@"Saving Outfit: UserData\SketchbookPlus\" + saveOutfitName);
                string textToSave = getOutfitString(PlayerManager.instance.localPlayer.Data.visualData);
                debugLog("File Text: " + textToSave);
                saveFile(textToSave, @"UserData\SketchbookPlus\" + saveOutfitName);
            }
            else if (currentScene == "Gym")
            {
                if (randomOutfit && (acceptedFiles.Count > 0))
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
                else if (doLoadOutfit)
                {
                    debugLog("Loading Selected: " + loadOutfitName);
                    loadOutfit(@"UserData\SketchbookPlus\" + loadOutfitName);
                }
            }
        }

        private void recheckFiles()
        {
            string[] files = Directory.GetFiles(@"UserData\SketchbookPlus");
            foreach (string file in files)
            {
                if ((file == @"UserData\SketchbookPlus\Settings.txt") || (file == @"UserData\SketchbookPlus\BlackListedItems.txt") || failedFiles.Contains(file) || acceptedFiles.Contains(file))
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
        
        // /*
        private bool checkingPlayers = false;//REMOVE

        private void playerJoined()//REMOVE
        {
            if (!checkingPlayers)
            {
                MelonCoroutines.Start(checkPlayers());
            }
        }

        private IEnumerator checkPlayers()//REMOVE
        {
            checkingPlayers = true;
            debugLog("Checking Players");
            yield return new WaitForSeconds(2);
            if (PlayerManager.instance.AllPlayers.Count > 1)
            {
                foreach (Player player in Calls.Players.GetEnemyPlayers())
                {
                    string visuals = getOutfitString(player.Data.visualData);
                    string name = player.Data.GeneralData.PublicUsername;
                    if (name.Contains("<") || name.Contains(">") || name.Contains(":"))
                    {
                        name = name.Replace("<", "_");
                        name = name.Replace(">", "_");
                        name = name.Replace(":", "_");
                    }
                    if (!File.Exists(@"UserData\SketchbookPlus\Stolen\" + name + ".txt"))
                    {
                        debugLog($"Saving {name}'s Model");
                    }
                    else
                    {
                        debugLog($"Found Existing {name}'s Model, Overriding Text");
                    }
                    saveFile(visuals, @"UserData\SketchbookPlus\Stolen\" + name + ".txt");
                }
            }
            else
            {
                debugLog("No Others Found");
            }
            checkingPlayers = false;
            yield break;
        }

        private void loadOutfit(string[] fileText)//REMOVE
        {
            PlayerVisualData pvd = PlayerManager.instance.localPlayer.Data.VisualData;
            PlayerVisualData tempPVD = new PlayerVisualData(pvd);
            setPVD(tempPVD, pvd);
            setPVD(tempPVD, fileText);
            if (!checkItems(tempPVD.ToPlayfabDataString()))
            {
                debugLog("Setting Illegal Outfit");
            }
            setPVD(pvd, tempPVD);
        }
        // */

        private void setupRandomizer()
        {
            debugLog("Setting Up Randomizer");
            PlayerVisualData pvd = PlayerManager.instance.localPlayer.Data.visualData;
            PlayerVisualData tempPVD = new PlayerVisualData(pvd);
            setPVD(tempPVD, pvd);
            string originalOutfit = getOutfitString(pvd);
            string[] originalOutfitSplit = originalOutfit.Split(",");
            int[] maxes = new int[] { 21, 23, 19, 23, 13, 17, 23, 15, 23, 35, 35, 35, 35, 35, 35, 35, 35, 35, 35, 3, 8, 2, 2, 4, 4, 4, 3, 3, 7, 5, 6, 3, 3, 3, 3, 6, 78, 0, 29, 29, 29, 29, 29, 24, 24, 24, 24, 24, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 5, 5, 5, 3, 4, 3, 4 };
            int[] currentOutfit = new int[maxes.Length];
            //resets current outfit
            for(int i = 0; i < maxes.Length; i++)
            {
                currentOutfit[i] = int.Parse(originalOutfitSplit[i]);
            }
            //for every cosmetic option
            for(int i = 0; i < maxes.Length; i++)
            {
                List<int> acceptedList = new List<int>();
                string debugMsg = "Spot " + i + ": ";
                //for every max for each cosmetic
                for (int x = 0; x <= maxes[i]; x++)
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
                for (int x = 0; x < maxes.Length; x++)
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

        private void mapInit()
        {
            MelonCoroutines.Start(Wait());
        }

        private IEnumerator Wait()
        {
            if (Calls.Mods.findOwnMod("CloneBending", "1.0.0", false))
            {
                yield return new WaitForSeconds(11f);
            }
            if (currentScene == "Gym")
            {
                if (!randomizerInit)
                {
                    setupRandomizer();
                }
                Save();
            }
            yield break;
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
