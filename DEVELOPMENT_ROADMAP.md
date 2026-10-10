# Windows Password Manager — Development Roadmap

Sürüm: 1.0 • Tarih: 5 Ekim 2026 • Dil: Türkçe

Bu belge, Codex ile küçük ve doğrulanabilir adımlarla geliştirilecek Windows parola yöneticisinin ürün ve mühendislik planıdır. Henüz uygulama kodu yazılmamıştır. Buradaki kabul kriterleri gelecek çalışmanın koşullarıdır; geçmiş test sonuçları değildir. Proje adı kesinleşmediği için kod adı `PasswordManager` kullanılır. Repo kökündeki `AGENTS.md`, agent çalışma kurallarını tanımlar.

## 1. Ürün hedefi ve kapsam

Hedef: Windows 11 üzerinde şık bir ana uygulama, bildirim alanında hızlı erişim penceresi ve desteklenen uygulamalarda kullanıcı isteğiyle auto-fill sağlayan, önce yerel çalışan bir parola yöneticisi.

“System tray”, görev çubuğunun sağındaki bildirim alanıdır. İlk hedef tray ikonuna tıklanınca açılan özel mini penceredir; görev çubuğuna gömülü toolbar/deskband veya Windows kabuğunu değiştiren bileşen değildir.

### Kullanıcı deneyimi

- Ana pencerede sol gezinme, ortada kayıt listesi, sağda seçili hesabın ayrıntıları.
- Kayıt ekleme/düzenleme/silme; arama, favoriler, kategoriler ve parola üretimi.
- Tray penceresinde kilit durumu, açık kasada kayıt sayısı, uygulamalar, son kullanılanlar ve hızlı arama.
- Etkin uygulamaya uygun hesap önerileri; hesap ve hedef doğrulandıktan sonra kullanıcı tarafından başlatılan doldurma.
- Kasa kilitliyken hesap başlığı, kullanıcı adı, alan adı, kategori, son kullanılanlar ve kayıt sayısı gösterilmez. Kullanıcıya yalnızca kilit durumu ve açma eylemi sunulur.
- İlk sürüm internetsiz çalışır. Üçüncü taraf favicon/ikon servisleri kullanılmaz; hesap alan adları dışarı gönderilmez.

### Aşamalar

| Aşama | Sonuç | Kullanım sınırı |
| --- | --- | --- |
| A — M0–M3 | Güvenli çekirdek, modern UI, geri yüklenebilir yedek | Önce sentetik kayıtlar; M3 çıkışında kontrollü kişisel deneme |
| B — M4–M6 | Tray, test edilmiş Windows auto-fill, sağlamlaştırma | Kişisel desktop MVP; desteklenen uygulamalar açıkça listelenir |
| C — M7 | İsteğe bağlı Windows Hello | Anahtar koruması kanıtlanırsa etkinleştirilir |
| D — M8 | Edge/Chrome entegrasyonu | Ayrı test ve güvenlik inceleme kapısı |
| E — M9–M10 | Paketleme, bağımsız inceleme ve ürün hazırlığı | Kamuya dağıtım ancak yayın kriterlerinden sonra |
| F — M11+ | Opsiyonel şifreli senkronizasyon ve diğer platformlar | Ayrı ürün projesi; ilk sürümü bloke etmez |

Kartlar, TOTP, passkey yönetimi, dosya ekleri, paylaşım, ekip kasaları, mobil uygulama, Firefox ve cloud sync ilk sürüm kapsamında değildir. Domain modeli bunları zorlaştırmamalı; henüz gerekmeyen implementasyonlar yapılmamalıdır.

## 2. Teknik temel ve karar kapıları

| Alan | Başlangıç tercihi | Gerekçe ve doğrulama |
| --- | --- | --- |
| Runtime | .NET 10; başlangıçta güncel desteklenen yama | SDK `global.json` ile sabitlenir; WinUI araç zinciri M0'da gerçek Windows üzerinde denenir |
| UI | WinUI 3 + Windows App SDK + MVVM | Fluent görünüm; Mica yalnızca desteklenen ortamda, opak fallback ile |
| İlk OS hedefi | Desteklenen Windows 11 x64 | Minimum OS build ve test matrisi M0 ADR'sinde kesinleştirilir; ARM64 daha sonra |
| Depolama | SQLite + EF Core | Tek yerel kasa; SQLite dosyasının kendisinin tümü şifreli olduğu iddia edilmez |
| Kayıt şifreleme | .NET `AesGcm`, AES-256-GCM | 32 bayt anahtar, 12 bayt nonce, 16 bayt tag; protokol M1'de yazılır |
| Parola KDF | Argon2id | Paket seçimi bakım, lisans, test vektörleri ve platform desteğiyle değerlendirilir |
| Anahtar modeli | Rastgele kasa kök anahtarı + parola ile sarılmış anahtar | Master password değişiminde tüm kayıtları yeniden şifrelemeyi gerektirmez |
| Native entegrasyon | Sınırlı Win32 interop | Tray, pencere kimliği, session olayları ve clipboard için |
| Windows auto-fill | UI Automation + uygulama adapter'ları | Her uygulama/kontrol desteklenmez; varsayım yerine uyumluluk testi |
| Browser | Edge/Chrome extension + native host | Web sayfasının origin/frame bağlamı browser tarafından değerlendirilir |
| Dağıtım | MSIX adayı | Tray, native host kaydı, güncelleme ve veri yolları spike ile doğrulanır; gerekirse imzalı alternatif ADR |

Microsoft WinUI 3'ü yeni Windows uygulamaları için önerir [S1]. Bu projede seçim yine de M0 spike sonucuna bağlıdır. WinUI/tray/toolchain engeli çıkarsa agent sessizce WPF'ye geçmez; ölçülen sorunu ve WPF + Fluent alternatifini ADR olarak sunar.

Paketlerin tam sürümleri bu roadmap'ten tahmin edilmez. Başlangıçta resmî kaynaklar ve ortam üzerinden doğrulanır, merkezi paket yönetimiyle sabitlenir. Otomatik floating version veya gerekçesiz framework upgrade yoktur.

