using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using QuizGame.Data;
using QuizGame.Managers;

namespace QuizGame.Editor
{
    /// <summary>
    /// Öğrenci İlerleme Paneli — Haftalık/Günlük grafikler, ders karşılaştırma, trend analizi.
    /// Menu: QuizGame > İlerleme Paneli
    /// 
    /// Play Mode olmadan da verileri JSON'dan okuyarak çalışır.
    /// </summary>
    public class ProgressDashboard : EditorWindow
    {
        [MenuItem("QuizGame/📊 İlerleme Paneli", false, 15)]
        public static void ShowWindow()
        {
            var w = GetWindow<ProgressDashboard>("İlerleme Paneli");
            w.minSize = new Vector2(1100, 700);
        }

        // ═══════════════════════ STATE ═══════════════════════

        // Veritabanları (offline okuma)
        private SchoolDatabase okulDB;
        private PerformanceDatabase performansDB;
        private string okulDosyaYolu, performansDosyaYolu;

        // Seçim
        private int secilenSinifIndex = -1;
        private int secilenOgrenciIndex = -1;
        private StudentPerformance secilenPerformans;

        // Grafik ayarları
        private int secilenTab; // 0=Özet, 1=Haftalık, 2=Günlük, 3=Ders Karşılaştırma, 4=Trend
        private int haftaSayisi = 4;
        private int gunSayisi = 14;
        private DersKategorisi secilenDers = DersKategorisi.Matematik;
        private bool tumDersleriGoster = true;

        // Graifk modu
        private int grafikModu; // 0 = Başarı, 1 = Soru Sayısı, 2 = Süre

        // Hesaplanmış veriler
        private List<HaftalikRapor> haftalikRaporlar;
        private List<GunlukRapor> gunlukRaporlar;
        private HaftaKarsilastirma karsilastirma;
        private bool veriGuncel;

        // UI
        private Vector2 solScroll, sagScroll;
        private GUIStyle baslikStyle, altBaslikStyle, kutuStyle, bilgiStyle;
        private bool stillerHazir;

        // ═══════════════════════ YAŞAM DÖNGÜSÜ ═══════════════════════

        private void OnEnable()
        {
            VeritabanlariniYukle();
        }

        private void OnFocus()
        {
            VeritabanlariniYukle();
        }

        /// <summary>
        /// Play Mode olmadan da JSON dosyalarından veritabanını okur.
        /// Play Mode'daysa DataManager'dan alır.
        /// </summary>
        private void VeritabanlariniYukle()
        {
            if (Application.isPlaying && DataManager.Instance != null)
            {
                okulDB = DataManager.Instance.okulVeritabani;
                performansDB = DataManager.Instance.performansVeritabani;
            }
            else
            {
                // Offline okuma
                okulDosyaYolu = Path.Combine(Application.persistentDataPath, "okul_veritabani.json");
                performansDosyaYolu = Path.Combine(Application.persistentDataPath, "performans_veritabani.json");

                if (File.Exists(okulDosyaYolu))
                {
                    try
                    {
                        okulDB = JsonUtility.FromJson<SchoolDatabase>(File.ReadAllText(okulDosyaYolu));
                    }
                    catch { okulDB = new SchoolDatabase(); }
                }
                else okulDB = new SchoolDatabase();

                if (File.Exists(performansDosyaYolu))
                {
                    try
                    {
                        performansDB = JsonUtility.FromJson<PerformanceDatabase>(File.ReadAllText(performansDosyaYolu));
                    }
                    catch { performansDB = new PerformanceDatabase(); }
                }
                else performansDB = new PerformanceDatabase();
            }

            veriGuncel = false;
        }

        private void VerileriHesapla()
        {
            if (secilenPerformans == null) return;

            haftalikRaporlar = RaporHesaplayici.HaftalikRaporlarOlustur(secilenPerformans, haftaSayisi);
            gunlukRaporlar = RaporHesaplayici.GunlukRaporlarOlustur(secilenPerformans,
                DateTime.Now.AddDays(-gunSayisi), DateTime.Now);
            karsilastirma = RaporHesaplayici.HaftaKarsilastirmasiOlustur(secilenPerformans);

            veriGuncel = true;
        }

        // ═══════════════════════ STİLLER ═══════════════════════

        private void StilleriHazirla()
        {
            if (stillerHazir) return;
            stillerHazir = true;

            baslikStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            altBaslikStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
            };

            kutuStyle = new GUIStyle("box")
            {
                padding = new RectOffset(10, 10, 8, 8)
            };

            bilgiStyle = new GUIStyle(EditorStyles.label)
            {
                richText = true,
                wordWrap = true,
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
            };
        }

        // ═══════════════════════ ANA GUI ═══════════════════════

        private void OnGUI()
        {
            StilleriHazirla();

            EditorGUILayout.BeginHorizontal();
            SolPanelCiz();

            // Ayırıcı
            GUILayout.Box("", GUILayout.Width(2), GUILayout.ExpandHeight(true));

            SagPanelCiz();
            EditorGUILayout.EndHorizontal();
        }

        // ═══════════════════════ SOL PANEL ═══════════════════════

