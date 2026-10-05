# Project Instructions — PasswordManager

Bu dosya repository kökünde tutulur. Tüm alt klasörlerdeki Codex çalışmalarına uygulanır. Kullanıcının mevcut oturumdaki açık talimatları önceliklidir. `DEVELOPMENT_ROADMAP.md` ürün planını, bu dosya çalışma kurallarını belirler.

## 1. Proje amacı

Windows 11 için modern, local-first parola yöneticisi geliştiriyoruz. Hedef WinUI 3 ana arayüz, system tray mini vault ve kanıtlanmış uygulama desteğiyle kullanıcı tetiklemeli auto-fill. İlk sürüm local SQLite encrypted vault kullanır. Cloud sync, browser extension, Windows Hello ve ürünleşme roadmap'teki ayrı kapılardan sonra gelir.

Kod adı `PasswordManager`; kesin ürün adı verilmeden isim icat etme. Kullanıcı belgeleri Türkçe, kod identifier'ları İngilizce olsun. UI metinlerini resource'larda tut; ilk UI dili roadmap'e göre İngilizce olabilir.

## 2. Her oturumun başlangıcı

1. Bu dosyayı, roadmap'i, varsa `docs/PROJECT_STATUS.md`, ilgili ADR ve test matrisini oku.
2. `git status` ve mevcut değişiklikleri incele; kullanıcı değişikliklerini koru.
3. Aktif alt görevi ve kabul kriterlerini belirle. Kullanıcı görev verdiyse onu izle; genel “devam et” talimatında status'taki en küçük açık alt görevi seç.
4. Platform/SDK/test araçlarının kullanılabilirliğini gerçek ortamdan doğrula. Windows doğrulaması Linux'ta tamamlanmış sayılamaz.
5. Kısa bir amaç ve doğrulama planı bildir; görevi uygulamaya taşı.

Var olmayan geçmiş test, commit, onay veya sürüm uydurma. Dosyalarda TBD olan kritik güvenlik kararını kod içinde sessizce seçme; ilgili ADR'yi somutlaştır. Güvenli ve geri alınabilir rutin uygulama kararlarında gereksiz onay döngüsü oluşturma.

## 3. Kapsam ve çalışma ritmi

- Bir alt görevi küçük, review edilebilir değişikliklerle tamamla. Bir milestone'un tamamını tek büyük patch'e dönüştürme.
- Aktif kapsam dışındaki crypto/auto-fill/sync/Hello kodunu önceden ekleme.
- Roadmap'teki bağımlılıklar ve exit gate'ler geçilmeden gerçek parola kullanımına veya kamuya dağıtıma geçme.
- Kullanıcı görevi implementasyon yetkisi verir; commit/push/merge/yayın için mevcut oturum yetkisini kontrol et. Yetki varsa tekrar sorma; yoksa değişiklikleri ve kanıtı review edilebilir halde bırak.
- Reset/clean/force-push, kullanıcı verisini silme, destructive migration ve mevcut güvenlik garantisini düşürme yapma. Kullanıcı bunları açıkça isterse önce etkiyi somutlaştır.
- Standart kullanıcı yetkisiyle çalış; auto-fill'i çözmek için administrator/UIAccess/UAC bypass ekleme.
- Yeni dependency eklerken resmî doküman, lisans, bakım, güvenlik ve sürüm uyumluluğunu doğrula. Sürümleri merkezi ve sabit yönet.

## 4. Mimari kuralları

