# KeyBrawler - Dalamud eklentisi


## 0.12: coklu yaratik ve mavi baloncuk duzeltmesi

Ayni arayuzde birden fazla cevap varsa hepsini eleme sorunu duzeltildi. Her gorunur metin dugumu ayri bir cevap olarak alinir; ayni dugumdeki birden fazla kelime bolunmez. Yuklu tum KTGEWord arayuzleri taranir. Ayni konumdaki ayni metni cizen kopyalar tek sayilir. Mavi/mor baloncuklar Mavi olarak taninir, kirmizi/sari/yesilden sonra islenir; belirsiz renk de cevap gonderimini engellemez.

## Baloncuklari ayri oku, skill isimlerini alma

**Oyun baloncuk metnini oku (onerilen)** secenegi varsayilan olarak aciktir; mevcut kurulumlarda da etkinlesir. Yalnizca gorunur `KTGEWord` arayuzlerinden tam cevap okunur. `KTGEnemyCastBar`, HP bari, chat ve giris kutusu cevap kaynagi degildir. Ust uste gelen dusmanlarin cevaplari birlestirilmez; harfler OCR ile tahmin edilmez. Ekran yakalama sadece renk onceligi icin kullanilir. 1500 ms varsayilan onay araligi ve ASCII donusumu korunur.

Metin dugumunun tamami secilen bolgede olmali. Arayuz bulunamazsa veya metin gecersizse tahmin etmeden bekler; genel OCR'ye kendiliginden gecmez. Onizleme gorunur arayuz/metin dugumu/kabul edilen cevap sayilarini gosterir. Hic cevap yoksa bu sayilari paylasin. Yazmaya baslamadan once cevabin hala bulundugu tekrar kontrol edilir. Windows/FFXIV icindeki yeni arayuz okumasinin gercek oyunda denenmesi gerekir.

Eski OCR icin bu secenegi kapatabilirsiniz. Eski modda satir birlestirmesi daha secicidir, ancak ust uste binmis yazilar ve skill adlari yine hataya neden olabilir. Paylastiginiz goruntuler icin oyun baloncugu modu onerilir.


FFXIV Keybound Brawler minioyunundaki secilen bolgede bulunan baloncuk metnini oyun arayuzunden okur; eski Windows OCR modu da secilebilir. **Bir baloncugun metnini yazar, Enter'a basar, ekrani yeniden okur ve sonraki baloncuga gecer.** Tum baloncuklari tek metinde toplamaz. Baloncuk bolgesi oyun icindeki secim katmaniyla belirlenir; yazi hedefi otomatik olarak minioyunun `KTGInput` arayuzudur. Harici secim penceresi veya Python gerektirmez.

[English](README.en.md)

0.10 surumunden beri **English** ve **Turkce (ASCII)** arayuzleri eklendi. F7 ile ayarlari acip en ustten dil secin; secim kaydedilir ve arayuze hemen uygulanir. Uygulamanin kendi Turkce etiket, durum ve hata metinleri ASCII kullanir. Windows kaynakli dis hata ayrintilari isletim sisteminin dilinde olabilir.

**Oyuna yazilan cevaplar da duzeltildi.** OCR'deki Turkce harf bicimleri, hem klavye olaylari hem dogrudan metin yolunda ASCII karsiliklarina cevrilir. Buyuk/kucuk harf korunur; normal Ingilizce kelimeler degismez. OCR onizlemesi gonderilecek duzeltilmis metni gosterir. Arayuz dili oyundaki cevabi tercume etmez. OCR once `en-US` modelini, yoksa kullanici profili modelini dener. Klavye duzeni yine Turkce harf uretirse, sadece ASCII'ye donusturulunce beklenen kelimeyle tam eslesen alan duzeltilir; farkli kelime ezilmez.

