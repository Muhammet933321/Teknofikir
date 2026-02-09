using System;
using System.Collections.Generic;
using System.Linq;

namespace QuizGame.Data
{
    // ═══════════════════════════════════════════════════════════
    //  TEK BİR SORU CEVABI KAYDI
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Bir öğrencinin tek bir soruya verdiği cevabın detaylı kaydı.
    /// Her cevap — doğru ya da yanlış — kalıcı olarak saklanır.
    /// </summary>
    [Serializable]
    public class AnswerRecord
    {
        public string soruId;
        public string soruMetni;
        public DersKategorisi ders;
        public ZorlukSeviyesi zorluk;
        public bool dogruMu;
        public int secilenSikIndex;
        public int dogruSikIndex;
        public float cevapSuresi;         // saniye
        public string tarih;              // ISO 8601 — "2026-02-09T14:30:00"
        public string macId;              // Hangi maçta verildi

        public DateTime TarihAsDateTime =>
            DateTime.TryParse(tarih, out var dt) ? dt : DateTime.MinValue;
    }

    // ═══════════════════════════════════════════════════════════
    //  DERS BAZLI İSTATİSTİKLER (hesaplanmış özet)
    // ═══════════════════════════════════════════════════════════

    [Serializable]
    public class DersIstatistik
    {
        public DersKategorisi ders;
        public int toplamSoru;
        public int dogruSayisi;
        public int yanlisSayisi;
        public float ortalamaSure;        // saniye
        public float basariYuzdesi;       // 0-100

        // Zorluk bazlı kırılım
        public int kolayDogru, kolayYanlis;
        public int ortaDogru, ortaYanlis;
        public int zorDogru, zorYanlis;
    }

    // ═══════════════════════════════════════════════════════════
    //  GELİŞİM KAYDI (zamana göre trend)
    // ═══════════════════════════════════════════════════════════

    [Serializable]
    public class GelisimKaydi
    {
        public string tarih;              // Gün bazında "2026-02-09"
        public DersKategorisi ders;
        public int dogruSayisi;
        public int yanlisSayisi;
        public float basariYuzdesi;
    }

    // ═══════════════════════════════════════════════════════════
    //  ÖĞRENCİ PERFORMANS VERİSİ (kök obje)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Bir öğrencinin tüm oyunlar boyunca biriken performans verisi.
    /// JSON dosyasına öğrenci başına kaydedilir.
    /// </summary>
    [Serializable]
    public class StudentPerformance
    {
        public string ogrenciId;
        public string ogrenciAd;
        public List<AnswerRecord> tumCevaplar = new List<AnswerRecord>();

        // ───────── Kayıt Ekleme ─────────

        public void CevapKaydet(AnswerRecord kayit)
        {
            tumCevaplar.Add(kayit);
        }

        // ───────── Ders Bazlı İstatistik ─────────

        public DersIstatistik DersIstatistigiHesapla(DersKategorisi ders)
        {
            var dersler = tumCevaplar.Where(c => c.ders == ders).ToList();
            if (dersler.Count == 0) return null;

            var ist = new DersIstatistik
            {
                ders = ders,
                toplamSoru = dersler.Count,
                dogruSayisi = dersler.Count(c => c.dogruMu),
                yanlisSayisi = dersler.Count(c => !c.dogruMu),
                ortalamaSure = dersler.Where(c => c.cevapSuresi > 0).Select(c => c.cevapSuresi).DefaultIfEmpty(0).Average()
            };

            ist.basariYuzdesi = ist.toplamSoru > 0 ? (float)ist.dogruSayisi / ist.toplamSoru * 100f : 0f;

            // Zorluk kırılımı
            ist.kolayDogru = dersler.Count(c => c.zorluk == ZorlukSeviyesi.Kolay && c.dogruMu);
            ist.kolayYanlis = dersler.Count(c => c.zorluk == ZorlukSeviyesi.Kolay && !c.dogruMu);
            ist.ortaDogru = dersler.Count(c => c.zorluk == ZorlukSeviyesi.Orta && c.dogruMu);
            ist.ortaYanlis = dersler.Count(c => c.zorluk == ZorlukSeviyesi.Orta && !c.dogruMu);
            ist.zorDogru = dersler.Count(c => c.zorluk == ZorlukSeviyesi.Zor && c.dogruMu);
            ist.zorYanlis = dersler.Count(c => c.zorluk == ZorlukSeviyesi.Zor && !c.dogruMu);

            return ist;
        }

        /// <summary>Tüm dersler için istatistik listesi döndürür.</summary>
        public List<DersIstatistik> TumDersIstatistikleri()
        {
            var liste = new List<DersIstatistik>();
            foreach (DersKategorisi ders in Enum.GetValues(typeof(DersKategorisi)))
            {
                var ist = DersIstatistigiHesapla(ders);
                if (ist != null) liste.Add(ist);
            }
            return liste;
        }

        // ───────── Gelişim Trendi ─────────

        /// <summary>
        /// Belirli bir ders için gün bazlı gelişim trend verisi döndürür.
        /// </summary>
        public List<GelisimKaydi> GelisimTrendiHesapla(DersKategorisi ders)
        {
            var dersler = tumCevaplar.Where(c => c.ders == ders).ToList();
            if (dersler.Count == 0) return new List<GelisimKaydi>();

            var gunluk = dersler
                .GroupBy(c => c.TarihAsDateTime.Date)
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    int d = g.Count(c => c.dogruMu);
                    int y = g.Count(c => !c.dogruMu);
                    return new GelisimKaydi
                    {
                        tarih = g.Key.ToString("yyyy-MM-dd"),
                        ders = ders,
                        dogruSayisi = d,
                        yanlisSayisi = y,
                        basariYuzdesi = (d + y) > 0 ? (float)d / (d + y) * 100f : 0f
                    };
                })
                .ToList();

