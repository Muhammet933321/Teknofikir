using System;
using System.Collections.Generic;

namespace QuizGame.Data
{
    /// <summary>
    /// Bir öğrenciyi temsil eden veri sınıfı.
    /// </summary>
    [Serializable]
    public class StudentData
    {
        public string id;          // Benzersiz ID (GUID)
        public string ad;          // Öğrenci adı
        public string soyad;       // Öğrenci soyadı
        public string ogrenciNo;   // Öğrenci numarası
        public string sinifId;     // Bağlı olduğu sınıfın ID'si

        public StudentData()
        {
            id = Guid.NewGuid().ToString();
        }

        public StudentData(string ad, string soyad, string ogrenciNo, string sinifId)
        {
            this.id = Guid.NewGuid().ToString();
            this.ad = ad;
            this.soyad = soyad;
            this.ogrenciNo = ogrenciNo;
            this.sinifId = sinifId;
        }

        public string TamAd => $"{ad} {soyad}";
    }

    /// <summary>
    /// Bir sınıfı temsil eden veri sınıfı.
    /// </summary>
    [Serializable]
    public class ClassData
    {
        public string id;              // Benzersiz ID (GUID)
        public string sinifAdi;        // Sınıf adı (ör: "5-A")
        public List<StudentData> ogrenciler = new List<StudentData>();

        public ClassData()
        {
            id = Guid.NewGuid().ToString();
        }

        public ClassData(string sinifAdi)
        {
            this.id = Guid.NewGuid().ToString();
            this.sinifAdi = sinifAdi;
        }

        public void OgrenciEkle(StudentData ogrenci)
        {
            ogrenci.sinifId = this.id;
            ogrenciler.Add(ogrenci);
        }

        public bool OgrenciSil(string ogrenciId)
        {
            return ogrenciler.RemoveAll(o => o.id == ogrenciId) > 0;
        }
    }

    /// <summary>
    /// Tüm sınıf verilerini tutan ana veri yapısı (JSON serialization için).
    /// </summary>
    [Serializable]
    public class SchoolDatabase
    {
        public List<ClassData> siniflar = new List<ClassData>();

        public ClassData SinifBul(string sinifId)
        {
            return siniflar.Find(s => s.id == sinifId);
        }

        public ClassData SinifEkle(string sinifAdi)
        {
            var yeniSinif = new ClassData(sinifAdi);
            siniflar.Add(yeniSinif);
            return yeniSinif;
        }

        public bool SinifSil(string sinifId)
        {
            return siniflar.RemoveAll(s => s.id == sinifId) > 0;
        }
    }
}
