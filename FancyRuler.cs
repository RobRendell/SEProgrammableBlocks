using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRageMath;

namespace SEProgrammableBlocks {
    public class Program : MyGridProgram {
        /*
        Blargmode's fancy ruler. Version 2.1 (2018-01-28) + hacks (2019-05-06)
        Measure the distance between your camera and some asteroid or whatnot.
        
        
        /// WHAT YOU NEED TO BE A RULER
        Camera with the tag #Ruler in the name.
        Tag(s) on suitable LCDs. (#Ruler tag to show stuff, add #Fancy to do it in style!)
        Recompile! 
        
        
        /// HOW TO RULE
        Run with argument "scan" to, well, scan. Duh.
        Hint; map to toolbar in your ship!
        
        
        /// ADVANCED RULING
        Add the tag #Ruler to a Programmable block, a Timer block, and/or a Sound block to
        trigger them with a scan.  The GPS coordinates are sent to the Programmable blocks as
        an argument.
        
        Run with argument "scan #" where # is a range in meters, ignoring the setting.
        
        Run with argument "range #" where # is a range in meters to change the setting.
        
        Run with argument "project #" (where # is a range in meters) to create a scan point
        which is at the given range straight forward from your camera(s).  The scan data does
        not contain any information about what grid or voxels might be at that point, but is
        useful if you want to translate a HUD marking to a GPS point.
        
        
        
         
        
        Change the text in the second quote of each line below if you're adventurous!
        */
        
        List<string[]> CUSTOM_TYPE_NAME = new List<string[]>() {
            new string[] {"CharacterHuman", "Human"},
            new string[] {"CharacterOther", "Beast"},
            new string[] {"LargeGrid", "Large ship"},
            new string[] {"SmallGrid", "Small ship"}
        };


        class DistanceInfo {
            private Program P;
            private MyDetectedEntityInfo Info;
            public IMyCameraBlock Camera;
            private IMyShipController Controller;
            public string Distance { get; private set; }
            public string Type { get; private set; }
            public string Relationship { get; private set; }
            public string TimeToTarget { get; private set; }
            public string TimeToTargetTopSpeed { get; private set; }
            public string GPS { get; private set; }
            private long LastTickRun = 0;

            public DistanceInfo(IMyCameraBlock camera, long range, IMyShipController controller, Program p) {
                P = p;
                Camera = camera;
                Info = Camera.Raycast((range <= 0) ? Camera.AvailableScanRange : range, 0, 0);
                Controller = controller;
                UpdateTargetInfo();
                UpdateLocation();
            }

            public DistanceInfo(Vector3 direction, IMyCameraBlock camera, long range, IMyShipController controller, Program p) {
                P = p;
                Camera = camera;
                Controller = controller;
                var position = camera.GetPosition() + direction * range;
                Info = new MyDetectedEntityInfo(0, "Projection", MyDetectedEntityType.Unknown, position, MatrixD.Identity, Vector3.Zero,
                    MyRelationsBetweenPlayerAndBlock.NoOwnership, new BoundingBoxD(), LastTickRun);
                UpdateTargetInfo();
                UpdateLocation();
            }

            private void UpdateTargetInfo() {
                Type = Info.Type.ToString();
                if (Type == "None") {
                    Type = String.Empty;
                    Relationship = String.Empty;
                } else {
                    Relationship = Info.Relationship.ToString();
                }
            }

            private void UpdateLocation() {
                if (Info.HitPosition.HasValue) {
                    var x = (Info.HitPosition.Value.X.ToString("0.00"));
                    var y = (Info.HitPosition.Value.Y.ToString("0.00"));
                    var z = (Info.HitPosition.Value.Z.ToString("0.00"));
                    var name = Info.Relationship == MyRelationsBetweenPlayerAndBlock.NoOwnership ? Info.Name : Info.Relationship + " " + Info.Name;
                    GPS = string.Format(":GPS:{0}:{1}:{2}:{3}:", name + " " + DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss"), x, y, z);
                }
            }

            public string GetDistance(long currentTick) {
                if (currentTick != LastTickRun) {
                    LastTickRun = currentTick;
                    if (Info.HitPosition.HasValue) {
                        var distance = Vector3D.Distance(Camera.GetPosition(), Info.HitPosition.Value);
                        Distance = General.AddPrefixKm(distance);
                        double topSpeed = (double) P.Settings[ID.TopSpeed].Value;
                        if (topSpeed < 1) topSpeed = 100.0;
                        TimeSpan t = TimeSpan.FromSeconds(distance / topSpeed);
                        TimeToTargetTopSpeed = FormatTime(t);
                        if (Controller != null) {
                            if (Controller.GetShipSpeed() < 1) TimeToTarget = "--";
                            else {
                                t = TimeSpan.FromSeconds(distance / Controller.GetShipSpeed());
                                TimeToTarget = FormatTime(t);
                            }
                        } else {
                            TimeToTarget = FormatTime(t);
                        }
                    }
                }
                return Distance;
            }

            private string FormatTime(TimeSpan t) {
                if (t.Hours > 0) return string.Format("{0:D1}h:{1:D1}m:{2:D1}s", t.Hours, t.Minutes, t.Seconds);
                if (t.Minutes > 0) return string.Format("{0:D1}m:{1:D1}s", t.Minutes, t.Seconds);
                return TimeToTarget = string.Format("{0:D1}s", t.Seconds);
            }
        }

        public class ExecutionTime {
            Program P;
            const int Size = 60;
            int[] Count;
            int Index;
            DateTime StartTime;
            double[] Times;

            public ExecutionTime(Program p) {
                P = p;
                Count = new int[Size];
                Times = new double[Size];
            }

            public void Start() {
                StartTime = DateTime.Now;
            }