Renk onceligi **kirmizi -> sari -> yesil -> mavi -> diger/belirsiz** sirasindadir. Renk onceligi varsayilan olarak aciktir ve ayarlardan kapatilabilir. Yeni dil ve metin duzeltmeleri Windows/FFXIV icinde henuz dogrulanmadi.

**Onaylar arasi varsayilan aralik 1500 ms'dir.** 0.9'dan onceki ayarlardan ilk geciste onceki kaydedilmis aralik da 1500 ms'ye tasinir; sonrasinda ayarlardan degistirilebilir. Bu aralik veya diger otomasyon ayarlari ban riskini ortadan kaldirmayi garanti etmez.

**Gecici sorunlarda eklenti kendini kapatmaz.** OCR hatasinda bekleyip yeniden dener. Oyun odagi kaybolursa eklentinin bastigi tuslari birakir, giris gondermeden oyunun one gelmesini bekler ve sonra devam eder. Minioyun girisi hazir degilse veya baloncuk yoksa tarama/bekleme surer. Yarim veya mevcut metin varsa onu ezmez; kutunun bosalmasini bekler. Gerekirse metni elle temizleyin. Tam durdurma F7, F8, F9, Durdur dugmesi veya eklentiyi kapatma ile yapilir. Yazma testi tek seferlik oldugu icin bitince kendi test oturumunu sonlandirir. Yeni renk ve bekleme davranislari Windows/FFXIV icinde henuz dogrulanmadi.

**Klavye olaylariyla yaz (tarama kodlari)** varsayilan olarak aciktir. Metni `SetText` ile dogrudan yerlestirmek yerine harfler Windows `SendInput` tarama kodlariyla basilip birakilir. Her karakterin basma ve birakmasi ayri karelere yayilir. Aktif Windows klavye duzeni ve Caps Lock dikkate alinir; eslenemeyen bir karakter varsa ilk harf gonderilmeden hata verilir. Yazi alani yine yalnizca `KTGInput` olabilir; butun metin geri okunarak dogrulanmadan onay gonderilmez. F9, odak kaybi veya hata durumunda eklentinin bastigi karakter ve modifier tuslari birakilir. Yazarken Shift/Ctrl/Alt tuslarina basmayin. Yanlis metin olusursa temizleyin; gerekirse bu secenegi kapatip onceki dogrudan metin yontemini deneyin.

**Son OCR turu** panelde ekran yakalama, yardimci surec ve OCR yanitini iceren sureyi milisaniye olarak gosterir. Ilk tur yardimci programin acilmasini icerdigi icin daha yavas olabilir. Sonraki turlar da yuksekse secilen alani yalnizca baloncuklari kapsayacak kadar kucultun; paneli OCR bolgesinin disinda tutun. Klavye modunda toplam cevap suresi kelime uzunluguna ve oyunun kare hizina da baglidir.

Hareketsizlik kontrolu artik `ContentInputTimer`, `InputTimer` ve pozitif `AfkTimer` degerlerini yeniler. Negatif AFK/away durumu ezilmez. Gorunur minioyun arayuzu varken gorev/ajan tanimlarindaki hatalar bu korumayi engellemez. Panel, korumanin acik/kapali/hatali oldugunu ve canli yerel sayaclari gosterir; bu gostergeler sunucunun atmayi engellediginin kaniti degildir. Yerel sayac degisiklikleri veya simule klavye olaylari oyunun tum etkinlik kontrollerini gecmeyi garanti etmez.

## Onceki surumlerden guncelleme

`/xlplugins` uzerinden **0.12.0.0** surumune guncelleyin. Eklentiyi yeniden yukleyin. 0.3 veya daha eskisinden geciyorsaniz oyunu bir kez yeniden baslatin; eski WinRT kaydi eklentiyi yeniden yuklemekle temizlenmeyebilir. Yeni pencerede **KeyBrawler 0.12** yazmali. ZIP ile kurulumda yalnizca DLL'yi degistirmeyin: `OcrWorker/KeyBrawler.OcrWorker.exe` alt klasoru de bulunmalidir. Yardimci program paket icinde kendi .NET calisma zamaniyla gelir; ayrica Python veya .NET kurmaniz gerekmez.

