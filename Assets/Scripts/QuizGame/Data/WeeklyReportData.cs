using System;
using System.Collections.Generic;
using System.Linq;

namespace QuizGame.Data
{
    /// <summary>
    /// Haftalık ve günlük rapor verileri.
    /// ProgressDashboard tarafından kullanılır.
    /// </summary>
    /// 
    // ═══════════════════════════════════════════════════
    //  GÜNLÜK RAPOR
    // ═══════════════════════════════════════════════════

    [Serializable]
    public class GunlukRapor
    {
        public string tarih;          // "2026-02-09"
        public int toplamSoru;
        public int dogruSayisi;
        public int yanlisSayisi;
        public float basariYuzdesi;
        public float ortSure;
        public int oyunSayisi;

        // Ders bazlı
        public Dictionary<DersKategorisi, int> dersBazliDogru = new Dictionary<DersKategorisi, int>();
        public Dictionary<DersKategorisi, int> dersBazliYanlis = new Dictionary<DersKategorisi, int>();
        public Dictionary<DersKategorisi, float> dersBazliBasari = new Dictionary<DersKategorisi, float>();
    }

    // ═══════════════════════════════════════════════════
    //  HAFTALIK RAPOR
    // ═══════════════════════════════════════════════════

    [Serializable]
    public class HaftalikRapor
    {
        public string haftaBaslangic;  // "2026-02-03"
        public string haftaBitis;      // "2026-02-09"
        public string haftaEtiketi;    // "3-9 Şub"
        public int toplamSoru;
        public int dogruSayisi;
        public int yanlisSayisi;
        public float basariYuzdesi;
        public float ortSure;
        public int oyunSayisi;
        public List<GunlukRapor> gunler = new List<GunlukRapor>();

        // Ders bazlı
        public Dictionary<DersKategorisi, float> dersBazliBasari = new Dictionary<DersKategorisi, float>();
    }

    // ═══════════════════════════════════════════════════
    //  KARŞILAŞTIRMA VERİSİ
    // ═══════════════════════════════════════════════════

    [Serializable]
    public class HaftaKarsilastirma
    {
        public HaftalikRapor oncekiHafta;
        public HaftalikRapor buHafta;
        public float basariFarki;       // buHafta - öncekiHafta başarı yüzdesi
        public Dictionary<DersKategorisi, float> dersBazliFark = new Dictionary<DersKategorisi, float>();
    }

    // ═══════════════════════════════════════════════════
    //  RAPOR HESAPLAYICI
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// StudentPerformance verisinden haftalık/günlük raporlar oluşturur.
    /// </summary>
    public static class RaporHesaplayici
    {
        private static readonly string[] AyAdlari = { "", "Oca", "Şub", "Mar", "Nis", "May", "Haz",
            "Tem", "Ağu", "Eyl", "Eki", "Kas", "Ara" };

        /// <summary>
        /// Tüm cevaplardan günlük rapor listesi oluşturur.
        /// </summary>
        public static List<GunlukRapor> GunlukRaporlarOlustur(StudentPerformance perf,
            DateTime? baslangic = null, DateTime? bitis = null)
        {
            if (perf == null || perf.tumCevaplar.Count == 0)
                return new List<GunlukRapor>();

            var cevaplar = perf.tumCevaplar.AsEnumerable();

            if (baslangic.HasValue)
                cevaplar = cevaplar.Where(c => c.TarihAsDateTime.Date >= baslangic.Value.Date);
            if (bitis.HasValue)
                cevaplar = cevaplar.Where(c => c.TarihAsDateTime.Date <= bitis.Value.Date);

            return cevaplar
                .GroupBy(c => c.TarihAsDateTime.Date)
                .OrderBy(g => g.Key)
                .Select(g => GunlukRaporOlustur(g.Key, g.ToList()))
                .ToList();
        }

        private static GunlukRapor GunlukRaporOlustur(DateTime tarih, List<AnswerRecord> cevaplar)
        {
            var rapor = new GunlukRapor
            {
                tarih = tarih.ToString("yyyy-MM-dd"),
                toplamSoru = cevaplar.Count,
                dogruSayisi = cevaplar.Count(c => c.dogruMu),
                yanlisSayisi = cevaplar.Count(c => !c.dogruMu),
                ortSure = cevaplar.Where(c => c.cevapSuresi > 0).Select(c => c.cevapSuresi).DefaultIfEmpty(0).Average(),
                oyunSayisi = cevaplar.Select(c => c.macId).Distinct().Count()
            };

            rapor.basariYuzdesi = rapor.toplamSoru > 0
                ? (float)rapor.dogruSayisi / rapor.toplamSoru * 100f : 0f;

            // Ders bazlı
            foreach (DersKategorisi ders in Enum.GetValues(typeof(DersKategorisi)))
            {
                var dersCevaplar = cevaplar.Where(c => c.ders == ders).ToList();
                if (dersCevaplar.Count == 0) continue;

                int d = dersCevaplar.Count(c => c.dogruMu);
                int y = dersCevaplar.Count(c => !c.dogruMu);
                rapor.dersBazliDogru[ders] = d;
                rapor.dersBazliYanlis[ders] = y;
                rapor.dersBazliBasari[ders] = (d + y) > 0 ? (float)d / (d + y) * 100f : 0f;
            }

            return rapor;
        }