### Önerilen repository

```text
PasswordManager.sln
AGENTS.md
DEVELOPMENT_ROADMAP.md
README.md
global.json
Directory.Build.props
Directory.Packages.props
.editorconfig
src/
  PasswordManager.Domain/
  PasswordManager.Application/
  PasswordManager.Security/
  PasswordManager.Infrastructure/
  PasswordManager.Platform.Windows/
  PasswordManager.Autofill.Windows/
  PasswordManager.Desktop/
  PasswordManager.NativeHost/            # M8'de eklenir
extensions/
  chromium/                              # M8'de eklenir
tests/
  PasswordManager.Domain.Tests/
  PasswordManager.Application.Tests/
  PasswordManager.Security.Tests/
  PasswordManager.Infrastructure.Tests/
  PasswordManager.Windows.Tests/
  PasswordManager.EndToEnd.Tests/
test-apps/
  AutofillTestApp/                        # M5'te sentetik hedef
docs/
  PROJECT_STATUS.md
  THREAT_MODEL.md
  VAULT_FORMAT.md
  SECURITY_TEST_MATRIX.md
  MANUAL_TESTS.md
  AUTOFILL_COMPATIBILITY.md
  BACKUP_AND_RECOVERY.md
  adr/
```

M0'da ihtiyaç duyulan projeler açılır; NativeHost/extension boş scaffold olarak önceden eklenmez. Microservice, web API ve ayrı local server ilk sürüm için gerekli değildir.

### Bağımlılık yönü

Domain başka proje bilmez. Application, Domain'a ve kendi portlarına dayanır. Security, kriptografi portlarını uygular; UI/EF/Win32 bilmez. Infrastructure, Application'ın depolama portlarını uygular. Platform.Windows Windows servislerini sağlar. Autofill.Windows, Application'ın dar kapsamlı doldurma portlarına ve Windows adapter'larına dayanır; DB'ye veya anahtar deposuna erişmez. Desktop composition root olur; kullanıcı akışlarını Application üzerinden yürütür. NativeHost daha sonra IPC gateway olur; kasayı bağımsız açmaz.

Interface yalnızca gerçek bir sınır veya test gereği için eklenir. Her sınıfa repository/interface, erken generic framework veya gereksiz CQRS/MediatR zorunluluğu yoktur.

## 3. Tehdit modeli ve güvenlik sözleşmesi

### Korunan varlıklar

Master password, anahtarlar, kayıt payload'ları, hesap metadata'sı, yedekler, açık kasa oturumu ve auto-fill hedef doğruluğu.

### Güvenlik hedefleri

| Tehdit | Beklenen koruma | Sınır |
| --- | --- | --- |
| DB/yedek dosyasının çalınması | Şifreli payload ve maliyetli parola KDF'si | Zayıf master password offline tahmin edilebilir |
| Kayıt/header değiştirme | AEAD ve authenticated context doğrulama | Geçerli eski bir dosyanın geri konması yerel v1'de kesin olarak tespit edilemez |
| Kayıtların yer değiştirilmesi | VaultId/RecordId/type/version bağlı AAD | Tüm kasanın veya kaydın silinmesi availability saldırısıdır |
| Yanlış pencereye doldurma | Hedef bağlama ve son anda yeniden doğrulama | Ele geçirilmiş hedef uygulama kendisine verilen parolayı görebilir |
| Clipboard geçmişi | OS history/sync dışlama + koşullu temizleme | Üçüncü taraf clipboard izleyicilerini engelleme garantisi yok |
| Kilitli kasada UI bilgi sızıntısı | Cache, liste, arama ve pending işlerin temizlenmesi | OS/grafik belleğinin tüm kopyalarının silindiği garanti edilmez |
| Aynı kullanıcı altında malware/admin | Süre ve erişim yüzeyi azaltma | Açık kasa veya ele geçirilmiş OS için tam koruma iddiası yok |
| Bozuk/hostile import | Boyut/parametre sınırları, doğrulama, atomik değişim | Şifreleme sağlam olsa da availability ayrı korunmalıdır |

“RAM only” ifadesi plaintext'i uygulamanın bilerek diske yazmaması anlamına gelir. Managed string'ler, GC kopyaları, pagefile, hibernation ve crash dump nedeniyle hiç diske değmeyeceği garanti edilmez. BitLocker gibi OS disk koruması tamamlayıcıdır; uygulama bunu kendi şifrelemesinin yerine kullanmaz.

### M1'den itibaren zorunlu kurallar

1. Master password saklanmaz, loglanmaz, telemetry'ye veya clipboard'a gönderilmez. UTF-8 kodlama ve Unicode davranışı formatta tanımlanır; gizli trim/normalization yapılmaz.
2. Kayıt başlığı, kullanıcı adı, password, URL/domain, not, kategori, uygulama eşlemesi ve tarihler şifreli payload içindedir. Plaintext FTS/search index yoktur. Rastgele kayıt ID'leri, format/KDF bilgisi, blob uzunlukları ve fiziksel satır sayısı gibi metadata sızabileceği belgelenir.
3. Nonce her şifreleme için CSPRNG ile yeni üretilir. Aynı anahtar/nonce çifti tekrar kullanılamaz [S3]. Yeniden kayıt, retry, backup restore sonrası düzenleme ve key rotation senaryoları bu kurala dahildir.
4. Protokolün key separation, serialization ve AAD kuralları yazılmadan üretim şifreleme kodu tamamlanmış sayılmaz. Kendi şifreleme algoritması, XOR, ECB veya sessiz downgrade yoktur.
5. Auth tag hatasında veri kullanıma açılmaz. Kısmen çözülen kayıt UI'ya verilmez. Hata raporu plaintext içermez.
6. Tüm hassas operasyonlar oturum kimliği/generation ile bağlanır. Kilitleme pending decrypt/reveal/fill işlerini iptal eder; geç dönen sonuç yeni UI'ya uygulanamaz.
7. Loglar sadece güvenli event code, süre, genel sonuç gibi whitelist alanlardan oluşur. Entity/DTO destructuring, EF sensitive logging, SQL parametre dökümü ve secret içeren exception context yasaktır.
8. App normal kullanıcı olarak çalışır. UAC, secure desktop, OS sign-in ve yüksek yetkili hedeflerde doldurma ilk kapsamın dışındadır.
9. İstek olmadan auto-fill/submit yapılmaz. Parola üreten RNG kriptografik olmalıdır.
10. M0–M2 sentetik verilerle test edilir. M3 öncesi gerçek kayıt taşınmaz. M3 kapısı bir güvenlik sertifikası değildir; mevcut güvenilir password manager'daki tek kopya silinmez.

