using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;


namespace FDSSplitter// Note: actual namespace depends on the project name.
{
    public class Program
    {
        private static string FDS_text = ""; //Text to use
        private static int MeshPoisition = 0; //Position of first mesh to add new meshes back to
        private static int precision = 3; //Precision to use for rounding.  This is mm.

        public static void Main(string[] args)
        {

            string FileAddress = @AppDomain.CurrentDomain.BaseDirectory;
            int Meshes = 0;

            switch (args.Length)
            {

                case 0:

                    Console.WriteLine("Enter number of meshes:");

                    if (!int.TryParse(Console.ReadLine(), out Meshes)) return;//trys to record meshes, and will exit if incorrect format used.
                    if (Meshes == 0) return; // must have at least one length, the number meshes needed

                    FileAddress = GetFirstFDS();//Get the first FDS file in the directory
                    if (!File.Exists(FileAddress)) return; //no address found

                    break;

                case 1://Command line input for number meshes only

                    Meshes = int.Parse(args.First());

                    FileAddress = GetFirstFDS();
                    if (!File.Exists(FileAddress)) return; //no address found

                    break;

                case 2://Command line input for number meshes and address of files

                    FileAddress = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, args.First());// combine to make full address
                    if (!File.Exists(FileAddress)) FileAddress = args.First(); //must try to use full address
                    if (!File.Exists(FileAddress)) return; //no address found

                    Meshes = int.Parse(args[1]);

                    break;

                default: return;

            }

            // get main text
            List<Mesh> MainMeshes = GetMeshes(System.IO.File.ReadAllLines(FileAddress));
            if (Meshes <= MainMeshes.Count) return;// Less meshes defined than are present!

            WriteFile(FileAddress, SplitMesh(Meshes, MainMeshes));// Split the mesh and write the files

        }

        private static string GetFirstFDS()
        {
            string newAddress = "";
            string[] addresses = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.FDS");

            if (addresses.Length > 0) newAddress = addresses[0];

            return newAddress;
        }


        private static List<Mesh> GetMeshes(string[] MainCase)
        {

            List<Mesh> MainMeshes = new();// Main Meshes to find

            var sb = new System.Text.StringBuilder();
            bool MeshFound = false;

            foreach (string line in MainCase)
            {

                if (IsMesh(line))//Look for a mesh
                {
                    MainMeshes.Add(new Mesh(line));//Create original messhes

                    if (MeshFound == false)
                    {
                        MeshPoisition = sb.Length;//Record position of first mesh
                        MeshFound = true;
                    }

                }
                else
                {
                    // Build file without meshes
                    sb.AppendLine(line.ToString());
                }

            }

            // Record text without meshes
            FDS_text = sb.ToString();
            return MainMeshes;


        }
        private static Boolean IsMesh(string aLine)
        {

            //aLine
            // confirm line contains &MESH
            var trimed = aLine.Trim();
            if (trimed.Contains(@"&MESH") & trimed.StartsWith(@"&MESH"))
            {
                return true;
            }

            return false;
        }