        /// <summary>
        /// Haftalık raporlar oluşturur. haftaSayisi kadar geriye bakar.
        /// </summary>
        public static List<HaftalikRapor> HaftalikRaporlarOlustur(StudentPerformance perf, int haftaSayisi = 8)
        {
            if (perf == null || perf.tumCevaplar.Count == 0)
                return new List<HaftalikRapor>();

            var raporlar = new List<HaftalikRapor>();
            var bugun = DateTime.Now.Date;

            // Bu haftanın Pazartesi'sini bul
            int gunFarki = ((int)bugun.DayOfWeek + 6) % 7; // Pazartesi = 0
            var buHaftaPazartesi = bugun.AddDays(-gunFarki);

            for (int h = haftaSayisi - 1; h >= 0; h--)
            {
                var haftaBaslangic = buHaftaPazartesi.AddDays(-7 * h);
                var haftaBitis = haftaBaslangic.AddDays(6);

                var haftaCevaplar = perf.tumCevaplar
                    .Where(c => c.TarihAsDateTime.Date >= haftaBaslangic && c.TarihAsDateTime.Date <= haftaBitis)
                    .ToList();

                var rapor = new HaftalikRapor
                {
                    haftaBaslangic = haftaBaslangic.ToString("yyyy-MM-dd"),
                    haftaBitis = haftaBitis.ToString("yyyy-MM-dd"),
                    haftaEtiketi = $"{haftaBaslangic.Day}-{haftaBitis.Day} {AyAdlari[haftaBitis.Month]}",
                    toplamSoru = haftaCevaplar.Count,
                    dogruSayisi = haftaCevaplar.Count(c => c.dogruMu),
                    yanlisSayisi = haftaCevaplar.Count(c => !c.dogruMu),
                    ortSure = haftaCevaplar.Where(c => c.cevapSuresi > 0).Select(c => c.cevapSuresi).DefaultIfEmpty(0).Average(),
                    oyunSayisi = haftaCevaplar.Select(c => c.macId).Distinct().Count()
                };

                rapor.basariYuzdesi = rapor.toplamSoru > 0
                    ? (float)rapor.dogruSayisi / rapor.toplamSoru * 100f : 0f;

                // Günlük raporlar
                rapor.gunler = GunlukRaporlarOlustur(perf, haftaBaslangic, haftaBitis);

                // Ders bazlı
                foreach (DersKategorisi ders in Enum.GetValues(typeof(DersKategorisi)))
                {
                    var dersCevaplar = haftaCevaplar.Where(c => c.ders == ders).ToList();
                    if (dersCevaplar.Count == 0) continue;
                    int d = dersCevaplar.Count(c => c.dogruMu);
                    int y = dersCevaplar.Count(c => !c.dogruMu);
                    rapor.dersBazliBasari[ders] = (d + y) > 0 ? (float)d / (d + y) * 100f : 0f;
                }

                raporlar.Add(rapor);
            }

            return raporlar;
        }

        /// <summary>
        /// Son iki hafta karşılaştırması yapar.
        /// </summary>
        public static HaftaKarsilastirma HaftaKarsilastirmasiOlustur(StudentPerformance perf)
        {
            var haftalar = HaftalikRaporlarOlustur(perf, 2);
            if (haftalar.Count < 2) return null;

            var karsilastirma = new HaftaKarsilastirma
            {
                oncekiHafta = haftalar[0],
                buHafta = haftalar[1],
                basariFarki = haftalar[1].basariYuzdesi - haftalar[0].basariYuzdesi
            };

            foreach (DersKategorisi ders in Enum.GetValues(typeof(DersKategorisi)))
            {
                float onceki = haftalar[0].dersBazliBasari.ContainsKey(ders) ? haftalar[0].dersBazliBasari[ders] : -1;
                float simdiki = haftalar[1].dersBazliBasari.ContainsKey(ders) ? haftalar[1].dersBazliBasari[ders] : -1;

                if (onceki >= 0 && simdiki >= 0)
                    karsilastirma.dersBazliFark[ders] = simdiki - onceki;
            }

            return karsilastirma;
        }

        /// <summary>
        /// Belirli tarih aralığı için ders bazlı başarı yüzdeleri.
        /// </summary>
        public static Dictionary<DersKategorisi, float> DersBazliBasariHesapla(
            StudentPerformance perf, DateTime baslangic, DateTime bitis)
        {
            var sonuc = new Dictionary<DersKategorisi, float>();
            if (perf == null) return sonuc;

            var cevaplar = perf.tumCevaplar
                .Where(c => c.TarihAsDateTime.Date >= baslangic.Date && c.TarihAsDateTime.Date <= bitis.Date)
                .ToList();

            foreach (DersKategorisi ders in Enum.GetValues(typeof(DersKategorisi)))
            {
                var dersC = cevaplar.Where(c => c.ders == ders).ToList();
                if (dersC.Count == 0) continue;
                int d = dersC.Count(c => c.dogruMu);
                sonuc[ders] = (float)d / dersC.Count * 100f;
            }

            return sonuc;
        }

        /// <summary>
        /// Tüm derslerin adını Türkçe döndürür.
        /// </summary>
        public static string DersAdi(DersKategorisi ders)
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
