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
        //public static bool CopyWaypointStrictly = false;

        static void Main(string[] args)
        {
            var settings = new IniFile("settings.ini").GetSection("settings");

            CopyOverlay = settings.GetBooleanValue("CopyOverlay", true);
            CopyStructure = settings.GetBooleanValue("CopyStructure", true);
            CopyUnit = settings.GetBooleanValue("CopyUnit", true);
            CopyInfantry = settings.GetBooleanValue("CopyInfantry", true);
            CopyAircraft = settings.GetBooleanValue("CopyAircraft", true);
            CopySmudge = settings.GetBooleanValue("CopySmudge", true);
            CopyTerrain = settings.GetBooleanValue("CopyTerrain", true);
            CopyCellTag = settings.GetBooleanValue("CopyCellTag", true);
            CopyWaypoint = settings.GetBooleanValue("CopyWaypoint", true);


            Console.WriteLine("请输入源地图文件名：");
            MapFileName = Console.ReadLine();

            Console.WriteLine("请输入起始路径点：");
            StartWayPoint = int.Parse(Console.ReadLine());

            Console.WriteLine("请输入结尾路径点：");
            EndWayPoint = int.Parse(Console.ReadLine());

            Console.WriteLine("请输入目标地图文件名：");
            TargetMapFileName = Console.ReadLine();

            Console.WriteLine("请输入起始路径点：");
            TargetWaypoint = int.Parse(Console.ReadLine());

            var originMap = new MapFile();
            originMap.ReadIsoMapPack5(MapFileName);
            originMap.GetUsedObjects(MapFileName);
            originMap.ReadOverlay(MapFileName);
            originMap.ReadNonTileObjects(MapFileName);

            var targetMap = new MapFile();
            targetMap.ReadIsoMapPack5(TargetMapFileName);
            targetMap.MergeIsoMapPack5(TargetMapFileName, originMap.IsoTileList);
            targetMap.SaveIsoMapPack5(TargetMapFileName);

            if (CopyOverlay)
            {
                targetMap.ReadOverlay(TargetMapFileName);
                targetMap.MergeOverlay(TargetMapFileName, originMap.OverlayList);
                targetMap.SaveOverlay(TargetMapFileName);
            }

            targetMap.ReadNonTileObjects(TargetMapFileName);
            targetMap.MergeNonTileObjects(TargetMapFileName, originMap.NonTileObjectList, originMap.IsoTileList);
            targetMap.SaveNonTileObjects(TargetMapFileName);

        }
    }
}
