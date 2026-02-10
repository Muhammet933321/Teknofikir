using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Linq;
using QuizGame.Data;
using QuizGame.Managers;

namespace QuizGame.UI
{
    /// <summary>
    /// Runtime ogrenci detay paneli. Sinif yonetiminden ogrenci tiklaninca acilir.
    /// Grafikler, istatistikler, haftalik/gunluk ilerleme, ders bazli analiz gosterir.
    /// </summary>
    public class StudentDetailUI : MonoBehaviour
    {
        [Header("=== Ust Bar ===")]
        [SerializeField] private TextMeshProUGUI baslikText;
        [SerializeField] private Button geriButton;

        [Header("=== Tab Butonlari ===")]
        [SerializeField] private Button tabGenel;
        [SerializeField] private Button tabDersler;
        [SerializeField] private Button tabHaftalik;
        [SerializeField] private Button tabGunluk;
        [SerializeField] private Button tabTrendler;

        [Header("=== Tab Icerikleri ===")]
        [SerializeField] private GameObject genelPanel;
        [SerializeField] private GameObject derslerPanel;
        [SerializeField] private GameObject haftalikPanel;
        [SerializeField] private GameObject gunlukPanel;
        [SerializeField] private GameObject trendlerPanel;

        [Header("=== Scroll Content'ler ===")]
        [SerializeField] private Transform genelContent;
        [SerializeField] private Transform derslerContent;
        [SerializeField] private Transform haftalikContent;
        [SerializeField] private Transform gunlukContent;
        [SerializeField] private Transform trendlerContent;

        // Durum
        private StudentData mevcutOgrenci;
        private StudentPerformance mevcutPerformans;
        private int aktifTab;
        private bool listenersReady;

        private static readonly Color tabAktifRenk = new Color(0.3f, 0.6f, 0.9f, 1f);
        private static readonly Color tabPasifRenk = new Color(0.2f, 0.22f, 0.28f, 0.9f);

        // ================================================
        //  PUBLIC API
        // ================================================

        /// <summary>
        /// Ogrenci detay panelini acar. ClassManagementUI'dan cagrilir.
        /// </summary>
        public void OgrenciGoster(StudentData ogrenci)
        {
            EnsureInit();
            mevcutOgrenci = ogrenci;
            gameObject.SetActive(true);

            if (baslikText != null)
                baslikText.text = ogrenci.TamAd + "  -  #" + ogrenci.ogrenciNo;

            // Performans getir
            if (DataManager.Instance != null)
                mevcutPerformans = DataManager.Instance.OgrenciPerformansiGetir(ogrenci.id);

            TabSec(0);
        }

        // ================================================
        //  INIT
        // ================================================

        private void EnsureInit()
        {
            if (listenersReady) return;
            listenersReady = true;

            if (geriButton != null) geriButton.onClick.AddListener(GeriDon);
            if (tabGenel != null) tabGenel.onClick.AddListener(() => TabSec(0));
            if (tabDersler != null) tabDersler.onClick.AddListener(() => TabSec(1));
            if (tabHaftalik != null) tabHaftalik.onClick.AddListener(() => TabSec(2));
            if (tabGunluk != null) tabGunluk.onClick.AddListener(() => TabSec(3));
            if (tabTrendler != null) tabTrendler.onClick.AddListener(() => TabSec(4));
        }

        private void OnEnable()
        {
            EnsureInit();
        }

        // ================================================
        //  TAB SISTEMI
        // ================================================

        private void TabSec(int idx)
        {
            aktifTab = idx;

            if (genelPanel != null) genelPanel.SetActive(idx == 0);
            if (derslerPanel != null) derslerPanel.SetActive(idx == 1);
            if (haftalikPanel != null) haftalikPanel.SetActive(idx == 2);
            if (gunlukPanel != null) gunlukPanel.SetActive(idx == 3);
            if (trendlerPanel != null) trendlerPanel.SetActive(idx == 4);

            TabRenginiAyarla(tabGenel, idx == 0);
            TabRenginiAyarla(tabDersler, idx == 1);
            TabRenginiAyarla(tabHaftalik, idx == 2);
            TabRenginiAyarla(tabGunluk, idx == 3);
            TabRenginiAyarla(tabTrendler, idx == 4);

            switch (idx)
            {
                case 0: GenelOzetDoldur(); break;
                case 1: DerslerDoldur(); break;
                case 2: HaftalikDoldur(); break;
                case 3: GunlukDoldur(); break;
                case 4: TrendlerDoldur(); break;
            }
        }