**Sohbet acmaniz gerekmez. F7 paneli acar ve calismayi durdurur; F8 baslatir/durdurur; F9 acil durdurur.** Baslatinca ayarlar kucuk bir kontrol paneline donusur; durum, son OCR aday sayisi, gonderim sayisi ve Durdur dugmesi gorunur kalir. Paneli secilen OCR bolgesinin disina surukleyin; aksi halde kendi yazilari OCR'ye girebilir. Panel gorunurken islem devam eder. Hatalarda kontrol paneli acik kalir ve bekleme nedenini gosterir; elle durdurmada ayar penceresi acilir.

**Bolgeyi yaratiklar veya baloncuklar cikmadan secip Baslat'a basabilirsiniz.** Minioyun henuz algilanmiyorsa bekler; bu durum eklentiyi durdurmaz. Giris alani etkin degilken de ekrani okur, ancak cevap gondermez. Bos OCR sonucu hata sayilmaz: "Baloncuk bekleniyor" durumunda yeniden tarar. Yeni dalgalar geldiginde ve minioyun girisi etkin oldugunda taze bir ekran okumasiyla devam eder.

**Minioyunda hareketsizlik korumasi** ve **Giris alanini Enter ile otomatik ac** varsayilan olarak aciktir. Koruma, eklenti F8 ile calistirildiginda her 10 saniyede yerel giris ve pozitif AFK sayaclarini sifirlar. Bunun icin Keybound Brawler gorev turu veya oyuna ozel gorunur `KTGInput`/HUD arayuzu bulunmali ve oyun onde olmalidir. Tek basina ajan bayragi yeterli sayilmaz. Karakteri hareket ettirmez; zaman asimi sinirlarini veya otomatik AFK ayarini degistirmez. F7/F9 veya ayarlari acmak korumayi durdurur; oyun odagi kaybinda koruma/giris duraklar ve odak donunce devam eder. Minioyun algilanmiyorsa koruma uygulanmaz. **Hareketsizlik sayaci yenilemeleri** gostergesi yerel yenilemeleri sayar; sunucunun atmayi engellediginin kaniti degildir.

Giris alani kapali veya etkin degilse otomatik acma secenegi Enter'a kisa sure basip birakir; yalnizca minioyunun aktif oldugu dogrulaninca calisir. Sohbet veya baska bir yazi alani odaktaysa Enter gondermez: once o alani kapatin. Otomatik acmayi kapatirsaniz eklenti durmadan bekler; giris icin Enter'a kendiniz basin. Giris acildiktan sonra ekran yeniden okunur; bekleme sirasinda eski OCR metni gonderilmez.

Once minioyunun yazi asamasini acin. **Hedef teshisi** ile `KTGInput` arayuzunun ve metin bileseninin bulundugunu kontrol edin. **Sadece OCR onizleme** ile baloncuklarin okunabildigini dogrulayin, ardindan **F8** ile baslatin. **Yazma testi** yalnizca minioyun alanina yazmayi kontrol eder; OCR dogrulamasi degildir. Hata varsa `/xllog` icinde **KeyBrawler** kaydi hata turunu, kodunu ve tam ayrintilari icerir.

## Kurulum - derleme gerekmez

Windows 10/11 ve **Dalamud API 15** kullanan XIVLauncher gerekir. Oyunu **Borderless Windowed** modunda calistirin. Eski OCR modunu kullanacaksaniz Windows'ta Ingilizce OCR bilesenini yukleyin. Oyun baloncugu modunda OCR dili gerekmez.

1. Oyunda `/xlsettings` acin.
2. **Experimental / Deneysel -> Custom Plugin Repositories** bolumune su adresi ekleyip etkinlestirin:

   ```text
   https://raw.githubusercontent.com/ekotutar35-cyber/keybrawler/main/repo.json
   ```

