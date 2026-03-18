using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using LiveSplit.Model;
using LiveSplit.UI;
using LiveSplit.UI.Components;

namespace LiveSplit.PhasmophobiaContractResolver
{
    public class ContractResolverComponent : IComponent
    {
        private readonly ContractResolverSettings settings;
        private readonly ContractResolverMemory memory;

        public string ComponentName => "Contract Resolver";

        public float HorizontalWidth => 240f;
        public float MinimumHeight => 36f;
        public float VerticalHeight => 52f;
        public float MinimumWidth => 80f;
        public float PaddingTop => 0f;
        public float PaddingBottom => 0f;
        public float PaddingLeft => 0f;
        public float PaddingRight => 0f;

        public IDictionary<string, Action> ContextMenuControls => new Dictionary<string, Action>();

        public ContractResolverComponent(LiveSplitState state)
        {
            settings = new ContractResolverSettings();
            memory = new ContractResolverMemory();
        }

        public Control GetSettingsControl(LayoutMode mode) => settings;

        public XmlNode GetSettings(XmlDocument document) => settings.GetSettings(document);

        public void SetSettings(XmlNode settingsNode) => settings.SetSettings(settingsNode);

        public void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode)
        {
            try
            {
                memory.Update();
            }
            catch
            {
                // Keep layout alive even if memory read fails this frame.
            }
            invalidator?.Invalidate(0, 0, width, height);
        }

        public void DrawHorizontal(Graphics g, LiveSplitState state, float height, Region clipRegion)
        {
            Draw(g, HorizontalWidth, height);
        }

        public void DrawVertical(Graphics g, LiveSplitState state, float width, Region clipRegion)
        {
            Draw(g, width, VerticalHeight);
        }

        private void Draw(Graphics g, float width, float height)
        {
            float contentX = 0f;
            float contentY = 0f;
            float contentW = width;
            float contentH = height;
            if (contentW <= 0 || contentH <= 0)
                return;

            Color effectiveBackground = settings.BackgroundColor.A < 24
                ? Color.FromArgb(200, 0, 0, 0)
                : settings.BackgroundColor;

            if (effectiveBackground.A > 0)
            {
                using (var bg = new SolidBrush(effectiveBackground))
                    g.FillRectangle(bg, contentX, contentY, contentW, contentH);
            }

            Color effectiveText = settings.TextColor.A < 24
                ? Color.White
                : settings.TextColor;
            if (effectiveText.ToArgb() == effectiveBackground.ToArgb())
                effectiveText = effectiveBackground.GetBrightness() > 0.5f ? Color.Black : Color.White;

            Font drawFont = settings.TextFont;
            if (drawFont == null || drawFont.Size < 6f)
                drawFont = SystemFonts.DefaultFont;

            using (var brush = new SolidBrush(effectiveText))
            using (var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            })
            {
                float textX = contentX + 2f;
                float textW = Math.Max(0f, contentW - 4f);

                if (memory.DisplayMode != ResolverDisplayMode.Resolved)
                {
                    string status = string.IsNullOrWhiteSpace(memory.SingleLineText) ? "Awaiting Contract" : memory.SingleLineText;
                    g.DrawString(status, drawFont, brush, new RectangleF(textX, contentY, textW, contentH), format);
                    return;
                }

                string line1 = string.IsNullOrWhiteSpace(memory.CursedText) ? "Finding Cursed Possession" : memory.CursedText;
                string line2 = string.IsNullOrWhiteSpace(memory.BoneText) ? "Finding Bone Room" : memory.BoneText;
                float lineHeight = contentH * 0.5f;
                g.DrawString(line1, drawFont, brush, new RectangleF(textX, contentY, textW, lineHeight), format);
                g.DrawString(line2, drawFont, brush, new RectangleF(textX, contentY + lineHeight, textW, lineHeight), format);
            }
        }

        public void Dispose()
        {
            memory.Dispose();
        }
    }
}
