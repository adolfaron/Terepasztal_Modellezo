using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.IO;

//kövi feladat, váltók szerkeszthetőslge (ág hozzáadása) és sín törölhetősége
//továbbá ne lehessen egymásra tenni a sínt, csak X-et (ellenőrzés kell)
//váltó gombja játék módba nyomható legyen és álítsa a hozzá tartozó váltót

public partial class Main : Node3D
{
	private Camera3D kamera;
	public Label kiiras;

	Menu menu;


    private MeshInstance3D eloSín;
	private MeshInstance3D eloTalpfa;
	private MeshInstance3D segedVonal;

	private MeshInstance3D csatlakozasiJelolo;

	private const float VegpontKeresesiTav = 0.8f;
	private Vector3? kozeliVegpont = null;

	private int kovetkezoGorbeID = 1;//soha sem lehet 0

	private readonly Vector3[] aktualisGorbe = new Vector3[4];
	private readonly MeshInstance3D[] segedPontok = new MeshInstance3D[4];

	private int kattAllapot = 0;
	private int valtoEpitsAllapot = 0;

	private Vector3 eloPozicio;
	private bool vanEloPozicio = false;

	private const float Nyomtav = 1.0f;
	private const float SinSzelesseg = 0.12f;
	private const float SinMagassag = 0.14f;
	private const float TalpfaTav = 0.45f;
	private const float TalpfaHossz = 0.22f;
	private const float TalpfaMagassag = 0.12f;
	private const float SinMagassagTalajtol = 0.20f;

	private const float MinimumVektorHossz = 3f;
	private const float MinimumKanyarSugar = 3.0f;

	private bool eloGorbeTulEles;
	private StandardMaterial3D tulElesElonezetiAnyag;

	private StandardMaterial3D sinAnyag;
	private StandardMaterial3D talpfaAnyag;
	private StandardMaterial3D segedVonalAnyag;

	private BoxMesh sinAlak;
	private BoxMesh talpfaAlak;

	public readonly List<KeszGorbe> gorbek = new();

	private KeszGorbe kijeloltGorbe;

	private KeszGorbe csatlakozoGorbe;
	private bool csatlakozasKezdoVeghez;

	private int aktP0KapcsolatID = 0;
	private int aktP2KapcsolatID = 0;

	private bool kezdoCsatlakozik = false;
	private bool vegCsatlakozik = false;

	private Vector3 kezdoCsatlakozasiIrany = Vector3.Zero;
	private Vector3 vegCsatlakozasiIrany = Vector3.Zero;

	private bool kezdoKapcsolodoVegP0 = false;
	private bool vegKapcsolodoVegP0 = false;

	private CanvasLayer feliratReteg = new CanvasLayer();
	

	KeszGorbe ujValto;
	int valtoAgakSzama;
	Vector3 ujValtoIrany;
	int[] ujValtoLetezoSinKapcs = new int[2];
	Vonat vonat1;


    


    public class KeszGorbe
	{
		public int ID;

		public int Tipus;//1 sín, 2 váltó

		public Vector3 P0;
		public Vector3 P1;
		public Vector3 P2;
		public Vector3 P3;

		public MeshInstance3D Sin;
		public MeshInstance3D Talpfa;

		public int P0KapcsolatID = 0;
		public int P2KapcsolatID = 0;

		public List<KeszGorbe> Allasok = new List<KeszGorbe>();
		public int[] letezoSinKapcs = new int[2];//sorszám, ID

		public int ValtoAllasErtek = 0;

        public KeszGorbe Masolat()
        {
            return (KeszGorbe)MemberwiseClone();
        }
    }

	public override void _Ready()
	{
        //kiir("Játék indul");

		menu = new Menu(this);
		AddChild(menu);

        kamera = new Camera3D();
		AddChild(kamera);
		kamera.Position = new Vector3(12, 14, 16);
		kamera.LookAt(new Vector3(5, 0, 5), Vector3.Up);
		kamera.Current = true;

		var feny = new DirectionalLight3D();
		AddChild(feny);
		feny.RotationDegrees = new Vector3(-50, -30, 0);

		var talaj = new MeshInstance3D();
		talaj.Mesh = new PlaneMesh { Size = new Vector2(100, 100) };

		var talajAnyag = new StandardMaterial3D();
		talajAnyag.AlbedoColor = new Color(0.20f, 0.23f, 0.20f);
		talaj.MaterialOverride = talajAnyag;
		talaj.Position = new Vector3(0, -0.05f, 0);
		AddChild(talaj);

        sinAnyag = new StandardMaterial3D();
		sinAnyag.AlbedoColor = new Color(0.65f, 0.68f, 0.72f);
		sinAnyag.Metallic = 0.75f;
		sinAnyag.Roughness = 0.30f;

		talpfaAnyag = new StandardMaterial3D();
		talpfaAnyag.AlbedoColor = new Color(0.30f, 0.17f, 0.08f);
		talpfaAnyag.Roughness = 0.9f;

		segedVonalAnyag = new StandardMaterial3D();
		segedVonalAnyag.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
		segedVonalAnyag.AlbedoColor = Colors.Cyan;

		csatlakozasiJelolo = new MeshInstance3D();

		csatlakozasiJelolo.Mesh = new SphereMesh
		{
			Radius = 0.24f,
			Height = 0.48f
		};

		AddChild(feliratReteg);
		

		var csatlakozasiAnyag = new StandardMaterial3D();
		csatlakozasiAnyag.AlbedoColor = Colors.Yellow;
		csatlakozasiAnyag.ShadingMode =
			BaseMaterial3D.ShadingModeEnum.Unshaded;

		csatlakozasiJelolo.MaterialOverride = csatlakozasiAnyag;
		csatlakozasiJelolo.Visible = false;

		AddChild(csatlakozasiJelolo);

		sinAlak = new BoxMesh
		{
			Size = new Vector3(SinSzelesseg, SinMagassag, 1.0f)
		};

		talpfaAlak = new BoxMesh
		{
			Size = new Vector3(Nyomtav + 0.35f, TalpfaMagassag, TalpfaHossz)
		};

		eloSín = new MeshInstance3D();
		eloSín.MaterialOverride = sinAnyag;
		AddChild(eloSín);

		eloTalpfa = new MeshInstance3D();
		eloTalpfa.MaterialOverride = talpfaAnyag;
		AddChild(eloTalpfa);

		segedVonal = new MeshInstance3D();
		segedVonal.MaterialOverride = segedVonalAnyag;
		AddChild(segedVonal);

		for (int i = 0; i < segedPontok.Length; i++)
		{
			segedPontok[i] = new MeshInstance3D();
			segedPontok[i].Mesh = new SphereMesh
			{
				Radius = 0.18f,
				Height = 0.36f
			};

			var anyag = new StandardMaterial3D();
			anyag.AlbedoColor = i switch
			{
				0 => Colors.LimeGreen,
				1 => Colors.Cyan,
				2 => Colors.Orange,
				_ => Colors.Cyan
			};

			segedPontok[i].MaterialOverride = anyag;
			segedPontok[i].Visible = false;
			AddChild(segedPontok[i]);
		}

		var felulet = new CanvasLayer();
		AddChild(felulet);

		kiiras = new Label();
		kiiras.Position = new Vector2(20, 80);
		kiiras.AddThemeFontSizeOverride("font_size", 20);
		felulet.AddChild(kiiras);

		UtmutatoFrissitese();
	}

