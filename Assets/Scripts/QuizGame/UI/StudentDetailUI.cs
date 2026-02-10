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
    /// Runtime ogrenci detay paneli - Tab bazli prefab sistemi.
    /// Her tab icin tek bir icerik sablonu vardir (GenelIcerik, DerslerIcerik vb.).
    /// Inspector'dan sablonlari acarak tum UI elemanlarini ozellestirin:
    /// boyut, font, renk, spacing, padding vs.
    /// </summary>
    public class StudentDetailUI : MonoBehaviour
    {
        // ================================================================
        //  INSPECTOR ALANLARI
        // ================================================================

        [Header("=== Ust Bar ===")]
        [SerializeField] private TextMeshProUGUI baslikText;
        [SerializeField] private Button geriButton;

        [Header("=== Tab Butonlari ===")]
        [SerializeField] private Button tabGenel;
        [SerializeField] private Button tabDersler;
        [SerializeField] private Button tabHaftalik;
        [SerializeField] private Button tabGunluk;
        [SerializeField] private Button tabTrendler;

        [Header("=== Tab Panelleri ===")]
        [SerializeField] private GameObject genelPanel;
        [SerializeField] private GameObject derslerPanel;
        [SerializeField] private GameObject haftalikPanel;
        [SerializeField] private GameObject gunlukPanel;
        [SerializeField] private GameObject trendlerPanel;

        [Header("=== Scroll Content Alanlari ===")]
        [SerializeField] private Transform genelContent;
        [SerializeField] private Transform derslerContent;
        [SerializeField] private Transform haftalikContent;
        [SerializeField] private Transform gunlukContent;
        [SerializeField] private Transform trendlerContent;

        [Header("=== Tab Icerik Sablonlari (Hierarchy'den Duzenleyin) ===")]
        [Tooltip("Genel Ozet tabinin tam icerik sablonu. Bos birakirsaniz otomatik olusur.")]
        [SerializeField] private GameObject genelIcerikPrefab;

        [Tooltip("Ders Detaylari tabinin tam icerik sablonu. Bos birakirsaniz otomatik olusur.")]
        [SerializeField] private GameObject derslerIcerikPrefab;

        [Tooltip("Haftalik Rapor tabinin tam icerik sablonu. Bos birakirsaniz otomatik olusur.")]
        [SerializeField] private GameObject haftalikIcerikPrefab;

        [Tooltip("Gunluk Ilerleme tabinin tam icerik sablonu. Bos birakirsaniz otomatik olusur.")]
        [SerializeField] private GameObject gunlukIcerikPrefab;

        [Tooltip("Trend Analizi tabinin tam icerik sablonu. Bos birakirsaniz otomatik olusur.")]
        [SerializeField] private GameObject trendlerIcerikPrefab;

        // ================================================================
        //  STATE
        // ================================================================

        private StudentData mevcutOgrenci;
        private StudentPerformance mevcutPerformans;
        private int aktifTab;
        private bool listenersReady;

        private static readonly Color tabAktifRenk = new Color(0.3f, 0.6f, 0.9f, 1f);
        private static readonly Color tabPasifRenk = new Color(0.2f, 0.22f, 0.28f, 0.9f);

        // ================================================================
        //  PUBLIC API
        // ================================================================

        public void OgrenciGoster(StudentData ogrenci)
        {
            EnsureInit();
            mevcutOgrenci = ogrenci;
            gameObject.SetActive(true);

            if (baslikText != null)
                baslikText.text = ogrenci.TamAd + "  -  #" + ogrenci.ogrenciNo;

            if (DataManager.Instance != null)
                mevcutPerformans = DataManager.Instance.OgrenciPerformansiGetir(ogrenci.id);

            TabSec(0);
        }

        // ================================================================
        //  INIT
        // ================================================================

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

            // Sablonlar atanmamissa otomatik olustur (inactive child olarak)
            if (genelIcerikPrefab == null) genelIcerikPrefab = GenelSablonOlustur();
            if (derslerIcerikPrefab == null) derslerIcerikPrefab = DerslerSablonOlustur();
            if (haftalikIcerikPrefab == null) haftalikIcerikPrefab = HaftalikSablonOlustur();
            if (gunlukIcerikPrefab == null) gunlukIcerikPrefab = GunlukSablonOlustur();
            if (trendlerIcerikPrefab == null) trendlerIcerikPrefab = TrendlerSablonOlustur();
        }

        private void OnEnable() { EnsureInit(); }

        // ================================================================
        //  TAB SISTEMI
        // ================================================================

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

        // ================================================================
        //  TAB 0 — GENEL OZET
        // ================================================================

        private void GenelOzetDoldur()
        {
            ContentTemizle(genelContent);
            if (genelContent == null) return;

            if (mevcutPerformans == null || mevcutPerformans.tumCevaplar.Count == 0)
            { UyariGoster(genelContent, "Bu ogrenci henuz oyun oynamamis."); return; }

            var icerik = Instantiate(genelIcerikPrefab, genelContent);
            icerik.SetActive(true);
            icerik.name = "GenelIcerik";

            var perf = mevcutPerformans;
            float basari = perf.GenelBasariYuzdesi();
            int dogru = perf.tumCevaplar.Count(c => c.dogruMu);
            int yanlis = perf.tumCevaplar.Count(c => !c.dogruMu);

            // Basari cubugu
            CubuguGuncelle(icerik.transform.Find("BasariCubugu"),
                "Genel Basari", basari, 100f, RuntimeGraphRenderer.BasariRengi(basari));

            // Istatistik kartlari
            var kartS = icerik.transform.Find("KartSatiri");
            if (kartS != null)
            {
                KartGuncelle(kartS.Find("Kart_ToplamSoru"),
                    perf.tumCevaplar.Count.ToString(), "Toplam Soru", new Color(0.4f, 0.7f, 1f));
                KartGuncelle(kartS.Find("Kart_Dogru"),
                    dogru.ToString(), "Dogru", new Color(0.3f, 0.85f, 0.3f));
                KartGuncelle(kartS.Find("Kart_Yanlis"),
                    yanlis.ToString(), "Yanlis", new Color(0.9f, 0.3f, 0.3f));
                KartGuncelle(kartS.Find("Kart_OrtSure"),
                    perf.OrtCevapSuresi().ToString("F1") + "s", "Ort. Sure",
                    new Color(0.9f, 0.7f, 0.2f));
                KartGuncelle(kartS.Find("Kart_OyunSayisi"),
                    perf.ToplamOyunSayisi().ToString(), "Oyun Sayisi",
                    new Color(0.6f, 0.5f, 0.9f));
            }

            // Ders bazli basari (dinamik liste)
            var dersIst = perf.TumDersIstatistikleri();
            var dersBaslik = icerik.transform.Find("DersBazliBaslik");
            var dersListe = icerik.transform.Find("DersBazliListe");

            if (dersIst.Count > 0 && dersListe != null)
            {
                MetniAyarla(dersBaslik, "Ders Bazli Basari");
                var sablon = dersListe.Find("DersCubugu_Sablon");
                if (sablon != null)
                {
                    foreach (var ist in dersIst)
                    {
                        int dI = (int)ist.ders;
                        Color renk = RuntimeGraphRenderer.DersRenkleri[
                            dI % RuntimeGraphRenderer.DersRenkleri.Length];
                        var item = Instantiate(sablon.gameObject, dersListe);
                        item.SetActive(true);
                        item.name = "DersCubugu_" + ist.ders;
                        CubuguGuncelle(item.transform,
                            RuntimeGraphRenderer.DersAdi(ist.ders) +
                            " (" + ist.toplamSoru + " soru)",
                            ist.basariYuzdesi, 100f, renk);
                    }
                }
            }
            else
            {
                Gizle(dersBaslik);
                Gizle(dersListe);
            }

            // Tekrar yanlislar (dinamik liste)
            var tekrarlar = perf.TekrarYanlislariBul();
            var tekBaslik = icerik.transform.Find("TekrarYanlislarBaslik");
            var tekListe = icerik.transform.Find("TekrarYanlislarListe");

            if (tekrarlar.Count > 0 && tekListe != null)
            {
                MetniAyarla(tekBaslik,
                    "Tekrar Edilen Yanlislar (" + tekrarlar.Count + ")");
                var sablon = tekListe.Find("TekrarSatir_Sablon");
                if (sablon != null)
                {
                    int gosterilen = Mathf.Min(tekrarlar.Count, 5);
                    for (int i = 0; i < gosterilen; i++)
                    {
                        var t = tekrarlar[i];
                        var item = Instantiate(sablon.gameObject, tekListe);
                        item.SetActive(true);
                        item.name = "TekrarSatir_" + i;

                        string soruKisa = t.soruMetni.Length > 40
                            ? t.soruMetni.Substring(0, 40) + "..." : t.soruMetni;
                        string durum = t.ogrendiMi ? " >> Ogrendi"
                            : (t.sonDenemeDogru ? " -> Son dogru" : " X Devam");
                        Color durumRenk = t.ogrendiMi ? new Color(0.3f, 0.85f, 0.3f)
                            : t.sonDenemeDogru ? new Color(1f, 0.8f, 0.2f)
                            : new Color(0.9f, 0.3f, 0.3f);

                        MetniAyarla(item.transform.Find("SoruText"), "- " + soruKisa);
                        MetniAyarla(item.transform.Find("DurumText"),
                            t.yanlisSayisi + "x yanlis" + durum, durumRenk);
                    }
                }
            }
            else
            {
                Gizle(tekBaslik);
                Gizle(tekListe);
            }
        }

        // ================================================================
        //  TAB 1 — DERS DETAYLARI
        // ================================================================

        private void DerslerDoldur()
        {
            ContentTemizle(derslerContent);
            if (derslerContent == null) return;

            if (mevcutPerformans == null || mevcutPerformans.tumCevaplar.Count == 0)
            { UyariGoster(derslerContent, "Henuz veri yok."); return; }

            var dersListesi = mevcutPerformans.TumDersIstatistikleri();
            if (dersListesi.Count == 0)
            { UyariGoster(derslerContent, "Hicbir ders icin veri bulunamadi."); return; }

            var icerik = Instantiate(derslerIcerikPrefab, derslerContent);
            icerik.SetActive(true);
            icerik.name = "DerslerIcerik";

            var sablon = icerik.transform.Find("DersDetay_Sablon");
            if (sablon == null) return;

            foreach (var ist in dersListesi)
            {
                int dI = (int)ist.ders;
                Color dersRenk = RuntimeGraphRenderer.DersRenkleri[
                    dI % RuntimeGraphRenderer.DersRenkleri.Length];

                var blok = Instantiate(sablon.gameObject, icerik.transform);
                blok.SetActive(true);
                blok.name = "DersDetay_" + ist.ders;

                // Ders basligi
                MetniAyarla(blok.transform.Find("DersBaslik"),
                    RuntimeGraphRenderer.DersAdi(ist.ders), dersRenk);

                // Basari cubugu
                CubuguGuncelle(blok.transform.Find("BasariCubugu"),
                    "Basari: %" + ist.basariYuzdesi.ToString("F0"),
                    ist.basariYuzdesi, 100f, dersRenk);

                // Zorluk segmentleri
                int toplamZor = ist.kolayDogru + ist.kolayYanlis
                    + ist.ortaDogru + ist.ortaYanlis
                    + ist.zorDogru + ist.zorYanlis;
                var zorluk = blok.transform.Find("ZorlukCubugu");
                if (zorluk != null && toplamZor > 0)
                {
                    SegmentGuncelle(zorluk.Find("Kolay_Segment"),
                        ist.kolayDogru + ist.kolayYanlis, toplamZor,
                        new Color(0.4f, 0.9f, 0.4f),
                        "Kolay: " + ist.kolayDogru + "/" + (ist.kolayDogru + ist.kolayYanlis));
                    SegmentGuncelle(zorluk.Find("Orta_Segment"),
                        ist.ortaDogru + ist.ortaYanlis, toplamZor,
                        new Color(1f, 0.8f, 0.2f),
                        "Orta: " + ist.ortaDogru + "/" + (ist.ortaDogru + ist.ortaYanlis));
                    SegmentGuncelle(zorluk.Find("Zor_Segment"),
                        ist.zorDogru + ist.zorYanlis, toplamZor,
                        new Color(1f, 0.3f, 0.3f),
                        "Zor: " + ist.zorDogru + "/" + (ist.zorDogru + ist.zorYanlis));
                }
                else
                {
                    Gizle(zorluk);
                }

                // Bilgi kartlari
                var bilgiS = blok.transform.Find("BilgiSatiri");
                if (bilgiS != null)
                {
                    KartGuncelle(bilgiS.Find("Kart_Dogru"),
                        ist.dogruSayisi.ToString(), "Dogru",
                        new Color(0.3f, 0.85f, 0.3f));
                    KartGuncelle(bilgiS.Find("Kart_Yanlis"),
                        ist.yanlisSayisi.ToString(), "Yanlis",
                        new Color(0.9f, 0.3f, 0.3f));
                    KartGuncelle(bilgiS.Find("Kart_Toplam"),
                        ist.toplamSoru.ToString(), "Toplam",
                        new Color(0.7f, 0.7f, 0.7f));
                    KartGuncelle(bilgiS.Find("Kart_OrtSure"),
                        ist.ortalamaSure.ToString("F1") + "s", "Ort.Sure",
                        new Color(0.9f, 0.7f, 0.2f));
                }
            }
        }

        // ================================================================
        //  TAB 2 — HAFTALIK RAPOR
        // ================================================================

        private void HaftalikDoldur()
        {
            ContentTemizle(haftalikContent);
            if (haftalikContent == null) return;

            if (mevcutPerformans == null || mevcutPerformans.tumCevaplar.Count == 0)
            { UyariGoster(haftalikContent, "Henuz veri yok."); return; }

            var haftalar = RaporHesaplayici.HaftalikRaporlarOlustur(mevcutPerformans, 8);
            if (haftalar.Count == 0)
            { UyariGoster(haftalikContent, "Haftalik rapor olusturulamadi."); return; }

            var icerik = Instantiate(haftalikIcerikPrefab, haftalikContent);
            icerik.SetActive(true);
            icerik.name = "HaftalikIcerik";

            // ---------- Grafik ----------
            var grafikListe = icerik.transform.Find("GrafikListe");
            var grafikSablon = grafikListe?.Find("GrafikCubugu_Sablon");
            if (grafikSablon != null)
            {
                foreach (var h in haftalar)
                {
                    if (h.toplamSoru == 0) continue;
                    var item = Instantiate(grafikSablon.gameObject, grafikListe);
                    item.SetActive(true);
                    item.name = "GrafikCubugu";
                    CubuguGuncelle(item.transform,
                        h.haftaEtiketi, h.basariYuzdesi, 100f,
                        RuntimeGraphRenderer.BasariRengi(h.basariYuzdesi));
                }
            }

            // ---------- Karsilastirma ----------
            var karsilastirma = RaporHesaplayici.HaftaKarsilastirmasiOlustur(mevcutPerformans);
            var kBaslik = icerik.transform.Find("KarsilastirmaBaslik");
            var kSatir = icerik.transform.Find("KarsilastirmaSatiri");

            if (karsilastirma != null && kSatir != null)
            {
                MetniAyarla(kBaslik, "Bu Hafta vs Onceki Hafta");

                string ok = karsilastirma.basariFarki >= 0 ? "+" : "";
                Color farkRenk = karsilastirma.basariFarki >= 0
                    ? new Color(0.3f, 0.85f, 0.3f) : new Color(0.9f, 0.3f, 0.3f);

                KartGuncelle(kSatir.Find("Kart_OncekiHafta"),
                    "%" + karsilastirma.oncekiHafta.basariYuzdesi.ToString("F0"),
                    "Onceki Hafta", new Color(0.5f, 0.5f, 0.6f));
                KartGuncelle(kSatir.Find("Kart_BuHafta"),
                    "%" + karsilastirma.buHafta.basariYuzdesi.ToString("F0"),
                    "Bu Hafta", new Color(0.3f, 0.6f, 0.9f));
                KartGuncelle(kSatir.Find("Kart_Degisim"),
                    ok + Mathf.Abs(karsilastirma.basariFarki).ToString("F1") + "%",
                    "Degisim", farkRenk);

                // Ders bazli degisim
                var ddBaslik = icerik.transform.Find("DersDegisimBaslik");
                var ddListe = icerik.transform.Find("DersDegisimListe");

                if (karsilastirma.dersBazliFark.Count > 0 && ddListe != null)
                {
                    MetniAyarla(ddBaslik, "Ders Bazli Degisim");
                    var ddSablon = ddListe.Find("DersDegisim_Sablon");
                    if (ddSablon != null)
                    {
                        foreach (var kv in karsilastirma.dersBazliFark)
                        {
                            string dOk = kv.Value >= 0 ? "+" : "";
                            Color c = kv.Value >= 0
                                ? new Color(0.3f, 0.85f, 0.3f)
                                : new Color(0.9f, 0.3f, 0.3f);

                            var item = Instantiate(ddSablon.gameObject, ddListe);
                            item.SetActive(true);
                            item.name = "DersDegisim_" + kv.Key;

                            MetniAyarla(item.transform.Find("DersAdiText"),
                                RuntimeGraphRenderer.DersAdi(kv.Key));
                            MetniAyarla(item.transform.Find("FarkText"),
                                dOk + kv.Value.ToString("F1") + "%", c);
                        }
                    }
                }
                else
                {
                    Gizle(ddBaslik);
                    Gizle(ddListe);
                }
            }
            else
            {
                Gizle(kBaslik);
                Gizle(kSatir);
                Gizle(icerik.transform.Find("DersDegisimBaslik"));
                Gizle(icerik.transform.Find("DersDegisimListe"));
            }

            // ---------- Tablo ----------
            MetniAyarla(icerik.transform.Find("DetayBaslik"), "Hafta Detaylari");

            // Baslik satiri
            TabloSatiriGuncelle(icerik.transform.Find("TabloBaslik"),
                new[] { "Hafta", "Soru", "Dogru", "Yanlis", "Basari", "Ort.Sure" },
                new Color(0.25f, 0.28f, 0.35f, 0.9f), true);

            // Veri satirlari
            var tabloListe = icerik.transform.Find("TabloListe");
            var tabloSablon = tabloListe?.Find("TabloSatir_Sablon");
            if (tabloSablon != null)
            {
                foreach (var h in haftalar)
                {
                    if (h.toplamSoru == 0) continue;
                    var item = Instantiate(tabloSablon.gameObject, tabloListe);
                    item.SetActive(true);
                    item.name = "TabloSatir";
                    TabloSatiriGuncelle(item.transform,
                        new[]
                        {
                            h.haftaEtiketi,
                            h.toplamSoru.ToString(),
                            h.dogruSayisi.ToString(),
                            h.yanlisSayisi.ToString(),
                            "%" + h.basariYuzdesi.ToString("F0"),
                            h.ortSure.ToString("F1") + "s"
                        },
                        new Color(0.16f, 0.18f, 0.22f, 0.7f), false);
                }
            }
        }

        // ================================================================
        //  TAB 3 — GUNLUK ILERLEME
        // ================================================================

        private void GunlukDoldur()
        {
            ContentTemizle(gunlukContent);
            if (gunlukContent == null) return;

            if (mevcutPerformans == null || mevcutPerformans.tumCevaplar.Count == 0)
            { UyariGoster(gunlukContent, "Henuz veri yok."); return; }

            var gunler = RaporHesaplayici.GunlukRaporlarOlustur(
                mevcutPerformans, DateTime.Now.AddDays(-30), DateTime.Now);
            if (gunler.Count == 0)
            { UyariGoster(gunlukContent, "Son 30 gunde veri bulunamadi."); return; }

            var icerik = Instantiate(gunlukIcerikPrefab, gunlukContent);
            icerik.SetActive(true);
            icerik.name = "GunlukIcerik";

            // ---------- Grafik ----------
            MetniAyarla(icerik.transform.Find("GrafikBaslik"),
                "Son 30 Gun - Gunluk Ilerleme");

            var grafikListe = icerik.transform.Find("GrafikListe");
            var grafikSablon = grafikListe?.Find("GrafikCubugu_Sablon");
            if (grafikSablon != null)
            {
                foreach (var g in gunler)
                {
                    DateTime dt = DateTime.Parse(g.tarih);
                    var item = Instantiate(grafikSablon.gameObject, grafikListe);
                    item.SetActive(true);
                    item.name = "GrafikCubugu";
                    CubuguGuncelle(item.transform,
                        dt.Day + "/" + dt.Month,
                        g.basariYuzdesi, 100f,
                        RuntimeGraphRenderer.BasariRengi(g.basariYuzdesi));
                }
            }

            // ---------- Tablo ----------
            MetniAyarla(icerik.transform.Find("DetayBaslik"), "Gun Detaylari");

            TabloSatiriGuncelle(icerik.transform.Find("TabloBaslik"),
                new[] { "Tarih", "Soru", "Dogru", "Yanlis", "Basari", "Sure" },
                new Color(0.25f, 0.28f, 0.35f, 0.9f), true);

            var tabloListe = icerik.transform.Find("TabloListe");
            var tabloSablon = tabloListe?.Find("TabloSatir_Sablon");
            if (tabloSablon != null)
            {
                for (int i = gunler.Count - 1; i >= 0; i--)
                {
                    var g = gunler[i];
                    DateTime dt = DateTime.Parse(g.tarih);
                    var item = Instantiate(tabloSablon.gameObject, tabloListe);
                    item.SetActive(true);
                    item.name = "TabloSatir";
                    TabloSatiriGuncelle(item.transform,
                        new[]
                        {
                            dt.Day + "/" + dt.Month,
                            g.toplamSoru.ToString(),
                            g.dogruSayisi.ToString(),
                            g.yanlisSayisi.ToString(),
                            "%" + g.basariYuzdesi.ToString("F0"),
                            g.ortSure.ToString("F1") + "s"
                        },
                        new Color(0.16f, 0.18f, 0.22f, 0.7f), false);
                }
            }

            // ---------- Son 7 Gun Ders Bazli ----------
            var son7Gun = gunler.Skip(Mathf.Max(0, gunler.Count - 7)).ToList();
            var s7Baslik = icerik.transform.Find("Son7GunBaslik");
            var s7Liste = icerik.transform.Find("Son7GunListe");

            if (son7Gun.Count > 0 && s7Liste != null)
            {
                MetniAyarla(s7Baslik, "Son 7 Gun - Ders Bazli");
                var s7Sablon = s7Liste.Find("DersCubugu_Sablon");
                if (s7Sablon != null)
                {
                    foreach (DersKategorisi ders in Enum.GetValues(typeof(DersKategorisi)))
                    {
                        var dVeri = son7Gun
                            .Where(g => g.dersBazliBasari.ContainsKey(ders)).ToList();
                        if (dVeri.Count == 0) continue;

                        float ort = dVeri.Average(g => g.dersBazliBasari[ders]);
                        int dI = (int)ders;
                        Color c = RuntimeGraphRenderer.DersRenkleri[
                            dI % RuntimeGraphRenderer.DersRenkleri.Length];

                        var item = Instantiate(s7Sablon.gameObject, s7Liste);
                        item.SetActive(true);
                        item.name = "DersCubugu_" + ders;
                        CubuguGuncelle(item.transform,
                            RuntimeGraphRenderer.DersAdi(ders) + ": %" + ort.ToString("F0"),
                            ort, 100f, c);
                    }
                }
            }
            else
            {
                Gizle(s7Baslik);
                Gizle(s7Liste);
            }
        }

        // ================================================================
        //  TAB 4 — TREND ANALIZI
        // ================================================================

        private void TrendlerDoldur()
        {
            ContentTemizle(trendlerContent);
            if (trendlerContent == null) return;

            if (mevcutPerformans == null || mevcutPerformans.tumCevaplar.Count == 0)
            { UyariGoster(trendlerContent, "Henuz veri yok."); return; }

            var icerik = Instantiate(trendlerIcerikPrefab, trendlerContent);
            icerik.SetActive(true);
            icerik.name = "TrendlerIcerik";

            MetniAyarla(icerik.transform.Find("TrendBaslik"),
                "Ders Bazli Gelisim Trendi");

            // ---------- Ders trendleri (dinamik) ----------
            var trendListe = icerik.transform.Find("TrendListe");
            var trendSablon = trendListe?.Find("TrendBlok_Sablon");

            if (trendSablon != null)
            {
                foreach (DersKategorisi ders in Enum.GetValues(typeof(DersKategorisi)))
                {
                    var trend = mevcutPerformans.GelisimTrendiHesapla(ders);
                    if (trend.Count < 2) continue;

                    int dI = (int)ders;
                    Color dersRenk = RuntimeGraphRenderer.DersRenkleri[
                        dI % RuntimeGraphRenderer.DersRenkleri.Length];

                    var blok = Instantiate(trendSablon.gameObject, trendListe);
                    blok.SetActive(true);
                    blok.name = "TrendBlok_" + ders;

                    // Ders basligi
                    MetniAyarla(blok.transform.Find("DersBaslik"),
                        RuntimeGraphRenderer.DersAdi(ders), dersRenk);

                    // Grafik cubuklari (ic sablon)
                    var gListe = blok.transform.Find("GrafikListe");
                    var cSablon = gListe?.Find("CubukSablon");
                    if (cSablon != null)
                    {
                        int gosterilecek = Mathf.Min(trend.Count, 14);
                        for (int i = trend.Count - gosterilecek; i < trend.Count; i++)
                        {
                            var g = trend[i];
                            DateTime dt = DateTime.Parse(g.tarih);
                            var cubuk = Instantiate(cSablon.gameObject, gListe);
                            cubuk.SetActive(true);
                            cubuk.name = "Cubuk_" + i;
                            CubuguGuncelle(cubuk.transform,
                                dt.Day + "/" + dt.Month,
                                g.basariYuzdesi, 100f, dersRenk);
                        }
                    }

                    // Trend yorumu
                    var yorumT = blok.transform.Find("TrendYorum");
                    if (yorumT != null && trend.Count >= 3)
                    {
                        float ilkYari = trend.Take(trend.Count / 2)
                            .Average(t => t.basariYuzdesi);
                        float sonYari = trend.Skip(trend.Count / 2)
                            .Average(t => t.basariYuzdesi);
                        float fark = sonYari - ilkYari;

                        string str; Color c;
                        if (fark > 5)
                        {
                            str = ">> Yukseliste (+" + fark.ToString("F1") + "%)";
                            c = new Color(0.3f, 0.85f, 0.3f);
                        }
                        else if (fark < -5)
                        {
                            str = "<< Dususte (" + fark.ToString("F1") + "%)";
                            c = new Color(0.9f, 0.3f, 0.3f);
                        }
                        else
                        {
                            str = "= Stabil (" + (fark >= 0 ? "+" : "")
                                + fark.ToString("F1") + "%)";
                            c = new Color(1f, 0.8f, 0.2f);
                        }
                        MetniAyarla(yorumT, str, c);
                    }
                }
            }

            // ---------- Genel degerlendirme ----------
            MetniAyarla(icerik.transform.Find("DegerlendirmeBaslik"),
                "Genel Degerlendirme");

            float genelBasari = mevcutPerformans.GenelBasariYuzdesi();
            int topSoru = mevcutPerformans.tumCevaplar.Count;
            int topOyun = mevcutPerformans.ToplamOyunSayisi();
            var sonTekrarlar = mevcutPerformans.TekrarYanlislariBul();
            int ogrenilenSayisi = sonTekrarlar.Count(t => t.ogrendiMi);

            string[] yorumlar =
            {
                "Toplam " + topSoru + " soru cevapladi, " + topOyun + " oyun oynadi.",
                "Genel basarisi %" + genelBasari.ToString("F0") + ".",
                sonTekrarlar.Count > 0
                    ? sonTekrarlar.Count + " konuda zorlandi, "
                      + ogrenilenSayisi + " tanesini ogrendi."
                    : "Tekrarlayan hata bulunmadi.",
                genelBasari >= 80 ? "Cok iyi! Basarisini korumaya devam etmeli."
                    : genelBasari >= 60 ? "Iyi yolda. Zayif derslere odaklanmali."
                    : genelBasari >= 40 ? "Gelisime acik. Daha fazla pratik yapmali."
                    : "Acil mudahale gerek. Temelden baslanmali."
            };

            var yorumListe = icerik.transform.Find("YorumListe");
            var yorumSablon = yorumListe?.Find("Yorum_Sablon");
            if (yorumSablon != null)
            {
                foreach (var y in yorumlar)
                {
                    var item = Instantiate(yorumSablon.gameObject, yorumListe);
                    item.SetActive(true);
                    item.name = "Yorum";
                    MetniAyarla(item.transform, y, new Color(0.85f, 0.85f, 0.9f));
                }
            }
        }

        // ================================================================
        //  GUNCELLEME YARDIMCILARI
        // ================================================================

        /// <summary>Ilerleme cubugunu gunceller (Fill anchor + renk + Text).</summary>
        private void CubuguGuncelle(Transform cubuk, string etiket,
            float deger, float maxDeger, Color renk)
        {
            if (cubuk == null) return;
            float yuzde = maxDeger > 0 ? Mathf.Clamp01(deger / maxDeger) : 0f;

            var fill = cubuk.Find("Fill");
            if (fill != null)
            {
                var frt = fill.GetComponent<RectTransform>();
                if (frt != null)
                {
                    frt.anchorMin = Vector2.zero;
                    frt.anchorMax = new Vector2(yuzde, 1f);
                    frt.offsetMin = Vector2.zero;
                    frt.offsetMax = Vector2.zero;
                }
                var fImg = fill.GetComponent<Image>();
                if (fImg != null) fImg.color = renk;
            }

            var txt = cubuk.Find("Text");
            if (txt != null)
            {
                var t = txt.GetComponent<TextMeshProUGUI>();
                if (t != null)
                    t.text = string.IsNullOrEmpty(etiket)
                        ? $"%{deger:F0}" : $"{etiket}  %{deger:F0}";
            }
        }

        /// <summary>Ozet kartini gunceller (DegerText + EtiketText).</summary>
        private void KartGuncelle(Transform kart, string deger,
            string etiket, Color degerRenk)
        {
            if (kart == null) return;
            var dT = kart.Find("DegerText")?.GetComponent<TextMeshProUGUI>();
            if (dT != null) { dT.text = deger; dT.color = degerRenk; }
            var eT = kart.Find("EtiketText")?.GetComponent<TextMeshProUGUI>();
            if (eT != null) eT.text = etiket;
        }

        /// <summary>Bir TMP metnini ayarlar (GO veya cocugundaki TMP).</summary>
        private void MetniAyarla(Transform go, string text, Color? renk = null)
        {
            if (go == null) return;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = go.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = text;
                if (renk.HasValue) tmp.color = renk.Value;
            }
        }

        /// <summary>Zorluk segment cubugunu gunceller.</summary>
        private void SegmentGuncelle(Transform seg, float deger,
            float toplam, Color renk, string text)
        {
            if (seg == null) return;
            float oran = toplam > 0 ? deger / toplam : 0f;
            seg.gameObject.SetActive(oran > 0);

            var le = seg.GetComponent<LayoutElement>();
            if (le != null) le.flexibleWidth = oran;
            var img = seg.GetComponent<Image>();
            if (img != null) img.color = renk;
            var tmp = seg.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = text;
        }

        /// <summary>Tablo satiri kolonlarini gunceller (Kolon0..Kolon5).</summary>
        private void TabloSatiriGuncelle(Transform satir, string[] kolonlar,
            Color bgRenk, bool baslikMi)
        {
            if (satir == null) return;
            var bgImg = satir.GetComponent<Image>();
            if (bgImg != null) bgImg.color = bgRenk;

            var le = satir.GetComponent<LayoutElement>();
            if (le != null) le.preferredHeight = baslikMi ? 32 : 28;

            for (int i = 0; i < kolonlar.Length && i < 6; i++)
            {
                var col = satir.Find("Kolon" + i);
                if (col == null) continue;
                var tmp = col.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = kolonlar[i];
                    tmp.fontStyle = baslikMi ? FontStyles.Bold : FontStyles.Normal;
                    tmp.color = baslikMi ? Color.white
                        : new Color(0.85f, 0.85f, 0.85f);
                }
            }
        }

        private void Gizle(Transform t)
        {
            if (t != null) t.gameObject.SetActive(false);
        }

        // ================================================================
        //  SABLON OLUŞTURMA — GENEL
        // ================================================================

        private GameObject GenelSablonOlustur()
        {
            var root = SablonKokuYap("GenelIcerik_Sablon");

            IlerlemeCubuguYap(root.transform, "BasariCubugu", 42);
            BoslukYap(root.transform, 12);

            var kartSatiri = YataySatirYap(root.transform, "KartSatiri", 80);
            OzetKartYap(kartSatiri.transform, "Kart_ToplamSoru", 120, 70);
            OzetKartYap(kartSatiri.transform, "Kart_Dogru", 120, 70);
            OzetKartYap(kartSatiri.transform, "Kart_Yanlis", 120, 70);
            OzetKartYap(kartSatiri.transform, "Kart_OrtSure", 120, 70);
            OzetKartYap(kartSatiri.transform, "Kart_OyunSayisi", 120, 70);

            BoslukYap(root.transform, 16);

            BaslikYap(root.transform, "DersBazliBaslik", "Ders Bazli Basari");
            var dersListe = DikeyListeYap(root.transform, "DersBazliListe");
            var dersSablon = IlerlemeCubuguYap(dersListe.transform,
                "DersCubugu_Sablon", 32);
            dersSablon.SetActive(false);

            BoslukYap(root.transform, 16);

            BaslikYap(root.transform, "TekrarYanlislarBaslik", "Tekrar Edilen Yanlislar");
            var tekrarListe = DikeyListeYap(root.transform, "TekrarYanlislarListe");
            var tekrarSablon = YataySatirYap(tekrarListe.transform,
                "TekrarSatir_Sablon", 26);
            tekrarSablon.SetActive(false);
            var soruT = MetinYap(tekrarSablon.transform, "SoruText", 13);
            soruT.GetComponent<LayoutElement>().flexibleWidth = 1;
            var durumT = MetinYap(tekrarSablon.transform, "DurumText", 12);
            var durumLE = durumT.GetComponent<LayoutElement>();
            durumLE.preferredWidth = 180;
            durumLE.flexibleWidth = -1;

            return root;
        }

        // ================================================================
        //  SABLON OLUŞTURMA — DERSLER
        // ================================================================

        private GameObject DerslerSablonOlustur()
        {
            var root = SablonKokuYap("DerslerIcerik_Sablon");

            // Ders detay sablonu (her ders icin klonlanir)
            var sablon = new GameObject("DersDetay_Sablon");
            sablon.SetActive(false);
            sablon.transform.SetParent(root.transform, false);
            sablon.AddComponent<RectTransform>();
            var sLE = sablon.AddComponent<LayoutElement>();
            sLE.flexibleWidth = 1;
            var sVLG = sablon.AddComponent<VerticalLayoutGroup>();
            sVLG.spacing = 4;
            sVLG.padding = new RectOffset(4, 4, 4, 4);
            sVLG.childForceExpandWidth = true;
            sVLG.childForceExpandHeight = false;
            sVLG.childControlHeight = true;
            sVLG.childControlWidth = true;
            var sCSF = sablon.AddComponent<ContentSizeFitter>();
            sCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            BaslikYap(sablon.transform, "DersBaslik", "Ders Adi");
            IlerlemeCubuguYap(sablon.transform, "BasariCubugu", 30);

            // Zorluk cubugu (3 segment)
            var zorluk = YataySatirYap(sablon.transform, "ZorlukCubugu", 26);
            zorluk.GetComponent<HorizontalLayoutGroup>().spacing = 1;
            ZorlukSegmendiYap(zorluk.transform, "Kolay_Segment",
                new Color(0.4f, 0.9f, 0.4f));
            ZorlukSegmendiYap(zorluk.transform, "Orta_Segment",
                new Color(1f, 0.8f, 0.2f));
            ZorlukSegmendiYap(zorluk.transform, "Zor_Segment",
                new Color(1f, 0.3f, 0.3f));

            // Bilgi kartlari
            var bilgiSatir = YataySatirYap(sablon.transform, "BilgiSatiri", 60);
            OzetKartYap(bilgiSatir.transform, "Kart_Dogru", 90, 55);
            OzetKartYap(bilgiSatir.transform, "Kart_Yanlis", 90, 55);
            OzetKartYap(bilgiSatir.transform, "Kart_Toplam", 90, 55);
            OzetKartYap(bilgiSatir.transform, "Kart_OrtSure", 90, 55);

            BoslukYap(sablon.transform, 10);

            return root;
        }

        // ================================================================
        //  SABLON OLUŞTURMA — HAFTALIK
        // ================================================================

        private GameObject HaftalikSablonOlustur()
        {
            var root = SablonKokuYap("HaftalikIcerik_Sablon");

            BaslikYap(root.transform, "GrafikBaslik", "Haftalik Basari Grafigi");
            var grafikListe = DikeyListeYap(root.transform, "GrafikListe");
            var gSablon = IlerlemeCubuguYap(grafikListe.transform,
                "GrafikCubugu_Sablon", 26);
            gSablon.SetActive(false);

            BoslukYap(root.transform, 16);

            BaslikYap(root.transform, "KarsilastirmaBaslik", "Bu Hafta vs Onceki Hafta");
            var kSatir = YataySatirYap(root.transform, "KarsilastirmaSatiri", 70);
            OzetKartYap(kSatir.transform, "Kart_OncekiHafta", 140, 65);
            OzetKartYap(kSatir.transform, "Kart_BuHafta", 140, 65);
            OzetKartYap(kSatir.transform, "Kart_Degisim", 140, 65);

            BoslukYap(root.transform, 10);

            BaslikYap(root.transform, "DersDegisimBaslik", "Ders Bazli Degisim");
            var ddListe = DikeyListeYap(root.transform, "DersDegisimListe");
            var ddSablon = YataySatirYap(ddListe.transform,
                "DersDegisim_Sablon", 28);
            ddSablon.SetActive(false);
            var adiT = MetinYap(ddSablon.transform, "DersAdiText", 14);
            adiT.GetComponent<LayoutElement>().flexibleWidth = 1;
            var farkT = MetinYap(ddSablon.transform, "FarkText", 14);
            var farkLE = farkT.GetComponent<LayoutElement>();
            farkLE.preferredWidth = 100;
            farkLE.flexibleWidth = -1;

            BoslukYap(root.transform, 16);

            BaslikYap(root.transform, "DetayBaslik", "Hafta Detaylari");
            TabloSatirYap(root.transform, "TabloBaslik", 6, 32);
            var tabloListe = DikeyListeYap(root.transform, "TabloListe");
            var tSablon = TabloSatirYap(tabloListe.transform,
                "TabloSatir_Sablon", 6, 28);
            tSablon.SetActive(false);

            return root;
        }

        // ================================================================
        //  SABLON OLUŞTURMA — GUNLUK
        // ================================================================

        private GameObject GunlukSablonOlustur()
        {
            var root = SablonKokuYap("GunlukIcerik_Sablon");

            BaslikYap(root.transform, "GrafikBaslik", "Son 30 Gun - Gunluk Ilerleme");
            var grafikListe = DikeyListeYap(root.transform, "GrafikListe");
            var gSablon = IlerlemeCubuguYap(grafikListe.transform,
                "GrafikCubugu_Sablon", 26);
            gSablon.SetActive(false);

            BoslukYap(root.transform, 16);

            BaslikYap(root.transform, "DetayBaslik", "Gun Detaylari");
            TabloSatirYap(root.transform, "TabloBaslik", 6, 32);
            var tabloListe = DikeyListeYap(root.transform, "TabloListe");
            var tSablon = TabloSatirYap(tabloListe.transform,
                "TabloSatir_Sablon", 6, 28);
            tSablon.SetActive(false);

            BoslukYap(root.transform, 16);

            BaslikYap(root.transform, "Son7GunBaslik", "Son 7 Gun - Ders Bazli");
            var s7Liste = DikeyListeYap(root.transform, "Son7GunListe");
            var s7Sablon = IlerlemeCubuguYap(s7Liste.transform,
                "DersCubugu_Sablon", 28);
            s7Sablon.SetActive(false);

            return root;
        }

        // ================================================================
        //  SABLON OLUŞTURMA — TRENDLER
        // ================================================================

        private GameObject TrendlerSablonOlustur()
        {
            var root = SablonKokuYap("TrendlerIcerik_Sablon");

            BaslikYap(root.transform, "TrendBaslik", "Ders Bazli Gelisim Trendi");

            var trendListe = DikeyListeYap(root.transform, "TrendListe");

            // Trend blok sablonu (her ders icin klonlanir)
            var blokSablon = new GameObject("TrendBlok_Sablon");
            blokSablon.SetActive(false);
            blokSablon.transform.SetParent(trendListe.transform, false);
            blokSablon.AddComponent<RectTransform>();
            var bLE = blokSablon.AddComponent<LayoutElement>();
            bLE.flexibleWidth = 1;
            var bVLG = blokSablon.AddComponent<VerticalLayoutGroup>();
            bVLG.spacing = 4;
            bVLG.padding = new RectOffset(4, 4, 4, 8);
            bVLG.childForceExpandWidth = true;
            bVLG.childForceExpandHeight = false;
            bVLG.childControlHeight = true;
            bVLG.childControlWidth = true;
            var bCSF = blokSablon.AddComponent<ContentSizeFitter>();
            bCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            BaslikYap(blokSablon.transform, "DersBaslik", "Ders Adi");

            var gListe = DikeyListeYap(blokSablon.transform, "GrafikListe");
            var cSablon = IlerlemeCubuguYap(gListe.transform, "CubukSablon", 24);
            cSablon.SetActive(false);

            MetinYap(blokSablon.transform, "TrendYorum", 15);

            BoslukYap(root.transform, 16);

            BaslikYap(root.transform, "DegerlendirmeBaslik", "Genel Degerlendirme");

            var yorumListe = DikeyListeYap(root.transform, "YorumListe");
            var ySablon = MetinYap(yorumListe.transform, "Yorum_Sablon", 14);
            ySablon.SetActive(false);
            ySablon.GetComponent<LayoutElement>().preferredHeight = 26;

            return root;
        }

        // ================================================================
        //  UI ELEMAN YAPIM YARDIMCILARI
        // ================================================================

        /// <summary>
        /// Sablonun kok GameObject'i: inactive, VLG + CSF + LayoutElement.
        /// </summary>
        private GameObject SablonKokuYap(string isim)
        {
            var go = new GameObject(isim);
            go.SetActive(false);
            go.transform.SetParent(transform, false);

            go.AddComponent<RectTransform>();
            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;

            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;

            var csf = go.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return go;
        }

        /// <summary>Ilerleme cubugu: BG Image + Fill Image + Text TMP.</summary>
        private GameObject IlerlemeCubuguYap(Transform parent, string isim,
            float yukseklik = 28)
        {
            var go = new GameObject(isim);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, yukseklik);
            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.preferredHeight = yukseklik;
            go.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.15f, 0.9f);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(go.transform, false);
            var frt = fill.AddComponent<RectTransform>();
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = new Vector2(0.5f, 1f);
            frt.offsetMin = Vector2.zero;
            frt.offsetMax = Vector2.zero;
            fill.AddComponent<Image>().color = new Color(0.3f, 0.85f, 0.4f);

            var txt = new GameObject("Text");
            txt.transform.SetParent(go.transform, false);
            var trt = txt.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(8, 0);
            trt.offsetMax = new Vector2(-8, 0);
            var tmp = txt.AddComponent<TextMeshProUGUI>();
            tmp.text = "%50";
            tmp.fontSize = 13;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;

            return go;
        }

        /// <summary>Ozet karti: BG Image + VLG + DegerText + EtiketText.</summary>
        private GameObject OzetKartYap(Transform parent, string isim,
            float genislik = 120, float yukseklik = 70)
        {
            var go = new GameObject(isim);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>().sizeDelta =
                new Vector2(genislik, yukseklik);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = genislik;
            le.preferredHeight = yukseklik;
            go.AddComponent<Image>().color = new Color(0.14f, 0.16f, 0.22f, 0.95f);

            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(4, 4, 6, 4);
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var dObj = new GameObject("DegerText");
            dObj.transform.SetParent(go.transform, false);
            dObj.AddComponent<RectTransform>();
            dObj.AddComponent<LayoutElement>().preferredHeight = 35;
            var dTmp = dObj.AddComponent<TextMeshProUGUI>();
            dTmp.text = "0";
            dTmp.fontSize = 22;
            dTmp.fontStyle = FontStyles.Bold;
            dTmp.color = Color.white;
            dTmp.alignment = TextAlignmentOptions.Center;
            dTmp.raycastTarget = false;

            var eObj = new GameObject("EtiketText");
            eObj.transform.SetParent(go.transform, false);
            eObj.AddComponent<RectTransform>();
            eObj.AddComponent<LayoutElement>().preferredHeight = 20;
            var eTmp = eObj.AddComponent<TextMeshProUGUI>();
            eTmp.text = "Etiket";
            eTmp.fontSize = 11;
            eTmp.color = new Color(0.6f, 0.6f, 0.6f);
            eTmp.alignment = TextAlignmentOptions.Center;
            eTmp.raycastTarget = false;

            return go;
        }

        /// <summary>Tablo satiri: BG Image + HLG + N kolon TMP.</summary>
        private GameObject TabloSatirYap(Transform parent, string isim,
            int kolonSayisi = 6, float yukseklik = 28)
        {
            var go = new GameObject(isim);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(0, yukseklik);
            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.preferredHeight = yukseklik;
            go.AddComponent<Image>().color = new Color(0.14f, 0.14f, 0.18f, 0.6f);

            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4;
            hlg.padding = new RectOffset(8, 8, 2, 2);
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            for (int i = 0; i < kolonSayisi; i++)
            {
                var col = new GameObject("Kolon" + i);
                col.transform.SetParent(go.transform, false);
                col.AddComponent<RectTransform>();
                col.AddComponent<LayoutElement>().preferredWidth = 65;
                var tmp = col.AddComponent<TextMeshProUGUI>();
                tmp.text = "";
                tmp.fontSize = 12;
                tmp.color = new Color(0.85f, 0.85f, 0.85f);
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                tmp.raycastTarget = false;
            }

            return go;
        }

        /// <summary>Baslik metni: TMP bold, buyuk font.</summary>
        private GameObject BaslikYap(Transform parent, string isim,
            string varsayilanMetin = "")
        {
            var go = new GameObject(isim);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.preferredHeight = 32;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = varsayilanMetin;
            tmp.fontSize = 17;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.9f, 0.9f, 0.95f);
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.raycastTarget = false;
            return go;
        }

        /// <summary>Genel metin: TMP normal.</summary>
        private GameObject MetinYap(Transform parent, string isim,
            float fontSize = 14)
        {
            var go = new GameObject(isim);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.preferredHeight = 24;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = "";
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.raycastTarget = false;
            return go;
        }

        /// <summary>Yatay satir container: HLG.</summary>
        private GameObject YataySatirYap(Transform parent, string isim,
            float yukseklik = 75)
        {
            var go = new GameObject(isim);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = yukseklik;
            le.flexibleWidth = 1;
            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            return go;
        }

        /// <summary>Dikey liste container: VLG + CSF.</summary>
        private GameObject DikeyListeYap(Transform parent, string isim,
            float spacing = 4)
        {
            var go = new GameObject(isim);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = spacing;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            var csf = go.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return go;
        }

        /// <summary>Zorluk segment cubugu parcasi: Image + Text.</summary>
        private void ZorlukSegmendiYap(Transform parent, string isim, Color renk)
        {
            var go = new GameObject(isim);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 0.33f;
            le.preferredHeight = 22;
            go.AddComponent<Image>().color = renk;

            var txt = new GameObject("Text");
            txt.transform.SetParent(go.transform, false);
            var trt = txt.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            var tmp = txt.AddComponent<TextMeshProUGUI>();
            tmp.text = "";
            tmp.fontSize = 10;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
        }

        /// <summary>Bosluk elemani.</summary>
        private void BoslukYap(Transform parent, float yukseklik = 12)
        {
            var go = new GameObject("Bosluk");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(0, yukseklik);
            go.AddComponent<LayoutElement>().preferredHeight = yukseklik;
        }

        // ================================================================
        //  GENEL YARDIMCI + TEMIZLIK
        // ================================================================

        private void ContentTemizle(Transform content)
        {
            if (content == null) return;
            foreach (Transform child in content)
                Destroy(child.gameObject);
        }

        private void UyariGoster(Transform parent, string mesaj)
        {
            var go = new GameObject("Uyari");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            go.AddComponent<LayoutElement>().preferredHeight = 60;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = mesaj;
            tmp.fontSize = 16;
            tmp.color = new Color(0.6f, 0.6f, 0.7f);
            tmp.alignment = TextAlignmentOptions.Center;
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
