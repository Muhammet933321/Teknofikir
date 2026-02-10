using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using QuizGame.Data;
using QuizGame.Managers;

namespace QuizGame.Editor
{
    /// <summary>
    /// Gelişmiş Soru Düzenleyici — Tablo görünümü, renk kodlaması, toplu işlemler.
    /// Soruları rahat görüp düzenleyebileceğiniz tam özellikli editör.
    /// Menu: QuizGame > Soru Düzenleyici Pro
    /// </summary>
    public class QuestionEditorPro : EditorWindow
    {
        // ── Veritabanı ──
        private QuestionDatabase db;
        private string dbYolu;

        // ── Liste ──
        private Vector2 tabloScroll;
        private Vector2 detayScroll;
        private int seciliIndex = -1;
        private List<QuestionData> filtreliListe;
        private bool listeGuncelle = true;

        // ── Filtre ──
        private string aramaMetni = "";
        private int filtreZorluk = -1;   // -1 = tümü
        private int filtreKategori = -1; // -1 = tümü
        private readonly string[] zorlukSec = { "Tümü", "Kolay", "Orta", "Zor" };
        private readonly string[] kategoriSec = { "Tümü", "Matematik", "Türkçe", "Fen", "Sosyal", "İngilizce", "Genel Kültür" };

        // ── Sıralama ──
        private int siralamaKolonu = -1;
        private bool siralamaArtan = true;

        // ── Düzenleme ──
        private bool duzenleModu;
        private bool yeniSoruModu;
        private string formSoru = "";
        private string[] formSiklar = new string[4] { "", "", "", "" };
        private int formDogruSik = 0;
        private ZorlukSeviyesi formZorluk = ZorlukSeviyesi.Kolay;
        private DersKategorisi formKategori = DersKategorisi.Matematik;
        private string formAciklama = "";
        private string formAnimasyon = "";
        private string formVideo = "";

        // ── Toplu İşlem ──
        private HashSet<int> seciliSorular = new HashSet<int>();
        private bool topluSecimModu;

        // ── İstatistikler ──
        private int toplamSoru, kolayCount, ortaCount, zorCount;
        private Dictionary<DersKategorisi, int> kategoriBazli = new Dictionary<DersKategorisi, int>();

        // ── Stiller ──
        private GUIStyle headerStyle, satirStyle, satirAltStyle, miniBaslikStyle;
        private GUIStyle kartBaslikStyle, kartDegerStyle;
        private bool stillerHazir;

        // ── Panel Boyutları ──
        private float solPanelGenislik = 0.6f; // %60 sol, %40 sağ

        [MenuItem("QuizGame/📝 Soru Düzenleyici Pro", false, 5)]
        public static void ShowWindow()
        {
            var w = GetWindow<QuestionEditorPro>("Soru Düzenleyici Pro");
            w.minSize = new Vector2(1000, 600);
        }

        private void OnEnable()
        {
            VeritabaniYukle();
        }

        private void OnFocus()
        {
            VeritabaniYukle();
        }

        // ═══════════════════════════════════════════════════
        //  VERİTABANI
        // ═══════════════════════════════════════════════════

        private void VeritabaniYukle()
        {
            dbYolu = Path.Combine(Application.persistentDataPath, "soru_veritabani.json");
            if (File.Exists(dbYolu))
            {
                string json = File.ReadAllText(dbYolu);
                db = JsonUtility.FromJson<QuestionDatabase>(json);
            }
            if (db == null) db = new QuestionDatabase();
            listeGuncelle = true;
            IstatistikleriGuncelle();
        }

        private void VeritabaniKaydet()
        {
            string json = JsonUtility.ToJson(db, true);
            File.WriteAllText(dbYolu, json);
            if (Application.isPlaying && DataManager.Instance != null)
                DataManager.Instance.soruVeritabani = db;
            listeGuncelle = true;
            IstatistikleriGuncelle();
        }

        private void IstatistikleriGuncelle()
        {
            if (db == null) return;
            toplamSoru = db.sorular.Count;
            kolayCount = db.sorular.Count(s => s.zorluk == ZorlukSeviyesi.Kolay);
            ortaCount = db.sorular.Count(s => s.zorluk == ZorlukSeviyesi.Orta);
            zorCount = db.sorular.Count(s => s.zorluk == ZorlukSeviyesi.Zor);

            kategoriBazli.Clear();
            foreach (DersKategorisi ders in System.Enum.GetValues(typeof(DersKategorisi)))
                kategoriBazli[ders] = db.sorular.Count(s => s.kategori == ders);
        }