        private static List<string> SplitMesh(int MeshesNeeded, List<Mesh> MainMeshes)
        {

            long TotalCell = 0;             // Total number of cells in domain
            long IdealCellsPerMesh = 0;     // Perfect ratio of cells to meshes
            int Factor = 2;                 // Factor to divide the meshes    
            int ijk = 0;                    // Mesh axis counter
            long Counter = 0;               // Exit counter if in endless loop
            List<Mesh> newMeshes = new();   // New meshes to create
            newMeshes.AddRange(MainMeshes); // Add existing meshes to be processed

            double[] MaxDiv = new double[3];// get domain maximum division for each axis.
            double[] DivOffset = new double[3];// get the offset for each axis, i.e. may be 0.125m origin for a 0.2m cell size
            double div = double.MinValue;   //local division for each mesh

            // get maximum devision in domain
            foreach (Mesh mesh in MainMeshes)
            {
                TotalCell += mesh.Cells;

                for (ijk = 0; ijk < 3; ijk++)
                {
                    div = (mesh.XB[ijk * 2 + 1] - mesh.XB[ijk * 2 + 0]) / mesh.IJK[ijk];
                    if (div > MaxDiv[ijk])
                    {   // get the offset for this division
                        MaxDiv[ijk] = div;
                        DivOffset[ijk] = (double)Math.Round(mesh.XB[ijk * 2]- snap(mesh.XB[ijk * 2], MaxDiv[ijk]), precision, MidpointRounding.ToEven);// gets the difference between snapped and actual grid size for offsets
                    }
                }
            }

            IdealCellsPerMesh = TotalCell / MeshesNeeded;

            for (ijk = 0; ijk < 3; ijk++)
            {
                MaxDiv[ijk] = (double)Math.Round(MaxDiv[ijk], precision, MidpointRounding.ToEven);//ensures it is rounded
            }

            do  //main loop to remesh the domains
            {

                Counter = Counter + 1;//counter to exit

                // sort by largest number of cells
                newMeshes = newMeshes.OrderByDescending(Mesh => Mesh.Cells).ToList();

                //find largest dimension
                Mesh LM = newMeshes.First();//Largest Mesh by cell count
                
                long newIJK_a = -1; long newIJK_b = -1;
                double NewDim = double.MinValue;
                double Div = double.MinValue;
                double a = double.MinValue; double b = double.MinValue;
                Mesh newMeshA; Mesh newMeshB;

                // Determine largest dimension to split
                if ((LM.IJK[0] >= LM.IJK[1]) & (LM.IJK[0] >= LM.IJK[2]))
                {
                    ijk = 0;
                }
                else if ((LM.IJK[1] >= LM.IJK[0]) & (LM.IJK[1] >= LM.IJK[2]))
                {
                    ijk = 1;
                }
                else
                {
                    ijk = 2;
                }

                // 'Limits of selected axis
                a = LM.XB[ijk * 2]; b = LM.XB[ijk * 2 + 1];

                Factor = Math.Min(6,Math.Max(2,(int)(LM.Cells / IdealCellsPerMesh / 2)));// sets up the devision to address very large cells needing multiple divisions max between 2 and 6

                // Snaps to largest grid size in domain, factored based on how big the cell is distance of a to b
                NewDim = (double)Math.Round(snap(a, b, MaxDiv[ijk], Factor ) + DivOffset[ijk], precision, MidpointRounding.ToEven);
                Div = (double)((LM.XB[ijk * 2 + 1] - LM.XB[ijk * 2 + 0]) / LM.IJK[ijk]);//Cell Size (division in X direction)

                newIJK_a = (int)(Math.Round((NewDim - a) / Div, 0, MidpointRounding.ToEven));//Number of dividers for new mesh
                newIJK_b = LM.IJK[ijk] - newIJK_a;//always keeps the same total dividers to maintain grid size

                //Create two new meshes
                switch (ijk)
                {
                    case 0:
                        newMeshA = new Mesh(newIJK_a, LM.IJK[1], LM.IJK[2], LM.XB[0], NewDim, LM.XB[2], LM.XB[3], LM.XB[4], LM.XB[5]);
                        newMeshB = new Mesh(newIJK_b, LM.IJK[1], LM.IJK[2], NewDim, LM.XB[1], LM.XB[2], LM.XB[3], LM.XB[4], LM.XB[5]);
                        break;
                    case 1:
                        newMeshA = new Mesh(LM.IJK[0], newIJK_a, LM.IJK[2], LM.XB[0], LM.XB[1], LM.XB[2], NewDim, LM.XB[4], LM.XB[5]);
                        newMeshB = new Mesh(LM.IJK[0], newIJK_b, LM.IJK[2], LM.XB[0], LM.XB[1], NewDim, LM.XB[3], LM.XB[4], LM.XB[5]);
                        break;

                    default:
                        newMeshA = new Mesh(LM.IJK[0], LM.IJK[1], newIJK_a, LM.XB[0], LM.XB[1], LM.XB[2], LM.XB[3], LM.XB[4], NewDim);
                        newMeshB = new Mesh(LM.IJK[0], LM.IJK[1], newIJK_b, LM.XB[0], LM.XB[1], LM.XB[2], LM.XB[3], NewDim, LM.XB[5]);
                        break;
                }

                if (newIJK_a > 2 & newIJK_b > 2) //Only create news meshes if they are both 3 cells or more
                {
                    //remove current mesh
                    newMeshes.RemoveAt(0);

                    //add the two new ones.
                    newMeshes.Add(newMeshA); newMeshes.Add(newMeshB);
                }

            } while (newMeshes.Count < MeshesNeeded & Counter < 100000); //Loop - only keep going where both counters is less than 100000 and new meshes are needed.


            // Parse new meshes to strings
            List<string> SplitMesh = new List<string>();
            int MeshCount = 0;
            foreach (Mesh aMesh in newMeshes)
            {

                MeshCount += 1;

                SplitMesh.Add($"&MESH ID = '{MeshCount}', IJK = {aMesh.IJK[0]}, {aMesh.IJK[1]}, {aMesh.IJK[2]}, XB = {aMesh.XB[0]}, {aMesh.XB[1]}, {aMesh.XB[2]}, {aMesh.XB[3]}, {aMesh.XB[4]}, {aMesh.XB[5]}/");

            }

            return SplitMesh;// give back new meshes

        }

