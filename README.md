# KeyBrawler — Dalamud eklentisi

FFXIV içindeki bir ekran bölgesinden Windows OCR ile metin okur. **Bir baloncuğun metnini yazar, Enter'a basar, ekranı yeniden okur ve sonraki baloncuğa geçer.** Tüm baloncukları tek metinde toplamaz. Bölge ve yazı kutusu oyun içindeki seçim katmanıyla belirlenir; harici pencere veya Python gerektirmez.

Deneysel sürüm 0.4: Dalamud API 15'e karşı derlendi. Kullanıcı, 0.3'te yazma testinin kutuya `test` yazdığını doğruladı; OCR'de “Attempt to update previously set global instance” hatası bildirdi. Bu sürüm Windows OCR'yi ayrı yardımcı süreçte çalıştırarak oyunun COM/WinRT kaydıyla çakışmasını önler. Yeni OCR yolunun Windows/FFXIV içinde çalışması henüz doğrulanmadı. Bu depodaki paket resmi Dalamud eklenti listesinde değildir.

## 0.3'ten güncelleme

`/xlplugins` üzerinden **0.4.0.0** sürümüne güncelleyin. Eklentiyi yeniden yükleyin ve oyunu bir kez yeniden başlatın; önceki sürümün süreç genelindeki WinRT kaydı eklentiyi yeniden yüklemekle temizlenmeyebilir. Yeni pencerede **KeyBrawler 0.4** yazmalı. ZIP ile kurulumda yalnızca DLL'yi değiştirmeyin: `OcrWorker/KeyBrawler.OcrWorker.exe` alt klasörü de bulunmalıdır. Yardımcı program paket içinde kendi .NET çalışma zamanıyla gelir; ayrıca Python veya .NET kurmanız gerekmez.

Önce **Sadece OCR önizleme** ile baloncukların okunabildiğini doğrulayın, ardından boş oyun kutusu odaktayken **F8** ile başlatın. **Yazma testi** yalnızca oyun alanına yazmayı kontrol eder; OCR doğrulaması değildir. Hata varsa `/xllog` içinde **KeyBrawler** kaydı hata türünü, kodunu ve tam ayrıntıları içerir.

## Kurulum — derleme gerekmez

Windows 10/11 ve **Dalamud API 15** kullanan XIVLauncher gerekir. Oyunu **Borderless Windowed** modunda çalıştırın. Windows'ta bir OCR dili kurulu olmalı; İngilizce baloncuklar için İngilizce dilinin OCR bileşenini yükleyin.

1. Oyunda `/xlsettings` açın.
2. **Experimental / Deneysel → Custom Plugin Repositories** bölümüne şu adresi ekleyip etkinleştirin:

   ```text
   https://raw.githubusercontent.com/ekotutar35-cyber/keybrawler/main/pluginmaster.json
   ```

3. Ayarları kaydedin. `/xlplugins` listesini yenileyin; **KeyBrawler** aratıp yükleyin.
4. `/keybrawler` komutuyla ayar penceresini açın.

API sürümü uyuşmuyorsa zorla yüklemeyin; eklentinin güncel Dalamud sürümüne göre yeniden derlenmesi gerekir.

### Alternatif: ZIP ile geliştirme eklentisi olarak yükleme

