using UnityEngine;
using System.IO;
using QuizGame.Data;

namespace QuizGame.Managers
{
    /// <summary>
    /// Sınıf ve öğrenci verilerini yöneten ve JSON olarak kaydeden/yükleyen manager.
    /// Singleton pattern kullanır.
    /// </summary>
    public class DataManager : MonoBehaviour
    {
        public static DataManager Instance { get; private set; }

        [Header("Veritabanları")]
        public SchoolDatabase okulVeritabani = new SchoolDatabase();
        public QuestionDatabase soruVeritabani = new QuestionDatabase();
        public AnalyticsDatabase analizVeritabani = new AnalyticsDatabase();
        public PerformanceDatabase performansVeritabani = new PerformanceDatabase();

        private string okulDosyaYolu;
        private string soruDosyaYolu;
        private string analizDosyaYolu;
        private string performansDosyaYolu;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Dosya yollarını ayarla
            okulDosyaYolu = Path.Combine(Application.persistentDataPath, "okul_veritabani.json");
            soruDosyaYolu = Path.Combine(Application.persistentDataPath, "soru_veritabani.json");
            analizDosyaYolu = Path.Combine(Application.persistentDataPath, "analiz_veritabani.json");
            performansDosyaYolu = Path.Combine(Application.persistentDataPath, "performans_veritabani.json");

            // Verileri yükle
            OkulVerisiniYukle();
            SoruVerisiniYukle();
            AnalizVerisiniYukle();
            PerformansVerisiniYukle();
        }

        // ═══════════════════════════════════════════════════
        //  OKUL VERİTABANI (Sınıflar ve Öğrenciler)
        // ═══════════════════════════════════════════════════

        public ClassData SinifEkle(string sinifAdi)
        {
            var sinif = okulVeritabani.SinifEkle(sinifAdi);
            OkulVerisiniKaydet();
            return sinif;
        }

        public bool SinifSil(string sinifId)
        {
            bool sonuc = okulVeritabani.SinifSil(sinifId);
            if (sonuc) OkulVerisiniKaydet();
            return sonuc;
        }

        public StudentData OgrenciEkle(string sinifId, string ad, string soyad, string ogrenciNo)
        {
            var sinif = okulVeritabani.SinifBul(sinifId);
            if (sinif == null)
            {
                Debug.LogError($"Sınıf bulunamadı: {sinifId}");
                return null;
            }

            var ogrenci = new StudentData(ad, soyad, ogrenciNo, sinifId);
            sinif.OgrenciEkle(ogrenci);
            OkulVerisiniKaydet();
            return ogrenci;
        }

        public bool OgrenciSil(string sinifId, string ogrenciId)
        {
            var sinif = okulVeritabani.SinifBul(sinifId);
            if (sinif == null) return false;

            bool sonuc = sinif.OgrenciSil(ogrenciId);
            if (sonuc) OkulVerisiniKaydet();
            return sonuc;
        }

        // ═══════════════════════════════════════════════════
        //  KAYDET / YÜKLE
        // ═══════════════════════════════════════════════════

        public void OkulVerisiniKaydet()
        {
            string json = JsonUtility.ToJson(okulVeritabani, true);
            File.WriteAllText(okulDosyaYolu, json);
            Debug.Log($"Okul verisi kaydedildi: {okulDosyaYolu}");
        }

        public void OkulVerisiniYukle()
        {
            if (File.Exists(okulDosyaYolu))
            {
                string json = File.ReadAllText(okulDosyaYolu);
                okulVeritabani = JsonUtility.FromJson<SchoolDatabase>(json);
                Debug.Log($"Okul verisi yüklendi. {okulVeritabani.siniflar.Count} sınıf bulundu.");
            }
            else
            {
                okulVeritabani = new SchoolDatabase();
                Debug.Log("Okul verisi dosyası bulunamadı, yeni veritabanı oluşturuldu.");
            }
        }

        public void SoruVerisiniKaydet()
        {
            string json = JsonUtility.ToJson(soruVeritabani, true);
            File.WriteAllText(soruDosyaYolu, json);
        }

        public void SoruVerisiniYukle()
        {
            if (File.Exists(soruDosyaYolu))
            {
                string json = File.ReadAllText(soruDosyaYolu);
                soruVeritabani = JsonUtility.FromJson<QuestionDatabase>(json);
                Debug.Log($"Soru verisi yüklendi. {soruVeritabani.sorular.Count} soru bulundu.");
            }
            else
            {
                soruVeritabani = new QuestionDatabase();
                VarsayilanSorulariOlustur();
                SoruVerisiniKaydet();
                Debug.Log("Varsayılan sorular oluşturuldu.");
            }
        }