## 4. Kasa ve anahtar tasarımı

### Anahtar yaşam döngüsü

Master password + rastgele salt + kaydedilmiş Argon2id parametreleri → 32 bayt KEK (key-encryption key). CSPRNG ile bağımsız 32 bayt kasa kök anahtarı üretilir. KEK yalnızca bu kök anahtarı AEAD ile sarmak/açmak için kullanılır. Kök anahtardan standart HKDF-SHA-256 ile amaç etiketleri ve VaultId bağlamında record ve manifest anahtarları türetilir. Tam encoding ve label değerleri M1 `VAULT_FORMAT.md` içinde sabitlenir.

Kasa açılırken header sınırları doğrulanır, KEK türetilir, wrapped key doğrulanır, manifest çözülür; bundan sonra oturum aktif olur. Yanlış parola ile hasarlı authenticated header için ortak güvenli hata verilir. Ayrı plaintext master-password verifier veya KEK'in kalıcı saklanması yoktur.

Master password değişiminde mevcut kök anahtar yeni salt/KEK ile yeniden sarılır; tüm kayıtlar gereksiz yere yeniden şifrelenmez. Bu değişiklik, eski DB/yedek kopyasını iptal etmez. Gerçek anahtar kompromisinde ayrı root-key rotation gerekir; eski kopyaların saldırganda kalmasını geri alamaz.

### KDF politikası

Başlangıç benchmark adayı: Argon2id v1.3, 64 MiB, 3 iteration, 4 lane, en az 16 bayt salt ve 32 bayt çıktı. Bu RFC 9106'daki düşük bellek profilinden alınmış başlangıç noktasıdır [S2]; “her cihaz için yeterli” onayı değildir. Hedef cihazda açma süresi/bellek ölçülür; parametreler ADR'de sabitlenir. Yerel performans tercihi güvenlik tabanının altına sessizce inemez.

Dosya içinden okunan KDF parametreleri güvenilmezdir. KDF'yi çalıştırmadan önce minimum/maksimum bellek, iteration, parallelism, toplam dosya ve alan uzunluğu sınırları kontrol edilir. İçe aktarımda devasa KDF değerleriyle DoS engellenir. KDF upgrade ancak başarılı açılış ve doğrulanmış geri dönüş planıyla, yeni salt ve atomik header değişimiyle yapılır.

### Kalıcı format taslağı

| Yapı | Dışarıdaki alanlar | Şifreli içerik |
| --- | --- | --- |
| VaultHeader | Magic, format version, VaultId, cipher/KDF kimlikleri, bounded KDF parametreleri, salt, wrapped key nonce/tag/ciphertext | Kasa kök anahtarı |
| VaultManifest | Nonce, tag, ciphertext | Kasa tercihleri, kategori tanımları, beklenen kayıt ID/revision seti ve gerekli bütünlük metadata'sı |
| VaultRecord | RecordId, envelope version, nonce, tag, ciphertext | Tür, başlık, hesap alanları, domain/app bindings, favori, tarihler ve revision |

AAD; VaultId, RecordId (kayıt için), amaç etiketi ve envelope sürümünü kanonik binary encoding ile bağlar. Key-wrap AAD, salt/KDF/cipher/header bilgilerini de bağlar. Manifest + kayıt mutasyonları aynı SQLite transaction içinde yapılır. Manifest kayıt silme/substitution gibi bozulmaları tespit etmeyi hedefler; son geçerli kasanın tamamen geri alınmasını tespit ettiği iddia edilmez.

Rastgele 96-bit nonce politikası; key başına kullanım sınırı, çakışma davranışı ve root-key rotation prosedürüyle belgelenir. Bilinen nonce'lar için çakışma denetimi eklenebilir; eski snapshot'lar dahil matematiksel benzersizlik garantisi diye sunulmaz. Protokol bağımsız incelemede değerlendirilir.

SQLite yalnızca encrypted envelope'ları alır. EF entity'sine plaintext credential bağlanmaz. WAL/SHM/journal, geçici dosya, log ve backup'ta sentetik secret canary taraması yapılır. Fiziksel dosyada salt/nonce/tag görülmesi beklenir; bunlar parola değildir.

### Oturum ve concurrency

Durumlar: `NoVault`, `Locked`, `Unlocking`, `Unlocked`, `Locking`, `Faulted`. Unlock aynı anda birden fazla kez çalıştırılmaz. Sensitive operation scope, oturum generation ve cancellation kullanır. Lock önce yeni işi durdurur, devam eden işleri iptal eder ve UI'ya teslimatı keser; sonra key buffer ve cache'leri temizler. Mutasyonlar serialize edilir; uygulamanın iki instance'ı aynı kasaya bağımsız yazamaz.

Managed buffer'larda `CryptographicOperations.ZeroMemory` ve deterministic dispose kullanılır. Immutable string'lerin tüm kopyalarını temizlediği iddia edilen bir `SecureString` wrapper geliştirilmez. UI binding, reveal ve clipboard geçişlerinde kaçınılmaz kopyalar minimize edilir ve sınırlamalar yazılır.

## 5. Modern arayüz ve davranış standardı

Ana ekran: NavigationView + kayıt listesi + detail panel. Dar pencerede detail ayrı sayfaya döner. Unlock/onboarding, kayıt formu, generator, settings ve backup ekranları ayrı akışlardır.

