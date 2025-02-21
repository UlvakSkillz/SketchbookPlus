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
        public const string ModVersion = "1.0.1";
        public const string Author = "UlvakSkillz";
    }
    public class main : MelonMod
    {
        public static Mod SketchbookPlus = new Mod();
        private string currentScene = "Loader";
        private bool randomOutfit = false;
        private System.Random random = new System.Random();
        private List<string> acceptedFiles = new List<string>();
        private List<string> failedFiles = new List<string>();
        private List<string> pendingFiles = new List<string>();
        private List<string> acceptedFilesTexts = new List<string>();
        private List<string> pendingFilesTexts = new List<string>();
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
            SketchbookPlus.ModName = "SketchbookPlus";
            SketchbookPlus.ModVersion = BuildInfo.ModVersion;
            SketchbookPlus.SetFolder("SketchbookPlus");
            SketchbookPlus.AddToList("Save Outfit", false, 1, "Set to True and Save to Store the Equipped Outfit", new Tags { DoNotSave = true });
            SketchbookPlus.AddToList("Saved Outfit Name", "DefaultName", "File Name To Save Under", new Tags { });
            SketchbookPlus.AddToList("Load Outfit", false, 1, "(Not Usable in Pit or Ring) Set to True and Save to Load the Selected Outfit", new Tags { });
            SketchbookPlus.AddToList("Outfit Name to Load", "DefaultName", "Outfit to Load", new Tags { });
            SketchbookPlus.AddToList("Random Saved Outfit", false, 0, "Selects a Random Outfit from Saved Outfit Files", new Tags { });
            SketchbookPlus.AddToList("Update Files Texts", false, 0, "Set to True to refresh the Mod's Data from Files", new Tags { DoNotSave = true });
            SketchbookPlus.GetFromFile();
            UI.instance.UI_Initialized += UIInit;
            SketchbookPlus.ModSaved += Save;
            Calls.onMapInitialized += mapInit;
            pendingFiles.Clear();
            acceptedFiles.Clear();
            failedFiles.Clear();
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
            randomOutfit = (bool)SketchbookPlus.Settings[4].SavedValue;
            SketchbookPlus.Settings[0].Value = false;
            SketchbookPlus.Settings[0].SavedValue = false;
            if ((bool)SketchbookPlus.Settings[5].SavedValue)
            {
                debugLog("Clearing Known Files");
                SketchbookPlus.Settings[5].Value = false;
                SketchbookPlus.Settings[5].SavedValue = false;
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
            if (doSaveOutfit)
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
                if ((file == @"UserData\SketchbookPlus\Settings.txt") || failedFiles.Contains(file) || acceptedFiles.Contains(file))
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

        private void saveFile(string textToSave, string file)
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

        private bool checkItems(string tempPVDEconomyDataString)
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
                            Log("Item Not Redeemed");
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
            if (currentScene == "Gym")
            {
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