        //Snaps to nearest half grid
        private static double snap(double a, double b, double div, int Factor)
        {

            return (double)Math.Round((a + ((b - a) / Factor)) / div, 0, MidpointRounding.AwayFromZero) * div;

        }

        // Snaps to nearest division
        private static double snap(double a, double div)
        {

            return (double)Math.Round(a / div, 0, MidpointRounding.AwayFromZero) * div;

        }


        private static void WriteFile(string FileAddress, List<string> NewMeshes)
        {

            var sb = new System.Text.StringBuilder();

            FileAddress = FileAddress.Substring(0, FileAddress.Length - 4);
            FileAddress += $"_{NewMeshes.Count}_Meshes.fds";

            foreach (string line in NewMeshes)
            {
                // Build file with meshes only
                sb.AppendLine(line.ToString());
                // show in console
                Console.WriteLine(line.ToString());
            }


            // Insert meshes back into FDS text
            FDS_text = FDS_text.Insert(MeshPoisition, sb.ToString());

            // save to disc
            File.WriteAllText(FileAddress, FDS_text.ToString());

        }

        public class Mesh
        {

            public long[] IJK = new long[3];
            public long Cells = 0;
            public double[] XB = new double[6];

            public Mesh(long a, long b, long c, double d, double e, double f, double g, double h, double i)
            {

                IJK[0] = a;
                IJK[1] = b;
                IJK[2] = c;

                Cells = IJK[0] * IJK[1] * IJK[2];

                XB[0] = d;
                XB[1] = e;
                XB[2] = f;
                XB[3] = g;
                XB[4] = h;
                XB[5] = i;

            }

            public Mesh(string MeshLine)
            {


                // parse the string
                if (MeshLine.EndsWith("/")) MeshLine = MeshLine.Substring(0, MeshLine.Length - 1);
                char[] SplitBy = new char[] { char.Parse(" "), char.Parse(",") };
                List<string> ListParts = MeshLine.Split(SplitBy).ToList();
                // remove any unwanted additional spaces or null items
                ListParts.RemoveAll(RemoveEmpty);

                string[] Parts = ListParts.ToArray();//The Main line to work on

                string trimed = "";

                for (int i = 0; i < Parts.Length; i++)
                {

                    trimed = Parts[i].Trim();
                    trimed = trimed.ToUpper();
                    trimed = trimed.Trim();


                    // This simply tries to catch all the different ways FDS can read the input file:).  If there is a better way please send on FDS 'reader' logic.
                    if (trimed.StartsWith("IJK"))
                    {
                        if (Parts[i].Split('=').Count() > 1)
                        {
                            IJK[0] = long.Parse(Parts[i].Split('=')[1]);
                            IJK[1] = long.Parse(Parts[i + 1]);
                            IJK[2] = long.Parse(Parts[i + 2]);
                        }
                        else
                        {
                            IJK[0] = long.Parse(Parts[i + 1].Trim(char.Parse("=")));// remove '='
                            IJK[1] = long.Parse(Parts[i + 2]);
                            IJK[2] = long.Parse(Parts[i + 3]);
                        }

                        Cells = IJK[0] * IJK[1] * IJK[2];
                    }
                    else if (trimed.StartsWith("XB"))
                    {

                        if (Parts[i].Split('=').Count() > 1)
                        {
                            XB[0] = double.Parse(Parts[i].Split('=')[1]);// take 2nd part of XB after =
                            XB[1] = double.Parse(Parts[i + 1]);
                            XB[2] = double.Parse(Parts[i + 2]);
                            XB[3] = double.Parse(Parts[i + 3]);
                            XB[4] = double.Parse(Parts[i + 4]);
                            XB[5] = double.Parse(Parts[i + 5]);
                        }
                        else
                        {
                            XB[0] = double.Parse(Parts[i + 1].Trim(char.Parse("="))); // remove '='
                            XB[1] = double.Parse(Parts[i + 2]);
                            XB[2] = double.Parse(Parts[i + 3]);
                            XB[3] = double.Parse(Parts[i + 4]);
                            XB[4] = double.Parse(Parts[i + 5]);
                            XB[5] = double.Parse(Parts[i + 6]);
                        }

                    }


                }

                //I dont allow FDS cases to be less than 1mm...So I have rounded here.  (it will filter out tiny scientific notation numbers near zero for example.)
                for (int i = 0; i < 6; i++)
                {
                    XB[i] = (double)Math.Round(XB[i], precision, MidpointRounding.AwayFromZero);
                }


                static bool RemoveEmpty(String s)
                {
                    if ("" == s.Trim()) return true;
                    if ("=" == s.Trim()) return true;
                    if (s is null) return true;

                    return false;
                }


            }


        }

    }



}