- Tema: system/light/dark; tutarlı spacing, typography, corner radius ve semantic renk token'ları.
- İlk UI dili İngilizce olabilir; localization-ready resources kullanılır. Türkçe desteği ürün aşamasında planlanır. Kod identifier'ları İngilizce, proje belgeleri Türkçe.
- Klavye gezinmesi, visible focus, screen reader, high contrast, %100/%150/%200 DPI ve küçük ekran test edilir.
- Parola varsayılan masked. Reveal varsayılan 10 saniye; focus kaybı/kilit durumunda hemen kapanır. Accessibility tree'ye plaintext password adı/değeri eklenmez.
- Clipboard varsayılan 20 saniye. Temizleme yalnızca aynı clipboard sequence/ownership devam ediyorsa yapılır; kullanıcının sonraki kopyası silinmez.
- Windows clipboard history/cloud sync dışlama formatları kullanılır [S4]; üçüncü taraf logger koruması olarak sunulmaz.
- Auto-lock başlangıçta 5 dakika app inactivity; süre test edilebilir clock üzerinden yürür. System lock, suspend ve kullanıcı session değişimi kasa kilitler. Resume kilitli başlar.
- Pencere kapatma varsayılan kasayı kilitleyip tray'e küçültür. Exit ayrıca açık sunulur; startup opt-in'dir ve her zaman locked açılır.
- Generator preview/copy ve hassas form taslakları da lock event'inde temizlenir. Hassas açık metin formu autosave edilmez.

## 6. Milestone planı

Her alt görev tek küçük uygulama döngüsüdür. Sıra bağımlılığa göre izlenir; süre taahhüdü değil kabul kriteri esas alınır. Aşağıdaki checklist'ler başlangıçta beklemededir.

### M0 — Foundation ve teknik fizibilite

Amaç: hedef Windows ortamında açılan, araç zinciri ve native ihtiyaçları doğrulanmış bir başlangıç.

- [x] M0.1 Ürün kapsamı, OS/x64 hedefi, repo adı ve threat model v0 kaydedilir.
- [x] M0.2 SDK/VS/Windows SDK/Windows App SDK sürümleri gerçek ortamda doğrulanır; ADR-001 stack/toolchain yazılır.
- [x] M0.3 Solution, gerekli projeler, merkezi paket sürümleri, nullable/analyzers ve gitignore kurulur.
- [x] M0.4 WinUI shell; light/dark, NavigationView, sentetik list/detail ve resource token'ları hazırlanır.
- [x] M0.5 Native spike: tray icon + mini pencere, monitor/DPI konumu, foreground HWND yakalama ve session event aboneliği denenir. Spike üretim tamamlanması sayılmaz.
- [x] M0.6 SQLite dosya yolu/ACL ve tek instance davranışı; paketli/unpackaged veri yolu farkları belgelenir.
- [x] M0.7 Windows CI build/test temeli; cross-platform core tests ayrıca çalışabilir.

Çıkış: temiz Windows ortamında shell açılır; build ve temel smoke test geçer; SDK sürümleri, tray riski ve paketleme adayı belgelenir. Linux build sonucu WinUI doğrulaması yerine geçmez. Gerçek parola veya çalışıyormuş gibi mock security ekranı yoktur.

### M1 — Secure Vault çekirdeği

Amaç: şifreleme formatı ve create/unlock/lock yaşam döngüsü.

- [x] M1.1 Threat model v1, VAULT_FORMAT v1 ve key/KDF ADR'leri yazılır.
- [x] M1.2 Argon2id paket seçimi; RFC vektörleri ve benchmark; sınır doğrulama implementasyonu.
- [x] M1.3 CSPRNG, key wrap, HKDF separation, record/manifest AEAD implementasyonu.
- [x] M1.4 Encrypted envelope şeması ve EF migration; schema version ile crypto format version ayrılır.
- [ ] M1.5 Create/unlock/lock, state machine, operation scope, key dispose ve cancellation.
- [ ] M1.6 Başlangıç auto-lock/session-lock/suspend olayları; log whitelist.
- [ ] M1.7 Güvenlik testleri ve dosya canary taraması.

Çıkış: doğru parola açar; yanlış parola ve ciphertext/tag/AAD/header değiştirme kapalı kalır; ciphertext record/vault değişimi reddedilir; aynı plaintext yeniden yazılınca yeni nonce oluşur; malformed input KDF öncesi reddedilir. Lock sırasında gecikmiş sonuçlar UI'ya ulaşmaz. UI güvenlik çekirdeğini atlayamaz.

### M2 — Credential yönetimi ve güvenli UX

Amaç: modern ana ekran üzerinde tam hesap yönetimi.

- [ ] M2.1 Domain login modeli, kategori/favori ve payload schema version.
- [ ] M2.2 Ekle/güncelle/sil; parola boş mu/bilinçli boş mu davranışı; maksimum alan uzunlukları.
- [ ] M2.3 Açık kasada in-memory arama; lock'ta index ve sonuçların silinmesi.
- [ ] M2.4 Detail, masked reveal, ayrı username/password copy.
- [ ] M2.5 CSPRNG password generator; seçili karakter grupları, minimum koşullar, tarafsız seçim/rejection sampling.
- [ ] M2.6 Clipboard timeout/history dışlama, reveal timeout, güvenli notification metni.
- [ ] M2.7 Safe URL açma: yalnızca izinli http/https şemaları; command/file URI çalıştırma yok.
- [ ] M2.8 DPI/theme/accessibility ve empty/loading/error durumları.

Çıkış: restart sonrası sentetik kayıtlar korunur; kilitli durumda metadata görünmez; arama plaintext diske yazmaz; clipboard başka içerikle değişince cleanup onu silmez; reveal, edit formu ve generator lock ile temizlenir. Keyboard/screen reader/DPI testleri kaydedilir.

### M3 — Backup, restore ve veri dayanıklılığı

Amaç: kişisel kullanım öncesi geri yükleme ve atomik mutasyon güvencesi.

