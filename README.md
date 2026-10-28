# KeyBrawler — Dalamud eklentisi

FFXIV içindeki bir ekran bölgesinden Windows OCR ile metin okur. **Bir baloncuğun metnini yazar, Enter'a basar, ekranı yeniden okur ve sonraki baloncuğa geçer.** Tüm baloncukları tek metinde toplamaz. Bölge ve yazı kutusu oyun içindeki seçim katmanıyla belirlenir; harici pencere veya Python gerektirmez.

Deneysel sürüm: Dalamud API 15'e karşı derlendi. Windows/FFXIV içinde yükleme, OCR ve metin kabulü henüz doğrulanmadı. Bu depodaki paket resmi Dalamud eklenti listesinde değildir.

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
2. **Oyundaki yazı alanını seç** düğmesine basın, hedef boş giriş kutusuna tıklayın. Bu seçim tıklaması oyuna iletilmez.
3. **Sadece OCR önizleme** ile tanınan metinleri kontrol edin. Bu işlem tuş göndermez. Bir baloncuktaki alt alta satırları birleştirmek için piksel aralığını ayarlayın. Ayrı baloncuklar birleşiyorsa aralığı küçültün.
4. Giriş alanını boş bırakın. **Başlat** veya **F8** ile çalıştırın. Ayar penceresi kapanır; metinler sırayla gönderilir.
5. **F9** veya `/keybrawler stop` ile durdurun. F8 başlat/durdur yapar. Oyun odağını kaybedince durur. `/keybrawler` ile pencere açılırken yeni tuş gönderimi bekletilir.

Ekran çözünürlüğü, pencere konumu veya oyun arayüz ölçeği değişirse seçimleri yenileyin. Eklenti her yüklemede kapalı başlar. Yeniden başlatmak daha önce gönderilen metinlerin listesini temizler.

## Bilinen sınırlar

- Baloncuk çerçevesi algılamaz; OCR satırlarının yakınlığına göre gruplar. Yakın veya üst üste baloncuklarda yanlış gruplama olabilir. Önizlemeyle kontrol edin.
- Aynı metin bir çalışma oturumunda bir kez gönderilir; iki ayrı baloncuk aynı metni içeriyorsa ikincisi atlanır.
- Unicode `SendInput` kullanır. Dalamud içinde çalışması, FFXIV'nin bu tuşları kabul edeceğini garanti etmez. Kayıttaki “Windows'a gönderildi” mesajı oyun içi kabul onayı değildir. Gönderim sonrası oyun durumunu doğrulayan bir mekanizma henüz yoktur.
- Yanlış giriş alanına tıklanması veya OCR hatası yanlış metin gönderilmesine neden olabilir. Durdurma/odak kaybı sırasında yarım metin kalırsa elle temizleyin. Panoyu veya mevcut giriş alanını otomatik temizlemez.
- Windows OCR tanıma dilini kullanıcı profilinden seçer; bulunamazsa `en-US` dener. Profil diliniz baloncuk diline uymuyorsa OCR sonucunu kontrol edin.
- Bu eklenti oyun girişini otomatikleştirir; oyun kuralları açısından hesap riski taşıyabilir. Koruma atlatma içermez.

## Kaynaktan derleme

.NET 10 SDK ve XIVLauncher üzerinden yüklenmiş Dalamud geliştirme DLL'leri gerekir. Windows PowerShell:

```powershell
dotnet build .\Dalamud\KeyBrawler\KeyBrawler.csproj -c Release
dotnet run --project .\Dalamud\KeyBrawler.Tests -c Release
```

İndirilebilir ZIP ve özel depo listesini birlikte güncellemek için Windows PowerShell'de `& .\Dalamud\Package.ps1` çalıştırın.

SDK normalde `%APPDATA%\XIVLauncher\addon\Hooks\dev` yolunu kullanır. Farklı bir kurulum için `DALAMUD_HOME` değişkenini Dalamud DLL klasörüne ayarlayın. Paket `Dalamud/KeyBrawler/bin/Release/KeyBrawler/latest.zip` altında oluşur. WinRT bağımlılıkları paketin içinde bulunmalıdır.

Eski Python Windows uygulaması: [WINDOWS-APP.md](WINDOWS-APP.md).
