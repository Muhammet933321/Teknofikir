using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using QuizGame.Data;
using QuizGame.Managers;

namespace QuizGame.Editor
{
    /// <summary>
    /// Öğrenci performans raporu Editor penceresi.
    /// Menu: QuizGame > Öğrenci Raporu
    /// 
    /// Sol panel  : Sınıf → Öğrenci listesi
    /// Sağ panel  : Genel özet, ders bazlı istatistik, gelişim trendi, tekrar yanlışlar
    /// </summary>
    public class StudentReportUI : EditorWindow
    {
        [MenuItem("QuizGame/Öğrenci Raporu")]
        public static void ShowWindow()
        {
            var w = GetWindow<StudentReportUI>("Öğrenci Raporu");
            w.minSize = new Vector2(900, 600);
        }

        // ── State ──
        private DataManager dm;
        private List<ClassData> siniflar;
        private int secilenSinifIndex = -1;
        private int secilenOgrenciIndex = -1;
        private StudentPerformance secilenPerformans;

        // UI
        private Vector2 solScroll, sagScroll;
        private int aktifTab; // 0=Özet, 1=Ders Detay, 2=Gelişim, 3=Tekrar Yanlışlar
        private DersKategorisi secilenDers = DersKategorisi.Matematik;

        // Stiller
        private GUIStyle baslikStyle, altBaslikStyle, kutuStyle, istatStyle;
        private bool stillerHazir;

        // ═══════════════════════════════════════════════════
        //  STİLLER
        // ═══════════════════════════════════════════════════

        private void StilleriHazirla()
        {
            if (stillerHazir) return;
            stillerHazir = true;

            baslikStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };

            altBaslikStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13
            };

            kutuStyle = new GUIStyle("box")
            {
                padding = new RectOffset(10, 10, 8, 8)
            };