	public override void _Input(InputEvent esemeny)
	{
        if (esemeny is InputEventKey billentyu &&
			billentyu.Pressed &&
			!billentyu.Echo)
        {
            if (billentyu.Keycode == Key.F5)
            {
                GorbekMenteseJsonba();
                return;
            }

            if (billentyu.Keycode == Key.F6)
            {
                GorbekBetolteseJsonbol();
                return;
            }
            if (billentyu.Keycode == Key.F7)
            {
				//VonatLetrehozasa();
                vonat1 = new Vonat(this);
				vonat1.vonatJelenet = GD.Load<PackedScene>("res://TeherVonat.gltf");
				AddChild(vonat1);
                vonat1.VonatInditasa(1, false);

                return;
            }
        }
        
        if (esemeny is InputEventMouseMotion)
		{
            if (esemeny is InputEventMouseButton ||
				esemeny is InputEventMouseMotion)
            {
                if (menu != null)
					if (menu.MenuFelettVanAzEger())
						return;
            }

            if (EgerPozicioTalajon(out Vector3 pozicio) && Menu.szerkMod == 1)
			{
				if (kattAllapot == 0 || kattAllapot == 2)
				{
					kozeliVegpont = KozeliVegpontKeresese(pozicio);

					if (kozeliVegpont.HasValue)
					{
						eloPozicio = kozeliVegpont.Value;

						csatlakozasiJelolo.Position =
							eloPozicio + Vector3.Up * 0.25f;

						csatlakozasiJelolo.Visible = true;


						Vector3 irany = CsatlakozasiIrany();

						if (kattAllapot == 0)
						{
							kezdoCsatlakozik = true;
							kezdoCsatlakozasiIrany = irany;
						}
						else
						{
							vegCsatlakozik = true;
							vegCsatlakozasiIrany = irany;
						}
					}
					else
					{
						eloPozicio = pozicio;
						csatlakozasiJelolo.Visible = false;

						if (kattAllapot == 0)
						{
							kezdoCsatlakozik = false;
							kezdoCsatlakozasiIrany = Vector3.Zero;
						}
						else
						{
							vegCsatlakozik = false;
							vegCsatlakozasiIrany = Vector3.Zero;
						}
					}
				}
				else if (kattAllapot == 1)
				{
					eloPozicio = pozicio;

					if (kezdoCsatlakozik &&
						kezdoCsatlakozasiIrany.LengthSquared() > 0.0001f)
					{
						Vector3 irany = kezdoCsatlakozasiIrany.Normalized();

						float hossz = Mathf.Max(
							0.0f,
							(pozicio - aktualisGorbe[0]).Dot(irany)
						);

						eloPozicio = aktualisGorbe[0] + irany * hossz;
					}
					Vector3 kezdoVektor = eloPozicio - aktualisGorbe[0];

					if (kezdoVektor.Length() < MinimumVektorHossz)
					{
						Vector3 irany = kezdoCsatlakozik
							? kezdoCsatlakozasiIrany
							: Vector3.Forward;

						eloPozicio =
							aktualisGorbe[0] + irany * MinimumVektorHossz;
					}
					csatlakozasiJelolo.Visible = false;
				}
				else if (kattAllapot == 3)
				{
					// A végirány hossza is szabadon változtatható.
					eloPozicio = pozicio;

					if (vegCsatlakozik)
					{
						float hossz = Mathf.Max(
							0.0f,
							(pozicio - aktualisGorbe[2])
							.Dot(vegCsatlakozasiIrany)
						);

						eloPozicio = aktualisGorbe[2]
							+ vegCsatlakozasiIrany * hossz;
					}
					Vector3 vegVektor = eloPozicio - aktualisGorbe[2];

					if (vegVektor.Length() < MinimumVektorHossz)
					{
						Vector3 irany = vegCsatlakozik
							? vegCsatlakozasiIrany
							: Vector3.Forward;

						eloPozicio =
							aktualisGorbe[2] + irany * MinimumVektorHossz;
					}
					csatlakozasiJelolo.Visible = false;
				}

				vanEloPozicio = true;

				if (kattAllapot > 0 && kattAllapot < 4)
				{
					ElojeloloFrissitese();
					EloKinezetFrissitese();
				}
			}
		}

		/*if (esemeny is InputEventKey billentyu &&
			billentyu.Pressed &&
			!billentyu.Echo &&
			billentyu.Keycode == Key.Delete)
		{
			GorbeTorlese();
			return;
		}*/

		if (esemeny is InputEventMouseButton kattintas &&
			kattintas.Pressed)
		{
			if (kattintas.ButtonIndex == MouseButton.Right)
			{
				if (EgerPozicioTalajon(out Vector3 kattPozicio) && Menu.szerkMod == 1)
				{
					Vector3? talaltVegpont =
						KozeliVegpontKeresese(kattPozicio);

					if (talaltVegpont.HasValue && csatlakozoGorbe != null)
					{
						ValtoEpites(csatlakozoGorbe.ID, csatlakozasKezdoVeghez);
					}
					else
					{
						EpitesMegszakitasa();
						if (valtoEpitsAllapot != 0)
							kattAllapot = 1;
					}
				}

				return;
			}

			if (kattintas.ButtonIndex != MouseButton.Left)
				return;

			// Ha éppen épül egy ív, a bal kattintás a következő pontot adja meg.
			if (kattAllapot < 4 && Menu.szerkMod == 1)
			{
				if (EgerPozicioTalajon(out Vector3 kattPozicio))
				{
					/*if (kattAllapot == 0 || kattAllapot == 2)
					{
						Vector3? talaltVegpont =
							KozeliVegpontKeresese(kattPozicio);

						if (talaltVegpont.HasValue)
							kattPozicio = talaltVegpont.Value;
					}*/

					if (kattAllapot == 1 || kattAllapot == 3)
					{
						// Pontosan azt a pozíciót rögzítjük,
						// amit az előnézetben látunk.
						kattPozicio = eloPozicio;
					}
					else
					{
						Vector3? talaltVegpont =
							KozeliVegpontKeresese(kattPozicio);

						if (talaltVegpont.HasValue)
						{
							kattPozicio = talaltVegpont.Value;

							if (csatlakozoGorbe != null &&
								VegpontFoglalt(csatlakozoGorbe, csatlakozasKezdoVeghez))
							{
								kiir("Ez a végpont már foglalt, nem lehet ide csatlakozni!");
								return;
							}

							Vector3 irany = CsatlakozasiIrany();

							if (kattAllapot == 0)
							{
								kezdoCsatlakozik = true;
								kezdoCsatlakozasiIrany = irany;
								aktP0KapcsolatID = csatlakozoGorbe.ID;
								kezdoKapcsolodoVegP0 = csatlakozasKezdoVeghez;

							}
							else
							{
								vegCsatlakozik = true;
								vegCsatlakozasiIrany = irany;
								aktP2KapcsolatID = csatlakozoGorbe.ID;
								vegKapcsolodoVegP0 = csatlakozasKezdoVeghez;
							}
						}
						else if (kattAllapot == 0)
						{
							kezdoCsatlakozik = false;
							kezdoCsatlakozasiIrany = Vector3.Zero;
						}
						else
						{
							vegCsatlakozik = false;
							vegCsatlakozasiIrany = Vector3.Zero;
						}
					}

					aktualisGorbe[kattAllapot] = kattPozicio;

					segedPontok[kattAllapot].Position =
						kattPozicio + Vector3.Up * 0.15f;

					segedPontok[kattAllapot].Visible = true;

					kattAllapot++;


					if (kattAllapot == 4)
					{
						var ellenorzo = new KeszGorbe
						{
							P0 = aktualisGorbe[0],
							P1 = aktualisGorbe[1],
							P2 = aktualisGorbe[2],
							P3 = aktualisGorbe[3]
						};

						if (GorbeTulElese(ellenorzo))
						{
							// Maradjon szerkesztésben, hogy javíthasd a görbét.
							kattAllapot = 3;
							eloPozicio = aktualisGorbe[3];
							vanEloPozicio = true;

							ElojeloloFrissitese();
							EloKinezetFrissitese();
							UtmutatoFrissitese();

							return;
						}
						if (valtoEpitsAllapot == 0)
							GorbeVeglegesitese();
						else
							ValtoAllasVeglegesitese();
					}

					else
					{
						eloPozicio = kattPozicio;
						vanEloPozicio = true;
					}

					ElojeloloFrissitese();
					EloKinezetFrissitese();
					UtmutatoFrissitese();
				}
			}
			else
			{
				// Kész ívre kattintva kijelöljük azt.
				KeszGorbeKivalasztasa();
			}
		}


	}