- [ ] M3.1 Encrypted snapshot backup formatı ve taşınabilir master-password key wrap.
- [ ] M3.2 SQLite online backup/consistent snapshot; yalnızca `.db` kopyalayıp WAL değişikliklerini kaybetme yok.
- [ ] M3.3 Import boyut/format/KDF sınırları; geçici alanda tüm authentication ve manifest doğrulaması.
- [ ] M3.4 Replace restore: mevcut kasa korunur, doğrulanmış snapshot atomik olarak değiştirilir. Merge import ertelenir.
- [ ] M3.5 Master password değişimi ve KDF upgrade; transaction/rollback testleri.
- [ ] M3.6 Migration öncesi şifreli yedek ve restore testi; unknown newer format salt okunur/ret davranışı.
- [ ] M3.7 Disk full, izin hatası, işlem kapanması, eksik dosya ve duplicate instance senaryoları.
- [ ] M3.8 BACKUP_AND_RECOVERY kılavuzu; restore drill başka test profili/cihazında.

Çıkış: doğru parola ile başka test ortamında bütün kayıtlar geri gelir; bozuk/yanlış parolalı import mevcut kasaya dokunmaz; iptal ve disk hatasında orijinal açılır. Yeni parola aktif kasayı açar, eski parola açamaz; önceki yedeğin eski parolayla açılmaya devam ettiği kullanıcıya açıklanır.

Recovery v1: master password unutulursa ve kasa zaten açılabilecek başka onaylı yöntem yoksa kurtarma yoktur. “Reset password” şifreleri kurtaran bir arka kapı değildir. Recovery key ileride ayrı key wrap ve ayrı threat review gerektirir. Yedek ile parola kurtarma aynı özellik değildir.

**Kişisel deneme kapısı:** M1–M3 testleri + session lock + canary taraması + başka ortamda restore başarılı. Bu kapıdan sonra sınırlı kişisel kayıt denenebilir; tek kopya bu uygulamada tutulmaz.

### M4 — System Tray mini vault

Amaç: ana uygulamayla aynı güvenlik oturumunu kullanan hızlı erişim.

- [ ] M4.1 Üretim tray lifecycle; Explorer restart, DPI değişimi ve instance aktivasyonu.
- [ ] M4.2 360–420 DIP genişlikte flyout adayı; ekran/taskbar konumuna göre clamp; erişilebilir focus/Escape.
- [ ] M4.3 Locked view; unlocked count, favorites/recent/search, copy, generator ve open vault.
- [ ] M4.4 Context suggestion için tray açılmadan önceki hedef pencerenin descriptor'ı yakalanır; tray'in kendisi hedef olmaz.
- [ ] M4.5 Kısayol kayıt/çakışma, startup opt-in, close-to-tray ve exit.
- [ ] M4.6 Tray ve ana pencere cache/state senkronizasyonu ve lock-race testleri.

Çıkış: tek oturum; tray'den lock ana pencereyi de temizler; Explorer yeniden başlayınca ikon geri gelir; farklı monitor/DPI/taskbar konumunda erişilebilir açılır. Unlock sonrası eski foreground hedef otomatik doldurulmaz, yeniden doğrulanır.

### M5 — Windows auto-fill: kontrollü pilot

Amaç: destek kanıtı olan uygulamalarda kullanıcı tetiklemeli doldurma.

- [ ] M5.1 Test-only WPF/Win32 hedef uygulama; standart alan, masked field, çoklu hesap, focus değişimi ve gecikme senaryoları.
- [ ] M5.2 TargetDescriptor: HWND, PID + process start identity, executable path, beklenen publisher/signature varsa, session/integrity, control descriptor ve capture time.
- [ ] M5.3 ApplicationBinding şifreli saklanır. Sadece `steam.exe` gibi basename veya window title güven kimliği değildir.
- [ ] M5.4 UI Automation kontrol capability tespiti; `ValuePattern` kullanılabiliyor mu gerçek hedefte ölçülür. Password field desteği varsayılmaz.
- [ ] M5.5 Fill Username / Fill Password / Fill Both; hesap ve hedef kullanıcıya gösterilir. Pending fill expires ve tek kullanımlı olur.
- [ ] M5.6 Kullanıcı hedefe döndüğünde foreground process/HWND/control yeniden doğrulanır. Mismatch, PID reuse, yeni dialog, lock veya cancellation işlemden vazgeçirir.
- [ ] M5.7 UIA çağrıları timeout/cancellation ve uygun thread modeliyle yürür; hang yapan provider UI thread'i bloklamaz.
- [ ] M5.8 Compatibility matrix: OS build, app version, field pattern, destek durumu ve son test tarihi.
- [ ] M5.9 Kullanıcının seçtiği ilk iki gerçek hedefte sentetik hesaplarla manuel pilot.

Fill Both tamamen atomik değildir. Username yazıldıktan sonra focus değişirse password gönderilmez; yarım işlem raporlanır. Her hassas yazma öncesi hedef yeniden doğrulanır. Hedef kontrolün parolayı okuma API'si sunması gerekmez; mevcut parola değerleri okunmaz. Otomatik Enter/submit yoktur.

İlk pilotta input injection fallback yoktur. Desteklenmeyen hedef için açık açıklama ve manuel copy seçeneği bulunur. SendInput daha sonra istenirse ayrı ADR ve failure-mode testleri gerekir; kör Tab/typing, administrator'a yükseltme veya UIAccess ile güvenlik sınırını aşma yapılmaz.

Microsoft bazı Windows authentication dialog'larında 2026 güvenlik güncellemeleriyle otomatik input kısıtlarını belgeler [S7]. Bunlar normal uygulama login ekranlarının hepsinin engellendiği anlamına gelmez; OS credential/sign-in dialog'ları bu ürünün kapsamından çıkarılır.

Çıkış: sentetik hedeflerde başarı; kötü hedef/focus yarışı/lock sırasında **sıfır password dispatch** kanıtı; seçilen uygulamalar için gerçek uyumluluk raporu. Steam/Discord/Visual Studio desteği test edilmeden vaat edilmez. Browser web form desteği bu milestone'a dahil değildir.

### M6 — Kişisel MVP hardening