            istatStyle = new GUIStyle(EditorStyles.label)
            {
                richText = true,
                wordWrap = true
            };
        }

        // ═══════════════════════════════════════════════════
        //  ANA ÇİZİM
        // ═══════════════════════════════════════════════════

        // Offline veri kaynakları
        private SchoolDatabase offlineOkulDB;
        private PerformanceDatabase offlinePerfDB;

        private void OfflineVerileriYukle()
        {
            string okulYolu = System.IO.Path.Combine(Application.persistentDataPath, "okul_veritabani.json");
            string perfYolu = System.IO.Path.Combine(Application.persistentDataPath, "performans_veritabani.json");

            if (System.IO.File.Exists(okulYolu))
            {
                try { offlineOkulDB = JsonUtility.FromJson<SchoolDatabase>(System.IO.File.ReadAllText(okulYolu)); }
                catch { offlineOkulDB = new SchoolDatabase(); }
            }
            else offlineOkulDB = new SchoolDatabase();

            if (System.IO.File.Exists(perfYolu))
            {
                try { offlinePerfDB = JsonUtility.FromJson<PerformanceDatabase>(System.IO.File.ReadAllText(perfYolu)); }
                catch { offlinePerfDB = new PerformanceDatabase(); }
            }
            else offlinePerfDB = new PerformanceDatabase();
        }

        private void OnEnable()
        {
            OfflineVerileriYukle();
        }

        private void OnFocus()
        {
            OfflineVerileriYukle();
        }

        private void OnGUI()
        {
            StilleriHazirla();

            // DataManager veya offline veri kullan
            if (Application.isPlaying && DataManager.Instance != null)
            {
                dm = DataManager.Instance;
                siniflar = dm.okulVeritabani.siniflar;
            }
            else
            {
                dm = null;
                if (offlineOkulDB == null) OfflineVerileriYukle();
                siniflar = offlineOkulDB?.siniflar;
            }

            if (siniflar == null)
            {
                EditorGUILayout.HelpBox("Veri bulunamadı. Oyunu en az bir kez çalıştırın.", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            SolPanelCiz();
            SagPanelCiz();
            EditorGUILayout.EndHorizontal();
        }

        // ═══════════════════════════════════════════════════
        //  SOL PANEL (Sınıf ve Öğrenci Listesi)
        // ═══════════════════════════════════════════════════

        private void SolPanelCiz()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(250));
            EditorGUILayout.LabelField("Öğrenci Seçimi", baslikStyle);
            EditorGUILayout.Space(4);

            solScroll = EditorGUILayout.BeginScrollView(solScroll);

            if (siniflar == null || siniflar.Count == 0)
            {
                EditorGUILayout.HelpBox("Henüz sınıf eklenmemiş.", MessageType.Warning);
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
                return;
            }

            // Sınıf seçimi
            string[] sinifAdlari = siniflar.Select(s => s.sinifAdi).ToArray();
            int yeniSinifIndex = EditorGUILayout.Popup("Sınıf", secilenSinifIndex, sinifAdlari);
            if (yeniSinifIndex != secilenSinifIndex)
            {
                secilenSinifIndex = yeniSinifIndex;
                secilenOgrenciIndex = -1;
                secilenPerformans = null;
            }

            EditorGUILayout.Space(6);

            if (secilenSinifIndex >= 0 && secilenSinifIndex < siniflar.Count)
            {
                var sinif = siniflar[secilenSinifIndex];
                EditorGUILayout.LabelField($"Öğrenciler ({sinif.ogrenciler.Count})", altBaslikStyle);

                for (int i = 0; i < sinif.ogrenciler.Count; i++)
                {
                    var ogr = sinif.ogrenciler[i];
                    bool secili = (i == secilenOgrenciIndex);
                    var style = secili
                        ? new GUIStyle(EditorStyles.miniButtonMid) { fontStyle = FontStyle.Bold }
                        : EditorStyles.miniButton;

                    if (GUILayout.Button($"{ogr.ogrenciNo} - {ogr.TamAd}", style))
                    {
                        secilenOgrenciIndex = i;
                        if (dm != null)
                            secilenPerformans = dm.OgrenciPerformansiGetir(ogr.id);
                        else
                            secilenPerformans = offlinePerfDB?.PerformansGetir(ogr.id);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // ═══════════════════════════════════════════════════
        //  SAĞ PANEL
        // ═══════════════════════════════════════════════════

        private void SagPanelCiz()
        {
            EditorGUILayout.BeginVertical();

            if (secilenPerformans == null)
            {
                EditorGUILayout.HelpBox(
                    "Soldaki listeden bir öğrenci seçin.\n" +
                    "Öğrenci daha önce oyun oynamış olmalıdır.",
                    MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            if (secilenPerformans.tumCevaplar.Count == 0)
            {
                var sinif = siniflar[secilenSinifIndex];
                var ogr = sinif.ogrenciler[secilenOgrenciIndex];
                EditorGUILayout.LabelField($"{ogr.TamAd}", baslikStyle);
                EditorGUILayout.HelpBox("Bu öğrenci henüz hiç oyun oynamamış.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

            // Sekme seçimi
            string[] tablar = { "Genel Özet", "Ders Detay", "Gelişim Trendi", "Tekrar Yanlışlar" };
            aktifTab = GUILayout.Toolbar(aktifTab, tablar, GUILayout.Height(28));

            EditorGUILayout.Space(4);

            sagScroll = EditorGUILayout.BeginScrollView(sagScroll);

            switch (aktifTab)
            {
                case 0: GenelOzetCiz(); break;
                case 1: DersDetayCiz(); break;
                case 2: GelisimTrendiCiz(); break;
                case 3: TekrarYanlislarCiz(); break;
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
            EditorGUILayout.LabelField($"{p.ogrenciAd} — Genel Rapor", baslikStyle);
            EditorGUILayout.Space(6);

            EditorGUILayout.BeginVertical(kutuStyle);
            EditorGUILayout.LabelField("Genel İstatistikler", altBaslikStyle);
            EditorGUILayout.Space(4);

            int toplam = p.tumCevaplar.Count;
            int dogru = p.tumCevaplar.Count(c => c.dogruMu);
            int yanlis = toplam - dogru;
            float yuzde = p.GenelBasariYuzdesi();
            float ortSure = p.OrtCevapSuresi();
            int oyunSayisi = p.ToplamOyunSayisi();

            Bilgi("Toplam Cevap", $"{toplam}");
            Bilgi("Doğru / Yanlış", $"<color=green>{dogru}</color> / <color=red>{yanlis}</color>");
            Bilgi("Başarı Oranı", $"%{yuzde:F1}");
            Bilgi("Ort. Cevap Süresi", $"{ortSure:F1}s");
            Bilgi("Toplam Oyun", $"{oyunSayisi}");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);

            // Ders bazlı kısa özet tablo
            EditorGUILayout.BeginVertical(kutuStyle);
            EditorGUILayout.LabelField("Ders Bazlı Özet", altBaslikStyle);
            EditorGUILayout.Space(4);

            var istatistikler = p.TumDersIstatistikleri();

            // Başlık satırı
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Ders", EditorStyles.boldLabel, GUILayout.Width(120));
            EditorGUILayout.LabelField("Soru", EditorStyles.boldLabel, GUILayout.Width(50));
            EditorGUILayout.LabelField("D", EditorStyles.boldLabel, GUILayout.Width(35));
            EditorGUILayout.LabelField("Y", EditorStyles.boldLabel, GUILayout.Width(35));
            EditorGUILayout.LabelField("Başarı", EditorStyles.boldLabel, GUILayout.Width(60));
            EditorGUILayout.LabelField("Ort Süre", EditorStyles.boldLabel, GUILayout.Width(60));
            EditorGUILayout.LabelField("Durum", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            foreach (var ist in istatistikler)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(DersAdi(ist.ders), GUILayout.Width(120));
                EditorGUILayout.LabelField($"{ist.toplamSoru}", GUILayout.Width(50));
                EditorGUILayout.LabelField($"{ist.dogruSayisi}", GUILayout.Width(35));
                EditorGUILayout.LabelField($"{ist.yanlisSayisi}", GUILayout.Width(35));
                EditorGUILayout.LabelField($"%{ist.basariYuzdesi:F0}", GUILayout.Width(60));
                EditorGUILayout.LabelField($"{ist.ortalamaSure:F1}s", GUILayout.Width(60));

                string durum = ist.basariYuzdesi >= 80 ? "⭐ Çok İyi" :
                               ist.basariYuzdesi >= 60 ? "✅ İyi" :
                               ist.basariYuzdesi >= 40 ? "⚠️ Orta" : "❌ Zayıf";
                EditorGUILayout.LabelField(durum);
                EditorGUILayout.EndHorizontal();
            }

            if (istatistikler.Count == 0)
                EditorGUILayout.LabelField("Henüz veri yok.");

            EditorGUILayout.EndVertical();
        }

        // ═══════════════════════════════════════════════════
        //  TAB 1: DERS DETAY
        // ═══════════════════════════════════════════════════

        private void DersDetayCiz()
        {
            var p = secilenPerformans;
            EditorGUILayout.LabelField("Ders Bazlı Detay", baslikStyle);
            EditorGUILayout.Space(4);

            // Ders seçimi
            secilenDers = (DersKategorisi)EditorGUILayout.EnumPopup("Ders Seçin", secilenDers);
            EditorGUILayout.Space(6);

            var ist = p.DersIstatistigiHesapla(secilenDers);
            if (ist == null)
            {
                EditorGUILayout.HelpBox($"{DersAdi(secilenDers)} dersinden henüz soru cevaplanmamış.", MessageType.Info);
                return;
            }

            // Genel
            EditorGUILayout.BeginVertical(kutuStyle);
            EditorGUILayout.LabelField($"{DersAdi(secilenDers)} — İstatistikler", altBaslikStyle);
            Bilgi("Toplam Soru", $"{ist.toplamSoru}");
            Bilgi("Doğru / Yanlış", $"<color=green>{ist.dogruSayisi}</color> / <color=red>{ist.yanlisSayisi}</color>");
            Bilgi("Başarı", $"%{ist.basariYuzdesi:F1}");
            Bilgi("Ort. Süre", $"{ist.ortalamaSure:F1}s");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);

            // Zorluk kırılımı
            EditorGUILayout.BeginVertical(kutuStyle);
            EditorGUILayout.LabelField("Zorluk Bazlı Kırılım", altBaslikStyle);
            EditorGUILayout.Space(4);

            ZorlukSatiri("Kolay", ist.kolayDogru, ist.kolayYanlis);
            ZorlukSatiri("Orta", ist.ortaDogru, ist.ortaYanlis);
            ZorlukSatiri("Zor", ist.zorDogru, ist.zorYanlis);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);

            // Son 10 cevap
            EditorGUILayout.BeginVertical(kutuStyle);
            EditorGUILayout.LabelField("Son Cevaplar", altBaslikStyle);
            EditorGUILayout.Space(4);

            var sonCevaplar = p.tumCevaplar
                .Where(c => c.ders == secilenDers)
                .OrderByDescending(c => c.TarihAsDateTime)
                .Take(10)
                .ToList();

            foreach (var c in sonCevaplar)
            {
                string icon = c.dogruMu ? "✅" : "❌";
                string sure = c.cevapSuresi > 0 ? $"{c.cevapSuresi:F1}s" : "-";
                string tarih = c.TarihAsDateTime.ToString("dd.MM HH:mm");
                string kisa = c.soruMetni.Length > 45 ? c.soruMetni.Substring(0, 45) + "..." : c.soruMetni;
                EditorGUILayout.LabelField($"{icon} [{tarih}] ({sure}) {kisa}", istatStyle);
            }

            EditorGUILayout.EndVertical();
        }

        // ═══════════════════════════════════════════════════
        //  TAB 2: GELİŞİM TRENDİ
        // ═══════════════════════════════════════════════════

        private void GelisimTrendiCiz()
        {
            var p = secilenPerformans;
            EditorGUILayout.LabelField("Gelişim Trendi", baslikStyle);
            EditorGUILayout.Space(4);

            secilenDers = (DersKategorisi)EditorGUILayout.EnumPopup("Ders", secilenDers);
            EditorGUILayout.Space(6);

            var trend = p.GelisimTrendiHesapla(secilenDers);
            if (trend.Count == 0)
            {
                EditorGUILayout.HelpBox($"{DersAdi(secilenDers)} dersinde gelişim verisi yok.", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginVertical(kutuStyle);
            EditorGUILayout.LabelField($"{DersAdi(secilenDers)} — Gün Bazlı Gelişim", altBaslikStyle);
            EditorGUILayout.Space(4);

            // Başlık
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Tarih", EditorStyles.boldLabel, GUILayout.Width(100));
            EditorGUILayout.LabelField("D", EditorStyles.boldLabel, GUILayout.Width(35));
            EditorGUILayout.LabelField("Y", EditorStyles.boldLabel, GUILayout.Width(35));
            EditorGUILayout.LabelField("Başarı", EditorStyles.boldLabel, GUILayout.Width(60));
            EditorGUILayout.LabelField("Grafik", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            float oncekiYuzde = -1;
            foreach (var gun in trend)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(gun.tarih, GUILayout.Width(100));
                EditorGUILayout.LabelField($"{gun.dogruSayisi}", GUILayout.Width(35));
                EditorGUILayout.LabelField($"{gun.yanlisSayisi}", GUILayout.Width(35));
                EditorGUILayout.LabelField($"%{gun.basariYuzdesi:F0}", GUILayout.Width(60));

                // Basit metin çubuk grafiği
                int barLen = Mathf.RoundToInt(gun.basariYuzdesi / 5f);
                string bar = new string('█', barLen) + new string('░', 20 - barLen);

                // Trend oku
                string trendStr = "";
                if (oncekiYuzde >= 0)
                {
                    if (gun.basariYuzdesi > oncekiYuzde + 5) trendStr = " ↑";
                    else if (gun.basariYuzdesi < oncekiYuzde - 5) trendStr = " ↓";
                    else trendStr = " →";
                }
                oncekiYuzde = gun.basariYuzdesi;

                EditorGUILayout.LabelField($"{bar}{trendStr}");
                EditorGUILayout.EndHorizontal();
            }

            // Genel yorum
            EditorGUILayout.Space(6);
            if (trend.Count >= 2)
            {
                float ilk = trend.First().basariYuzdesi;
                float son = trend.Last().basariYuzdesi;
                float fark = son - ilk;

                string yorum;
                if (fark > 15)
                    yorum = $"📈 Harika gelişim! Başarı %{ilk:F0}'den %{son:F0}'e yükseldi (+{fark:F0}).";
                else if (fark > 5)
                    yorum = $"📈 Olumlu gelişim görülüyor. %{ilk:F0} → %{son:F0} (+{fark:F0}).";
                else if (fark > -5)
                    yorum = $"➡️ Performans stabil. %{ilk:F0} → %{son:F0}.";
                else if (fark > -15)
                    yorum = $"📉 Hafif düşüş var. %{ilk:F0} → %{son:F0} ({fark:F0}). Tekrar çalışması önerilir.";
                else
                    yorum = $"📉 Ciddi düşüş! %{ilk:F0} → %{son:F0} ({fark:F0}). Konuların tekrar edilmesi gerekli.";

                EditorGUILayout.LabelField(yorum, istatStyle);
            }

            EditorGUILayout.EndVertical();
        }

        // ═══════════════════════════════════════════════════
        //  TAB 3: TEKRAR YANLIŞLAR
        // ═══════════════════════════════════════════════════

        private void TekrarYanlislarCiz()
        {
            var p = secilenPerformans;
            EditorGUILayout.LabelField("Tekrar Edilen Yanlışlar", baslikStyle);
            EditorGUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "2 veya daha fazla kez yanlış cevaplanan sorular listelenir.\n" +
                "'Öğrendi' = Son 2 denemede doğru cevap verdi.",
                MessageType.Info);
            EditorGUILayout.Space(4);

            var tekrarlar = p.TekrarYanlislariBul();
            if (tekrarlar.Count == 0)
            {
                EditorGUILayout.LabelField("Tekrar yanlış yapılan soru bulunmadı. 👏");
                return;
            }

            // Öğrendi / Öğrenemedi grupları
            var ogrenenler = tekrarlar.Where(t => t.ogrendiMi).ToList();
            var ogrenemeyen = tekrarlar.Where(t => !t.ogrendiMi).ToList();

            if (ogrenemeyen.Count > 0)
            {
                EditorGUILayout.BeginVertical(kutuStyle);
                EditorGUILayout.LabelField($"❌ Hâlâ Zorlanıyor ({ogrenemeyen.Count} soru)", altBaslikStyle);
                EditorGUILayout.Space(4);

                foreach (var t in ogrenemeyen)
                {
                    EditorGUILayout.BeginVertical("helpbox");
                    string kisa = t.soruMetni.Length > 60 ? t.soruMetni.Substring(0, 60) + "..." : t.soruMetni;
                    EditorGUILayout.LabelField(kisa, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(
                        $"Ders: {DersAdi(t.ders)} | Zorluk: {t.zorluk} | " +
                        $"Deneme: {t.toplamDeneme} | Yanlış: {t.yanlisSayisi} | " +
                        $"Son: {(t.sonDenemeDogru ? "Doğru" : "Yanlış")}",
                        istatStyle);
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(2);
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(4);

            if (ogrenenler.Count > 0)
            {
                EditorGUILayout.BeginVertical(kutuStyle);
                EditorGUILayout.LabelField($"✅ Öğrenildi ({ogrenenler.Count} soru)", altBaslikStyle);
                EditorGUILayout.Space(4);

                foreach (var t in ogrenenler)
                {
                    string kisa = t.soruMetni.Length > 60 ? t.soruMetni.Substring(0, 60) + "..." : t.soruMetni;
                    EditorGUILayout.LabelField(
                        $"✅ {kisa} — {DersAdi(t.ders)} | " +
                        $"İlk {t.yanlisSayisi} yanlış → Şimdi doğru",
                        istatStyle);
                }

                EditorGUILayout.EndVertical();
            }

            // Genel yorum
            EditorGUILayout.Space(8);
            EditorGUILayout.BeginVertical(kutuStyle);
            EditorGUILayout.LabelField("📋 Değerlendirme", altBaslikStyle);

            int toplamTekrar = tekrarlar.Count;
            int ogrendi = ogrenenler.Count;
            float ogrenmeOrani = toplamTekrar > 0 ? (float)ogrendi / toplamTekrar * 100f : 0f;

            string degerlendirme;
            if (ogrenmeOrani >= 80)
                degerlendirme = $"Mükemmel! Yanlış yapılan soruların %{ogrenmeOrani:F0}'ı öğrenilmiş. Öğrenci hatalarından ders çıkarıyor.";
            else if (ogrenmeOrani >= 50)
                degerlendirme = $"İlerliyor. Yanlışların %{ogrenmeOrani:F0}'ı düzeltilmiş. Kalan {ogrenemeyen.Count} soru için ekstra çalışma önerilir.";
            else
                degerlendirme = $"Dikkat! {ogrenemeyen.Count} soruda hâlâ aynı hatalar tekrarlanıyor. Bu konuların bire bir çalışılması önerilir.";

            EditorGUILayout.LabelField(degerlendirme, istatStyle);
            EditorGUILayout.EndVertical();
        }

        // ═══════════════════════════════════════════════════
        //  YARDIMCILAR
        // ═══════════════════════════════════════════════════

        private void Bilgi(string etiket, string deger)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(etiket, GUILayout.Width(140));
            EditorGUILayout.LabelField(deger, istatStyle);
            EditorGUILayout.EndHorizontal();
        }

        private void ZorlukSatiri(string zorlukAdi, int dogru, int yanlis)
        {
            int toplam = dogru + yanlis;
            if (toplam == 0) return;

            float yuzde = (float)dogru / toplam * 100f;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(zorlukAdi, GUILayout.Width(60));
            EditorGUILayout.LabelField($"{dogru}D / {yanlis}Y", GUILayout.Width(80));
            EditorGUILayout.LabelField($"%{yuzde:F0}");

            int bar = Mathf.RoundToInt(yuzde / 5f);
            EditorGUILayout.LabelField(new string('█', bar) + new string('░', 20 - bar));
            EditorGUILayout.EndHorizontal();
        }

        private string DersAdi(DersKategorisi ders)
        {
            switch (ders)
            {
                case DersKategorisi.Matematik: return "Matematik";
                case DersKategorisi.Turkce: return "Türkçe";
                case DersKategorisi.Fen: return "Fen Bilimleri";
                case DersKategorisi.Sosyal: return "Sosyal Bilgiler";
                case DersKategorisi.Ingilizce: return "İngilizce";
                case DersKategorisi.GenelKultur: return "Genel Kültür";
                default: return ders.ToString();
            }
        }
    }
}