    public override void _Process(double delta)
    {
        //VonatMozgatasa(delta);
    }

    private void ValtoAllasVeglegesitese()
	{
		if (valtoEpitsAllapot == 1)
		{
			ujValto = new KeszGorbe
			{
				ID = kovetkezoGorbeID++,


				P0KapcsolatID = aktP0KapcsolatID,
				P2KapcsolatID = aktP2KapcsolatID,
				Tipus = 2,
				ValtoAllasErtek = 1,
                letezoSinKapcs = ujValtoLetezoSinKapcs
			};
			gorbek.Add(ujValto);
		}
		var gorbe = new KeszGorbe
		{

			ID = kovetkezoGorbeID++,
			P0 = aktualisGorbe[0],
			P1 = aktualisGorbe[1],
			P2 = aktualisGorbe[2],
			P3 = aktualisGorbe[3],
			Sin = new MeshInstance3D(),
			Talpfa = new MeshInstance3D(),
            P0KapcsolatID = ujValto.P0KapcsolatID,
			P2KapcsolatID = aktP2KapcsolatID,
            Tipus = 1
		};



		gorbe.Sin.MaterialOverride = sinAnyag;
		gorbe.Talpfa.MaterialOverride = talpfaAnyag;

		AddChild(gorbe.Sin);
		AddChild(gorbe.Talpfa);

		GorbeGeometriaElkeszitese(gorbe);



        // Az új sín P0 végének kapcsolatát visszavezetjük a régi sínre.
        /*if (aktP0KapcsolatID != 0)
		{
			var regiGorbe = GorbeKereseseIDAlapjan(aktP0KapcsolatID);

			if (regiGorbe != null)
			{
				if (kezdoKapcsolodoVegP0)
					regiGorbe.P0KapcsolatID = ujValto.ID;
				else
					regiGorbe.P2KapcsolatID = ujValto.ID;
			}
		}

		// Az új sín P2 végének kapcsolatát visszavezetjük a régi sínre.
		if (aktP2KapcsolatID != 0)
        {
            var regiGorbe = GorbeKereseseIDAlapjan(aktP2KapcsolatID);

            if (regiGorbe != null)
            {
                if (vegKapcsolodoVegP0)
                    regiGorbe.P0KapcsolatID = ujValto.P0KapcsolatID;
                else
                    regiGorbe.P2KapcsolatID = ujValto.P0KapcsolatID;
            }
        }*/

        var regiGorbe = GorbeKereseseIDAlapjan(aktP0KapcsolatID);

        if (regiGorbe != null)
        {
            if (kezdoKapcsolodoVegP0)
                regiGorbe.P0KapcsolatID = ujValto.ID;
            else
                regiGorbe.P2KapcsolatID = ujValto.ID;
        }

        if (aktP2KapcsolatID != 0)
        {
            //var regiGorbe = GorbeKereseseIDAlapjan(aktP2KapcsolatID);

            if (regiGorbe != null)
            {
                if (vegKapcsolodoVegP0)
                    regiGorbe.P0KapcsolatID = ujValto.ID;
                else
                    regiGorbe.P2KapcsolatID = ujValto.ID;
            }
        }


        //gorbek.Add(gorbe);
        ujValto.Allasok.Add(gorbe);

        ValtoGombLerak(ujValto);

        EpitesMegszakitasa();

		if (valtoEpitsAllapot < valtoAgakSzama)
		{
			Vector3 kezdoPont = aktualisGorbe[0];

			//aktualisGorbe[0] = kezdoPont;

			//csatlakozoGorbe = kivalasztottSin;
			//csatlakozasKezdoVeghez = valasztoottP0Veg;

			kezdoCsatlakozik = true;
			kezdoCsatlakozasiIrany = ujValtoIrany;

			//aktP0KapcsolatID = kivalasztottSin.ID;
			//kezdoKapcsolodoVegP0 = valasztoottP0Veg;

			segedPontok[0].Position =
				kezdoPont + Vector3.Up * 0.15f;

			segedPontok[0].Visible = true;

			kattAllapot = 1;
			eloPozicio = kezdoPont;
			vanEloPozicio = true;

			ElojeloloFrissitese();
			EloKinezetFrissitese();
			UtmutatoFrissitese();

			valtoEpitsAllapot++;
		}
		else
		{
			valtoEpitsAllapot = 0;
			ujValto = null;
			valtoAgakSzama = 0;
			//ujValtoIrany=Nullable<Vector3>.Null;
			ujValtoLetezoSinKapcs = new int[2];

        }


		// Az elkészült ív után automatikusan új ív kezdhető.


	}