Amaç: M1–M5'i günlük kullanımda tutarlı ve güvenilir hale getirmek.

- [ ] M6.1 Uzun çalışma ve lock/unlock döngüleri; cache ve subscription leak ölçümleri.
- [ ] M6.2 App close, sleep, resume, OS lock, session switch, restart ve crash testleri.
- [ ] M6.3 Clipboard/reveal/fill eşzamanlılık ve async navigation yarışları.
- [ ] M6.4 Crypto envelope/backup parser fuzz/property test; hostile KDF ve uzun alanlar.
- [ ] M6.5 Root-key rotation prosedürü/testleri; backup etkilerinin dokümantasyonu.
- [ ] M6.6 Dependency vulnerability/license incelemesi; logging ve dump politikası.
- [ ] M6.7 Local unlock retry gecikmesi, cancellation ve DoS davranışı. Bu özellik offline brute-force'u engelliyor diye sunulmaz.
- [ ] M6.8 Açılış/search/tray/unlock benchmark; sentetik 1.000 ve 10.000 kayıt; hedef donanım sonuçları.
- [ ] M6.9 Bilinen sınırlamalar ve bakım/backup rutini.

Çıkış: güvenlik test matrisi tamam; bilinen kritik/yüksek sorunlar çözülmüş; kişisel restore drill tekrarlanmış; desteklenen auto-fill matrisi mevcut. Windows Hello ve browser olmadan da personal desktop MVP tamamlanabilir.

### M7 — Opsiyonel Windows Hello

Amaç: master password alternatifi olarak cihazda kolay açılış; taşınabilir master-password yolunu koruma.

- [ ] M7.1 API feasibility spike ve ADR: consent ile key access arasındaki bağ nasıl kuruluyor?
- [ ] M7.2 Kullanıcı opt-in ve master-password ile enrollment; cihaz özel wrapper/protected key tasarımı.
- [ ] M7.3 Hello yok, iptal, lockout, PIN/biometric değişimi, TPM/user-profile değişimi, key reset ve fallback.
- [ ] M7.4 Disable/revoke, parola değişimi/key rotation sonrası cihaz yetkisinin yenilenmesi.
- [ ] M7.5 Başka cihaz backup restore'unda Hello materyali aktarılmaz; master password gerekir.

`UserConsentVerifier` bir doğrulama sonucudur [S5]. DPAPI CurrentUser ise aynı kullanıcı bağlamında çalışan süreçlere karşı Hello-gated cryptographic isolation garantisi sağlamaz [S6]. Sadece “Hello success → DPAPI decrypt” tasarımı bu sınırlamayla bir kolaylık özelliği olabilir; donanım korumalı vault unlock diye pazarlanamaz. Donanıma bağlı güçlü açılış hedefleniyorsa anahtar kullanımı gerçekten OS/Hello ile bağlanmalı ve uygun desteklenen API incelenmelidir. Standart, incelenmiş model mümkün değilse bu milestone ertelenir; imzadan kendi KDF/şifreleme tasarımı türetilmez.

Çıkış: threat model güncel; gerçekten kullanılan anahtar koruma mekanizması belgeli; bypass/fallback senaryoları doğrulanmış. Özellik olmaması güvenli master-password akışını bozmaz.

### M8 — Edge/Chrome browser entegrasyonu

Amaç: doğru origin'e ve doğru frame'e kullanıcı isteğiyle doldurma.

- [ ] M8.1 Manifest V3, minimum/optional host permissions ve inceleme ADR'si.
- [ ] M8.2 Native host kurulum/kayıt spike; package identity ve Store/side-load farkı.
- [ ] M8.3 Versioned bounded message schema; pairing, request ID, expiry, replay/cancellation ve session generation.
- [ ] M8.4 Host ↔ desktop IPC: kullanıcı scope ACL, peer/process doğrulaması ve açık sınırlamalar. Açık local HTTP port yoktur.
- [ ] M8.5 Browser sender tab/frame/origin bağlamını service worker doğrular; sayfanın verdiği URL'ye güvenilmez.
- [ ] M8.6 Exact-origin varsayılan: scheme, punycode hostname ve effective port. Alt domain/related domain yalnızca açık kullanıcı eşlemesiyle.
- [ ] M8.7 `example.com.evil.test`, farklı port, HTTP downgrade, IDN benzerliği, iframe ve tab-navigation testleri.
- [ ] M8.8 Çok adımlı login, SPA ve shadow DOM destek matrisi; cross-origin iframe başlangıçta kapalı.
- [ ] M8.9 Extension storage/log/crash report içinde secret yok; tüm kasayı extension'a gönderme yok.
- [ ] M8.10 Fake host/direct process invocation, malformed message, key/session expiry ve browser kapanması testleri.

Native Messaging transport ve `allowed_origins`, tek başına aynı makinedeki kötü süreçlere karşı güvenli kanal veya kimlik kanıtı değildir [S8–S9]. Ek doğrulama ve yetki sınırı gerekir; aynı kullanıcı malware'ine karşı mutlak garanti verilemez. Web sayfası doldurulmuş değeri görebilir; sayfa üzerinde password'ü gizleme vaadi yoktur.

Çıkış: kullanıcı tetikler; browser/desktop kilit durumu ortak; navigation sonrası eski talep reddedilir; yanlış origin/frame'e secret gönderilmez; incognito varsayılan kapalı; minimum permissions listesi ve inceleme raporu hazır. Firefox ayrı sonraki milestone'dur.

### M9 — Paketleme ve kontrollü beta adayı

Amaç: temiz Windows cihazında kurulabilir, güncellenebilir ve geri alınabilir sürüm.