            public void End() {
                if (Index >= Size) Index = 0;
                Count[Index] = P.Runtime.CurrentInstructionCount;
                Times[Index] = (DateTime.Now - StartTime).TotalMilliseconds;
                Index++;
            }

            public double GetAvrage() {
                return Count.Average();
            }

            public int GetPeak() {
                return Count.Max();
            }

            public double GetAvrageTime() {
                return Times.Average();
            }

            public double GetPeakTime() {
                return Times.Max();
            }
        }

        class General {
            public static bool IsLocal(IMyTerminalBlock block, IMyTerminalBlock me) {
                return (block.CubeGrid == me.CubeGrid);
            }

            public static List<T> InitBlocks<T>(IMyGridTerminalSystem gts, IMyTerminalBlock me) where T : class, IMyTerminalBlock {
                var blocks = new List<T>();
                gts.GetBlocksOfType(blocks, x => IsLocal(x, me));
                return blocks;
            }

            public static List<T> InitBlocks<T>(IMyGridTerminalSystem gts) where T : class, IMyTerminalBlock {
                var blocks = new List<T>();
                gts.GetBlocksOfType(blocks);
                return blocks;
            }

            public static bool ContainsExact(string match, string text) {
                return System.Text.RegularExpressions.Regex.IsMatch(text, @"(^|\s)" + match + @"(\s|$)");
            }

            public static string ExtractNumber(string text) {
                return System.Text.RegularExpressions.Regex.Match(text, @"\d+").Value;
            }

            public static string ToPercent(double part, double whole, int decimals = 1) {
                double result = (part / whole) * 100;
                return result.ToString("n" + decimals) + "%";
            }

            public static bool DoesBlockExist(IMyTerminalBlock block) {
                return block.CubeGrid?.GetCubeBlock(block.Position)?.FatBlock == block;
            }

            public static string AddPrefixKm(double val, bool addSpace = false) {
                string spacer = addSpace ? " " : "";
                if (val < 1000) return val.ToString("0.##") + spacer + "m";
                if (val < 1000000) return (val / 1000).ToString("0.##") + spacer + "km";
                if (val < 1000000000) return (val / 1000000).ToString("0.##") + spacer + "Mm";
                return (val / 1000000000).ToString("0.##") + spacer + "Gm";
            }

            public static string Dots(int n, char c = '.') {
                return new String(c, n);
            }
        }

        public class FixedWidthText {
            private List<string> Text;
            public int Width { get; private set; }

            public FixedWidthText(int width) {
                Text = new List<string>();
                Width = width;
            }

            public void Clear() {
                Text.Clear();
            }

            public void Append(string t) {
                Text[Text.Count - 1] += t;
            }

            public void AppendLine() {
                Text.Add("");
            }

            public void AppendLine(string t) {
                Text.Add(t);
            }

            public string GetText() {
                return GetText(Width);
            }

            public string GetText(int lineWidth, string padding = "") {
                string finalText = "";
                foreach (var line in Text) {
                    string rest = padding + line;
                    if (line.StartsWith("&L")) {
                        rest = new string(line.ToCharArray()[2], lineWidth);
                    } else if (rest.Length > lineWidth) {
                        while (rest.Length > lineWidth) {
                            string part = rest.Substring(0, lineWidth);
                            rest = rest.Substring(lineWidth);
                            for (int i = part.Length - 1; i > 0; i--) {
                                if (part[i] == ' ') {
                                    finalText += part.Substring(0, i) + "\n";
                                    rest = part.Substring(i + 1) + rest;
                                    break;
                                }
                            }
                        }
                    }
                    finalText += rest + "\n";
                }
                return finalText;
            }
        }

/*This is mostly/https:Edited by Blargmode.Changes:Graphics object now takes a list of panels instead of one.The number 7 visually changed.Adjusted the print function so that x and y represents the top left corner of it.Added text aligment to printEverything about colors ChangedDrawing changed. It will now skip all background pixels after the last forground pixel resulting in a huge reduction in number of pixels sent to other clients.This however forces you to use a few very specific background colors with the default monospace font. It worksgreat with my own font "DotMatrix"(it's on the workshop)which has transparent pixels.https:Methods:SetForeground-Set a named color(need expansion of colors, there's more avalible)SetPreviousForeground-Revert to the last used foreground colorSetBackground-Set a named color(need expansion of colors, there's more avalible)ProgressBar-Draw a progress barFire-Silly fire-ish effect on progress bars. Looks stupid on thick bars.ProgressLine-A slider, similar to progres bar, but it can go past 100%and shows that with an arrow.Map-A super useful method from Arduino to take any range and squeeze it into another range. E.g. for getting how many pixels 25%would be on an x pixels long progress bar.Added public variablesColors*/
        class Graphics {
            public List<IMyTextPanel> Panels;
            private string[] Screen;
            private string[] ClearScreen;
            private string[] ScreenLines;
            private int[] IndexOfLastPixel;
            public int Width { get; private set; }
            public int Height { get; private set; }
            private string[] Foreground = {"\uE2FF", "\uE2FF"};
            private string Background = "\uE100";
            public string BlankPixel = "\uE079";
            Action<string> Echo;
            private Random Rand;
            private const int Offset = 0x21;

            private short[] Glyphs = {
                9346, 23040, 24445, 15602, 17057, 10923, 9216, 5265, 17556, 21824, 1488, 20, 448, 2, 672, 31599, 11415, 25255, 29326, 23497, 31118, 10666,
                29330, 10922, 10954, 1040, 1044, 5393, 3640, 17492, 25218, 15203, 11245, 27566, 14627, 27502, 31143, 31140, 14827, 23533, 29847, 12906, 23469,
                18727, 24557, 27501, 11114, 27556, 11131, 27565, 14478, 29842, 23403, 23378, 23549, 23213, 23186, 29351, 13459, 2184, 25750, 10752, 7, 17408,
                239, 18862, 227, 4843, 1395, 14756, 1886, 18861, 8595, 4302, 18805, 25745, 509, 429, 170, 1396, 1369, 228, 1934, 18851, 363, 362, 383, 341,
                2766, 3671, 5521, 9234, 17620, 1920
            };

