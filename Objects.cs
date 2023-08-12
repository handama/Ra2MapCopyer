using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RA2MapCopyer
{
    public class Waypoint
    {
        public int RelativeX;
        public int X;
        public int RelativeY;
        public int Y;
        public int Index;

        public void Initialize(KeyValuePair<string, string> iniLine)
        {
            string value = iniLine.Value;
            int length = value.Length;
            string x = value.Substring(value.Length - 3, 3);
            string y = value.Substring(0, value.Length - 3);
            Index = int.Parse(iniLine.Key);
            X = int.Parse(x);
            Y = int.Parse(y);
            //RelativeX = int.Parse(x) - WorkingMap.StartingX;
            //RelativeY = int.Parse(y) - WorkingMap.StartingY;
        }

        public Waypoint Clone()
        {
            return (Waypoint)this.MemberwiseClone();
        }
        public KeyValuePair<string, string> CreateINILine()
        {
            string value = Y.ToString() + string.Format("{0:000}", X);
            var iniLine = new KeyValuePair<string, string>(Index.ToString(), value);
            return iniLine;
        }
    }

    public class Line
    {
        public Waypoint waypoint1;
        public Waypoint waypoint2;
        public bool vertical;
        public float k;
        public float b;

    }
    public class Structure
    {
        public string Owner;
        public string Name;
        public int Strength;
        public int RelativeX;
        public int X;
        public int RelativeY;
        public int Y;
        public int Direction;
        public string Tag;
        public int Sellable;
        public int Rebuild;
        public int EnergySupport;
        public int UpgradeCount;
        public int SpotLight;
        public string Upgrade1;
        public string Upgrade2;
        public string Upgrade3;
        public int AIRepairs;
        public int ShowName;

        public void Initialize(string iniValue)
        {
            string[] values = iniValue.Split(',');
            if (values.Count() == 17)
            {
                Owner = values[0];
                Name = values[1];
                Strength = int.Parse(values[2]);
                X = int.Parse(values[3]);
                Y = int.Parse(values[4]);
                Direction = int.Parse(values[5]);
                Tag = values[6];
                Sellable = int.Parse(values[7]);
                Rebuild = int.Parse(values[8]);
                EnergySupport = int.Parse(values[9]);
                UpgradeCount = int.Parse(values[10]);
                SpotLight = int.Parse(values[11]);
                Upgrade1 = values[12];
                Upgrade2 = values[13];
                Upgrade3 = values[14];
                AIRepairs = int.Parse(values[15]);
                ShowName = int.Parse(values[16]);
            }

        }
        public Structure Clone()
        {
            return (Structure)this.MemberwiseClone();
        }
        public string CreateINIValue()
        {
            return Owner + "," + Name + "," + Strength + "," + X + "," + Y + "," + Direction + "," + Tag + "," + Sellable
                + "," + Rebuild + "," + EnergySupport + "," + UpgradeCount + "," + SpotLight + "," + Upgrade1 + "," + Upgrade2
                + "," + Upgrade3 + "," + AIRepairs + "," + ShowName;
        }
    }

    public class Infantry
    {
        public string Owner;
        public string Name;
        public int Strength;
        public int RelativeX;
        public int X;
        public int RelativeY;
        public int Y;
        public int Unknown;
        public int Direction;
        public string State;
        public string Tag;
        public int Veteran;
        public int Group;
        public int OnBridge;
        public int AutocreateNoRecruitable;
        public int AutocreateYesRecruitable;

        public void Initialize(string iniValue)
        {
            string[] values = iniValue.Split(',');
            if (values.Count() == 14)
            {
                Owner = values[0];
                Name = values[1];
                Strength = int.Parse(values[2]);
                X = int.Parse(values[3]);
                Y = int.Parse(values[4]);
                Unknown = int.Parse(values[5]);
                State = values[6];
                Direction = int.Parse(values[7]);
                Tag = values[8];
                Veteran = int.Parse(values[9]);
                Group = int.Parse(values[10]);
                OnBridge = int.Parse(values[11]);
                AutocreateNoRecruitable = int.Parse(values[12]);
                AutocreateYesRecruitable = int.Parse(values[13]);
            }
        }
        public Infantry Clone()
        {
            return (Infantry)this.MemberwiseClone();
        }
        public string CreateINIValue()
        {
            return Owner + "," + Name + "," + Strength + "," + X + "," + Y + "," + Unknown + "," + State + "," + Direction
                + "," + Tag + "," + Veteran + "," + Group + "," + OnBridge + "," + AutocreateNoRecruitable + "," + AutocreateYesRecruitable;
        }
    }

    public class Unit
    {
        public string Owner;
        public string Name;
        public int Strength;
        public int RelativeX;
        public int X;
        public int RelativeY;
        public int Y;
        public int Direction;
        public string State;
        public string Tag;
        public int Veteran;
        public int Group;
        public int OnBridge;
        public int FollowerID;
        public int AutocreateNoRecruitable;
        public int AutocreateYesRecruitable;

        public void Initialize(string iniValue)
        {
            string[] values = iniValue.Split(',');
            if (values.Count() == 14)
            {
                Owner = values[0];
                Name = values[1];
                Strength = int.Parse(values[2]);
                X = int.Parse(values[3]);
                Y = int.Parse(values[4]);
                Direction = int.Parse(values[5]);
                State = values[6];
                Tag = values[7];
                Veteran = int.Parse(values[8]);
                Group = int.Parse(values[9]);
                OnBridge = int.Parse(values[10]);
                FollowerID = int.Parse(values[11]);
                AutocreateNoRecruitable = int.Parse(values[12]);
                AutocreateYesRecruitable = int.Parse(values[13]);
            }
        }
        public Unit Clone()
        {
            return (Unit)this.MemberwiseClone();
        }
        public string CreateINIValue()
        {
            return Owner + "," + Name + "," + Strength + "," + X + "," + Y + "," + Direction + "," + State + "," + Tag
                + "," + Veteran + "," + Group + "," + OnBridge + "," + FollowerID + "," + AutocreateNoRecruitable + "," + AutocreateYesRecruitable;
        }
    }

    public class Aircraft
    {
        public string Owner;
        public string Name;
        public int Strength;
        public int RelativeX;
        public int X;
        public int RelativeY;
        public int Y;
        public int Direction;
        public string State;
        public string Tag;
        public int Veteran;
        public int Group;
        public int AutocreateNoRecruitable;
        public int AutocreateYesRecruitable;

        public void Initialize(string iniValue)
        {
            string[] values = iniValue.Split(',');
            if (values.Count() == 12)
            {
                Owner = values[0];
                Name = values[1];
                Strength = int.Parse(values[2]);
                X = int.Parse(values[3]);
                Y = int.Parse(values[4]);
                Direction = int.Parse(values[5]);
                State = values[6];
                Tag = values[7];
                Veteran = int.Parse(values[8]);
                Group = int.Parse(values[9]);
                AutocreateNoRecruitable = int.Parse(values[10]);
                AutocreateYesRecruitable = int.Parse(values[11]);
            }
        }
        public Aircraft Clone()
        {
            return (Aircraft)this.MemberwiseClone();
        }
        public string CreateINIValue()
        {
            return Owner + "," + Name + "," + Strength + "," + X + "," + Y + "," + Direction + "," + State + "," + Tag
                + "," + Veteran + "," + Group + "," + AutocreateNoRecruitable + "," + AutocreateYesRecruitable;
        }
    }

    public class Smudge
    {
        public string Name;
        public int RelativeX;
        public int X;
        public int RelativeY;
        public int Y;
        public int unknown;

        public void Initialize(string iniValue)
        {
            string[] values = iniValue.Split(',');
            if (values.Count() == 4)
            {
                Name = values[0];
                X = int.Parse(values[1]);
                Y = int.Parse(values[2]);
                unknown = int.Parse(values[3]);
            }
        }
        public Smudge Clone()
        {
            return (Smudge)this.MemberwiseClone();
        }
        public string CreateINIValue()
        {
            return Name + "," + X + "," + Y + "," + unknown;
        }
    }

    public class Terrain
    {
        public int RelativeX;
        public int X;
        public int RelativeY;
        public int Y;
        public string Name;

        public void Initialize(KeyValuePair<string, string> iniLine)
        {
            string key = iniLine.Key;
            int length = key.Length;
            string x = key.Substring(key.Length - 3, 3);
            string y = key.Substring(0, key.Length - 3);
            Name = iniLine.Value;
            X = int.Parse(x);
            Y = int.Parse(y);
        }
        public Terrain Clone()
        {
            return (Terrain)this.MemberwiseClone();
        }
        public KeyValuePair<string, string> CreateINILine()
        {
            string key = Y.ToString() + string.Format("{0:000}", X);
            var iniLine = new KeyValuePair<string, string>(key, Name);
            return iniLine;
        }
    }

    public class CellTag
    {
        public int RelativeX;
        public int X;
        public int RelativeY;
        public int Y;
        public string Tag;

        public void Initialize(KeyValuePair<string, string> iniLine)
        {
            string key = iniLine.Key;
            int length = key.Length;
            string x = key.Substring(key.Length - 3, 3);
            string y = key.Substring(0, key.Length - 3);
            Tag = iniLine.Value;
            X = int.Parse(x);
            Y = int.Parse(y);
        }
        public CellTag Clone()
        {
            return (CellTag)this.MemberwiseClone();
        }
        public KeyValuePair<string, string> CreateINILine()
        {
            string key = Y.ToString() + string.Format("{0:000}", X);
            var iniLine = new KeyValuePair<string, string>(key, Tag);
            return iniLine;
        }
    }


}
