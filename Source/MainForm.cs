﻿using System;
using SkiaSharp;
using System.Diagnostics;
using System.Numerics;
using SkiaSharp.Views.Desktop;
using System.Text;
using System.Collections.ObjectModel;
using eft_dma_radar.Source.Tarkov;
using eft_dma_radar.Source;
using System.Runtime.InteropServices;

namespace eft_dma_radar
{
    public partial class MainForm : Form
    {
        private readonly Config _config;
        private readonly SKGLControl _mapCanvas;
        private readonly Stopwatch _fpsWatch = new();
        private readonly object _renderLock = new();
        private readonly System.Timers.Timer _mapChangeTimer = new(900);
        private readonly List<Map> _maps = new(); // Contains all maps from \\Maps folder

        private float _uiScale = 1.0f;
        private float _aimviewWindowSize = 200;
        private Player _closestToMouse = null;
        private int? _mouseOverGroup = null;
        private string _filterEntry = null;
        private int _fps = 0;
        private int _mapSelectionIndex = 0;
        private Map _selectedMap;
        private SKBitmap[] _loadedBitmaps;
        private MapPosition _mapPanPosition = new();

        #region Getters
        /// <summary>
        /// Radar has found Escape From Dyrkov process and is ready.
        /// </summary>
        private bool Ready
        {
            get => Memory.Ready;
        }
        /// <summary>
        /// Radar has found Local Game World.
        /// </summary>
        private bool InGame
        {
            get => Memory.InGame;
        }
        /// <summary>
        /// LocalPlayer (who is running Radar) 'Player' object.
        /// </summary>
        private Player LocalPlayer
        {
            get => Memory.Players?.FirstOrDefault(x => x.Value.Type is PlayerType.LocalPlayer).Value;
        }
        /// <summary>
        /// All Players in Local Game World (including dead/exfil'd) 'Player' collection.
        /// </summary>
        private ReadOnlyDictionary<string, Player> AllPlayers
        {
            get => Memory.Players;
        }
        /// <summary>
        /// Contains all loot in Local Game World.
        /// </summary>
        private LootManager Loot
        {
            get => Memory.Loot;
        }
        /// <summary>
        /// Contains all 'Hot' grenades in Local Game World, and their position(s).
        /// </summary>
        private ReadOnlyCollection<Grenade> Grenades
        {
            get => Memory.Grenades;
        }
        /// <summary>
        /// Radar is in the process of loading loot. Radar may be paused during this operation.
        /// </summary>
        private bool LoadingLoot
        {
            get => Memory.LoadingLoot;
        }
        /// <summary>
        /// Contains all 'Exfils' in Local Game World, and their status/position(s).
        /// </summary>
        private ReadOnlyCollection<Exfil> Exfils
        {
            get => Memory.Exfils;
        }
        #endregion

        #region Constructor
        /// <summary>
        /// GUI Constructor.
        /// </summary>
        public MainForm()
        {
            _config = Program.Config; // get ref to config
            InitializeComponent();
            // init skia
            _mapCanvas = new SKGLControl()
            {
                Size = new Size(50, 50),
                Dock = DockStyle.Fill,
                VSync = _config.Vsync // cap fps to refresh rate, reduce tearing
            };
            tabPage1.Controls.Add(_mapCanvas); // place Radar Map Canvas on top of TabPage1
            checkBox_MapFree.Parent = _mapCanvas; // change parent for checkBox_MapFree 'button'
            button_Loot.Parent = _mapCanvas; // change parent for button_LootFilter 'button'
            textBox_LootFilterByName.KeyDown += TextBox_LootFilterByName_KeyDown; // Handle enter keypress
            textBox_LootRegValue.KeyDown += TextBox_LootRegValue_KeyDown; // Handle enter keypress
            textBox_LootImpValue.KeyDown += TextBox_LootImpValue_KeyDown; // Handle enter keypress
            trackBar_UIScale.ValueChanged += TrackBar_UIScale_ValueChanged; // Handle UI Adjustments

            LoadConfig();
            LoadMaps();
            _mapChangeTimer.AutoReset = false;
            _mapChangeTimer.Elapsed += MapChangeTimer_Elapsed;

            this.DoubleBuffered = true; // Prevent flickering
            this.Shown += MainForm_Shown;
            _mapCanvas.PaintSurface += MapCanvas_PaintSurface; // Radar Drawing Event
            _mapCanvas.MouseMove += MapCanvas_MouseMove; // Handle mouseover events on radar
            tabControl1.SelectedIndexChanged += TabControl1_SelectedIndexChanged;
            _mapCanvas.MouseClick += MapCanvas_MouseClick;
            listView_PmcHistory.MouseDoubleClick += ListView_PmcHistory_MouseDoubleClick;
            _fpsWatch.Start(); // fps counter
        }

        #endregion

        [DllImport("user32.dll")]
        static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

