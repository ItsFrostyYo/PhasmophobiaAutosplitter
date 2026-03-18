using LiveSplit.Model;
using LiveSplit.UI;
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;

namespace LiveSplit.PhasmophobiaAutosplitter
{
    public class PhasmophobiaSettings : UserControl
    {
        private readonly CheckBox chkStartWhenContractInitialization;
        private readonly CheckBox chkSplitOnContractFinish;
        private readonly CheckBox chkAllowResetting;
        private readonly CheckBox chkMultiContract;
        private readonly CheckBox chkLoadTimeRemoval;
        private readonly CheckBox chkWarnOnResetIfGold;
        private readonly ToolTip toolTips;

        public bool StartWhenTruckLoaded { get; set; } = true;
        public bool StartOnFirstMovement { get; private set; } = true;
        public bool SplitOnContractFinish { get; set; } = true;
        public bool ResetWhenAtLobby { get; set; } = true;
        public bool MultiContractEnabled { get; set; } = false;
        public bool LoadTimeRemovalEnabled { get; set; } = false;
        public bool WarnOnResetIfGold { get; set; } = false;

        public bool IntroStart
        {
            get => StartWhenTruckLoaded;
            set
            {
                StartWhenTruckLoaded = value;
                SyncUiFromState();
            }
        }

        public bool CreativeStart
        {
            get => StartOnFirstMovement;
            set
            {
                if (value)
                    StartWhenTruckLoaded = true;
                SyncUiFromState();
            }
        }

        public bool EndOnTruckUnload
        {
            get => SplitOnContractFinish;
            set
            {
                SplitOnContractFinish = value;
                SyncUiFromState();
            }
        }

        public bool Reset
        {
            get => ResetWhenAtLobby;
            set
            {
                ResetWhenAtLobby = value;
                SyncUiFromState();
            }
        }

        public bool EnableStartSplit => StartWhenTruckLoaded;
        public bool EnableEndSplit => SplitOnContractFinish;
        public bool EnableAutoResetOnLeave => ResetWhenAtLobby;
        public bool EnableMultiContract => MultiContractEnabled;
        public bool EnableLoadTimeRemoval => LoadTimeRemovalEnabled;

        public PhasmophobiaSettings(LiveSplitState state)
        {
            _ = state;

            AutoSize = false;
            Dock = DockStyle.Top;
            Margin = new Padding(0);
            Padding = new Padding(0);

            int settingsWidth = 460;
            int settingsHeight = 340;
            MinimumSize = new Size(settingsWidth, settingsHeight);
            MaximumSize = new Size(settingsWidth, settingsHeight);
            Size = new Size(settingsWidth, settingsHeight);

            toolTips = new ToolTip
            {
                AutoPopDelay = 12000,
                InitialDelay = 350,
                ReshowDelay = 150,
                ShowAlways = true
            };

            var root = new TableLayoutPanel
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Margin = new Padding(0),
                Padding = new Padding(6)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 142f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 168f));