            private short GetGlyph(char code) {
                return Glyphs[code - Offset];
            }

            public Graphics(int width, int height, List<IMyTextPanel> panels, Action<string> echo) {
                Width = width;
                Height = height;
                Screen = new string[Width * Height];
                ClearScreen = new string[Width * Height];
                ScreenLines = new string[Width * Height + Height - 1];
                IndexOfLastPixel = new int[Height];
                Panels = panels;
                Echo = echo;
                SetBackground(Background, true);
                Rand = new Random();
                Clear();
            }

            public Graphics(int width, int height, IMyTextPanel panel, Action<string> echo) {
                Width = width;
                Height = height;
                Screen = new string[Width * Height];
                ClearScreen = new string[Width * Height];
                ScreenLines = new string[Width * Height + Height - 1];
                IndexOfLastPixel = new int[Height];
                Panels = new List<IMyTextPanel>();
                Panels.Add(panel);
                Echo = echo;
                SetBackground(Background, true);
                Rand = new Random();
                Clear();
            }

            public void Pixel(int x, int y) {
                if (Within(x, 0, Width) && Within(y, 0, Height)) {
                    Screen[y * Width + x] = Foreground[0];
                    if (Foreground[0] != "\uE100") {
                        if (x > IndexOfLastPixel[y]) IndexOfLastPixel[y] = x + 1;
                    }
                }
            }

            public void Draw() {
                for (int i = 0; i < Height; i++) {
                    ScreenLines[i] = string.Join(null, Screen, i * Width, IndexOfLastPixel[i]) + "\n";
                }
                string combinedFrame = string.Concat(ScreenLines);
                foreach (var panel in Panels) {
                    panel.WriteText(combinedFrame);
                }
            }

            public void Line(int x0, int y0, int x1, int y1) {
                if (x0 == x1) {
                    int high = Math.Max(y1, y0);
                    for (int y = Math.Min(y1, y0); y <= high; y++) {
                        Pixel(x0, y);
                    }
                } else if (y0 == y1) {
                    int high = Math.Max(x1, x0);
                    for (int x = Math.Min(x1, x0); x <= high; x++) {
                        Pixel(x, y0);
                    }
                } else {
                    bool yLonger = false;
                    int incrementVal, endVal;
                    int shortLen = y1 - y0;
                    int longLen = x1 - x0;
                    if (Math.Abs(shortLen) > Math.Abs(longLen)) {
                        int swap = shortLen;
                        shortLen = longLen;
                        longLen = swap;
                        yLonger = true;
                    }
                    endVal = longLen;
                    if (longLen < 0) {
                        incrementVal = -1;
                        longLen = -longLen;
                    } else incrementVal = 1;
                    int decInc;
                    if (longLen == 0) decInc = 0;
                    else decInc = (shortLen << 16) / longLen;
                    int j = 0;
                    if (yLonger) {
                        for (int i = 0; i - incrementVal != endVal; i += incrementVal) {
                            Pixel(x0 + (j >> 16), y0 + i);
                            j += decInc;
                        }
                    } else {
                        for (int i = 0; i - incrementVal != endVal; i += incrementVal) {
                            Pixel(x0 + i, y0 + (j >> 16));
                            j += decInc;
                        }
                    }
                }
            }

            public void Rect(int x, int y, int w, int h, bool fill = false) {
                if (!fill) {
                    Line(x, y, x, y + h - 1);
                    Line(x, y, x + w - 1, y);
                    Line(x + w - 1, y, x + w - 1, y + h - 1);
                    Line(x, y + h - 1, x + w - 1, y + h - 1);
                } else {
                    for (int xi = x; xi < x + w; xi++) {
                        for (int yi = y; yi < y + h; yi++) {
                            Pixel(xi, yi);
                        }
                    }
                }
            }

            public void Print(int x, int y, string text, Align align = Align.Left) {
                y += 4;
                if (align == Align.Right) x -= text.Length * 4 - 1;
                if (align == Align.Center) x -= (int) (text.Length * 4 - 1) / 2;
                int x1 = x;
                int y1 = y;
                for (int i = 0; i < text.Length; i++) {
                    switch (text[i]) {
                        case '\n':
                            y1 += 6;
                            x1 = x;
                            break;
                        case ' ':
                            x1 += 4;
                            break;
                        default:
                            short glyph = GetGlyph(text[i]);
                            int j = 14;
                            do {
                                if ((glyph & 1) != 0) {
                                    Pixel(x1 + j % 3, y1 - 4 + j / 3);
                                }
                                glyph >>= 1;
                                j--;
                            } while (glyph > 0);
                            x1 += 4;
                            break;
                    }
                }
            }