        #region Events
        /// <summary>
        /// Event fires when MainForm becomes visible. Loops endlessly but is asynchronously non-blocking.
        /// </summary>
        private async void MainForm_Shown(object sender, EventArgs e)
        {
            //SetWindowDisplayAffinity(this.Handle, 0x00000011);
            while (_mapCanvas.GRContext is null) await Task.Delay(1);
            _mapCanvas.GRContext.SetResourceCacheLimit(503316480); // Fixes low FPS on big maps
            while (true)
            {
                //await Task.Run(() => Thread.SpinWait(8 * 50000)); // High performance async delay
                await Task.Run(() => Thread.Sleep(20));
                _mapCanvas.Refresh(); // draw next frame
            }
        }
        /// <summary>
        /// Event fires when switching Tab Pages.
        /// </summary>
        private void TabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedIndex == 2) // Player Loadouts Tab
            {
                richTextBox_PlayersInfo.Clear();
                var enemyPlayers = this.AllPlayers?.Select(x => x.Value)
                    .Where(x => x.IsHumanHostileActive)
                        .ToList()
                        .OrderBy(x => x.GroupID)
                        .ThenBy(x => x.Name);
                if (this.InGame && enemyPlayers is not null)
                {
                    var sb = new StringBuilder();
                    sb.Append(@"{\rtf1\ansi");
                    foreach (var player in enemyPlayers)
                    {
                        string title = $"*** {player.Name} ({player.Type})  L:{player.Lvl}";
                        if (player.GroupID != -1) title += $" G:{player.GroupID}";
                        if (player.KDA != -1f) title += $" KD{player.KDA.ToString("n1")}";
                        sb.Append(@$"\b {title} \b0 ");
                        sb.Append(@" \line ");
                        var gear = player.Gear; // cache ref
                        if (gear is not null) foreach (var slot in gear)
                            {
                                sb.Append(@$"\b {slot.Key}: \b0 ");
                                sb.Append(slot.Value.Long); // Use long item name
                                sb.Append(@" \line ");
                            }
                        else sb.Append(@" ERROR retrieving gear \line");
                        sb.Append(@" \line ");
                    }
                    sb.Append(@"}");
                    richTextBox_PlayersInfo.Rtf = sb.ToString();
                }
            }
            else if (tabControl1.SelectedIndex == 3) // Player History Tab
            {
                listView_PmcHistory.Items.Clear(); // Clear old view
                listView_PmcHistory.Items.AddRange(Player.History); // Obtain new view
                listView_PmcHistory.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent); // resize Player History columns automatically
            }
        }
        /// <summary>
        /// Fired when loot is toggled.
        /// </summary>
        private void checkBox_Loot_CheckedChanged(object sender, EventArgs e)
        {
            button_Loot.Visible = checkBox_Loot.Checked;
            groupBox_Loot.Visible = false;
            button_Loot.Enabled = true;
        }
        /// <summary>
        /// Fired when UI Scale Trackbar is Adjusted
        /// </summary>
        private void TrackBar_UIScale_ValueChanged(object sender, EventArgs e)
        {
            _uiScale = (.01f * trackBar_UIScale.Value);
            label_UIScale.Text = $"UI Scale {_uiScale.ToString("n2")}";
            #region UpdatePaints
            SKPaints.PaintMouseoverGroup.StrokeWidth = 3 * _uiScale;
            SKPaints.TextMouseoverGroup.TextSize = 12 * _uiScale;
            SKPaints.PaintLocalPlayer.StrokeWidth = 3 * _uiScale;
            SKPaints.PaintTeammate.StrokeWidth = 3 * _uiScale;
            SKPaints.TextTeammate.TextSize = 12 * _uiScale;
            SKPaints.PaintPMC.StrokeWidth = 3 * _uiScale;
            SKPaints.TextPMC.TextSize = 12 * _uiScale;
            SKPaints.PaintSpecial.StrokeWidth = 3 * _uiScale;
            SKPaints.TextSpecial.TextSize = 12 * _uiScale;
            SKPaints.PaintScav.StrokeWidth = 3 * _uiScale;
            SKPaints.TextScav.TextSize = 12 * _uiScale;
            SKPaints.PaintRaider.StrokeWidth = 3 * _uiScale;
            SKPaints.TextRaider.TextSize = 12 * _uiScale;
            SKPaints.PaintBoss.StrokeWidth = 3 * _uiScale;
            SKPaints.TextBoss.TextSize = 12 * _uiScale;
            SKPaints.PaintPScav.StrokeWidth = 3 * _uiScale;
            SKPaints.TextWhite.TextSize = 12 * _uiScale;
            SKPaints.PaintDeathMarker.StrokeWidth = 3 * _uiScale;
            SKPaints.PaintLoot.StrokeWidth = 3 * _uiScale;
            SKPaints.PaintImportantLoot.StrokeWidth = 3 * _uiScale;
            SKPaints.TextLoot.TextSize = 13 * _uiScale;
            SKPaints.TextImportantLoot.TextSize = 13 * _uiScale;
            SKPaints.PaintTransparentBacker.StrokeWidth = 1 * _uiScale;
            SKPaints.PaintAimviewCrosshair.StrokeWidth = 1 * _uiScale;
            SKPaints.PaintAimviewLocalPlayer.StrokeWidth = 1 * _uiScale;
            SKPaints.PaintAimviewPMC.StrokeWidth = 1 * _uiScale;
            SKPaints.PaintAimviewSpecial.StrokeWidth = 1 * _uiScale;
            SKPaints.PaintAimviewTeammate.StrokeWidth = 1 * _uiScale;
            SKPaints.PaintAimviewBoss.StrokeWidth = 1 * _uiScale;
            SKPaints.PaintAimviewScav.StrokeWidth = 1 * _uiScale;
            SKPaints.PaintAimviewRaider.StrokeWidth = 1 * _uiScale;
            SKPaints.PaintAimviewPScav.StrokeWidth = 1 * _uiScale;
            SKPaints.TextRadarStatus.TextSize = 48 * _uiScale;
            SKPaints.PaintGrenades.StrokeWidth = 3 * _uiScale;
            SKPaints.PaintExfilOpen.StrokeWidth = 1 * _uiScale;
            SKPaints.PaintExfilPending.StrokeWidth = 1 * _uiScale;
            SKPaints.PaintExfilClosed.StrokeWidth = 1 * _uiScale;
            #endregion
            _aimviewWindowSize = 200 * _uiScale;
        }

        /// <summary>
        /// Event fires when the "Map Free" or "Map Follow" checkbox (button) is clicked on the Main Window.
        /// </summary>
        private void checkBox_MapFree_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_MapFree.Checked)
            {
                checkBox_MapFree.Text = "Map Follow";
                lock (_renderLock)
                {
                    var localPlayer = this.LocalPlayer;
                    if (localPlayer is not null)
                    {
                        var localPlayerMapPos = localPlayer.Position.ToMapPos(_selectedMap);
                        _mapPanPosition = new MapPosition()
                        {
                            X = localPlayerMapPos.X,
                            Y = localPlayerMapPos.Y,
                            Height = localPlayerMapPos.Height
                        };
                    }
                }
            }
            else checkBox_MapFree.Text = "Map Free";
        }
        /// <summary>
        /// Handles mouse movement on Map Canvas, specifically checks if mouse moves close to a 'Player' position.
        /// </summary>
        private void MapCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.InGame) // Must be in-game
            {
                var players = this.AllPlayers?.Select(x => x.Value)
                    .Where(x => x.Type is not PlayerType.LocalPlayer &&
                    !x.HasExfild); // Get all players except LocalPlayer & Exfil'd Players
                if (players is not null && players.Any())
                {
                    var mouse = new Vector2(e.X, e.Y); // Get current mouse position in control
                    var closest = players.Aggregate(
                        (x1, x2) => Vector2.Distance(x1.ZoomedPosition, mouse)
                        < Vector2.Distance(x2.ZoomedPosition, mouse) ? x1 : x2); // Get object 'closest' to mouse position
                    if (closest is not null)
                    {
                        var dist = Vector2.Distance(closest.ZoomedPosition, mouse);
                        if (dist < 12) // See if 'closest object' is close enough.
                        {
                            _closestToMouse = closest; // Save ref to closest object
                            if (closest.IsHumanHostile
                                && closest.GroupID != -1)
                                _mouseOverGroup = closest.GroupID; // Set group ID for closest player(s)
                            else _mouseOverGroup = null; // Clear Group ID
                        }
                        else ClearRefs();
                    }
                    else ClearRefs();
                }
                else ClearRefs();
            }
            else ClearRefs();
            void ClearRefs()
            {
                _closestToMouse = null;
                _mouseOverGroup = null;
            }
        }

        /// <summary>
        /// Event fires when Map Setup box is checked/unchecked.
        /// </summary>
        private void checkBox_MapSetup_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_MapSetup.Checked)
            {
                groupBox_MapSetup.Visible = true;
                textBox_mapX.Text = _selectedMap.ConfigFile.X.ToString();
                textBox_mapY.Text = _selectedMap.ConfigFile.Y.ToString();
                textBox_mapScale.Text = _selectedMap.ConfigFile.Scale.ToString();
                textBox_R.Text = _selectedMap.ConfigFile.R.ToString();
            }
            else groupBox_MapSetup.Visible = false;
        }

        /// <summary>
        /// Event fires when Restart Game button is clicked in Settings.
        /// </summary>
        private void button_Restart_Click(object sender, EventArgs e)
        {
            Memory.Restart();
        }
        /// <summary>
        /// Event fires when Refresh Loot button is clicked in Settings.
        /// </summary>
        private void button_RefreshLoot_Click(object sender, EventArgs e)
        {
            try { Memory.RefreshLoot(); }
            finally 
            {
                button_Loot.Enabled = true;
                groupBox_Loot.Visible = false;
            }
        }

        /// <summary>
        /// Event fires when Apply button is clicked in the "Map Setup Groupbox".
        /// </summary>
        private void button_MapSetupApply_Click(object sender, EventArgs e)
        {
            if (float.TryParse(textBox_mapX.Text, out float x) &&
                float.TryParse(textBox_mapY.Text, out float y) &&
                float.TryParse(textBox_mapScale.Text, out float scale) &&
                float.TryParse(textBox_R.Text, out float r))
            {
                lock (_renderLock)
                {
                    _selectedMap.ConfigFile.X = x;
                    _selectedMap.ConfigFile.Y = y;
                    _selectedMap.ConfigFile.Scale = scale;
                    _selectedMap.ConfigFile.R = r;
                    _selectedMap.ConfigFile.Save(_selectedMap);
                }
            }
            else
            {
                throw new Exception("INVALID float values in Map Setup.");
            }
        }

        /// <summary>
        /// Allows panning the map when in "Free" mode.
        /// </summary>
        private void MapCanvas_MouseClick(object sender, MouseEventArgs e)
        {
            if (checkBox_MapFree.Checked)
            {
                var center = new SKPoint(_mapCanvas.Width / 2, _mapCanvas.Height / 2); // Get center of canvas
                lock (_renderLock)
                {
                    _mapPanPosition = new MapPosition() // Pan based on distance/direction from center
                    {
                        X = _mapPanPosition.X + (e.X - center.X),
                        Y = _mapPanPosition.Y + (e.Y - center.Y)
                    };
                }
            }
            if (groupBox_Loot.Visible) // Close loot window
            {
                groupBox_Loot.Visible = false;
                button_Loot.Enabled = true;
            }
        }
        /// <summary>
        /// Executes map change after a short delay, in case switching through maps quickly to reduce UI lag.
        /// </summary>
        private void MapChangeTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.BeginInvoke(new MethodInvoker(delegate
            {
                button_Map.Enabled = false;
                button_Map.Text = "Loading...";
            }));
            lock (_renderLock)
            {
                try
                {
                    _selectedMap = _maps[_mapSelectionIndex]; // Swap map
                    if (_loadedBitmaps is not null)
                    {
                        foreach (var bitmap in _loadedBitmaps) bitmap?.Dispose(); // Cleanup resources
                    }
                    _loadedBitmaps = new SKBitmap[_selectedMap.ConfigFile.Maps.Count];
                    for (int i = 0; i < _loadedBitmaps.Length; i++)
                    {
                        using (var stream = File.Open(_selectedMap.ConfigFile.Maps[i].Item2, FileMode.Open, FileAccess.Read))
                        {
                            _loadedBitmaps[i] = SKBitmap.Decode(stream); // Load new bitmap(s)
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"ERROR loading {_selectedMap.ConfigFile.Maps[0].Item2}: {ex}");
                }
                finally
                {
                    this.BeginInvoke(new MethodInvoker(delegate
                    {
                        button_Map.Enabled = true;
                        button_Map.Text = "Toggle Map (F5)";
                    }));
                }
            }
        }

        /// <summary>
        /// Event fires when the Map button is clicked in Settings.
        /// </summary>
        private void button_Map_Click(object sender, EventArgs e)
        {
            ToggleMap();
        }

        /// <summary>
        /// Copies Player "BSG ID" to Clipboard upon double clicking History Entry.
        /// </summary>
        private void ListView_PmcHistory_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            var info = listView_PmcHistory.HitTest(e.X, e.Y);
            var view = info.Item;
            if (view is not null)
            {
                var entry = (PlayerHistoryEntry)view.Tag;
                if (entry is not null)
                {
                    var acctId = entry.ToString();
                    if (acctId is not null && acctId != string.Empty)
                    {
                        Clipboard.SetText(acctId); // Copy BSG ID to clipboard
                        MessageBox.Show($"Copied '{acctId}' to Clipboard!");
                    }
                }
            }
        }
        /// <summary>
        /// Fired when 'Loot' button is pressed in main radar window.
        /// </summary>
        private void button_LootFilter_Click(object sender, EventArgs e)
        {
            button_Loot.Enabled = false;
            groupBox_Loot.Visible = true;
        }
        /// <summary>
        /// Fired when 'Regular' Loot Value is changed.
        /// </summary>
        private void textBox_LootRegValue_TextChanged(object sender, EventArgs e)
        {
            if (!int.TryParse(textBox_LootRegValue.Text, out var i)) textBox_LootRegValue.Text = "0";
            button_LootApply.Enabled = true;
        }
        /// <summary>
        /// Fired when 'Important' Loot Value is changed.
        /// </summary>
        private void textBox_LootImpValue_TextChanged(object sender, EventArgs e)
        {
            if (!int.TryParse(textBox_LootImpValue.Text, out var i)) textBox_LootImpValue.Text = "0";
            button_LootApply.Enabled = true;
        }
        /// <summary>
        /// Fired when textBox_LootFilterByName is changed.
        /// </summary>
        private void textBox_LootFilterByName_TextChanged(object sender, EventArgs e)
        {
            button_LootApply.Enabled = true;
            button_LootApply.Text = "Apply";
        }
        /// <summary>
        /// Fired when 'Apply' button is pressed in Loot Filter Window.
        /// </summary>
        private void button_LootApply_Click(object sender, EventArgs e)
        {
            LootApply();
        }
        /// <summary>
        /// Handles enter keypress on TextBox_LootFilterByName
        /// </summary>
        private void TextBox_LootFilterByName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode is Keys.Enter)
            {
                LootApply();
            }
        }
        /// <summary>
        /// Handles enter keypress on TextBox_LootImpValue
        /// </summary>
        private void TextBox_LootImpValue_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode is Keys.Enter)
            {
                LootApply();
            }
        }
        /// <summary>
        /// Handles enter keypress on TextBox_LootRegValue
        /// </summary>
        private void TextBox_LootRegValue_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode is Keys.Enter)
            {
                LootApply();
            }
        }

        #endregion

        #region Methods
        /// <summary>
        /// Load previously set GUI Config values. Run at startup.
        /// </summary>
        private void LoadConfig()
        {
            trackBar_AimLength.Value = _config.PlayerAimLineLength;
            checkBox_Loot.Checked = _config.LootEnabled;
            checkBox_Aimview.Checked = _config.AimViewEnabled;
            checkBox_HideNames.Checked = _config.HideNames;
            trackBar_Zoom.Value = _config.DefaultZoom;
            trackBar_UIScale.Value = _config.UIScale;
            textBox_PrimTeamID.Text = _config.PrimaryTeammateId;
            textBox_LootRegValue.Text = _config.MinLootValue.ToString();
            textBox_LootImpValue.Text = _config.MinImportantLootValue.ToString();
        }

        /// <summary>
        /// Load map files (.PNG) and Configs (.JSON) from \\Maps folder. Run at startup.
        /// </summary>
        private void LoadMaps()
        {
            var dir = new DirectoryInfo($"{Environment.CurrentDirectory}\\Maps");
            if (!dir.Exists) dir.Create();
            var configs = dir.GetFiles("*.json"); // Get all PNG Files
            if (configs.Length == 0) throw new IOException("No .json map configs found!");
            
            foreach (var config in configs)
            {
                var name = Path.GetFileNameWithoutExtension(config.Name); // map name ex. 'CUSTOMS' w/o extension
                var map = new Map(name.ToUpper(),
                    MapConfig.LoadFromFile(config.FullName),
                    config.FullName);
                map.ConfigFile.Maps = map.ConfigFile.Maps.OrderBy(x => x.Item1).ToList(); // 'Lowest' Height starting at Index 0
                _maps.Add(map);
                Program.Log(name);
            }
            try
            {
                _selectedMap = _maps[0];
                _loadedBitmaps = new SKBitmap[_selectedMap.ConfigFile.Maps.Count];
                for (int i = 0; i < _loadedBitmaps.Length; i++)
                {
                    using (var stream = File.Open(_selectedMap.ConfigFile.Maps[i].Item2, FileMode.Open, FileAccess.Read))
                    {
                        _loadedBitmaps[i] = SKBitmap.Decode(stream);
                    }
                }
                tabPage1.Text = $"Radar ({_selectedMap.Name})";
            }
            catch (Exception ex)
            {
                throw new Exception($"ERROR loading initial map: {ex}");
            }
        }
        /// <summary>
        /// Zooms the bitmap 'in'.
        /// </summary>
        private void ZoomIn(int amt)
        {
            if (trackBar_Zoom.Value - amt >= 1) trackBar_Zoom.Value -= amt;
            else trackBar_Zoom.Value = 1;
        }
        /// <summary>
        /// Zooms the bitmap 'out'.
        /// </summary>
        private void ZoomOut(int amt)
        {
            if (trackBar_Zoom.Value + amt <= 200) trackBar_Zoom.Value += amt;
            else trackBar_Zoom.Value = 200;
        }
        /// <summary>
        /// Provides miscellaneous map parameters used throughout the entire render.
        /// </summary>
        private MapParameters GetMapParameters(MapPosition localPlayerPos)
        {
            int mapLayerIndex = 0;
            for (int i = _loadedBitmaps.Length; i > 0; i--)
            {
                if (localPlayerPos.Height > _selectedMap.ConfigFile.Maps[i - 1].Item1)
                {
                    mapLayerIndex = i - 1;
                    break;
                }
            }
            var zoomWidth = _loadedBitmaps[mapLayerIndex].Width * (.01f * trackBar_Zoom.Value);
            var zoomHeight = _loadedBitmaps[mapLayerIndex].Height * (.01f * trackBar_Zoom.Value);

            var bounds = new SKRect(localPlayerPos.X - zoomWidth / 2,
                localPlayerPos.Y - zoomHeight / 2,
                localPlayerPos.X + zoomWidth / 2,
                localPlayerPos.Y + zoomHeight / 2)
                .AspectFill(_mapCanvas.CanvasSize);

            return new MapParameters()
            {
                UIScale = _uiScale,
                MapLayerIndex = mapLayerIndex,
                Bounds = bounds,
                XScale = (float)_mapCanvas.Width / (float)bounds.Width, // Set scale for this frame
                YScale = (float)_mapCanvas.Height / (float)bounds.Height // Set scale for this frame
            };
        }

        /// <summary>
        /// Determines if an aggressor player is facing a friendly player.
        /// </summary>
        private static bool IsAggressorFacingTarget(SKPoint aggressor, float aggressorDegrees, SKPoint target, float distance)
        {
            double maxDiff = 31.3573 - 3.51726 * Math.Log(Math.Abs(0.626957 - 15.6948 * distance)); // Max degrees variance based on distance variable
            if (maxDiff < 1f) maxDiff = 1f; // Non linear equation, handle low/negative results
            var radians = Math.Atan2(target.Y - aggressor.Y, target.X - aggressor.X); // radians
            var degs = radians.ToDegrees();
            if (degs < 0) degs += 360f; // handle if negative
            var diff = Math.Abs(degs - aggressorDegrees); // Get angular difference (in degrees)
            return diff <= maxDiff; // See if calculated degrees is within max difference
        }

        /// <summary>
        /// Toggles currently selected map.
        /// </summary>
        private void ToggleMap()
        {
            if (!button_Map.Enabled) return;
            if (_mapSelectionIndex == _maps.Count - 1) _mapSelectionIndex = 0; // Start over when end of maps reached
            else _mapSelectionIndex++; // Move onto next map
            tabPage1.Text = $"Radar ({_maps[_mapSelectionIndex].Name})";
            _mapChangeTimer.Restart(); // Start delay
        }
        /// <summary>
        /// Checks if item is important.
        /// </summary>
        private bool IsItemImportant(LootItem item)
        {
            if (_filterEntry is null ||
                _filterEntry.Trim() == string.Empty)
            {
                if (item.isImportant(_config.MinImportantLootValue, _config.MinImportantLootValuePerSlot)) return true;
                else return false;
            }
            if (item.Important) return true;
            return false;
        }
        /// <summary>
        /// Returns proper label for Item.
        /// </summary>
        private string GetItemLabel(LootItem item)
        {
            if (_filterEntry is null ||
                _filterEntry.Trim() == string.Empty)
            {
                if (item.Label is not null) return item.Label;
            }
            if (item.AlwaysShow) return item.Label ?? "null";
            return item.Item?.shortName ?? "null";
        }
        /// <summary>
        /// Runs/Updates Loot Filter.
        /// </summary>
        private void LootApply()
        {
            try
            {
                if (button_LootApply.Text == "Clear") textBox_LootFilterByName.Text = null; // Clear 'named filter'
                groupBox_Loot.Visible = false;
                _config.MinLootValue = int.Parse(textBox_LootRegValue.Text);
                _config.MinImportantLootValue = int.Parse(textBox_LootImpValue.Text);
                _config.MinImportantLootValuePerSlot = int.Parse(textBox_lootImportantPerSlot.Text);
                textBox_LootFilterByName.Text = textBox_LootFilterByName.Text?.Trim(); // Trim spaces
                _filterEntry = new string(textBox_LootFilterByName.Text); // deep copy string
                this.Loot?.ApplyFilter(_filterEntry);
            }
            finally
            {
                button_Loot.Enabled = true;
                if (_filterEntry is null || _filterEntry == string.Empty)
                {
                    button_LootApply.Text = "Apply";
                    button_LootApply.Enabled = false;
                }
                else button_LootApply.Text = "Clear";
            }
        }
        #endregion

        #region Render
        /// <summary>
        /// Main Render Event.
        /// </summary>
        private void MapCanvas_PaintSurface(object sender, SKPaintGLSurfaceEventArgs e)
        {
            lock (_renderLock) // Acquire lock on 'Render Resources'
            {
                bool isReady = this.Ready; // cache bool
                bool inGame = this.InGame; // cache bool
                var localPlayer = this.LocalPlayer; // cache ref to current player
                if (_fpsWatch.ElapsedMilliseconds >= 1000)
                {
                    _mapCanvas.GRContext.PurgeResources(); // Seems to fix mem leak issue on increasing resource cache
                    string title = "Dyrkov Pidar";
                    if (inGame && localPlayer is not null)
                    {
                        title += $" ({_fps} fps) ({Memory.Ticks} mem/s)";
                        if (this.LoadingLoot) title += " - LOADING LOOT";
                    }
                    this.Text = title; // Set window title
                    _fpsWatch.Restart();
                    _fps = 0;
                }
                else _fps++;
                SKSurface surface = e.Surface;
                SKCanvas canvas = surface.Canvas;
                canvas.Clear();
                try
                {
                    if (inGame && localPlayer is not null)
                    {
                        var closestToMouse = _closestToMouse; // cache ref
                        var mouseOverGrp = _mouseOverGroup; // cache value for entire render
                        // Get main player location
                        var localPlayerPos = localPlayer.Position;
                        // Program.Log($"Player pos: {localPlayer.Position.X}, {localPlayer.Position.Y}");
                        var localPlayerMapPos = localPlayerPos.ToMapPos(_selectedMap);
                        if (groupBox_MapSetup.Visible) // Print coordinates (to make it easy to setup JSON configs)
                        {
                            label_Pos.Text = $"Unity X,Y,Z: {localPlayerPos.X},{localPlayerPos.Y},{localPlayerPos.Z}";
                        }

                        // Prepare to draw Game Map
                        MapParameters mapParams; // Drawing Source
                        if (checkBox_MapFree.Checked) // Map fixed location, click to pan map
                        {
                            _mapPanPosition.Height = localPlayerMapPos.Height;
                            mapParams = GetMapParameters(_mapPanPosition);
                        }
                        else mapParams = GetMapParameters(localPlayerMapPos); // Map auto follow LocalPlayer
                        var mapCanvasBounds = new SKRect() // Drawing Destination
                        {
                            Left = _mapCanvas.Left,
                            Right = _mapCanvas.Right,
                            Top = _mapCanvas.Top,
                            Bottom = _mapCanvas.Bottom
                        };
                        // Draw Game Map
                        canvas.DrawBitmap(_loadedBitmaps[mapParams.MapLayerIndex], mapParams.Bounds, mapCanvasBounds, SKPaints.PaintBitmap);

                        // Draw LocalPlayer Scope
                        {
                            var localPlayerZoomedPos = localPlayerMapPos.ToZoomedPos(mapParams); // always true
                            localPlayerZoomedPos.DrawPlayerMarker(canvas, localPlayer, _selectedMap, trackBar_AimLength.Value, null);
                        }

                        // Draw other players
                        var allPlayers = this.AllPlayers?.Select(x => x.Value)
                            .Where(x => !x.HasExfild); // Skip exfil'd players
                        var friendlies = allPlayers?.Where(x => x.IsFriendlyActive);
                        if (allPlayers is not null)
                        {
                            foreach (var player in allPlayers) // Draw PMCs
                            {
                                if (player.Type is PlayerType.LocalPlayer) continue; // Already drawn current player, move on
                                var playerPos = player.Position;
                                var playerMapPos = playerPos.ToMapPos(_selectedMap);
                                var playerZoomedPos = playerMapPos.ToZoomedPos(mapParams);
                                player.ZoomedPosition = new Vector2() // Cache Position as Vec2 for MouseMove event
                                {
                                    X = playerZoomedPos.X,
                                    Y = playerZoomedPos.Y
                                };
                                int aimlineLength = 15;
                                if (player.IsAlive is false)
                                { // Draw 'X' death marker
                                    playerZoomedPos.DrawDeathMarker(canvas);
                                    continue;
                                }
                                else if (player.Type is not PlayerType.Teammate)
                                {
                                    if (friendlies is not null) foreach (var friendly in friendlies)
                                    {
                                        var friendlyPos = friendly.Position;
                                        var friendlyDist = Vector3.Distance(playerPos, friendlyPos);
                                        if (friendlyDist > _config.MaxDistance) continue; // max range, no lines across entire map
                                        var friendlyMapPos = friendlyPos.ToMapPos(_selectedMap);
                                        if (IsAggressorFacingTarget(playerMapPos.GetPoint(),
                                            player.Rotation.X,
                                            friendlyMapPos.GetPoint(),
                                            friendlyDist))
                                        {
                                            aimlineLength = 1000; // Lengthen aimline
                                            break;
                                        }
                                    }
                                }
                                else if (player.Type is PlayerType.Teammate)
                                {
                                    aimlineLength = trackBar_AimLength.Value; // Allies use player's aim length
                                }
                                // Draw Player Scope
                                {
                                    var height = playerMapPos.Height - localPlayerMapPos.Height;
                                    string[] lines = null;
                                    if (!checkBox_HideNames.Checked) // show full names & info
                                    {
                                        var dist = Vector3.Distance(localPlayerPos, playerPos);
                                        lines = new string[2]
                                        {
                                            string.Empty,
                                            $"H: {(int)Math.Round(height)} D: {(int)Math.Round(dist)}"
                                        };
                                        string name = player.Name;
                                        if (player.ErrorCount > 10) name = "ERROR"; // In case POS stops updating, let us know!
                                        lines[0] += $"{name} ({player.Health})";
                                    }
                                    else // just height & hp (for humans)
                                    {
                                        lines = new string[1]
                                        {
                                            $"H: {(int)Math.Round(height)}"
                                        };
                                        if (player.IsHuman) lines[0] += $" ({player.Health})";
                                        if (player.ErrorCount > 10) lines[0] = "ERROR"; // In case POS stops updating, let us know!
                                    }
                                    playerZoomedPos.DrawPlayerText(canvas, player, lines, mouseOverGrp);
                                    playerZoomedPos.DrawPlayerMarker(canvas, player, _selectedMap, aimlineLength, mouseOverGrp);
                                }
                            }
                            if (checkBox_Loot.Checked) // Draw loot (if enabled)
                            {
                                var loot = this.Loot; // cache ref
                                if (loot is not null)
                                {
                                    if (Loot.Filter is null)
                                    {
                                        Loot.ApplyFilter(_filterEntry);
                                    }
                                    var filter = Loot.Filter; // Get ref to collection
                                    if (filter is not null) foreach (var item in filter)
                                        {
                                            var itemZoomedPos = item.Position.ToMapPos(_selectedMap).ToZoomedPos(mapParams);
                                            itemZoomedPos.DrawLoot(canvas, GetItemLabel(item), IsItemImportant(item), item.Position.Z - localPlayerMapPos.Height);
                                        }
                                }
                            }
                            var grenades = this.Grenades; // cache ref
                            if (grenades is not null) // Draw grenades
                            {
                                foreach (var grenade in grenades)
                                {
                                    var grenadeZoomedPos = grenade.Position.ToMapPos(_selectedMap).ToZoomedPos(mapParams);
                                    grenadeZoomedPos.DrawGrenade(canvas);
                                }
                            }
                            var exfils = this.Exfils; // cache ref
                            if (exfils is not null)
                            {
                                foreach (var exfil in exfils)
                                {
                                    var exfilZoomedPos = exfil.Position.ToMapPos(_selectedMap).ToZoomedPos(mapParams);
                                    exfilZoomedPos.DrawExfil(canvas, exfil, localPlayerMapPos.Height);
                                }
                            }
                        }
                        if (checkBox_Aimview.Checked) // Aimview Drawing
                        {
                            var aimviewPlayers = allPlayers?.Where(x => x.IsActive && x.IsAlive); // get all alive & active players
                            if (aimviewPlayers is not null)
                            {
                                var localPlayerAimviewBounds = new SKRect() // bottom left of screen
                                {
                                    Left = _mapCanvas.Left,
                                    Right = _mapCanvas.Left + _aimviewWindowSize,
                                    Bottom = _mapCanvas.Bottom,
                                    Top = _mapCanvas.Bottom - _aimviewWindowSize
                                };
                                var primaryTeammateAimviewBounds = new SKRect() // bottom right of screen
                                {
                                    Left = _mapCanvas.Right - _aimviewWindowSize,
                                    Right = _mapCanvas.Right,
                                    Bottom = _mapCanvas.Bottom,
                                    Top = _mapCanvas.Bottom - _aimviewWindowSize
                                };
                                var primaryTeammate = friendlies?.FirstOrDefault(x =>
                                x.AccountID == textBox_PrimTeamID.Text); // Find Primary Teammate
                                // Draw LocalPlayer Aimview
                                RenderAimview(canvas, localPlayerAimviewBounds, localPlayer, aimviewPlayers);
                                // Draw Primary Teammate Aimview
                                RenderAimview(canvas, primaryTeammateAimviewBounds, primaryTeammate, aimviewPlayers);
                            }
                        }
                        if (closestToMouse is not null) // draw tooltip for player the mouse is closest to
                        {
                            var playerZoomedPos = closestToMouse.Position.ToMapPos(_selectedMap).ToZoomedPos(mapParams);
                            playerZoomedPos.DrawTooltip(canvas, closestToMouse);
                        }
                    }
                    else // Not rendering, display reason
                    {
                        if (!isReady)
                            canvas.DrawText("Game Process Not Running", _mapCanvas.Width / 2, _mapCanvas.Height / 2, SKPaints.TextRadarStatus);
                        else if (!inGame)
                            canvas.DrawText("Waiting for Raid Start...", _mapCanvas.Width / 2, _mapCanvas.Height / 2, SKPaints.TextRadarStatus);
                        else if (localPlayer is null)
                            canvas.DrawText("Cannot find LocalPlayer", _mapCanvas.Width / 2, _mapCanvas.Height / 2, SKPaints.TextRadarStatus);
                    }
                } catch { }
                canvas.Flush(); // commit to GPU
            }
        }

        /// <summary>
        /// Renders an Aimview Window with the specified parameters.
        /// </summary>
        /// <param name="canvas">SKCanvas reference for drawing.</param>
        /// <param name="drawingLocation">Rectangular (Square) location on the SKCanvas to draw.</param>
        /// <param name="sourcePlayer">The player whom the Aimview will have 'point of view'.</param>
        /// <param name="aimviewPlayers">Collection of players to render in the AimView window.</param>
        private void RenderAimview(SKCanvas canvas, SKRect drawingLocation, Player sourcePlayer, IEnumerable<Player> aimviewPlayers)
        {
            try
            {
                if (sourcePlayer is not null && sourcePlayer.IsActive && sourcePlayer.IsAlive)
                {
                    var myPosition = sourcePlayer.Position;
                    var myRotation = sourcePlayer.Rotation;
                    canvas.DrawRect(drawingLocation, SKPaints.PaintTransparentBacker); // draw backer
                    if (aimviewPlayers is not null)
                    {
                        var normalizedDirection = -myRotation.X;
                        if (normalizedDirection < 0) normalizedDirection += 360;

                        var pitch = myRotation.Y;
                        if (pitch >= 270)
                        {
                            pitch = 360 - pitch;
                        }
                        else
                        {
                            pitch = -pitch;
                        }
                        foreach (var player in aimviewPlayers)
                        {
                            if (player == sourcePlayer) continue; // don't draw self
                            var playerPos = player.Position;
                            float dist = Vector3.Distance(myPosition, playerPos);
                            if (dist > _config.MaxDistance) continue; // Only draw within range
                            float heightDiff = playerPos.Z - myPosition.Z;
                            float angleY = (float)(180 / Math.PI * Math.Atan(heightDiff / dist)) - pitch;
                            float y = angleY / _config.AimViewFOV * _aimviewWindowSize + _aimviewWindowSize / 2;

                            float opposite = playerPos.Y - myPosition.Y;
                            float adjacent = playerPos.X - myPosition.X;
                            float angleX = (float)(180 / Math.PI * Math.Atan(opposite / adjacent));

                            if (adjacent < 0 && opposite > 0)
                            {
                                angleX += 180;
                            }
                            else if (adjacent < 0 && opposite < 0)
                            {
                                angleX += 180;
                            }
                            else if (adjacent > 0 && opposite < 0)
                            {
                                angleX += 360;
                            }
                            // Handle split planes (source/target each on a different side of 0 / 360 )
                            if (angleX >= 360 - _config.AimViewFOV && normalizedDirection <= _config.AimViewFOV)
                            {
                                var diff = 360 + normalizedDirection;
                                angleX -= diff;
                            }
                            else if (angleX <= _config.AimViewFOV && normalizedDirection >= 360 - _config.AimViewFOV)
                            {
                                var diff = 360 - normalizedDirection;
                                angleX += diff;
                            }
                            else angleX -= normalizedDirection;
                            float x = angleX / _config.AimViewFOV * _aimviewWindowSize + _aimviewWindowSize / 2;

                            float drawX = drawingLocation.Right - x;
                            float drawY = drawingLocation.Bottom - y;
                            if (drawX > drawingLocation.Right || drawX < drawingLocation.Left ||
                                drawY < drawingLocation.Top || drawY > drawingLocation.Bottom)
                                continue; // not in FOV
                            float circleSize = (float)(31.6437 - 5.09664 * Math.Log(0.591394 * dist + 70.0756));
                            canvas.DrawCircle(drawX, drawY, circleSize * _uiScale, player.GetAimviewPaint());
                        }
                    }
                    // draw crosshair at end
                    canvas.DrawLine(drawingLocation.Left, drawingLocation.Bottom - (_aimviewWindowSize / 2), drawingLocation.Right, drawingLocation.Bottom - (_aimviewWindowSize / 2), SKPaints.PaintAimviewCrosshair);
                    canvas.DrawLine(drawingLocation.Right - (_aimviewWindowSize / 2), drawingLocation.Top, drawingLocation.Right - (_aimviewWindowSize / 2), drawingLocation.Bottom, SKPaints.PaintAimviewCrosshair);
                }
            }
            catch { }
        }
        #endregion

        #region Overrides
        /// <summary>
        /// Form closing event.
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            e.Cancel = true; // Cancel shutdown
            this.Enabled = false; // Lock window
            _config.PlayerAimLineLength = trackBar_AimLength.Value;
            _config.LootEnabled = checkBox_Loot.Checked;
            _config.AimViewEnabled = checkBox_Aimview.Checked;
            _config.HideNames = checkBox_HideNames.Checked;
            _config.DefaultZoom = trackBar_Zoom.Value;
            _config.UIScale = trackBar_UIScale.Value;
            _config.PrimaryTeammateId = textBox_PrimTeamID.Text;
            Config.SaveConfig(_config); // Save Config to Config.json
            Memory.Shutdown(); // Wait for Memory Thread to gracefully exit
            e.Cancel = false; // Ready to close
            base.OnFormClosing(e); // Proceed with closing
        }

        /// <summary>
        /// Process hotkey presses.
        /// </summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.F1))
            {
                ZoomIn(5);
                return true;
            }
            else if (keyData == (Keys.F2))
            {
                if (InGame)
                {
                    Memory.Game.FPSCamera.ToggleThermalVision();
                }
                //ZoomOut(5);
                return true;    
            }
            else if (keyData == (Keys.F3))
            {
                this.checkBox_Loot.Checked = !this.checkBox_Loot.Checked; // Toggle loot
                _config.LootEnabled = checkBox_Loot.Checked;
                return true;
            }
            else if (keyData == (Keys.F4))
            {
                this.checkBox_Aimview.Checked = !this.checkBox_Aimview.Checked; // Toggle aimview
                return true;
            }
            else if (keyData == (Keys.F5))
            {
                ToggleMap(); // Toggle to next map
                return true;
            }
            else if (keyData == (Keys.F6))
            {
                checkBox_HideNames.Checked = !checkBox_HideNames.Checked; // Toggle Hide Names
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        /// <summary>
        /// Process mousewheel events.
        /// </summary>
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (tabControl1.SelectedIndex == 0) // Main Radar Tab should be open
            {
                if (e.Delta > 0) // mouse wheel up (zoom in)
                {
                    int amt = (e.Delta / SystemInformation.MouseWheelScrollDelta) * 5; // Calculate zoom amount based on number of deltas
                    ZoomIn(amt);
                    return;
                }
                else if (e.Delta < 0) // mouse wheel down (zoom out)
                {
                    int amt = (e.Delta / -SystemInformation.MouseWheelScrollDelta) * 5; // Calculate zoom amount based on number of deltas
                    ZoomOut(amt);
                    return;
                }
            }
            base.OnMouseWheel(e);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }

        private void groupBox_MapSetup_Enter(object sender, EventArgs e)
        {

        }

        private void bindingSource1_CurrentChanged(object sender, EventArgs e)
        {

        }

        private void groupBox_Loot_Enter(object sender, EventArgs e)
        {

        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }

        private void lootWalls_CheckedChanged(object sender, EventArgs e)
        {
            if (this.LootWalls.Checked)
            {
                
            } else
            {

            }
        }

        private void groupBox_MapSetup_Enter_1(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void label10_Click(object sender, EventArgs e)
        {

        }

        private void textBox_mapScale_TextChanged(object sender, EventArgs e)
        {

        }

        private void ThermalVision_CheckedChanged(object sender, EventArgs e)
        {
            if (InGame)
            {
                Memory.Game.FPSCamera.ToggleThermalVision();
            }
        }

        private void MaxStamina_CheckedChanged(object sender, EventArgs e)
        {
            if (InGame && LocalPlayer != null && LocalPlayer.Base != 0 && MaxStamina.Checked)
            {
                LocalPlayer.ToggleMaxStamina();
            }
        }

        private void NoRecoil_CheckedChanged(object sender, EventArgs e)
        {
            if (InGame && LocalPlayer != null && LocalPlayer.Base != 0)
            {
                LocalPlayer.noRecoil = NoRecoil.Checked;
            }
        }

        private void groupBox_Loot_Enter_1(object sender, EventArgs e)
        {

        }

        private Thread kekWorker;
        private void kek_CheckedChanged(object sender, EventArgs e)
        {
            if (Kek.kek == null)
            {
                kekWorker = new Thread(() =>
                {
                    var KekForm = new Kek();
                    KekForm.ShowDialog();
                })
                {
                    IsBackground = true,
                    //Priority = ThreadPriority.AboveNormal
                };
                kekWorker.Start();
            }
            if (Kek.kek != null)
            {
                if (kek.Checked)
                {
                    //Kek.kek.Invoke(new MethodInvoker(Kek.kek.Show));
                }
                else
                {
                    Kek.kek.Invoke(new MethodInvoker(Kek.kek.Close));
                    Kek.kek = null; 
                }
            }
        }

        private void NoRecoil_CheckedChanged_1(object sender, EventArgs e)
        {
            NoRecoil_CheckedChanged(sender, e);
        }

        private void ThermalVision_CheckedChanged_1(object sender, EventArgs e)
        {
            ThermalVision_CheckedChanged(sender, e);
        }

        private void MaxStamina_CheckedChanged_1(object sender, EventArgs e)
        {
            MaxStamina_CheckedChanged(sender, e);
        }

        private void groupBox_Loot_Enter_2(object sender, EventArgs e)
        {

        }

        private void textBox_lootImportantPerSlot_TextChanged(object sender, EventArgs e)
        {
            if (!int.TryParse(textBox_lootImportantPerSlot.Text, out var i)) textBox_lootImportantPerSlot.Text = "0";
            button_LootApply.Enabled = true;
        }

        private void textBox_lootImportantPerSlot_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode is Keys.Enter)
            {
                LootApply();
            }
        }
    }
    #endregion
}
