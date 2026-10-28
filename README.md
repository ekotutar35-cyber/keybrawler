# KeyBrawler — Dalamud eklentisi

FFXIV Keybound Brawler minioyunundaki bir ekran bölgesinden Windows OCR ile metin okur. **Bir baloncuğun metnini yazar, Enter'a basar, ekranı yeniden okur ve sonraki baloncuğa geçer.** Tüm baloncukları tek metinde toplamaz. Baloncuk bölgesi oyun içindeki seçim katmanıyla belirlenir; yazı hedefi otomatik olarak minioyunun `KTGInput` arayüzüdür. Harici seçim penceresi veya Python gerektirmez.

Deneysel sürüm 0.7: Dalamud API 15'e karşı derlendi. 0.6'da eklenen görev/ajan koşulu, gerçek minioyun alanı hazır olsa bile yazmayı durdurabiliyordu. Bu sürüm bu zorunlu koşulu kaldırır: etkin `KTGInput` bulunduğunda önceki yazma yolu çalışır. Destekleyici algılama veya hareketsizlik koruması hata verirse bu hata tek başına yazmayı durdurmaz. Yeni akışın Windows/FFXIV içinde çalışması henüz doğrulanmadı. Bu depodaki paket resmi Dalamud eklenti listesinde değildir.

## Önceki sürümlerden güncelleme

`/xlplugins` üzerinden **0.7.0.0** sürümüne güncelleyin. Eklentiyi yeniden yükleyin. 0.3 veya daha eskisinden geçiyorsanız oyunu bir kez yeniden başlatın; eski WinRT kaydı eklentiyi yeniden yüklemekle temizlenmeyebilir. Yeni pencerede **KeyBrawler 0.7** yazmalı. ZIP ile kurulumda yalnızca DLL'yi değiştirmeyin: `OcrWorker/KeyBrawler.OcrWorker.exe` alt klasörü de bulunmalıdır. Yardımcı program paket içinde kendi .NET çalışma zamanıyla gelir; ayrıca Python veya .NET kurmanız gerekmez.

**Sohbet açmanız gerekmez. F7 paneli açar ve çalışmayı durdurur; F8 başlatır/durdurur; F9 acil durdurur.** Başlatınca ayarlar küçük bir kontrol paneline dönüşür; durum, son OCR aday sayısı, gönderim sayısı ve Durdur düğmesi görünür kalır. Paneli seçilen OCR bölgesinin dışına sürükleyin; aksi halde kendi yazıları OCR'ye girebilir. Panel görünürken işlem devam eder. Hata veya durdurma durumunda ayar penceresi açılır.

**Bölgeyi yaratıklar veya baloncuklar çıkmadan seçip Başlat'a basabilirsiniz.** Minioyun henüz algılanmıyorsa bekler; bu durum eklentiyi durdurmaz. Giriş alanı etkin değilken de ekranı okur, ancak cevap göndermez. Boş OCR sonucu hata sayılmaz: “Baloncuk bekleniyor” durumunda yeniden tarar. Yeni dalgalar geldiğinde ve minioyun girişi etkin olduğunda taze bir ekran okumasıyla devam eder.

**Minioyunda hareketsizlik koruması** ve **Giriş alanını Enter ile otomatik aç** varsayılan olarak açıktır. Koruma, eklenti F8 ile çalıştırıldığında her 10 saniyede yerel `ContentInputTimer` ve `InputTimer` sayaçlarını sıfırlar. Bunun için Keybound Brawler görev türü veya oyuna özel görünür `KTGInput`/HUD arayüzü bulunmalı ve oyun önde olmalıdır. Tek başına ajan bayrağı yeterli sayılmaz. Karakteri hareket ettirmez; zaman aşımı sınırlarını veya otomatik AFK ayarını değiştirmez. F7/F9, ayarları açmak veya oyun odağını kaybetmek korumayı durdurur. Minioyun algılanmıyorsa koruma uygulanmaz. **Hareketsizlik sayacı yenilemeleri** göstergesi yerel yenilemeleri sayar; sunucunun atmayı engellediğinin kanıtı değildir.

Giriş alanı kapalı veya etkin değilse otomatik açma seçeneği Enter'a kısa süre basıp bırakır; yalnızca minioyunun aktif olduğu doğrulanınca çalışır. Sohbet veya başka bir yazı alanı odaktaysa Enter göndermez: önce o alanı kapatın. Otomatik açmayı kapatırsanız eklenti durmadan bekler; giriş için Enter'a kendiniz basın. Giriş açıldıktan sonra ekran yeniden okunur; bekleme sırasında eski OCR metni gönderilmez.

Önce minioyunun yazı aşamasını açın. **Hedef teşhisi** ile `KTGInput` arayüzünün ve metin bileşeninin bulunduğunu kontrol edin. **Sadece OCR önizleme** ile baloncukların okunabildiğini doğrulayın, ardından **F8** ile başlatın. **Yazma testi** yalnızca minioyun alanına yazmayı kontrol eder; OCR doğrulaması değildir. Hata varsa `/xllog` içinde **KeyBrawler** kaydı hata türünü, kodunu ve tam ayrıntıları içerir.

## Kurulum — derleme gerekmez

Windows 10/11 ve **Dalamud API 15** kullanan XIVLauncher gerekir. Oyunu **Borderless Windowed** modunda çalıştırın. Windows'ta bir OCR dili kurulu olmalı; İngilizce baloncuklar için İngilizce dilinin OCR bileşenini yükleyin.