            public void ProgressBar(int x, int y, int w, int h, double percentage, bool inverted = false, bool fill = true, bool enableFire = false) {
                bool vertical = false;
                int longest;
                if (w > h) {
                    longest = w;
                } else {
                    longest = h;
                    vertical = true;
                }
                if (double.IsNaN(percentage)) percentage = 0;
                int barSize = Map(Math.Abs((int) percentage), 0, 100, 0, longest);
                if (percentage < 0) {
                    if (inverted == false) inverted = true;
                    else if (inverted == true) inverted = false;
                }
                if (barSize > longest) barSize = longest;
                if (barSize == 0 && percentage > 0.05 && longest >= 10) barSize = 1;
                if (vertical) {
                    if (!inverted) {
                        Rect(x, y + (h - barSize), w, barSize, true);
                        if (fill) {
                            SetForeground(Color.Gray);
                            Rect(x, y, w, h - barSize, true);
                            if (enableFire && percentage >= 100) Fire(x, y, w, h, true);
                            SetPreviousForeground();
                        }
                    } else {
                        Rect(x, y, w, barSize, true);
                        if (fill) {
                            SetForeground(Color.Gray);
                            Rect(x, y + barSize, w, h - barSize, true);
                            if (enableFire && percentage >= 100) Fire(x, y, w, h, false);
                            SetPreviousForeground();
                        }
                    }
                } else {
                    if (inverted) {
                        Rect(x + (w - barSize), y, barSize, h, true);
                        if (fill) {
                            SetForeground(Color.Gray);
                            Rect(x, y, w - barSize, h, true);
                            if (enableFire && percentage >= 100) Fire(x, y, w, h, true);
                            SetPreviousForeground();
                        }
                    } else {
                        Rect(x, y, barSize, h, true);
                        if (fill) {
                            SetForeground(Color.Gray);
                            Rect(x + barSize, y, w - barSize, h, true);
                            if (enableFire && percentage >= 100) Fire(x, y, w, h, false);
                            SetPreviousForeground();
                        }
                    }
                }
            }

            private void Fire(int x, int y, int w, int h, bool inverted = false) {
                int start = (int) ((w / 3) * 2);
                int color = Rand.Next(0, 10);
                if (color == 0) SetForeground(Color.Yellow, false);
                else if (color == 1) SetForeground(Color.Blue, false);
                else SetForeground(Color.Red, false);
                if (inverted) {
                    Line(x, y, x + Rand.Next(0, w - start), y);
                    Line(x, y + 1, x + Rand.Next(0, w - start), y + 1);
                    Line(x, y + 2, x + Rand.Next(0, w - start), y + 2);
                } else {
                    Line(x + Rand.Next(start, w), y, x + w - 1, y);
                    Line(x + Rand.Next(start, w), y + 1, x + w - 1, y + 1);
                    Line(x + Rand.Next(start, w), y + 2, x + w - 1, y + 2);
                }
            }

            public void ProgressLine(int x, int y, int w, int h, double percentage, bool inverted = false, bool fill = false) {
                bool vertical = false;
                int longest;
                int shortest;
                if (w > h) {
                    longest = w;
                    shortest = h;
                } else {
                    longest = h;
                    shortest = w;
                    vertical = true;
                }
                int barSize = Map((int) percentage, 0, 100, 0, longest);
                if (barSize > longest) barSize = longest;
                if (fill) {
                    SetForeground(Color.Gray);
                    Rect(x, y, w, h, true);
                    SetPreviousForeground();
                }
                if (vertical) {
                    if (inverted) {
                        if (percentage < -0.01) {
                            Arrow(x, y, w, 1);
                        } else if (percentage > 100.01) {
                            Arrow(x, y + h, w, 3);
                        } else {
                            Line(x, y + barSize, x + w - 1, y + barSize);
                        }
                    } else {
                        int y1 = y + (h - barSize);
                        if (percentage > 100.01) {
                            Arrow(x, y, w, 1);
                        } else if (percentage < -0.01) {
                            Arrow(x, y + h, w, 3);
                        } else {
                            Line(x, y1, x + w - 1, y1);
                        }
                    }
                } else {
                    if (inverted) {
                        int x1 = x + (w - barSize);
                        if (percentage < -0.01) {
                            Arrow(x + w, y, h, 2);
                        } else if (percentage > 100.01) {
                            Arrow(x, y, h, 4);
                        } else {
                            Line(x1, y, x1, y + h - 1);
                        }
                    } else {
                        if (percentage > 100.01) {
                            Arrow(x + w, y, h, 2);
                        } else if (percentage < -0.01) {
                            Arrow(x, y, h, 4);
                        } else {
                            Line(x + barSize, y, x + barSize, y + h - 1);
                        }
                    }
                }
            }

            private void Arrow(int x, int y, int s, int dir) {
                int even = (s % 2 == 0) ? 1 : 0;
                int half = (s - 1) / 2;
                switch (dir) {
                    case 1:
                        y--;
                        Line(x, y, x + half, y - half);
                        Line(x + half + even, y - half, x + s - 1, y);
                        break;
                    case 2:
                        Line(x, y, x + half, y + half);
                        Line(x + half, y + even + half, x, y + s - 1);
                        break;
                    case 3:
                        Line(x, y, x + half, y + half);
                        Line(x + half + even, y + half, x + s - 1, y);
                        break;
                    case 4:
                        x--;
                        Line(x, y, x - half, y + half);
                        Line(x - half, y + even + half, x, y + s - 1);
                        break;
                }
            }

            public void Clear() {
                Screen = (string[]) ClearScreen.Clone();
            }

            public void SetForeground(string color, bool log = true) {
                if (log) Foreground[1] = Foreground[0];
                Foreground[0] = color;
            }

            public void SetForeground(Color color, bool log = true) {
                if (log) Foreground[1] = Foreground[0];
                Foreground[0] = GetColorString(color);
            }

            public void SetPreviousForeground() {
                Foreground[0] = Foreground[1];
            }

            public void SetBackground(string color, bool forceUpdate = false) {
                if (Background != color || forceUpdate) {
                    Background = color;
                    for (int i = 0; i < ClearScreen.Length; i++) ClearScreen[i] = Background;
                }
            }

            public void SetBackground(Color color, bool forceUpdate = false) {
                string stringColor = GetColorString(color);
                if (Background != stringColor || forceUpdate) {
                    Background = stringColor;
                    for (int i = 0; i < ClearScreen.Length; i++) ClearScreen[i] = Background;
                }
            }

