using System;
using System.Collections.Generic;

namespace QuizGame.Data
{
    /// <summary>
    /// Bir oyuncunun tek bir sorudaki performans verisi.
    /// </summary>
    [Serializable]
    public class RoundResult
    {
        public string soruId;
        public string soruMetni;
        public DersKategorisi kategori;
        public ZorlukSeviyesi zorluk;
        public bool dogruMu;
        public float cevapSuresi;        // Saniye cinsinden ne kadar sürede cevapladı
        public int secilenSikIndex;       // Oyuncunun seçtiği şık
        public int dogruSikIndex;         // Doğru şık
    }

    /// <summary>
    /// Bir oyuncunun tüm oyun boyunca performans verisi.
    /// </summary>
    [Serializable]
    public class PlayerGameResult
    {
        public string ogrenciId;
        public string ogrenciAd;
        public string sinifAdi;
        public List<RoundResult> turSonuclari = new List<RoundResult>();
        public int toplamDogru;
        public int toplamYanlis;
        public int kalanCan;
        public bool kazandiMi;

        // Ders bazında analiz
        public Dictionary<DersKategorisi, int> dersBazliDogru = new Dictionary<DersKategorisi, int>();
        public Dictionary<DersKategorisi, int> dersBazliYanlis = new Dictionary<DersKategorisi, int>();

        public void SonucEkle(RoundResult sonuc)
        {
            turSonuclari.Add(sonuc);
            if (sonuc.dogruMu)
            {
                toplamDogru++;
                if (!dersBazliDogru.ContainsKey(sonuc.kategori))
                    dersBazliDogru[sonuc.kategori] = 0;
                dersBazliDogru[sonuc.kategori]++;
            }
            else
            {
                toplamYanlis++;
                if (!dersBazliYanlis.ContainsKey(sonuc.kategori))
                    dersBazliYanlis[sonuc.kategori] = 0;
                dersBazliYanlis[sonuc.kategori]++;
            }
        }

        /// <summary>
        /// Belirli bir dersteki başarı yüzdesini döndürür.
        /// </summary>
        public float DersBasariYuzdesi(DersKategorisi ders)
        {
            int dogru = dersBazliDogru.ContainsKey(ders) ? dersBazliDogru[ders] : 0;
            int yanlis = dersBazliYanlis.ContainsKey(ders) ? dersBazliYanlis[ders] : 0;
            int toplam = dogru + yanlis;
            if (toplam == 0) return -1f; // Bu dersten soru gelmemiş
            return (float)dogru / toplam * 100f;
        }
    }

    /// <summary>
    /// Bir maçın tüm sonuçlarını tutan veri yapısı.
    /// </summary>
    [Serializable]
    public class MatchResult
    {
        public string matchId;
        public DateTime tarih;
        public PlayerGameResult oyuncu1Sonuc;
        public PlayerGameResult oyuncu2Sonuc;
        public string kazananOgrenciId;

        public MatchResult()
        {
            matchId = Guid.NewGuid().ToString();
            tarih = DateTime.Now;
        }
    }

    /// <summary>
    /// Tüm maç sonuçlarını tutan veritabanı.
    /// </summary>
    [Serializable]
    public class AnalyticsDatabase
    {
        public List<MatchResult> macSonuclari = new List<MatchResult>();

        /// <summary>
        /// Belirli bir öğrencinin tüm maç sonuçlarını döndürür.
        /// </summary>
        public List<PlayerGameResult> OgrenciSonuclariGetir(string ogrenciId)
        {
            var sonuclar = new List<PlayerGameResult>();
            foreach (var mac in macSonuclari)
            {
                if (mac.oyuncu1Sonuc.ogrenciId == ogrenciId)
                    sonuclar.Add(mac.oyuncu1Sonuc);
                else if (mac.oyuncu2Sonuc.ogrenciId == ogrenciId)
                    sonuclar.Add(mac.oyuncu2Sonuc);
            }
            return sonuclar;
        }

        /// <summary>
        /// Belirli bir öğrencinin ders bazlı zayıf olduğu alanları döndürür.
        /// %50'nin altındaki dersler zayıf kabul edilir.
        /// </summary>
        public List<DersKategorisi> ZayifDersleriGetir(string ogrenciId)
        {
            var zayifDersler = new List<DersKategorisi>();
            var sonuclar = OgrenciSonuclariGetir(ogrenciId);

            var toplamDogru = new Dictionary<DersKategorisi, int>();
            var toplamYanlis = new Dictionary<DersKategorisi, int>();

            foreach (var sonuc in sonuclar)
            {
                foreach (var kvp in sonuc.dersBazliDogru)
                {
                    if (!toplamDogru.ContainsKey(kvp.Key)) toplamDogru[kvp.Key] = 0;
                    toplamDogru[kvp.Key] += kvp.Value;
                }
                foreach (var kvp in sonuc.dersBazliYanlis)
                {
                    if (!toplamYanlis.ContainsKey(kvp.Key)) toplamYanlis[kvp.Key] = 0;
                    toplamYanlis[kvp.Key] += kvp.Value;
                }
            }

            foreach (DersKategorisi ders in Enum.GetValues(typeof(DersKategorisi)))
            {
                int dogru = toplamDogru.ContainsKey(ders) ? toplamDogru[ders] : 0;
                int yanlis = toplamYanlis.ContainsKey(ders) ? toplamYanlis[ders] : 0;
                int toplam = dogru + yanlis;
                if (toplam > 0)
                {
                    float yuzde = (float)dogru / toplam * 100f;
                    if (yuzde < 50f)
                        zayifDersler.Add(ders);
                }
            }

            return zayifDersler;
        }
    }
}
