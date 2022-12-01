using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;
using System.Reflection;


namespace whereis
{
    class Program
    {
        static bool wildcard = false;
        //static string sdxroot = null;
        static string SdxRootPath = null;
        static int totalFiles = 0;
        static int depotFiles = 0;
        static string csvname = @"whereis_idx.csv";
        static string sdxsavename = @"sxdroot_saved.txt";
        static string filepath = @"c:\temp\";
        static string csvfull = filepath + csvname;
        static string sdxsavefull = filepath + sdxsavename;
        static string searchterm = null;
        static bool sdxrootvalid = false;
        static bool sdxsavedvalid = false;
        static bool rebuildidx = false;
        static List<string> outputLines = new List<string>();


        // Following variables for new switches
        static bool verboseMode = false;
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
            Console.WriteLine(" /ld\tList Depots (Display current filtered good and bad depots lists. ignores all other switches, outputs depot lists and exits)");
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

        public static void GetSystemVariables()
        {

            SdxRootPath = Environment.GetEnvironmentVariable("sdxroot");

            bool tmpsdxrootvalid = sdxrootvalid;
            bool tmpsdxsavedvalid = sdxsavedvalid;
            string tmpsdxsavepath = null;

            if (File.Exists(csvfull))
            {
                rebuildidx = false;
            }
            else
            {
                rebuildidx = true;
            }
            if (File.Exists(sdxsavefull))
            {
                tmpsdxsavepath = File.ReadAllText(sdxsavefull);
                if (Directory.Exists(tmpsdxsavepath))
                {
                    tmpsdxsavedvalid = true;
                    SdxRootPath = tmpsdxsavepath;
                    tmpsdxrootvalid = true;
                }
                else
                {
                    Console.WriteLine("Invalid path in {0}. Deleting.", sdxsavefull);
                    File.Delete(sdxsavefull);
                }
            }
            if (SdxRootPath != null)
            {
                if (Directory.Exists(SdxRootPath))
                {
                    tmpsdxrootvalid = true;
                    if (tmpsdxsavedvalid == false)
                    {
                        File.WriteAllText(sdxsavefull, SdxRootPath);
                        tmpsdxsavedvalid = true;
                    }
                }
                else
                {
                    SdxRootPath = null;
                    tmpsdxrootvalid = false;
                }
            }

            if (tmpsdxrootvalid == false)
            {
                Console.WriteLine("sdxroot not defined and/or {0} invalid. Please enter sdxroot path to filter available depots: ", sdxsavefull);
                string testsdxroot = null;
                while (SdxRootPath == null)
                {
                    testsdxroot = Console.ReadLine();
                    if (Directory.Exists(testsdxroot))
                    {
                        SdxRootPath = testsdxroot;
                        tmpsdxrootvalid = true;
                        tmpsdxsavedvalid = true;
                        File.WriteAllText(sdxsavefull, SdxRootPath);
                    }
                    else
                    {
                        Console.WriteLine();
                        Console.WriteLine("Invalid directory, please re-enter");
                        Console.WriteLine();
                    }
                }
            }
            sdxrootvalid = tmpsdxrootvalid;
            sdxsavedvalid = tmpsdxsavedvalid;
        }

        public static void FilterDirectories()
        {
            string[] reducedDepotsTemp = { "avcore", "base", "drivers", "ds", "enduser", "inetcore", "mincore", "minio", "minkernel", "nanoserver", "net", "onecore", "onecoreuap", "sdktools", "servercommon", "termsrv", "vm" };
            reducedDepotsList.AddRange(reducedDepotsTemp);
            List<string> availableDepots = new List<string>();
            List<string> removedDepots = new List<string>();

            availableDepots.AddRange(Directory.GetDirectories(SdxRootPath));
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

            if (ld == true)
            {
                Console.WriteLine();
                Console.WriteLine("ld switch entered, so displaying filtered depot lists and exiting. Remove ld switch to use full features of whereis.");
                Console.WriteLine();
                Console.WriteLine("Current filtered list of bad depots:");
                foreach (var cbd in currentBadDepotsList)
                {
                    Console.WriteLine(cbd);
                }
                Console.WriteLine();
                Console.WriteLine("Current filtered list of good depots:");
                foreach (var cdl in currentDepotsList)
                {
                    Console.WriteLine(cdl);
                }
                Console.WriteLine();
                Console.WriteLine("Exiting");
                return;
            }

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
            bool _help = false;
            int idx = 0;
            int argnum = 0;

            if (args.Count() != 0)
            {
                foreach (var ag in args)
                {
                    if (ag.Contains("*"))
                    {
                        wildcard = true;
                        args[argnum] = ag.Trim('*');
                        searchterm = args[argnum];
                    }
                    argnum++;
                }

                foreach (var a in args)
                {
                    switch (a.ToLower())
                    {
                        case @"-ad":
                        case @"/ad":
                            ed = true;
                            excludeDepots.AddRange(args[idx + 1].Split(','));
                            idx += 2;
                            rebuildidx = true;
                            break;
                        case @"-id":
                        case @"/id":
                            id = true;
                            includeDepots.AddRange(args[idx + 1].Split(','));
                            idx += 2;
                            rebuildidx = true;
                            break;
                        case @"-ed":
                        case @"/ed":
                            ed = true;
                            excludeDepots.AddRange(args[idx + 1].Split(','));
                            idx += 2;
                            rebuildidx = true;
                            break;
                        case @"-it":
                        case @"/it":
                            it = true;
                            includeBinaryTypes.AddRange(args[idx + 1].Split(','));
                            idx += 2;
                            rebuildidx = true;
                            break;
                        case @"-et":
                        case @"/et":
                            et = true;
                            excludeBinaryTypes.AddRange(args[idx + 1].Split(','));
                            idx += 2;
                            rebuildidx = true;
                            break;
                        case @"-v":
                        case @"/v":
                            verboseMode = true;
                            idx += 1;
                            rebuildidx = false;
                            break;
                        case @"-ld":
                        case @"/ld":
                            ld = true;
                            idx += 1;
                            rebuildidx = false;
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

                if (_help)
                {
                    Help();
                    return;
                }
            }
            else
            {
                rebuildidx = true;
                string answer = "no";
                Console.WriteLine("Index file exists, but no search term specified. Do you want to rebuild index? (y)es / (n)o: ");
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
                else
                {
                    Console.WriteLine("Invalid response, exiting.");
                    return;
                }
            }
            GetSystemVariables();
            if (ld == true)
            {
                FilterDirectories();
                return;
            }
            if (rebuildidx == true)
            {
                FilterDirectories();
                BuildIndex build = new BuildIndex(SdxRootPath);
                if (searchterm != null)
                {
                    SearchIndex index = new SearchIndex(searchterm);
                }
                return;
            }
            else
            {
                if (searchterm != null)
                {
                    SearchIndex index = new SearchIndex(searchterm);
                }
            }

        }

        public class BuildIndex
        {
            
            public BuildIndex(string dir)
            {
                Console.WriteLine("Gathering directory structure.");
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
                Console.WriteLine("Completed index. Indexed {0} files and wrote index to: {1}", totalFiles, csvfull);
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

                    File.AppendAllLines(csvfull, outputLines.ToArray());

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

                using (TextFieldParser parser = new TextFieldParser(csvfull))
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