            var grpStartEndReset = new GroupBox
            {
                Text = "Start / Split / Reset",
                AutoSize = false,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 4, 4),
                Padding = new Padding(6, 14, 6, 6)
            };

            var leftFlow = new FlowLayoutPanel
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            chkStartWhenContractInitialization = new CheckBox
            {
                AutoSize = true,
                Text = "Start on Contract Initialization",
                Checked = StartWhenTruckLoaded,
                Margin = new Padding(0, 0, 0, 2)
            };
            chkStartWhenContractInitialization.CheckedChanged += (s, e) =>
            {
                StartWhenTruckLoaded = chkStartWhenContractInitialization.Checked;
                SyncUiFromState();
            };

            chkSplitOnContractFinish = new CheckBox
            {
                AutoSize = true,
                Text = "Split on Contract Finish",
                Checked = SplitOnContractFinish,
                Margin = new Padding(0, 0, 0, 6)
            };
            chkSplitOnContractFinish.CheckedChanged += (s, e) =>
            {
                SplitOnContractFinish = chkSplitOnContractFinish.Checked;
            };

            chkAllowResetting = new CheckBox
            {
                AutoSize = false,
                Size = new Size(196, 34),
                Text = "Allow Resetting on Leave, Game Close,\r\nand New Run Start",
                Checked = ResetWhenAtLobby,
                Margin = new Padding(0),
                UseVisualStyleBackColor = true
            };
            chkAllowResetting.CheckedChanged += (s, e) =>
            {
                ResetWhenAtLobby = chkAllowResetting.Checked;
            };

            toolTips.SetToolTip(
                chkStartWhenContractInitialization,
                "Start:\nStarts the timer when the player and truck are completely finished initializing and the player is allowed to move.\nBackup trigger: if contract initialization is missed, start on first movement.");
            toolTips.SetToolTip(
                chkSplitOnContractFinish,
                "Split:\nSplits when a contract-finish loading transition is triggered from truck context.");
            toolTips.SetToolTip(
                chkAllowResetting,
                "Reset:\nAllows reset on contract leave, game close, and new run start detection while timer is running.\nIf this is off, all auto-reset behavior is disabled.");

            leftFlow.Controls.Add(chkStartWhenContractInitialization);
            leftFlow.Controls.Add(chkSplitOnContractFinish);
            leftFlow.Controls.Add(chkAllowResetting);
            grpStartEndReset.Controls.Add(leftFlow);

            var grpOptions = new GroupBox
            {
                Text = "Options",
                AutoSize = false,
                Dock = DockStyle.Fill,
                Margin = new Padding(4, 0, 0, 4),
                Padding = new Padding(6, 14, 6, 6)
            };

            var optionsFlow = new FlowLayoutPanel
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            chkMultiContract = new CheckBox
            {
                AutoSize = true,
                Text = "Multi-Contract",
                Checked = MultiContractEnabled,
                Margin = new Padding(0, 0, 0, 2),
                UseVisualStyleBackColor = true
            };
            chkMultiContract.CheckedChanged += (s, e) =>
            {
                MultiContractEnabled = chkMultiContract.Checked;
            };

            chkLoadTimeRemoval = new CheckBox
            {
                AutoSize = true,
                Text = "Load Time Removal (Game Time)",
                Checked = LoadTimeRemovalEnabled,
                Margin = new Padding(0, 0, 0, 4),
                UseVisualStyleBackColor = true
            };
            chkLoadTimeRemoval.CheckedChanged += (s, e) =>
            {
                LoadTimeRemovalEnabled = chkLoadTimeRemoval.Checked;
            };

            chkWarnOnResetIfGold = new CheckBox
            {
                AutoSize = true,
                Text = "Warn on Reset if Gold",
                Checked = WarnOnResetIfGold,
                Margin = new Padding(0, 0, 0, 4),
                UseVisualStyleBackColor = true
            };
            chkWarnOnResetIfGold.CheckedChanged += (s, e) =>
            {
                WarnOnResetIfGold = chkWarnOnResetIfGold.Checked;
            };

            toolTips.SetToolTip(
                chkMultiContract,
                "Options:\nMulti-Contract: lets you chain contracts/maps in one attempt without resetting after a split.\nReal Time and Game Time behavior outside resets is unchanged.");
            toolTips.SetToolTip(
                chkLoadTimeRemoval,
                "Options:\nLoad Time Removal: when using Game Time, loading-screen time after a split is removed.\nUses memory/UI state detection. Real Time is unchanged.");
            toolTips.SetToolTip(
                chkWarnOnResetIfGold,
                "Options:\nShows LiveSplit's reset confirmation prompt when the current attempt has at least one gold split.");

            optionsFlow.Controls.Add(chkMultiContract);
            optionsFlow.Controls.Add(chkLoadTimeRemoval);
            optionsFlow.Controls.Add(chkWarnOnResetIfGold);
            grpOptions.Controls.Add(optionsFlow);

            var grpKnownIssues = new GroupBox
            {
                Text = "Known Issues",
                AutoSize = false,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(6, 14, 6, 6)
            };
            var lblKnownIssues = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Text = "- Leaving the truck and re-entering WILL be treated as a split.\n"
                     + "- Multiplayer memory state can be unreliable and may cause missed or duplicate behavior.\n"
                     + "- Load-removal timing can vary slightly on some transitions because game/UI readiness edges are not identical every run.\n"
                     + "- Game updates can change memory structures and break detection until offsets are updated.\n"
                     + "- Restarting the game can rarely desync detection; reload the component or restart LiveSplit.",
                Margin = new Padding(0),
                TextAlign = ContentAlignment.TopLeft
            };
            grpKnownIssues.Controls.Add(lblKnownIssues);

            root.Controls.Add(grpStartEndReset, 0, 0);
            root.Controls.Add(grpOptions, 1, 0);
            root.Controls.Add(grpKnownIssues, 0, 1);
            root.SetColumnSpan(grpKnownIssues, 2);
            Controls.Add(root);

            SyncUiFromState();
        }

        public XmlNode UpdateSettings(XmlDocument document)
        {
            XmlElement xmlSettings = document.CreateElement("Settings");

            AddBool(document, xmlSettings, "StartWhenTruckLoaded", StartWhenTruckLoaded);
            AddBool(document, xmlSettings, "StartOnFirstMovement", StartOnFirstMovement);
            AddBool(document, xmlSettings, "EndOnTruckUnload", SplitOnContractFinish);
            AddBool(document, xmlSettings, "SplitOnContractFinish", SplitOnContractFinish);
            AddBool(document, xmlSettings, "ResetWhenAtLobby", ResetWhenAtLobby);
            AddBool(document, xmlSettings, "MultiContractEnabled", MultiContractEnabled);
            AddBool(document, xmlSettings, "LoadTimeRemovalEnabled", LoadTimeRemovalEnabled);
            AddBool(document, xmlSettings, "MultiContractLoadRemoval", MultiContractEnabled && LoadTimeRemovalEnabled);
            AddBool(document, xmlSettings, "WarnOnResetIfGold", WarnOnResetIfGold);
            AddBool(document, xmlSettings, "EnableStartSplit", EnableStartSplit);
            AddBool(document, xmlSettings, "EnableEndSplit", EnableEndSplit);
            AddBool(document, xmlSettings, "EnableAutoResetOnLeave", ResetWhenAtLobby);
            AddBool(document, xmlSettings, "IntroStart", StartWhenTruckLoaded);
            AddBool(document, xmlSettings, "CreativeStart", StartOnFirstMovement);
            AddBool(document, xmlSettings, "Reset", ResetWhenAtLobby);

            return xmlSettings;
        }

        public void SetSettings(XmlNode settings)
        {
            bool startTruckLoaded = ReadBool(settings, "StartWhenTruckLoaded", true);
            bool endOnTruckUnload = ReadBool(settings, "EndOnTruckUnload", true);
            bool resetAtLobby = ReadBool(settings, "ResetWhenAtLobby", true);
            bool multiContractEnabled = ReadBool(settings, "MultiContractEnabled", false);
            bool loadTimeRemovalEnabled = ReadBool(settings, "LoadTimeRemovalEnabled", false);
            bool legacyCombined = ReadBool(settings, "MultiContractLoadRemoval", false);
            bool warnOnResetIfGold = ReadBool(settings, "WarnOnResetIfGold", false);

            if (!HasNode(settings, "StartWhenTruckLoaded"))
                startTruckLoaded = ReadBool(settings, "IntroStart", true);
            if (!HasNode(settings, "EndOnTruckUnload"))
                endOnTruckUnload = ReadBool(settings, "EnableEndSplit", true);
            if (!HasNode(settings, "ResetWhenAtLobby"))
                resetAtLobby = ReadBool(settings, "EnableAutoResetOnLeave", true) || ReadBool(settings, "Reset", true);
            if (!HasNode(settings, "MultiContractEnabled") && HasNode(settings, "MultiContractLoadRemoval"))
                multiContractEnabled = legacyCombined;
            if (!HasNode(settings, "LoadTimeRemovalEnabled") && HasNode(settings, "MultiContractLoadRemoval"))
                loadTimeRemovalEnabled = legacyCombined;
            if (!HasNode(settings, "WarnOnResetIfGold"))
                warnOnResetIfGold = false;

            StartWhenTruckLoaded = startTruckLoaded;
            SplitOnContractFinish = endOnTruckUnload;
            ResetWhenAtLobby = resetAtLobby;
            MultiContractEnabled = multiContractEnabled;
            LoadTimeRemovalEnabled = loadTimeRemovalEnabled;
            WarnOnResetIfGold = warnOnResetIfGold;
            SyncUiFromState();
        }

        private void SyncUiFromState()
        {
            StartOnFirstMovement = StartWhenTruckLoaded;

            if (chkStartWhenContractInitialization != null)
                chkStartWhenContractInitialization.Checked = StartWhenTruckLoaded;
            if (chkSplitOnContractFinish != null)
                chkSplitOnContractFinish.Checked = SplitOnContractFinish;
            if (chkAllowResetting != null)
                chkAllowResetting.Checked = ResetWhenAtLobby;
            if (chkMultiContract != null)
                chkMultiContract.Checked = MultiContractEnabled;
            if (chkLoadTimeRemoval != null)
                chkLoadTimeRemoval.Checked = LoadTimeRemovalEnabled;
            if (chkWarnOnResetIfGold != null)
                chkWarnOnResetIfGold.Checked = WarnOnResetIfGold;
        }

        private static bool HasNode(XmlNode root, string name)
        {
            if (root == null)
                return false;
            return root.SelectSingleNode(".//" + name) != null;
        }

        private static XmlElement AddBool(XmlDocument doc, XmlElement root, string name, bool value)
        {
            var e = doc.CreateElement(name);
            e.InnerText = value.ToString();
            root.AppendChild(e);
            return e;
        }

        private static bool ReadBool(XmlNode root, string name, bool def = false)
        {
            if (root == null)
                return def;

            var n = root.SelectSingleNode(".//" + name);
            if (n == null)
                return def;

            return bool.TryParse(n.InnerText, out var b) ? b : def;
        }
    }
}
