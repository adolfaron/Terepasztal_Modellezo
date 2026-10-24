
using Godot;

public partial class Menu : Control
{
    private Main main;
    private static bool letrehozva
    {
        get; set
        {
            field = true;
        }
    }

    public Menu(Main mainNode)
    {
        if (letrehozva)
            return;
        main = mainNode;
        FelsoMenuLetrehozasa();
        ////main.kiir("Felső menü létrehozva.");
        letrehozva = true;
    }
    public enum Mod
    {
        Szerkesztes,
        Jatek,
        Beallitasok
    }

    [Signal]
    public delegate void ModValtozottEventHandler(Mod ujMod);

    private Button szerkesztesGomb;
    private Button jatekGomb;
    private Button beallitasokGomb;

    public PanelContainer panel;

    public static int szerkMod = 1;//1: építés

    private void FelsoMenuLetrehozasa()
    {
        CanvasLayer canvasLayer = new CanvasLayer();
        AddChild(canvasLayer);

        panel = new PanelContainer();
        canvasLayer.AddChild(panel);

        panel.SetAnchorsAndOffsetsPreset(
            Control.LayoutPreset.TopWide);

        panel.CustomMinimumSize = new Vector2(0, 65);

        HBoxContainer sor = new HBoxContainer();
        panel.AddChild(sor);

        Label cimke = new Label();
        cimke.Text = "SÍNÉPÍTŐ";
        cimke.VerticalAlignment = VerticalAlignment.Center;
        sor.AddChild(cimke);

        Button szerkesztes = new Button();
        szerkesztes.Text = "Szerkesztés";
        szerkesztes.ToggleMode = true;
        szerkesztes.ButtonPressed = true;
        sor.AddChild(szerkesztes);

        Button jatek = new Button();
        jatek.Text = "Játék";
        jatek.ToggleMode = true;
        sor.AddChild(jatek);

        Button beallitasok = new Button();
        beallitasok.Text = "Beállítások";
        beallitasok.ToggleMode = true;
        sor.AddChild(beallitasok);

        szerkesztes.Pressed += () =>
            ModValtas(0, szerkesztes, jatek, beallitasok);

        jatek.Pressed += () =>
            ModValtas(1, szerkesztes, jatek, beallitasok);

        beallitasok.Pressed += () =>
            ModValtas(2, szerkesztes, jatek, beallitasok);
    }

    private void ModValtas(
        int mod,
        Button szerkesztes,
        Button jatek,
        Button beallitasok)
    {
        szerkesztes.ButtonPressed = mod == 0;
        jatek.ButtonPressed = mod == 1;
        beallitasok.ButtonPressed = mod == 2;

        switch (mod)
        {
            case 0:
                //main.kiir("Szerkesztés mód");
                main.EpitesMegszakitasa();
                szerkMod = 1;
                break;
            case 1:
                //main.kiir("Játék mód");
                main.EpitesMegszakitasa();
                szerkMod = 2;
                main.kiiras.Text = "Játék mód";
                break;
            case 2:
                //main.kiir("Beállítások mód");
                main.EpitesMegszakitasa();
                szerkMod = 3;
                main.kiiras.Text = "Beállítások mód";
                break;
        }
    }

    public bool MenuFelettVanAzEger()
    {
        if (panel == null)
            return false;

        return panel.GetGlobalRect().HasPoint(
            panel.GetGlobalTransformWithCanvas()
                .AffineInverse()
                .BasisXform(panel.GetViewport().GetMousePosition())
        );
    }

}