            public bool Within(double val, double min, double max) {
                if (val < max && val >= min) return true;
                return false;
            }

            private int Map(int x, int in_min, int in_max, int out_min, int out_max) {
                return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
            }

            private char GetColorChar(byte r, byte g, byte b) {
                return (char) (0xe100 + (r << 6) + (g << 3) + b);
            }

            private string GetColorString(Color color) {
                byte r = (byte) ((color.R) / 32);
                byte g = (byte) ((color.G) / 32);
                byte b = (byte) ((color.B) / 32);
                return ((char) ((0xe100 + (r << 6) + (g << 3) + b))).ToString();
            }

            public static void GetPanelDimentions(string subtypeId, out int width, out int height) {
                subtypeId = subtypeId.ToLower();
                if (subtypeId.Contains("wide")) {
                    width = 118;
                    height = 59;
                } else if (subtypeId.Contains("corner")) {
                    width = 59;
                    height = 10;
                } else width = 59;
                height = 59;
            }

            public static PanelType GetPanelType(string subtypeId) {
                subtypeId = subtypeId.ToLower();
                if (subtypeId.Contains("wide")) return PanelType.Wide;
                else if (subtypeId.Contains("corner")) return PanelType.Corner;
                else return PanelType.Normal;
            }
        }

        public enum PanelType {
            Normal,
            Wide,
            Corner
        };

        public enum Align {
            Left,
            Center,
            Right
        };

        long TickID = 0;
        public const string ScriptName = "Blarg's Fancy Ruler";
        bool Initialized = false;
        Dictionary<string, int> FontSize = new Dictionary<string, int>() {{"Debug", 42}, {"Monospace", 26}, {"DotMatrix", 27}};
        public Dictionary<ID, Setting> Settings;
        public FixedWidthText DetailedInfo;
        public string DetailedInfoStandardText;
        private FixedWidthText CustomData;
        private FixedWidthText SettingsProblems;
        bool ShowSettingsProblems = false;
        private ExecutionTime ExeTime;
        int DotDotDot = 0;
        string Dots = "";
        private List<IMyCameraBlock> Cameras;
        private List<IMyTerminalBlock> TriggerBlocks;
        public IMyShipController Controller;
        private DistanceInfo Info;
        private string CurrentCamera = "";
        private string Range = "";
        Graphics GNormal;
        List<IMyTextPanel> SlimTextPanels;
        List<IMyTextPanel> TextPanels;
        public Problems Problem = new Problems();

        public class Problems {
            public bool NoCamera = false;
            public bool NoLCD = false;
            public bool NoController = false;
            public bool RangeError = false;
            public string InvalidCommand = "";
        }

        public Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            ExeTime = new ExecutionTime(this);
            DetailedInfo = new FixedWidthText(40);
            DetailedInfo.AppendLine(ScriptName + "{0}");
            DetailedInfo.AppendLine();
            DetailedInfo.AppendLine(Strings.SettingsInCustomData);
            DetailedInfoStandardText = DetailedInfo.GetText();
        }

        public void Save() {
        }

        public void Main(string argument, UpdateType updateType) {
            TickID++;
            try {
                ExeTime.Start();
                if ((updateType & (UpdateType.Trigger | UpdateType.Terminal)) != 0) {
                    Input(argument);
                }
                Loop(updateType);
                ExeTime.End();
            } catch (Exception e) {
                var sb = new StringBuilder();
                sb.AppendLine("Exception Message:");
                sb.AppendLine($"   {e.Message}");
                sb.AppendLine();
                sb.AppendLine("Stack trace:");
                sb.AppendLine(e.StackTrace);
                sb.AppendLine();
                var exceptionDump = sb.ToString();
                var lcd = this.GridTerminalSystem.GetBlockWithName("EXCEPTION DUMP") as IMyTextPanel;
                Echo(exceptionDump);
                lcd?.WriteText(exceptionDump, append: false);
                throw;
            }
        }

        private void Init() {
            Settings = new Dictionary<ID, Setting>();
            Settings.Add(ID.Tag, new Setting(Strings.Tag, "#Ruler"));
            Settings.Add(ID.FancyTag, new Setting(Strings.FancyTag, "#Fancy"));
            Settings.Add(ID.Range, new Setting(Strings.Range, (long) -1));
            Settings.Add(ID.TopSpeed, new Setting(Strings.TopSpeed, 100.0));
            Settings.Add(ID.OnlyEnemies, new Setting(Strings.OnlyEnemies, false));
            Settings.Add(ID.TextPanelStartWithEmptyLine, new Setting(Strings.TextPanelStartWithEmptyLine, true, 1));
            Settings.Add(ID.TextPanelPadding, new Setting(Strings.TextPanelPadding, 1));
            ParseUserSettings(Me.CustomData);
            PrintSettingsToCustomData();
            Cameras = new List<IMyCameraBlock>();
            SlimTextPanels = new List<IMyTextPanel>();
            TextPanels = new List<IMyTextPanel>();
            TriggerBlocks = new List<IMyTerminalBlock>();
            var blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType(blocks, x => General.ContainsExact((string) Settings[ID.Tag].Value, x.CustomName));
            foreach (var block in blocks) {
                if (block is IMyCameraBlock) {
                    Cameras.Add(block as IMyCameraBlock);
                    Cameras.Last().EnableRaycast = true;
                } else if (block is IMyTextPanel) {
                    var panel = block as IMyTextPanel;
                    switch (Graphics.GetPanelType(panel.BlockDefinition.SubtypeId)) {
                        case PanelType.Corner:
                            SlimTextPanels.Add(panel);
                            break;
                        default:
                            if (General.ContainsExact((string) Settings[ID.FancyTag].Value, block.CustomName)) {
                                if (GNormal == null) GNormal = new Graphics(59, 59, panel, Echo);
                                else GNormal.Panels.Add(panel);
                                panel.FontSize = 0.3f;
                                if (panel.GetValue<long>("Font") == 151057691) panel.SetValue<long>("Font", 1147350002);
                                else if (panel.Font == "DotMatrix") GNormal.SetBackground(GNormal.BlankPixel);
                            } else {
                                TextPanels.Add(panel);
                            }
                            break;
                    }
                } else if (block is IMyProgrammableBlock || block is IMyTimerBlock || block is IMySoundBlock) {
                    TriggerBlocks.Add(block);
                }
            }
            var controllers = new List<IMyShipController>();
            GridTerminalSystem.GetBlocksOfType(controllers);
            if (controllers.Count > 0) Controller = controllers[0];
            else {
                Problem.NoController = true;
            }
            if (GNormal == null && SlimTextPanels.Count == 0 && TextPanels.Count == 0) {
                Problem.NoLCD = true;
            }
            if (Cameras.Count == 0) {
                Problem.NoCamera = true;
            }
            Initialized = true;
        }