        private void TabRenginiAyarla(Button tab, bool aktif)
        {
            if (tab == null) return;
            var img = tab.GetComponent<Image>();
            if (img != null) img.color = aktif ? tabAktifRenk : tabPasifRenk;
            var tmp = tab.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.color = aktif ? Color.white : new Color(0.7f, 0.7f, 0.7f);
                tmp.fontStyle = aktif ? FontStyles.Bold : FontStyles.Normal;
            }
        }

        // ================================================
        //  TAB 0 - GENEL OZET
        // ================================================

        private void GenelOzetDoldur()
        {
            ContentTemizle(genelContent);
            if (genelContent == null) return;

            if (mevcutPerformans == null || mevcutPerformans.tumCevaplar.Count == 0)
            {
                BosVeriUyarisi(genelContent, "Bu ogrenci henuz oyun oynamamis.");
                return;
            }

            var perf = mevcutPerformans;
            float basari = perf.GenelBasariYuzdesi();
            int dogru = perf.tumCevaplar.Count(c => c.dogruMu);
            int yanlis = perf.tumCevaplar.Count(c => !c.dogruMu);

            // Basari cubugu
            RuntimeGraphRenderer.IlerlemeCubuguOlustur(genelContent,
                "Genel Basari", basari, 100f,
                basari >= 70 ? new Color(0.3f, 0.85f, 0.3f) : basari >= 40 ? new Color(1f, 0.75f, 0.2f) : new Color(0.9f, 0.3f, 0.3f),
                42);

            Spacer(genelContent, 12);

            // Ozet kartlar
            GameObject kartRow = SatirOlustur(genelContent);
            RuntimeGraphRenderer.EtiketliKartOlustur(kartRow.transform,
                "Toplam Soru", perf.tumCevaplar.Count.ToString(), new Color(0.4f, 0.7f, 1f), 140, 75);
            RuntimeGraphRenderer.EtiketliKartOlustur(kartRow.transform,
                "Dogru", dogru.ToString(), new Color(0.3f, 0.85f, 0.3f), 140, 75);
            RuntimeGraphRenderer.EtiketliKartOlustur(kartRow.transform,
                "Yanlis", yanlis.ToString(), new Color(0.9f, 0.3f, 0.3f), 140, 75);
            RuntimeGraphRenderer.EtiketliKartOlustur(kartRow.transform,
                "Ort. Sure", perf.OrtCevapSuresi().ToString("F1") + "s", new Color(0.9f, 0.7f, 0.2f), 140, 75);
            RuntimeGraphRenderer.EtiketliKartOlustur(kartRow.transform,
                "Oyun Sayisi", perf.ToplamOyunSayisi().ToString(), new Color(0.6f, 0.5f, 0.9f), 140, 75);

            Spacer(genelContent, 16);

            // Ders bazli kisa cubuklar
            Baslik(genelContent, "Ders Bazli Basari");
            var dersIst = perf.TumDersIstatistikleri();
            foreach (var ist in dersIst)
            {
                int dI = (int)ist.ders;
                Color renk = RuntimeGraphRenderer.DersRenkleri[dI % RuntimeGraphRenderer.DersRenkleri.Length];
                RuntimeGraphRenderer.IlerlemeCubuguOlustur(genelContent,
                    RuntimeGraphRenderer.DersAdi(ist.ders) + " (" + ist.toplamSoru + " soru)",
                    ist.basariYuzdesi, 100f, renk, 32);
            }

            Spacer(genelContent, 16);

            // Tekrar yanlislar
            var tekrarlar = perf.TekrarYanlislariBul();
            if (tekrarlar.Count > 0)
            {
                Baslik(genelContent, "Tekrar Edilen Yanlislar (" + tekrarlar.Count + ")");
                int gosterilen = Mathf.Min(tekrarlar.Count, 5);
                for (int i = 0; i < gosterilen; i++)
                {
                    var t = tekrarlar[i];
                    string durum = t.ogrendiMi ? " >> Ogrendi" : (t.sonDenemeDogru ? " -> Son dogru" : " X Devam");
                    Color durumRenk = t.ogrendiMi ? new Color(0.3f, 0.85f, 0.3f) :
                                      t.sonDenemeDogru ? new Color(1f, 0.8f, 0.2f) : new Color(0.9f, 0.3f, 0.3f);

                    var satir = SatirOlustur(genelContent);
                    string soruKisa = t.soruMetni.Length > 40 ? t.soruMetni.Substring(0, 40) + "..." : t.soruMetni;
                    RuntimeGraphRenderer.TMPOlustur(satir.transform,
                        "- " + soruKisa,
                        13, Color.white, TextAlignmentOptions.Left, 24);
                    RuntimeGraphRenderer.TMPOlustur(satir.transform,
                        t.yanlisSayisi + "x yanlis" + durum,
                        12, durumRenk, TextAlignmentOptions.Right, 24, 180);
                }
            }
        }

