using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Globalization;

namespace KrediFaizHesaplayicisi
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBox2.Items.Clear();
            comboBox2.SelectedItem = null; 
            comboBox2.Text = ""; 

            if (comboBox1.SelectedItem == null) return; 

            string krediTuru = comboBox1.SelectedItem.ToString();

            if (krediTuru == "İhtiyaç Kredisi")
            {
                string[] taksitSecenekleri = { "3", "6", "12", "24", "36" };
                comboBox2.Items.AddRange(taksitSecenekleri);
            }
            else if (krediTuru == "Taşıt Kredisi")
            {
                string[] taksitSecenekleri = { "12", "24", "36", "48" };
                comboBox2.Items.AddRange(taksitSecenekleri);
            }
            else if (krediTuru == "Konut Kredisi")
            {
                string[] taksitSecenekleri = { "24", "36", "48", "60", "120" };
                comboBox2.Items.AddRange(taksitSecenekleri);
            }
        }

        
        private bool GetFaizOrani(string krediTuru, int taksitSayisi, bool customFaizChecked, string customFaizText, out decimal faizOrani)
        {
            faizOrani = 0;

            if (customFaizChecked)
            {
                if (!decimal.TryParse(customFaizText.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out faizOrani))
                {
                    MessageBox.Show("Geçerli Bir Faiz Oranı Giriniz !", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                if (faizOrani > 1 && faizOrani <= 100) // Genellikle faiz %100'den fazla olmaz, ama bu bir varsayım.
                {
                    faizOrani /= 100.0m; 
                }
                else if (faizOrani > 100) // Çok yüksek bir değer girilirse hata verilebilir.
                {
                    MessageBox.Show("Faiz oranı çok yüksek görünüyor. Lütfen kontrol edin.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
            else
            {
                // CheckBox işaretli değilse, sabit faiz oranlarını kullanıyoruz
                switch (krediTuru)
                {
                    case "İhtiyaç Kredisi":
                        switch (taksitSayisi)
                        {
                            case 3: faizOrani = 0.0150m; break;
                            case 6: faizOrani = 0.0133m; break;
                            case 12: faizOrani = 0.0117m; break;
                            case 24: faizOrani = 0.0100m; break;
                            case 36: faizOrani = 0.0100m; break;
                            default:
                                MessageBox.Show("İhtiyaç kredisi için geçersiz taksit sayısı.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return false;
                        }
                        break;
                    case "Taşıt Kredisi":
                        switch (taksitSayisi)
                        {
                            case 12: faizOrani = 0.0117m; break;
                            case 24: faizOrani = 0.0100m; break;
                            case 36: faizOrani = 0.0092m; break;
                            case 48: faizOrani = 0.0083m; break;
                            default:
                                MessageBox.Show("Taşıt kredisi için geçersiz taksit sayısı.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return false;
                        }
                        break;
                    case "Konut Kredisi":
                        switch (taksitSayisi)
                        {
                            case 24: faizOrani = 0.0100m; break;
                            case 36: faizOrani = 0.0083m; break;
                            case 48: faizOrani = 0.0075m; break;
                            case 60: faizOrani = 0.0075m; break;
                            case 120: faizOrani = 0.0067m; break;
                            default:
                                MessageBox.Show("Konut kredisi için geçersiz taksit sayısı.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return false;
                        }
                        break;
                    default:
                        MessageBox.Show("Geçersiz kredi türü.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                }
            }
            return true;
        }

        //Eşit Taksitli ödeme planı için aylık taksit hesaplama
        private decimal CalculateAnnuityPayment(decimal principal, decimal monthlyInterestRate, int numberOfMonths)
        {
            if (numberOfMonths <= 0) return 0; // Geçersiz taksit sayısı
            if (principal <= 0) return 0; // Geçersiz anapara

            if (monthlyInterestRate == 0) // Faizsiz kredi durumu
            {
                return principal / numberOfMonths;
            }

            
            decimal factor = (decimal)Math.Pow(1 + (double)monthlyInterestRate, numberOfMonths);
            if (factor - 1 == 0) return principal; // Faiz oranı çok düşükse ve factor 1'e yakınsa taksit anaparaya eşit olabilir (pratikte zor)

            decimal monthlyPayment = principal * (monthlyInterestRate * factor) / (factor - 1);
            return monthlyPayment;
        }


        private void button1_Click(object sender, EventArgs e) // Hesapla Butonu
        {
            listBox1.Items.Clear();

            if (comboBox1.SelectedItem == null || string.IsNullOrEmpty(textBox1.Text) || comboBox2.SelectedItem == null)
            {
                MessageBox.Show("Lütfen kredi türünü, tutarını ve taksit sayısını seçiniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(textBox1.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal krediTutari) || krediTutari <= 0)
            {
                MessageBox.Show("Geçerli bir kredi tutarı giriniz.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string krediTuru = comboBox1.SelectedItem.ToString();
            int taksitSayisi = Convert.ToInt32(comboBox2.SelectedItem.ToString());
            decimal aylikFaizOrani;

            if (!GetFaizOrani(krediTuru, taksitSayisi, checkBox1.Checked, textBox2.Text, out aylikFaizOrani))
            {
                return; // Faiz oranı alınamazsa işlemi durdur
            }

            decimal aylikTaksitTutari = CalculateAnnuityPayment(krediTutari, aylikFaizOrani, taksitSayisi);
            decimal toplamGeriOdeme = aylikTaksitTutari * taksitSayisi; 

            listBox1.Items.Add("Kredi Türü: " + krediTuru);
            listBox1.Items.Add("Kredi Tutarı: " + krediTutari.ToString("N2", CultureInfo.GetCultureInfo("tr-TR")) + " TL");
            listBox1.Items.Add("Taksit Sayısı: " + taksitSayisi + " Ay");
            listBox1.Items.Add("Aylık Faiz Oranı: %" + (aylikFaizOrani * 100).ToString("N2", CultureInfo.GetCultureInfo("tr-TR")));
            listBox1.Items.Add("Aylık Taksit Tutarı : " + aylikTaksitTutari.ToString("N2", CultureInfo.GetCultureInfo("tr-TR")) + " TL"); 
            listBox1.Items.Add("Toplam Geri Ödenecek Tutar : " + toplamGeriOdeme.ToString("N2", CultureInfo.GetCultureInfo("tr-TR")) + " TL");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            textBox2.Enabled = false;
        }

        private void button2_Click(object sender, EventArgs e) // Döküman Oluştur Butonu
        {
            if (comboBox1.SelectedItem == null || string.IsNullOrEmpty(textBox1.Text) || comboBox2.SelectedItem == null)
            {
                MessageBox.Show("Lütfen önce hesaplama yapmak için kredi türünü, tutarını ve taksit sayısını seçiniz ve 'Hesapla' butonuna basınız.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(textBox1.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal krediTutari) || krediTutari <= 0)
            {
                MessageBox.Show("Geçerli bir kredi tutarı giriniz.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string krediTuru = comboBox1.SelectedItem.ToString();
            int taksitSayisi = Convert.ToInt32(comboBox2.SelectedItem.ToString());
            decimal aylikFaizOrani;

            if (!GetFaizOrani(krediTuru, taksitSayisi, checkBox1.Checked, textBox2.Text, out aylikFaizOrani))
            {
                return; // Faiz oranı alınamazsa işlemi durdur
            }

            // Standart aylık taksiti hesapla
            decimal standartAylikTaksit = CalculateAnnuityPayment(krediTutari, aylikFaizOrani, taksitSayisi);

            try
            {
                string dosyaAdi = "ÖdemePlanı_" + krediTuru.Replace(" ", "_") + "_" + DateTime.Now.ToString("dd/MM/yyyy") + ".csv";
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.FileName = dosyaAdi;
                saveFileDialog.Filter = "CSV Dosyası (*.csv)|*.csv|Metin Dosyası (*.txt)|*.txt";
                saveFileDialog.DefaultExt = "csv";
                saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    dosyaAdi = saveFileDialog.FileName;
                    using (StreamWriter sw = new StreamWriter(dosyaAdi, false, Encoding.UTF8))
                    {
                        sw.WriteLine("Ay;Ödeme Tarihi;Aylık Taksit;Anapara Ödemesi;Faiz Ödemesi;Kalan Anapara");

                        DateTime odemeBaslangicTarihi = DateTime.Now.AddMonths(1);
                        decimal kalanAnapara = krediTutari;
                        CultureInfo cultureTr = CultureInfo.GetCultureInfo("tr-TR");

                        decimal toplamOdenenTaksit = 0m;
                        decimal toplamOdenenAnapara = 0m;
                        decimal toplamOdenenFaiz = 0m;

                        for (int ay = 1; ay <= taksitSayisi; ay++)
                        {
                            DateTime odemeTarihi = odemeBaslangicTarihi.AddMonths(ay - 1);
                            decimal buAykiFaizTutari = Math.Round(kalanAnapara * aylikFaizOrani, 2, MidpointRounding.AwayFromZero); // Faizi yuvarla

                            decimal anlikAylikTaksit;
                            decimal buAykiAnaparaOdeme;

                            if (ay == taksitSayisi)
                            {
                                // Son taksitte anapara ödemesi kalan anaparadır.
                                buAykiAnaparaOdeme = kalanAnapara;
                                // Son taksit, kalan anapara ve bu ayki faiz toplamıdır.
                                anlikAylikTaksit = buAykiAnaparaOdeme + buAykiFaizTutari;
                            }
                            else
                            {
                                anlikAylikTaksit = standartAylikTaksit;
                                buAykiAnaparaOdeme = anlikAylikTaksit - buAykiFaizTutari;
                            }

                            // Toplamları biriktir (yuvarlanmış değerler üzerinden değil, hesaplananlar üzerinden)
                            // Ancak CSV'ye yazılan değerlerin toplamı olmalı, bu yüzden yuvarlanmış faizle anapara hesaplanmalı
                            // Eğer faiz yuvarlandıysa, anapara da buna göre ayarlanmalı
                            if (ay != taksitSayisi)
                            { // Son taksit zaten özel hesaplanıyor
                                buAykiAnaparaOdeme = standartAylikTaksit - buAykiFaizTutari;
                            }


                            toplamOdenenTaksit += anlikAylikTaksit;
                            toplamOdenenAnapara += buAykiAnaparaOdeme;
                            toplamOdenenFaiz += buAykiFaizTutari;

                            kalanAnapara -= buAykiAnaparaOdeme;

                            // Kalan anapara çok küçük bir negatif/pozitif değere düşerse sıfırla (ondalık hesaplama ve yuvarlama hataları nedeniyle)
                            if (ay == taksitSayisi || (kalanAnapara < 0.01m && kalanAnapara > -0.01m))
                            {
                                kalanAnapara = 0m;
                            }

                            sw.WriteLine($"{ay};" +
                                         $"{odemeTarihi.ToString("dd.MM.yyyy", cultureTr)};" +
                                         $"{anlikAylikTaksit.ToString("N2", cultureTr)};" +
                                         $"{buAykiAnaparaOdeme.ToString("N2", cultureTr)};" +
                                         $"{buAykiFaizTutari.ToString("N2", cultureTr)};" +
                                         $"{kalanAnapara.ToString("N2", cultureTr)}");
                        }

                        
                        // Toplam faiz = toplam ödenen taksit - kredi tutarı
                        decimal gercekToplamFaiz = toplamOdenenTaksit - krediTutari;


                        sw.WriteLine($"TOPLAM;;{toplamOdenenTaksit.ToString("N2", cultureTr)};{krediTutari.ToString("N2", cultureTr)};{gercekToplamFaiz.ToString("N2", cultureTr)};");
                    }
                    MessageBox.Show($"Ödeme planı başarıyla '{Path.GetFileName(dosyaAdi)}' olarak kaydedildi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Dosya oluşturulurken bir hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            textBox2.Enabled = checkBox1.Checked;
            if (!checkBox1.Checked)
            {
                textBox2.Clear();
            }
            else
            {
                textBox2.Focus();
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
        }

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '.' || e.KeyChar == ',')
            {
                if (textBox2.Text.Contains(".") || textBox2.Text.Contains(",") || (textBox2.TextLength == 0 && (e.KeyChar == '.' || e.KeyChar == ',')))
                {
                    e.Handled = true;
                }
                if (e.KeyChar == ',') e.KeyChar = '.'; // Gelen virgülü noktaya çevir 
            }
            else if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true; 
            }
        }
    }
}