	private async void ValtoEpites(int iD, bool P0Veg)
	{
		KeszGorbe kattsin = GorbeKereseseIDAlapjan(iD);

		int sinId = iD;

		bool ketSinKoze = false;
		KeszGorbe valasztottSin = kattsin;

		bool masodikP0Veg = false;

		if ((P0Veg ? kattsin.P0KapcsolatID : kattsin.P2KapcsolatID) != 0)
		{
			KeszGorbe masodikGorbe = GorbeKereseseIDAlapjan(
				P0Veg ? kattsin.P0KapcsolatID : kattsin.P2KapcsolatID);
			ketSinKoze = true;
			Label elsoSzam = new Label();
			elsoSzam.Text = "1";
			elsoSzam.AddThemeFontSizeOverride("font_size", 32);
			feliratReteg.AddChild(elsoSzam);
			elsoSzam.Position = kamera.UnprojectPosition(
				(P0Veg ? kattsin.P2 : kattsin.P0) + new Vector3(0, 1, 0));

			Label masodikSzam = new Label();
			masodikSzam.Text = "2";
			masodikSzam.AddThemeFontSizeOverride("font_size", 32);
			feliratReteg.AddChild(masodikSzam);
			masodikSzam.Position = kamera.UnprojectPosition(
				(masodikGorbe.P0KapcsolatID == iD
					? masodikGorbe.P2
					: masodikGorbe.P0) + new Vector3(0, 1, 0));

			AcceptDialog dialog = new AcceptDialog();
			dialog.Title = "1 vagy 2 felől nyíljon a váltó?";

			LineEdit mezo = new LineEdit();
			mezo.PlaceholderText = "1 vagy 2";
			mezo.CustomMinimumSize = new Vector2(300, 0);

			dialog.AddChild(mezo);
			AddChild(dialog);

			// Ezt a feladatot várjuk meg az await-tel.
			TaskCompletionSource<int> valasz =
				new TaskCompletionSource<int>();

			dialog.Confirmed += () =>
			{
				string szoveg = mezo.Text;

				if (szoveg != "1" && szoveg != "2")
				{
					kiir("Csak 1 vagy 2-t lehet megadni!");
					return;
				}

				int kivalasztottSinId =
					szoveg == "1" ? kattsin.ID : masodikGorbe.ID;

				elsoSzam.QueueFree();
				masodikSzam.QueueFree();
				dialog.QueueFree();

				valasz.TrySetResult(kivalasztottSinId);
			};

			dialog.Canceled += () =>
			{
				elsoSzam.QueueFree();
				masodikSzam.QueueFree();
				dialog.QueueFree();

				valasz.TrySetResult(0);
			};

			dialog.PopupCentered();
			mezo.GrabFocus();

			// A metódus itt várakozik, amíg a felhasználó választ.
			sinId = await valasz.Task;
			valasztottSin = GorbeKereseseIDAlapjan(sinId);
			if (iD != sinId)
			{
				if (valasztottSin.P0KapcsolatID == iD)
				{
					masodikP0Veg = true;
				}
				else if (valasztottSin.P2KapcsolatID == iD)
				{
					masodikP0Veg = false;
				}
				else
				{
					kiir("A kiválasztott sín nem kapcsolódik az eredeti sínhez!");
					return;
				}
			}

			// Bezárás esetén ne folytassa az építést.
			if (sinId == 0)
			{
				kiir("Valamit választani kell!");
				return;
			}
		}

		// Ez a rész csak az első ablak lezárása után fut le.
		AcceptDialog agDialog = new AcceptDialog();
		agDialog.Title = $"Mennyi {(ketSinKoze ? "új " : "")}ága legyen a váltónak?";

		LineEdit agMezo = new LineEdit();
		agMezo.PlaceholderText = "2, 3, ...";
		agMezo.CustomMinimumSize = new Vector2(300, 0);

		agDialog.AddChild(agMezo);
		AddChild(agDialog);

		agDialog.Confirmed += async () =>
		{
			if (!int.TryParse(agMezo.Text, out valtoAgakSzama))
			{
				kiir("Érvényes egész számot adj meg!");
				return;
			}
			if ((valtoAgakSzama < 2 && !ketSinKoze) || (valtoAgakSzama < 1 && ketSinKoze))
			{
				kiir(ketSinKoze ? "Legalább 1 új ág kell!" : "Legalább 2 ágat kell megadni!");
				return;
			}

			agDialog.QueueFree();

			kiir($"{valtoAgakSzama} ágú váltó építése folyamatban...");
			kiir($"A kiválasztott sín ID-ja: {sinId}");

			int meglevoAgSorszam;

			if (ketSinKoze)
			{
				AcceptDialog agSorszamDialog = new AcceptDialog();
				agSorszamDialog.Title = "Hányadik ága legyen a meglévő sínnek?";

				LineEdit agSorszamMezo = new LineEdit();
				agSorszamMezo.PlaceholderText = $"1 - {valtoAgakSzama + 1}";
				agSorszamMezo.CustomMinimumSize = new Vector2(300, 0);

				agSorszamDialog.AddChild(agSorszamMezo);
				AddChild(agSorszamDialog);

				TaskCompletionSource<int> agSorszamValasz =
					new TaskCompletionSource<int>();

				agSorszamDialog.Confirmed += () =>
				{
                    int maxSorszam = valtoAgakSzama + (ketSinKoze ? 1 : 0);

                    if (!int.TryParse(agSorszamMezo.Text, out int agSorszam)
                        || agSorszam < 1
                        || agSorszam > maxSorszam)
                    {
                        kiir($"1 és {maxSorszam} közötti számot adj meg!");
                        return;
                    }

                    agSorszamDialog.QueueFree();
					agSorszamValasz.TrySetResult(agSorszam);
				};

				agSorszamDialog.Canceled += () =>
				{
					agSorszamDialog.QueueFree();
					agSorszamValasz.TrySetResult(0);
				};

				agSorszamDialog.PopupCentered();
				agSorszamMezo.GrabFocus();

				// Itt megvárja a választ.
				meglevoAgSorszam = await agSorszamValasz.Task;
				ujValtoLetezoSinKapcs[0] = meglevoAgSorszam;

				if (meglevoAgSorszam == 0)
				{
					kiir("Az ág sorszámának megadása megszakítva.");
					return;
				}

				kiir($"A meglévő sín a(z) {meglevoAgSorszam}. ágra kerül.");
			}



			valtoEpitsAllapot = 1;

			KeszGorbe kivalasztottSin = GorbeKereseseIDAlapjan(sinId);
			bool valasztoottP0Veg = P0Veg;
			ujValtoLetezoSinKapcs[1] = iD;

			if (iD != sinId)
			{
				valasztoottP0Veg = masodikP0Veg;
				ujValtoLetezoSinKapcs[1] = sinId;
			}

			if (kivalasztottSin == null)
				return;

			Vector3 kezdoPont = valasztoottP0Veg
				? kivalasztottSin.P0
				: kivalasztottSin.P2;

			aktualisGorbe[0] = kezdoPont;

			csatlakozoGorbe = kivalasztottSin;
			csatlakozasKezdoVeghez = valasztoottP0Veg;

			kezdoCsatlakozik = true;
			kezdoCsatlakozasiIrany = CsatlakozasiIrany();
			ujValtoIrany = kezdoCsatlakozasiIrany;

			aktP0KapcsolatID = kivalasztottSin.ID;
			kezdoKapcsolodoVegP0 = valasztoottP0Veg;

			segedPontok[0].Position =
				kezdoPont + Vector3.Up * 0.15f;

			segedPontok[0].Visible = true;

			kattAllapot = 1;

		};

		agDialog.Canceled += () => agDialog.QueueFree();

		agDialog.PopupCentered();
		agMezo.GrabFocus();
	}

	private bool EgerPozicioTalajon(out Vector3 pozicio)
	{
		Vector2 eger = GetViewport().GetMousePosition();
		Vector3 eredet = kamera.ProjectRayOrigin(eger);
		Vector3 irany = kamera.ProjectRayNormal(eger);

		if (Mathf.Abs(irany.Y) < 0.001f)
		{
			pozicio = Vector3.Zero;
			return false;
		}

		float tavolsag = -eredet.Y / irany.Y;

		if (tavolsag < 0)
		{
			pozicio = Vector3.Zero;
			return false;
		}

		pozicio = eredet + irany * tavolsag;
		pozicio.Y = 0;
		return true;
	}