- [ ] M9.1 MSIX/alternatif installer kararı; tray startup/native host kayıt ve uninstall cleanup doğrulaması.
- [ ] M9.2 Release CI, dependency lock, SBOM, artifact hash ve private signing materyali yönetimi.
- [ ] M9.3 Kod/paket imzalama; güncellemede imza/kimlik doğrulama. İlk tercih güvenilir Store/packaging güncelleme yolu; özel updater zorunlu değil.
- [ ] M9.4 Upgrade öncesi encrypted backup; migration fail/rollback testleri ve ileri format uyumluluk politikası.
- [ ] M9.5 Temiz VM install/upgrade/uninstall; kullanıcı kasası açık izin olmadan silinmez.
- [ ] M9.6 README, kullanıcı güvenlik rehberi, support/bug-report sanitization ve SECURITY.md.
- [ ] M9.7 Private beta için sentetik veri test paketi; dağıtım kanalı ve bilinen sınırlamalar.

Çıkış: release artifact doğrulanmış; signing secret repo/log'da yok; eski sürümden upgrade ve restore denenmiş. İmzalama kimliği sahibi sağlar; agent eksik sertifikayı üretim güveni varmış gibi taklit etmez.

### M10 — Ürünleşme ve public release kapısı

Amaç: başkalarının parolalarını emanet edeceği ürün için teknik ve operasyonel hazırlık.

- [ ] M10.1 Bağımsız uzman incelemesi: crypto format, key lifecycle, auto-fill trust, Hello ve extension/IPC (yayınlanan özelliklere göre).
- [ ] M10.2 Bulgu giderme ve yeniden test; kritik/yüksek açık veya veri kaybı sorunu release bloke eder.
- [ ] M10.3 LICENSE, dependency lisansları, ürün adı/marka kontrolü ve dağıtım hakları.
- [ ] M10.4 Gizlilik metni ve telemetry kararı. Varsayılan secret-free/no telemetry; opsiyonel crash raporunda explicit opt-in ve redaction.
- [ ] M10.5 Incident response, vulnerability disclosure, güvenlik update SLA hedefi ve sürdürülebilir bakım sorumlusu.
- [ ] M10.6 Hesap kurtarma beklentisi, backup eğitimi, desteklenen OS/apps ve güvenlik sınırlamaları kullanıcıya açık anlatılır.
- [ ] M10.7 Dağıtım bölgesi/iş modeli seçildiğinde ilgili gizlilik/tüketici/yazılım yükümlülükleri güncel uzman değerlendirmesiyle kontrol edilir; “uyumlu” etiketi kendiliğinden kullanılmaz.
- [ ] M10.8 Release candidate test matrisi ve açık sorun listesi; ürün sahibi yayın kararı.

Çıkış: dış inceleme sonuçları ve remediasyon kanıtı, restore/upgrade testleri, doğrulanmış paket ve bakım planı mevcut. “AES kullanıyoruz”, yüksek unit-test yüzdesi veya bir penetration test tek başına güvenli ürün kanıtı değildir. Kanıtlanmayan zero-knowledge, sertifikalı veya malware-proof iddiaları yapılmaz.

## 7. M11+ — Opsiyonel cloud sync ve genişleme

Local-first v1 tek başına ürün olabilir. Cloud sync için şimdilik yalnızca VaultId, envelope/payload version ve taşınabilir key wrap temeli korunur; backend henüz açılmaz.

### Cloud sync tasarımında ayrı çözülmesi gerekenler

- Sunucu hesabı kimliği ve kasa açma sırrı farklıdır. Login token/MFA kasayı otomatik çözen anahtar değildir.
- İstemci tarafı şifreleme; sunucuda plaintext kayıt/master password/KEK bulunmaz. Gerekirse standart incelenmiş authentication protokolü seçilir; kendi PAKE yapılmaz.
- Cihaz enrollment/revocation, key distribution, recovery ve parola değişiminin bütün cihazlara etkisi.
- Ciphertext revision, tombstone, conflict resolution ve idempotent sync. “Son yazan kazanır” veri kaybı için incelenmeden seçilmez.
- Sunucunun eski geçerli kayıt/kasa döndürmesi, silmesi veya cihazlar arasında farklı geçmiş göstermesi: AEAD bunları tek başına çözmez. Rollback/fork threat model gerekir.
- Şifrelenmiş metadata ve sunucu tarafında kaçınılmaz UserId, boyut, erişim zamanı, IP gibi bilgilerin gizlilik politikası.
- TLS, access control, rate limiting, quotas ve incident handling; bunlar end-to-end encryption'dan ayrı sorumluluklardır.
- Yeni dış güvenlik incelemesi. Local v1 audit sonucu sync mimarisine otomatik taşınmaz.

Ardından mobil companion, Firefox, TOTP, passkey veya paylaşım ayrı kullanıcı ihtiyacı ve ayrı threat model ile planlanabilir. Windows UI/core ayrımı portability için yardımcıdır; gelecekteki her feature için şimdiden framework yazılmaz.

## 8. Test ve kabul kanıtı

| Katman | Gerekli test örnekleri | Ortam |
| --- | --- | --- |
| Security | Argon2/HKDF/AEAD bilinen vektör, tamper, AAD substitution, yanlış parola, nonce, hostile header | Core runner + Windows doğrulama |
| Application | State transitions, lock-race, cancel, clipboard ownership, clock, validation | Unit test, fake clock/platform ports |
| Infrastructure | Gerçek SQLite transaction, WAL canary, migration, disk/IO hata injection | Geçici test klasörü |
| Backup | Yeni cihaz restore, wrong password, truncated file, newer version, rollback | Test profili/VM |
| Windows | Tray lifecycle, session/suspend, DPI, UIA target/focus/integrity | Gerçek Windows, sentetik hedef |
| Browser | Origin/frame/navigation, IPC/message/replay, minimum permissions | Gerçek Chrome/Edge + test sayfaları |
| Release | Install/upgrade/uninstall, imza, yedek, restore, clean startup | Temiz Windows VM |

Testler güvenlik davranışını doğrular; mock'un kendisini doğrulayan testler yeterli değildir. “Test geçti” yalnızca gerçek komut çıktısı mevcutsa yazılır. UI ve Windows olayları manuel doğrulanmadıysa durum `Manual verification pending` olur.

### Her milestone için ortak Done tanımı