        private void SolPanelCiz()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(260));

            EditorGUILayout.LabelField("📊 İlerleme Paneli", baslikStyle);
            EditorGUILayout.Space(6);

            // Yenile butonu
            if (GUILayout.Button("🔄 Verileri Yenile", GUILayout.Height(25)))
            {
                VeritabanlariniYukle();
                if (secilenPerformans != null) VerileriHesapla();
            }

            EditorGUILayout.Space(6);

            solScroll = EditorGUILayout.BeginScrollView(solScroll);

            if (okulDB == null || okulDB.siniflar.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "Henüz sınıf/öğrenci verisi bulunamadı.\n\n" +
                    "1) Oyunu en az bir kez çalıştırın\n" +
                    "2) Sınıf ve öğrenci ekleyin\n" +
                    "3) En az bir oyun oynayın",
                    MessageType.Info);
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
                return;
            }

            // Sınıf seçimi
            EditorGUILayout.LabelField("Sınıf Seçimi", altBaslikStyle);
            string[] sinifAdlari = okulDB.siniflar.Select(s => $"{s.sinifAdi} ({s.ogrenciler.Count})").ToArray();
            int yeniSinif = EditorGUILayout.Popup(secilenSinifIndex, sinifAdlari);
            if (yeniSinif != secilenSinifIndex)
            {
                secilenSinifIndex = yeniSinif;
                secilenOgrenciIndex = -1;
                secilenPerformans = null;
                veriGuncel = false;
            }

            EditorGUILayout.Space(8);

            // Öğrenci listesi
            if (secilenSinifIndex >= 0 && secilenSinifIndex < okulDB.siniflar.Count)
            {
                var sinif = okulDB.siniflar[secilenSinifIndex];
                EditorGUILayout.LabelField($"Öğrenciler ({sinif.ogrenciler.Count})", altBaslikStyle);

                for (int i = 0; i < sinif.ogrenciler.Count; i++)
                {
                    var ogr = sinif.ogrenciler[i];
                    bool secili = (i == secilenOgrenciIndex);

                    // Performans varsa mini gösterge
                    var perf = performansDB?.PerformansGetir(ogr.id);
                    string perfGosterge = "";
                    if (perf != null && perf.tumCevaplar.Count > 0)
                    {
                        float yuzde = perf.GenelBasariYuzdesi();
                        perfGosterge = $" [%{yuzde:F0}]";
                    }

                    Rect btnRect = GUILayoutUtility.GetRect(new GUIContent($"{ogr.ogrenciNo} - {ogr.TamAd}{perfGosterge}"),
                        EditorStyles.miniButton, GUILayout.Height(24));

                    if (secili)
                        EditorGUI.DrawRect(btnRect, new Color(0.2f, 0.4f, 0.7f, 0.4f));

                    // Mini ilerleme çubuğu
                    if (perf != null && perf.tumCevaplar.Count > 0)
                    {
                        float yuzde = perf.GenelBasariYuzdesi();
                        var barRect = new Rect(btnRect.x, btnRect.yMax - 3, btnRect.width * Mathf.Clamp01(yuzde / 100f), 3);
                        EditorGUI.DrawRect(barRect, GraphRenderer.BasariRengi(yuzde));
                    }

                    if (GUI.Button(btnRect, $"{ogr.ogrenciNo} - {ogr.TamAd}{perfGosterge}",
                        secili ? new GUIStyle(EditorStyles.miniButton) { fontStyle = FontStyle.Bold } : EditorStyles.miniButton))
                    {
                        secilenOgrenciIndex = i;
                        secilenPerformans = performansDB?.PerformansGetir(ogr.id);
                        veriGuncel = false;
                    }
                }
            }

            EditorGUILayout.Space(12);

            // Grafik Ayarları
            if (secilenPerformans != null)
            {
                EditorGUILayout.LabelField("⚙ Grafik Ayarları", altBaslikStyle);
                EditorGUILayout.Space(4);

                haftaSayisi = EditorGUILayout.IntSlider("Hafta Sayısı", haftaSayisi, 2, 12);
                gunSayisi = EditorGUILayout.IntSlider("Gün Sayısı", gunSayisi, 7, 60);

                EditorGUILayout.Space(4);
                string[] modlar = { "Başarı %", "Soru Sayısı", "Ort. Süre" };
                grafikModu = GUILayout.SelectionGrid(grafikModu, modlar, 3, EditorStyles.miniButton);

                EditorGUILayout.Space(4);
                tumDersleriGoster = EditorGUILayout.Toggle("Tüm Dersleri Göster", tumDersleriGoster);
                if (!tumDersleriGoster)
                    secilenDers = (DersKategorisi)EditorGUILayout.EnumPopup("Ders", secilenDers);

                if (GUILayout.Button("Grafikleri Güncelle", GUILayout.Height(25)))
                    veriGuncel = false;
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // ═══════════════════════ SAĞ PANEL ═══════════════════════

        private void SagPanelCiz()
        {
            EditorGUILayout.BeginVertical();

            if (secilenPerformans == null)
            {
                EditorGUILayout.Space(40);
                EditorGUILayout.HelpBox(
                    "🎓 Soldaki listeden bir öğrenci seçin.\n\n" +
                    "İlerleme paneli, öğrencinin haftalık ve günlük\n" +
                    "performansını grafiklerle gösterir.\n\n" +
                    "NOT: Play Mode olmadan da veriler görüntülenebilir.",
                    MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            if (secilenPerformans.tumCevaplar.Count == 0)
            {
                EditorGUILayout.Space(40);
                EditorGUILayout.HelpBox("Bu öğrenci henüz hiç oyun oynamamış.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

            // Veri hesapla
            if (!veriGuncel) VerileriHesapla();

            // Sekmeler
            string[] tablar = { "📋 Genel Özet", "📅 Haftalık", "📆 Günlük", "📊 Ders Karşılaştırma", "📈 Trend Analizi" };
            secilenTab = GUILayout.Toolbar(secilenTab, tablar, GUILayout.Height(30));

            EditorGUILayout.Space(4);

            sagScroll = EditorGUILayout.BeginScrollView(sagScroll);

            switch (secilenTab)
            {
                case 0: GenelOzetCiz(); break;
                case 1: HaftalikGrafikCiz(); break;
                case 2: GunlukGrafikCiz(); break;
                case 3: DersKarsilastirmaCiz(); break;
                case 4: TrendAnaliziCiz(); break;
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // ═══════════════════════════════════════════════════
        //  TAB 0: GENEL ÖZET
        // ═══════════════════════════════════════════════════

        private void GenelOzetCiz()
        {
            var p = secilenPerformans;
            EditorGUILayout.LabelField($"🎓 {p.ogrenciAd}", baslikStyle);
            EditorGUILayout.Space(8);

            // Özet kartları
            float kartH = 65f;
            Rect kartAlani = GUILayoutUtility.GetRect(position.width * 0.6f, kartH + 8);
            float kartGen = (kartAlani.width - 32) / 5f;
            float x = kartAlani.x + 4;

            int toplam = p.tumCevaplar.Count;
            int dogru = p.tumCevaplar.Count(c => c.dogruMu);
            int yanlis = toplam - dogru;

            GraphRenderer.OzetKart(new Rect(x, kartAlani.y, kartGen, kartH),
                $"%{p.GenelBasariYuzdesi():F0}", "Başarı Oranı", GraphRenderer.BasariRengi(p.GenelBasariYuzdesi()));
            x += kartGen + 5;

            GraphRenderer.OzetKart(new Rect(x, kartAlani.y, kartGen, kartH),
                $"{toplam}", "Toplam Soru", new Color(0.5f, 0.7f, 1f));
            x += kartGen + 5;

            GraphRenderer.OzetKart(new Rect(x, kartAlani.y, kartGen, kartH),
                $"{dogru}", "Doğru", new Color(0.3f, 0.85f, 0.4f));
            x += kartGen + 5;

            GraphRenderer.OzetKart(new Rect(x, kartAlani.y, kartGen, kartH),
                $"{yanlis}", "Yanlış", new Color(1f, 0.3f, 0.3f));
            x += kartGen + 5;

            GraphRenderer.OzetKart(new Rect(x, kartAlani.y, kartGen, kartH),
                $"{p.ToplamOyunSayisi()}", "Oyun Sayısı", new Color(0.9f, 0.7f, 0.2f));

            EditorGUILayout.Space(12);

            // Hafta karşılaştırma kartı
            if (karsilastirma != null)
            {
                EditorGUILayout.BeginVertical(kutuStyle);
                EditorGUILayout.LabelField("📅 Bu Hafta vs Geçen Hafta", altBaslikStyle);
                EditorGUILayout.Space(4);

                float fark = karsilastirma.basariFarki;
                string trend = fark > 0 ? $"▲ +{fark:F1}" : fark < 0 ? $"▼ {fark:F1}" : "▶ 0";
                Color trendRenk = fark > 0 ? new Color(0.3f, 0.9f, 0.4f) : fark < 0 ? new Color(1f, 0.3f, 0.3f) : Color.gray;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Genel Başarı:", GUILayout.Width(100));

                var haftaComp = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = trendRenk }, fontSize = 14 };
                EditorGUILayout.LabelField(
                    $"%{karsilastirma.oncekiHafta.basariYuzdesi:F0} → %{karsilastirma.buHafta.basariYuzdesi:F0}  ({trend})",
                    haftaComp);
                EditorGUILayout.EndHorizontal();

                // Ders bazlı farklar
                if (karsilastirma.dersBazliFark.Count > 0)
                {
                    EditorGUILayout.Space(4);
                    foreach (var kvp in karsilastirma.dersBazliFark)
                    {
                        string dersOk = kvp.Value > 0 ? $"▲ +{kvp.Value:F0}" :
                                        kvp.Value < 0 ? $"▼ {kvp.Value:F0}" : "▶ 0";
                        Color dRenk = kvp.Value > 0 ? new Color(0.3f, 0.9f, 0.4f) :
                                      kvp.Value < 0 ? new Color(1f, 0.3f, 0.3f) : Color.gray;

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(RaporHesaplayici.DersAdi(kvp.Key), GUILayout.Width(120));
                        EditorGUILayout.LabelField(dersOk,
                            new GUIStyle(EditorStyles.label) { normal = { textColor = dRenk } });
                        EditorGUILayout.EndHorizontal();
                    }
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(12);

            // Ders bazlı ilerleme çubukları
            EditorGUILayout.BeginVertical(kutuStyle);
            EditorGUILayout.LabelField("📚 Ders Bazlı Başarı", altBaslikStyle);
            EditorGUILayout.Space(6);

            var istatistikler = p.TumDersIstatistikleri();
            foreach (var ist in istatistikler)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(RaporHesaplayici.DersAdi(ist.ders), GUILayout.Width(110));

                Rect barRect = GUILayoutUtility.GetRect(200, 22);
                int dersIdx = (int)ist.ders;
                Color renk = GraphRenderer.DersRenkleri[dersIdx % GraphRenderer.DersRenkleri.Length];
                GraphRenderer.IlerlemeÇubugu(barRect, ist.basariYuzdesi,
                    $"{ist.dogruSayisi}D/{ist.yanlisSayisi}Y", renk);

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(2);
            }

            EditorGUILayout.EndVertical();
        }

        // ═══════════════════════════════════════════════════
        //  TAB 1: HAFTALIK GRAFİK
        // ═══════════════════════════════════════════════════

        private void HaftalikGrafikCiz()
        {
            if (haftalikRaporlar == null || haftalikRaporlar.Count == 0)
            {
                EditorGUILayout.HelpBox("Haftalık rapor verisi bulunamadı.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("📅 Haftalık İlerleme", baslikStyle);
            EditorGUILayout.Space(8);

            // Haftalık genel başarı çizgi grafiği
            string[] haftaEtiketleri = haftalikRaporlar.Select(h => h.haftaEtiketi).ToArray();
            float[] basariSerileri = haftalikRaporlar.Select(h => h.basariYuzdesi).ToArray();

            // Genel başarı çizgi grafiği
            Rect cizgiRect = GUILayoutUtility.GetRect(600, 250);
            GraphRenderer.CizgiGrafik(cizgiRect, haftaEtiketleri,
                new float[][] { basariSerileri },
                new string[] { "Genel Başarı" },
                new Color[] { new Color(0.3f, 0.7f, 1f) },
                "Haftalık Başarı Trendi (%)", 100f, 0f);

            EditorGUILayout.Space(12);

            // Ders bazlı haftalık karşılaştırma
            if (tumDersleriGoster)
            {
                // Tüm derslerin haftalık çizgi grafiği
                var dersler = Enum.GetValues(typeof(DersKategorisi)).Cast<DersKategorisi>().ToArray();
                var seriListesi = new List<float[]>();
                var seriAdlari = new List<string>();
                var seriRenkleri = new List<Color>();

                foreach (var ders in dersler)
                {
                    float[] dersSeri = haftalikRaporlar.Select(h =>
                        h.dersBazliBasari.ContainsKey(ders) ? h.dersBazliBasari[ders] : 0f).ToArray();

                    // Sadece veri varsa göster
                    if (dersSeri.Any(v => v > 0))
                    {
                        seriListesi.Add(dersSeri);
                        seriAdlari.Add(RaporHesaplayici.DersAdi(ders));
                        seriRenkleri.Add(GraphRenderer.DersRenkleri[(int)ders % GraphRenderer.DersRenkleri.Length]);
                    }
                }

                if (seriListesi.Count > 0)
                {
                    Rect dersCizgiRect = GUILayoutUtility.GetRect(600, 280);
                    GraphRenderer.CizgiGrafik(dersCizgiRect, haftaEtiketleri,
                        seriListesi.ToArray(), seriAdlari.ToArray(), seriRenkleri.ToArray(),
                        "Ders Bazlı Haftalık Başarı (%)", 100f, 0f);
                }
            }
            else
            {
                // Tek ders
                float[] dersSeri = haftalikRaporlar.Select(h =>
                    h.dersBazliBasari.ContainsKey(secilenDers) ? h.dersBazliBasari[secilenDers] : 0f).ToArray();

                Rect tekDersRect = GUILayoutUtility.GetRect(600, 250);
                int idx = (int)secilenDers;
                GraphRenderer.CizgiGrafik(tekDersRect, haftaEtiketleri,
                    new float[][] { dersSeri },
                    new string[] { RaporHesaplayici.DersAdi(secilenDers) },
                    new Color[] { GraphRenderer.DersRenkleri[idx % GraphRenderer.DersRenkleri.Length] },
                    $"{RaporHesaplayici.DersAdi(secilenDers)} — Haftalık", 100f, 0f);
            }

            EditorGUILayout.Space(12);

            // Haftalık soru sayısı dikey çubuk
            float[] soruSayilari = haftalikRaporlar.Select(h => (float)h.toplamSoru).ToArray();
            float[] dogruSayilari = haftalikRaporlar.Select(h => (float)h.dogruSayisi).ToArray();
            float[] yanlisSayilari = haftalikRaporlar.Select(h => (float)h.yanlisSayisi).ToArray();

            Rect cubukRect = GUILayoutUtility.GetRect(600, 220);
            GraphRenderer.DikeyCubukGrafik(cubukRect, haftaEtiketleri,
                new float[][] { dogruSayilari, yanlisSayilari },
                new string[] { "Doğru", "Yanlış" },
                new Color[] { new Color(0.3f, 0.85f, 0.4f), new Color(1f, 0.3f, 0.3f) },
                "Haftalık Soru Sayısı");

            EditorGUILayout.Space(12);

            // Haftalık tablo
            HaftalikTablosCiz();
        }

        private void HaftalikTablosCiz()
        {
            EditorGUILayout.BeginVertical(kutuStyle);
            EditorGUILayout.LabelField("📋 Haftalık Detay Tablo", altBaslikStyle);
            EditorGUILayout.Space(4);

            // Başlık
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Hafta", EditorStyles.boldLabel, GUILayout.Width(90));
            EditorGUILayout.LabelField("Soru", EditorStyles.boldLabel, GUILayout.Width(45));
            EditorGUILayout.LabelField("D", EditorStyles.boldLabel, GUILayout.Width(35));
            EditorGUILayout.LabelField("Y", EditorStyles.boldLabel, GUILayout.Width(35));
            EditorGUILayout.LabelField("Başarı", EditorStyles.boldLabel, GUILayout.Width(55));
            EditorGUILayout.LabelField("Ort Süre", EditorStyles.boldLabel, GUILayout.Width(60));
            EditorGUILayout.LabelField("Oyun", EditorStyles.boldLabel, GUILayout.Width(40));
            EditorGUILayout.LabelField("Trend", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < haftalikRaporlar.Count; i++)
            {
                var h = haftalikRaporlar[i];
                if (h.toplamSoru == 0) continue;

                Rect satirRect = EditorGUILayout.BeginHorizontal();
                if (i % 2 == 0) EditorGUI.DrawRect(satirRect, new Color(0.25f, 0.25f, 0.25f, 0.3f));

                EditorGUILayout.LabelField(h.haftaEtiketi, GUILayout.Width(90));
                EditorGUILayout.LabelField($"{h.toplamSoru}", GUILayout.Width(45));
                EditorGUILayout.LabelField($"{h.dogruSayisi}", GUILayout.Width(35));
                EditorGUILayout.LabelField($"{h.yanlisSayisi}", GUILayout.Width(35));
                EditorGUILayout.LabelField($"%{h.basariYuzdesi:F0}", GUILayout.Width(55));
                EditorGUILayout.LabelField($"{h.ortSure:F1}s", GUILayout.Width(60));
                EditorGUILayout.LabelField($"{h.oyunSayisi}", GUILayout.Width(40));

                // Trend
                if (i > 0)
                {
                    var onceki = haftalikRaporlar[i - 1];
                    if (onceki.toplamSoru > 0)
                    {
                        string ok = GraphRenderer.TrendOku(onceki.basariYuzdesi, h.basariYuzdesi);
                        Color tRenk = GraphRenderer.TrendRengi(onceki.basariYuzdesi, h.basariYuzdesi);
                        EditorGUILayout.LabelField(ok, new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = tRenk } });
                    }
                    else
                        EditorGUILayout.LabelField("-");
                }
                else
                    EditorGUILayout.LabelField("-");

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        // ═══════════════════════════════════════════════════
        //  TAB 2: GÜNLÜK GRAFİK
        // ═══════════════════════════════════════════════════

        private void GunlukGrafikCiz()
        {
            if (gunlukRaporlar == null || gunlukRaporlar.Count == 0)
            {
                EditorGUILayout.HelpBox("Günlük rapor verisi bulunamadı.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("📆 Günlük İlerleme", baslikStyle);
            EditorGUILayout.Space(8);

            // Günlük başarı çizgi grafiği
            string[] gunEtiketleri = gunlukRaporlar.Select(g =>
            {
                var dt = DateTime.Parse(g.tarih);
                return $"{dt.Day}/{dt.Month}";
            }).ToArray();

            float[] gunBasari = gunlukRaporlar.Select(g => g.basariYuzdesi).ToArray();

            Rect cizgiRect = GUILayoutUtility.GetRect(600, 250);
            GraphRenderer.CizgiGrafik(cizgiRect, gunEtiketleri,
                new float[][] { gunBasari },
                new string[] { "Günlük Başarı" },
                new Color[] { new Color(0.3f, 0.7f, 1f) },
                $"Son {gunSayisi} Gün — Günlük Başarı (%)", 100f, 0f);

            EditorGUILayout.Space(12);

            // Günlük soru sayıları
            float[] gunDogru = gunlukRaporlar.Select(g => (float)g.dogruSayisi).ToArray();
            float[] gunYanlis = gunlukRaporlar.Select(g => (float)g.yanlisSayisi).ToArray();

            Rect cubukRect = GUILayoutUtility.GetRect(600, 200);
            GraphRenderer.DikeyCubukGrafik(cubukRect, gunEtiketleri,
                new float[][] { gunDogru, gunYanlis },
                new string[] { "Doğru", "Yanlış" },
                new Color[] { new Color(0.3f, 0.85f, 0.4f), new Color(1f, 0.3f, 0.3f) },
                "Günlük Soru Sayısı");

            EditorGUILayout.Space(12);

            // Günlük tablo
            GunlukTabloCiz();
        }

        private void GunlukTabloCiz()
        {
            EditorGUILayout.BeginVertical(kutuStyle);
            EditorGUILayout.LabelField("📋 Günlük Detay Tablo", altBaslikStyle);
            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Tarih", EditorStyles.boldLabel, GUILayout.Width(85));
            EditorGUILayout.LabelField("Soru", EditorStyles.boldLabel, GUILayout.Width(45));
            EditorGUILayout.LabelField("D", EditorStyles.boldLabel, GUILayout.Width(35));
            EditorGUILayout.LabelField("Y", EditorStyles.boldLabel, GUILayout.Width(35));
            EditorGUILayout.LabelField("Başarı", EditorStyles.boldLabel, GUILayout.Width(55));
            EditorGUILayout.LabelField("Ort Süre", EditorStyles.boldLabel, GUILayout.Width(60));
            EditorGUILayout.LabelField("Bar", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < gunlukRaporlar.Count; i++)
            {
                var g = gunlukRaporlar[i];
                var dt = DateTime.Parse(g.tarih);
                string gunAdi = GunAdi(dt.DayOfWeek);

                Rect satirRect = EditorGUILayout.BeginHorizontal();
                if (i % 2 == 0) EditorGUI.DrawRect(satirRect, new Color(0.25f, 0.25f, 0.25f, 0.3f));

                EditorGUILayout.LabelField($"{dt:dd.MM} {gunAdi}", GUILayout.Width(85));
                EditorGUILayout.LabelField($"{g.toplamSoru}", GUILayout.Width(45));
                EditorGUILayout.LabelField($"{g.dogruSayisi}", GUILayout.Width(35));
                EditorGUILayout.LabelField($"{g.yanlisSayisi}", GUILayout.Width(35));
                EditorGUILayout.LabelField($"%{g.basariYuzdesi:F0}", GUILayout.Width(55));
                EditorGUILayout.LabelField($"{g.ortSure:F1}s", GUILayout.Width(60));

                // Mini ilerleme çubuğu
                Rect barRect = GUILayoutUtility.GetRect(100, 16);
                GraphRenderer.IlerlemeÇubugu(barRect, g.basariYuzdesi, "", null, false);

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        // ═══════════════════════════════════════════════════
        //  TAB 3: DERS KARŞILAŞTIRMA
        // ═══════════════════════════════════════════════════

        private void DersKarsilastirmaCiz()
        {
            EditorGUILayout.LabelField("📊 Ders Bazlı Karşılaştırma", baslikStyle);
            EditorGUILayout.Space(8);

            var p = secilenPerformans;
            var istatistikler = p.TumDersIstatistikleri();

            if (istatistikler.Count == 0)
            {
                EditorGUILayout.HelpBox("Henüz yeterli ders verisi yok.", MessageType.Info);
                return;
            }

            // Yatay Çubuk Grafik: Ders bazlı başarı
            string[] dersEtiketler = istatistikler.Select(i => RaporHesaplayici.DersAdi(i.ders)).ToArray();
            float[] basariDegerler = istatistikler.Select(i => i.basariYuzdesi).ToArray();
            Color[] dersRenkler = istatistikler.Select(i =>
                GraphRenderer.DersRenkleri[(int)i.ders % GraphRenderer.DersRenkleri.Length]).ToArray();

            Rect yatayRect = GUILayoutUtility.GetRect(500, 200);
            GraphRenderer.CubukGrafik(yatayRect, dersEtiketler, basariDegerler, dersRenkler,
                "Ders Bazlı Başarı Oranı (%)", 100f, true);

            EditorGUILayout.Space(12);

            // Haftalık ders bazlı dikey çubuk karşılaştırma (geçen hafta vs bu hafta)
            if (karsilastirma != null &&
                karsilastirma.oncekiHafta.toplamSoru > 0 &&
                karsilastirma.buHafta.toplamSoru > 0)
            {
                var dersler = karsilastirma.oncekiHafta.dersBazliBasari.Keys
                    .Union(karsilastirma.buHafta.dersBazliBasari.Keys)
                    .Distinct().OrderBy(d => (int)d).ToArray();

                if (dersler.Length > 0)
                {
                    string[] kEtiketler = dersler.Select(d => RaporHesaplayici.DersAdi(d)).ToArray();
                    float[] gecenHafta = dersler.Select(d =>
                        karsilastirma.oncekiHafta.dersBazliBasari.ContainsKey(d)
                            ? karsilastirma.oncekiHafta.dersBazliBasari[d] : 0f).ToArray();
                    float[] buHafta = dersler.Select(d =>
                        karsilastirma.buHafta.dersBazliBasari.ContainsKey(d)
                            ? karsilastirma.buHafta.dersBazliBasari[d] : 0f).ToArray();

                    Rect karRect = GUILayoutUtility.GetRect(500, 250);
                    GraphRenderer.DikeyCubukGrafik(karRect, kEtiketler,
                        new float[][] { gecenHafta, buHafta },
                        new string[] { "Geçen Hafta", "Bu Hafta" },
                        new Color[] { new Color(0.5f, 0.5f, 0.5f, 0.7f), new Color(0.3f, 0.7f, 1f) },
                        "Geçen Hafta vs Bu Hafta — Ders Başarısı", 100f);
                }
            }

            EditorGUILayout.Space(12);

            // Zorluk bazlı kırılım
            EditorGUILayout.BeginVertical(kutuStyle);
            EditorGUILayout.LabelField("🎯 Zorluk Bazlı Kırılım", altBaslikStyle);
            EditorGUILayout.Space(4);

            // Başlık
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Ders", EditorStyles.boldLabel, GUILayout.Width(110));
            EditorGUILayout.LabelField("Kolay", EditorStyles.boldLabel, GUILayout.Width(80));
            EditorGUILayout.LabelField("Orta", EditorStyles.boldLabel, GUILayout.Width(80));
            EditorGUILayout.LabelField("Zor", EditorStyles.boldLabel, GUILayout.Width(80));
            EditorGUILayout.LabelField("Genel", EditorStyles.boldLabel, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();

            foreach (var ist in istatistikler)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(RaporHesaplayici.DersAdi(ist.ders), GUILayout.Width(110));

                ZorlukHucreCiz(ist.kolayDogru, ist.kolayYanlis, GraphRenderer.ZorlukKolay);
                ZorlukHucreCiz(ist.ortaDogru, ist.ortaYanlis, GraphRenderer.ZorlukOrta);
                ZorlukHucreCiz(ist.zorDogru, ist.zorYanlis, GraphRenderer.ZorlukZor);

                EditorGUILayout.LabelField($"%{ist.basariYuzdesi:F0}",
                    new GUIStyle(EditorStyles.boldLabel)
                    { normal = { textColor = GraphRenderer.BasariRengi(ist.basariYuzdesi) } },
                    GUILayout.Width(60));

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void ZorlukHucreCiz(int dogru, int yanlis, Color renk)
        {
            int toplam = dogru + yanlis;
            if (toplam == 0)
            {
                EditorGUILayout.LabelField("-", GUILayout.Width(80));
                return;
            }
            float yuzde = (float)dogru / toplam * 100f;
            EditorGUILayout.LabelField($"{dogru}D/{yanlis}Y (%{yuzde:F0})",
                new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = renk } },
                GUILayout.Width(80));
        }

        // ═══════════════════════════════════════════════════
        //  TAB 4: TREND ANALİZİ
        // ═══════════════════════════════════════════════════

        private void TrendAnaliziCiz()
        {
            EditorGUILayout.LabelField("📈 Trend Analizi", baslikStyle);
            EditorGUILayout.Space(8);

            var p = secilenPerformans;

            // Ders seçimi
            secilenDers = (DersKategorisi)EditorGUILayout.EnumPopup("Analiz Edilecek Ders", secilenDers);
            EditorGUILayout.Space(8);

            var trend = p.GelisimTrendiHesapla(secilenDers);
            if (trend.Count == 0)
            {
                EditorGUILayout.HelpBox($"{RaporHesaplayici.DersAdi(secilenDers)} dersinde gelişim verisi bulunamadı.", MessageType.Info);
                return;
            }

            // Çizgi grafik
            string[] tarihler = trend.Select(t =>
            {
                var dt = DateTime.Parse(t.tarih);
                return $"{dt.Day}/{dt.Month}";
            }).ToArray();
            float[] basarilar = trend.Select(t => t.basariYuzdesi).ToArray();

            int idx = (int)secilenDers;
            Color dersRenk = GraphRenderer.DersRenkleri[idx % GraphRenderer.DersRenkleri.Length];

            Rect trendRect = GUILayoutUtility.GetRect(600, 260);
            GraphRenderer.CizgiGrafik(trendRect, tarihler,
                new float[][] { basarilar },
                new string[] { RaporHesaplayici.DersAdi(secilenDers) },
                new Color[] { dersRenk },
                $"{RaporHesaplayici.DersAdi(secilenDers)} — Gelişim Trendi", 100f, 0f);

            EditorGUILayout.Space(12);

            // Trend tablosu ve analiz
            EditorGUILayout.BeginVertical(kutuStyle);
            EditorGUILayout.LabelField("📋 Gün Bazlı Detay", altBaslikStyle);
            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Tarih", EditorStyles.boldLabel, GUILayout.Width(85));
            EditorGUILayout.LabelField("D", EditorStyles.boldLabel, GUILayout.Width(35));
            EditorGUILayout.LabelField("Y", EditorStyles.boldLabel, GUILayout.Width(35));
            EditorGUILayout.LabelField("Başarı", EditorStyles.boldLabel, GUILayout.Width(55));
            EditorGUILayout.LabelField("Trend", EditorStyles.boldLabel, GUILayout.Width(50));
            EditorGUILayout.LabelField("Bar", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            float onceki = -1;
            foreach (var gun in trend)
            {
                var dt = DateTime.Parse(gun.tarih);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{dt:dd.MM} {GunAdi(dt.DayOfWeek)}", GUILayout.Width(85));
                EditorGUILayout.LabelField($"{gun.dogruSayisi}", GUILayout.Width(35));
                EditorGUILayout.LabelField($"{gun.yanlisSayisi}", GUILayout.Width(35));
                EditorGUILayout.LabelField($"%{gun.basariYuzdesi:F0}", GUILayout.Width(55));

                if (onceki >= 0)
                {
                    string ok = GraphRenderer.TrendOku(onceki, gun.basariYuzdesi);
                    Color tRenk = GraphRenderer.TrendRengi(onceki, gun.basariYuzdesi);
                    EditorGUILayout.LabelField(ok, new GUIStyle(EditorStyles.boldLabel)
                    { normal = { textColor = tRenk } }, GUILayout.Width(50));
                }
                else
                    EditorGUILayout.LabelField("-", GUILayout.Width(50));

                onceki = gun.basariYuzdesi;

                Rect barRect = GUILayoutUtility.GetRect(100, 16);
                GraphRenderer.IlerlemeÇubugu(barRect, gun.basariYuzdesi, "", dersRenk, false);

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();

            // Genel yorum
            EditorGUILayout.Space(8);
            EditorGUILayout.BeginVertical(kutuStyle);
            EditorGUILayout.LabelField("💬 Değerlendirme", altBaslikStyle);
            EditorGUILayout.Space(4);

            if (trend.Count >= 2)
            {
                float ilk = trend.First().basariYuzdesi;
                float son = trend.Last().basariYuzdesi;
                float fark = son - ilk;

                // Son 3 günün ortalaması
                var son3 = trend.TakeLast(Mathf.Min(3, trend.Count)).ToList();
                float son3Ort = son3.Average(t => t.basariYuzdesi);

                // İlk 3 günün ortalaması
                var ilk3 = trend.Take(Mathf.Min(3, trend.Count)).ToList();
                float ilk3Ort = ilk3.Average(t => t.basariYuzdesi);

                string yorum;
                if (fark > 15)
                    yorum = $"📈 Harika gelişim! Başarı %{ilk:F0}'dan %{son:F0}'e yükseldi (+{fark:F0}). Öğrenci bu derste hızla ilerliyor.";
                else if (fark > 5)
                    yorum = $"📈 Olumlu gelişim. %{ilk:F0} → %{son:F0} (+{fark:F0}). İstikrarlı ilerleme gözleniyor.";
                else if (fark > -5)
                    yorum = $"➡ Performans stabil. %{ilk:F0} → %{son:F0}. Bir üst seviyeye geçmek için daha fazla pratik gerekebilir.";
                else if (fark > -15)
                    yorum = $"📉 Hafif düşüş. %{ilk:F0} → %{son:F0} ({fark:F0}). Konuların tekrar edilmesi önerilir.";
                else
                    yorum = $"📉 Ciddi düşüş! %{ilk:F0} → %{son:F0} ({fark:F0}). Bu derste acil destek gerekiyor.";

                EditorGUILayout.LabelField(yorum, bilgiStyle);

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField(
                    $"İlk dönem ort: %{ilk3Ort:F0}  |  Son dönem ort: %{son3Ort:F0}  |  Fark: {(son3Ort - ilk3Ort >= 0 ? "+" : "")}{son3Ort - ilk3Ort:F0}",
                    bilgiStyle);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(12);

            // Tekrar yanlışlar bu ders için
            EditorGUILayout.BeginVertical(kutuStyle);
            EditorGUILayout.LabelField("❌ Bu Dersteki Tekrar Yanlışlar", altBaslikStyle);
            EditorGUILayout.Space(4);

            var tekrarlar = p.TekrarYanlislariBul()
                .Where(t => t.ders == secilenDers)
                .ToList();

            if (tekrarlar.Count == 0)
            {
                EditorGUILayout.LabelField("Bu derste tekrar yanlış yapılan soru yok 👏", bilgiStyle);
            }
            else
            {
                foreach (var t in tekrarlar)
                {
                    string kisa = t.soruMetni.Length > 60 ? t.soruMetni.Substring(0, 60) + "..." : t.soruMetni;
                    string durum = t.ogrendiMi ? "✅ Öğrendi" : (t.sonDenemeDogru ? "⚠ İlerliyor" : "❌ Zorlanıyor");

                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    EditorGUILayout.LabelField($"{durum} | {kisa} ({t.toplamDeneme} deneme, {t.yanlisSayisi} yanlış)", bilgiStyle);
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndVertical();
        }

        // ═══════════════════════════════════════════════════
        //  YARDIMCILAR
        // ═══════════════════════════════════════════════════

        private string GunAdi(DayOfWeek gun)
        {
            switch (gun)
            {
                case DayOfWeek.Monday: return "Pzt";
                case DayOfWeek.Tuesday: return "Sal";
                case DayOfWeek.Wednesday: return "Çar";
                case DayOfWeek.Thursday: return "Per";
                case DayOfWeek.Friday: return "Cum";
                case DayOfWeek.Saturday: return "Cmt";
                case DayOfWeek.Sunday: return "Paz";
                default: return "";
            }
        }
    }
}
