using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace SEProgrammableBlocks {
    public sealed class Program : MyGridProgram {
        // ShipLayout by zanders3
        // This script displays the layout and health status of your ship or station.
        // To use run this script and add e.g. 'ShipLayout 0' or 'ShipLayoutHealth' to the CustomData of an LCD.
        public Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public delegate Vector3I RotateFunc(Vector3I pos, Vector3I size);
        
        static Vector3I Rot1(Vector3I pos, Vector3I size) { return new Vector3I(size.Y - pos.Y, pos.X, pos.Z); }
        static Vector3I Rot2(Vector3I pos, Vector3I size) { return new Vector3I(size.X - pos.X, size.Y - pos.Y, pos.Z); }
        static Vector3I Rot3(Vector3I pos, Vector3I size) { return new Vector3I(pos.Y, size.X - pos.X, pos.Z); }

        static Vector3I XUp(Vector3I pos, Vector3I size) { return new Vector3I(pos.Z, pos.Y, pos.X); }
        static Vector3I YUp(Vector3I pos, Vector3I size) { return new Vector3I(pos.Z, pos.X, pos.Y); }
        static Vector3I ZUp(Vector3I pos, Vector3I size) { return new Vector3I(pos.X, pos.Y, pos.Z); }
        
        static Vector3I XUp1(Vector3I pos, Vector3I size) { return Rot1(XUp(pos, size), XUp(size, size)); }
        static Vector3I XUp2(Vector3I pos, Vector3I size) { return Rot2(XUp(pos, size), XUp(size, size)); }
        static Vector3I XUp3(Vector3I pos, Vector3I size) { return Rot3(XUp(pos, size), XUp(size, size)); }
        
        static Vector3I YUp1(Vector3I pos, Vector3I size) { return Rot1(YUp(pos, size), YUp(size, size)); }
        static Vector3I YUp2(Vector3I pos, Vector3I size) { return Rot2(YUp(pos, size), YUp(size, size)); }
        static Vector3I YUp3(Vector3I pos, Vector3I size) { return Rot3(YUp(pos, size), YUp(size, size)); }
        
        static Vector3I ZUp1(Vector3I pos, Vector3I size) { return Rot1(ZUp(pos, size), ZUp(size, size)); }
        static Vector3I ZUp2(Vector3I pos, Vector3I size) { return Rot2(ZUp(pos, size), ZUp(size, size)); }
        static Vector3I ZUp3(Vector3I pos, Vector3I size) { return Rot3(ZUp(pos, size), ZUp(size, size)); }

        static Vector3I XDown(Vector3I pos, Vector3I size) { return new Vector3I(pos.Z, pos.Y, size.X - pos.X); }
        static Vector3I YDown(Vector3I pos, Vector3I size) { return new Vector3I(pos.Z, pos.X, size.Y - pos.Y); }
        static Vector3I ZDown(Vector3I pos, Vector3I size) { return new Vector3I(pos.X, pos.Y, size.Z - pos.Z); }

        static Vector3I XDown1(Vector3I pos, Vector3I size) { return Rot1(XDown(pos, size), XDown(size, size)); }
        static Vector3I XDown2(Vector3I pos, Vector3I size) { return Rot2(XDown(pos, size), XDown(size, size)); }
        static Vector3I XDown3(Vector3I pos, Vector3I size) { return Rot3(XDown(pos, size), XDown(size, size)); }

        static Vector3I YDown1(Vector3I pos, Vector3I size) { return Rot1(YDown(pos, size), YDown(size, size)); }
        static Vector3I YDown2(Vector3I pos, Vector3I size) { return Rot2(YDown(pos, size), YDown(size, size)); }
        static Vector3I YDown3(Vector3I pos, Vector3I size) { return Rot3(YDown(pos, size), YDown(size, size)); }

        static Vector3I ZDown1(Vector3I pos, Vector3I size) { return Rot1(ZDown(pos, size), ZDown(size, size)); }
        static Vector3I ZDown2(Vector3I pos, Vector3I size) { return Rot2(ZDown(pos, size), ZDown(size, size)); }
        static Vector3I ZDown3(Vector3I pos, Vector3I size) { return Rot3(ZDown(pos, size), ZDown(size, size)); }

        int idx = 0;
        static RotateFunc[] funcs = new RotateFunc[] {
            XUp, XUp1, XUp2, XUp3, YUp, YUp1, YUp2, YUp3, ZUp, ZUp1, ZUp2, ZUp3,
            XDown, XDown1, XDown2, XDown3, YDown, YDown1, YDown2, YDown3, ZDown, ZDown1, ZDown2, ZDown3
        };
        enum BlockState {
            Empty, Destroyed, Damaged, Normal
        }
        BlockState[,,] saved_grid;
        Vector3I gridmin, gridmax;
        struct TileState {
            public int Healthy, Total;
            public int Depth;
        }
        int tilesize;
        TileState[,] tiles;

        int hull_percent = 0;

        IEnumerable<bool> CheckGrid(IMyCubeGrid grid, bool check_damaged) {
            if (check_damaged == false) {
                gridmin = grid.Min - Vector3I.One;
                gridmax = grid.Max + Vector3I.One;
                saved_grid = new BlockState[gridmax.X - gridmin.X, gridmax.Y - gridmin.Y, gridmax.Z - gridmin.Z];
                tilesize = Math.Max(gridmax.X - gridmin.X, Math.Max(gridmax.Y - gridmin.Y, gridmax.Z - gridmin.Z)) + 1;
                tiles = new TileState[tilesize, tilesize];
            }
            int total_healthy = 0, total = 0;
            for (int x = gridmin.X; x < gridmax.X; ++x) {
                for (int y = gridmin.Y; y < gridmax.Y; ++y) {
                    for (int z = gridmin.Z; z < gridmax.Z; ++z) {
                        Vector3I pos = new Vector3I(x, y, z);
                        if (check_damaged) {
                            if (saved_grid[x - gridmin.X, y - gridmin.Y, z - gridmin.Z] != BlockState.Empty) {
                                ++total;
                                if (!grid.CubeExists(pos))
                                    saved_grid[x - gridmin.X, y - gridmin.Y, z - gridmin.Z] = BlockState.Destroyed;
                                else
                                    ++total_healthy;
                            }
                        } else
                            saved_grid[x - gridmin.X, y - gridmin.Y, z - gridmin.Z] = grid.CubeExists(pos) ? BlockState.Normal : BlockState.Empty;
                    }
                }
                yield return true;
            }
            hull_percent = total > 0 ? total_healthy * 100 / total : 100;
        }

        IEnumerable<bool> Draw(MySpriteDrawFrame frame, RotateFunc swizzle, RectangleF screen) {
            Vector3I size = gridmax - gridmin;
            Vector3I sizecube = swizzle(gridmax - gridmin, size * 2);
            float scale = Math.Min(screen.Width, screen.Height) / Math.Max(sizecube.X, sizecube.Y);
            Vector2 blocksize = new Vector2(scale, scale);
            float xoff = (screen.Width - sizecube.X * scale) * 0.5f + screen.X;
            float yoff = (screen.Width - sizecube.Y * scale) * 0.5f + screen.Y;
            for (int x = 0; x <= sizecube.X; x++)
                for (int y = 0; y <= sizecube.Y; y++)
                    tiles[x, y] = new TileState();
            for (int x = gridmin.X; x < gridmax.X; ++x) {
                for (int y = gridmin.Y; y < gridmax.Y; ++y) {
                    for (int z = gridmin.Z; z < gridmax.Z; ++z) {
                        BlockState state = saved_grid[x - gridmin.X, y - gridmin.Y, z - gridmin.Z];
                        if (state != BlockState.Empty) {
                            Vector3I pos = new Vector3I(x, y, z);
                            Vector3I poscube = swizzle(pos - gridmin, size);
                            TileState tile = tiles[poscube.X, poscube.Y];
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
            for (int x = 0; x<=sizecube.X; x++) {
                for (int y = 0; y<=sizecube.Y; y++) {
                    TileState tile = tiles[x, y];
                    if (tile.Total == 0)
                        continue;
                    float depth = ((float)tile.Depth / (float)sizecube.Z);
                    depth = depth * depth * depth * depth + 0.05f;
                    float health = tile.Healthy / (float)tile.Total;
                    if (tile.Healthy < tile.Total)
                        health *= 0.5f;
                    frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(x * scale + xoff, y * scale + yoff), new Vector2(scale, scale), new Color(depth, depth * health, depth * health)));
                }
            }
            for (int i = 0; i < blocks.Count; ++i) {
                Vector3I poscube = swizzle(blocks[i].Position - gridmin, size);
                Vector3I possize = swizzle(blocks[i].Size, Vector3I.Zero);
                if (possize.X < 0) { poscube.X += possize.X + 1; possize.X = -possize.X; }
                if (possize.Y < 0) { poscube.Y += possize.Y + 1; possize.Y = -possize.Y; }
                if (possize.Z < 0) { poscube.Z += possize.Z + 1; possize.Z = -possize.Z; }
                frame.Add(new MySprite(SpriteType.TEXTURE, "SquareHollow", new Vector2((poscube.X + possize.X * 0.5f - 0.5f) * scale + xoff, (poscube.Y + possize.Y * 0.5f - 0.5f) * scale + yoff), new Vector2(possize.X * scale, possize.Y * scale), blocks[i].State == BlockState.Normal ? Color.Green : blocks[i].State == BlockState.Damaged ? Color.Yellow : Color.Red));
            }
        }

        struct TerminalBlockState {
            public Vector3I Position, Size;
            public BlockState State;
            public IMyTerminalBlock Block;
        }
        List<TerminalBlockState> blocks = new List<TerminalBlockState>();
        List<RectangleF> rects = new List<RectangleF>();
        List<IMyTerminalBlock> lcds = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> healthbars = new List<IMyTerminalBlock>();
        string health_string = "";
        bool want_reset = true;

        IEnumerable<bool> RunProgram() {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            IMyCubeGrid grid = Me.CubeGrid;
            if (want_reset) {
                Echo("Reset Grid!");
                want_reset = false;
                foreach (bool val in CheckGrid(grid, false))
                    yield return val;
                List<IMyTerminalBlock> full_blocks = new List<IMyTerminalBlock>();
                GridTerminalSystem.GetBlocks(full_blocks);
                blocks.Clear();
                for (int i = 0; i < full_blocks.Count; ++i)
                    if (full_blocks[i].IsFunctional && full_blocks[i].CubeGrid == grid)
                        blocks.Add(new TerminalBlockState { Position = full_blocks[i].Min, Size = full_blocks[i].Max - full_blocks[i].Min + Vector3I.One, State = BlockState.Normal, Block = full_blocks[i] });
                yield break;
            }
            //Update grid
            {
                idx = (idx + 1) % funcs.Length;
                GridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(lcds, block => block.CustomData.Contains("ShipLayout") && !block.CustomData.Contains("ShipLayoutHealth"));
                GridTerminalSystem.GetBlocksOfType<IMyTextSurface>(healthbars, block => block.CustomData.Contains("ShipLayoutHealth"));
                foreach (bool val in CheckGrid(grid, true))
                    yield return val;
                int terminal_healthy = 0;
                for (int i = 0; i < blocks.Count; ++i) {
                    bool exists = grid.CubeExists(blocks[i].Position);
                    bool working = blocks[i].Block.IsWorking;
                    if (exists)
                        ++terminal_healthy;
                    TerminalBlockState s = blocks[i];
                    s.State = exists && working ? BlockState.Normal : exists ? BlockState.Damaged : BlockState.Destroyed;
                    blocks[i] = s;
                }
                int terminal_percent = blocks.Count > 0 ? terminal_healthy * 100 / blocks.Count : 100;
                health_string = string.Format("Hull " + hull_percent.ToString("000") + "% Systems " + terminal_percent.ToString("000") + "%");
            }
            //Update healthbars
            for (int i = 0; i < healthbars.Count; ++i) {
                IMyTextSurface surf = (IMyTextSurface)healthbars[i];
                surf.ContentType = ContentType.TEXT_AND_IMAGE;
                surf.WriteText(health_string);
            }
            //Draw Program screen
            {
                IMyTextSurface surf = Me.GetSurface(0);
                surf.ContentType = ContentType.SCRIPT;
                MySpriteDrawFrame frame = surf.DrawFrame();
                foreach (bool val in Draw(frame, funcs[idx], new RectangleF(0f, 0f, surf.SurfaceSize.X, surf.SurfaceSize.Y)))
                    yield return val;
                frame.Add(MySprite.CreateText("ShipLayout by zanders3\nAdd ShipLayout " + idx + " or ShipLayoutHealth\nto CustomData on LCD\n" + health_string, "DEBUG", Color.Black));
                frame.Dispose();
            }
            //Draw LCD screen
            {
                RotateFunc swizzle = funcs[idx];
                for (int i = 0; i < lcds.Count; ++i) {
                    IMyTerminalBlock lcd = lcds[i];
                    string[] bits = lcd.CustomData.Split(' ');
                    if (bits.Length >= 2) {
                        int mode;
                        int.TryParse(bits[1], out mode);
                        swizzle = funcs[mode % funcs.Length];
                    }

                    IMyTextSurface surf = ((IMyTextSurfaceProvider)lcd).GetSurface(0);
                    surf.ContentType = ContentType.SCRIPT;
                    MySpriteDrawFrame frame = surf.DrawFrame();
                    foreach (bool val in Draw(frame, swizzle, new RectangleF(0f, 0f, surf.SurfaceSize.X, surf.SurfaceSize.Y)))
                        yield return val;
                    frame.Dispose();
                }
            }
            if (!want_reset)
                Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        IEnumerator<bool> current_work;

        public void Main(string argument, UpdateType updateSource) {
            Echo("View " + idx + " LCDs " + lcds.Count);
            Echo("Update Time: " + Runtime.LastRunTimeMs.ToString("F"));
            if (updateSource == UpdateType.Terminal)
                want_reset = true;
            if (current_work == null)
                current_work = RunProgram().GetEnumerator();
            if (current_work.MoveNext() == false) {
                current_work.Dispose();
                current_work = null;
            }
        }
    }
}