        private int GetBestCameraIndex(long range) {
            if (range > 0) {
                // If we have a max range, find the camera with the lowest charge >= range which is also currently working.
                var result = Enumerable.Range(0, Cameras.Count).Aggregate(-1, (bestIndex, index) => 
                    Cameras[index].IsWorking && Cameras[index].AvailableScanRange >= range
                                             && (bestIndex < 0 || Cameras[index].AvailableScanRange < Cameras[bestIndex].AvailableScanRange)
                        ? index : bestIndex
                );
                if (result >= 0) {
                    // Only return result if we actually found a camera.
                    return result;
                }
                // Otherwise, fall through and use the camera with the highest charge.
            }
            // Use the camera with the highest charge which is also currently working.
            return Enumerable.Range(0, Cameras.Count).Aggregate(-1, (bestIndex, index) =>
                Cameras[index].IsWorking && (bestIndex < 0 || Cameras[index].AvailableScanRange > Cameras[bestIndex].AvailableScanRange)
                ? index : bestIndex
            );
        }

        private bool ParseRange(string argument, out long range) {
            return long.TryParse(System.Text.RegularExpressions.Regex.Match(argument, @"-?\d+").Value, out range);
        }

        private void Input(string argument) {
            Problem.InvalidCommand = "";
            argument = argument.ToLower();
            if (argument == Strings.Update) {
                Init();
            } else if (argument.Contains(Strings.ScanArg)) {
                var range = (long) Settings[ID.Range].Value;
                Problem.RangeError = argument != Strings.ScanArg && !ParseRange(argument, out range);
                if (!Problem.RangeError) {
                    var index = GetBestCameraIndex(range);
                    if (index >= 0) {
                        Info = new DistanceInfo(Cameras[index], range, Controller, this);
                        Trigger();
                    } else {
                        Info = null;
                        Problem.NoCamera = true;
                    }
                }
            } else if (argument.Contains(Strings.RangeArg)) {
                long range;
                if (ParseRange(argument, out range)) {
                    Settings[ID.Range].Value = range;
                    PrintSettingsToCustomData();
                    Problem.RangeError = false;
                } else {
                    Problem.RangeError = true;
                }
            } else if (argument.Contains(Strings.ProjectArg)) {
                long range;
                if (ParseRange(argument, out range)) {
                    var index = GetBestCameraIndex(range);
                    if (index >= 0) {
                        var forward = Cameras[index].WorldMatrix.GetOrientation().Forward;
                        Info = new DistanceInfo(forward, Cameras[index], range, Controller, this);
                        Problem.RangeError = false;
                    } else {
                        Info = null;
                        Problem.NoCamera = true;
                    }
                } else {
                    Problem.RangeError = true;
                }
            } else {
                Problem.InvalidCommand = argument;
            }
            DrawAll();
        }

        void ConsolePrint() {
            Echo("Current: " + CurrentCamera);
            Echo("Available scan range: " + Range);
            if ((long) Settings[ID.Range].Value > 0) Echo("Range limit: " + Settings[ID.Range].Value + "m");
            else Echo("Range limit: -1 (Unlimited)");
            Echo("");
            if (Problem.RangeError) Echo(Strings.RangeError);
            if (!string.IsNullOrEmpty(Problem.InvalidCommand)) Echo("Invalid command: " + Problem.InvalidCommand);
            if (Info == null || string.IsNullOrEmpty(Info.Type)) {
                Echo("No scan data.");
            } else {
                Echo("Scan by: " + Info.Camera.CustomName);
                Echo("Type: " + Info.Type);
                Echo("Relationship: " + Info.Relationship);
                Echo("Distance: " + Info.GetDistance(TickID));
                Echo("Time to reach: " + Info.TimeToTarget);
                Echo("At top speed: " + Info.TimeToTargetTopSpeed);
                Echo("GPS: Available in \"Custom Data\" here and in attached LCDs.");
                PrintSettingsToCustomData();
                Me.CustomData = Info.GPS + "\n\n" + Me.CustomData;
            }
        }