	private void GorbeVeglegesitese()
	{
		var gorbe = new KeszGorbe
		{
			ID = kovetkezoGorbeID++,
			P0 = aktualisGorbe[0],
			P1 = aktualisGorbe[1],
			P2 = aktualisGorbe[2],
			P3 = aktualisGorbe[3],
			Sin = new MeshInstance3D(),
			Talpfa = new MeshInstance3D(),
			P0KapcsolatID = aktP0KapcsolatID,
			P2KapcsolatID = aktP2KapcsolatID,
			Tipus = 1
		};



		gorbe.Sin.MaterialOverride = sinAnyag;
		gorbe.Talpfa.MaterialOverride = talpfaAnyag;

		AddChild(gorbe.Sin);
		AddChild(gorbe.Talpfa);

		GorbeGeometriaElkeszitese(gorbe);

		/*if (csatlakozoGorbe != null)
		{
			// Az új sín P0 vége a meglévő sín kiválasztott végéhez csatlakozik.
			if (csatlakozasKezdoVeghez)
			{
				gorbe.P0KapcsolatID = csatlakozoGorbe.ID;
				csatlakozoGorbe.P0KapcsolatID = gorbe.ID;
			}
			else
			{
				gorbe.P0KapcsolatID = csatlakozoGorbe.ID;
				csatlakozoGorbe.P2KapcsolatID = gorbe.ID;
			}

			kiir($"Új sín ID: {gorbe.ID}");
			kiir($"Kapcsolódó sín ID: {csatlakozoGorbe.ID}");

		}*/

		// Az új sín P0 végének kapcsolatát visszavezetjük a régi sínre.
		if (aktP0KapcsolatID != 0)
		{
			var regiGorbe = GorbeKereseseIDAlapjan(aktP0KapcsolatID);

			if (regiGorbe != null)
			{
				if (kezdoKapcsolodoVegP0)
					regiGorbe.P0KapcsolatID = gorbe.ID;
				else
					regiGorbe.P2KapcsolatID = gorbe.ID;
			}
		}

		// Az új sín P2 végének kapcsolatát visszavezetjük a régi sínre.
		if (aktP2KapcsolatID != 0)
		{
			var regiGorbe = GorbeKereseseIDAlapjan(aktP2KapcsolatID);

			if (regiGorbe != null)
			{
				if (vegKapcsolodoVegP0)
					regiGorbe.P0KapcsolatID = gorbe.ID;
				else
					regiGorbe.P2KapcsolatID = gorbe.ID;
			}
		}

		/*kiir($"Új sín ID: {gorbe.ID}");
		kiir($"csatlakozó görbék: {gorbe.P0KapcsolatID}, {gorbe.P2KapcsolatID}");
		var kapcs1 = GorbeKereseseIDAlapjan(aktP0KapcsolatID);
		var kapcs2 = GorbeKereseseIDAlapjan(aktP2KapcsolatID);
        kiir($"csatlakozó görbékhez csatlakozó végek: {kapcs1?.P0KapcsolatID}, {kapcs1?.P2KapcsolatID}");
        kiir($"csatlakozó görbékhez csatlakozó végek: {kapcs2?.P0KapcsolatID}, {kapcs2?.P2KapcsolatID}");
		*/
		gorbek.Add(gorbe);

		// Az elkészült ív után automatikusan új ív kezdhető.
		EpitesMegszakitasa();

		//kiir($"Elkészült sínek száma: {gorbek.Count}");
	}

	private void GorbeGeometriaElkeszitese(KeszGorbe gorbe)
	{
		var pontok = new List<Vector3>();
		const int szakaszok = 100;

		for (int i = 0; i <= szakaszok; i++)
		{
			float t = (float)i / szakaszok;
			float u = 1.0f - t;

			Vector3 pont =
				u * u * u * gorbe.P0 +
				3.0f * u * u * t * gorbe.P1 +
				3.0f * u * t * t * gorbe.P3 +
				t * t * t * gorbe.P2;

			pontok.Add(pont);
		}

		var sinek = new SurfaceTool();
		sinek.Begin(Mesh.PrimitiveType.Triangles);

		var talpfak = new SurfaceTool();
		talpfak.Begin(Mesh.PrimitiveType.Triangles);

		float talpfaSzamlalo = 0;

		for (int i = 0; i < pontok.Count - 1; i++)
		{
			Vector3 a = pontok[i];
			Vector3 b = pontok[i + 1];

			Vector3 kulonbseg = b - a;
			float hossz = kulonbseg.Length();

			if (hossz < 0.0001f)
				continue;

			Vector3 irany = kulonbseg.Normalized();
			Vector3 oldal = Vector3.Up.Cross(irany).Normalized();

			if (oldal.LengthSquared() < 0.001f)
				continue;

			Vector3 kozep =
				(a + b) * 0.5f +
				Vector3.Up * SinMagassagTalajtol;

			Vector3 balSin = kozep - oldal * (Nyomtav * 0.5f);
			Vector3 jobbSin = kozep + oldal * (Nyomtav * 0.5f);

			float sinHossz = hossz * 1.04f;

			var sinAlap = new Basis(oldal, Vector3.Up, irany * sinHossz);

			sinek.AppendFrom(sinAlak, 0, new Transform3D(sinAlap, balSin));
			sinek.AppendFrom(sinAlak, 0, new Transform3D(sinAlap, jobbSin));

			talpfaSzamlalo += hossz;

			if (talpfaSzamlalo >= TalpfaTav)
			{
				talpfaSzamlalo = 0;

				Vector3 talpfaKozep =
					kozep - Vector3.Up *
					(SinMagassagTalajtol - TalpfaMagassag * 0.5f);

				var talpfaAlap = new Basis(oldal, Vector3.Up, irany);

				talpfak.AppendFrom(
					talpfaAlak,
					0,
					new Transform3D(talpfaAlap, talpfaKozep)
				);
			}
		}

		gorbe.Sin.Mesh = sinek.Commit();
		gorbe.Talpfa.Mesh = talpfak.Commit();

        
    }