        // ================================================
        //  TAB 1 - DERS DETAYLARI
        // ================================================

        private void DerslerDoldur()
        {
            ContentTemizle(derslerContent);
            if (derslerContent == null) return;

            if (mevcutPerformans == null || mevcutPerformans.tumCevaplar.Count == 0)
            {
                BosVeriUyarisi(derslerContent, "Henuz veri yok.");
                return;
            }

            var dersListesi = mevcutPerformans.TumDersIstatistikleri();
            if (dersListesi.Count == 0)
            {
                BosVeriUyarisi(derslerContent, "Hicbir ders icin veri bulunamadi.");
                return;
            }

            foreach (var ist in dersListesi)
            {
                int dI = (int)ist.ders;
                Color dersRenk = RuntimeGraphRenderer.DersRenkleri[dI % RuntimeGraphRenderer.DersRenkleri.Length];

                // Ders basligi
                Baslik(derslerContent, RuntimeGraphRenderer.DersAdi(ist.ders), dersRenk);

                // Basari cubugu
                RuntimeGraphRenderer.IlerlemeCubuguOlustur(derslerContent,
                    "Basari: %" + ist.basariYuzdesi.ToString("F0"), ist.basariYuzdesi, 100f, dersRenk, 30);

                // Zorluk segmentli cubuk
                int toplamZor = ist.kolayDogru + ist.kolayYanlis + ist.ortaDogru + ist.ortaYanlis + ist.zorDogru + ist.zorYanlis;
                if (toplamZor > 0)
                {
                    var segmentler = new (float deger, Color renk, string etiket)[]
                    {
                        (ist.kolayDogru + ist.kolayYanlis, new Color(0.4f, 0.9f, 0.4f),
                         "Kolay: " + ist.kolayDogru + "/" + (ist.kolayDogru + ist.kolayYanlis)),
                        (ist.ortaDogru + ist.ortaYanlis, new Color(1f, 0.8f, 0.2f),
                         "Orta: " + ist.ortaDogru + "/" + (ist.ortaDogru + ist.ortaYanlis)),
                        (ist.zorDogru + ist.zorYanlis, new Color(1f, 0.3f, 0.3f),
                         "Zor: " + ist.zorDogru + "/" + (ist.zorDogru + ist.zorYanlis))
                    };
                    RuntimeGraphRenderer.SegmentliCubukOlustur(derslerContent, segmentler, toplamZor, 26);
                }

                // Kisa istatistikler
                var infoRow = SatirOlustur(derslerContent);
                RuntimeGraphRenderer.EtiketliKartOlustur(infoRow.transform,
                    "Dogru", ist.dogruSayisi.ToString(), new Color(0.3f, 0.85f, 0.3f), 100, 55);
                RuntimeGraphRenderer.EtiketliKartOlustur(infoRow.transform,
                    "Yanlis", ist.yanlisSayisi.ToString(), new Color(0.9f, 0.3f, 0.3f), 100, 55);
                RuntimeGraphRenderer.EtiketliKartOlustur(infoRow.transform,
                    "Toplam", ist.toplamSoru.ToString(), new Color(0.7f, 0.7f, 0.7f), 100, 55);
                RuntimeGraphRenderer.EtiketliKartOlustur(infoRow.transform,
                    "Ort.Sure", ist.ortalamaSure.ToString("F1") + "s", new Color(0.9f, 0.7f, 0.2f), 100, 55);

                Spacer(derslerContent, 14);
            }
        }

