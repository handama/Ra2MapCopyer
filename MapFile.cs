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
    class NonTileObjectList
    {
        public List<Structure> StructureList;
        public List<Unit> UnitList;
        public List<Infantry> InfantryList;
        public List<Aircraft> AircraftList;
        public List<Smudge> SmudgeList;
        public List<Terrain> TerrainList;
        public List<CellTag> CellTagList;
        public List<Waypoint> WaypointList;
    }
    class MapFile
    {
        public int Width;
        public int Height;
        public List<IsoTile> IsoTileList;
        public List<Overlay> OverlayList;

        public NonTileObjectList NonTileObjectList;

        /*public IniSection Unit = new IniSection("Units");
        public IniSection Infantry = new IniSection("Infantry");
        public IniSection Structure = new IniSection("Structures");
        public IniSection Terrain = new IniSection("Terrain");
        public IniSection Aircraft = new IniSection("Aircraft");
        public IniSection Smudge = new IniSection("Smudge");
        public IniSection Waypoint = new IniSection("Waypoints");*/
        public int[] LocalSize = new int[2];
        public bool[,] Polygon;
        public void ReadIsoMapPack5(string filePath)
        {
            var MapFile = new IniFile(filePath);
            var MapPackSections = MapFile.GetSection("IsoMapPack5");
            var MapSize = MapFile.GetStringValue("Map", "Size", "0,0,0,0");
            string IsoMapPack5String = "";

            int sectionIndex = 1;
            while (MapPackSections.KeyExists(sectionIndex.ToString()))
            {
                IsoMapPack5String += MapPackSections.GetStringValue(sectionIndex.ToString(), "");
                sectionIndex++;
            }

            string[] sArray = MapSize.Split(',');
            Width = Int32.Parse(sArray[2]);
            Height = Int32.Parse(sArray[3]);
            int cells = (Width * 2 - 1) * Height;
            IsoTile[,] Tiles = new IsoTile[Width * 2 - 1, Height];//这里值得注意
            byte[] lzoData = Convert.FromBase64String(IsoMapPack5String);

            //Log.Information(cells);
            int lzoPackSize = cells * 11 + 4;
            var isoMapPack = new byte[lzoPackSize];
            uint totalDecompressSize = Format5.DecodeInto(lzoData, isoMapPack);//TODO 源，目标 输入应该是解码后长度，isoMapPack被赋值解码值了
                                                                               //uint	0 to 4,294,967,295	Unsigned 32-bit integer	System.UInt32
            var mf = new MemoryFile(isoMapPack);

            //Log.Information(BitConverter.ToString(lzoData));
            int count = 0;
            //List<List<IsoTile>> TilesList = new List<List<IsoTile>>(Width * 2 - 1);
            IsoTileList = new List<IsoTile>();
            //Log.Information(TilesList.Capacity);
            for (int i = 0; i < cells; i++)
            {
                ushort rx = mf.ReadUInt16();//ushort	0 to 65,535	Unsigned 16-bit integer	System.UInt16
                ushort ry = mf.ReadUInt16();
                short tilenum = mf.ReadInt16();//short	-32,768 to 32,767	Signed 16-bit integer	System.Int16
                short zero1 = mf.ReadInt16();//Reads a 2-byte signed integer from the current stream and advances the current position of the stream by two bytes.
                byte subtile = mf.ReadByte();//Reads a byte from the stream and advances the position within the stream by one byte, or returns -1 if at the end of the stream.
                byte z = mf.ReadByte();
                byte zero2 = mf.ReadByte();

                count++;
                int dx = rx - ry + Width - 1;

                int dy = rx + ry - Width - 1;
                //Log.Information("{1}", rx, ry, tilenum, subtile, z, dx, dy,count);
                //上面是一个线性变换 旋转45度、拉长、平移
                if (dx >= 0 && dx < 2 * Width &&
                    dy >= 0 && dy < 2 * Height)
                {
                    var tile = new IsoTile((ushort)dx, (ushort)dy, rx, ry, z, tilenum, subtile);//IsoTile定义是NumberedMapObject

                    Tiles[(ushort)dx, (ushort)dy / 2] = tile;//给瓷砖赋值
                    IsoTileList.Add(tile);
                }
            }
            //用来检查有没有空着的
            for (ushort y = 0; y < Height; y++)
            {
                for (ushort x = 0; x < Width * 2 - 1; x++)
                {
                    var isoTile = Tiles[x, y];//从这儿来看，isoTile指的是一块瓷砖，Tile是一个二维数组，存着所有瓷砖
                                              //isoTile的定义在TileLayer.cs里
                    if (isoTile == null)
                    {
                        // fix null tiles to blank
                        ushort dx = (ushort)(x);
                        ushort dy = (ushort)(y * 2 + x % 2);
                        ushort rx = (ushort)((dx + dy) / 2 + 1);
                        ushort ry = (ushort)(dy - rx + Width + 1);
                        Tiles[x, y] = new IsoTile(dx, dy, rx, ry, 0, 0, 0);
                    }
                }

            }
        }

        public void SaveIsoMapPack5(string path)
        {
            long di = 0;
            int cells = (Width * 2 - 1) * Height;
            int lzoPackSize = cells * 11 + 4;
            var isoMapPack2 = new byte[lzoPackSize];
            foreach (var tile in IsoTileList)
            {
                var bs = tile.ToMapPack5Entry().ToArray();//ToMapPack5Entry的定义在MapObjects.cs
                                                          //ToArray将ArrayList转换为Array：
                Array.Copy(bs, 0, isoMapPack2, di, 11);//把bs复制给isoMapPack,从di索引开始复制11个字节
                di += 11;//一次循环复制11个字节
            }

            var compressed = Format5.Encode(isoMapPack2, 5);

            string compressed64 = Convert.ToBase64String(compressed);
            int j = 1;
            int idx = 0;

            var saveFile = new IniFile(path);

            if (saveFile.SectionExists("IsoMapPack5"))
                saveFile.RemoveSection("IsoMapPack5");

            saveFile.AddSection("IsoMapPack5");
            var saveMapPackSection = saveFile.GetSection("IsoMapPack5");

            while (idx < compressed64.Length)
            {
                int adv = Math.Min(74, compressed64.Length - idx);//74 is the length of each line
                saveMapPackSection.SetStringValue(j.ToString(), compressed64.Substring(idx, adv));
                j++;
                idx += adv;//idx=adv+1
            }
            saveFile.WriteIniFile();
        }
        public void SaveWorkingMapPack(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            var mapPack = new IniFile(path);
            mapPack.AddSection("mapPack");
            var mapPackSection = mapPack.GetSection("mapPack");
            int mapPackIndex = 1;
            mapPackSection.SetStringValue("0", "Dx,Dy,Rx,Ry,Z,TileNum,SubTile");
            mapPack.SetStringValue("Map", "Size", Width.ToString() + "," + Height.ToString());

            for (int i = 0; i < IsoTileList.Count; i++)
            {
                var isoTile = IsoTileList[i];
                mapPackSection.SetStringValue(mapPackIndex++.ToString(),
                       isoTile.Dx.ToString() + "," +
                       isoTile.Dy.ToString() + "," +
                       isoTile.Rx.ToString() + "," +
                       isoTile.Ry.ToString() + "," +
                       isoTile.Z.ToString() + "," +
                       isoTile.TileNum.ToString() + "," +
                       isoTile.SubTile.ToString());
            }
            mapPack.WriteIniFile();
        }

        public void SaveWorkingMapPack2(string path, List<IsoTile> IsoTileList2)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            var mapPack = new IniFile(path);
            mapPack.AddSection("mapPack");
            var mapPackSection = mapPack.GetSection("mapPack");
            int mapPackIndex = 1;
            mapPackSection.SetStringValue("0", "Dx,Dy,Rx,Ry,Z,TileNum,SubTile");
            mapPack.SetStringValue("Map", "Size", Width.ToString() + "," + Height.ToString());

            for (int i = 0; i < IsoTileList2.Count; i++)
            {
                
                var isoTile = IsoTileList2[i];
                if (isoTile.Used)
                {
                    mapPackSection.SetStringValue(mapPackIndex++.ToString(),
                       isoTile.Dx.ToString() + "," +
                       isoTile.Dy.ToString() + "," +
                       isoTile.Rx.ToString() + "," +
                       isoTile.Ry.ToString() + "," +
                       isoTile.Z.ToString() + "," +
                       isoTile.TileNum.ToString() + "," +
                       isoTile.SubTile.ToString());
                } 
            }
            mapPack.WriteIniFile();
        }


        /*public void SaveFullMap(string path)
        {
            var fullMap = new IniFile(Program.TemplateMap);
            var settings = new IniFile(Program.WorkingFolder + "settings.ini");
            var bottomSpace = settings.GetIntValue("settings", "BottomSpace", 4);
            fullMap.SetStringValue("Map", "Size", "0,0," + Width.ToString() + "," + Height.ToString());
            LocalSize[0] = Width - 4;
            LocalSize[1] = Height - 11 + 4 - bottomSpace;
            fullMap.SetStringValue("Map", "LocalSize", "2,5," + LocalSize[0].ToString() + "," + LocalSize[1].ToString());
            fullMap.SetStringValue("Map", "Theater", Enum.GetName(typeof(Theater), MapTheater));
            if (Unit != null)
                fullMap.AddSection(Unit);
            if (Infantry != null)
                fullMap.AddSection(Infantry);
            if (Structure != null)
                fullMap.AddSection(Structure);
            if (Terrain != null)
                fullMap.AddSection(Terrain);
            if (Aircraft != null)
                fullMap.AddSection(Aircraft);
            if (Smudge != null)
                fullMap.AddSection(Smudge);
            if (Waypoint != null)
                fullMap.AddSection(Waypoint);
            fullMap.WriteIniFile(path);
            SaveIsoMapPack5(path);
            SaveOverlay(path);
        }*/
        public void LoadWorkingMapPack(string path)
        {
            IsoTileList = new List<IsoTile>();
            var mapPack = new IniFile(path);
            var mapPackSection = mapPack.GetSection("mapPack");
            string[] size = mapPack.GetStringValue("Map", "Size", "0,0").Split(',');
            Width = int.Parse(size[0]);
            Height = int.Parse(size[1]);

            int i = 1;
            while (mapPackSection.KeyExists(i.ToString()))
            {
                if (mapPackSection.KeyExists(i.ToString()))
                {
                    string[] isoTileInfo = mapPackSection.GetStringValue(i.ToString(), "").Split(',');
                    var isoTile = new IsoTile(ushort.Parse(isoTileInfo[0]),
                        ushort.Parse(isoTileInfo[1]),
                        ushort.Parse(isoTileInfo[2]),
                        ushort.Parse(isoTileInfo[3]),
                        (byte)int.Parse(isoTileInfo[4]),
                        short.Parse(isoTileInfo[5]),
                        (byte)int.Parse(isoTileInfo[6]));
                    IsoTileList.Add(isoTile);
                    i++;
                }
            }
        }

        public List<Overlay> ReadOverlay(string path)
        {
            OverlayList = new List<Overlay>();
            var mapFile = new IniFile(path);
            if (!mapFile.SectionExists("OverlayPack") || !mapFile.SectionExists("OverlayDataPack"))
                return null;
            IniSection overlaySection = mapFile.GetSection("OverlayPack");
            if (overlaySection == null)
                return null;

            string OverlayPackString = "";
            int sectionIndex = 1;
            while (overlaySection.KeyExists(sectionIndex.ToString()))
            {
                OverlayPackString += overlaySection.GetStringValue(sectionIndex.ToString(), "");
                sectionIndex++;
            }

            byte[] format80Data = Convert.FromBase64String(OverlayPackString);
            var overlayPack = new byte[1 << 18];
            Format5.DecodeInto(format80Data, overlayPack, 80);

            IniSection overlayDataSection = mapFile.GetSection("OverlayDataPack");
            if (overlayDataSection == null)
                return null;

            string OverlayDataPackString = "";
            sectionIndex = 1;
            while (overlayDataSection.KeyExists(sectionIndex.ToString()))
            {
                OverlayDataPackString += overlayDataSection.GetStringValue(sectionIndex.ToString(), "");
                sectionIndex++;
            }

            format80Data = Convert.FromBase64String(OverlayDataPackString);
            var overlayDataPack = new byte[1 << 18];
            Format5.DecodeInto(format80Data, overlayDataPack, 80);

            foreach (var tile in IsoTileList)
            {
                if (tile == null) continue;
                int idx = tile.Rx + 512 * tile.Ry;
                byte overlay_id = overlayPack[idx];

/*                if (overlay_id != 0xff)
                {*/
                    byte overlay_value = overlayDataPack[idx];
                    var ovl = new Overlay(overlay_id, overlay_value);
                    ovl.Tile = tile.Clone();
                    OverlayList.Add(ovl);
/*                }*/
            }

            return OverlayList;
        }

        public void SaveOverlay(string path)
        {

            var overlayPack = new byte[1 << 18];
            for (int i = 0; i < overlayPack.Length; i++)
            {
                overlayPack[i] = 0xff;
            }
            var overlayDataPack = new byte[1 << 18];
            foreach (var overlay in OverlayList)
            {
                int index = overlay.Tile.Rx + 512 * overlay.Tile.Ry;
                overlayPack[index] = overlay.OverlayID;
                overlayDataPack[index] = overlay.OverlayValue;

            }

            var compressedPack = Format5.Encode(overlayPack, 80);
            var compressedDataPack = Format5.Encode(overlayDataPack, 80);

            string compressedPack64 = Convert.ToBase64String(compressedPack);
            string compressedDataPack64 = Convert.ToBase64String(compressedDataPack);
            int j = 1;
            int idx = 0;

            int j2 = 1;
            int idx2 = 0;

            var saveFile = new IniFile(path);
            if (saveFile.SectionExists("OverlayPack"))
                saveFile.RemoveSection("OverlayPack");

            saveFile.AddSection("OverlayPack");
            if (saveFile.SectionExists("OverlayDataPack"))
                saveFile.RemoveSection("OverlayDataPack");

            saveFile.AddSection("OverlayDataPack");

            var OverlayPackSection = saveFile.GetSection("OverlayPack");
            var OverlayDataPackSection = saveFile.GetSection("OverlayDataPack");

            while (idx < compressedPack64.Length)
            {
                int adv = Math.Min(70, compressedPack64.Length - idx);//70 is the length of each line
                OverlayPackSection.SetStringValue(j.ToString(), compressedPack64.Substring(idx, adv));
                j++;
                idx += adv;//idx=adv+1
            }
            while (idx2 < compressedDataPack64.Length)
            {
                int adv = Math.Min(70, compressedDataPack64.Length - idx2);//70 is the length of each line
                OverlayDataPackSection.SetStringValue(j2.ToString(), compressedDataPack64.Substring(idx2, adv));
                j2++;
                idx2 += adv;//idx=adv+1
            }
            saveFile.WriteIniFile();
        }

        public void SaveWorkingOverlay(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            var overlayPack = new IniFile(path);
            overlayPack.AddSection("overlayPack");
            var mapPackSection = overlayPack.GetSection("overlayPack");
            int mapPackIndex = 1;
            //mapPackSection.SetStringValue("0", "Dx,Dy,Rx,Ry,Z,TileNum,SubTile");

            for (int i = 0; i < OverlayList.Count; i++)
            {
                var overlay = OverlayList[i];
                mapPackSection.SetStringValue(mapPackIndex++.ToString(),
                        overlay.OverlayID.ToString() + "," +
                        overlay.OverlayValue.ToString());

            }
            overlayPack.WriteIniFile();
        }

        public void SaveWorkingOverlay2(string path, List<Overlay> OverlayList2)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            var overlayPack = new IniFile(path);
            overlayPack.AddSection("overlayPack");
            var mapPackSection = overlayPack.GetSection("overlayPack");
            int mapPackIndex = 1;
            //mapPackSection.SetStringValue("0", "Dx,Dy,Rx,Ry,Z,TileNum,SubTile");

            for (int i = 0; i < OverlayList2.Count; i++)
            {
                var overlay = OverlayList2[i];

                if (overlay.Tile.Used)
                {
                    mapPackSection.SetStringValue(mapPackIndex++.ToString(),
                        overlay.OverlayID.ToString() + "," +
                        overlay.OverlayValue.ToString() + ";" +
                        overlay.Tile.RelativeRx + "," +
                        overlay.Tile.RelativeRy);
                }


            }
            overlayPack.WriteIniFile();
        }

        public IsoTile GetIsoTileByXY(int x, int y)
        {
            if (x >= Width + Height || x < 0 || y >= Width + Height || y < 0)
            {
                return null;
            }
            else
            {
                foreach(var isoTile in IsoTileList)
                {
                    if (isoTile.Rx == x && isoTile.Ry == y)
                        return isoTile;
                }
                return null;
            }
        }

        public void GetUsedObjects(string path)
        {
            var mapFile = new IniFile(path);
            var waypointSection = mapFile.GetSection("Waypoints");

            List<Waypoint> WaypointList = new List<Waypoint>();
            List<Line> LineList = new List<Line>();

            for (int i = Program.StartWayPoint; i <= Program.EndWayPoint; i++)
            {
                foreach (var waypointLine in waypointSection.Keys)
                {
                    if (int.Parse(waypointLine.Key) == i)
                    {
                        var waypoint = new Waypoint();
                        waypoint.Initialize(waypointLine);
                        WaypointList.Add(waypoint);
                    }
                }
            }
            for (int i = 0; i < WaypointList.Count(); i++)
            {
                var line = new Line();
                if (i < WaypointList.Count()-1)
                {
                    line.waypoint1 = WaypointList[i];
                    line.waypoint2 = WaypointList[i+1];
                }
                else
                {
                    line.waypoint1 = WaypointList[i];
                    line.waypoint2 = WaypointList[0];
                }
                if (line.waypoint1.X == line.waypoint2.X)
                {
                    line.vertical = true;
                }
                else
                {
                    line.vertical = false;
                    line.k = (float)(line.waypoint1.Y - line.waypoint2.Y) / (float)(line.waypoint1.X - line.waypoint2.X);
                    line.b = (float)line.waypoint2.Y - (float)line.waypoint2.X * line.k;
                    //Console.WriteLine("y={0}x+{1}", line.k, line.b);
                }
                LineList.Add(line);
            }
            var range = Width + Height;

            Polygon = new bool[range, range];

            var Polygon2 = new bool[range, range];
            var Polygon3 = new bool[range, range];

            for (int y = 0; y < range; y++)
            {
                for (int x = 0; x < range; x++)
                {
                    int intersectCount1 = 0;
                    int intersectCount2 = 0;

                    foreach (var line in LineList)
                    {
                        bool xInRange = false;

                        if (line.waypoint1.X >= line.waypoint2.X)
                        {
                            if (x <= line.waypoint1.X && x > line.waypoint2.X)
                                xInRange = true;
                        }
                        else
                        {
                            if (x >= line.waypoint1.X && x < line.waypoint2.X)
                                xInRange = true;
                        }

                        bool yInRange = false;

                        if (line.waypoint1.Y >= line.waypoint2.Y)
                        {
                            if (y <= line.waypoint1.Y && y > line.waypoint2.Y)
                                yInRange = true;
                        }
                        else
                        {
                            if (y >= line.waypoint1.Y && y < line.waypoint2.Y)
                                yInRange = true;
                        }

                        if (xInRange)
                        {
                            if (line.vertical)
                            {
                                if (line.waypoint1.X == x && yInRange)
                                {
                                    intersectCount1++;
                                    intersectCount2++;
                                }
                            }
                            else
                            {
                                if (x * line.k + line.b >= y)
                                {
                                    intersectCount1++;
                                }
                                if (x * line.k + line.b <= y)
                                {
                                    intersectCount2++;
                                }
                            }
                        }
                    }

                    if (intersectCount1 % 2 == 0 || intersectCount2 % 2 == 0)
                    {
                        Polygon[x, y] = false;
                        Polygon2[x, y] = false;
                        Polygon3[x, y] = false;
                    }
                    else
                    {
                        Polygon[x, y] = true;
                        Polygon2[x, y] = false;
                        Polygon3[x, y] = false;
                    }
                }
            }

            for (int y = 0; y < range; y++)
            {
                for (int x = 0; x < range - 1; x++)
                {
                    if (Polygon[x, y] && !Polygon2[x, y])
                    {
                        if (!Polygon[x + 1, y])
                        {
                            Polygon[x + 1, y] = true;
                            Polygon2[x + 1, y] = true;
                        }
                    }
                }
            }
            for (int y = 0; y < range; y++)
            {
                for (int x = 1; x < range; x++)
                {
                    if (Polygon[x, y] && !Polygon3[x, y])
                    {
                        if (!Polygon[x - 1, y])
                        {
                            Polygon[x - 1, y] = true;
                            Polygon3[x - 1, y] = true;
                        }
                    }
                }
            }

            //去除划界路径点，避免复制无用路径点
            foreach (var wp in WaypointList)
            {
                Polygon[wp.X, wp.Y] = false;
            }


            foreach (var isoTile in IsoTileList)
            {
                if (Polygon[isoTile.Rx, isoTile.Ry])
                {
                    isoTile.Used = true;
                    isoTile.RelativeRx = isoTile.Rx - WaypointList[0].X;
                    isoTile.RelativeRy = isoTile.Ry - WaypointList[0].Y;
                }
            }
        }

        public void MergeIsoMapPack5(string path, List<IsoTile> sourceIsoTileList)
        {
            var mapFile = new IniFile(path);
            var waypointSection = mapFile.GetSection("Waypoints");
            var wp = new Waypoint();

            foreach (var waypointLine in waypointSection.Keys)
            {
                if (int.Parse(waypointLine.Key) == Program.TargetWaypoint)
                {
                    wp.Initialize(waypointLine);
                }
            }
            foreach (var sourceIsoTile in sourceIsoTileList)
            {
                
                if (sourceIsoTile.Used)
                {
                    foreach(var isoTile in IsoTileList)
                    {
                        if (isoTile.Rx == sourceIsoTile.RelativeRx + wp.X && isoTile.Ry == sourceIsoTile.RelativeRy + wp.Y)
                        {
                            isoTile.TileNum = sourceIsoTile.TileNum;
                            isoTile.SubTile = sourceIsoTile.SubTile;
                            isoTile.Z = sourceIsoTile.Z;
                            isoTile.Used = sourceIsoTile.Used;
                        }
                    }
                }
            }
        }

        public void MergeOverlay(string path, List<Overlay> sourceOverlayList)
        {
            var mapFile = new IniFile(path);
            var waypointSection = mapFile.GetSection("Waypoints");
            var wp = new Waypoint();

            foreach (var waypointLine in waypointSection.Keys)
            {
                if (int.Parse(waypointLine.Key) == Program.TargetWaypoint)
                {
                    wp.Initialize(waypointLine);
                }
            }
            foreach (var sourceOverlay in sourceOverlayList)
            {

                if (sourceOverlay.Tile.Used)
                {
                    
                    foreach (var overlay in OverlayList)
                    {
                        if (overlay.Tile.Rx == sourceOverlay.Tile.RelativeRx + wp.X && overlay.Tile.Ry == sourceOverlay.Tile.RelativeRy + wp.Y)
                        {
                            overlay.OverlayID = sourceOverlay.OverlayID;
                            overlay.OverlayValue = sourceOverlay.OverlayValue;
                        }
                    }
                }
            }
        }

        public void ReadNonTileObjects(string path)
        {
            var mapFile = new IniFile(path);
            var waypointSection = mapFile.GetSection("Waypoints");
            var wp = new Waypoint();
            NonTileObjectList = new NonTileObjectList();

            foreach (var waypointLine in waypointSection.Keys)
            {
                if (int.Parse(waypointLine.Key) == Program.StartWayPoint)
                {
                    wp.Initialize(waypointLine);
                }
            }

            //建筑
            NonTileObjectList.StructureList = new List<Structure>();
            if (mapFile.SectionExists("Structures"))
            {
                var section = mapFile.GetSection("Structures");
                foreach (var objKey in section.Keys)
                {
                    var obj = new Structure();
                    obj.Initialize(objKey.Value);
                    obj.RelativeX = obj.X - wp.X;
                    obj.RelativeY = obj.Y - wp.Y;
                    NonTileObjectList.StructureList.Add(obj);
                }
            }

            //单位
            NonTileObjectList.UnitList = new List<Unit>();
            if (mapFile.SectionExists("Units"))
            {
                var section = mapFile.GetSection("Units");
                foreach (var objKey in section.Keys)
                {
                    var obj = new Unit();
                    obj.Initialize(objKey.Value);
                    obj.RelativeX = obj.X - wp.X;
                    obj.RelativeY = obj.Y - wp.Y;
                    NonTileObjectList.UnitList.Add(obj);
                }
            }

            //步兵
            NonTileObjectList.InfantryList = new List<Infantry>();
            if (mapFile.SectionExists("Infantry"))
            {
                var section = mapFile.GetSection("Infantry");
                foreach (var objKey in section.Keys)
                {
                    var obj = new Infantry();
                    obj.Initialize(objKey.Value);
                    obj.RelativeX = obj.X - wp.X;
                    obj.RelativeY = obj.Y - wp.Y;
                    NonTileObjectList.InfantryList.Add(obj);
                }
            }

            //飞行器
            NonTileObjectList.AircraftList = new List<Aircraft>();
            if (mapFile.SectionExists("Aircraft"))
            {
                var section = mapFile.GetSection("Aircraft");
                foreach (var objKey in section.Keys)
                {
                    var obj = new Aircraft();
                    obj.Initialize(objKey.Value);
                    obj.RelativeX = obj.X - wp.X;
                    obj.RelativeY = obj.Y - wp.Y;
                    NonTileObjectList.AircraftList.Add(obj);
                }
            }

            //污染
            NonTileObjectList.SmudgeList = new List<Smudge>();
            if (mapFile.SectionExists("Smudge"))
            {
                var section = mapFile.GetSection("Smudge");
                foreach (var objKey in section.Keys)
                {
                    var obj = new Smudge();
                    obj.Initialize(objKey.Value);
                    obj.RelativeX = obj.X - wp.X;
                    obj.RelativeY = obj.Y - wp.Y;
                    NonTileObjectList.SmudgeList.Add(obj);
                }
            }

            //地形对象
            NonTileObjectList.TerrainList = new List<Terrain>();
            if (mapFile.SectionExists("Terrain"))
            {
                var section = mapFile.GetSection("Terrain");
                foreach (var objKey in section.Keys)
                {
                    var obj = new Terrain();
                    obj.Initialize(objKey);
                    obj.RelativeX = obj.X - wp.X;
                    obj.RelativeY = obj.Y - wp.Y;
                    NonTileObjectList.TerrainList.Add(obj);
                }
            }

            
            //单元标记
            NonTileObjectList.CellTagList = new List<CellTag>();
            if (mapFile.SectionExists("CellTags"))
            {
                var section = mapFile.GetSection("CellTags");
                foreach (var objKey in section.Keys)
                {
                    var obj = new CellTag();
                    obj.Initialize(objKey);
                    obj.RelativeX = obj.X - wp.X;
                    obj.RelativeY = obj.Y - wp.Y;
                    NonTileObjectList.CellTagList.Add(obj);
                }
            }

            //路径点
            NonTileObjectList.WaypointList = new List<Waypoint>();
            if (mapFile.SectionExists("Waypoints"))
            {
                var section = mapFile.GetSection("Waypoints");
                foreach (var objKey in section.Keys)
                {
                    var obj = new Waypoint();
                    obj.Initialize(objKey);
                    obj.RelativeX = obj.X - wp.X;
                    obj.RelativeY = obj.Y - wp.Y;
                    NonTileObjectList.WaypointList.Add(obj);
                }
            }

        }


        public void MergeNonTileObjects(string path, NonTileObjectList list, List<IsoTile> sourceIsoTileList)
        {
            var mapFile = new IniFile(path);
            var waypointSection = mapFile.GetSection("Waypoints");
            var wp = new Waypoint();

            foreach (var waypointLine in waypointSection.Keys)
            {
                if (int.Parse(waypointLine.Key) == Program.TargetWaypoint)
                {
                    wp.Initialize(waypointLine);
                }
            }

            foreach (var isoTile in sourceIsoTileList)
            {
                if (isoTile.Used)
                {
                    //建筑
                    if (Program.CopyStructure)
                    {
                        foreach (var obj in list.StructureList)
                        {
                            if (isoTile.Rx == obj.X && isoTile.Ry == obj.Y)
                            {
                                var obj2 = obj.Clone();
                                obj2.X = obj.RelativeX + wp.X;
                                obj2.Y = obj.RelativeY + wp.Y;
                                NonTileObjectList.StructureList.Add(obj2);
                            }
                        }
                    }

                    //单位
                    if (Program.CopyUnit)
                    {
                        foreach (var obj in list.UnitList)
                        {
                            if (isoTile.Rx == obj.X && isoTile.Ry == obj.Y)
                            {
                                var obj2 = obj.Clone();
                                obj2.X = obj.RelativeX + wp.X;
                                obj2.Y = obj.RelativeY + wp.Y;
                                NonTileObjectList.UnitList.Add(obj2);
                            }
                        }
                    }

                    //步兵
                    if (Program.CopyInfantry)
                    {
                        foreach (var obj in list.InfantryList)
                        {
                            if (isoTile.Rx == obj.X && isoTile.Ry == obj.Y)
                            {
                                var obj2 = obj.Clone();
                                obj2.X = obj.RelativeX + wp.X;
                                obj2.Y = obj.RelativeY + wp.Y;
                                NonTileObjectList.InfantryList.Add(obj2);
                            }
                        }
                    }

                    //飞行器
                    if (Program.CopyAircraft)
                    {
                        foreach (var obj in list.AircraftList)
                        {
                            if (isoTile.Rx == obj.X && isoTile.Ry == obj.Y)
                            {
                                var obj2 = obj.Clone();
                                obj2.X = obj.RelativeX + wp.X;
                                obj2.Y = obj.RelativeY + wp.Y;
                                NonTileObjectList.AircraftList.Add(obj2);
                            }
                        }
                    }

                    //污染
                    if (Program.CopySmudge)
                    {
                        foreach (var obj in list.SmudgeList)
                        {
                            if (isoTile.Rx == obj.X && isoTile.Ry == obj.Y)
                            {
                                var obj2 = obj.Clone();
                                obj2.X = obj.RelativeX + wp.X;
                                obj2.Y = obj.RelativeY + wp.Y;
                                NonTileObjectList.SmudgeList.Add(obj2);
                            }
                        }
                    }

                    //地形对象
                    if (Program.CopyTerrain)
                    {
                        foreach (var obj in list.TerrainList)
                        {
                            if (isoTile.Rx == obj.X && isoTile.Ry == obj.Y)
                            {
                                var obj2 = obj.Clone();
                                obj2.X = obj.RelativeX + wp.X;
                                obj2.Y = obj.RelativeY + wp.Y;
                                NonTileObjectList.TerrainList.Add(obj2);
                            }
                        }
                    }

                    //单元标记
                    if (Program.CopyCellTag)
                    {
                        foreach (var obj in list.CellTagList)
                        {
                            if (isoTile.Rx == obj.X && isoTile.Ry == obj.Y)
                            {
                                var obj2 = obj.Clone();
                                obj2.X = obj.RelativeX + wp.X;
                                obj2.Y = obj.RelativeY + wp.Y;
                                NonTileObjectList.CellTagList.Add(obj2);
                            }
                        }
                    }

                    //路径点
                    if (Program.CopyWaypoint)
                    {
                        foreach (var obj in list.WaypointList)
                        {
                            if (isoTile.Rx == obj.X && isoTile.Ry == obj.Y)
                            {
                                var obj2 = obj.Clone();
                                obj2.X = obj.RelativeX + wp.X;
                                obj2.Y = obj.RelativeY + wp.Y;
/*                                if (Program.CopyWaypointStrictly)
                                {*/
                                    bool same = false;
                                    foreach(var wp2 in NonTileObjectList.WaypointList)
                                    {
                                        if (wp2.Index == obj2.Index)
                                        {
                                            wp2.X = obj2.X;
                                            wp2.Y = obj2.Y;
                                            wp2.RelativeX = obj2.RelativeX;
                                            wp2.RelativeY = obj2.RelativeY;
                                            same = true;
                                        }
                                    }
                                    if (!same)
                                        NonTileObjectList.WaypointList.Add(obj2);
/*                                }
                                else
                                {
                                    int i = 0;
                                    bool avaIndex = true;
                                    while (!avaIndex)
                                    {
                                        avaIndex = true;
                                        foreach (var wp2 in NonTileObjectList.WaypointList)
                                        {
                                            if (wp2.Index == i)
                                                avaIndex = false;
                                        }
                                        if (!avaIndex)
                                            i++;
                                    }
                                    obj2.Index = i;
                                    NonTileObjectList.WaypointList.Add(obj2);
                                }*/
                                
                            }
                        }
                    }

                }
            }
        }

        public void SaveNonTileObjects(string path)
        {
            var mapFile = new IniFile(path);
            int range = Width + Height;

            //建筑
            if (Program.CopyStructure)
            {
                if (mapFile.SectionExists("Structures"))
                {
                    mapFile.RemoveSection("Structures");
                }
                mapFile.AddSection("Structures");
                int i = 0;
                foreach (var obj in NonTileObjectList.StructureList)
                {
                    if (obj.X < 0 || obj.Y < 0 || obj.X > range || obj.Y > range)
                        continue;
                    mapFile.SetStringValue("Structures", i.ToString(), obj.CreateINIValue());
                    i++;
                }
            }

            //单位
            if (Program.CopyUnit)
            {
                if (mapFile.SectionExists("Units"))
                {
                    mapFile.RemoveSection("Units");
                }
                mapFile.AddSection("Units");
                int i = 0;
                foreach (var obj in NonTileObjectList.UnitList)
                { 
                    if (obj.X < 0 || obj.Y < 0 || obj.X > range || obj.Y > range)
                        continue;
                    mapFile.SetStringValue("Units", i.ToString(), obj.CreateINIValue());
                    i++;
                }
            }

            //步兵
            if (Program.CopyInfantry)
            {
                if (mapFile.SectionExists("Infantry"))
                {
                    mapFile.RemoveSection("Infantry");
                }
                mapFile.AddSection("Infantry");
                int i = 0;
                foreach (var obj in NonTileObjectList.InfantryList)
                {
                    if (obj.X < 0 || obj.Y < 0 || obj.X > range || obj.Y > range)
                        continue;
                    mapFile.SetStringValue("Infantry", i.ToString(), obj.CreateINIValue());
                    i++;
                }
            }

            //飞行器
            if (Program.CopyAircraft)
            {
                if (mapFile.SectionExists("Aircraft"))
                {
                    mapFile.RemoveSection("Aircraft");
                }
                mapFile.AddSection("Aircraft");
                int i = 0;
                foreach (var obj in NonTileObjectList.AircraftList)
                {
                    if (obj.X < 0 || obj.Y < 0 || obj.X > range || obj.Y > range)
                        continue;
                    mapFile.SetStringValue("Aircraft", i.ToString(), obj.CreateINIValue());
                    i++;
                }
            }

            //污染
            if (Program.CopySmudge)
            {
                if (mapFile.SectionExists("Smudge"))
                {
                    mapFile.RemoveSection("Smudge");
                }
                mapFile.AddSection("Smudge");
                int i = 0;
                foreach (var obj in NonTileObjectList.SmudgeList)
                {
                    if (obj.X < 0 || obj.Y < 0 || obj.X > range || obj.Y > range)
                        continue;
                    mapFile.SetStringValue("Smudge", i.ToString(), obj.CreateINIValue());
                    i++;
                }
            }

            //地形对象
            if (Program.CopyTerrain)
            {
                if (mapFile.SectionExists("Terrain"))
                {
                    mapFile.RemoveSection("Terrain");
                }
                mapFile.AddSection("Terrain");
                foreach (var obj in NonTileObjectList.TerrainList)
                {
                    if (obj.X < 0 || obj.Y < 0 || obj.X > range || obj.Y > range)
                        continue;
                    var key = obj.CreateINILine().Key;
                    var value = obj.CreateINILine().Value;
                    mapFile.SetStringValue("Terrain", key, value);
                }
            }

            //单元标记
            if (Program.CopyCellTag)
            {
                if (mapFile.SectionExists("CellTags"))
                {
                    mapFile.RemoveSection("CellTags");
                }
                mapFile.AddSection("CellTags");
                foreach (var obj in NonTileObjectList.CellTagList)
                {
                    if (obj.X < 0 || obj.Y < 0 || obj.X > range || obj.Y > range)
                        continue;
                    var key = obj.CreateINILine().Key;
                    var value = obj.CreateINILine().Value;
                    mapFile.SetStringValue("CellTags", key, value);
                }
            }

            //路径点
            if (Program.CopyWaypoint)
            {
                if (mapFile.SectionExists("Waypoints"))
                {
                    mapFile.RemoveSection("Waypoints");
                }
                mapFile.AddSection("Waypoints");
                foreach (var obj in NonTileObjectList.WaypointList)
                {
                    if (obj.X < 0 || obj.Y < 0 || obj.X > range || obj.Y > range)
                        continue;
                    var key = obj.CreateINILine().Key;
                    var value = obj.CreateINILine().Value;
                    mapFile.SetStringValue("Waypoints", key, value);
                }
            }

            mapFile.WriteIniFile();

        }
    }
}
