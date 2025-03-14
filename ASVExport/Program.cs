﻿using ASVPack.Models;
using Newtonsoft.Json.Linq;
using System;
using static System.Net.Mime.MediaTypeNames;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using SavegameToolkit;
using System.Runtime.CompilerServices;

namespace ASVExport
{
    class Program
    {
        private static string logFile = Path.Combine(AppContext.BaseDirectory, "ASV_Error.log");

        private static void LogError(string message, Exception ex = null)
        {
            try
            {
                string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                if (ex != null)
                {
                    logMessage += $"\nException: {ex.Message}";
                    logMessage += $"\nStack Trace: {ex.StackTrace}";
                    if (ex.InnerException != null)
                    {
                        logMessage += $"\nInner Exception: {ex.InnerException.Message}";
                    }
                }
                File.AppendAllText(logFile, logMessage + "\n\n");
                Console.WriteLine(logMessage);
            }
            catch { /* Silently fail if logging fails */ }
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            try
            {
                //support quoted command line arguments which doesn't seem to be supported with Environment.GetCommandLineArgs() 
                string[] commandArguments = Environment.CommandLine.Trim().Split('"')
                                    .Select((element, index) => index % 2 == 0  // If even index
                                                          ? element.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)  // Split the item
                                                          : new string[] { element })  // Keep the entire item
                                    .SelectMany(element => element).ToArray();

                commandArguments = commandArguments.Where(a => string.IsNullOrEmpty(a) == false).ToArray();

                if (commandArguments != null && commandArguments.Length > 1)
                {
                    ExportWithCommandLineOptions(commandArguments);
                }
                else
                {
                    LogError("No command line arguments provided");
                    Environment.ExitCode = -1;
                }
            }
            catch (Exception ex)
            {
                LogError("Fatal error in Main", ex);
                Environment.ExitCode = -1;
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;
            LogError("Unhandled Exception", ex);
            Environment.ExitCode = -1;
        }