        // ================================================
        //  TAB 2 - HAFTALIK
        // ================================================

        private void HaftalikDoldur()
        {
            ContentTemizle(haftalikContent);
            if (haftalikContent == null) return;

            if (mevcutPerformans == null || mevcutPerformans.tumCevaplar.Count == 0)
            {
                BosVeriUyarisi(haftalikContent, "Henuz veri yok.");
                return;
            }

            var haftalar = RaporHesaplayici.HaftalikRaporlarOlustur(mevcutPerformans, 8);
            if (haftalar.Count == 0)
            {
                BosVeriUyarisi(haftalikContent, "Haftalik rapor olusturulamadi.");
                return;
            }

            Baslik(haftalikContent, "Haftalik Basari Grafigi");

            // Cubuk grafik - haftalik basari
            var cubukVeriler = new List<(string etiket, float deger, Color renk)>();
            foreach (var h in haftalar)
            {
                Color renk = h.basariYuzdesi >= 70 ? new Color(0.3f, 0.85f, 0.3f) :
                             h.basariYuzdesi >= 40 ? new Color(1f, 0.75f, 0.2f) :
                             new Color(0.9f, 0.3f, 0.3f);
                cubukVeriler.Add((h.haftaEtiketi, h.basariYuzdesi, renk));
            }
            RuntimeGraphRenderer.YatayCubukGrafikOlustur(haftalikContent, cubukVeriler, 100f);

            Spacer(haftalikContent, 16);

            // Hafta karsilastirma
            var karsilastirma = RaporHesaplayici.HaftaKarsilastirmasiOlustur(mevcutPerformans);
            if (karsilastirma != null)
            {
                Baslik(haftalikContent, "Bu Hafta vs Onceki Hafta");

                string ok = karsilastirma.basariFarki >= 0 ? "+" : "-";
                Color farkRenk = karsilastirma.basariFarki >= 0 ? new Color(0.3f, 0.85f, 0.3f) : new Color(0.9f, 0.3f, 0.3f);

                var satirK = SatirOlustur(haftalikContent);
                RuntimeGraphRenderer.EtiketliKartOlustur(satirK.transform,
                    "Onceki Hafta", "%" + karsilastirma.oncekiHafta.basariYuzdesi.ToString("F0"),
                    new Color(0.5f, 0.5f, 0.6f), 150, 65);
                RuntimeGraphRenderer.EtiketliKartOlustur(satirK.transform,
                    "Bu Hafta", "%" + karsilastirma.buHafta.basariYuzdesi.ToString("F0"),
                    new Color(0.3f, 0.6f, 0.9f), 150, 65);
                RuntimeGraphRenderer.EtiketliKartOlustur(satirK.transform,
                    "Degisim", ok + Mathf.Abs(karsilastirma.basariFarki).ToString("F1") + "%",
                    farkRenk, 150, 65);

                Spacer(haftalikContent, 10);

                // Ders bazli farklar
                if (karsilastirma.dersBazliFark.Count > 0)
                {
                    Baslik(haftalikContent, "Ders Bazli Degisim");
                    foreach (var kv in karsilastirma.dersBazliFark)
                    {
                        string dersOk = kv.Value >= 0 ? "+" : "-";
                        Color c = kv.Value >= 0 ? new Color(0.3f, 0.85f, 0.3f) : new Color(0.9f, 0.3f, 0.3f);
                        var row = SatirOlustur(haftalikContent);
                        RuntimeGraphRenderer.TMPOlustur(row.transform,
                            RuntimeGraphRenderer.DersAdi(kv.Key), 14, Color.white,
                            TextAlignmentOptions.Left, 28);
                        RuntimeGraphRenderer.TMPOlustur(row.transform,
                            dersOk + Mathf.Abs(kv.Value).ToString("F1") + "%", 14, c,
                            TextAlignmentOptions.Right, 28, 100);
                    }
                }
            }

            Spacer(haftalikContent, 16);

            // Hafta detay tablosu
            Baslik(haftalikContent, "Hafta Detaylari");
            RuntimeGraphRenderer.TabloSatiriOlustur(haftalikContent,
                new string[] { "Hafta", "Soru", "Dogru", "Yanlis", "Basari", "Ort.Sure" },
                new float[] { 90, 55, 55, 55, 65, 65 },
                new Color(0.25f, 0.28f, 0.35f, 0.9f), true);

            foreach (var h in haftalar)
            {
                if (h.toplamSoru == 0) continue;
                RuntimeGraphRenderer.TabloSatiriOlustur(haftalikContent,
                    new string[]
                    {
                        h.haftaEtiketi,
                        h.toplamSoru.ToString(),
                        h.dogruSayisi.ToString(),
                        h.yanlisSayisi.ToString(),
                        "%" + h.basariYuzdesi.ToString("F0"),
                        h.ortSure.ToString("F1") + "s"
                    },
                    new float[] { 90, 55, 55, 55, 65, 65 },
                    new Color(0.16f, 0.18f, 0.22f, 0.7f), false);
            }
        }