1. Oyunda `/xlsettings` açın.
2. **Experimental / Deneysel → Custom Plugin Repositories** bölümüne şu adresi ekleyip etkinleştirin:

   ```text
   https://raw.githubusercontent.com/ekotutar35-cyber/keybrawler/main/repo.json
   ```

3. Ayarları kaydedin. `/xlplugins` listesini yenileyin; **KeyBrawler** aratıp yükleyin.
4. **F7** ile ayar penceresini açın. `/keybrawler` komutu da kullanılabilir.

API sürümü uyuşmuyorsa zorla yüklemeyin; eklentinin güncel Dalamud sürümüne göre yeniden derlenmesi gerekir.

### Güncelleme görünmüyor veya yeniden yüklenmiyorsa

`/xlsettings → Experimental → Custom Plugin Repositories` bölümünden eski `.../pluginmaster.json` satırını kaldırın ve yukarıdaki **repo.json** adresini ekleyip etkinleştirin. İki adresi birlikte bırakmayın. Ayarları kaydedin, eklenti listesini yenileyin; gerekirse oyunu tamamen kapatıp yeniden açın. Listede **KeyBrawler 0.7.0.0** görünmeli. ZIP bağlantıları artık yönlendirmesiz, sürüme özel `packages/KeyBrawler-0.7.0.0.zip` adresini kullanır. Eski depo listesi önbelleğinin farklı bir ZIP sürümüyle eşleşmesi böylece önlenir. Kurulum yine başarısızsa Dalamud'un tam hata mesajı gerekir.

### Alternatif: ZIP ile geliştirme eklentisi olarak yükleme

[KeyBrawler 0.7 ZIP indir](https://raw.githubusercontent.com/ekotutar35-cyber/keybrawler/main/packages/KeyBrawler-0.7.0.0.zip). ZIP'i kalıcı bir klasöre çıkarın; DLL'leri aynı klasörde tutun. `/xlsettings → Experimental → Dev Plugin Locations` bölümüne çıkarılan **KeyBrawler.dll dosyasının tam yolunu** ekleyin. `/xlplugins` içindeki geliştirme eklentileri bölümünden yükleyin. Bu yolu normal oyun eklentileri klasörüne elle kopyalamayın.

## Kullanım

1. **Baloncuk bölgesini oyun içinde seç** düğmesine basın, yalnızca baloncukları çevreleyen alanı sürükleyin. Yazı alanını bu bölgenin dışında tutun. Esc seçimi iptal eder.
2. Yazı alanı koordinatı seçmeniz gerekmez. Eklenti yalnızca minioyunun `KTGInput` metin alanına yazar; giriş hazır değilse bekler. İsterseniz **Hedef teşhisi** ile algılama ayrıntılarını görüntüleyin.
3. **Sadece OCR önizleme** ile tanınan metinleri kontrol edin. Bu işlem tuş göndermez. Bir baloncuktaki alt alta satırları birleştirmek için piksel aralığını ayarlayın. Ayrı baloncuklar birleşiyorsa aralığı küçültün.
4. İsterseniz **Yazma testi: test yaz** düğmesini deneyin. Küçük panel görünür kalır; 3 saniye sonra etkin minioyun kutusuna yazılır. Otomatik açma açıksa alanı etkinleştirmek için Enter kullanılır; testi **onaylamak için** Enter gönderilmez. Test OCR kullanmaz. Başarılıysa minioyun kutusunda `test` görmelisiniz. Bu metni elle temizleyin.
5. **F8** veya **Başlat** ile çalıştırın. Baloncuklar henüz çıkmamış olabilir; eklenti bekler. Kontrol paneli görünür kalır ve metinler hazır olduğunda sırayla gönderilir. Dolu giriş alanına yazmaya çalışılırsa mevcut metni ezmek yerine hata gösterir.
6. **F7**, **F9**, kontrol panelindeki **Durdur** veya `/keybrawler stop` ile durdurun. F8 başlat/durdur yapar. Oyun odağını kaybedince durur. Ayarlar açılır ve eklentinin basılı tuttuğu tuşlar bırakılır.

Ekran çözünürlüğü, pencere konumu veya oyun arayüz ölçeği değişirse seçimleri yenileyin. Eklenti her yüklemede kapalı başlar. Yeniden başlatmak daha önce gönderilen metinlerin listesini temizler.

## Bilinen sınırlar

- Baloncuk çerçevesi algılamaz; OCR satırlarının yakınlığına göre gruplar. Yakın veya üst üste baloncuklarda yanlış gruplama olabilir. Önizlemeyle kontrol edin.
- Aynı metin bir çalışma oturumunda bir kez gönderilir; iki ayrı baloncuk aynı metni içeriyorsa ikincisi atlanır.
- Metin için `AtkComponentTextInput.SetText` kullanır. Metni yazmadan ve onaylamak için Enter'a basmadan önce alanın `KTGInput` arayüzüne ait olduğunu doğrular. Minioyun alanına yazılan metin geri okunarak doğrulanmadan onay gönderilmez; mevcut metni ezmez. Hedef değişirse durur. Oyun güncellemelerinden sonra FFXIVClientStructs uyumu gereklidir. Farklı bir minioyun giriş bileşeni kullanılıyorsa “Hedef teşhisi” çıktısıyla ek uyarlama gerekir; sohbete geri dönmez.
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
