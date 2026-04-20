using System.Drawing;
using System.Windows.Forms;

namespace SimpleYoutubeDownloader;

/// <summary>Tema escuro + botões no estilo Bootstrap (flat, borda 1px, hovers).</summary>
internal static class UiTheme
{
    public static readonly Color Background = Color.FromArgb(28, 28, 30);
    public static readonly Color Surface = Color.FromArgb(38, 38, 42);
    public static readonly Color SurfaceElevated = Color.FromArgb(48, 48, 54);
    public static readonly Color Border = Color.FromArgb(62, 62, 70);
    public static readonly Color TextPrimary = Color.FromArgb(238, 238, 242);
    public static readonly Color TextMuted = Color.FromArgb(160, 160, 172);
    public static readonly Color LogBackground = Color.FromArgb(22, 22, 24);

    // Bootstrap 5 (adaptado ao fundo escuro)
    public static readonly Color BsPrimary = Color.FromArgb(13, 110, 253);
    public static readonly Color BsPrimaryHover = Color.FromArgb(10, 88, 202);
    public static readonly Color BsSecondaryBorder = Color.FromArgb(108, 117, 125);
    public static readonly Color BsSecondaryHoverBorder = Color.FromArgb(142, 150, 157);
    public static readonly Color BsSecondaryHoverBg = Color.FromArgb(52, 58, 66);
    public static readonly Color BsDanger = Color.FromArgb(220, 53, 69);
    public static readonly Color BsDangerHoverBg = Color.FromArgb(60, 28, 34);
    public static readonly Color BsDangerText = Color.FromArgb(248, 215, 218);
    public static readonly Color BsLink = Color.FromArgb(110, 168, 254);
    public static readonly Color BsLinkHover = Color.FromArgb(140, 190, 255);

    public static Font TitleFont { get; } = new("Segoe UI", 11.25f, FontStyle.Bold);
    public static Font BodyFont { get; } = new("Segoe UI", 9.75f);
    public static Font MonoFont { get; } = new("Consolas", 9f, FontStyle.Regular);

    public static void StyleForm(Form form)
    {
        form.BackColor = Background;
        form.ForeColor = TextPrimary;
        form.Font = BodyFont;
    }

    public static void StyleLabel(Label label, bool muted = false)
    {
        label.BackColor = Color.Transparent;
        label.ForeColor = muted ? TextMuted : TextPrimary;
        label.UseMnemonic = true;
    }

    public static void StyleTextBox(TextBox box, bool readOnly = false, bool monospace = false)
    {
        box.BorderStyle = BorderStyle.FixedSingle;
        box.BackColor = readOnly ? LogBackground : SurfaceElevated;
        box.ForeColor = TextPrimary;
        if (monospace) box.Font = MonoFont;
        box.CausesValidation = false;
    }

    public static void StyleComboBox(ComboBox cb)
    {
        cb.FlatStyle = FlatStyle.Flat;
        cb.BackColor = SurfaceElevated;
        cb.ForeColor = TextPrimary;
        cb.DropDownStyle = ComboBoxStyle.DropDownList;
        cb.IntegralHeight = false;
        cb.Cursor = Cursors.Hand;
    }

    /// <summary>Etiqueta compacta (tipo tag) ao lado do formato.</summary>
    public static void StyleUrlKindTag(Label lbl)
    {
        lbl.AutoSize = true;
        lbl.Padding = new Padding(10, 5, 10, 5);
        lbl.Margin = new Padding(14, 4, 0, 0);
        lbl.BorderStyle = BorderStyle.FixedSingle;
        lbl.BackColor = Surface;
        lbl.ForeColor = TextMuted;
        lbl.TextAlign = ContentAlignment.MiddleCenter;
        lbl.UseMnemonic = false;
    }

    public static void StyleNumeric(NumericUpDown nud)
    {
        nud.BackColor = SurfaceElevated;
        nud.ForeColor = TextPrimary;
        nud.BorderStyle = BorderStyle.FixedSingle;
    }