        // ================================================
        //  TAB 3 - GUNLUK
        // ================================================

        private void GunlukDoldur()
        {
            ContentTemizle(gunlukContent);
            if (gunlukContent == null) return;

            if (mevcutPerformans == null || mevcutPerformans.tumCevaplar.Count == 0)
            {
                BosVeriUyarisi(gunlukContent, "Henuz veri yok.");
                return;
            }

            var gunler = RaporHesaplayici.GunlukRaporlarOlustur(mevcutPerformans,
                DateTime.Now.AddDays(-30), DateTime.Now);

            if (gunler.Count == 0)
            {
                BosVeriUyarisi(gunlukContent, "Son 30 gunde veri bulunamadi.");
                return;
            }

            Baslik(gunlukContent, "Son 30 Gun - Gunluk Ilerleme");

            // Gunluk cubuk grafik
            var cubukler = new List<(string etiket, float deger, Color renk)>();
            foreach (var g in gunler)
            {
                DateTime dt = DateTime.Parse(g.tarih);
                string etiket = dt.Day + "/" + dt.Month;
                Color renk = g.basariYuzdesi >= 70 ? new Color(0.3f, 0.85f, 0.3f) :
                             g.basariYuzdesi >= 40 ? new Color(1f, 0.75f, 0.2f) :
                             new Color(0.9f, 0.3f, 0.3f);
                cubukler.Add((etiket, g.basariYuzdesi, renk));
            }
            RuntimeGraphRenderer.YatayCubukGrafikOlustur(gunlukContent, cubukler, 100f);

            Spacer(gunlukContent, 16);

            // Gunluk tablo
            Baslik(gunlukContent, "Gun Detaylari");
            RuntimeGraphRenderer.TabloSatiriOlustur(gunlukContent,
                new string[] { "Tarih", "Soru", "Dogru", "Yanlis", "Basari", "Sure" },
                new float[] { 80, 50, 50, 50, 60, 55 },
                new Color(0.25f, 0.28f, 0.35f, 0.9f), true);

            // Son gunlerden itibaren goster (en yeni ustte)
            for (int i = gunler.Count - 1; i >= 0; i--)
            {
                var g = gunler[i];
                DateTime dt = DateTime.Parse(g.tarih);
                RuntimeGraphRenderer.TabloSatiriOlustur(gunlukContent,
                    new string[]
                    {
                        dt.Day + "/" + dt.Month,
                        g.toplamSoru.ToString(),
                        g.dogruSayisi.ToString(),
                        g.yanlisSayisi.ToString(),
                        "%" + g.basariYuzdesi.ToString("F0"),
                        g.ortSure.ToString("F1") + "s"
                    },
                    new float[] { 80, 50, 50, 50, 60, 55 },
                    new Color(0.16f, 0.18f, 0.22f, 0.7f), false);
            }

            Spacer(gunlukContent, 16);

            // Ders bazli gunluk ilerleme (son 7 gun)
            var son7Gun = gunler.Skip(Mathf.Max(0, gunler.Count - 7)).ToList();
            if (son7Gun.Count > 0)
            {
                Baslik(gunlukContent, "Son 7 Gun - Ders Bazli");
                foreach (DersKategorisi ders in Enum.GetValues(typeof(DersKategorisi)))
                {
                    var dersVerileri = son7Gun.Where(g => g.dersBazliBasari.ContainsKey(ders)).ToList();
                    if (dersVerileri.Count == 0) continue;

                    float ort = dersVerileri.Average(g => g.dersBazliBasari[ders]);
                    int dI = (int)ders;
                    Color c = RuntimeGraphRenderer.DersRenkleri[dI % RuntimeGraphRenderer.DersRenkleri.Length];
                    RuntimeGraphRenderer.IlerlemeCubuguOlustur(gunlukContent,
                        RuntimeGraphRenderer.DersAdi(ders) + ": %" + ort.ToString("F0"),
                        ort, 100f, c, 28);
                }
            }
        }

