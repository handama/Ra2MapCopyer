using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Rampastring.Tools;
using System.Linq.Expressions;
using System.Diagnostics;
using System.Globalization;

namespace RA2MapCopyer
{

    static class Program
    {
        public static string MapFileName;
        public static int StartWayPoint;
        public static int EndWayPoint;

        public static string TargetMapFileName;
        public static int TargetWaypoint;

        public static bool CopyOverlay = true;
        public static bool CopyStructure = true;
        public static bool CopyUnit = true;
        public static bool CopyInfantry = true;
        public static bool CopyAircraft = true;
        public static bool CopySmudge = true;
        public static bool CopyTerrain = true;
        public static bool CopyCellTag = true;
        public static bool CopyWaypoint = true;
        public static bool CoverExisting = true;
        //public static bool CopyWaypointStrictly = false;

        static void Main(string[] args)
        {

            var map = new MapFile();
/*            map.ReadIsoMapPack5("map.map");
            map.SaveWorkingMapPack("decompressed.ini");*/

            map.LoadWorkingMapPack("decompressed.ini");
            map.SaveIsoMapPack5("map.map");

        }
    }
}