        public void AnalizVerisiniKaydet()
        {
            string json = JsonUtility.ToJson(analizVeritabani, true);
            File.WriteAllText(analizDosyaYolu, json);
        }

        public void AnalizVerisiniYukle()
        {
            if (File.Exists(analizDosyaYolu))
            {
                string json = File.ReadAllText(analizDosyaYolu);
                analizVeritabani = JsonUtility.FromJson<AnalyticsDatabase>(json);
            }
            else
            {
                analizVeritabani = new AnalyticsDatabase();
            }
        }

        public void MacSonucuKaydet(MatchResult sonuc)
        {
            analizVeritabani.macSonuclari.Add(sonuc);
            AnalizVerisiniKaydet();
        }

        // ═══════════════════════════════════════════════════
        //  PERFORMANS VERİTABANI (Öğrenci Bazlı Detaylı Takip)
        // ═══════════════════════════════════════════════════

        public void PerformansVerisiniKaydet()
        {
            string json = JsonUtility.ToJson(performansVeritabani, true);
            File.WriteAllText(performansDosyaYolu, json);
        }

        public void PerformansVerisiniYukle()
        {
            if (File.Exists(performansDosyaYolu))
            {
                string json = File.ReadAllText(performansDosyaYolu);
                performansVeritabani = JsonUtility.FromJson<PerformanceDatabase>(json);
                Debug.Log($"Performans verisi yüklendi. {performansVeritabani.ogrenciPerformanslari.Count} öğrenci kaydı.");
            }
            else
            {
                performansVeritabani = new PerformanceDatabase();
            }
        }

        /// <summary>
        /// Tek bir cevabı öğrenci performans veritabanına kaydeder.
        /// GameManager her doğru/yanlış cevaptan sonra bunu çağırır.
        /// </summary>
        public void CevapPerformansKaydet(string ogrenciId, string ogrenciAd, AnswerRecord kayit)
        {
            var perf = performansVeritabani.PerformansGetirVeyaOlustur(ogrenciId, ogrenciAd);
            perf.CevapKaydet(kayit);
            PerformansVerisiniKaydet();
        }

        /// <summary>Öğrenci performansını getir (salt-okunur)</summary>
        public StudentPerformance OgrenciPerformansiGetir(string ogrenciId)
        {
            return performansVeritabani.PerformansGetir(ogrenciId);
        }

        // ═══════════════════════════════════════════════════
        //  VARSAYILAN SORULAR
        // ═══════════════════════════════════════════════════

