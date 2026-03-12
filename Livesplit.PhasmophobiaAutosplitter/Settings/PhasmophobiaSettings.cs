using LiveSplit.Model;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;

namespace LiveSplit.PhasmophobiaAutosplitter
{
    public class PhasmophobiaSettings : UserControl
    {
        private readonly CheckBox chkStartWhenContractInitialization;
        private readonly CheckBox chkEndOnContractFinish;
        private readonly CheckBox chkAllowResetting;
        private readonly ToolTip toolTips;

        public bool StartWhenTruckLoaded { get; set; } = true;
        public bool StartOnFirstMovement { get; private set; } = true;
        public bool EndOnTruckUnload { get; set; } = true;
        public bool ResetWhenAtLobby { get; set; } = true;

        // Compatibility properties used by existing component/memory flow.
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
                // Start on first movement is always enabled as a backup whenever Start is enabled.
                if (value)
                    StartWhenTruckLoaded = true;
                SyncUiFromState();
            }
        }

        // Kept for compatibility with shared base component logic.
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
        public bool EnableEndSplit => EndOnTruckUnload;
        public bool EnableAutoResetOnLeave => ResetWhenAtLobby;

        public PhasmophobiaSettings(LiveSplitState state)
        {
            AutoSize = false;
            Dock = DockStyle.Top;
            Margin = new Padding(0);
            Padding = new Padding(0);
            MinimumSize = new Size(500, 280);
            MaximumSize = new Size(500, 280);
            Size = new Size(500, 280);

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
                Padding = new Padding(4)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 128f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 128f));

            var grpStartEndReset = new GroupBox
            {
                Text = "Start / End / Reset",
                AutoSize = false,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 6, 6),
                Padding = new Padding(8, 18, 8, 8)
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

            chkEndOnContractFinish = new CheckBox
            {
                AutoSize = true,
                Text = "End on Contract Finish",
                Checked = EndOnTruckUnload,
                Margin = new Padding(0, 0, 0, 6)
            };
            chkEndOnContractFinish.CheckedChanged += (s, e) =>
            {
                EndOnTruckUnload = chkEndOnContractFinish.Checked;
            };

            chkAllowResetting = new CheckBox
            {
                AutoSize = false,
                Size = new Size(205, 34),
                Text = "Allow Resetting on Contract Leave,\r\nGame Close and New Run Start",
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
                "Start:\nStarts the timer when the player and truck are completely finished initalizing and the player is allowed to move.\nBackup trigger: if contract initialization is missed, start on first movement.");
            toolTips.SetToolTip(
                chkEndOnContractFinish,
                "End:\nSplits when a loading transition is triggered while you are inside the truck.");
            toolTips.SetToolTip(
                chkAllowResetting,
                "Reset:\nAllows reset on contract leave, game close, and new run start detection.\nIf this is off, all auto-reset behavior is disabled.");

            leftFlow.Controls.Add(chkStartWhenContractInitialization);
            leftFlow.Controls.Add(chkEndOnContractFinish);
            leftFlow.Controls.Add(chkAllowResetting);
            grpStartEndReset.Controls.Add(leftFlow);

            var grpOptions = new GroupBox
            {
                Text = "Options",
                AutoSize = false,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 6),
                Padding = new Padding(8, 18, 8, 8)
            };
            var lblOptions = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Text = "More Options might be added here in future updates.\n(Like Full Game Load Removal)",
                Margin = new Padding(0),
                TextAlign = ContentAlignment.TopLeft
            };
            toolTips.SetToolTip(
                lblOptions,
                "Options:\nMore Options might be added here in future updates. (Like Full Game Load Removal)");
            grpOptions.Controls.Add(lblOptions);

            var grpKnownIssues = new GroupBox
            {
                Text = "Known Issues",
                AutoSize = false,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(8, 18, 8, 8)
            };
            var lblKnownIssues = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Text = "- Leaving truck then re-entering can treat a normal leave\n"
                     + "  loading transition as a split/end.\n"
                     + "- Multiplayer may be unreliable.\n"
                     + "- Game updates can break memory signatures until the\n"
                     + "  autosplitter is updated.\n"
                     + "- Restarting the game may rarely break the autosplitter.\n"
                     + "  If this happens, restart LiveSplit or reload the component.",
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
            AddBool(document, xmlSettings, "EndOnTruckUnload", EndOnTruckUnload);
            AddBool(document, xmlSettings, "ResetWhenAtLobby", ResetWhenAtLobby);
            AddBool(document, xmlSettings, "EnableStartSplit", EnableStartSplit);
            AddBool(document, xmlSettings, "EnableEndSplit", EnableEndSplit);
            AddBool(document, xmlSettings, "EnableAutoResetOnLeave", ResetWhenAtLobby);

            // Keep old keys for backward compatibility with existing layout files.
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

            // Backward compatibility with older config keys.
            if (!HasNode(settings, "StartWhenTruckLoaded"))
                startTruckLoaded = ReadBool(settings, "IntroStart", true);
            if (!HasNode(settings, "EndOnTruckUnload"))
                endOnTruckUnload = ReadBool(settings, "EnableEndSplit", true);
            if (!HasNode(settings, "ResetWhenAtLobby"))
                resetAtLobby = ReadBool(settings, "EnableAutoResetOnLeave", true) || ReadBool(settings, "Reset", true);

            StartWhenTruckLoaded = startTruckLoaded;
            EndOnTruckUnload = endOnTruckUnload;
            ResetWhenAtLobby = resetAtLobby;
            SyncUiFromState();
        }

        private void SyncUiFromState()
        {
            // Start on first movement is always enabled as the backup behavior when Start is enabled.
            StartOnFirstMovement = StartWhenTruckLoaded;

            if (chkStartWhenContractInitialization != null)
                chkStartWhenContractInitialization.Checked = StartWhenTruckLoaded;
            if (chkEndOnContractFinish != null)
                chkEndOnContractFinish.Checked = EndOnTruckUnload;
            if (chkAllowResetting != null)
                chkAllowResetting.Checked = ResetWhenAtLobby;
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