3. Ayarlari kaydedin. `/xlplugins` listesini yenileyin; **KeyBrawler** aratip yukleyin.
4. **F7** ile ayar penceresini acin. `/keybrawler` komutu da kullanilabilir.

API surumu uyusmuyorsa zorla yuklemeyin; eklentinin guncel Dalamud surumune gore yeniden derlenmesi gerekir.

### Guncelleme gorunmuyor veya yeniden yuklenmiyorsa

`/xlsettings -> Experimental -> Custom Plugin Repositories` bolumunden eski `.../pluginmaster.json` satirini kaldirin ve yukaridaki **repo.json** adresini ekleyip etkinlestirin. Iki adresi birlikte birakmayin. Ayarlari kaydedin, eklenti listesini yenileyin; gerekirse oyunu tamamen kapatip yeniden acin. Listede **KeyBrawler 0.12.0.0** gorunmeli. ZIP baglantilari artik yonlendirmesiz, surume ozel `packages/KeyBrawler-0.12.0.0.zip` adresini kullanir. Eski depo listesi onbelleginin farkli bir ZIP surumuyle eslesmesi boylece onlenir. Kurulum yine basarisizsa Dalamud'un tam hata mesaji gerekir.

### Alternatif: ZIP ile gelistirme eklentisi olarak yukleme