	private void EloKinezetFrissitese()
	{
		if (kattAllapot == 0 || !vanEloPozicio)
		{
			eloSín.Mesh = null;
			eloTalpfa.Mesh = null;
			segedVonal.Mesh = null;
			return;
		}

		/*var ideiglenes = new KeszGorbe
		{
			P0 = aktualisGorbe[0],
			P1 = kattAllapot >= 2 ? aktualisGorbe[1] : eloPozicio,
			P2 = kattAllapot >= 4 ? aktualisGorbe[3] : eloPozicio,
			P3 = kattAllapot >= 3 ? aktualisGorbe[2] : eloPozicio
		};*/

		/*var ideiglenes = new KeszGorbe //újabbik
		{
			P0 = aktualisGorbe[0],
			P1 = kattAllapot >= 2
				? aktualisGorbe[1]
				: eloPozicio,
			P2 = kattAllapot >= 3
				? aktualisGorbe[2]
				: eloPozicio,
			P3 = kattAllapot >= 4
				? aktualisGorbe[3]
				: eloPozicio
		};*/
		var ideiglenes = new KeszGorbe //még újabb
		{
			P0 = aktualisGorbe[0],
			P1 = kattAllapot >= 2 ? aktualisGorbe[1] : eloPozicio,
			P2 = kattAllapot >= 3 ? aktualisGorbe[2] : eloPozicio,
			P3 = kattAllapot >= 4 ? aktualisGorbe[3] : eloPozicio
		};


		if (kattAllapot == 2)
		{
			ideiglenes.P2 = eloPozicio;
			ideiglenes.P3 = eloPozicio;
		}
		else if (kattAllapot == 3)
		{
			ideiglenes.P2 = eloPozicio;
			ideiglenes.P3 = aktualisGorbe[2];
		}
		else if (kattAllapot == 1)
		{
			ideiglenes.P1 = eloPozicio;
			ideiglenes.P2 = eloPozicio;
			ideiglenes.P3 = eloPozicio;
		}

		var pontok = new List<Vector3>();
		const int szakaszok = 50;

		for (int i = 0; i <= szakaszok; i++)
		{
			float t = (float)i / szakaszok;
			float u = 1.0f - t;

			pontok.Add(
				u * u * u * ideiglenes.P0 +
				3.0f * u * u * t * ideiglenes.P1 +
				3.0f * u * t * t * ideiglenes.P2 +
				t * t * t * ideiglenes.P3
			);
			/*pontok.Add( //újabbik
				u * u * u * ideiglenes.P0 +
				3.0f * u * u * t * ideiglenes.P1 +
				3.0f * u * t * t * ideiglenes.P3 +
				t * t * t * ideiglenes.P2
			);*/
		}

		var sinek = new SurfaceTool();
		sinek.Begin(Mesh.PrimitiveType.Triangles);

		var talpfak = new SurfaceTool();
		talpfak.Begin(Mesh.PrimitiveType.Triangles);

		float talpfaSzamlalo = 0;

		for (int i = 0; i < pontok.Count - 1; i++)
		{
			Vector3 a = pontok[i];
			Vector3 b = pontok[i + 1];
			Vector3 kulonbseg = b - a;
			float hossz = kulonbseg.Length();

			if (hossz < 0.0001f)
				continue;

			Vector3 irany = kulonbseg.Normalized();
			Vector3 oldal = Vector3.Up.Cross(irany).Normalized();

			if (oldal.LengthSquared() < 0.001f)
				continue;

			Vector3 kozep =
				(a + b) * 0.5f +
				Vector3.Up * SinMagassagTalajtol;

			var alap = new Basis(oldal, Vector3.Up, irany * (hossz * 1.04f));

			sinek.AppendFrom(
				sinAlak, 0,
				new Transform3D(alap, kozep - oldal * Nyomtav * 0.5f)
			);

			sinek.AppendFrom(
				sinAlak, 0,
				new Transform3D(alap, kozep + oldal * Nyomtav * 0.5f)
			);

			talpfaSzamlalo += hossz;

			if (talpfaSzamlalo >= TalpfaTav)
			{
				talpfaSzamlalo = 0;

				Vector3 talpfaKozep =
					kozep - Vector3.Up *
					(SinMagassagTalajtol - TalpfaMagassag * 0.5f);

				talpfak.AppendFrom(
					talpfaAlak, 0,
					new Transform3D(
						new Basis(oldal, Vector3.Up, irany),
						talpfaKozep
					)
				);
			}
		}

		eloSín.Mesh = sinek.Commit();
		eloTalpfa.Mesh = talpfak.Commit();

		var kuldIdeiglenes = new KeszGorbe
		{
			P0 = ideiglenes.P0,
			P1 = ideiglenes.P1,
			P2 = ideiglenes.P3,
			P3 = ideiglenes.P2,
		};
		eloGorbeTulEles =
			kattAllapot >= 1 && GorbeTulElese(kuldIdeiglenes);

		if (eloGorbeTulEles)
		{
			if (tulElesElonezetiAnyag == null)
			{
				tulElesElonezetiAnyag = new StandardMaterial3D();
				tulElesElonezetiAnyag.AlbedoColor = Colors.Red;
				tulElesElonezetiAnyag.ShadingMode =
					BaseMaterial3D.ShadingModeEnum.Unshaded;
			}

			eloSín.MaterialOverride = tulElesElonezetiAnyag;
			eloTalpfa.MaterialOverride = tulElesElonezetiAnyag;
		}
		else
		{
			eloSín.MaterialOverride = null;
			eloTalpfa.MaterialOverride = null;
		}


		var vonalak = new SurfaceTool();
		vonalak.Begin(Mesh.PrimitiveType.Lines);

		if (kattAllapot >= 2)
		{
			vonalak.AddVertex(aktualisGorbe[0]);
			vonalak.AddVertex(aktualisGorbe[1]);
		}
		else
		{
			vonalak.AddVertex(aktualisGorbe[0]);
			vonalak.AddVertex(eloPozicio);
		}

		if (kattAllapot == 3)
		{
			vonalak.AddVertex(aktualisGorbe[2]);
			vonalak.AddVertex(eloPozicio);
		}

		segedVonal.Mesh = vonalak.Commit();
	}

	private void ElojeloloFrissitese()
	{
		if (kattAllapot > 0 && kattAllapot < 4 && vanEloPozicio)
		{
			segedPontok[kattAllapot].Position =
				eloPozicio + Vector3.Up * 0.15f;

			segedPontok[kattAllapot].Visible = true;
		}
	}

	private void KeszGorbeKivalasztasa()
	{
		if (!EgerPozicioTalajon(out Vector3 kattPozicio))
			return;

		KeszGorbe legkozelebbi = null;
		float legkisebbTav = 0.8f;

		foreach (var gorbe in gorbek)
		{
			float tav = GorbeTavolsagBecslese(gorbe, kattPozicio);

			if (tav < legkisebbTav)
			{
				legkisebbTav = tav;
				legkozelebbi = gorbe;
			}
		}

		kijeloltGorbe = legkozelebbi;

		if (kijeloltGorbe != null)
		{
			kiiras.Text =
				$"Sín kijelölve. Delete: törlés. Sínek: {gorbek.Count}";
		}
		else
		{
			UtmutatoFrissitese();
		}
	}

	private float GorbeTavolsagBecslese(KeszGorbe gorbe, Vector3 hely)
	{
		float legkisebb = float.MaxValue;

		for (int i = 0; i <= 100; i++)
		{
			float t = i / 100.0f;
			float u = 1.0f - t;

			Vector3 pont =
				u * u * u * gorbe.P0 +
				3.0f * u * u * t * gorbe.P1 +
				3.0f * u * t * t * gorbe.P2 +
				t * t * t * gorbe.P3;

			float tav = new Vector2(pont.X - hely.X, pont.Z - hely.Z).Length();

			if (tav < legkisebb)
				legkisebb = tav;
		}

		return legkisebb;
	}

	private void GorbeTorlese()
	{
		if (kijeloltGorbe == null)
			return;

		gorbek.Remove(kijeloltGorbe);

		kijeloltGorbe.Sin.QueueFree();
		kijeloltGorbe.Talpfa.QueueFree();

		kijeloltGorbe = null;
		UtmutatoFrissitese();
	}

	public void EpitesMegszakitasa()
	{
		kattAllapot = 0;
		vanEloPozicio = false;

		foreach (var pont in segedPontok)
			pont.Visible = false;

		eloSín.Mesh = null;
		eloTalpfa.Mesh = null;
		segedVonal.Mesh = null;

		UtmutatoFrissitese();
		csatlakozasiJelolo.Visible = false;
		kozeliVegpont = null;
		aktP0KapcsolatID = 0;
		aktP2KapcsolatID = 0;
    }

	private void UtmutatoFrissitese()
	{
		if (Menu.szerkMod != 1)
			return;
		kiiras.Text = kattAllapot switch
		{
			0 => $"Új sín: bal kattintással kezdj. Sínek: {gorbek.Count}. Jobb katt: megszakítás.",
			1 => "2/4 - Add meg a kezdőirányt!",
			2 => "3/4 - Add meg a végpontot!",
			3 => "4/4 - Add meg a végirányt!",
			_ => $"Sínek: {gorbek.Count}. Kattints egy sínre, majd Delete a törléshez."
		};
	}