        private static void ExportWithCommandLineOptions(string[] commandArguments)
        {
            try
            {
                string logFilename = Path.Combine(AppContext.BaseDirectory, @"ASV.log");
                string commandOptionCheck = commandArguments[1].ToString().Trim().ToLower();
                string exportFilePath = Path.Combine(AppContext.BaseDirectory, @"Export\");
                string exportFilename = Path.Combine(exportFilePath, "");

                LogError($"ASV Command Line Started with {commandArguments.Length} parameters.");

                int argIndex = 0;
                foreach (string arg in commandArguments)
                {
                    LogError($"CommandLineArg-{argIndex} = {arg}");
                    argIndex++;
                }

                string inputFilename = "";
                if (commandArguments.Length > 2)
                {
                    inputFilename = commandArguments[2].ToString().Trim().Replace("\"", "");
                    LogError($"Input filename: {inputFilename}");
                }

                if (commandArguments.Length > 3)
                {
                    exportFilename = commandArguments[3].ToString().Trim().Replace("\"", "");
                    exportFilePath = Path.GetDirectoryName(exportFilename);
                    LogError($"Export filename: {exportFilename}");
                }

                string clusterFolder = "";
                if(commandArguments.Length > 4)
                {
                    clusterFolder = commandArguments[3].ToString().Trim().Replace("\"", "");
                    exportFilename = commandArguments[4].ToString().Trim().Replace("\"", "");
                    exportFilePath = Path.GetDirectoryName(exportFilename);
                    LogError($"Cluster folder: {clusterFolder}");
                }

                try
                {
                    switch (commandOptionCheck)
                    {
                        case "pack":
                            LogError($"Exporting ASV pack for configuration: {inputFilename}");
                            ExportCommandLinePack(inputFilename);
                            break;

                        case "arktribe":
                            LogError($"Exporting .arktribe data from: {inputFilename}");
                            ExportStoredTribes(inputFilename, exportFilePath);
                            break;

                        case "arkprofile":
                            LogError($"Exporting .arkprofile data from: {inputFilename}");
                            ExportStoredProfiles(inputFilename, exportFilePath);
                            break;

                        case "json":
                            LogError($"Exporting JSON for configuration: {inputFilename}");
                            ExportCommandLine(inputFilename);
                            break;

                        default:
                            if (!File.Exists(inputFilename))
                            {
                                LogError($"File Not Found: {inputFilename}");
                                Environment.ExitCode = -1;
                                break;
                            }

                            try
                            {
                                ContentContainer container = new ContentContainer();
                                LogError($"Loading save game: {inputFilename}");
                                container.LoadSaveGame(inputFilename, "", clusterFolder, 90);

                                ContentPack exportPack = new ContentPack(container, 0, 0, 50, 50, 100, true, true, true, true, true, true, true);

                                switch (commandOptionCheck)
                                {
                                    case "all":
                                        LogError($"Exporting JSON (all) for: {inputFilename}");
                                        exportPack.ExportJsonAll(exportFilePath);
                                        break;

                                    case "map":
                                        LogError($"Exporting JSON (map) for: {inputFilename}");
                                        exportPack.ExportJsonMapStructures(exportFilename);
                                        break;

                                    case "structures":
                                        LogError($"Exporting JSON (structures) for: {inputFilename}");
                                        exportPack.ExportJsonPlayerStructures(exportFilename);
                                        break;

                                    case "logs":
                                        LogError($"Exporting JSON (tribe logs) for: {inputFilename}");
                                        exportPack.ExportJsonPlayerTribeLogs(exportFilename);
                                        break;

                                    case "tribes":
                                        LogError($"Exporting JSON (tribes) for: {inputFilename}");
                                        exportPack.ExportJsonPlayerTribes(exportFilename);
                                        break;

                                    case "players":
                                        LogError($"Exporting JSON (players) for: {inputFilename}");
                                        exportPack.ExportJsonPlayers(exportFilename);
                                        break;

                                    case "wild":
                                        LogError($"Exporting JSON (wild) for: {inputFilename}");
                                        exportPack.ExportJsonWild(exportFilename);
                                        break;

                                    case "tamed":
                                        LogError($"Exporting JSON (tamed) for: {inputFilename}");
                                        exportPack.ExportJsonTamed(exportFilename);
                                        break;
                                }
                                exportPack = null;
                                LogError($"Completed export for: {inputFilename}");
                            }
                            catch (Exception ex)
                            {
                                LogError($"Error during export process for {inputFilename}", ex);
                                Environment.ExitCode = -1;
                            }
                            break;
                    }

                    Environment.ExitCode = 0;
                }
                catch (Exception ex)
                {
                    LogError($"Failed export for: {inputFilename}", ex);
                    Environment.ExitCode = -1;
                }
            }
            catch (Exception ex)
            {
                LogError("Error in ExportWithCommandLineOptions", ex);
                Environment.ExitCode = -1;
            }
        }

        private static void ExportCommandLinePack(string configFilename)
        {
            try
            {
                LogError($"Starting ExportCommandLinePack for config: {configFilename}");
                //defaults
                string mapFilename = "";
                string exportFilename = Path.Combine(AppContext.BaseDirectory, @"Export\ASV_ContentPack.asv");
                string clusterFolder = "";
                long tribeId = 0;
                long playerId = 0;
                decimal filterLat = 50;
                decimal filterLon = 50;
                decimal filterRad = 250;
                bool packStructureLocations = true;
                bool packStructureContent = true;
                bool packDroppedItems = true;
                bool packTribesPlayers = true;
                bool packTamed = true;
                bool packWild = true;
                bool packPlayerStructures = true;
                


                if (File.Exists(configFilename))
                {
                    //config found, load settings from file.
                    //LogWriter.Debug($"Reading pack export configuation.");
                    string packConfigText = File.ReadAllText(configFilename);
                    try
                    {
                        JObject packConfig = JObject.Parse(packConfigText);

                        mapFilename = packConfig.Property("mapFilename") == null ? "" : packConfig.Property("mapFilename").Value.ToString();
                        exportFilename = packConfig.Property("exportFilename") == null ? "" : packConfig.Property("exportFilename").Value.ToString();
                        tribeId = packConfig.Property("tribeId") == null ? 0 : (long)packConfig.Property("tribeId").Value;
                        playerId = packConfig.Property("playerId") == null ? 0 : (long)packConfig.Property("playerId").Value;
                        filterLat = packConfig.Property("filterLat") == null ? 50 : (decimal)packConfig.Property("filterLat").Value;
                        filterLon = packConfig.Property("filterLon") == null ? 50 : (decimal)packConfig.Property("filterLon").Value;
                        filterRad = packConfig.Property("filterRad") == null ? 250 : (decimal)packConfig.Property("filterRad").Value;
                        packStructureLocations = packConfig.Property("packStructureLocations") == null ? true : (bool)packConfig.Property("packStructureLocations").Value;
                        packStructureContent = packConfig.Property("packStructureContent") == null ? true : (bool)packConfig.Property("packStructureContent").Value;
                        packDroppedItems = packConfig.Property("packDroppedItems") == null ? true : (bool)packConfig.Property("packDroppedItems").Value;
                        packTribesPlayers = packConfig.Property("packTribesPlayers") == null ? true : (bool)packConfig.Property("packTribesPlayers").Value;
                        packTamed = packConfig.Property("packTamed") == null ? true : (bool)packConfig.Property("packTamed").Value;
                        packWild = packConfig.Property("packWild") == null ? true : (bool)packConfig.Property("packWild").Value;
                        packPlayerStructures = packConfig.Property("packPlayerStructures") == null ? true : (bool)packConfig.Property("packPlayerStructures").Value;
                        clusterFolder = packConfig.Property("clusterFolder") == null ? "" : packConfig.Property("clusterFolder").Value.ToString();

                    }
                    catch
                    {
                        //LogWriter.Debug($"Unable to parse pack export configuration.");

                        //bad file data, ignore
                    }
                }


                //ensure folder exists
                string exportFolder = Path.GetDirectoryName(exportFilename);
                if (exportFolder.Length == 0) exportFolder = Path.Combine(AppContext.BaseDirectory, @"Export\");
                if (!Directory.Exists(exportFolder)) Directory.CreateDirectory(exportFolder);

                //ensure filename set, and ends with .asv
                if (exportFilename.Length == 0) exportFilename = Path.Combine(exportFolder, "ASV_ContentPack.asv");
                if (!exportFilename.ToLower().EndsWith("asv")) exportFilename += ".asv";

                //create pack and export
                ContentContainer exportPack = new ContentContainer();

                //LogWriter.Debug($"Loading .ark save file: {mapFilename}");
                
                exportPack.LoadSaveGame(mapFilename, "", clusterFolder,90);

                //LogWriter.Debug($"Creating ContentPack");
                ContentPack pack = new ContentPack(exportPack, tribeId, playerId, filterLat, filterLon, filterRad, packStructureLocations, packStructureContent, packTribesPlayers, packTamed, packWild, packPlayerStructures, packDroppedItems);

                //LogWriter.Debug($"Exporting ContentPack");
                pack.ExportPack(exportFilename);

                

                //LogWriter.Trace("END ExportCommandLinePack()");
            }
            catch (Exception ex)
            {
                LogError($"Error in ExportCommandLinePack for config: {configFilename}", ex);
                Environment.ExitCode = -1;
            }
        }

        private static void ExportCommandLine(string configFilename)
        {
            try
            {
                LogError($"Starting ExportCommandLine for config: {configFilename}");
                //LogWriter.Trace("BEGIN ExportCommandLine()");

                long tribeId = 0;
                long playerId = 0;
                decimal filterLat = 50;
                decimal filterLon = 50;
                decimal filterRad = 250;

                string tribeExportFilename = Path.Combine(AppContext.BaseDirectory, @"Export\ASV_Export_Tribes.json");
                string tribeImageFilename = Path.Combine(AppContext.BaseDirectory, @"Export\ASV_Export_Tribes.png");
                bool tribeStructures = true;
                bool mapStructureContent = true;
                bool tribePlayers = true;
                bool tribeTames = true;

                string structureExportFilename = Path.Combine(AppContext.BaseDirectory, @"Export\ASV_Export_Structures.json");
                string structureImageFilename = Path.Combine(AppContext.BaseDirectory, @"Export\ASV_Export_Structures.png");
                string structureClassName = "";

                string mapStructureExportFilename = Path.Combine(AppContext.BaseDirectory, @"Export\ASV_Export_MapStructures.json");

                string playerExportFilename = Path.Combine(AppContext.BaseDirectory, @"Export\ASV_Export_Players.json");
                string playerImageFilename = Path.Combine(AppContext.BaseDirectory, @"Export\ASV_Export_Players.png");

                string wildExportFilename = Path.Combine(AppContext.BaseDirectory, @"Export\ASV_Export_Wild.json");
                string wildImageFilename = Path.Combine(AppContext.BaseDirectory, @"Export\ASV_Export_Wild.png");
                string wildClassName = "";
                int wildMinLevel = 0;
                int wildMaxLevel = 999;


                string tamedExportFilename = Path.Combine(AppContext.BaseDirectory, @"Export\ASV_Export_Tamed.json");
                string tamedImageFilename = Path.Combine(AppContext.BaseDirectory, @"Export\ASV_Export_Tamed.png");
                string tamedClassName = "";

                string mapFilename = "";
                string clusterFolder = "";

                if (File.Exists(configFilename))
                {
                    //LogWriter.Debug($"Reading export configuation.");
                    string configText = File.ReadAllText(configFilename);
                    try
                    {
                        JObject packConfig = JObject.Parse(configText);

                        mapFilename = packConfig.Property("mapFilename") == null ? "" : packConfig.Property("mapFilename").Value.ToString();
                        clusterFolder= packConfig.Property("clusterFolder") == null ? "" : packConfig.Property("clusterFolder").Value.ToString();


                        //if no save file provided, use ProgramConfig
                        if (mapFilename.Length == 0)
                        {


                        };


                        // parse filters for export options
                        tribeId = packConfig.Property("tribeId") == null ? 0 : (long)packConfig.Property("tribeId").Value;
                        playerId = packConfig.Property("playerId") == null ? 0 : (long)packConfig.Property("playerId").Value;
                        filterLat = packConfig.Property("filterLat") == null ? 50 : (decimal)packConfig.Property("filterLat").Value;
                        filterLon = packConfig.Property("filterLon") == null ? 50 : (decimal)packConfig.Property("filterLon").Value;
                        filterRad = packConfig.Property("filterRad") == null ? 250 : (decimal)packConfig.Property("filterRad").Value;
                        mapStructureContent = packConfig.Property("structureContent") == null ? false : (bool)packConfig.Property("structureContent").Value;

                        //Map structures
                        JObject exportMapStructures = (JObject)packConfig["exportMapStructures"];
                        if (exportMapStructures != null)
                        {
                            mapStructureExportFilename = exportMapStructures.Property("jsonFilename") == null ? "" : exportMapStructures.Property("jsonFilename").Value.ToString();
                            //structureImageFilename = exportStructures.Property("imageFilename") == null ? "" : exportStructures.Property("imageFilename").Value.ToString();
                            //structureClassName = exportStructures.Property("className") == null ? "" : exportStructures.Property("className").Value.ToString();
                        }

                        //Tribes
                        JObject exportTribes = (JObject)packConfig["exportTribes"];
                        if (exportTribes != null)
                        {
                            tribeExportFilename = exportTribes.Property("jsonFilename") == null ? "" : (string)exportTribes.Property("jsonFilename").Value;
                            tribeImageFilename = exportTribes.Property("imageFilename") == null ? "" : (string)exportTribes.Property("imageFilename").Value;
                            tribeStructures = exportTribes.Property("addStructures") == null ? true : (bool)exportTribes.Property("addStructures").Value;
                            tribePlayers = exportTribes.Property("addPlayers") == null ? true : (bool)exportTribes.Property("addPlayers").Value;
                            tribeTames = exportTribes.Property("addTames") == null ? true : (bool)exportTribes.Property("addTames").Value;
                        }

                        //Structures
                        JObject exportStructures = (JObject)packConfig["exportStructures"];
                        if (exportStructures != null)
                        {
                            structureExportFilename = exportStructures.Property("jsonFilename") == null ? "" : exportStructures.Property("jsonFilename").Value.ToString();
                            structureImageFilename = exportStructures.Property("imageFilename") == null ? "" : exportStructures.Property("imageFilename").Value.ToString();
                            structureClassName = exportStructures.Property("className") == null ? "" : exportStructures.Property("className").Value.ToString();
                        }

                        //Players
                        JObject exportPlayers = (JObject)packConfig["exportPlayers"];
                        if (exportPlayers != null)
                        {
                            playerExportFilename = exportPlayers.Property("jsonFilename") == null ? "" : exportPlayers.Property("jsonFilename").Value.ToString();
                            playerImageFilename = exportPlayers.Property("imageFilename") == null ? "" : exportPlayers.Property("imageFilename").Value.ToString();

                        }

                        //Wilds
                        JObject exportWild = (JObject)packConfig["exportWild"];
                        if (exportWild != null)
                        {
                            wildExportFilename = exportWild.Property("jsonFilename") == null ? "" : exportWild.Property("jsonFilename").Value.ToString();
                            wildImageFilename = exportWild.Property("imageFilename") == null ? "" : exportWild.Property("imageFilename").Value.ToString();
                            wildClassName = exportWild.Property("className") == null ? "" : exportWild.Property("className").Value.ToString();
                            wildMinLevel = exportWild.Property("minLevel") == null ? 0 : (int)exportWild.Property("minLevel").Value;
                            wildMaxLevel = exportWild.Property("maxLevel") == null ? 0 : (int)exportWild.Property("maxLevel").Value;

                        }

                        //Tamed
                        JObject exportTamed = (JObject)packConfig["exportTamed"];
                        if (exportTamed != null)
                        {
                            tamedExportFilename = exportTamed.Property("jsonFilename") == null ? "" : exportTamed.Property("jsonFilename").Value.ToString();
                            tamedImageFilename = exportTamed.Property("imageFilename") == null ? "" : exportTamed.Property("imageFilename").Value.ToString();
                            tamedClassName = exportTamed.Property("className") == null ? "" : exportTamed.Property("className").Value.ToString();
                        }

                    }
                    catch
                    {
                        //bad file data, ignore
                    }
                }

                //load game data
                if (!File.Exists(mapFilename))
                {
                    return;
                }

                //load everything
                ContentContainer exportContainer = new ContentContainer();
                //LogWriter.Debug($"Loading .ark save file.");

                ContentMap loadedMap = new ASVPack.ContentMapPack().SupportedMaps.FirstOrDefault(m => mapFilename.ToLower().Contains(m.Filename.ToLower()));
                
                exportContainer.LoadSaveGame( mapFilename, "", clusterFolder,90);

                //load manager from filtered pack
                //LogWriter.Debug($"Creating filtered ContentPack.");
                ContentPack exportManger = new ContentPack(exportContainer, tribeId, playerId, filterLat, filterLon, filterRad, true, mapStructureContent, tribePlayers, tribeTames, true, tribeStructures, false);

                //Export tribes
                if (tribeExportFilename.Length > 0)
                {
                    string exportFolder = Path.GetDirectoryName(tribeExportFilename);
                    if (!Directory.Exists(exportFolder)) Directory.CreateDirectory(exportFolder);

                    //LogWriter.Info($"Exporting Tribes.");
                    Console.WriteLine($"Exporting Tribes.");
                    exportManger.ExportJsonPlayerTribes(tribeExportFilename);
                }
                if (tribeImageFilename.Length > 0)
                {
                    string exportFolder = Path.GetDirectoryName(tribeImageFilename);
                    if (!Directory.Exists(exportFolder)) Directory.CreateDirectory(exportFolder);
                    //LogWriter.Info($"Exporting Tribes Image.");

                    //var image = exportManger.GetMapImageTribes(tribeId, tribeStructures, tribePlayers, tribeTames, 0, 0, new ASVStructureOptions(), new List<ContentMarker>());
                    //if (image != null)
                    //{
                    //    image.Save(tribeImageFilename);
                    //}
                }




                //Structures
                if (structureExportFilename.Length > 0)
                {
                    string exportFolder = Path.GetDirectoryName(structureExportFilename);
                    if (!Directory.Exists(exportFolder)) Directory.CreateDirectory(exportFolder);
                    //LogWriter.Info($"Exporting Structures.");
                    Console.WriteLine($"Exporting Structures.");
                    exportManger.ExportJsonPlayerStructures(structureExportFilename);
                }

                if (structureImageFilename.Length > 0)
                {
                    string exportFolder = Path.GetDirectoryName(structureImageFilename);
                    if (!Directory.Exists(exportFolder)) Directory.CreateDirectory(exportFolder);
                    //LogWriter.Info($"Exporting Structures Image.");
                    //var image = exportManger.GetMapImagePlayerStructures(structureImageFilename, tribeId, playerId, 0, 0, new ASVStructureOptions(), new List<ContentMarker>());
                    //if (image != null)
                    //{
                    //    image.Save(structureImageFilename);
                    //}
                }






                //Export Players
                if (playerExportFilename.Length > 0)
                {
                    string exportFolder = Path.GetDirectoryName(playerExportFilename);
                    if (!Directory.Exists(exportFolder)) Directory.CreateDirectory(exportFolder);
                    //LogWriter.Info($"Exporting Players.");
                    Console.WriteLine($"Exporting Players.");
                    exportManger.ExportJsonPlayers(playerExportFilename);
                }

                if (playerImageFilename.Length > 0)
                {
                    string exportFolder = Path.GetDirectoryName(playerImageFilename);
                    if (!Directory.Exists(exportFolder)) Directory.CreateDirectory(exportFolder);
                    //LogWriter.Info($"Exporting Players Image.");
                    //var image = exportManger.GetMapImagePlayers(tribeId, playerId, 0, 0, new ASVStructureOptions(), new List<ContentMarker>());
                    //if (image != null)
                    //{
                    //    image.Save(playerImageFilename);
                    //}
                }

                //Export Wild
                if (wildExportFilename.Length > 0)
                {
                    string exportFolder = Path.GetDirectoryName(wildExportFilename);
                    if (!Directory.Exists(exportFolder)) Directory.CreateDirectory(exportFolder);
                    //LogWriter.Info($"Exporting Wilds.");
                    Console.WriteLine($"Exporting Wilds.");
                    exportManger.ExportJsonWild(wildExportFilename);
                }
                if (wildImageFilename.Length > 0)
                {
                    string exportFolder = Path.GetDirectoryName(wildImageFilename);
                    if (!Directory.Exists(exportFolder)) Directory.CreateDirectory(exportFolder);
                    //LogWriter.Info($"Exporting Wilds Image.");
                    //var image = exportManger.GetMapImageWild(wildClassName, "", wildMinLevel, wildMaxLevel, (float)filterLat, (float)filterLon, (float)filterRad, 0, 0, new ASVStructureOptions(), new List<ContentMarker>());
                    //if (image != null)
                    //{
                    //    image.Save(tamedImageFilename);
                    //}
                }

                //Export tamed
                if (tamedExportFilename.Length > 0)
                {
                    string exportFolder = Path.GetDirectoryName(tamedExportFilename);
                    if (!Directory.Exists(exportFolder)) Directory.CreateDirectory(exportFolder);
                    //LogWriter.Info($"Exporting Tames.");
                    Console.WriteLine($"Exporting Tames.");
                    exportManger.ExportJsonTamed(tamedExportFilename);
                }
                if (tamedImageFilename.Length > 0)
                {
                    string exportFolder = Path.GetDirectoryName(tamedImageFilename);
                    if (!Directory.Exists(exportFolder)) Directory.CreateDirectory(exportFolder);
                    //LogWriter.Info($"Exporting Tames Image.");
                    //var image = exportManger.GetMapImageTamed(tamedClassName, "", true, tribeId, playerId, 0, 0, new ASVStructureOptions(), new List<ContentMarker>());
                    //if (image != null)
                    //{
                    //    image.Save(tamedImageFilename);
                    //}
                }


                //LogWriter.Trace("END ExportCommandLine()");
            }
            catch (Exception ex)
            {
                LogError($"Error in ExportCommandLine for config: {configFilename}", ex);
                Environment.ExitCode = -1;
            }
        }


        private static void ExportStoredTribes(string saveFilename, string exportFolder)
        {
            try
            {
                LogError($"Starting ExportStoredTribes for file: {saveFilename}");
                FileInfo fileInfo = new FileInfo(saveFilename);

                using (Stream stream = File.OpenRead(saveFilename))
                {
                    using (ArkArchive archive = new ArkArchive(stream))
                    {

                        ArkSavegame arkSavegame = new ArkSavegame();


                        arkSavegame.ReadBinary(archive, ReadingOptions.Create()
                                .WithDataFiles(true)
                                .WithGameObjects(false)
                                .WithStoredCreatures(false)
                                .WithStoredTribes(true)
                                .WithStoredProfiles(false,90)
                                .WithBuildComponentTree(false));

                        arkSavegame.FileTime = fileInfo.LastWriteTime.ToLocalTime();
                        arkSavegame.ExtractStoredArkTribes(archive, exportFolder);
                    }

                }
                LogError($"Completed ExportStoredTribes for file: {saveFilename}");
            }
            catch (Exception ex)
            {
                LogError($"Error in ExportStoredTribes for file: {saveFilename}", ex);
                Environment.ExitCode = -1;
            }
        }

        private static void ExportStoredProfiles(string saveFilename, string exportFolder)
        {
            try
            {
                LogError($"Starting ExportStoredProfiles for file: {saveFilename}");
                FileInfo fileInfo = new FileInfo(saveFilename);

                using (Stream stream = File.OpenRead(saveFilename))
                {
                    using (ArkArchive archive = new ArkArchive(stream))
                    {
                        ArkSavegame arkSavegame = new ArkSavegame();

                        arkSavegame.ReadBinary(archive, ReadingOptions.Create()
                                .WithDataFiles(true)
                                .WithGameObjects(false)
                                .WithStoredCreatures(false)
                                .WithStoredTribes(false)
                                .WithStoredProfiles(true,90)
                                .WithBuildComponentTree(false));


                        arkSavegame.FileTime = fileInfo.LastWriteTime.ToLocalTime();
                        arkSavegame.ExtractStoredArkProfiles(archive, exportFolder);
                    }
                }
                LogError($"Completed ExportStoredProfiles for file: {saveFilename}");
            }
            catch (Exception ex)
            {
                LogError($"Error in ExportStoredProfiles for file: {saveFilename}", ex);
                Environment.ExitCode = -1;
            }
        }

        
    }
}