        // ================================================
        //  TAB 4 - TREND ANALIZI
        // ================================================

        private void TrendlerDoldur()
        {
            ContentTemizle(trendlerContent);
            if (trendlerContent == null) return;

            if (mevcutPerformans == null || mevcutPerformans.tumCevaplar.Count == 0)
            {
                BosVeriUyarisi(trendlerContent, "Henuz veri yok.");
                return;
            }

            Baslik(trendlerContent, "Ders Bazli Gelisim Trendi");

            foreach (DersKategorisi ders in Enum.GetValues(typeof(DersKategorisi)))
            {
                var trend = mevcutPerformans.GelisimTrendiHesapla(ders);
                if (trend.Count < 2) continue;

                int dI = (int)ders;
                Color dersRenk = RuntimeGraphRenderer.DersRenkleri[dI % RuntimeGraphRenderer.DersRenkleri.Length];

                Baslik(trendlerContent, RuntimeGraphRenderer.DersAdi(ders), dersRenk);

                // Trend cubuk grafik
                var cubukler = new List<(string etiket, float deger, Color renk)>();
                int gosterilecek = Mathf.Min(trend.Count, 14);
                for (int i = trend.Count - gosterilecek; i < trend.Count; i++)
                {
                    var g = trend[i];
                    DateTime dt = DateTime.Parse(g.tarih);
                    cubukler.Add((dt.Day + "/" + dt.Month, g.basariYuzdesi, dersRenk));
                }
                RuntimeGraphRenderer.YatayCubukGrafikOlustur(trendlerContent, cubukler, 100f);

                // Trend analizi
                if (trend.Count >= 3)
                {
                    float ilkYari = trend.Take(trend.Count / 2).Average(t => t.basariYuzdesi);
                    float sonYari = trend.Skip(trend.Count / 2).Average(t => t.basariYuzdesi);
                    float fark = sonYari - ilkYari;

                    string trendStr;
                    Color trendRenk;
                    if (fark > 5)
                    {
                        trendStr = ">> Yukseliste (+" + fark.ToString("F1") + "%)";
                        trendRenk = new Color(0.3f, 0.85f, 0.3f);
                    }
                    else if (fark < -5)
                    {
                        trendStr = "<< Dususte (" + fark.ToString("F1") + "%)";
                        trendRenk = new Color(0.9f, 0.3f, 0.3f);
                    }
                    else
                    {
                        trendStr = "= Stabil (" + (fark >= 0 ? "+" : "") + fark.ToString("F1") + "%)";
                        trendRenk = new Color(1f, 0.8f, 0.2f);
                    }

                    RuntimeGraphRenderer.TMPOlustur(trendlerContent,
                        trendStr, 15, trendRenk, TextAlignmentOptions.Left, 30);
                }

                Spacer(trendlerContent, 14);
            }

            // Genel trend
            Spacer(trendlerContent, 10);
            Baslik(trendlerContent, "Genel Degerlendirme");

            float genelBasari = mevcutPerformans.GenelBasariYuzdesi();
            int topSoru = mevcutPerformans.tumCevaplar.Count;
            int topOyun = mevcutPerformans.ToplamOyunSayisi();
            var tekrarlar = mevcutPerformans.TekrarYanlislariBul();
            int ogrenilenSayisi = tekrarlar.Count(t => t.ogrendiMi);

            string[] yorumlar = new string[]
            {
                "Toplam " + topSoru + " soru cevapladi, " + topOyun + " oyun oynadi.",
                "Genel basarisi %" + genelBasari.ToString("F0") + ".",
                tekrarlar.Count > 0
                    ? tekrarlar.Count + " konuda zorlandi, " + ogrenilenSayisi + " tanesini ogrendi."
                    : "Tekrarlayan hata bulunmadi.",
                genelBasari >= 80 ? "Cok iyi! Basarisini korumaya devam etmeli."
                    : genelBasari >= 60 ? "Iyi yolda. Zayif derslere odaklanmali."
                    : genelBasari >= 40 ? "Gelisime acik. Daha fazla pratik yapmali."
                    : "Acil mudahale gerek. Temelden baslanmali."
            };

            foreach (var yorum in yorumlar)
            {
                RuntimeGraphRenderer.TMPOlustur(trendlerContent,
                    yorum, 14, new Color(0.85f, 0.85f, 0.9f),
                    TextAlignmentOptions.Left, 26);
            }
        }

