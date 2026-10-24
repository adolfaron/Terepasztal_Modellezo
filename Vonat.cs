using System;
using System.Collections.Generic;
using Godot;
using static System.Text.Json.JsonSerializer;

public partial class Vonat : Node
{
    private Main main;

    private Node3D vonat;
    private Main.KeszGorbe vonatGorbe { get;
        set => field = value;//Deserialize<Main.KeszGorbe>(Serialize(value)); 
    }

    private const int vonatMintakSzama = 200;

    private readonly List<float> vonatMintakTavok = new();

    public float vonatSebesseg; // világ-egység / másodperc
    public float vonatMegtettUt { get; private set; }
    public float vonatGorbeHossz { get; private set; }

    private bool P0kezd;



    public PackedScene vonatJelenet { get; set { 
           if (vonatJelenet == null)
                field = value;
        } }


    public Vonat(Main mainNode)
    {
        main = mainNode;
        vonatSebesseg = 5.0f; // világ-egység / másodperc
        vonatMegtettUt = 0.0f;
        vonatGorbeHossz = 0.0f;
}

    public void Beallitas(Main mainNode)
    {
        main = mainNode;
    }

    public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
        VonatMozgatasa(delta);
    }

    private void VonatLetrehozasa()
    {
        if (vonatJelenet == null)
        {
            main.kiir("Hiba: a vonat jelenete nem tölthető be!");
            return;
        }

        Node3D vonat = vonatJelenet.Instantiate<Node3D>();

        //AddChild(vonat);

        vonat.Position = new Vector3(0, 0, 0);
        vonat.Rotation = Vector3.Zero;
        vonat.Scale = Vector3.One;
    }

    public void VonatInditasa(int id, bool elore)
    {
        // Ha már fut egy vonat, ne indítsunk másikat.
        if (vonat != null && IsInstanceValid(vonat))
            return;
        P0kezd = elore;
        vonatGorbe = main.GorbeKereseseIDAlapjan(id)?.Masolat();
        if (!elore)
        {
            Main.KeszGorbe ideiglenes = vonatGorbe.Masolat();
            vonatGorbe.P0 = ideiglenes.P2;
            vonatGorbe.P1 = ideiglenes.P3;
            vonatGorbe.P2 = ideiglenes.P0;
            vonatGorbe.P3 = ideiglenes.P1;
        }

        if (vonatGorbe == null || vonatGorbe.Tipus != 1)
        {
            main.kiir($"Nem található {id} ID-jú sima sín!");
            return;
        }



        if (vonatJelenet == null)
        {
            main.kiir("Nem tölthető be a vonat modellje!");
            return;
        }

        vonat = vonatJelenet.Instantiate<Node3D>();
        AddChild(vonat);

        VonatGorbeMintavetelezese();

        vonatMegtettUt = 0.0f;

        vonat.Position = VonatPontja(0.0f);

        main.kiir("A vonat elindult az 1-es sínen.");
    }
    private void VonatGorbeMintavetelezese()
    {
        vonatMintakTavok.Clear();
        vonatGorbeHossz = 0.0f;

        Vector3 elozoPont = VonatPontja(0.0f);
        vonatMintakTavok.Add(0.0f);

        for (int i = 1; i <= vonatMintakSzama; i++)
        {
            float t = (float)i / vonatMintakSzama;
            Vector3 pont = VonatPontja(t);

            vonatGorbeHossz += elozoPont.DistanceTo(pont);
            vonatMintakTavok.Add(vonatGorbeHossz);

            elozoPont = pont;
        }
    }
    private Vector3 VonatPontja(float t)
    {
        float u = 1.0f - t;

        return
            u * u * u * vonatGorbe.P0 +
            3.0f * u * u * t * vonatGorbe.P1 +
            3.0f * u * t * t * vonatGorbe.P3 +
            t * t * t * vonatGorbe.P2;
    }
    private void VonatMozgatasa(double delta)
    {
        if (vonat == null || !IsInstanceValid(vonat))
            return;

        if (vonatGorbeHossz <= 0.0f)
            return;

        vonatMegtettUt += vonatSebesseg * (float)delta;

        if (vonatMegtettUt >= vonatGorbeHossz)
        {
            vonatMegtettUt = vonatGorbeHossz;
            KovetkezoSin();
            return;
        }

        // Megkeressük a megtett úthoz tartozó görbeparamétert.
        int index = 1;

        while (index < vonatMintakTavok.Count - 1 &&
               vonatMintakTavok[index] < vonatMegtettUt)
        {
            index++;
        }

        float elozoTav = vonatMintakTavok[index - 1];
        float kovetkezoTav = vonatMintakTavok[index];

        float arany = kovetkezoTav > elozoTav
            ? (vonatMegtettUt - elozoTav) / (kovetkezoTav - elozoTav)
            : 0.0f;

        float t = (index - 1 + arany) / vonatMintakSzama;

        Vector3 pont = VonatPontja(t);
        float kovetkezoT = Mathf.Min(t + 0.001f, 1.0f);
        Vector3 iranyPont = VonatPontja(kovetkezoT);

        vonat.Position = pont;

        if (pont.DistanceSquaredTo(iranyPont) > 0.000001f)
            vonat.LookAt(iranyPont, Vector3.Up);
    }

    /*private void KovetkezoSin()
    {
        if (main == null)
        {
            main.kiir("HIBA: main == null");
            return;
        }

        if (vonatGorbe == null)
        {
            main.kiir("HIBA: vonatGorbe == null");
            return;
        }

        int kovetkezoID = P0kezd
            ? vonatGorbe.P0KapcsolatID
            : vonatGorbe.P2KapcsolatID;

        //main.kiir($"Következő sín keresése: {kovetkezoID}");
        //GD.Print($"Következő sín keresése: {kovetkezoID}");

        Main.KeszGorbe kovetkezo =
            main.GorbeKereseseIDAlapjan(kovetkezoID);

        if (kovetkezo == null)
        {
            //main.kiir($"HIBA: nincs ilyen sín: {kovetkezoID}");
            return;
        }
        vonatGorbe = kovetkezo.Masolat();
        VonatGorbeMintavetelezese();
        vonatMegtettUt = 0.0f;
        vonat.Position = VonatPontja(0.0f);
        
    }*/

    private void KovetkezoSin()
    {
        if (main == null)
            return;

        if (vonatGorbe == null)
            return;

        // Ha a vonat előrefelé indult, a görbe P2 végén érkezik meg.
        // Ha visszafelé indult, a görbe P0 végén érkezik meg.
        int kovetkezoID = P0kezd
            ? vonatGorbe.P2KapcsolatID
            : vonatGorbe.P0KapcsolatID;

        Main.KeszGorbe kovetkezo =
            main.GorbeKereseseIDAlapjan(kovetkezoID).Masolat();

        if (kovetkezo.Tipus == 2)
        {
            // Ha a következő sín váltó, akkor a vonatnak meg kell várnia, amíg a váltó átáll.
            
            if (kovetkezo.ValtoAllasErtek == 0)
            {
                //main.kiir($"Várakozás a váltóra: {kovetkezoID}");
                new Exception("Várakozás a váltóra: " + kovetkezoID);
                return;
            }
            if (kovetkezo.ValtoAllasErtek == kovetkezo.letezoSinKapcs[0])
            {
                kovetkezo = main.GorbeKereseseIDAlapjan(kovetkezo.letezoSinKapcs[1]);
            }
            else
            {
                kovetkezo.Allasok.Insert(kovetkezo.letezoSinKapcs[0]-1, new Main.KeszGorbe());
                kovetkezo = kovetkezo.Allasok[kovetkezo.ValtoAllasErtek-1];
                kovetkezoID = kovetkezo.ID;
            }

        }

        if (kovetkezo == null)
        {
            main.kiir($"Nincs következő sín: {kovetkezoID}");
            return;
        }
        if(kovetkezo.Tipus != 1)
        {
            main.kiir($"Nincs következő sima sín: {kovetkezoID}");
            return;
        }

        // Ha visszafelé haladunk, megfordítjuk a saját másolatát.
        if (kovetkezo.P0KapcsolatID != vonatGorbe.ID)
        {
            P0kezd = false;
            Main.KeszGorbe regi = kovetkezo.Masolat();

            kovetkezo.P0 = regi.P2;
            kovetkezo.P1 = regi.P3;
            kovetkezo.P2 = regi.P0;
            kovetkezo.P3 = regi.P1;
        }
        else
        {
            P0kezd = true;
        }

        // Először beállítjuk az új görbét!
        vonatGorbe = kovetkezo.Masolat();

        

        // Az új sínhez tartozó adatok újraszámítása.
        vonatMegtettUt = 0.0f;
        VonatGorbeMintavetelezese();
        vonat.Position = VonatPontja(0.0f);
    }
}