- Domain bağımsız; Application use case ve portları tanımlar.
- Security UI/EF/Win32 bilmez; onaylı standart primitive'lerle kriptografi portlarını uygular.
- Infrastructure yalnızca encrypted envelope ve manifest saklar. Plaintext credential EF entity'si veya FTS index oluşturma.
- Platform.Windows native OS servislerini; Autofill.Windows hedef doğrulama ve adapter'ları kapsar.
- Autofill anahtar, DB veya sınırsız kasa okuma yetkisi almaz. Tek kullanıcı eylemi için kısa ömürlü, oturuma bağlı credential erişimi alır.
- Desktop composition root'tur. UI güvenlik kontrolünü atlayarak DB/key API çağırmaz.
- Gelecekte NativeHost ayrı kasayı açmaz; masaüstündeki aktif oturuma dar kapsamlı IPC yapar.
- Gereksiz generic framework, her sınıfa interface, microservice, local HTTP API veya zorunlu CQRS ekleme.

## 5. Değişmez güvenlik kuralları

1. Gerçek password, master password, kullanıcı hesabı, token, vault DB, backup, signing key veya certificate secret'ını source/test fixture/issue/log/commit içine koyma. Test verisi sentetik ve açıkça test-only olsun.
2. Master password kaydetme; input'u sessizce trim/normalize etme. Encoding politikası `VAULT_FORMAT.md` ile aynıdır.
3. KEK/root key/record key ayrımını koru. Algoritma, HKDF label, AAD, serializer, nonce, tag ve KDF parametresi değişikliği format/ADR/test güncellemesi gerektirir.
4. AES-GCM her encryption'da yeni CSPRNG nonce kullanır. Aynı key/nonce tekrarını, retry/restore/edit dahil önle. Sabit nonce, deterministic IV veya `Random` kullanma.
5. Bilinmeyen format veya authentication failure'da fail closed. Ciphertext'i hasarlı diye plaintext fallback ile açma.
6. Header/import KDF ve boyut değerleri sınır kontrolünden sonra işlenir. KDF'yi sınırsız attacker-controlled değerle çalıştırma.
7. Kilit durumunda hesap metadata'sı, count, search sonucu, form taslağı ve secret gösterme. Tüm UI/tray yüzeyleri aynı session generation'a bağlıdır.
8. Lock pending decrypt/reveal/clipboard/fill işlerini iptal eder; geç gelen sonuçlar uygulanamaz. Session lock/suspend sonrası kasa locked kalır.
9. Anahtar ve kontrollü byte buffer'ları deterministic dispose/zero yap. Managed string veya process/OS belleğinin tamamını temizlediğini iddia etme.
10. Log alanları whitelist olsun. DTO/entity dump, EF sensitive-data logging veya password içeren exception context kullanma. Production debug dump/telemetry'yi kendiliğinden etkinleştirme.
11. Clipboard history/sync dışlama ve conditional cleanup kullan. Kullanıcının sonraki clipboard içeriğini silme. Clipboard logger koruması iddia etme.
12. Parola reveal süreli; lock/focus loss'ta masked. Accessibility/automation metadata'sına password yayınlama.
13. Backup/restore atomik ve doğrulanmış olsun; SQLite WAL varken sadece ana DB dosyasını kopyalama. Restore doğrulanana kadar aktif kasa korunur.
14. Local retry throttling offline cracking savunması değildir; weak master password riskini gizleme.
15. Önceki geçerli snapshot rollback'i AEAD tek başına önlemez. Eksik korumayı “tamper-proof” diye sunma.

Bu kurallar için test gerektiğinde roadmap'in güvenlik test matrisini kullan. Güvenlik işlevini yalnızca compile olduğu için tamamlandı sayma.

## 6. Auto-fill kuralları