    /// <summary>btn-primary: fundo azul, texto branco.</summary>
    /// <param name="fillWidth">true = barra inferior (Dock Fill, sem AutoSize).</param>
    public static void ApplyBootstrapPrimary(Button btn, bool fillWidth = false)
    {
        btn.UseVisualStyleBackColor = false;
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.BackColor = BsPrimary;
        btn.ForeColor = Color.White;
        btn.Cursor = Cursors.Hand;
        btn.Padding = new Padding(20, 10, 20, 10);
        if (fillWidth)
        {
            btn.AutoSize = false;
            btn.Dock = DockStyle.Fill;
            btn.Margin = Padding.Empty;
            btn.Height = 42;
        }
        else
        {
            btn.AutoSize = true;
            btn.MinimumSize = new Size(0, 38);
            btn.Margin = new Padding(0, 0, 8, 0);
        }

        WirePrimaryHover(btn);
    }

    /// <summary>btn-outline-secondary: borda cinza, fundo do painel.</summary>
    public static void ApplyBootstrapOutlineSecondary(Button btn, Color toolbarBack)
    {
        btn.UseVisualStyleBackColor = false;
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.BorderColor = BsSecondaryBorder;
        btn.BackColor = toolbarBack;
        btn.ForeColor = TextPrimary;
        btn.Cursor = Cursors.Hand;
        btn.Padding = new Padding(18, 9, 18, 9);
        btn.AutoSize = true;
        btn.MinimumSize = new Size(0, 38);
        btn.Margin = new Padding(0, 0, 8, 0);
        WireOutlineSecondaryHover(btn, toolbarBack);
    }

    /// <summary>btn-outline-danger.</summary>
    public static void ApplyBootstrapOutlineDanger(Button btn, Color toolbarBack)
    {
        btn.UseVisualStyleBackColor = false;
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.BorderColor = BsDanger;
        btn.BackColor = toolbarBack;
        btn.ForeColor = BsDangerText;
        btn.Cursor = Cursors.Hand;
        btn.Padding = new Padding(18, 9, 18, 9);
        btn.AutoSize = true;
        btn.MinimumSize = new Size(0, 38);
        btn.Margin = new Padding(0, 0, 8, 0);
        WireOutlineDangerHover(btn, toolbarBack);
    }

    /// <summary>btn-link: sem borda, cor de link.</summary>
    public static void ApplyBootstrapLink(Button btn, Color toolbarBack)
    {
        btn.UseVisualStyleBackColor = false;
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.BackColor = toolbarBack;
        btn.ForeColor = BsLink;
        btn.Cursor = Cursors.Hand;
        btn.Padding = new Padding(12, 9, 12, 9);
        btn.AutoSize = true;
        btn.MinimumSize = new Size(0, 38);
        btn.Margin = new Padding(8, 0, 0, 0);
        WireLinkHover(btn);
    }

    private static void WirePrimaryHover(Button btn)
    {
        btn.MouseEnter += (_, _) => btn.BackColor = BsPrimaryHover;
        btn.MouseLeave += (_, _) => btn.BackColor = BsPrimary;
    }

    private static void WireOutlineSecondaryHover(Button btn, Color toolbarBack)
    {
        btn.MouseEnter += (_, _) =>
        {
            btn.BackColor = BsSecondaryHoverBg;
            btn.FlatAppearance.BorderColor = BsSecondaryHoverBorder;
        };
        btn.MouseLeave += (_, _) =>
        {
            btn.BackColor = toolbarBack;
            btn.FlatAppearance.BorderColor = BsSecondaryBorder;
        };
    }

    private static void WireOutlineDangerHover(Button btn, Color toolbarBack)
    {
        btn.MouseEnter += (_, _) =>
        {
            btn.BackColor = BsDangerHoverBg;
            btn.ForeColor = Color.White;
        };
        btn.MouseLeave += (_, _) =>
        {
            btn.BackColor = toolbarBack;
            btn.ForeColor = BsDangerText;
        };
    }

    private static void WireLinkHover(Button btn)
    {
        btn.MouseEnter += (_, _) => btn.ForeColor = BsLinkHover;
        btn.MouseLeave += (_, _) => btn.ForeColor = BsLink;
    }
}