[KeyBrawler 0.12 ZIP indir](https://raw.githubusercontent.com/ekotutar35-cyber/keybrawler/main/packages/KeyBrawler-0.12.0.0.zip). ZIP'i kalici bir klasore cikarin; DLL'leri ayni klasorde tutun. `/xlsettings -> Experimental -> Dev Plugin Locations` bolumune cikarilan **KeyBrawler.dll dosyasinin tam yolunu** ekleyin. `/xlplugins` icindeki gelistirme eklentileri bolumunden yukleyin. Bu yolu normal oyun eklentileri klasorune elle kopyalamayin.

## Kullanim

1. **Baloncuk bolgesini oyun icinde sec** dugmesine basin, yalnizca baloncuklari cevreleyen alani surukleyin. Yazi alanini bu bolgenin disinda tutun. Esc secimi iptal eder.
2. Yazi alani koordinati secmeniz gerekmez. Eklenti yalnizca minioyunun `KTGInput` metin alanina yazar; giris hazir degilse bekler. Isterseniz **Hedef teshisi** ile algilama ayrintilarini goruntuleyin.
3. **Sadece OCR onizleme** ile taninan metinleri kontrol edin. Bu islem tus gondermez. Bir baloncuktaki alt alta satirlari birlestirmek icin piksel araligini ayarlayin. Ayri baloncuklar birlesiyorsa araligi kucultun.
4. Isterseniz **Yazma testi: test yaz** dugmesini deneyin. Kucuk panel gorunur kalir; 3 saniye sonra etkin minioyun kutusuna yazilir. Otomatik acma aciksa alani etkinlestirmek icin Enter kullanilir; testi **onaylamak icin** Enter gonderilmez. Test OCR kullanmaz. Basariliysa minioyun kutusunda `test` gormelisiniz. Bu metni elle temizleyin.
5. **F8** veya **Baslat** ile calistirin. Baloncuklar henuz cikmamis olabilir; eklenti bekler. Kontrol paneli gorunur kalir ve metinler hazir oldugunda sirayla gonderilir. Dolu giris alani bosalana kadar mevcut metni ezmeden bekler.
6. **F7**, **F9**, kontrol panelindeki **Durdur** veya `/keybrawler stop` ile durdurun. F8 baslat/durdur yapar. Oyun odagi kaybolunca tuslari birakip bekler, odak donunce devam eder. Ayarlar acilir ve eklentinin basili tuttugu tuslar birakilir.

Ekran cozunurlugu, pencere konumu veya oyun arayuz olcegi degisirse secimleri yenileyin. Eklenti her yuklemede kapali baslar. Yeniden baslatmak daha once gonderilen metinlerin listesini temizler.

## Bilinen sinirlar

- Baloncuk cercevesi algilamaz; OCR satirlarinin yakinligina gore gruplar. Yakin veya ust uste baloncuklarda yanlis gruplama olabilir. Onizlemeyle kontrol edin.
- Ayni metin bir calisma oturumunda bir kez gonderilir; iki ayri baloncuk ayni metni iceriyorsa ikincisi atlanir.
- Varsayilan olarak Windows klavye tarama kodlari kullanir; klavye secenegi kapaliysa `AtkComponentTextInput.SetText` kullanir. Metni yazmadan ve onaylamak icin Enter'a basmadan once alanin `KTGInput` arayuzune ait oldugunu dogrular. Minioyun alanina yazilan metin geri okunarak dogrulanmadan onay gonderilmez; mevcut metni ezmez. Hedef degisirse tuslari birakip bekler. Oyun guncellemelerinden sonra FFXIVClientStructs uyumu gereklidir. Farkli bir minioyun giris bileseni kullaniliyorsa "Hedef teshisi" ciktisiyla ek uyarlama gerekir; sohbete geri donmez.
- Enter ve istege bagli fare tiklamasi Windows `SendInput` kullanir; basma/birakma ayri karelerde gerceklesir. "Oyun alaninda metin dogrulandi; Enter gonderildi" mesaji metin alaninin icerigini dogrular, oyunun yaniti dogru kabul ettigini dogrulamaz. Onay sonrasi alan bosalmiyorsa sonraki yazma islemi alan bosalana kadar bekletilir.
- Yanlis giris alanina tiklanmasi veya OCR hatasi yanlis metin gonderilmesine neden olabilir. Durdurma/odak kaybi sirasinda yarim metin kalirsa elle temizleyin. Panoyu veya mevcut giris alanini otomatik temizlemez.
- Windows OCR once `en-US` dilini, bulunamazsa kullanici profilini dener. Profil diliniz baloncuk diline uymuyorsa OCR sonucunu kontrol edin. OCR yerel bir yardimci surecte yapilir; goruntuler ve metinler sunucuya gonderilmez. Yardimci program eklenti kapaninca sonlandirilir; yanit vermeyen surec 30 saniye sonra kapatilir.
- Bu eklenti oyun girisini otomatiklestirir; oyun kurallari acisindan hesap riski tasiyabilir. Koruma atlatma icermez.

## Kaynaktan derleme

.NET 10 SDK ve XIVLauncher uzerinden yuklenmis Dalamud gelistirme DLL'leri gerekir. Windows PowerShell:

```powershell
dotnet build .\Dalamud\KeyBrawler\KeyBrawler.csproj -c Release
dotnet run --project .\Dalamud\KeyBrawler.Tests -c Release
```

Indirilebilir ZIP ve ozel depo listesini birlikte guncellemek icin Windows PowerShell'de `& .\Dalamud\Package.ps1` calistirin.

SDK normalde `%APPDATA%\XIVLauncher\addon\Hooks\dev` yolunu kullanir. Farkli bir kurulum icin `DALAMUD_HOME` degiskenini Dalamud DLL klasorune ayarlayin. Tam kurulum paketi uretmek icin **Package.ps1** gereklidir; ciktisi deponun kokundeki `KeyBrawler-Dalamud.zip` dosyasidir. SDK'nin varsayilan `latest.zip` ciktisi OCR yardimci programini icermez ve tek basina yeterli degildir. `WinRT.Runtime.dll` ve `Microsoft.Windows.SDK.NET.dll` eklenti surecine yuklenmez; yardimci EXE'nin icindedir.

Eski Python Windows uygulamasi: [WINDOWS-APP.md](WINDOWS-APP.md).