        // ═══════════════════════════════════════════════════
        //  STİLLER
        // ═══════════════════════════════════════════════════

        private void StilleriHazirla()
        {
            if (stillerHazir) return;
            stillerHazir = true;

            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
            };

            satirStyle = new GUIStyle("box")
            {
                padding = new RectOffset(4, 4, 2, 2),
                margin = new RectOffset(0, 0, 1, 0)
            };

            satirAltStyle = new GUIStyle("box")
            {
                padding = new RectOffset(4, 4, 2, 2),
                margin = new RectOffset(0, 0, 1, 0)
            };

            miniBaslikStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter
            };

            kartBaslikStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
            };

            kartDegerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
        }

        // ═══════════════════════════════════════════════════
        //  ANA GUI
        // ═══════════════════════════════════════════════════

        private void OnGUI()
        {
            StilleriHazirla();
            if (db == null) VeritabaniYukle();

            // Üst bar: İstatistik kartları
            UstBarCiz();

            EditorGUILayout.Space(4);

            // Ana alan
            EditorGUILayout.BeginHorizontal();

            // Sol: Tablo
            float solGen = position.width * solPanelGenislik;
            EditorGUILayout.BeginVertical(GUILayout.Width(solGen));
            SolPanelCiz();
            EditorGUILayout.EndVertical();

            // Ayırıcı
            GUILayout.Box("", GUILayout.Width(2), GUILayout.ExpandHeight(true));

            // Sağ: Detay/Düzenleme
            EditorGUILayout.BeginVertical();
            SagPanelCiz();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        // ═══════════════════════════════════════════════════
        //  ÜST BAR (İSTATİSTİK KARTLARI)
        // ═══════════════════════════════════════════════════

        private void UstBarCiz()
        {
            float kartYukseklik = 60f;
            Rect ustRect = GUILayoutUtility.GetRect(position.width, kartYukseklik + 8);

            float padding = 6f;
            int kartSayisi = 10; // Toplam + 3 zorluk + 6 ders
            float kartGen = (ustRect.width - padding * (kartSayisi + 1)) / kartSayisi;
            kartGen = Mathf.Max(kartGen, 70f);

            float x = ustRect.x + padding;
            float y = ustRect.y + 4;

            // Toplam
            GraphRenderer.OzetKart(new Rect(x, y, kartGen, kartYukseklik),
                $"{toplamSoru}", "Toplam Soru", new Color(0.5f, 0.7f, 1f));
            x += kartGen + padding;

            // Zorluklar
            GraphRenderer.OzetKart(new Rect(x, y, kartGen, kartYukseklik),
                $"{kolayCount}", "Kolay", GraphRenderer.ZorlukKolay);
            x += kartGen + padding;

            GraphRenderer.OzetKart(new Rect(x, y, kartGen, kartYukseklik),
                $"{ortaCount}", "Orta", GraphRenderer.ZorlukOrta);
            x += kartGen + padding;

            GraphRenderer.OzetKart(new Rect(x, y, kartGen, kartYukseklik),
                $"{zorCount}", "Zor", GraphRenderer.ZorlukZor);
            x += kartGen + padding;

            // Ders bazlı
            foreach (DersKategorisi ders in System.Enum.GetValues(typeof(DersKategorisi)))
            {
                int adet = kategoriBazli.ContainsKey(ders) ? kategoriBazli[ders] : 0;
                int dersIdx = (int)ders;
                Color renk = GraphRenderer.DersRenkleri[dersIdx % GraphRenderer.DersRenkleri.Length];
                GraphRenderer.OzetKart(new Rect(x, y, kartGen, kartYukseklik),
                    $"{adet}", DersKisaAd(ders), renk);
                x += kartGen + padding;

                if (x + kartGen > ustRect.xMax) break; // Taşma kontrolü
            }
        }

        // ═══════════════════════════════════════════════════
        //  SOL PANEL (FİLTRE + TABLO)
        // ═══════════════════════════════════════════════════

        private void SolPanelCiz()
        {
            // Filtre bar
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Arama
            EditorGUILayout.LabelField("🔍", GUILayout.Width(20));
            string yeniArama = EditorGUILayout.TextField(aramaMetni, EditorStyles.toolbarSearchField, GUILayout.MinWidth(120));
            if (yeniArama != aramaMetni) { aramaMetni = yeniArama; listeGuncelle = true; }
            if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(22)))
            {
                aramaMetni = "";
                listeGuncelle = true;
            }

            GUILayout.Space(8);

            // Zorluk filtresi
            int yeniZorluk = EditorGUILayout.Popup(filtreZorluk + 1, zorlukSec, EditorStyles.toolbarPopup, GUILayout.Width(75)) - 1;
            if (yeniZorluk != filtreZorluk) { filtreZorluk = yeniZorluk; listeGuncelle = true; }

            // Kategori filtresi
            int yeniKat = EditorGUILayout.Popup(filtreKategori + 1, kategoriSec, EditorStyles.toolbarPopup, GUILayout.Width(100)) - 1;
            if (yeniKat != filtreKategori) { filtreKategori = yeniKat; listeGuncelle = true; }

            GUILayout.FlexibleSpace();

            // Toplu seçim toggle
            if (GUILayout.Button(topluSecimModu ? "☑ Toplu Seçim" : "☐ Toplu Seçim",
                EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                topluSecimModu = !topluSecimModu;
                if (!topluSecimModu) seciliSorular.Clear();
            }

            // Yeni soru
            if (GUILayout.Button("+ Yeni Soru", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                YeniSoruBaslat();
            }

            EditorGUILayout.EndHorizontal();

            // Toplu işlem barı
            if (topluSecimModu && seciliSorular.Count > 0)
            {
                TopluIslemBarCiz();
            }

            // Liste güncelle
            if (listeGuncelle)
            {
                filtreliListe = FiltreVeSirala();
                listeGuncelle = false;
            }

            // Tablo başlık
            TabloBasklikCiz();

            // Tablo satırları
            tabloScroll = EditorGUILayout.BeginScrollView(tabloScroll);

            for (int i = 0; i < filtreliListe.Count; i++)
            {
                TabloSatiriCiz(filtreliListe[i], i);
            }

            EditorGUILayout.EndScrollView();

            // Alt durum çubuğu
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField($"Gösterilen: {filtreliListe.Count} / {toplamSoru}", EditorStyles.miniLabel);
            if (topluSecimModu)
                EditorGUILayout.LabelField($"Seçili: {seciliSorular.Count}", EditorStyles.miniLabel, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
        }

        private void TabloBasklikCiz()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (topluSecimModu)
            {
                bool hepsiSecili = filtreliListe.Count > 0 && seciliSorular.Count == filtreliListe.Count;
                bool yeniHepsi = EditorGUILayout.Toggle(hepsiSecili, GUILayout.Width(20));
                if (yeniHepsi != hepsiSecili)
                {
                    seciliSorular.Clear();
                    if (yeniHepsi)
                    {
                        foreach (var s in filtreliListe)
                            seciliSorular.Add(db.sorular.IndexOf(s));
                    }
                }
            }

            if (SiralamaButonu("#", 30, 0)) listeGuncelle = true;
            if (SiralamaButonu("Zorluk", 65, 1)) listeGuncelle = true;
            if (SiralamaButonu("Ders", 90, 2)) listeGuncelle = true;
            if (SiralamaButonu("Soru Metni", 0, 3)) listeGuncelle = true; // Width 0 = FlexibleSpace
            if (SiralamaButonu("Şık", 35, 4)) listeGuncelle = true;

            GUILayout.Space(26); // Silme butonu için boşluk
            EditorGUILayout.EndHorizontal();
        }

        private bool SiralamaButonu(string metin, float genislik, int kolonIndex)
        {
            string ok = "";
            if (siralamaKolonu == kolonIndex)
                ok = siralamaArtan ? " ▲" : " ▼";

            bool tiklaResult;
            if (genislik > 0)
                tiklaResult = GUILayout.Button($"{metin}{ok}", EditorStyles.toolbarButton, GUILayout.Width(genislik));
            else
                tiklaResult = GUILayout.Button($"{metin}{ok}", EditorStyles.toolbarButton);

            if (tiklaResult)
            {
                if (siralamaKolonu == kolonIndex)
                    siralamaArtan = !siralamaArtan;
                else
                {
                    siralamaKolonu = kolonIndex;
                    siralamaArtan = true;
                }
            }
            return tiklaResult;
        }

        private void TabloSatiriCiz(QuestionData soru, int filtreIndex)
        {
            int gercekIndex = db.sorular.IndexOf(soru);
            bool secili = (gercekIndex == seciliIndex);

            // Alternatif satır rengi
            Color bgColor = filtreIndex % 2 == 0
                ? new Color(0.22f, 0.22f, 0.22f, 0.3f)
                : new Color(0.25f, 0.25f, 0.25f, 0.3f);

            if (secili) bgColor = new Color(0.2f, 0.4f, 0.6f, 0.5f);

            Rect satirRect = EditorGUILayout.BeginHorizontal();
            EditorGUI.DrawRect(satirRect, bgColor);

            // Toplu seçim checkbox
            if (topluSecimModu)
            {
                bool soruSecili = seciliSorular.Contains(gercekIndex);
                bool yeniSecili = EditorGUILayout.Toggle(soruSecili, GUILayout.Width(20));
                if (yeniSecili != soruSecili)
                {
                    if (yeniSecili) seciliSorular.Add(gercekIndex);
                    else seciliSorular.Remove(gercekIndex);
                }
            }

            // # Numara
            EditorGUILayout.LabelField($"{gercekIndex + 1}", GUILayout.Width(30));

            // Zorluk (renkli etiket)
            Color zorlukRenk = soru.zorluk == ZorlukSeviyesi.Kolay ? GraphRenderer.ZorlukKolay :
                               soru.zorluk == ZorlukSeviyesi.Orta ? GraphRenderer.ZorlukOrta :
                               GraphRenderer.ZorlukZor;
            var zorlukStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = zorlukRenk },
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField(ZorlukKisa(soru.zorluk), zorlukStyle, GUILayout.Width(65));

            // Ders (renkli)
            int dersIdx = (int)soru.kategori;
            Color dersRenk = GraphRenderer.DersRenkleri[dersIdx % GraphRenderer.DersRenkleri.Length];
            var dersStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = dersRenk },
                fontStyle = FontStyle.Bold
            };
            EditorGUILayout.LabelField(DersKisaAd(soru.kategori), dersStyle, GUILayout.Width(90));

            // Soru metni (tıklanabilir)
            string onIzleme = soru.soruMetni.Length > 60 ? soru.soruMetni.Substring(0, 60) + "..." : soru.soruMetni;
            if (GUILayout.Button(onIzleme, EditorStyles.linkLabel))
            {
                seciliIndex = gercekIndex;
                duzenleModu = false;
                yeniSoruModu = false;
                GUI.FocusControl(null);
            }

            // Doğru şık
            string dogruHarf = new string[] { "A", "B", "C", "D" }[soru.dogruSikIndex];
            var dogruStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.4f, 0.9f, 0.4f) },
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField(dogruHarf, dogruStyle, GUILayout.Width(35));

            // Silme butonu
            if (GUILayout.Button("✕", GUILayout.Width(22)))
            {
                if (EditorUtility.DisplayDialog("Soru Sil",
                    $"Bu soruyu silmek istediğinize emin misiniz?\n\n{soru.soruMetni}", "Sil", "İptal"))
                {
                    db.sorular.RemoveAt(gercekIndex);
                    VeritabaniKaydet();
                    if (seciliIndex == gercekIndex) seciliIndex = -1;
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        // ═══════════════════════════════════════════════════
        //  TOPLU İŞLEM BARI
        // ═══════════════════════════════════════════════════

        private void TopluIslemBarCiz()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"✅ {seciliSorular.Count} soru seçili", EditorStyles.boldLabel, GUILayout.Width(130));

            // Toplu zorluk değiştir
            if (GUILayout.Button("Zorluk Değiştir", GUILayout.Width(110)))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Kolay"), false, () => TopluZorlukDegistir(ZorlukSeviyesi.Kolay));
                menu.AddItem(new GUIContent("Orta"), false, () => TopluZorlukDegistir(ZorlukSeviyesi.Orta));
                menu.AddItem(new GUIContent("Zor"), false, () => TopluZorlukDegistir(ZorlukSeviyesi.Zor));
                menu.ShowAsContext();
            }

            // Toplu kategori değiştir
            if (GUILayout.Button("Ders Değiştir", GUILayout.Width(100)))
            {
                var menu = new GenericMenu();
                foreach (DersKategorisi ders in System.Enum.GetValues(typeof(DersKategorisi)))
                {
                    var d = ders;
                    menu.AddItem(new GUIContent(DersKisaAd(d)), false, () => TopluKategoriDegistir(d));
                }
                menu.ShowAsContext();
            }

            // Toplu sil
            if (GUILayout.Button("Seçilenleri Sil", GUILayout.Width(110)))
            {
                if (EditorUtility.DisplayDialog("Toplu Silme",
                    $"{seciliSorular.Count} soruyu silmek istediğinize emin misiniz?", "Sil", "İptal"))
                {
                    var silinecekler = seciliSorular.OrderByDescending(i => i).ToList();
                    foreach (int idx in silinecekler)
                    {
                        if (idx >= 0 && idx < db.sorular.Count)
                            db.sorular.RemoveAt(idx);
                    }
                    seciliSorular.Clear();
                    seciliIndex = -1;
                    VeritabaniKaydet();
                }
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Seçimi Temizle", GUILayout.Width(100)))
                seciliSorular.Clear();

            EditorGUILayout.EndHorizontal();
        }

        private void TopluZorlukDegistir(ZorlukSeviyesi yeniZorluk)
        {
            foreach (int idx in seciliSorular)
            {
                if (idx >= 0 && idx < db.sorular.Count)
                    db.sorular[idx].zorluk = yeniZorluk;
            }
            VeritabaniKaydet();
        }

        private void TopluKategoriDegistir(DersKategorisi yeniKategori)
        {
            foreach (int idx in seciliSorular)
            {
                if (idx >= 0 && idx < db.sorular.Count)
                    db.sorular[idx].kategori = yeniKategori;
            }
            VeritabaniKaydet();
        }

        // ═══════════════════════════════════════════════════
        //  SAĞ PANEL (DETAY / DÜZENLEME)
        // ═══════════════════════════════════════════════════

        private void SagPanelCiz()
        {
            detayScroll = EditorGUILayout.BeginScrollView(detayScroll);

            if (yeniSoruModu || duzenleModu)
            {
                DuzenleFormCiz();
            }
            else if (seciliIndex >= 0 && seciliIndex < db.sorular.Count)
            {
                DetayPanelCiz(db.sorular[seciliIndex]);
            }
            else
            {
                // Ders dağılımı mini grafik
                MiniDagilimCiz();
            }

            EditorGUILayout.EndScrollView();
        }

        private void MiniDagilimCiz()
        {
            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("Soru Dağılımı", miniBaslikStyle);
            EditorGUILayout.Space(10);

            // Ders bazlı çubuk grafik
            var dersler = System.Enum.GetValues(typeof(DersKategorisi)).Cast<DersKategorisi>().ToArray();
            string[] etiketler = dersler.Select(d => DersKisaAd(d)).ToArray();
            float[] degerler = dersler.Select(d => (float)(kategoriBazli.ContainsKey(d) ? kategoriBazli[d] : 0)).ToArray();
            Color[] renkler = dersler.Select((d, i) => GraphRenderer.DersRenkleri[i % GraphRenderer.DersRenkleri.Length]).ToArray();

            Rect grafikRect = GUILayoutUtility.GetRect(200, 180);
            GraphRenderer.CubukGrafik(grafikRect, etiketler, degerler, renkler, "Ders Bazlı Soru Sayısı");

            EditorGUILayout.Space(20);

            // Zorluk dağılımı
            float[] zorlukDegerler = { kolayCount, ortaCount, zorCount };
            Color[] zorlukRenkler = { GraphRenderer.ZorlukKolay, GraphRenderer.ZorlukOrta, GraphRenderer.ZorlukZor };
            string[] zorlukEtiketler = { "Kolay", "Orta", "Zor" };

            Rect segmentRect = GUILayoutUtility.GetRect(200, 24);
            GraphRenderer.SegmentliCubuk(segmentRect, zorlukDegerler, zorlukRenkler, zorlukEtiketler);

            EditorGUILayout.Space(30);
            EditorGUILayout.LabelField("Bir soru seçin veya yeni soru ekleyin.", EditorStyles.centeredGreyMiniLabel);
        }

        private void DetayPanelCiz(QuestionData soru)
        {
            // Üst bar: zorluk ve ders göstergesi
            EditorGUILayout.BeginHorizontal();

            // Zorluk badge
            Color zorlukRenk = soru.zorluk == ZorlukSeviyesi.Kolay ? GraphRenderer.ZorlukKolay :
                               soru.zorluk == ZorlukSeviyesi.Orta ? GraphRenderer.ZorlukOrta :
                               GraphRenderer.ZorlukZor;
            Rect zorlukBadge = GUILayoutUtility.GetRect(80, 24);
            EditorGUI.DrawRect(zorlukBadge, zorlukRenk);
            GUI.Label(zorlukBadge, soru.zorluk.ToString(),
                new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.black } });

            GUILayout.Space(4);

            // Ders badge
            int dersIdx = (int)soru.kategori;
            Color dersRenk = GraphRenderer.DersRenkleri[dersIdx % GraphRenderer.DersRenkleri.Length];
            Rect dersBadge = GUILayoutUtility.GetRect(100, 24);
            EditorGUI.DrawRect(dersBadge, dersRenk);
            GUI.Label(dersBadge, DersKisaAd(soru.kategori),
                new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.black } });

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Soru metni
            EditorGUILayout.LabelField("Soru:", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(soru.soruMetni, EditorStyles.wordWrappedLabel, GUILayout.MinHeight(40));

            EditorGUILayout.Space(8);

            // Şıklar
            string[] harfler = { "A", "B", "C", "D" };
            for (int i = 0; i < 4; i++)
            {
                bool dogru = (i == soru.dogruSikIndex);
                Rect sikRect = EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                if (dogru)
                    EditorGUI.DrawRect(sikRect, new Color(0.2f, 0.5f, 0.2f, 0.15f));

                var harfStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    normal = { textColor = dogru ? new Color(0.4f, 0.9f, 0.4f) : new Color(0.7f, 0.7f, 0.7f) },
                    fontSize = 14
                };

                EditorGUILayout.LabelField($"{harfler[i]})", harfStyle, GUILayout.Width(25));

                var sikStyle = new GUIStyle(EditorStyles.label)
                {
                    wordWrap = true,
                    fontStyle = dogru ? FontStyle.Bold : FontStyle.Normal,
                    normal = { textColor = dogru ? new Color(0.4f, 0.9f, 0.4f) : Color.white }
                };
                EditorGUILayout.LabelField(soru.siklar[i], sikStyle);

                if (dogru)
                    EditorGUILayout.LabelField("✓", new GUIStyle(EditorStyles.boldLabel)
                    { normal = { textColor = new Color(0.4f, 0.9f, 0.4f) }, fontSize = 16, alignment = TextAnchor.MiddleRight },
                    GUILayout.Width(25));

                EditorGUILayout.EndHorizontal();
            }

            // Açıklama
            if (soru.AciklamaVar)
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("💡 Açıklama:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(soru.aciklama, EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndVertical();
            }

            if (!string.IsNullOrEmpty(soru.animasyonAdi))
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField($"🎬 Animasyon: {soru.animasyonAdi}");
            }
            if (!string.IsNullOrEmpty(soru.videoYolu))
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField($"📹 Video: {soru.videoYolu}");
            }

            EditorGUILayout.Space(15);

            // Butonlar
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("✏ Düzenle", GUILayout.Height(32)))
            {
                DuzenleModuBaslat(soru);
            }

            if (GUILayout.Button("📋 Kopyala", GUILayout.Height(32)))
            {
                KopyalaSoru(soru);
            }

            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("🗑 Sil", GUILayout.Height(32)))
            {
                if (EditorUtility.DisplayDialog("Soru Sil", "Bu soru silinecek. Emin misiniz?", "Sil", "İptal"))
                {
                    db.sorular.Remove(soru);
                    VeritabaniKaydet();
                    seciliIndex = -1;
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        // ═══════════════════════════════════════════════════
        //  DÜZENLEME FORMU
        // ═══════════════════════════════════════════════════

        private void YeniSoruBaslat()
        {
            yeniSoruModu = true;
            duzenleModu = false;
            seciliIndex = -1;
            formSoru = "";
            formSiklar = new string[4] { "", "", "", "" };
            formDogruSik = 0;
            formZorluk = ZorlukSeviyesi.Kolay;
            formKategori = DersKategorisi.Matematik;
            formAciklama = "";
            formAnimasyon = "";
            formVideo = "";
        }

        private void DuzenleModuBaslat(QuestionData soru)
        {
            duzenleModu = true;
            yeniSoruModu = false;
            formSoru = soru.soruMetni;
            formSiklar = (string[])soru.siklar.Clone();
            formDogruSik = soru.dogruSikIndex;
            formZorluk = soru.zorluk;
            formKategori = soru.kategori;
            formAciklama = soru.aciklama ?? "";
            formAnimasyon = soru.animasyonAdi ?? "";
            formVideo = soru.videoYolu ?? "";
        }

        private void DuzenleFormCiz()
        {
            string baslik = yeniSoruModu ? "➕ Yeni Soru Ekle" : "✏ Soru Düzenle";
            EditorGUILayout.LabelField(baslik, miniBaslikStyle);
            EditorGUILayout.Space(8);

            // Zorluk & Kategori yan yana
            EditorGUILayout.BeginHorizontal();
            formZorluk = (ZorlukSeviyesi)EditorGUILayout.EnumPopup("Zorluk", formZorluk);
            formKategori = (DersKategorisi)EditorGUILayout.EnumPopup("Ders", formKategori);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);

            // Soru metni
            EditorGUILayout.LabelField("Soru Metni:", EditorStyles.boldLabel);
            formSoru = EditorGUILayout.TextArea(formSoru, GUILayout.MinHeight(60));

            EditorGUILayout.Space(6);

            // Şıklar
            string[] harfler = { "A", "B", "C", "D" };
            EditorGUILayout.LabelField("Şıklar (doğru olanı işaretleyin):", EditorStyles.boldLabel);

            for (int i = 0; i < 4; i++)
            {
                EditorGUILayout.BeginHorizontal();

                bool dogru = (formDogruSik == i);
                Color eskiRenk = GUI.backgroundColor;
                if (dogru) GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);

                bool yeniDogru = EditorGUILayout.Toggle(dogru, GUILayout.Width(20));
                if (yeniDogru && !dogru) formDogruSik = i;

                EditorGUILayout.LabelField($"{harfler[i]})", GUILayout.Width(22));
                formSiklar[i] = EditorGUILayout.TextField(formSiklar[i]);

                GUI.backgroundColor = eskiRenk;
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(6);

            // Açıklama
            EditorGUILayout.LabelField("💡 Açıklama (Yanlış cevap sonrası gösterilir):", EditorStyles.boldLabel);
            formAciklama = EditorGUILayout.TextArea(formAciklama, GUILayout.MinHeight(45));

            EditorGUILayout.Space(4);

            // Opsiyonel
            EditorGUILayout.LabelField("Opsiyonel Alanlar", EditorStyles.boldLabel);
            formAnimasyon = EditorGUILayout.TextField("Animasyon Adı", formAnimasyon);
            formVideo = EditorGUILayout.TextField("Video Yolu", formVideo);

            EditorGUILayout.Space(12);

            // Doğrulama
            bool formGecerli = !string.IsNullOrWhiteSpace(formSoru) && formSiklar.All(s => !string.IsNullOrWhiteSpace(s));

            if (!formGecerli)
            {
                EditorGUILayout.HelpBox("⚠ Soru metni ve tüm şıklar doldurulmalıdır.", MessageType.Warning);
            }

            // Kaydet / İptal
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = formGecerli;
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
            if (GUILayout.Button("✓ Kaydet", GUILayout.Height(35)))
            {
                FormuKaydet();
            }
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;

            if (GUILayout.Button("✕ İptal", GUILayout.Height(35)))
            {
                duzenleModu = false;
                yeniSoruModu = false;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void FormuKaydet()
        {
            if (yeniSoruModu)
            {
                var yeni = new QuestionData(formSoru, (string[])formSiklar.Clone(),
                    formDogruSik, formZorluk, formKategori, formAciklama);
                yeni.animasyonAdi = formAnimasyon;
                yeni.videoYolu = formVideo;
                db.sorular.Add(yeni);
                seciliIndex = db.sorular.Count - 1;
            }
            else if (seciliIndex >= 0 && seciliIndex < db.sorular.Count)
            {
                var soru = db.sorular[seciliIndex];
                soru.soruMetni = formSoru;
                soru.siklar = (string[])formSiklar.Clone();
                soru.dogruSikIndex = formDogruSik;
                soru.zorluk = formZorluk;
                soru.kategori = formKategori;
                soru.aciklama = formAciklama;
                soru.animasyonAdi = formAnimasyon;
                soru.videoYolu = formVideo;
            }

            VeritabaniKaydet();
            duzenleModu = false;
            yeniSoruModu = false;
        }

        private void KopyalaSoru(QuestionData kaynak)
        {
            var kopya = new QuestionData(
                kaynak.soruMetni + " (kopya)",
                (string[])kaynak.siklar.Clone(),
                kaynak.dogruSikIndex,
                kaynak.zorluk,
                kaynak.kategori,
                kaynak.aciklama);
            kopya.animasyonAdi = kaynak.animasyonAdi;
            kopya.videoYolu = kaynak.videoYolu;
            db.sorular.Add(kopya);
            VeritabaniKaydet();
            seciliIndex = db.sorular.Count - 1;
        }

        // ═══════════════════════════════════════════════════
        //  FİLTRE & SIRALAMA
        // ═══════════════════════════════════════════════════

        private List<QuestionData> FiltreVeSirala()
        {
            var sonuc = db.sorular.AsEnumerable();

            if (filtreZorluk >= 0)
                sonuc = sonuc.Where(s => (int)s.zorluk == filtreZorluk);
            if (filtreKategori >= 0)
                sonuc = sonuc.Where(s => (int)s.kategori == filtreKategori);
            if (!string.IsNullOrWhiteSpace(aramaMetni))
            {
                string kucuk = aramaMetni.ToLowerInvariant();
                sonuc = sonuc.Where(s =>
                    s.soruMetni.ToLowerInvariant().Contains(kucuk) ||
                    s.siklar.Any(sik => sik.ToLowerInvariant().Contains(kucuk)) ||
                    (s.aciklama != null && s.aciklama.ToLowerInvariant().Contains(kucuk)));
            }

            // Sıralama
            if (siralamaKolonu >= 0)
            {
                switch (siralamaKolonu)
                {
                    case 0: sonuc = siralamaArtan ? sonuc.OrderBy(s => db.sorular.IndexOf(s)) : sonuc.OrderByDescending(s => db.sorular.IndexOf(s)); break;
                    case 1: sonuc = siralamaArtan ? sonuc.OrderBy(s => s.zorluk) : sonuc.OrderByDescending(s => s.zorluk); break;
                    case 2: sonuc = siralamaArtan ? sonuc.OrderBy(s => s.kategori) : sonuc.OrderByDescending(s => s.kategori); break;
                    case 3: sonuc = siralamaArtan ? sonuc.OrderBy(s => s.soruMetni) : sonuc.OrderByDescending(s => s.soruMetni); break;
                    case 4: sonuc = siralamaArtan ? sonuc.OrderBy(s => s.dogruSikIndex) : sonuc.OrderByDescending(s => s.dogruSikIndex); break;
                }
            }

            return sonuc.ToList();
        }

        // ═══════════════════════════════════════════════════
        //  YARDIMCILAR
        // ═══════════════════════════════════════════════════

        private string DersKisaAd(DersKategorisi ders)
        {
            switch (ders)
            {
                case DersKategorisi.Matematik: return "Matematik";
                case DersKategorisi.Turkce: return "Türkçe";
                case DersKategorisi.Fen: return "Fen";
                case DersKategorisi.Sosyal: return "Sosyal";
                case DersKategorisi.Ingilizce: return "İngilizce";
                case DersKategorisi.GenelKultur: return "Genel Kültür";
                default: return ders.ToString();
            }
        }

        private string ZorlukKisa(ZorlukSeviyesi z)
        {
            switch (z)
            {
                case ZorlukSeviyesi.Kolay: return "● Kolay";
                case ZorlukSeviyesi.Orta: return "● Orta";
                case ZorlukSeviyesi.Zor: return "● Zor";
                default: return z.ToString();
            }
        }
    }
}
