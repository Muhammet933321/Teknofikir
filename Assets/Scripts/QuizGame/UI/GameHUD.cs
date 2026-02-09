using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace QuizGame.UI
{
    /// <summary>
    /// Oyun sırasında can barlarını, soru sayacını ve oyuncu bilgilerini gösteren HUD.
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        [Header("═══ Panel ═══")]
        [SerializeField] private GameObject hudPanel;

        [Header("═══ Oyuncu 1 Bilgileri (Sol) ═══")]
        [SerializeField] private TextMeshProUGUI oyuncu1AdText;
        [SerializeField] private TextMeshProUGUI oyuncu1SinifText;
        [SerializeField] private Image[] oyuncu1CanIkonlari;
        [SerializeField] private TextMeshProUGUI oyuncu1SkorText;

        [Header("═══ Oyuncu 2 Bilgileri (Sağ) ═══")]
        [SerializeField] private TextMeshProUGUI oyuncu2AdText;
        [SerializeField] private TextMeshProUGUI oyuncu2SinifText;
        [SerializeField] private Image[] oyuncu2CanIkonlari;
        [SerializeField] private TextMeshProUGUI oyuncu2SkorText;

        [Header("═══ Orta Bilgiler ═══")]
        [SerializeField] private TextMeshProUGUI soruSayaciText;
        [SerializeField] private TextMeshProUGUI turBilgiText;

        [Header("═══ Renkler ═══")]
        [SerializeField] private Color canDoluRenk = Color.red;
        [SerializeField] private Color canBosRenk = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        public void Goster()
        {
            if (hudPanel != null) hudPanel.SetActive(true);
        }

        public void Gizle()
        {
            if (hudPanel != null) hudPanel.SetActive(false);
        }

        public void OyuncuBilgileriniAyarla(string oyuncu1Ad, string oyuncu1Sinif,
                                             string oyuncu2Ad, string oyuncu2Sinif)
        {
            if (oyuncu1AdText != null) oyuncu1AdText.text = oyuncu1Ad;
            if (oyuncu1SinifText != null) oyuncu1SinifText.text = oyuncu1Sinif;
            if (oyuncu2AdText != null) oyuncu2AdText.text = oyuncu2Ad;
            if (oyuncu2SinifText != null) oyuncu2SinifText.text = oyuncu2Sinif;
        }

        public void CanlariGuncelle(int oyuncuIndex, int mevcutCan)
        {
            Image[] ikonlar = oyuncuIndex == 0 ? oyuncu1CanIkonlari : oyuncu2CanIkonlari;
            if (ikonlar == null) return;

            for (int i = 0; i < ikonlar.Length; i++)
            {
                if (ikonlar[i] != null)
                    ikonlar[i].color = (i < mevcutCan) ? canDoluRenk : canBosRenk;
            }
        }

        public void SkoruGuncelle(int oyuncuIndex, int dogru, int yanlis)
        {
            if (oyuncuIndex == 0 && oyuncu1SkorText != null)
                oyuncu1SkorText.text = $"Doğru: {dogru} | Yanlış: {yanlis}";
            else if (oyuncuIndex == 1 && oyuncu2SkorText != null)
                oyuncu2SkorText.text = $"Doğru: {dogru} | Yanlış: {yanlis}";
        }

        public void SoruSayaciniGuncelle(int mevcutSoru, int toplamSoru)
        {
            if (soruSayaciText != null)
                soruSayaciText.text = $"Soru: {mevcutSoru}/{toplamSoru}";
        }

        public void TurBilgisiniGuncelle(string bilgi)
        {
            if (turBilgiText != null)
                turBilgiText.text = bilgi;
        }
    }
}