	private Vector3? KozeliVegpontKeresese(Vector3 hely)
	{
		Vector3? legkozelebbi = null;
		float legkisebbTav = VegpontKeresesiTav;

		csatlakozoGorbe = null;
		csatlakozasKezdoVeghez = false;

		void VizsgalGorbet(KeszGorbe gorbe)
		{
			Vector3[] vegpontok = { gorbe.P0, gorbe.P2 };

			for (int i = 0; i < vegpontok.Length; i++)
			{
				Vector3 vegpont = vegpontok[i];

				float tav = new Vector2(
					hely.X - vegpont.X,
					hely.Z - vegpont.Z
				).Length();

				if (tav < legkisebbTav)
				{
					legkisebbTav = tav;
					legkozelebbi = vegpont;
					csatlakozoGorbe = gorbe;
					csatlakozasKezdoVeghez = (i == 0);
				}
			}
		}

		foreach (var gorbe in gorbek)
		{
			if (gorbe.Tipus == 2)
			{
				// A váltó minden ágának végpontját megvizsgáljuk.
				foreach (var ag in gorbe.Allasok)
					VizsgalGorbet(ag);
			}
			else
			{
				VizsgalGorbet(gorbe);
			}
		}

		return legkozelebbi;
	}


	private Vector3 CsatlakozasiIrany()
	{
		if (csatlakozoGorbe == null)
			return Vector3.Zero;

		Vector3 irany;

		if (csatlakozasKezdoVeghez)
		{
			// A régi sín kezdőpontjától kifelé.
			irany = csatlakozoGorbe.P0 - csatlakozoGorbe.P1;
		}
		else
		{
			// A régi sín végpontjától kifelé.
			irany = csatlakozoGorbe.P2 - csatlakozoGorbe.P3;
		}

		irany.Y = 0.0f;

		if (irany.LengthSquared() < 0.0001f)
			return Vector3.Zero;

		return irany.Normalized();
	}

	private bool GorbeTulElese(KeszGorbe gorbe)
	{
		const int mintak = 100;

		Vector3 elozoPont = Vector3.Zero;
		Vector3 elozoElottiPont = Vector3.Zero;

		for (int i = 0; i <= mintak; i++)
		{
			float t = (float)i / mintak;
			float u = 1.0f - t;

			// A meglévő pontsorrend változatlan: P0, P1, P3, P2.
			Vector3 pont =
				u * u * u * gorbe.P0 +
				3.0f * u * u * t * gorbe.P1 +
				3.0f * u * t * t * gorbe.P3 +
				t * t * t * gorbe.P2;

			if (i >= 2)
			{
				Vector2 a = new Vector2(
					elozoElottiPont.X, elozoElottiPont.Z
				);

				Vector2 b = new Vector2(
					elozoPont.X, elozoPont.Z
				);

				Vector2 c = new Vector2(
					pont.X, pont.Z
				);

				float ab = a.DistanceTo(b);
				float bc = b.DistanceTo(c);
				float ac = a.DistanceTo(c);

				float kereszt =
					(b.X - a.X) * (c.Y - a.Y) -
					(b.Y - a.Y) * (c.X - a.X);

				float ketszeresTerulet = Mathf.Abs(kereszt);

				if (ab > 0.0001f &&
					bc > 0.0001f &&
					ac > 0.0001f &&
					ketszeresTerulet > 0.00001f)
				{
					// A három egymást követő pont köré írt kör sugara.
					float sugar =
						(ab * bc * ac) / (2.0f * ketszeresTerulet);

					if (sugar < MinimumKanyarSugar)
						return true;
				}
			}

			elozoElottiPont = elozoPont;
			elozoPont = pont;
		}

		return false;
	}

	public void kiir(string szoveg)
	{
		AcceptDialog dialog = new AcceptDialog();
		dialog.DialogText = szoveg;
		AddChild(dialog);
		dialog.PopupCentered();
	}

	public KeszGorbe GorbeKereseseIDAlapjan(int id)
	{
		// Először a normál sínek és a váltók keresése.
		KeszGorbe gorbe = gorbek.Find(g => g.ID == id);

		if (gorbe != null)
			return gorbe;

		// Ha nincs találat, keressük meg a váltók ágai között.
		foreach (var valto in gorbek)
		{
			if (valto.Tipus != 2)
				continue;

			gorbe = valto.Allasok.Find(ag => ag.ID == id);

			if (gorbe != null)
				return gorbe;
		}

		return null;
	}

	private bool VegpontFoglalt(KeszGorbe gorbe, bool p0Veg)
	{
		if (p0Veg)
			return gorbe.P0KapcsolatID != 0;

		return gorbe.P2KapcsolatID != 0;
	}

	private void GorbekMenteseJsonba()
	{
		var json = new JsonArray();

		foreach (var gorbe in gorbek)
			json.Add(GorbeJsonba(gorbe));

		string fajlUtvonal =
			ProjectSettings.GlobalizePath("user://gorbek.json");

		File.WriteAllText(
			fajlUtvonal,
			json.ToJsonString(new JsonSerializerOptions
			{
				WriteIndented = true
			})
		);

		kiir($"Mentés kész: {fajlUtvonal}");
	}

	private JsonObject GorbeJsonba(KeszGorbe gorbe)
	{
		var json = new JsonObject
		{
			["ID"] = gorbe.ID,
			["Tipus"] = gorbe.Tipus
		};
        if (gorbe.ValtoAllasErtek != 0)
            json["ValtoAllasErtek"] = gorbe.ValtoAllasErtek;

        if (gorbe.P0 != Vector3.Zero)
			json["P0"] = VektorJsonba(gorbe.P0);

		if (gorbe.P1 != Vector3.Zero)
			json["P1"] = VektorJsonba(gorbe.P1);

		if (gorbe.P2 != Vector3.Zero)
			json["P2"] = VektorJsonba(gorbe.P2);

		if (gorbe.P3 != Vector3.Zero)
			json["P3"] = VektorJsonba(gorbe.P3);

		if (gorbe.P0KapcsolatID != 0)
			json["P0KapcsolatID"] = gorbe.P0KapcsolatID;

		if (gorbe.P2KapcsolatID != 0)
			json["P2KapcsolatID"] = gorbe.P2KapcsolatID;

		if (gorbe.letezoSinKapcs[0] != 0 ||
			gorbe.letezoSinKapcs[1] != 0)
		{
			var kapcsolatok = new JsonArray();

			foreach (int id in gorbe.letezoSinKapcs)
				kapcsolatok.Add(id == 0 ? null : JsonValue.Create(id));

			json["letezoSinKapcs"] = kapcsolatok;
		}

		if (gorbe.Allasok.Count > 0)
		{
			var allasok = new JsonArray();

			foreach (var ag in gorbe.Allasok)
				allasok.Add(GorbeJsonba(ag));

			json["Allasok"] = allasok;
		}

		return json;
	}

