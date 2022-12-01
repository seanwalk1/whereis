using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;


namespace whereis
{
    class Program
    {
        static bool wildcard = false;
        static string sdxroot = null;
        static int totalFiles = 0;
        static int depotFiles = 0;
        static List<string> outputLines = new List<string>();

        // Following variables for new switches
        static bool runNew = false;
        static bool verboseMode = false;
        static bool reducedDepots = false;
        static string[] defaultDepotsArray = { "avcore", "base", "clientcore", "developer", "drivers", "ds", "enduser", "inetcore", "MergedComponents", "mincore", "minio", "minkernel", "nanoserver", "net", "onecore", "onecoreuap", "sdktools", "servercommon", "termsrv", "vm" };
        //static string[] defaultBadDepotsArray = { "_RequiredOriginal", "git", "intl", ".", "loc", "admin", "amcore", "analog", "build", "bvtbin", "com", "data", "gamecore", "inbox", "inetsrv", "iot", "mrcommon", "multimedia" };
        static string[] defaultBadDepotsArray = { "_RequiredOriginal", "branchconfig", "config", "githooks", "git", "intl", ".", "loc", "admin", "amcore", "analog", "build", "bvtbin", "com", "data", "gamecore", "inbox", "inetsrv", "iot", "mrcommon", "multimedia", "osclient", "pcshell", "printscan", "Public", "redist", "sdpublic", "server", "services", "shell", "shellcommon", "shellcommondesktopbase", "team", "ua", "windows", "xbox" };
        static List<string> reducedDepotsList = new List<string>();
        static List<string> currentDepotsList = new List<string>();
        static List<string> defaultBadDepotsList = new List<string>();
        static List<string> currentBadDepotsList = new List<string>();
        

        static bool ad = false;
        static bool id = false;
        static bool ed = false;
        static bool it = false;
        static bool et = false;
        static bool sn = false;
        static bool sp = false;
        static bool ifpf = false;
        static bool sb = false;
        static bool ld = false;

        static List<string> addDepots = new List<string>();
        static List<string> includeDepots = new List<string>();
        static List<string> excludeDepots = new List<string>();
        static List<string> includeBinaryTypes = new List<string>();
        static List<string> excludeBinaryTypes = new List<string>();
        // Setting default csv file (search and save) name and path to regular location when not overridden
        static string saveNameDefault = @"whereis_idx.csv";
        static string saveName = saveNameDefault;

        static string savePathDefault = @"c:\temp";
        static string savePath = savePathDefault;
        //static string idxFullPathDefault=  @"c:\temp\whereis_idx.csv";

        static string idxFullPathDefault = @"c:\temp\whereis_idx.csv";
        static string savePathFull = idxFullPathDefault;
        // New switches end

