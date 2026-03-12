using LiveSplit.Model;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;

namespace LiveSplit.Phasmophobia
{
    public class PhasmophobiaSettings : UserControl
    {
        private readonly CheckBox chkStartWhenTruckLoaded;
        private readonly CheckBox chkStartOnFirstMovement;
        private readonly CheckBox chkEndOnTruckUnload;
        private readonly CheckBox chkResetWhenAtLobby;

        public bool StartWhenTruckLoaded { get; set; } = true;
        public bool StartOnFirstMovement { get; set; } = true;
        public bool EndOnTruckUnload { get; set; } = true;
        public bool ResetWhenAtLobby { get; set; } = true;

        // Compatibility properties used by existing component/memory flow.
        public bool IntroStart
        {
            get => StartWhenTruckLoaded;
            set
            {
                StartWhenTruckLoaded = value;
                if (!StartWhenTruckLoaded)
                    StartOnFirstMovement = false;
                SyncUiFromState();
            }
        }

        public bool CreativeStart
        {
            get => StartOnFirstMovement;
            set
            {
                StartOnFirstMovement = value;
                if (StartOnFirstMovement)
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
            MinimumSize = new Size(475, 150);
            MaximumSize = new Size(475, 150);
            Size = new Size(475, 150);

            var root = new TableLayoutPanel
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0),
                Padding = new Padding(4)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 235f));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var grpStartEndReset = new GroupBox
            {
                Text = "Start / End / Reset",
                AutoSize = false,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 6, 0),
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

            chkStartWhenTruckLoaded = new CheckBox
            {
                AutoSize = true,
                Text = "Start on Truck Load",
                Checked = StartWhenTruckLoaded,
                Margin = new Padding(0, 0, 0, 2)
            };
            chkStartWhenTruckLoaded.CheckedChanged += (s, e) =>
            {
                StartWhenTruckLoaded = chkStartWhenTruckLoaded.Checked;
                if (!StartWhenTruckLoaded)
                    StartOnFirstMovement = false;
                SyncUiFromState();
            };

            chkStartOnFirstMovement = new CheckBox
            {
                AutoSize = true,
                Text = "+Start on First Movement",
                Checked = StartOnFirstMovement,
                Margin = new Padding(18, 0, 0, 6)
            };
            chkStartOnFirstMovement.CheckedChanged += (s, e) =>
            {
                StartOnFirstMovement = chkStartOnFirstMovement.Checked;
                if (StartOnFirstMovement)
                    StartWhenTruckLoaded = true;
                SyncUiFromState();
            };

            chkEndOnTruckUnload = new CheckBox
            {
                AutoSize = true,
                Text = "End on Truck Unload",
                Checked = EndOnTruckUnload,
                Margin = new Padding(0, 0, 0, 2)
            };
            chkEndOnTruckUnload.CheckedChanged += (s, e) =>
            {
                EndOnTruckUnload = chkEndOnTruckUnload.Checked;
            };

            chkResetWhenAtLobby = new CheckBox
            {
                AutoSize = true,
                Text = "Reset on Contract Leave",
                Checked = ResetWhenAtLobby,
                Margin = new Padding(0, 0, 0, 0)
            };
            chkResetWhenAtLobby.CheckedChanged += (s, e) =>
            {
                ResetWhenAtLobby = chkResetWhenAtLobby.Checked;
            };

            leftFlow.Controls.Add(chkStartWhenTruckLoaded);
            leftFlow.Controls.Add(chkStartOnFirstMovement);
            leftFlow.Controls.Add(chkEndOnTruckUnload);
            leftFlow.Controls.Add(chkResetWhenAtLobby);
            grpStartEndReset.Controls.Add(leftFlow);

            var grpOptions = new GroupBox
            {
                Text = "Options",
                AutoSize = false,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(8, 18, 8, 8)
            };
            var lblOptions = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Text = "Options will be added here\nin future updates",
                Margin = new Padding(0),
                TextAlign = ContentAlignment.TopLeft
            };
            grpOptions.Controls.Add(lblOptions);

            root.Controls.Add(grpStartEndReset, 0, 0);
            root.Controls.Add(grpOptions, 1, 0);
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
            // New keys first.
            bool startTruckLoaded = ReadBool(settings, "StartWhenTruckLoaded", true);
            bool startFirstMovement = ReadBool(settings, "StartOnFirstMovement", true);
            bool endOnTruckUnload = ReadBool(settings, "EndOnTruckUnload", true);
            bool resetAtLobby = ReadBool(settings, "ResetWhenAtLobby", true);

            // Backward compatibility with older config keys.
            if (!HasNode(settings, "StartWhenTruckLoaded"))
                startTruckLoaded = ReadBool(settings, "IntroStart", true);
            if (!HasNode(settings, "StartOnFirstMovement"))
                startFirstMovement = ReadBool(settings, "CreativeStart", true);
            if (!HasNode(settings, "EndOnTruckUnload"))
                endOnTruckUnload = ReadBool(settings, "EnableEndSplit", true);
            if (!HasNode(settings, "ResetWhenAtLobby"))
                resetAtLobby = ReadBool(settings, "EnableAutoResetOnLeave", true) || ReadBool(settings, "Reset", true);

            // Keep first movement as a sub-option of truck load.
            if (startFirstMovement && !startTruckLoaded)
                startTruckLoaded = true;

            StartWhenTruckLoaded = startTruckLoaded;
            StartOnFirstMovement = startFirstMovement;
            EndOnTruckUnload = endOnTruckUnload;
            ResetWhenAtLobby = resetAtLobby;
            SyncUiFromState();
        }

        private void SyncUiFromState()
        {
            if (chkStartWhenTruckLoaded != null)
                chkStartWhenTruckLoaded.Checked = StartWhenTruckLoaded;
            if (chkStartOnFirstMovement != null)
            {
                chkStartOnFirstMovement.Checked = StartOnFirstMovement;
                chkStartOnFirstMovement.Enabled = StartWhenTruckLoaded;
            }
            if (chkEndOnTruckUnload != null)
                chkEndOnTruckUnload.Checked = EndOnTruckUnload;
            if (chkResetWhenAtLobby != null)
                chkResetWhenAtLobby.Checked = ResetWhenAtLobby;
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
