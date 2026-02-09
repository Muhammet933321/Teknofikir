using System;
using System.Collections.Generic;

namespace QuizGame.Data
{
    /// <summary>
    /// Soru zorluk seviyeleri.
    /// </summary>
    public enum ZorlukSeviyesi
    {
        Kolay = 0,
        Orta = 1,
        Zor = 2
    }

    /// <summary>
    /// Soru kategorileri / dersler.
    /// </summary>
    public enum DersKategorisi
    {
        Matematik = 0,
        Turkce = 1,
        Fen = 2,
        Sosyal = 3,
        Ingilizce = 4,
        GenelKultur = 5
    }

    /// <summary>
    /// Bir soruyu temsil eden veri sınıfı.
    /// </summary>
    [Serializable]
    public class QuestionData
    {
        public string id;
        public string soruMetni;
        public string[] siklar = new string[4];   // 4 adet şık (A, B, C, D)
        public int dogruSikIndex;                  // 0-3 arası doğru şık indeksi
        public ZorlukSeviyesi zorluk;
        public DersKategorisi kategori;

        // ═══ Açıklama Sistemi ═══
        public string aciklama;            // Doğru cevabın neden doğru olduğunu açıklayan metin
        public string animasyonAdi;        // (Opsiyonel) Oynatılacak animasyon adı / trigger
        public string videoYolu;           // (Opsiyonel) Gösterilecek video dosyasının yolu

        public bool AciklamaVar => !string.IsNullOrEmpty(aciklama);

        public QuestionData()
        {
            id = Guid.NewGuid().ToString();
        }

        public QuestionData(string soruMetni, string[] siklar, int dogruSikIndex,
                            ZorlukSeviyesi zorluk, DersKategorisi kategori,
                            string aciklama = "")
        {
            this.id = Guid.NewGuid().ToString();
            this.soruMetni = soruMetni;
            this.siklar = siklar;
            this.dogruSikIndex = dogruSikIndex;
            this.zorluk = zorluk;
            this.kategori = kategori;
            this.aciklama = aciklama;
        }
    }

    /// <summary>
    /// Soru havuzunu tutan veri yapısı.
    /// </summary>
    [Serializable]
    public class QuestionDatabase
    {
        public List<QuestionData> sorular = new List<QuestionData>();

        public List<QuestionData> SorulariGetir(ZorlukSeviyesi zorluk)
        {
            return sorular.FindAll(s => s.zorluk == zorluk);
        }

        public List<QuestionData> SorulariGetir(DersKategorisi kategori)
        {
            return sorular.FindAll(s => s.kategori == kategori);
        }

        public List<QuestionData> SorulariGetir(ZorlukSeviyesi zorluk, DersKategorisi kategori)
        {
            return sorular.FindAll(s => s.zorluk == zorluk && s.kategori == kategori);
        }
    }
}