- Kabul kriterleri karşılanmış, regresyon testleri geçmiştir.
- İlgili `git diff --check`, build/test ve secret/artifact kontrolleri tamamlanmıştır.
- ADR, status, test matrisi ve manual test kayıtları günceldir.
- Gerçek parola, vault dosyası, kişisel hesap veya signing credential commit edilmemiştir.
- Veri formatı değiştiyse compat/backup/migration kanıtı vardır.
- Çalıştırılamayan Windows/cihaz testleri açıkça pending olarak listelenmiştir.
- Bilinen sorunlar ve kalan riskler kaydedilmiştir; sonraki milestone otomatik tamamlandı sayılmaz.

## 9. Codex ile çalışma ve handoff

Repo köküne `AGENTS.md` ve bu dosyayı koy. Başlangıç görevi M0.1–M0.2'dir. Büyük milestone tek seferde implementasyon görevi verilmez. Agent mevcut status/ADR/repo durumunu okuyup en küçük açık alt görevi ele alır.

Her alt görev sonunda: davranış değişikliği, değişen dosyalar, gerçek komut ve sonuçları, manuel test adımları, açık engeller, sonraki alt görev raporlanır. Belgeler güncellenir. Commit/push/merge/yayın mevcut kullanıcı yetkisine göre yürür; varsayılan ilk görev bunları yetkilendirmez.

`docs/PROJECT_STATUS.md` başlangıç şablonu:

```markdown
# Project Status
Last updated: <date>
Current milestone: M0
Current task: M0.1–M0.2
Completed: None
Verified toolchain: Not yet verified
Build/test evidence: Not run
Manual checks: Pending
Decisions/ADRs: None
Known risks/blockers: Windows environment/toolchain verification pending
Next task: Record product scope and run Windows toolchain spike
```

### Agent'a verilecek ilk mesaj

```text
AGENTS.md ve DEVELOPMENT_ROADMAP.md dosyalarını tamamen oku.
Bu oturumun hedefi yalnızca M0.1 ve M0.2.
Mevcut repo ve araç zincirini incele; ürün kapsamını ve Windows hedefini kaydet.
.NET 10 / WinUI 3 / Windows App SDK uyumluluğunu gerçek ortamda doğrula.
THREAT_MODEL.md başlangıcını, PROJECT_STATUS.md ve stack/toolchain ADR'sini oluştur.
Ortam Windows değilse core hazırlığını yapabilir, Windows doğrulamasını pending bırakabilirsin.
Gelecek milestone'ların production kodunu, crypto, autofill veya cloud bileşenlerini yazma.
Mevcut kullanıcı dosyalarını koru; gerçek hesap/parola kullanma.
Tamamlanınca değişiklikleri, doğrulama kanıtını, engelleri ve M0.3 önerisini raporla.
Bu görev commit, push, merge, yayın veya sertifika satın alma yetkisi vermiyor.
```

Sonraki görev örneği: “Status ve ADR'leri oku; yalnızca M1.3'ü uygula; VAULT_FORMAT onaylı sözleşmesine uy; test vektörleri ve tamper testlerini çalıştır; M1.4'e başlamadan sonucu raporla.”

## 10. Karar kaydı listesi

| ADR | Karar | Son tarih |
| --- | --- | --- |
| 001 | OS, SDK, WinUI toolchain, UI fallback ve paketleme adayı | M0 |
| 002 | Crypto format, root/KEK/key separation ve AAD | M1 başlamadan |
| 003 | Argon2id provider, parametreler, benchmark ve parser sınırları | M1 |
| 004 | Session/lock, plaintext buffer ownership ve concurrency | M1 |
| 005 | SQLite envelope/manifest, transaction ve nonce politikası | M1 |
| 006 | Backup/restore, master change, rotation ve migration | M3 |
| 007 | Auto-fill target trust ve destek/fallback sınırları | M5 |
| 008 | Hello key protection ve garanti sınırları | M7 |
| 009 | Extension origin policy, pairing ve native IPC | M8 |
| 010 | Installer, signing, update ve release policy | M9 |
| 011 | Sync protocol, device/recovery/conflict threat model | M11 başlamadan |

ADR; context, seçilen karar, alternatif, gerekçe, güvenlik etkisi, test kanıtı ve review trigger içerir. Henüz karar verilmemiş alan “TBD” kalır; implementasyon varmış gibi yazılmaz.

## 11. Teknik kaynaklar

Kaynaklar 5 Ekim 2026'da kontrol edildi. Bu belgenin ürün sıralaması, varsayılan UX süreleri ve kabul kriterleri proje önerileridir. API/platform davranışları implementasyon sırasında tekrar kontrol edilir.

- [S1 — Microsoft WinUI 3](https://learn.microsoft.com/en-us/windows/apps/winui/)
- [S2 — RFC 9106: Argon2](https://www.rfc-editor.org/rfc/rfc9106.html)
- [S3 — .NET AesGcm.Encrypt](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aesgcm.encrypt?view=net-10.0)
- [S4 — Microsoft Clipboard Formats](https://learn.microsoft.com/en-us/windows/win32/dataxchg/clipboard-formats)
- [S5 — UserConsentVerifier](https://learn.microsoft.com/en-us/uwp/api/windows.security.credentials.ui.userconsentverifier)
- [S6 — CryptProtectData / DPAPI](https://learn.microsoft.com/en-us/windows/win32/api/dpapi/nf-dpapi-cryptprotectdata)
- [S7 — Microsoft Windows authentication auto-fill restrictions, KB5080542](https://support.microsoft.com/en-us/servicing/os/windows/docs/2026/01/new-behavior-restricting-certain-applications-to-autofill-credentials-introduced-by-the-windows-janu)
- [S8 — Chrome Native Messaging](https://developer.chrome.com/docs/extensions/develop/concepts/native-messaging)
- [S9 — Chromium Extensions Security FAQ](https://chromium.googlesource.com/chromium/src/+/main/extensions/docs/security_faq.md)
- [S10 — Microsoft UI Automation security](https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-securityoverview)
- [S11 — .NET releases and support](https://learn.microsoft.com/en-us/dotnet/core/releases-and-support)