        public static void Help()
        {
            Console.WriteLine();
            Console.WriteLine("Usage");
            Console.WriteLine("===============================================================================");
            Console.WriteLine("whereis.exe </?> </ad> </id> </ed> </ld> </b> </v> </it> </et> </n> </p> </l>");
            Console.WriteLine();
            Console.WriteLine(" /?\tDisplay Help (Currently Displayed)");
            Console.WriteLine(" /ad\tAdd Depot (Add comma-seperated list of depots to default list)");
            Console.WriteLine(" /id\tInclude Depot (Include ONLY comma-seperated list of depots)");
            Console.WriteLine(" /ed\tExclude Depot (Exclude comma-seperated list of depots from full list)");
            Console.WriteLine(" /ld\tList Depots (Display default depot list. ignores all other switches, outputs depot list and exits)");
            Console.WriteLine(" /b\tBare output (Only display one line for each found binary in search mode)");
            Console.WriteLine(" /v\tVerbose output (Display verbose output while building index)");
            Console.WriteLine();
            Console.WriteLine(" /it\tInclude Types (Add file extension(s) to default list (current default: *.exe, *.sys, *.dll) (Not currently implemented)");
            Console.WriteLine(" /et\tExclude Types (Exclude file extension(s) from default list) (Not currently implemented)");
            Console.WriteLine(" /n\tSave Name (Not currently implemented)");
            Console.WriteLine(" /p\tSave Path (Not currently implemented)");
            Console.WriteLine(" /l\tIDX Full Path Default (Not currently implemented)");
            Console.WriteLine();
        }
        public static void FilterDirectories()
        {
            string[] reducedDepotsTemp = { "avcore", "base", "drivers", "ds", "enduser", "inetcore", "mincore", "minio", "minkernel", "nanoserver", "net", "onecore", "onecoreuap", "sdktools", "servercommon", "termsrv", "vm" };
            reducedDepotsList.AddRange(reducedDepotsTemp);
            List<string> availableDepots = new List<string>();
            List<string> removedDepots = new List<string>();
            availableDepots.AddRange(Directory.GetDirectories(sdxroot));
            availableDepots.ConvertAll(d => d.ToLower());

            //Setting up bad depot list.
            defaultBadDepotsList.AddRange(defaultBadDepotsArray);
            defaultBadDepotsList.ConvertAll(d => d.ToLower());
            currentBadDepotsList.AddRange(defaultBadDepotsArray);
            currentBadDepotsList.ConvertAll(d => d.ToLower());

            string[] copyAvailableDepots = availableDepots.ToArray();

            foreach (var cavd in copyAvailableDepots)
            {
                string tmpdepot = cavd.Split('\\', '.').Last();
                foreach (var cbd in currentBadDepotsList)
                {
                    string tmpbad = cbd.Split('\\', '.').Last();
                    if (tmpdepot.Equals(tmpbad))
                    {
                        if (verboseMode)
                        {
                            removedDepots.Add(cavd);
                        }
                        availableDepots.Remove(cavd);
                    }
                }
            }
            if (verboseMode)
            {
                foreach (var rd in removedDepots.Distinct().ToList())
                {
                    Console.WriteLine("Bad depot detected, so removing {0} from depot list.", rd);
                }
                removedDepots.Clear();
            }
            currentDepotsList = availableDepots;


            if (ad == true)
            {
                List<string> tmpAddDepots = new List<string>();

                foreach (var bd in currentDepotsList)
                {
                    foreach (var ad in addDepots)
                    {
                        if (bd.ToLower().Contains(ad.ToLower()))
                            continue;
                        tmpAddDepots.Add(ad);
                    }
                }
                tmpAddDepots.AddRange(currentDepotsList);
                currentDepotsList = tmpAddDepots.Distinct().ToList();
                tmpAddDepots.Clear();
            }
            else if (id == true)
            {
                currentDepotsList = includeDepots;
            }
            else if (ed == true)
            {
                List<string> tempRemovedDepots = new List<string>();

                foreach (var cdl in currentDepotsList)
                {
                    foreach (var ed in excludeDepots)
                    {
                        if (cdl.ToLower().Contains(ed.ToLower()))
                        {
                            tempRemovedDepots.Add(ed);
                        }
                    }
                }
                currentDepotsList = tempRemovedDepots.Distinct().ToList();
                tempRemovedDepots.Clear();
            }
        }