	private JsonObject VektorJsonba(Vector3 v)
	{
		var json = new JsonObject();

		if (v.X != 0)
			json["X"] = v.X;

		if (v.Y != 0)
			json["Y"] = v.Y;

		if (v.Z != 0)
			json["Z"] = v.Z;

		return json;
	}
    private void GorbekBetolteseJsonbol()
    {
        string fajlUtvonal =
            ProjectSettings.GlobalizePath("user://gorbek.json");

        if (!System.IO.File.Exists(fajlUtvonal))
        {
            kiir("Nem található a gorbek.json fájl!");
            return;
        }

        try
        {
            string tartalom = System.IO.File.ReadAllText(fajlUtvonal);
            JsonNode gyoker = JsonNode.Parse(tartalom);

            if (gyoker is not JsonArray gorbeTomb)
            {
                kiir("Hibás JSON fájl: a gyökérelem nem tömb!");
                return;
            }

            // A jelenlegi pálya megjelenítőinek eltávolítása.
            foreach (var gorbe in gorbek)
                GorbeMeshTorlese(gorbe);

            gorbek.Clear();

            int legnagyobbID = 0;

            // A görbék és a váltóágak adatainak visszaállítása.
            foreach (JsonNode elem in gorbeTomb)
            {
                if (elem == null)
                    continue;

                KeszGorbe gorbe = GorbeJsonbol(elem);
                gorbek.Add(gorbe);

                legnagyobbID = Mathf.Max(
                    legnagyobbID,
                    LegnagyobbGorbeID(gorbe)
                );
            }

            // Minden görbe és váltóág megjelenítésének újraépítése.
            foreach (var gorbe in gorbek)
                GorbeMeshLetrehozasa(gorbe);

            // A következő új görbe is egyedi ID-t kapjon.
            kovetkezoGorbeID = legnagyobbID + 1;

            //kiir($"Pálya betöltve. Görbék száma: {gorbek.Count}");
        }
        catch (System.Exception e)
        {
            kiir($"Hiba a pálya betöltésekor: {e.Message}");
        }
    }
    private KeszGorbe GorbeJsonbol(JsonNode json)
    {
        var gorbe = new KeszGorbe
        {
            ID = json["ID"]?.GetValue<int>() ?? 0,
            Tipus = json["Tipus"]?.GetValue<int>() ?? 0,
			ValtoAllasErtek = json["ValtoAllasErtek"]?.GetValue<int>() ?? 0,

            P0 = VektorJsonbol(json["P0"]),
            P1 = VektorJsonbol(json["P1"]),
            P2 = VektorJsonbol(json["P2"]),
            P3 = VektorJsonbol(json["P3"]),

            P0KapcsolatID =
                json["P0KapcsolatID"]?.GetValue<int>() ?? 0,

            P2KapcsolatID =
                json["P2KapcsolatID"]?.GetValue<int>() ?? 0
        };

        JsonArray kapcsolatok = json["letezoSinKapcs"] as JsonArray;

        if (kapcsolatok != null)
        {
            for (int i = 0; i < Mathf.Min(2, kapcsolatok.Count); i++)
            {
                gorbe.letezoSinKapcs[i] =
                    kapcsolatok[i]?.GetValue<int>() ?? 0;
            }
        }

        if (json["Allasok"] is JsonArray allasok)
        {
            foreach (JsonNode ag in allasok)
            {
                if (ag != null)
                    gorbe.Allasok.Add(GorbeJsonbol(ag));
            }
        }

        return gorbe;
    }

    private Vector3 VektorJsonbol(JsonNode json)
    {
        if (json == null)
            return Vector3.Zero;

        return new Vector3(
            json["X"]?.GetValue<float>() ?? 0.0f,
            json["Y"]?.GetValue<float>() ?? 0.0f,
            json["Z"]?.GetValue<float>() ?? 0.0f
        );
    }
    private void GorbeMeshLetrehozasa(KeszGorbe gorbe)
    {
        /*kiir(
            $"Mesh létrehozás: ID={gorbe.ID}, " +
            $"Tipus={gorbe.Tipus}, " +
            $"P0={gorbe.P0}, P1={gorbe.P1}, " +
            $"P2={gorbe.P2}, P3={gorbe.P3}, " +
            $"Ágak={gorbe.Allasok.Count}"
        );*/

        if (gorbe.Tipus == 1)
        {
            gorbe.Sin = new MeshInstance3D();
            gorbe.Talpfa = new MeshInstance3D();

            gorbe.Sin.MaterialOverride = sinAnyag;
            gorbe.Talpfa.MaterialOverride = talpfaAnyag;

            AddChild(gorbe.Sin);
            AddChild(gorbe.Talpfa);

            GorbeGeometriaElkeszitese(gorbe);

            /*kiir(
                $"Mesh elkészült: ID={gorbe.ID}, " +
                $"Sín mesh null={gorbe.Sin.Mesh == null}, " +
                $"Talpfa mesh null={gorbe.Talpfa.Mesh == null}"
            );*/
        }

        if (gorbe.Tipus == 2)
        {
            ValtoGombLerak(gorbe);
        }

        foreach (var ag in gorbe.Allasok)
            GorbeMeshLetrehozasa(ag);
    }
    private void GorbeMeshTorlese(KeszGorbe gorbe)
    {
        foreach (var ag in gorbe.Allasok)
            GorbeMeshTorlese(ag);

        if (GodotObject.IsInstanceValid(gorbe.Sin))
            gorbe.Sin.QueueFree();

        if (GodotObject.IsInstanceValid(gorbe.Talpfa))
            gorbe.Talpfa.QueueFree();
    }
    private int LegnagyobbGorbeID(KeszGorbe gorbe)
    {
        int maxID = gorbe.ID;

        foreach (var ag in gorbe.Allasok)
            maxID = Mathf.Max(maxID, LegnagyobbGorbeID(ag));

        return maxID;
    }

    private void ValtoGombLerak(KeszGorbe gorbe)
    {
        var kocka = new MeshInstance3D();

        var kockaMesh = new BoxMesh();
        kockaMesh.Size = new Vector3(.5f, .5f, .5f);
        kocka.Mesh = kockaMesh;

        var feketeAnyag = new StandardMaterial3D();
        feketeAnyag.AlbedoColor = Colors.Black;
        kocka.MaterialOverride = feketeAnyag;

        AddChild(kocka);

        Vector3 p0 = gorbe.Allasok[0].P0;
        Vector3 p1 = gorbe.Allasok[0].P1;

        Vector3 irany = (p1 - p0).Normalized();
        Vector3 oldal = Vector3.Up.Cross(irany).Normalized();

        kocka.Position = p0 + oldal * 1.2f + Vector3.Up * 0.1f;
        kocka.Rotation = new Vector3(0, Mathf.Atan2(irany.X, irany.Z), 0);

        // Kattintás kezelése
        var terulet = new Area3D();
        var alakzat = new CollisionShape3D();
        alakzat.Shape = new BoxShape3D { Size = kockaMesh.Size };

        terulet.AddChild(alakzat);
        kocka.AddChild(terulet);

        terulet.InputRayPickable = true;
        terulet.InputEvent += (kamera, esemeny, pozicio, normal, alakzatIndex) =>
        {
			if (esemeny is InputEventMouseButton e &&
				e.Pressed &&
				e.ButtonIndex == MouseButton.Left)
			{
				AcceptDialog agDialog = new AcceptDialog();
				agDialog.Title = $"Hanyadik ágra váltasz?";

				LineEdit agMezo = new LineEdit();
				agMezo.PlaceholderText = $"1 - {gorbe.Allasok.Count + (gorbe.letezoSinKapcs[0]!=0?1:0)}";
				agMezo.CustomMinimumSize = new Vector2(300, 0);

				agDialog.AddChild(agMezo);
				AddChild(agDialog);
				agDialog.Confirmed += () =>
				{
                    int.TryParse(agMezo.Text, out int ujErtek);

                    if (ujErtek >= 1 && ujErtek <= gorbe.Allasok.Count + (gorbe.letezoSinKapcs[0] != 0 ? 1 : 0))
					{
						gorbe.ValtoAllasErtek = ujErtek;
					}
				};
                agDialog.Canceled += () => agDialog.QueueFree();

                agDialog.PopupCentered();
                agMezo.GrabFocus();
            }
        };
    }



}