[KeyBrawler-Dalamud.zip indir](https://github.com/ekotutar35-cyber/keybrawler/raw/refs/heads/main/KeyBrawler-Dalamud.zip). ZIP'i kalıcı bir klasöre çıkarın; DLL'leri aynı klasörde tutun. `/xlsettings → Experimental → Dev Plugin Locations` bölümüne çıkarılan **KeyBrawler.dll dosyasının tam yolunu** ekleyin. `/xlplugins` içindeki geliştirme eklentileri bölümünden yükleyin. Bu yolu normal oyun eklentileri klasörüne elle kopyalamayın.

## Kullanım

1. **Baloncuk bölgesini oyun içinde seç** düğmesine basın, yalnızca baloncukları çevreleyen alanı sürükleyin. Yazı alanını bu bölgenin dışında tutun. Esc seçimi iptal eder.
2. Varsayılan olarak otomatik tıklama kapalıdır: oyundaki boş yazı kutusuna kendiniz tıklayın. Otomatik tıklama istiyorsanız **Oyundaki yazı alanını seç** ile hedefi seçin ve **Her baloncuktan önce seçilen alana tıkla** seçeneğini açın. Tıklama kutuyu aktif/deaktif ediyorsa kapalı bırakın.
3. **Sadece OCR önizleme** ile tanınan metinleri kontrol edin. Bu işlem tuş göndermez. Bir baloncuktaki alt alta satırları birleştirmek için piksel aralığını ayarlayın. Ayrı baloncuklar birleşiyorsa aralığı küçültün.
4. Önce **Yazma testi: test yaz** düğmesini deneyin. Pencere kapanır; boş oyun kutusunu odaklamak için 3 saniyeniz vardır. Test OCR kullanmaz ve Enter göndermez. Başarılıysa kutuda `test` görmelisiniz. Bu metni elle temizleyin.
5. Boş oyun kutusu odaktayken **F8** ile çalıştırın. Ayar penceresi kapanır; metinler sırayla gönderilir. Alan odakta değilse veya boş değilse eklenti hata gösterip durur.
6. **F9** veya `/keybrawler stop` ile durdurun. F8 başlat/durdur yapar. Oyun odağını kaybedince durur. `/keybrawler` ile ayar penceresini açmak da çalışmayı durdurur ve basılı tuşları bırakır.

Ekran çözünürlüğü, pencere konumu veya oyun arayüz ölçeği değişirse seçimleri yenileyin. Eklenti her yüklemede kapalı başlar. Yeniden başlatmak daha önce gönderilen metinlerin listesini temizler.

## Bilinen sınırlar

- Baloncuk çerçevesi algılamaz; OCR satırlarının yakınlığına göre gruplar. Yakın veya üst üste baloncuklarda yanlış gruplama olabilir. Önizlemeyle kontrol edin.
- Aynı metin bir çalışma oturumunda bir kez gönderilir; iki ayrı baloncuk aynı metni içeriyorsa ikincisi atlanır.
- Metin için `AtkComponentTextInput.SetText` kullanır. Oyun alanına yazılan metin geri okunarak doğrulanmadan Enter'a basılmaz. Boş ve odaktaki desteklenen metin kutusu gerekir; mevcut metni ezmez. Odaktaki metin kutusu değişirse durur. Oyun güncellemelerinden sonra FFXIVClientStructs uyumu gereklidir.
- Enter ve isteğe bağlı fare tıklaması Windows `SendInput` kullanır; basma/bırakma ayrı karelerde gerçekleşir. “Oyun alanında metin doğrulandı; Enter gönderildi” mesajı metin alanının içeriğini doğrular, oyunun yanıtı doğru kabul ettiğini doğrulamaz. Onay sonrası alan boşalmıyorsa sonraki yazma işlemi durur.
- Yanlış giriş alanına tıklanması veya OCR hatası yanlış metin gönderilmesine neden olabilir. Durdurma/odak kaybı sırasında yarım metin kalırsa elle temizleyin. Panoyu veya mevcut giriş alanını otomatik temizlemez.
- Windows OCR tanıma dilini kullanıcı profilinden seçer; bulunamazsa `en-US` dener. Profil diliniz baloncuk diline uymuyorsa OCR sonucunu kontrol edin. OCR yerel bir yardımcı süreçte yapılır; görüntüler ve metinler sunucuya gönderilmez. Yardımcı program eklenti kapanınca sonlandırılır; yanıt vermeyen süreç 30 saniye sonra kapatılır.
- Bu eklenti oyun girişini otomatikleştirir; oyun kuralları açısından hesap riski taşıyabilir. Koruma atlatma içermez.

## Kaynaktan derleme

.NET 10 SDK ve XIVLauncher üzerinden yüklenmiş Dalamud geliştirme DLL'leri gerekir. Windows PowerShell:

```powershell
dotnet build .\Dalamud\KeyBrawler\KeyBrawler.csproj -c Release
dotnet run --project .\Dalamud\KeyBrawler.Tests -c Release
```

İndirilebilir ZIP ve özel depo listesini birlikte güncellemek için Windows PowerShell'de `& .\Dalamud\Package.ps1` çalıştırın.

SDK normalde `%APPDATA%\XIVLauncher\addon\Hooks\dev` yolunu kullanır. Farklı bir kurulum için `DALAMUD_HOME` değişkenini Dalamud DLL klasörüne ayarlayın. Tam kurulum paketi üretmek için **Package.ps1** gereklidir; çıktısı deponun kökündeki `KeyBrawler-Dalamud.zip` dosyasıdır. SDK'nın varsayılan `latest.zip` çıktısı OCR yardımcı programını içermez ve tek başına yeterli değildir. `WinRT.Runtime.dll` ve `Microsoft.Windows.SDK.NET.dll` eklenti sürecine yüklenmez; yardımcı EXE'nin içindedir.

Eski Python Windows uygulaması: [WINDOWS-APP.md](WINDOWS-APP.md).