        static void Main(string[] args)
        {
            string getEnv = Environment.GetEnvironmentVariable("sdxroot");
            string answer = "no";
            bool rebuild = false;
            bool _help = false;


            if (args.Count() != 0)
            {
                rebuild = args[0].ToLower().Contains("rebuild") ? true : false;

                if (args[0].Contains("*"))
                {
                    wildcard = true;
                    args[0] = args[0].Trim('*');
                }

                // Adding new command line switches start
                if (runNew)
                {
                    int idx = 0;
                    foreach (var a in args)
                    {
                        switch (a.ToLower())
                        {
                            case @"-ad":
                            case @"/ad":
                                ed = true;
                                excludeDepots.AddRange(args[idx + 1].Split(','));
                                idx += 2;
                                rebuild = true;
                                break;
                            case @"-id":
                            case @"/id":
                                id = true;
                                includeDepots.AddRange(args[idx + 1].Split(','));
                                idx += 2;
                                rebuild = true;
                                break;
                            case @"-ed":
                            case @"/ed":
                                ed = true;
                                excludeDepots.AddRange(args[idx + 1].Split(','));
                                idx += 2;
                                rebuild = true;
                                break;
                            case @"-it":
                            case @"/it":
                                it = true;
                                includeBinaryTypes.AddRange(args[idx + 1].Split(','));
                                idx += 2;
                                rebuild = true;
                                break;
                            case @"-et":
                            case @"/et":
                                et = true;
                                excludeBinaryTypes.AddRange(args[idx + 1].Split(','));
                                idx += 2;
                                rebuild = true;
                                break;
                            case @"-n":
                            case @"/n":
                                sn = true;
                                saveName = args[idx + 1];
                                idx += 2;
                                rebuild = true;
                                break;
                            case @"-p":
                            case @"/p":
                                sp = true;
                                savePath = args[idx + 1];
                                idx += 2;
                                rebuild = true;
                                break;
                            case @"-l":
                            case @"/l":
                                ifpf = true;
                                idxFullPathDefault = args[idx + 1];
                                idx += 2;
                                rebuild = false;
                                break;
                            case @"-v":
                            case @"/v":
                                verboseMode = true;
                                idx += 1;
                                rebuild = false;
                                break;
                            case @"-ld":
                            case @"/ld":
                                ld = true;
                                idx += 1;
                                rebuild = false;
                                break;
                            case @"/?":
                            case @"-?":
                            case @"/help":
                            case @"-help":
                            case @"/usage":
                            case @"-usage":
                                _help = true;
                                break;

                        }
                    }
                    if (verboseMode)
                    {
                        if (ld)
                        {
                            Console.WriteLine("Default list of indexed depots:");
                            for (int i = 0; i < defaultDepotsArray.Count(); i++)
                            {
                                Console.WriteLine(defaultDepotsArray[i]);
                            }
                            return;
                        }
                        Console.WriteLine("-id switch: {0}", id);
                        foreach (var i in includeDepots)
                        {
                            Console.WriteLine("Include depot: {0}", i);
                        }
                        Console.WriteLine("\n-ed switch: {0}", ed);
                        foreach (var i in excludeDepots)
                        {
                            Console.WriteLine("Exclude depot: {0}", i);
                        }
                        //Console.WriteLine("\n-n switch: {0}, -n name: {1}", sn, saveName);
                        //Console.WriteLine("\n-p switch: {0}, -p path: {1}", sp, savePath);
                        //if (sn == true && sp == true)
                        //{
                        //    savePathFull = savePath + "\\" + saveName + ".csv";
                        //    Console.WriteLine("Full save path: {0}", savePathFull);
                        //}
                        //Console.WriteLine("\n-l switch: {0}, -l full path: {1}", ifpf, idxFullPathDefault == null ? "N\\A" : idxFullPathDefault);
                    }
                }
                else
                {
                    int idx = 0;
                    foreach (var a in args)
                    {
                        switch (a.ToLower())
                        {
                            case @"-b":
                            case @"/b":
                                sb = true;
                                idx += 1;
                                rebuild = false;
                                break;
                            case @"-v":
                            case @"/v":
                                verboseMode = true;
                                idx += 1;
                                rebuild = false;
                                break;
                            case @"-ld":
                            case @"/ld":
                                ld = true;
                                idx += 1;
                                rebuild = false;
                                break;
                            case @"/?":
                            case @"-?":
                            case @"/help":
                            case @"-help":
                            case @"/usage":
                            case @"-usage":
                                _help = true;
                                break;
                        }
                    }

                }
                if (_help)
                {
                    Help();
                    return;

                }
                if (ld)
                {
                    Console.WriteLine("Default list of indexed depots:");
                    for (int i = 0; i < defaultDepotsArray.Count(); i++)
                    {
                        Console.WriteLine(defaultDepotsArray[i]);
                    }
                    return;
                }
                // New switches end

                Console.WriteLine();
                Console.WriteLine("Checking for existing index location at: {0}", idxFullPathDefault);
            }
            else
            {
                Console.WriteLine("Index file exists, but no search term specified. Do you want to rebuild index? (yes/no): ");
                Console.WriteLine("NOTE: Indexing process can take more than 60 minutes to complete.");
                // Scripting workaround start
                answer = "y";
                //answer = Console.ReadLine();
                // Scripting workaround end

                if (answer.ToLower().StartsWith("n") && (args.Count() == 0))
                {
                    Console.WriteLine();
                    Console.WriteLine("No index file, or no search term specified. Exiting.");
                    return;
                }
                else if (answer.ToLower().StartsWith("y"))
                {
                    rebuild = true;
                }
                else
                {
                    Console.WriteLine("Invalid response, exiting.");
                    return;
                }
            }

            if (File.Exists(idxFullPathDefault) == false || rebuild.Equals(true))
            {
                if (rebuild)
                {
                    if (runNew)
                    {
                        Console.WriteLine("No file exists, rebuild specified, or rebuild intrinsic switch specified (-i, -e, -n, -p) so deleting and/or rebuilding index.");
                    }
                    else
                    {
                        Console.WriteLine("No file exists or forced rebuild specified, so deleting and/or rebuilding index.");
                    }
                    Console.WriteLine();
                    if (File.Exists(idxFullPathDefault))
                    {
                        File.Delete(idxFullPathDefault);
                    }
                }
                else
                {
                    Console.WriteLine("Index file doesn't exist. Do you want to build index? (yes/no): ");
                    Console.WriteLine("NOTE: Indexing process can take more than 60 minutes to complete.");
                    answer = Console.ReadLine();

                    if (answer.ToLower().StartsWith("n") && (args.Count() == 0))
                    {
                        Console.WriteLine();
                        Console.WriteLine("No index file, or no search term specified. Exiting.");
                        return;
                    }
                    else if (answer.ToLower().StartsWith("y"))
                    {
                        rebuild = true;
                    }
                    else
                    {
                        Console.WriteLine("Invalid response, exiting.");
                        return;
                    }
                }
                if (getEnv != null)
                {
                    Console.WriteLine("Getting sdxroot path for index root.");
                    sdxroot = getEnv;
                    Console.WriteLine("Got sdxroot path of: {0}", getEnv);
                }
                else
                {
                    Console.WriteLine("sdxroot not defined. Please enter sdxroot path: ");
                    string getDir = Console.ReadLine();
                    try
                    {
                        if (Directory.Exists(getDir))
                        {
                            sdxroot = getDir;
                        }
                        else
                        {
                            Console.WriteLine("Invalid directory specified for sdxroot. Exiting.");
                            return;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception {0}", e);
                        return;
                    }
                }
            }
            else
            {
                Console.WriteLine("Found index file ({0}) ", idxFullPathDefault);
                Console.WriteLine();

                if (args.Count() == 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("No search term specified. To search index specify search string after whereis.exe (e.g. \"whereis.exe krn\"). Exiting");
                    return;
                }

            }

            if (rebuild)
            {
                FilterDirectories();
                BuildIndex build = new BuildIndex(sdxroot);
            }
            if (File.Exists(idxFullPathDefault) && args.Count() != 0)
            {
                SearchIndex index = new SearchIndex(args[0].ToString());
            }
            if (args.Count() == 0)
            {
                Console.WriteLine();
                Console.WriteLine("No search term specified. Exiting");
            }
        }




        public class BuildIndex
        {
            
            public BuildIndex(string dir)
            {
                Console.WriteLine("Gathering directory structure.");
                //bool skip = false;
                string fileName = "sources*";



                Console.WriteLine("Building index: ");

                foreach (var ad in currentDepotsList)
                {
                    DirectoryInfo di = new DirectoryInfo(ad);

                    if (verboseMode)
                    {
                        Console.WriteLine("Checking depot {0}", ad);
                    }
                    FileInfo[] allFiles = di.GetFiles(fileName, System.IO.SearchOption.AllDirectories);
                    Console.WriteLine("Compiling files in {0} depot.", ad);
                    if (verboseMode)
                    {
                        depotFiles = 0;
                    }

                    if (allFiles.Count() == 0)
                    {
                        if (verboseMode)
                        {
                            Console.WriteLine("\t {0} files indexed in {1} depot", depotFiles, ad);
                        }
                        continue;
                    }

                    foreach (var af in allFiles)
                    {

                        checkFiles(af.Name.ToLower(), af.DirectoryName.ToLower());

                    }
                    if (verboseMode)
                    {
                        Console.WriteLine("\t {0} files indexed in {1} depot", depotFiles, ad);
                    }


                }
                Console.WriteLine("Completed index. Indexed {0} files and wrote index to: {1}", totalFiles, savePathFull);
            }

            private static void checkFiles(string name, string path)
            {
                string targetNameText = "TARGETNAME";
                string targetTypeText = "TARGETTYPE";
                string targetKernelText = "NTKERNEL";
                string targetTypeHal = "TARGETTYPE=HAL";
                string nameUnknown = "unknown";
                // Filtering out all the unecessary sources file by the only three strings we care about
                string[] validTypes = { targetNameText.ToLower(), targetTypeText.ToLower(), targetKernelText.ToLower() };
                string[] selTypes = { "program", "dynlink", "hal", "driver", "miniport", "export_driver", "gdi_driver", "proglib", "library"}; //Not including bootpgm type because of its limited use
                string fullPath = path + '\\' + name;
                bool halType = false;
                bool kernNameType = false;
                List<string> tempLinesLower = new List<string>();
                List<string> tempTargetNames = new List<string>();

                using (TextFieldParser parser = new TextFieldParser(fullPath))
                {
                    string Type = null;
                    bool match = false;
                    bool valid = false;
                    parser.TrimWhiteSpace = true;

                    List<string> tempLines = new List<string>();

                    while (!parser.EndOfData)
                    {
                        parser.TrimWhiteSpace = true;
                        string tempFieldOrgLower = parser.ReadLine().ToLower();

                        foreach (var s in selTypes)
                        {
                            if (tempFieldOrgLower.StartsWith(s))
                            {
                                valid = true;
                            }
                        }

                        if (valid == false)
                        {
                            foreach (var t in validTypes)
                            {
                                if (tempFieldOrgLower.StartsWith(t))
                                {
                                    valid = true;
                                }
                            }
                        }
                        if (valid == false)
                        {
                            continue;
                        }

                        tempLines.Add(tempFieldOrgLower.Trim());

                        valid = false;

                    }
                    string tmpTgtName = null;

                    //if (path.Contains(@"src\ds\esent\test\ese\src\blue\src\esetest\loadlibrary"))
                    //{
                    //    System.Diagnostics.Debugger.Break();
                    //}
                    foreach (var tl in tempLines)
                    {
                        char[] delims = { ':', ' ', '\\', '\t', ')', '(', '$' };
                        string tmptl = tl.Trim();
                        string[] splitFieldLowerTemp = tmptl.Split('=', ' ', '\t');//tl.Split('=', '\t');

                        List<string> tempSplitFieldLower = new List<string>();

                        if (splitFieldLowerTemp.Count() != 0 && (splitFieldLowerTemp.Last().Contains("$") || splitFieldLowerTemp.Last().Contains("(") || splitFieldLowerTemp.Last().Contains(")")))
                            continue;

                        if (tmptl.Equals(targetTypeHal.ToLower()))
                        {
                            Type = selTypes[1];
                            halType = true;
                            match = true;
                        }

                        if (splitFieldLowerTemp.Contains(targetKernelText.ToLower()))
                        {
                            tmpTgtName = splitFieldLowerTemp.Last().Trim();
                            Type = selTypes[0];
                            kernNameType = true;
                            match = true;
                        }
                        // ntkernel string end

                        // Grabbing TARGETNAME field for later use
                        else
                        {
                            if (splitFieldLowerTemp[0].Contains(targetNameText.ToLower()))
                            {


                                tmpTgtName = splitFieldLowerTemp.Last().Trim();
                            }
                        }
                        if (splitFieldLowerTemp[0].Contains(targetTypeText.ToLower()))
                        {
                            Type = splitFieldLowerTemp.Last().Trim();

                            foreach (var st in selTypes)
                            {
                                if (Type.Equals(st))
                                {
                                    match = true;
                                }
                            }
                        }

                        if (path != null && Type != null && match == true && tmpTgtName != null)
                        {

                            tempLinesLower.Add(tmpTgtName + ',' + Type + ',' + path);
                            match = false;
                            tmpTgtName = null;
                            Type = null;
                            halType = false;
                            kernNameType = false;

                        }
                    }
                    if (tempLinesLower.Count() == 0)
                    {
                        parser.Close();
                        return;
                    }
                    foreach (var tl in tempLinesLower)
                    {
                        if (tl.StartsWith(nameUnknown))
                        {
                            break;
                        }
                        else
                        {
                            outputLines.Add(tl);
                        }
                    }
                    tempLinesLower.Clear();
                    
                    if (outputLines.Count() == 0)
                    {
                        parser.Close();
                        return;
                    }

                    totalFiles += outputLines.Count();
                    depotFiles += outputLines.Count();

                    File.AppendAllLines(savePathFull, outputLines.ToArray());

                    if (outputLines.Count() > 1 && outputLines.Count() % 1000 == 0)
                    {
                        Console.Write(".");
                        outputLines.Clear();
                    }

                    parser.Close();
                }
                outputLines.Clear();
            }
        }

        public class SearchIndex
        {
            public SearchIndex(string term)
            {

                using (TextFieldParser parser = new TextFieldParser(idxFullPathDefault))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(new string[] { "," });
                    parser.TrimWhiteSpace = true;
                    parser.CommentTokens = new string[] { "#" };


                    while (!parser.EndOfData)
                    {
                        string[] fields = parser.ReadFields();
                        string Name = fields[0].ToLower();
                        string Type = fields[1].ToLower();
                        string Path = fields[2].ToLower();
                        string Ext = null;

                        
                        if (Type.ToLower().Contains("driver") || Type.ToLower().Contains("miniport") || Type.ToLower().Contains("export_driver"))
                        {
                            Ext = "sys";
                        }
                        else if (Type.ToLower().Contains("dynlink") || Type.ToLower().Contains("hal") || Type.ToLower().Contains("gdi_driver"))
                        {
                            Ext = "dll";
                        }
                        else if (Type.ToLower().Contains("program") || Type.ToLower().Contains("proglib"))
                        {
                            Ext = "exe";
                        }
                        else if (Type.ToLower().Contains("library"))
                        {
                            Ext = "lib";
                        }
                        else
                        {
                            Ext = "?";
                        }


                        if (wildcard == true)
                        {

                            if (Name.Contains(term.ToLower()))
                            {
                                if (sb == true)
                                {
                                    Console.WriteLine("{0}\\{1}.{2}", Path, Name, Ext);
                                }
                                else
                                {
                                    Console.WriteLine("Found: {0}.{1}, with path: {2}", Name, Ext, Path);
                                }
                                continue;
                            }
                        }
                        else if (Name.StartsWith(term.ToLower()))
                        {
                            if (sb == true)
                            {
                                Console.WriteLine("{0}\\{1}.{2}", Path, Name, Ext);
                            }
                            else
                            {
                                Console.WriteLine("Found: {0}.{1}, with path: {2}", Name, Ext, Path);
                            }
                            continue;
                        }

                    }
                }
            }

        }

        }
}
