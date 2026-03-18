using System;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using LiveSplit.UI;

namespace LiveSplit.PhasmophobiaContractResolver
{
    public class ContractResolverSettings : UserControl
    {
        private readonly Button btnBackground;
        private readonly Button btnTextColor;
        private readonly Button btnFont;
        private readonly Label lblFontValue;

        public Color BackgroundColor { get; set; } = Color.Black;
        public Color TextColor { get; set; } = Color.White;
        public Font TextFont { get; set; } = new Font("Segoe UI", 12f, FontStyle.Regular);

        public ContractResolverSettings()
        {
            AutoSize = false;
            Size = new Size(300, 145);

            var lblBg = new Label
            {
                AutoSize = true,
                Text = "Background Color:",
                Location = new Point(8, 12)
            };

            btnBackground = new Button
            {
                Width = 100,
                Height = 22,
                Location = new Point(130, 8),
                FlatStyle = FlatStyle.Flat
            };
            btnBackground.Click += BtnBackground_Click;
            btnBackground.BackColor = BackgroundColor;

            var lblText = new Label
            {
                AutoSize = true,
                Text = "Text Color:",
                Location = new Point(8, 42)
            };

            btnTextColor = new Button
            {
                Width = 100,
                Height = 22,
                Location = new Point(130, 38),
                FlatStyle = FlatStyle.Flat,
                BackColor = TextColor
            };
            btnTextColor.Click += BtnTextColor_Click;

            var lblFont = new Label
            {
                AutoSize = true,
                Text = "Font:",
                Location = new Point(8, 72)
            };

            btnFont = new Button
            {
                Width = 70,
                Height = 22,
                Location = new Point(130, 68)
            };
            btnFont.Text = "Choose";
            btnFont.Click += BtnFont_Click;

            lblFontValue = new Label
            {
                AutoSize = false,
                Width = 160,
                Height = 36,
                Location = new Point(8, 98),
                Text = SettingsHelper.FormatFont(TextFont)
            };

            Controls.Add(lblBg);
            Controls.Add(btnBackground);
            Controls.Add(lblText);
            Controls.Add(btnTextColor);
            Controls.Add(lblFont);
            Controls.Add(btnFont);
            Controls.Add(lblFontValue);
        }

        private void BtnBackground_Click(object sender, EventArgs e)
        {
            SettingsHelper.ColorButtonClick(btnBackground, this);
            BackgroundColor = btnBackground.BackColor;
        }

        private void BtnTextColor_Click(object sender, EventArgs e)
        {
            SettingsHelper.ColorButtonClick(btnTextColor, this);
            TextColor = btnTextColor.BackColor;
        }

        private void BtnFont_Click(object sender, EventArgs e)
        {
            using (var dialog = new FontDialog())
            {
                dialog.Font = TextFont;
                dialog.MinSize = 8;
                dialog.MaxSize = 72;
                dialog.ShowEffects = false;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    TextFont = dialog.Font;
                    lblFontValue.Text = SettingsHelper.FormatFont(TextFont);
                }
            }
        }

        public void SetSettings(XmlNode settingsNode)
        {
            if (settingsNode == null)
                return;

            var el = settingsNode["BackgroundColor"];
            if (el != null && int.TryParse(el.InnerText, out int bgArgb))
                BackgroundColor = Color.FromArgb(bgArgb);

            el = settingsNode["TextColor"];
            if (el != null && int.TryParse(el.InnerText, out int textArgb))
                TextColor = Color.FromArgb(textArgb);

            el = settingsNode["TextFont"];
            if (el != null)
            {
                var parsed = SettingsHelper.GetFontFromElement(el);
                if (parsed != null)
                    TextFont = parsed;
            }
            else
            {
                // Backward compatibility for older resolver settings.
                el = settingsNode["FontSize"];
                if (el != null && int.TryParse(el.InnerText, out int fs))
                    TextFont = new Font(TextFont.FontFamily, Math.Max(8, Math.Min(72, fs)), TextFont.Style);
            }

            if (BackgroundColor.A < 24)
                BackgroundColor = Color.Black;
            if (TextColor.A < 24)
                TextColor = Color.White;
            if (TextColor.ToArgb() == BackgroundColor.ToArgb())
                TextColor = BackgroundColor.GetBrightness() > 0.5f ? Color.Black : Color.White;

            btnBackground.BackColor = BackgroundColor;
            btnTextColor.BackColor = TextColor;
            lblFontValue.Text = SettingsHelper.FormatFont(TextFont);
        }

        public XmlNode GetSettings(XmlDocument doc)
        {
            XmlElement root = doc.CreateElement("Settings");

            XmlElement bg = doc.CreateElement("BackgroundColor");
            bg.InnerText = BackgroundColor.ToArgb().ToString();
            root.AppendChild(bg);

            XmlElement text = doc.CreateElement("TextColor");
            text.InnerText = TextColor.ToArgb().ToString();
            root.AppendChild(text);

            SettingsHelper.CreateSetting(doc, root, "TextFont", TextFont);

            return root;
        }
    }
}
