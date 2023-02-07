using SkiaSharp.Views.Desktop;

namespace eft_dma_radar
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.bindingSource1 = new System.Windows.Forms.BindingSource(this.components);
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.listView_PmcHistory = new System.Windows.Forms.ListView();
            this.columnHeader_Entry = new System.Windows.Forms.ColumnHeader();
            this.columnHeader_ID = new System.Windows.Forms.ColumnHeader();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.richTextBox_PlayersInfo = new System.Windows.Forms.RichTextBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.LootWalls = new System.Windows.Forms.CheckBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label_UIScale = new System.Windows.Forms.Label();
            this.trackBar_UIScale = new System.Windows.Forms.TrackBar();
            this.checkBox_HideNames = new System.Windows.Forms.CheckBox();
            this.textBox_PrimTeamID = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.checkBox_Aimview = new System.Windows.Forms.CheckBox();
            this.button_Restart = new System.Windows.Forms.Button();
            this.checkBox_MapSetup = new System.Windows.Forms.CheckBox();
            this.checkBox_Loot = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.trackBar_Zoom = new System.Windows.Forms.TrackBar();
            this.label2 = new System.Windows.Forms.Label();
            this.trackBar_AimLength = new System.Windows.Forms.TrackBar();
            this.button_Map = new System.Windows.Forms.Button();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.panel1 = new System.Windows.Forms.Panel();
            this.NoRecoil = new System.Windows.Forms.CheckBox();
            this.kek = new System.Windows.Forms.CheckBox();
            this.MaxStamina = new System.Windows.Forms.CheckBox();
            this.ThermalVision = new System.Windows.Forms.CheckBox();
            this.groupBox_Loot = new System.Windows.Forms.GroupBox();
            this.textBox_lootImportantPerSlot = new System.Windows.Forms.TextBox();
            this.ImportPerSlot = new System.Windows.Forms.Label();
            this.button_RefreshLoot = new System.Windows.Forms.Button();
            this.button_LootApply = new System.Windows.Forms.Button();
            this.label9 = new System.Windows.Forms.Label();
            this.textBox_LootFilterByName = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.textBox_LootImpValue = new System.Windows.Forms.TextBox();
            this.textBox_LootRegValue = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.groupBox_MapSetup = new System.Windows.Forms.GroupBox();
            this.textBox_R = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.button_Loot = new System.Windows.Forms.Button();
            this.checkBox_MapFree = new System.Windows.Forms.CheckBox();
            this.button_MapSetupApply = new System.Windows.Forms.Button();
            this.textBox_mapScale = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.textBox_mapY = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.textBox_mapX = new System.Windows.Forms.TextBox();
            this.label_Pos = new System.Windows.Forms.Label();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            ((System.ComponentModel.ISupportInitialize)(this.bindingSource1)).BeginInit();
            this.tabPage4.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_UIScale)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_Zoom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_AimLength)).BeginInit();
            this.tabPage1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.groupBox_Loot.SuspendLayout();
            this.groupBox_MapSetup.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabPage4
            // 
            this.tabPage4.Controls.Add(this.listView_PmcHistory);
            this.tabPage4.Location = new System.Drawing.Point(4, 24);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Size = new System.Drawing.Size(1328, 1033);
            this.tabPage4.TabIndex = 3;
            this.tabPage4.Text = "Player History";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // listView_PmcHistory
            // 
            this.listView_PmcHistory.AutoArrange = false;
            this.listView_PmcHistory.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader_Entry,
            this.columnHeader_ID});
            this.listView_PmcHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listView_PmcHistory.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.listView_PmcHistory.FullRowSelect = true;
            this.listView_PmcHistory.GridLines = true;
            this.listView_PmcHistory.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.listView_PmcHistory.Location = new System.Drawing.Point(0, 0);
            this.listView_PmcHistory.MultiSelect = false;
            this.listView_PmcHistory.Name = "listView_PmcHistory";
            this.listView_PmcHistory.Size = new System.Drawing.Size(1328, 1033);
            this.listView_PmcHistory.TabIndex = 0;
            this.listView_PmcHistory.UseCompatibleStateImageBehavior = false;
            this.listView_PmcHistory.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader_Entry
            // 
            this.columnHeader_Entry.Text = "Entry";
            this.columnHeader_Entry.Width = 200;
            // 
            // columnHeader_ID
            // 
            this.columnHeader_ID.Text = "ID";
            this.columnHeader_ID.Width = 50;
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.richTextBox_PlayersInfo);
            this.tabPage3.Location = new System.Drawing.Point(4, 24);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Size = new System.Drawing.Size(1328, 1033);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Player Loadouts";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // richTextBox_PlayersInfo
            // 
            this.richTextBox_PlayersInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBox_PlayersInfo.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.richTextBox_PlayersInfo.Location = new System.Drawing.Point(0, 0);
            this.richTextBox_PlayersInfo.Name = "richTextBox_PlayersInfo";
            this.richTextBox_PlayersInfo.ReadOnly = true;
            this.richTextBox_PlayersInfo.Size = new System.Drawing.Size(1328, 1033);
            this.richTextBox_PlayersInfo.TabIndex = 0;
            this.richTextBox_PlayersInfo.Text = "";
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.LootWalls);
            this.tabPage2.Controls.Add(this.groupBox1);
            this.tabPage2.Location = new System.Drawing.Point(4, 24);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(1328, 1033);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Settings";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // LootWalls
            // 
            this.LootWalls.AutoSize = true;
            this.LootWalls.Location = new System.Drawing.Point(582, 41);
            this.LootWalls.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.LootWalls.Name = "LootWalls";
            this.LootWalls.Size = new System.Drawing.Size(125, 19);
            this.LootWalls.TabIndex = 9;
            this.LootWalls.Text = "Loot through walls";
            this.LootWalls.UseVisualStyleBackColor = true;
            this.LootWalls.CheckedChanged += new System.EventHandler(this.lootWalls_CheckedChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label_UIScale);
            this.groupBox1.Controls.Add(this.trackBar_UIScale);
            this.groupBox1.Controls.Add(this.checkBox_HideNames);
            this.groupBox1.Controls.Add(this.textBox_PrimTeamID);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.checkBox_Aimview);
            this.groupBox1.Controls.Add(this.button_Restart);
            this.groupBox1.Controls.Add(this.checkBox_MapSetup);
            this.groupBox1.Controls.Add(this.checkBox_Loot);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.trackBar_Zoom);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.trackBar_AimLength);
            this.groupBox1.Controls.Add(this.button_Map);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Left;
            this.groupBox1.Location = new System.Drawing.Point(3, 3);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.groupBox1.Size = new System.Drawing.Size(525, 1027);
            this.groupBox1.TabIndex = 8;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Radar Config";
            // 
            // label_UIScale
            // 
            this.label_UIScale.AutoSize = true;
            this.label_UIScale.Location = new System.Drawing.Point(383, 247);
            this.label_UIScale.Name = "label_UIScale";
            this.label_UIScale.Size = new System.Drawing.Size(66, 15);
            this.label_UIScale.TabIndex = 28;
            this.label_UIScale.Text = "UI Scale 1.0";
            this.label_UIScale.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // trackBar_UIScale
            // 
            this.trackBar_UIScale.LargeChange = 10;
            this.trackBar_UIScale.Location = new System.Drawing.Point(395, 265);
            this.trackBar_UIScale.Maximum = 200;
            this.trackBar_UIScale.Minimum = 50;
            this.trackBar_UIScale.Name = "trackBar_UIScale";
            this.trackBar_UIScale.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.trackBar_UIScale.Size = new System.Drawing.Size(45, 403);
            this.trackBar_UIScale.TabIndex = 27;
            this.trackBar_UIScale.Value = 100;
            // 
            // checkBox_HideNames
            // 
            this.checkBox_HideNames.AutoSize = true;
            this.checkBox_HideNames.Location = new System.Drawing.Point(326, 151);
            this.checkBox_HideNames.Name = "checkBox_HideNames";
            this.checkBox_HideNames.Size = new System.Drawing.Size(114, 19);
            this.checkBox_HideNames.TabIndex = 26;
            this.checkBox_HideNames.Text = "Hide Names (F6)";
            this.checkBox_HideNames.UseVisualStyleBackColor = true;
            // 
            // textBox_PrimTeamID
            // 
            this.textBox_PrimTeamID.Location = new System.Drawing.Point(44, 97);
            this.textBox_PrimTeamID.MaxLength = 12;
            this.textBox_PrimTeamID.Name = "textBox_PrimTeamID";
            this.textBox_PrimTeamID.Size = new System.Drawing.Size(147, 23);
            this.textBox_PrimTeamID.TabIndex = 25;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(44, 79);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(147, 15);
            this.label3.TabIndex = 22;
            this.label3.Text = "Primary Teammate Acct ID";
            // 
            // checkBox_Aimview
            // 
            this.checkBox_Aimview.AutoSize = true;
            this.checkBox_Aimview.Location = new System.Drawing.Point(193, 151);
            this.checkBox_Aimview.Name = "checkBox_Aimview";
            this.checkBox_Aimview.Size = new System.Drawing.Size(127, 19);
            this.checkBox_Aimview.TabIndex = 19;
            this.checkBox_Aimview.Text = "Show Aimview (F4)";
            this.checkBox_Aimview.UseVisualStyleBackColor = true;
            // 
            // button_Restart
            // 
            this.button_Restart.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.button_Restart.Location = new System.Drawing.Point(359, 33);
            this.button_Restart.Name = "button_Restart";
            this.button_Restart.Size = new System.Drawing.Size(81, 61);
            this.button_Restart.TabIndex = 18;
            this.button_Restart.Text = "Restart Game";
            this.button_Restart.UseVisualStyleBackColor = true;
            this.button_Restart.Click += new System.EventHandler(this.button_Restart_Click);
            // 
            // checkBox_MapSetup
            // 
            this.checkBox_MapSetup.AutoSize = true;
            this.checkBox_MapSetup.Location = new System.Drawing.Point(44, 176);
            this.checkBox_MapSetup.Name = "checkBox_MapSetup";
            this.checkBox_MapSetup.Size = new System.Drawing.Size(153, 19);
            this.checkBox_MapSetup.TabIndex = 9;
            this.checkBox_MapSetup.Text = "Show Map Setup Helper";
            this.checkBox_MapSetup.UseVisualStyleBackColor = true;
            this.checkBox_MapSetup.CheckedChanged += new System.EventHandler(this.checkBox_MapSetup_CheckedChanged);
            // 
            // checkBox_Loot
            // 
            this.checkBox_Loot.AutoSize = true;
            this.checkBox_Loot.Location = new System.Drawing.Point(44, 151);
            this.checkBox_Loot.Name = "checkBox_Loot";
            this.checkBox_Loot.Size = new System.Drawing.Size(105, 19);
            this.checkBox_Loot.TabIndex = 17;
            this.checkBox_Loot.Text = "Show Loot (F3)";
            this.checkBox_Loot.UseVisualStyleBackColor = true;
            this.checkBox_Loot.CheckedChanged += new System.EventHandler(this.checkBox_Loot_CheckedChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(193, 217);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(136, 45);
            this.label1.TabIndex = 16;
            this.label1.Text = "Zoom\r\nF1/Mouse Whl Up = In\r\nF2/Mouse Whl Dn = Out";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // trackBar_Zoom
            // 
            this.trackBar_Zoom.LargeChange = 1;
            this.trackBar_Zoom.Location = new System.Drawing.Point(237, 265);
            this.trackBar_Zoom.Maximum = 200;
            this.trackBar_Zoom.Minimum = 1;
            this.trackBar_Zoom.Name = "trackBar_Zoom";
            this.trackBar_Zoom.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.trackBar_Zoom.Size = new System.Drawing.Size(45, 403);
            this.trackBar_Zoom.TabIndex = 15;
            this.trackBar_Zoom.Value = 100;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(77, 232);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(99, 30);
            this.label2.TabIndex = 13;
            this.label2.Text = "Player/Teammate\r\nAimline";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // trackBar_AimLength
            // 
            this.trackBar_AimLength.LargeChange = 50;
            this.trackBar_AimLength.Location = new System.Drawing.Point(104, 265);
            this.trackBar_AimLength.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.trackBar_AimLength.Maximum = 1000;
            this.trackBar_AimLength.Minimum = 10;
            this.trackBar_AimLength.Name = "trackBar_AimLength";
            this.trackBar_AimLength.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.trackBar_AimLength.Size = new System.Drawing.Size(45, 403);
            this.trackBar_AimLength.SmallChange = 5;
            this.trackBar_AimLength.TabIndex = 11;
            this.trackBar_AimLength.Value = 500;
            // 
            // button_Map
            // 
            this.button_Map.Location = new System.Drawing.Point(44, 33);
            this.button_Map.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.button_Map.Name = "button_Map";
            this.button_Map.Size = new System.Drawing.Size(107, 27);
            this.button_Map.TabIndex = 7;
            this.button_Map.Text = "Toggle Map (F5)";
            this.button_Map.UseVisualStyleBackColor = true;
            this.button_Map.Click += new System.EventHandler(this.button_Map_Click);
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.panel1);
            this.tabPage1.Controls.Add(this.groupBox_Loot);
            this.tabPage1.Controls.Add(this.groupBox_MapSetup);
            this.tabPage1.Location = new System.Drawing.Point(4, 24);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(1328, 1033);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Radar";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.NoRecoil);
            this.panel1.Controls.Add(this.kek);
            this.panel1.Controls.Add(this.MaxStamina);
            this.panel1.Controls.Add(this.ThermalVision);
            this.panel1.Location = new System.Drawing.Point(360, 6);
            this.panel1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(263, 63);
            this.panel1.TabIndex = 14;
            // 
            // NoRecoil
            // 
            this.NoRecoil.AutoSize = true;
            this.NoRecoil.Location = new System.Drawing.Point(15, 40);
            this.NoRecoil.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.NoRecoil.Name = "NoRecoil";
            this.NoRecoil.Size = new System.Drawing.Size(74, 19);
            this.NoRecoil.TabIndex = 15;
            this.NoRecoil.Text = "NoRecoil";
            this.NoRecoil.UseVisualStyleBackColor = true;
            this.NoRecoil.CheckedChanged += new System.EventHandler(this.NoRecoil_CheckedChanged_1);
            // 
            // kek
            // 
            this.kek.AutoSize = true;
            this.kek.Location = new System.Drawing.Point(130, 40);
            this.kek.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.kek.Name = "kek";
            this.kek.Size = new System.Drawing.Size(44, 19);
            this.kek.TabIndex = 13;
            this.kek.Text = "kek";
            this.kek.UseVisualStyleBackColor = true;
            this.kek.CheckedChanged += new System.EventHandler(this.kek_CheckedChanged);
            // 
            // MaxStamina
            // 
            this.MaxStamina.AutoSize = true;
            this.MaxStamina.Location = new System.Drawing.Point(15, 10);
            this.MaxStamina.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.MaxStamina.Name = "MaxStamina";
            this.MaxStamina.Size = new System.Drawing.Size(92, 19);
            this.MaxStamina.TabIndex = 14;
            this.MaxStamina.Text = "MaxStamina";
            this.MaxStamina.UseVisualStyleBackColor = true;
            this.MaxStamina.CheckedChanged += new System.EventHandler(this.MaxStamina_CheckedChanged_1);
            // 
            // ThermalVision
            // 
            this.ThermalVision.AutoSize = true;
            this.ThermalVision.Location = new System.Drawing.Point(130, 10);
            this.ThermalVision.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.ThermalVision.Name = "ThermalVision";
            this.ThermalVision.Size = new System.Drawing.Size(104, 19);
            this.ThermalVision.TabIndex = 13;
            this.ThermalVision.Text = "Thermal Vision";
            this.ThermalVision.UseVisualStyleBackColor = true;
            this.ThermalVision.CheckedChanged += new System.EventHandler(this.ThermalVision_CheckedChanged_1);
            // 
            // groupBox_Loot
            // 
            this.groupBox_Loot.Controls.Add(this.textBox_lootImportantPerSlot);
            this.groupBox_Loot.Controls.Add(this.ImportPerSlot);
            this.groupBox_Loot.Controls.Add(this.button_RefreshLoot);
            this.groupBox_Loot.Controls.Add(this.button_LootApply);
            this.groupBox_Loot.Controls.Add(this.label9);
            this.groupBox_Loot.Controls.Add(this.textBox_LootFilterByName);
            this.groupBox_Loot.Controls.Add(this.label8);
            this.groupBox_Loot.Controls.Add(this.label7);
            this.groupBox_Loot.Controls.Add(this.textBox_LootImpValue);
            this.groupBox_Loot.Controls.Add(this.textBox_LootRegValue);
            this.groupBox_Loot.Controls.Add(this.label6);
            this.groupBox_Loot.Location = new System.Drawing.Point(8, 6);
            this.groupBox_Loot.Name = "groupBox_Loot";
            this.groupBox_Loot.Size = new System.Drawing.Size(318, 202);
            this.groupBox_Loot.TabIndex = 12;
            this.groupBox_Loot.TabStop = false;
            this.groupBox_Loot.Text = "Loot";
            this.groupBox_Loot.Visible = false;
            this.groupBox_Loot.Enter += new System.EventHandler(this.groupBox_Loot_Enter_2);
            // 
            // textBox_lootImportantPerSlot
            // 
            this.textBox_lootImportantPerSlot.Location = new System.Drawing.Point(165, 52);
            this.textBox_lootImportantPerSlot.Name = "textBox_lootImportantPerSlot";
            this.textBox_lootImportantPerSlot.Size = new System.Drawing.Size(68, 23);
            this.textBox_lootImportantPerSlot.TabIndex = 23;
            this.textBox_lootImportantPerSlot.Text = "40000";
            this.textBox_lootImportantPerSlot.TextChanged += new System.EventHandler(this.textBox_lootImportantPerSlot_TextChanged);
            this.textBox_lootImportantPerSlot.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBox_lootImportantPerSlot_KeyDown);
            // 
            // ImportPerSlot
            // 
            this.ImportPerSlot.AutoSize = true;
            this.ImportPerSlot.Location = new System.Drawing.Point(165, 34);
            this.ImportPerSlot.Name = "ImportPerSlot";
            this.ImportPerSlot.Size = new System.Drawing.Size(80, 15);
            this.ImportPerSlot.TabIndex = 22;
            this.ImportPerSlot.Text = "ImportPerSlot";
            // 
            // button_RefreshLoot
            // 
            this.button_RefreshLoot.Location = new System.Drawing.Point(251, 38);
            this.button_RefreshLoot.Name = "button_RefreshLoot";
            this.button_RefreshLoot.Size = new System.Drawing.Size(55, 49);
            this.button_RefreshLoot.TabIndex = 21;
            this.button_RefreshLoot.Text = "Refresh Loot";
            this.button_RefreshLoot.UseVisualStyleBackColor = true;
            this.button_RefreshLoot.Click += new System.EventHandler(this.button_RefreshLoot_Click);
            // 
            // button_LootApply
            // 
            this.button_LootApply.Enabled = false;
            this.button_LootApply.Location = new System.Drawing.Point(82, 147);
            this.button_LootApply.Name = "button_LootApply";
            this.button_LootApply.Size = new System.Drawing.Size(61, 46);
            this.button_LootApply.TabIndex = 7;
            this.button_LootApply.Text = "Apply";
            this.button_LootApply.UseVisualStyleBackColor = true;
            this.button_LootApply.Click += new System.EventHandler(this.button_LootApply_Click);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(6, 100);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(210, 15);
            this.label9.TabIndex = 6;
            this.label9.Text = "Find Item(s) by Name (sep by comma)";
            // 
            // textBox_LootFilterByName
            // 
            this.textBox_LootFilterByName.Location = new System.Drawing.Point(6, 118);
            this.textBox_LootFilterByName.MaxLength = 512;
            this.textBox_LootFilterByName.Name = "textBox_LootFilterByName";
            this.textBox_LootFilterByName.Size = new System.Drawing.Size(227, 23);
            this.textBox_LootFilterByName.TabIndex = 5;
            this.textBox_LootFilterByName.TextChanged += new System.EventHandler(this.textBox_LootFilterByName_TextChanged);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(86, 34);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(60, 15);
            this.label8.TabIndex = 4;
            this.label8.Text = "Important";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(24, 34);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(47, 15);
            this.label7.TabIndex = 3;
            this.label7.Text = "Regular";
            // 
            // textBox_LootImpValue
            // 
            this.textBox_LootImpValue.Location = new System.Drawing.Point(86, 52);
            this.textBox_LootImpValue.MaxLength = 7;
            this.textBox_LootImpValue.Name = "textBox_LootImpValue";
            this.textBox_LootImpValue.Size = new System.Drawing.Size(57, 23);
            this.textBox_LootImpValue.TabIndex = 2;
            this.textBox_LootImpValue.Text = "300000";
            this.textBox_LootImpValue.TextChanged += new System.EventHandler(this.textBox_LootImpValue_TextChanged);
            // 
            // textBox_LootRegValue
            // 
            this.textBox_LootRegValue.Location = new System.Drawing.Point(21, 52);
            this.textBox_LootRegValue.MaxLength = 6;
            this.textBox_LootRegValue.Name = "textBox_LootRegValue";
            this.textBox_LootRegValue.Size = new System.Drawing.Size(50, 23);
            this.textBox_LootRegValue.TabIndex = 1;
            this.textBox_LootRegValue.Text = "50000";
            this.textBox_LootRegValue.TextChanged += new System.EventHandler(this.textBox_LootRegValue_TextChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(6, 19);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(137, 15);
            this.label6.TabIndex = 0;
            this.label6.Text = "Minimum Value to Show";
            // 
            // groupBox_MapSetup
            // 
            this.groupBox_MapSetup.Controls.Add(this.textBox_R);
            this.groupBox_MapSetup.Controls.Add(this.label10);
            this.groupBox_MapSetup.Controls.Add(this.button_Loot);
            this.groupBox_MapSetup.Controls.Add(this.checkBox_MapFree);
            this.groupBox_MapSetup.Controls.Add(this.button_MapSetupApply);
            this.groupBox_MapSetup.Controls.Add(this.textBox_mapScale);
            this.groupBox_MapSetup.Controls.Add(this.label5);
            this.groupBox_MapSetup.Controls.Add(this.textBox_mapY);
            this.groupBox_MapSetup.Controls.Add(this.label4);
            this.groupBox_MapSetup.Controls.Add(this.textBox_mapX);
            this.groupBox_MapSetup.Controls.Add(this.label_Pos);
            this.groupBox_MapSetup.Location = new System.Drawing.Point(8, 6);
            this.groupBox_MapSetup.Name = "groupBox_MapSetup";
            this.groupBox_MapSetup.Size = new System.Drawing.Size(346, 175);
            this.groupBox_MapSetup.TabIndex = 11;
            this.groupBox_MapSetup.TabStop = false;
            this.groupBox_MapSetup.Text = "Map Setup";
            this.groupBox_MapSetup.Visible = false;
            this.groupBox_MapSetup.Enter += new System.EventHandler(this.groupBox_MapSetup_Enter_1);
            // 
            // textBox_R
            // 
            this.textBox_R.Location = new System.Drawing.Point(126, 102);
            this.textBox_R.Name = "textBox_R";
            this.textBox_R.Size = new System.Drawing.Size(50, 23);
            this.textBox_R.TabIndex = 19;
            this.textBox_R.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(105, 104);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(14, 15);
            this.label10.TabIndex = 18;
            this.label10.Text = "R";
            this.label10.Click += new System.EventHandler(this.label10_Click);
            // 
            // button_Loot
            // 
            this.button_Loot.Location = new System.Drawing.Point(85, 0);
            this.button_Loot.Name = "button_Loot";
            this.button_Loot.Size = new System.Drawing.Size(44, 25);
            this.button_Loot.TabIndex = 12;
            this.button_Loot.Text = "Loot";
            this.button_Loot.UseVisualStyleBackColor = true;
            this.button_Loot.Visible = false;
            this.button_Loot.Click += new System.EventHandler(this.button_LootFilter_Click);
            // 
            // checkBox_MapFree
            // 
            this.checkBox_MapFree.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBox_MapFree.AutoSize = true;
            this.checkBox_MapFree.Location = new System.Drawing.Point(0, 0);
            this.checkBox_MapFree.Name = "checkBox_MapFree";
            this.checkBox_MapFree.Size = new System.Drawing.Size(66, 25);
            this.checkBox_MapFree.TabIndex = 17;
            this.checkBox_MapFree.Text = "Map Free";
            this.checkBox_MapFree.UseVisualStyleBackColor = true;
            this.checkBox_MapFree.CheckedChanged += new System.EventHandler(this.checkBox_MapFree_CheckedChanged);
            // 
            // button_MapSetupApply
            // 
            this.button_MapSetupApply.Location = new System.Drawing.Point(6, 143);
            this.button_MapSetupApply.Name = "button_MapSetupApply";
            this.button_MapSetupApply.Size = new System.Drawing.Size(75, 23);
            this.button_MapSetupApply.TabIndex = 16;
            this.button_MapSetupApply.Text = "Apply";
            this.button_MapSetupApply.UseVisualStyleBackColor = true;
            this.button_MapSetupApply.Click += new System.EventHandler(this.button_MapSetupApply_Click);
            // 
            // textBox_mapScale
            // 
            this.textBox_mapScale.Location = new System.Drawing.Point(46, 101);
            this.textBox_mapScale.Name = "textBox_mapScale";
            this.textBox_mapScale.Size = new System.Drawing.Size(50, 23);
            this.textBox_mapScale.TabIndex = 15;
            this.textBox_mapScale.TextChanged += new System.EventHandler(this.textBox_mapScale_TextChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 104);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(34, 15);
            this.label5.TabIndex = 14;
            this.label5.Text = "Scale";
            // 
            // textBox_mapY
            // 
            this.textBox_mapY.Location = new System.Drawing.Point(102, 67);
            this.textBox_mapY.Name = "textBox_mapY";
            this.textBox_mapY.Size = new System.Drawing.Size(50, 23);
            this.textBox_mapY.TabIndex = 13;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 70);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(24, 15);
            this.label4.TabIndex = 12;
            this.label4.Text = "X,Y";
            // 
            // textBox_mapX
            // 
            this.textBox_mapX.Location = new System.Drawing.Point(46, 67);
            this.textBox_mapX.Name = "textBox_mapX";
            this.textBox_mapX.Size = new System.Drawing.Size(50, 23);
            this.textBox_mapX.TabIndex = 11;
            // 
            // label_Pos
            // 
            this.label_Pos.AutoSize = true;
            this.label_Pos.Location = new System.Drawing.Point(7, 19);
            this.label_Pos.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label_Pos.Name = "label_Pos";
            this.label_Pos.Size = new System.Drawing.Size(43, 15);
            this.label_Pos.TabIndex = 10;
            this.label_Pos.Text = "coords";
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Controls.Add(this.tabPage4);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1336, 1061);
            this.tabControl1.TabIndex = 8;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1336, 1061);
            this.Controls.Add(this.tabControl1);
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Name = "MainForm";
            this.Text = "Dyrkov Pidar v0.12";
            this.Load += new System.EventHandler(this.MainForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.bindingSource1)).EndInit();
            this.tabPage4.ResumeLayout(false);
            this.tabPage3.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_UIScale)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_Zoom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_AimLength)).EndInit();
            this.tabPage1.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.groupBox_Loot.ResumeLayout(false);
            this.groupBox_Loot.PerformLayout();
            this.groupBox_MapSetup.ResumeLayout(false);
            this.groupBox_MapSetup.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private BindingSource bindingSource1;
        private CheckBox checkBox1;
        private TabPage tabPage4;
        private ListView listView_PmcHistory;
        private ColumnHeader columnHeader_Entry;
        private ColumnHeader columnHeader_ID;
        private TabPage tabPage3;
        private RichTextBox richTextBox_PlayersInfo;
        private TabPage tabPage2;
        private CheckBox LootWalls;
        private GroupBox groupBox1;
        private Label label_UIScale;
        private TrackBar trackBar_UIScale;
        private CheckBox checkBox_HideNames;
        private TextBox textBox_PrimTeamID;
        private Label label3;
        private CheckBox checkBox_Aimview;
        private Button button_Restart;
        private CheckBox checkBox_MapSetup;
        private CheckBox checkBox_Loot;
        private Label label1;
        private TrackBar trackBar_Zoom;
        private Label label2;
        private TrackBar trackBar_AimLength;
        private Button button_Map;
        private TabPage tabPage1;
        private GroupBox groupBox_Loot;
        private Button button_RefreshLoot;
        private Button button_LootApply;
        private Label label9;
        private TextBox textBox_LootFilterByName;
        private Label label8;
        private Label label7;
        private TextBox textBox_LootImpValue;
        private TextBox textBox_LootRegValue;
        private Label label6;
        private GroupBox groupBox_MapSetup;
        private TextBox textBox_R;
        private Label label10;
        private Button button_Loot;
        private CheckBox checkBox_MapFree;
        private Button button_MapSetupApply;
        private TextBox textBox_mapScale;
        private Label label5;
        private TextBox textBox_mapY;
        private Label label4;
        private TextBox textBox_mapX;
        private Label label_Pos;
        private TabControl tabControl1;
        private CheckBox kek;
        private Panel panel1;
        private CheckBox NoRecoil;
        private CheckBox MaxStamina;
        private CheckBox ThermalVision;
        private TextBox textBox_lootImportantPerSlot;
        private Label ImportPerSlot;
    }
}