        private void VarsayilanSorulariOlustur()
        {
            // Matematik - Kolay
            SoruEkle("5 + 3 = ?", new string[] { "6", "7", "8", "9" }, 2,
                     ZorlukSeviyesi.Kolay, DersKategorisi.Matematik);
            SoruEkle("12 - 4 = ?", new string[] { "6", "7", "8", "9" }, 2,
                     ZorlukSeviyesi.Kolay, DersKategorisi.Matematik);
            SoruEkle("3 x 4 = ?", new string[] { "7", "10", "12", "14" }, 2,
                     ZorlukSeviyesi.Kolay, DersKategorisi.Matematik);
            SoruEkle("20 / 5 = ?", new string[] { "3", "4", "5", "6" }, 1,
                     ZorlukSeviyesi.Kolay, DersKategorisi.Matematik);

            // Matematik - Orta
            SoruEkle("15 x 8 = ?", new string[] { "100", "110", "120", "130" }, 2,
                     ZorlukSeviyesi.Orta, DersKategorisi.Matematik);
            SoruEkle("144'ün karekökü kaçtır?", new string[] { "10", "11", "12", "13" }, 2,
                     ZorlukSeviyesi.Orta, DersKategorisi.Matematik);
            SoruEkle("Bir üçgenin iç açıları toplamı kaç derecedir?", new string[] { "90", "180", "270", "360" }, 1,
                     ZorlukSeviyesi.Orta, DersKategorisi.Matematik);

            // Matematik - Zor
            SoruEkle("2^10 = ?", new string[] { "512", "1024", "2048", "4096" }, 1,
                     ZorlukSeviyesi.Zor, DersKategorisi.Matematik);
            SoruEkle("Pi sayısının ilk 4 rakamı nedir?", new string[] { "3.141", "3.142", "3.145", "3.144" }, 0,
                     ZorlukSeviyesi.Zor, DersKategorisi.Matematik);

            // Türkçe - Kolay
            SoruEkle("Hangisi bir edat değildir?", new string[] { "için", "ile", "kadar", "güzel" }, 3,
                     ZorlukSeviyesi.Kolay, DersKategorisi.Turkce);
            SoruEkle("'Kitap' kelimesinin çoğul hali nedir?", new string[] { "Kitapçı", "Kitaplar", "Kitaplık", "Kitapsız" }, 1,
                     ZorlukSeviyesi.Kolay, DersKategorisi.Turkce);

            // Türkçe - Orta
            SoruEkle("Hangisi bir bağlaçtır?", new string[] { "hızlı", "fakat", "güzel", "koşmak" }, 1,
                     ZorlukSeviyesi.Orta, DersKategorisi.Turkce);
            SoruEkle("Hangisi soyut bir isimdir?", new string[] { "masa", "sevgi", "araba", "ağaç" }, 1,
                     ZorlukSeviyesi.Orta, DersKategorisi.Turkce);

            // Fen - Kolay
            SoruEkle("Suyun kimyasal formülü nedir?", new string[] { "CO2", "H2O", "O2", "NaCl" }, 1,
                     ZorlukSeviyesi.Kolay, DersKategorisi.Fen);
            SoruEkle("Güneş Sistemi'nde kaç gezegen vardır?", new string[] { "7", "8", "9", "10" }, 1,
                     ZorlukSeviyesi.Kolay, DersKategorisi.Fen);

            // Fen - Orta
            SoruEkle("Hangisi bir element değildir?", new string[] { "Altın", "Su", "Demir", "Oksijen" }, 1,
                     ZorlukSeviyesi.Orta, DersKategorisi.Fen);
            SoruEkle("Işık hızı yaklaşık kaç km/s'dir?", new string[] { "100.000", "200.000", "300.000", "400.000" }, 2,
                     ZorlukSeviyesi.Orta, DersKategorisi.Fen);

            // Fen - Zor
            SoruEkle("DNA'nın açılımı nedir?", new string[] { "Deoksiribonükleik Asit", "Diribonükleik Asit", "Deoksiribo Nitrik Asit", "Dinamik Nükleer Asit" }, 0,
                     ZorlukSeviyesi.Zor, DersKategorisi.Fen);

            // Sosyal - Kolay
            SoruEkle("Türkiye'nin başkenti neresidir?", new string[] { "İstanbul", "Ankara", "İzmir", "Bursa" }, 1,
                     ZorlukSeviyesi.Kolay, DersKategorisi.Sosyal);
            SoruEkle("Cumhuriyet kaç yılında ilan edilmiştir?", new string[] { "1920", "1921", "1922", "1923" }, 3,
                     ZorlukSeviyesi.Kolay, DersKategorisi.Sosyal);

            // Sosyal - Orta
            SoruEkle("İstanbul'un fethi hangi yılda gerçekleşmiştir?", new string[] { "1453", "1463", "1443", "1473" }, 0,
                     ZorlukSeviyesi.Orta, DersKategorisi.Sosyal);

            // İngilizce - Kolay
            SoruEkle("'Apple' ne demektir?", new string[] { "Armut", "Elma", "Portakal", "Üzüm" }, 1,
                     ZorlukSeviyesi.Kolay, DersKategorisi.Ingilizce);
            SoruEkle("'Cat' ne demektir?", new string[] { "Köpek", "Kuş", "Kedi", "Balık" }, 2,
                     ZorlukSeviyesi.Kolay, DersKategorisi.Ingilizce);

            // İngilizce - Orta
            SoruEkle("'I ___ a student' boşluğa ne gelmelidir?", new string[] { "is", "am", "are", "be" }, 1,
                     ZorlukSeviyesi.Orta, DersKategorisi.Ingilizce);

            // Genel Kültür - Kolay
            SoruEkle("Dünya'nın en büyük okyanusu hangisidir?", new string[] { "Atlantik", "Hint", "Kuzey Buz", "Büyük (Pasifik)" }, 3,
                     ZorlukSeviyesi.Kolay, DersKategorisi.GenelKultur);

            // Genel Kültür - Orta
            SoruEkle("Hangisi bir kıta değildir?", new string[] { "Asya", "Avrupa", "Arktika", "Afrika" }, 2,
                     ZorlukSeviyesi.Orta, DersKategorisi.GenelKultur);
        }

        private void SoruEkle(string metin, string[] siklar, int dogruIndex,
                               ZorlukSeviyesi zorluk, DersKategorisi kategori,
                               string aciklama = "")
        {
            soruVeritabani.sorular.Add(new QuestionData(metin, siklar, dogruIndex, zorluk, kategori, aciklama));
        }
    }
}