- Kullanıcı credential ve fill eylemini seçmeden secret gönderme; otomatik submit/Enter yoktur.
- Tray açılmadan önceki hedefi yakala; unlock/focus dönüşünden sonra tekrar doğrula.
- Process adı veya window title tek başına güven kimliği değildir. HWND/PID/start identity/path/integrity ve beklenen control/context kontrol edilir.
- Her password write öncesi hedefi yeniden doğrula. Focus değişimi, PID reuse, yeni dialog, lock, timeout veya cancellation durumunda iptal et.
- UIA provider'ının password alanını dolduracağını varsayma; capability ve gerçek uygulama testleri gerekir. Hedefin mevcut password'ünü okumaya çalışma.
- İlk pilotta SendInput fallback yoktur; unsupported state ve manuel copy sun.
- OS sign-in/UAC/secure desktop/elevated hedefler kapsam dışıdır.
- Desteklenen uygulama ve sürümleri `AUTOFILL_COMPATIBILITY.md` ile kanıtla. Steam/Discord gibi adları test olmadan destek listesine ekleme.
- Browser için exact origin/frame ve navigation-bound request kontrolü gerekir; desktop UIA browser entegrasyonunun yerine geçmez.
- Native Messaging/allowed_origins veya pairing'in aynı kullanıcı malware'ine karşı mutlak güven sağladığını iddia etme. Protocol/IPC threat review yapılmadan browser özelliğini production'a açma.

## 7. Windows Hello ve recovery

Hello consent sonucu ile cryptographically gated key access aynı değildir. DPAPI CurrentUser tek başına her decrypt için Hello zorunluluğu sağlamaz. Kullanılan mekanizma ve sınırları ADR'de belirt; donanım koruması kanıtlanmadan vaat etme.

Master password yolu taşınabilir kalır. Cihaz özel Hello wrapper'ını backup ile başka cihaza aktarma. Recovery için plaintext master password saklama veya uygulama sahibine kasa açan backdoor ekleme. Unutulan master password, backup var diye kendiliğinden kurtarılmaz.

## 8. Doğrulama ve completion

İlgili kapsam için gerçek build/test komutlarını çalıştır. Solution/Windows toolchain'e göre komutlar README'de kesinleştirilir; sırf genel `dotnet test` çıktı verdi diye WinUI build ve native testler geçti yazma.

- Core: bilinen crypto vektörleri, tamper/AAD, state/lock-race, validation.
- Storage: gerçek SQLite, transaction, WAL/journal/backup canary taraması, migration.
- Recovery: farklı test ortamında restore; yanlış parola/bozuk dosya aktif kasayı değiştirmez.
- Windows: tray/session/DPI/UIA gerçek hedef testleri.
- Browser: origin/frame/navigation, native-host/IPC ve malformed/replay testleri.
- Release: temiz VM kurulum/upgrade/uninstall ve imza kontrolü.

Kod değişikliği bittikten sonra `git diff --check`, ilgili test/build ve hassas/üretilmiş dosya kontrolünü tamamla. Geçerli doğrulamalar yeni değişiklik/failure olmadan gereksiz tekrar edilmez.

UI testinde screenshot veya test log'unda gerçek secret kullanma. Çalıştırılamayan kontrolü `Not run`/`Manual verification pending` olarak kaydet. Test sayısı veya başarı kanıtı uydurma.

## 9. Belge ve teslim standardı

Her alt görev sonunda `docs/PROJECT_STATUS.md` güncellenir:

- Tarih, aktif milestone/alt görev ve tamamlanan iş.
- Çalıştırılmış komutlar ve gerçek sonuçlar.
- Manuel doğrulama durumu, açık sorun/engel ve risk.
- Yeni/güncellenen ADR'ler ve sıradaki alt görev.

Format/API davranışı değiştiyse ilgili VAULT_FORMAT, THREAT_MODEL, BACKUP_AND_RECOVERY, MANUAL_TESTS ve test matrisi de güncellenir. Kullanıcıya kısa rapor ver: sonuç, doğrulama, kalan engel ve sonraki görev. Belgelenmemiş varsayımı “onaylandı” veya “güvenli” diye yazma.

Bir güvenlik kararı veri formatını geri dönülmez değiştiriyorsa, doğrulanabilir spike/ADR/test planını hazırla; mevcut oturum yetkisi ve roadmap gate'ine göre ilerle. Zorunlu insan/gerçek cihaz kontrolü varsa işi tamamlandı saymak yerine somut test adımlarını bırak.