        // ================================================
        //  YARDIMCI
        // ================================================

        private void ContentTemizle(Transform content)
        {
            if (content == null) return;
            foreach (Transform child in content)
                Destroy(child.gameObject);
        }

        private void Baslik(Transform parent, string text, Color? renk = null)
        {
            RuntimeGraphRenderer.TMPOlustur(parent, text, 17,
                renk ?? new Color(0.9f, 0.9f, 0.95f),
                TextAlignmentOptions.Left, 32, -1, FontStyles.Bold);
        }

        private void BosVeriUyarisi(Transform parent, string mesaj)
        {
            Spacer(parent, 40);
            RuntimeGraphRenderer.TMPOlustur(parent, mesaj, 16,
                new Color(0.6f, 0.6f, 0.7f), TextAlignmentOptions.Center, 40);
        }

        private void Spacer(Transform parent, float yuk)
        {
            var sp = new GameObject("Spacer");
            sp.transform.SetParent(parent, false);
            var rt = sp.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, yuk);
            var le = sp.AddComponent<LayoutElement>();
            le.preferredHeight = yuk;
        }

        private GameObject SatirOlustur(Transform parent)
        {
            var row = new GameObject("Row");
            row.transform.SetParent(parent, false);
            row.AddComponent<RectTransform>();
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 80;
            var csf = row.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            return row;
        }

        private void GeriDon()
        {
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (geriButton != null) geriButton.onClick.RemoveAllListeners();
            if (tabGenel != null) tabGenel.onClick.RemoveAllListeners();
            if (tabDersler != null) tabDersler.onClick.RemoveAllListeners();
            if (tabHaftalik != null) tabHaftalik.onClick.RemoveAllListeners();
            if (tabGunluk != null) tabGunluk.onClick.RemoveAllListeners();
            if (tabTrendler != null) tabTrendler.onClick.RemoveAllListeners();
        }
    }
}
