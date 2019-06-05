using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI.Ingame;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace ShipLayout {
    public sealed class Program : MyGridProgram {
        // ShipLayout by zanders3
        // This script displays the layout and health status of your ship or station.
        // To use run this script and add e.g. 'ShipLayout 0' or 'ShipLayoutHealth' to the CustomData of an LCD.
        public Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }
        
        private delegate Vector3I RotateFunc(Vector3I pos, Vector3I size);
        
        private static Vector3I Rot1(Vector3I pos, Vector3I size) { return new Vector3I(size.Y - pos.Y, pos.X, pos.Z); }
        private static Vector3I Rot2(Vector3I pos, Vector3I size) { return new Vector3I(size.X - pos.X, size.Y - pos.Y, pos.Z); }
        private static Vector3I Rot3(Vector3I pos, Vector3I size) { return new Vector3I(pos.Y, size.X - pos.X, pos.Z); }
        
        private static Vector3I XUp(Vector3I pos, Vector3I size) { return new Vector3I(pos.Z, pos.Y, pos.X); }
        private static Vector3I YUp(Vector3I pos, Vector3I size) { return new Vector3I(size.Z - pos.Z, pos.X, pos.Y); }
        private static Vector3I ZUp(Vector3I pos, Vector3I size) { return new Vector3I(size.X - pos.X, pos.Y, pos.Z); }
        
        private static Vector3I XUp1(Vector3I pos, Vector3I size) { return Rot1(XUp(pos, size), XUp(size, size * 2)); }
        private static Vector3I XUp2(Vector3I pos, Vector3I size) { return Rot2(XUp(pos, size), XUp(size, size * 2)); }
        private static Vector3I XUp3(Vector3I pos, Vector3I size) { return Rot3(XUp(pos, size), XUp(size, size * 2)); }
        
        private static Vector3I YUp1(Vector3I pos, Vector3I size) { return Rot1(YUp(pos, size), YUp(size, size * 2)); }
        private static Vector3I YUp2(Vector3I pos, Vector3I size) { return Rot2(YUp(pos, size), YUp(size, size * 2)); }
        private static Vector3I YUp3(Vector3I pos, Vector3I size) { return Rot3(YUp(pos, size), YUp(size, size * 2)); }
        
        private static Vector3I ZUp1(Vector3I pos, Vector3I size) { return Rot1(ZUp(pos, size), ZUp(size, size * 2)); }
        private static Vector3I ZUp2(Vector3I pos, Vector3I size) { return Rot2(ZUp(pos, size), ZUp(size, size * 2)); }
        private static Vector3I ZUp3(Vector3I pos, Vector3I size) { return Rot3(ZUp(pos, size), ZUp(size, size * 2)); }
        
        private static Vector3I XDown(Vector3I pos, Vector3I size) { return new Vector3I(pos.Z, size.Y - pos.Y, size.X - pos.X); }
        private static Vector3I YDown(Vector3I pos, Vector3I size) { return new Vector3I(size.Z - pos.Z, size.X - pos.X, size.Y - pos.Y); }
        private static Vector3I ZDown(Vector3I pos, Vector3I size) { return new Vector3I(size.X - pos.X, size.Y - pos.Y, size.Z - pos.Z); }
        
        private static Vector3I XDown1(Vector3I pos, Vector3I size) { return Rot1(XDown(pos, size), XDown(size, size * 2)); }
        private static Vector3I XDown2(Vector3I pos, Vector3I size) { return Rot2(XDown(pos, size), XDown(size, size * 2)); }
        private static Vector3I XDown3(Vector3I pos, Vector3I size) { return Rot3(XDown(pos, size), XDown(size, size * 2)); }
        
        private static Vector3I YDown1(Vector3I pos, Vector3I size) { return Rot1(YDown(pos, size), YDown(size, size * 2)); }
        private static Vector3I YDown2(Vector3I pos, Vector3I size) { return Rot2(YDown(pos, size), YDown(size, size * 2)); }
        private static Vector3I YDown3(Vector3I pos, Vector3I size) { return Rot3(YDown(pos, size), YDown(size, size * 2)); }
        
        private static Vector3I ZDown1(Vector3I pos, Vector3I size) { return Rot1(ZDown(pos, size), ZDown(size, size * 2)); }
        private static Vector3I ZDown2(Vector3I pos, Vector3I size) { return Rot2(ZDown(pos, size), ZDown(size, size * 2)); }
        private static Vector3I ZDown3(Vector3I pos, Vector3I size) { return Rot3(ZDown(pos, size), ZDown(size, size * 2)); }
        
        private int idx;
        
        private static readonly RotateFunc[] SwizzleFuncs = {
            XUp, XUp1, XUp2, XUp3, YUp, YUp1, YUp2, YUp3, ZUp, ZUp1, ZUp2, ZUp3,
            XDown, XDown1, XDown2, XDown3, YDown, YDown1, YDown2, YDown3, ZDown, ZDown1, ZDown2, ZDown3
        };
        enum BlockState {
            Empty, Destroyed, NonFunctional, Damaged, Normal
        }
        
        private BlockState[,,] savedGrid;
        private Vector3I gridMin, gridMax;
        
        private struct TileState {
            public int Healthy, Total;
            public int Depth;
        }
        
        private int tilesize;
        private TileState[,] tiles;
        
        private int hullPercent;
        
        private IEnumerable<bool> CheckGrid(IMyCubeGrid grid) {
            int totalHealthy = 0, total = 0;
            for (var x = gridMin.X; x < gridMax.X; ++x) {
                for (var y = gridMin.Y; y < gridMax.Y; ++y) {
                    for (var z = gridMin.Z; z < gridMax.Z; ++z) {
                        var pos = new Vector3I(x, y, z);
                        if (savedGrid[x - gridMin.X, y - gridMin.Y, z - gridMin.Z] != BlockState.Empty) {
                            ++total;
                            savedGrid[x - gridMin.X, y - gridMin.Y, z - gridMin.Z] = grid.CubeExists(pos) ? BlockState.Normal : BlockState.Destroyed;
                            if (grid.CubeExists(pos)) {
                                ++totalHealthy;
                            }
                        }
                    }
                }
                yield return true;
            }
            hullPercent = total > 0 ? totalHealthy * 100 / total : 100;
        }
        
        private void ResetGridData(IMyCubeGrid grid) {
            gridMin = grid.Min - Vector3I.One;
            gridMax = grid.Max + Vector3I.One;
            var size = gridMax - gridMin;
            savedGrid = new BlockState[size.X, size.Y, size.Z];
            tilesize = Math.Max(size.X, Math.Max(size.Y, size.Z)) + 1;
            tiles = new TileState[tilesize, tilesize];
        }
        
        private IEnumerable<bool> ResetSavedGrid(IMyCubeGrid grid) {
            var lastY = -1;
            foreach (var vector in Vector3I.EnumerateRange(gridMin, gridMax)) {
                if (lastY > vector.Y) {
                    yield return true;
                }
                lastY = vector.Y;
                savedGrid[vector.X - gridMin.X, vector.Y - gridMin.Y, vector.Z - gridMin.Z] =
                    grid.CubeExists(vector) ? BlockState.Normal : BlockState.Empty;
            }
        }
        
        private void SaveGridToCustomData() {
            var output = new StringBuilder();
            var size = gridMax - gridMin;
            for (var z = 0; z < size.Z; ++z) {
                for (var y = size.Y - 1; y >= 0; --y) {
                    for (var x = 0; x < size.X; ++x) {
                        output.Append(savedGrid[x, y, z] == BlockState.Empty ? "~" : "=");
                    }
                    output.Append("\n");
                }
                output.Append("\n");
            }
            Me.CustomData = output.ToString();
        }
        
        private IEnumerable<bool> LoadGridFromCustomData() {
            ResetGridData(largestGrid);
            var lines = Me.CustomData.Split('\n');
            var size = gridMax - gridMin;
            var expected = (size.Y + 1) * size.Z + 1;
            if (lines.Length != expected || lines[0].Length != size.X) {
                foreach (var result in ResetSavedGrid(largestGrid)) {
                    yield return result;
                }
                SaveGridToCustomData();
            } else {
                var line = 0;
                for (var z = 0; z < size.Z; ++z) {
                    for (var y = size.Y - 1; y >= 0; --y) {
                        for (var x = 0; x < size.X; ++x) {
                            savedGrid[x, y, z] = lines[line][x] == '=' ? BlockState.Normal : BlockState.Empty;
                        }
                        line++;
                    }
                    line++;
                }
            }
        }
        
        private IEnumerable<bool> Draw(MySpriteDrawFrame frame, RotateFunc swizzle, RectangleF screen) {
            var size = gridMax - gridMin;
            var sizecube = swizzle(size, size * 2);
            var scale = Math.Min(screen.Width, screen.Height) / Math.Max(sizecube.X, sizecube.Y);
            var xoff = (screen.Width - sizecube.X * scale) * 0.5f + screen.X;
            var yoff = (screen.Width - sizecube.Y * scale) * 0.5f + screen.Y;
            for (var x = 0; x <= sizecube.X; x++)
                for (var y = 0; y <= sizecube.Y; y++)
                    tiles[x, y] = new TileState();
            for (var x = gridMin.X; x < gridMax.X; ++x) {
                for (var y = gridMin.Y; y < gridMax.Y; ++y) {
                    for (var z = gridMin.Z; z < gridMax.Z; ++z) {
                        var state = savedGrid[x - gridMin.X, y - gridMin.Y, z - gridMin.Z];
                        if (state != BlockState.Empty) {
                            var pos = new Vector3I(x, y, z);
                            var poscube = swizzle(pos - gridMin, size);
                            var tile = tiles[poscube.X, poscube.Y];
                            tile.Depth = Math.Max(tile.Depth, poscube.Z);
                            tile.Total++;
                            if (state == BlockState.Normal)
                                tile.Healthy++;
                            tiles[poscube.X, poscube.Y] = tile;
                        }
                    }
                }
                yield return true;
            }
            for (var x = 0; x<=sizecube.X; x++) {
                for (var y = 0; y<=sizecube.Y; y++) {
                    var tile = tiles[x, y];
                    if (tile.Total == 0)
                        continue;
                    var depth = tile.Depth / (float)sizecube.Z;
                    depth = depth * depth * depth * depth + 0.05f;
                    var health = tile.Healthy / (float)tile.Total;
                    if (tile.Healthy < tile.Total)
                        health *= 0.5f;
                    frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(x * scale + xoff, y * scale + yoff), new Vector2(scale, scale), new Color(depth, depth * health, depth * health)));
                }
            }
            for (var i = 0; i < blocks.Count; ++i) {
                var posCube = swizzle(blocks[i].Position - gridMin, size);
                var posSize = swizzle(blocks[i].Size, Vector3I.Zero);
                if (posSize.X < 0) { posCube.X += posSize.X + 1; posSize.X = -posSize.X; }
                if (posSize.Y < 0) { posCube.Y += posSize.Y + 1; posSize.Y = -posSize.Y; }
                if (posSize.Z < 0) { posCube.Z += posSize.Z + 1; posSize.Z = -posSize.Z; }
                var colour = blocks[i].State == BlockState.Normal ? Color.Green : blocks[i].State == BlockState.Damaged ? Color.Yellow : blocks[i].State == BlockState.NonFunctional ? Color.Orange : Color.Red;
                frame.Add(new MySprite(SpriteType.TEXTURE, "SquareHollow", new Vector2((posCube.X + posSize.X * 0.5f - 0.5f) * scale + xoff, (posCube.Y + posSize.Y * 0.5f - 0.5f) * scale + yoff), new Vector2(posSize.X * scale, posSize.Y * scale), colour));
            }
        }
        
        struct TerminalBlockState {
            public Vector3I Position, Size;
            public BlockState State;
            public IMyTerminalBlock Block;
        }
        
        private readonly List<TerminalBlockState> blocks = new List<TerminalBlockState>();
        private readonly List<IMyTerminalBlock> lcds = new List<IMyTerminalBlock>();
        private readonly List<IMyTerminalBlock> healthBars = new List<IMyTerminalBlock>();
        private string healthString = "";
        private bool wantReset = true;
        private IMyCubeGrid largestGrid;
        
        private IMyCubeGrid GetLargestCubeGrid(IEnumerable<IMyTerminalBlock> fullBlocks) {
            var result = Me.CubeGrid;
            foreach (var terminalBlock in fullBlocks) {
                if (terminalBlock.CubeGrid.IsSameConstructAs(Me.CubeGrid) && terminalBlock.CubeGrid.GridSize > result.GridSize) {
                    result = terminalBlock.CubeGrid;
                }
            }
            return result;
        }
        
        IEnumerable<bool> RunProgram() {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            if (wantReset) {
                Echo("Reset Grid!");
                List<IMyTerminalBlock> fullBlocks = new List<IMyTerminalBlock>();
                GridTerminalSystem.GetBlocks(fullBlocks);
                largestGrid = GetLargestCubeGrid(fullBlocks);
                wantReset = false;
                foreach (var val in LoadGridFromCustomData())
                    yield return val;
                blocks.Clear();
                foreach (var block in fullBlocks) {
                    if (block.CubeGrid == largestGrid) {
                        blocks.Add(new TerminalBlockState { Position = block.Min, Size = block.Max - block.Min + Vector3I.One, State = BlockState.Normal, Block = block });
                    }
                }
                yield break;
            }
            //Update grid
            {
                idx = (idx + 1) % 1000000;
                GridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(lcds, block => block.CustomData.Contains("ShipLayout") && !block.CustomData.Contains("ShipLayoutHealth"));
                GridTerminalSystem.GetBlocksOfType<IMyTextSurface>(healthBars, block => block.CustomData.Contains("ShipLayoutHealth"));
                foreach (var val in CheckGrid(largestGrid))
                    yield return val;
                var terminalHealthy = 0;
                for (var i = 0; i < blocks.Count; ++i) {
                    var slimBlock = largestGrid.GetCubeBlock(blocks[i].Position);
                    var exists = slimBlock != null;
                    var undamaged = exists && slimBlock.IsFullIntegrity;
                    var functional = exists && blocks[i].Block.IsFunctional;
                    if (exists)
                        ++terminalHealthy;
                    TerminalBlockState s = blocks[i];
                    s.State = exists ? undamaged ? BlockState.Normal : functional ? BlockState.Damaged : BlockState.NonFunctional : BlockState.Destroyed;
                    blocks[i] = s;
                }
                var terminalPercent = blocks.Count > 0 ? terminalHealthy * 100 / blocks.Count : 100;
                healthString = string.Format("Hull " + hullPercent.ToString("000") + "% Systems " + terminalPercent.ToString("000") + "%");
            }
            //Update healthbars
            foreach (var healthBar in healthBars) {
                var surf = (IMyTextSurface) healthBar;
                surf.ContentType = ContentType.TEXT_AND_IMAGE;
                surf.WriteText(healthString);
            }
            var swizzleIndex = idx % SwizzleFuncs.Length;
            RotateFunc swizzle = SwizzleFuncs[swizzleIndex];
            //Draw Program screen
            {
                IMyTextSurface surf = Me.GetSurface(0);
                surf.ContentType = ContentType.SCRIPT;
                MySpriteDrawFrame frame = surf.DrawFrame();
                foreach (bool val in Draw(frame, swizzle, new RectangleF(0f, 0f, surf.SurfaceSize.X, surf.SurfaceSize.Y)))
                    yield return val;
                frame.Add(MySprite.CreateText("ShipLayout by zanders3\nAdd ShipLayout " + swizzleIndex + " or ShipLayoutHealth\nto CustomData on LCD\n" + healthString, "DEBUG", Color.Black));
                frame.Dispose();
            }
            //Draw LCD screen
            {
                foreach (var lcd in lcds) {
                    var bits = lcd.CustomData.Split(' ');
                    if (bits.Length >= 2) {
                        var viewIndex = 1 + idx % (bits.Length - 1);
                        int mode;
                        int.TryParse(bits[viewIndex], out mode);
                        swizzle = SwizzleFuncs[mode % SwizzleFuncs.Length];
                    }
        
                    var surf = ((IMyTextSurfaceProvider)lcd).GetSurface(0);
                    surf.ContentType = ContentType.SCRIPT;
                    MySpriteDrawFrame frame = surf.DrawFrame();
                    foreach (bool val in Draw(frame, swizzle, new RectangleF(0f, 0f, surf.SurfaceSize.X, surf.SurfaceSize.Y)))
                        yield return val;
                    frame.Dispose();
                }
            }
            if (!wantReset)
                Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }
        
        private IEnumerator<bool> currentWork;
        
        public void Main(string argument, UpdateType updateSource) {
            Echo("View " + (idx % SwizzleFuncs.Length) + " LCDs " + lcds.Count);
            Echo("Update Time: " + Runtime.LastRunTimeMs.ToString("F"));
            if (updateSource == UpdateType.Terminal && argument == "reset") {
                Me.CustomData = "";
                wantReset = true;
            }
            if (currentWork == null)
                currentWork = RunProgram().GetEnumerator();
            if (currentWork.MoveNext() == false) {
                currentWork.Dispose();
                currentWork = null;
            }
        }
    }
}