        void PanelPrint() {
            var text = new FixedWidthText(70);
            if ((bool) Settings[ID.TextPanelStartWithEmptyLine].Value) text.AppendLine();
            text.AppendLine("Current: " + CurrentCamera);
            text.AppendLine("Range: " + Range);
            text.AppendLine("&L—");
            if (Problem.RangeError) text.AppendLine(Strings.RangeError);
            if (!string.IsNullOrEmpty(Problem.InvalidCommand)) text.AppendLine("Invalid command: " + Problem.InvalidCommand);
            if (Info == null || string.IsNullOrEmpty(Info.Type)) {
                text.AppendLine("No scan data.");
            } else {
                text.AppendLine("Scan by: " + Info.Camera.CustomName);
                text.AppendLine("Type: " + ReplaceNames(Info.Type));
                text.AppendLine("Relationship: " + Info.Relationship);
                text.AppendLine("Distance: " + Info.GetDistance(TickID));
                text.AppendLine("Reach in: " + Info.TimeToTarget);
                text.AppendLine("At top speed: " + Info.TimeToTargetTopSpeed);
            }
            foreach (var panel in TextPanels) {
                string font = "Debug";
                if (FontSize.ContainsKey(panel.Font)) font = panel.Font;
                int w = (int) (FontSize[font] / panel.FontSize);
                if (Info != null && !string.IsNullOrEmpty(Info.GPS)) panel.CustomData = Info.GPS;
                panel.WriteText(text.GetText(w, General.Dots((int) Settings[ID.TextPanelPadding].Value, ' ')));
            }
        }

        void Trigger() {
            if (TriggerBlocks.Count > 0) {
                if (Info != null && !string.IsNullOrEmpty(Info.GPS)) {
                    if ((bool) Settings[ID.OnlyEnemies].Value && Info.Relationship != "Enemies") return;
                    foreach (var block in TriggerBlocks) {
                        if (block is IMyProgrammableBlock) {
                            (block as IMyProgrammableBlock).TryRun(Info.GPS);
                        } else if (block is IMyTimerBlock) {
                            block.ApplyAction("TriggerNow");
                        } else if (block is IMySoundBlock) {
                            block.ApplyAction("PlaySound");
                        }
                    }
                }
            }
        }

        void DrawAll() {
            UpdateDetailedInfo();
            if (GNormal != null) Draw();
            if (SlimTextPanels != null) SlimPrint();
            if (TextPanels != null) PanelPrint();
        }

        private void Loop(UpdateType updateType) {
            if (!Initialized) Init();
            if ((updateType & UpdateType.Update100) != 0) {
                DrawAll();
            }
            if (Cameras != null && Cameras.Count > 0) UpdateRange();
        }

        public string ReplaceNames(string input) {
            foreach (var array in CUSTOM_TYPE_NAME) {
                if (array[0] == input) return array[1];
            }
            return input;
        }

        public void UpdateRange() {
            var range = (long) Settings[ID.Range].Value;
            var cameraIndex = GetBestCameraIndex(range);
            if (cameraIndex < 0) {
                CurrentCamera = "No camera!";
                Range = "";
            } else {
                CurrentCamera = Cameras[cameraIndex].CustomName;
                if (range < 1) {
                    var scanRange = Cameras[cameraIndex].AvailableScanRange;
                    Range = General.AddPrefixKm(scanRange);
                } else {
                    var numScans = (int) Cameras.Sum(camera => camera.IsWorking ? Math.Floor(camera.AvailableScanRange / range) : 0);
                    Range = General.AddPrefixKm(range) + " x " + numScans;
                }
            }
        }

        void SlimPrint() {
            if (SlimTextPanels.Count > 0) {
                string text = "Range | Thing | Distance\n" + Range + " | ";
                string gps = "";
                if (Info == null || string.IsNullOrEmpty(Info.Type)) {
                    text += "-- | --";
                } else {
                    text += ReplaceNames(Info.Type) + " | " + Info.GetDistance(TickID);
                    gps = Info.GPS;
                }
                foreach (var panel in SlimTextPanels) {
                    panel.WriteText(text);
                    panel.CustomData = gps;
                }
            }
        }

        private void Draw(int x = 3, int y = 0) {
            int w = 59;
            GNormal.Clear();
            GNormal.SetForeground(Color.Gray);
            GNormal.Print(x, y + 4, "Scan range");
            GNormal.SetForeground(Color.White);
            GNormal.Print(x, y + 11, Range);
            GNormal.SetForeground(Color.Gray);
            GNormal.Line(x, y + 18, w - (x * 2), y + 18);
            GNormal.Print(x, y + 21, "Scan result");
            GNormal.SetForeground(Color.White);
            if (Info == null || string.IsNullOrEmpty(Info.Type)) {
                GNormal.Print(x, y + 28, "-");
            } else {
                GNormal.Print(x, y + 28, ReplaceNames(Info.Type));
                GNormal.Print(x, y + 35, ReplaceNames(Info.Relationship));
                GNormal.Print(x, y + 42, Info.GetDistance(TickID));
                GNormal.Print(x, y + 49, Info.TimeToTarget);
                foreach (var panel in GNormal.Panels) panel.CustomData = Info.GPS;
            }
            GNormal.Draw();
        }

        private void UpdateDetailedInfo() {
            Dots += ".";
            DotDotDot++;
            if (DotDotDot > 3) {
                DotDotDot = 0;
                Dots = "";
            }
            DetailedInfo.Clear();
            Echo(string.Format(DetailedInfoStandardText, Dots));
            if (ShowSettingsProblems) {
                Echo(SettingsProblems.GetText());
            }
            if (Problem.NoCamera) {
                DetailedInfo.AppendLine();
                DetailedInfo.AppendLine(Strings.NoCamera + " " + Settings[ID.Tag].Value);
            }
            if (Problem.NoLCD) {
                DetailedInfo.AppendLine();
                DetailedInfo.AppendLine(Strings.NoLCD + " " + Settings[ID.Tag].Value);
            }
            Echo(DetailedInfo.GetText());
            ConsolePrint();
        }

