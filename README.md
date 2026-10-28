# KeyBrawler

Windows için FFXIV ekranındaki metin baloncuklarını OCR ile okuyup seçilen yazı alanına **bir baloncuk, bir Enter** sırasıyla gönderen deneysel uygulama. Dalamud gerektirmez. Oyun belleğini okumaz.

## Kurulum

Windows 10/11, 64 bit Python 3.11 veya 3.12 gereklidir. PowerShell'de bu klasörde:

```powershell
py -3.11 -m venv .venv
.\.venv\Scripts\python.exe -m pip install -r requirements.txt
.\.venv\Scripts\python.exe app.py
```

## Kullanım

1. FFXIV'yi ana ekranda **Borderless Windowed** veya **Windowed** moda alın. Exclusive fullscreen ekran seçimini veya yakalamayı engelleyebilir.
2. Baloncukların bulunduğu alanı sürükleyerek seçin. Giriş kutusunu bu bölgenin dışında tutun.
3. Oyundaki boş yazı kutusunu seçin. Uygulama ve oyun aynı Windows yetki düzeyinde çalışmalı.
4. Oyuna odaklanın ve F8 ile başlatın. F8 duraklatır, F9 durdurur. İlk OCR yüklemesi zaman alabilir.
5. Bir baloncuk gönderilip Enter'a basıldıktan sonra ekran yeniden okunur. Aynı metin o çalışma oturumunda tekrar gönderilmez. F8 ile yeniden başlatmak geçmişi sıfırlar.

Seçim katmanı görünmüyorsa koordinatları elle girme düğmesini kullanın. Ana ekranın sol üst köşesi `0,0` kabul edilir. Koordinatları girdikten sonra oyunu hemen öne getirin; hedef pencere 1,5 saniye sonra belirlenir. Bu yöntem seçim katmanı gerektirmez; ekran yakalama engelleniyorsa bunu çözmez.

Satır birleştirme aralığı, aynı baloncuktaki alt alta satırların birleştirilmesini sağlar. Komşu baloncuklar birleşiyorsa değeri küçültün; `0` satırları ayrı tutar. OCR gerçek baloncuk sınırlarını algılamaz; yakın/üst üste baloncuklarda yanlış gruplama olabilir. Varsayılan model Latin metinler içindir; Japonca OCR bu sürümde hazırlanmadı.

## Sınırlar ve doğrulama

FFXIV'nin Unicode `SendInput` girişlerini kabul edeceği garanti değildir. Oyun tuşları reddederse uygulama bunu oyun içi kabul olarak doğrulayamaz. Kayıttaki “Gönderildi” Windows'a gönderimi ifade eder. Boş giriş kutusu kullanın; odak kaybı veya durdurma sırasında kısmi metin kalırsa elle temizleyin. Önce kısa bir metinle deneyin. Koruma atlatma veya oyun içine enjeksiyon içermez. Oyun otomasyonu kullanım koşulları açısından hesap riski taşıyabilir.

Bu sürüm Linux ortamında gruplama testleri ve Python derleme kontrolüyle doğrulanır; Windows ekran yakalama, pencere seçimi ve FFXIV içinde uçtan uca çalışma ayrıca denenmelidir.

```powershell
py -3.11 -m unittest discover -s tests -v
```