            return gunluk;
        }

        // ───────── Tekrar Edilen Yanlışlar ─────────

        /// <summary>
        /// Öğrencinin birden fazla kez yanlış yaptığı soruları döndürür.
        /// Her soru için toplam deneme ve yanlış sayısı verilir.
        /// </summary>
        public List<TekrarYanlisKaydi> TekrarYanlislariBul()
        {
            return tumCevaplar
                .GroupBy(c => c.soruId)
                .Where(g => g.Count(c => !c.dogruMu) >= 2)
                .Select(g =>
                {
                    var hepsi = g.OrderBy(c => c.TarihAsDateTime).ToList();
                    var son = hepsi.Last();
                    return new TekrarYanlisKaydi
                    {
                        soruId = g.Key,
                        soruMetni = hepsi.First().soruMetni,
                        ders = hepsi.First().ders,
                        zorluk = hepsi.First().zorluk,
                        toplamDeneme = hepsi.Count,
                        yanlisSayisi = hepsi.Count(c => !c.dogruMu),
                        sonDenemeDogru = son.dogruMu,
                        ogrendiMi = hepsi.Count >= 3 &&
                                    hepsi.Skip(hepsi.Count - 2).All(c => c.dogruMu)
                    };
                })
                .OrderByDescending(t => t.yanlisSayisi)
                .ToList();
        }

        // ───────── Belirli Soruya Özel Geçmiş ─────────

        /// <summary>
        /// Öğrencinin belirli bir soruyu kaç kez denediğini,
        /// ilk yanlış olup sonra öğrenip öğrenmediğini gösterir.
        /// </summary>
        public SoruGecmisi SoruGecmisiniGetir(string soruId)
        {
            var kayitlar = tumCevaplar
                .Where(c => c.soruId == soruId)
                .OrderBy(c => c.TarihAsDateTime)
                .ToList();

            if (kayitlar.Count == 0) return null;

            return new SoruGecmisi
            {
                soruId = soruId,
                soruMetni = kayitlar.First().soruMetni,
                ders = kayitlar.First().ders,
                zorluk = kayitlar.First().zorluk,
                denemeler = kayitlar,
                toplamDeneme = kayitlar.Count,
                toplamDogru = kayitlar.Count(c => c.dogruMu),
                toplamYanlis = kayitlar.Count(c => !c.dogruMu),
                sonDenemeDogru = kayitlar.Last().dogruMu,
                ilkDenemeDogruMu = kayitlar.First().dogruMu
            };
        }

        // ───────── Genel Özet ─────────

        public float GenelBasariYuzdesi()
        {
            if (tumCevaplar.Count == 0) return 0f;
            return (float)tumCevaplar.Count(c => c.dogruMu) / tumCevaplar.Count * 100f;
        }

        public float OrtCevapSuresi()
        {
            var gecerli = tumCevaplar.Where(c => c.cevapSuresi > 0).ToList();
            return gecerli.Count > 0 ? gecerli.Average(c => c.cevapSuresi) : 0f;
        }

        public int ToplamOyunSayisi()
        {
            return tumCevaplar.Select(c => c.macId).Distinct().Count();
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  YARDIMCI VERİ YAPILARI
    // ═══════════════════════════════════════════════════════════

    [Serializable]
    public class TekrarYanlisKaydi
    {
        public string soruId;
        public string soruMetni;
        public DersKategorisi ders;
        public ZorlukSeviyesi zorluk;
        public int toplamDeneme;
        public int yanlisSayisi;
        public bool sonDenemeDogru;
        public bool ogrendiMi;           // Son 2 deneme doğruysa "öğrendi" kabul
    }

    [Serializable]
    public class SoruGecmisi
    {
        public string soruId;
        public string soruMetni;
        public DersKategorisi ders;
        public ZorlukSeviyesi zorluk;
        public List<AnswerRecord> denemeler;
        public int toplamDeneme;
        public int toplamDogru;
        public int toplamYanlis;
        public bool sonDenemeDogru;
        public bool ilkDenemeDogruMu;
    }

    // ═══════════════════════════════════════════════════════════
    //  PERFORMANS VERİTABANI (tüm öğrenciler)
    // ═══════════════════════════════════════════════════════════

    [Serializable]
    public class PerformanceDatabase
    {
        public List<StudentPerformance> ogrenciPerformanslari = new List<StudentPerformance>();

        /// <summary>Öğrenci performansını getir (yoksa oluştur)</summary>
        public StudentPerformance PerformansGetirVeyaOlustur(string ogrenciId, string ogrenciAd)
        {
            var perf = ogrenciPerformanslari.Find(p => p.ogrenciId == ogrenciId);
            if (perf == null)
            {
                perf = new StudentPerformance
                {
                    ogrenciId = ogrenciId,
                    ogrenciAd = ogrenciAd
                };
                ogrenciPerformanslari.Add(perf);
            }
            // İsim güncellemesi
            perf.ogrenciAd = ogrenciAd;
            return perf;
        }

        public StudentPerformance PerformansGetir(string ogrenciId)
        {
            return ogrenciPerformanslari.Find(p => p.ogrenciId == ogrenciId);
        }
    }
}