        private void ParseUserSettings(string text) {
            if (SettingsProblems == null) SettingsProblems = new FixedWidthText(30);
            SettingsProblems.Clear();
            SettingsProblems.AppendLine("______________________________");
            SettingsProblems.AppendLine(Strings.SettingsProblem + ":");
            if (text.Length > 0) {
                var lines = text.Split('\n');
                bool inSettings = false;
                for (int i = 0; i < lines.Length; i++) {
                    if (!inSettings) {
                        if (lines[i].Contains(ScriptName + " " + Strings.Settings)) inSettings = true;
                    } else {
                        if (lines[i].Contains("</settings>")) inSettings = false;
                        else {
                            var keys = new List<ID>(Settings.Keys);
                            foreach (var key in keys) {
                                if (lines[i].StartsWith(Settings[key].Text)) {
                                    var parts = lines[i].Split(new char[] {':'}, 2);
                                    if (!string.IsNullOrEmpty(parts[1])) {
                                        var val = parts[1].Trim();
                                        if (Settings[key].Value is bool) {
                                            if (val.ToLower() == Strings.Yes) {
                                                Settings[key].Value = true;
                                            } else if (val.ToLower() == Strings.No) {
                                                Settings[key].Value = false;
                                            } else {
                                                SettingsProblemIllegible(key, Strings.HasToBeBool);
                                            }
                                        } else if (Settings[key].Value is int) {
                                            int temp = 0;
                                            if (int.TryParse(val, out temp)) {
                                                Settings[key].Value = temp;
                                            } else {
                                                SettingsProblemIllegible(key, Strings.HasToBeNumber);
                                            }
                                        } else if (Settings[key].Value is long) {
                                            long temp = 0;
                                            if (long.TryParse(val, out temp)) {
                                                Settings[key].Value = temp;
                                            } else {
                                                SettingsProblemIllegible(key, Strings.HasToBeNumber);
                                            }
                                        } else if (Settings[key].Value is double) {
                                            double temp = 0;
                                            if (double.TryParse(val, out temp)) {
                                                Settings[key].Value = temp;
                                            } else {
                                                SettingsProblemIllegible(key, Strings.HasToBeNumber);
                                            }
                                        } else if (Settings[key].Value is string) {
                                            if (!string.IsNullOrEmpty(val) && !string.IsNullOrWhiteSpace(val)) {
                                                Settings[key].Value = val;
                                            } else {
                                                SettingsProblemIllegible(key, Strings.HasToBeString);
                                            }
                                        }
                                    } else {
                                        SettingsProblemIllegible(key);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void SettingsProblemIllegible(ID key, string additionalInfo = "") {
            ShowSettingsProblems = true;
            SettingsProblems.AppendLine();
            SettingsProblems.AppendLine(Strings.IllegibleInput + " " + Strings.UsingDefault);
            SettingsProblems.AppendLine("> " + Settings[key].Text);
            if (additionalInfo != "") SettingsProblems.AppendLine("> " + additionalInfo);
        }

        private void PrintSettingsToCustomData() {
            if (CustomData == null) CustomData = new FixedWidthText(70);
            CustomData.Clear();
            CustomData.AppendLine(ScriptName + " " + Strings.Settings);
            CustomData.AppendLine("----------------------------------------------------------------------");
            CustomData.AppendLine(Strings.SettingsInstructions);
            CustomData.AppendLine(Strings.CommandsInstructions);
            CustomData.AppendLine("----------------------------------------------------------------------");
            CustomData.AppendLine();
            foreach (var setting in Settings.Values) {
                for (int i = 0; i < setting.SpaceAbove; i++) {
                    CustomData.AppendLine();
                }
                CustomData.AppendLine(setting.Text + ": " + SettingToString(setting.Value));
            }
            Me.CustomData = CustomData.GetText();
        }

        private string SettingToString(object input) {
            if (input is bool) {
                return (bool) input ? Strings.Yes : Strings.No;
            }
            return input.ToString();
        }

        public class Setting {
            public string Text;
            private object _value;

            public object Value {
                get { return _value; }
                set { _value = value; }
            }

            public int SpaceAbove;

            public Setting(string text, object value, int spaceAbove = 0) {
                Text = text;
                Value = value;
                SpaceAbove = spaceAbove;
            }
        }

        public enum ID {
            Tag,
            FancyTag,
            Range,
            TopSpeed,
            OnlyEnemies,
            TextPanelStartWithEmptyLine,
            TextPanelPadding
        };

        class Strings {
            public const string Settings = "settings";
            public const string Tag = "Tag";
            public const string FancyTag = "Graphic display (add after regular tag)";
            public const string Range = "Range";
            public const string TopSpeed = "Top speed";
            public const string OnlyEnemies = "Only trigger on enemies";
            public const string TextPanelStartWithEmptyLine = "Text view - Start with empty line";
            public const string TextPanelPadding = "Text view - Padding before text";
            public const string RangeArg = "range";
            public const string ScanArg = "scan";
            public const string ProjectArg = "project";
            public const string SettingsProblem = "Problem with settings";
            public const string IllegibleInput = "Did not understand setting.";
            public const string HasToBeNumber = "Has to be a number.";
            public const string HasToBeBool = "Has to be " + Yes + " or " + No + ".";
            public const string HasToBeString = "Text missing.";
            public const string UsingDefault = "Using default or previous value.";

            public const string SettingsInstructions =
                "To change settings: Edit the value after the colon, then send the command '" + Update + "' to the script.";

            public const string CommandsInstructions =
                "To send a command, enter it as an argument in the programmable block and press run. (Can also be done via an action, e.g. in a button).";

            public const string Yes = "yes";
            public const string No = "no";
            public const string Update = "update";
            public const string Performance = "Load (avg, peak)";
            public const string SettingsInCustomData = "Settings: See Custom Data.";
            public const string NoCamera = "No working cameras found. You need to tag at least one camera with";
            public const string NoLCD = "No LCDs found. Don't you want one? I would want one. Tag one or many with";
            public const string RangeError = "Oops, failed to set range.";
        }
    }
}