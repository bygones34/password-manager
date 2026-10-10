# Threat Model — PasswordManager (v1.0)

* **Sürüm:** 1.0 (Milestone M1.1 Güncellemesi)
* **Tarih:** 2026-10-10
* **Durum:** Onaylandı / Aktif
* **Hedef Platform:** Windows 11 (x64)
* **İlgili Standartlar & Dokümanlar:** `VAULT_FORMAT.md`, `ADR-002`, `ADR-003`, `SECURITY_TEST_MATRIX.md`

---

## 1. Sistem Tanımı ve Kapsam

### 1.1. Ürün Hedefi
Windows 11 üzerinde çalışan, önce yerel (local-first), WinUI 3 tabanlı modern arayüze ve bildirim alanı (system tray) mini kasasına sahip, kullanıcı denetimli otomatik doldurma (auto-fill) sağlayan parola yöneticisi (`PasswordManager`).

### 1.2. Hedef Platform ve Çalışma Ortamı
* **İşletim Sistemi:** Windows 11 (x64 mimarisi).
* **Kullanıcı Yetki Seviyesi:** Standart kullanıcı (UAC elevation veya UIAccess gerektirmez).
* **Ağ Durumu:** Tamamen internetsiz (offline) çalışır. Harici favicon/ikon indirme servisi veya telemetri bulunmaz; hesap alan adları ve parolalar internete iletilmez.
* **Depolama Modeli:** SQLite üzerinde AES-256-GCM ile şifrelenmiş zarflar (encrypted envelope). Düz metin (plaintext) veritabanı alanı, arama dizini veya log dosyası bulunmaz.

### 1.3. Kapsam Dışı Bırakılanlar (v1 Kapsamı)
* Bulut senkronizasyonu (Cloud Sync) ve uzak sunucu mimarisi (M10+ sonrası).
* Tarayıcı eklentileri (Chromium / Firefox) — M8 aşamasına kadar kapsam dışıdır.
* Windows Hello / Biyometrik entegrasyon — M7 aşamasına kadar kapsam dışıdır.
* OS oturum açma (Windows Sign-in), UAC ve güvenli masaüstü (Secure Desktop) ekranlarında otomatik doldurma.
* Kredi kartı, TOTP, Passkey, güvenli dosya eki yönetimi.
* `SendInput` tabanlı kör klavye enjeksiyonu.

---

## 2. Korunan Varlıklar (Assets) ve Gizlilik Düzeyleri

| Varlık ID | Varlık Adı | Tanım ve Koruma Seviyesi | Yaşam Döngüsü & Saklama Biçimi |
| --- | --- | --- | --- |
| **A-01** | **Master Password** | Kullanıcının kasayı açmak için girdiği tek birincil parola. En yüksek gizlilik. | Asla diske yazılmaz, loglanmaz; KEK türetildikten hemen sonra bellekten `ZeroMemory` ile temizlenir. |
| **A-02** | **KEK (Key Encryption Key)** | Master password ve salt ile Argon2id üzerinden türetilen 256-bit geçici anahtar. | Yalnızca kasa açılırken/kilitlenirken kök anahtarı çözmek/sarmak için bellekte tutulur, diske yazılmaz. |
| **A-03** | **Kasa Kök Anahtarı (Root Key)** | CSPRNG ile üretilen bağımsız 256-bit birincil anahtar. | Diskte KEK ile sarılmış (AES-256-GCM wrapped) olarak saklanır. Bellekte açık kasa süresince kalır, kilit anında sıfırlanır. |
| **A-04** | **Record & Manifest Anahtarları** | Root Key'den HKDF-SHA-256 ile amaç etiketlerine göre türetilen 256-bit anahtarlar. | Bellekte geçici türetilir; kayıt ve manifest şifreleme/çözme işlemlerinde kullanılır. |
| **A-05** | **Kayıt Verileri (Credentials)** | Kullanıcı adları, parolalar, URL'ler, notlar, başlıklar, kategoriler, favoriler. | Yalnızca `RecordKey` ile şifrelenmiş envelope içinde diskte bulunur. Bellekte sadece aktif görüntüleme anında çözülür. |
| **A-06** | **Kasa Manifesti & Bütünlük** | Kasa yapılandırması, kayıt kimlikleri (RecordId) ve revizyon eşlemeleri. | `ManifestKey` ile şifrelenmiş envelope içinde saklanır; kayıt silme ve yerine koyma (substitution) tespitini sağlar. |
| **A-07** | **Açık Kasa Oturumu (Active Session)** | Bellekteki açık kasa oturumu, arama indeksleri, pano tamponu. | Kilitlenme (lock), sistem kilitlenmesi veya zaman aşımında deterministik olarak bellekten boşaltılır. |

---

## 3. Tehdit Aktörleri ve Güven Sınırları

### 3.1. Tehdit Aktörleri
1. **Durağan Dosya Hırsızı (Passive At-Rest Attacker):** Kasa veritabanını (`.db`), WAL dosyalarını veya yedek dosyasını diskten, çıkarılabilir medyadan veya yedekleme sunucusundan ele geçiren saldırgan.
2. **Aktif Dosya Kurcalayıcısı (Tampering / Active File Attacker):** Veritabanı dosyasına doğrudan müdahale ederek kayıtları silen, değiştiren, eski kayıtları araya sokan (substitution), başlığı (header) manipüle eden veya KDF parametrelerini değiştirerek DoS yaratmaya çalışan aktör.
3. **Ekran / UI İzleyicisi (Shoulder Surfer / Casual Observer):** Kullanıcı ekran başından ayrıldığında veya kasa kilitliyken ekrana bakan kişi.
4. **Pano İzleyicisi (Clipboard Monitor):** Windows panosuna kopyalanan kimlik bilgilerini yakalamaya çalışan üçüncü taraf yazılımlar.
5. **Aynı Kullanıcı Altında Çalışan Zararlı Yazılım (Same-User Malware):** Standart kullanıcı oturumu altında çalışan, bellek okuma veya pencere mesajları gönderebilen zararlı yazılımlar.

### 3.2. Güven Sınırları (Trust Boundaries)
* **Kasa Depolama Sınırı (Storage Boundary):** Veritabanı ve dosya sistemi yalnızca şifreli zarfları (encrypted envelope) saklar. Düz metin hiçbir zaman disk sınırını geçmez. SQLite WAL, SHM ve geçici dosyalar da şifreli kalır.
* **Oturum ve Bellek Sınırı (Session & Memory Boundary):** Uygulama kilitlendiğinde anahtar materyali ve çözülmüş veriler bellekten temizlenir. UI katmanı güvenlik çekirdeğini atlayamaz; doğrudan DB veya anahtar API'lerini çağıramaz.
* **Auto-fill Hedef Sınırı (Target Boundary):** Masaüstü hedef pencere (HWND), süreç (PID, bütünlük seviyesi, imza) ve focus bağlamı doğrulanmadan parola transferi yapılmaz.

---

## 4. Tehdit Analizi, Hafifletmeler ve Sınırlar

| Tehdit ID | Tehdit ve Saldırı Senaryosu | Tasarım Hafifletmesi (Mitigation) | Kabul Edilen Sınır / Kalan Risk |
| --- | --- | --- | --- |
| **T-01** | Çalınan veritabanı veya yedek dosyasının çevrimdışı kırılması (Offline Cracking) | Bağımsız 256-bit CSPRNG Root Key; Argon2id v1.3 ile 64 MiB bellek, 3 iterasyon, 4 paralellik ve 32 bayt salt ile KEK türetimi; AES-256-GCM ile sarmalama. | Zayıf master password seçilirse offline sözlük saldırısıyla kırılabilir. Kullanıcıya parola karmaşıklık kuralları ve güç göstergesi sunulur. |
| **T-02** | Kayıt veya başlık (header) manipülasyonu (Tampering) | AES-256-GCM 128-bit kimlik doğrulama etiketi (auth tag) ve AAD doğrulaması. Başarısızlık durumunda katı fail-closed ilkesi. | Saldırgan dosyayı bozarak kasayı açılamaz hale getirebilir (DoS/availability saldırısı). |
| **T-03** | Kayıt yer değiştirme veya başka kasadan kayıt aktarma (Substitution / Cross-Vault Injection) | Her kayıt için kanonik AAD yapısı: `VaultId` (16B) \|\| `RecordId` (16B) \|\| `EnvelopeVersion` (uint32) \|\| Purpose. Farklı kayıt veya kasa anahtarıyla çözülemez. | Kasanın tamamının silinmesi veya geçmiş geçerli bir yedekle değiştirilmesi (rollback) yerel düzeyde tespit edilemez. |
| **T-04** | Düşmanca hazırlanmış dosya ile KDF DoS saldırısı (Hostile KDF Parameters) | Başlık veya import dosyasından okunan KDF parametreleri işlenmeden önce katı sınır kontrolünden geçer (Bellek: 16-512 MiB, İterasyon: 1-10, Paralellik: 1-16). Sınır dışı parametreler anında reddedilir. | Aşırı büyük dosya indirme/açma denemeleri dosya boyutu sınırı (ör. 50 MB) ile kesilir. |
| **T-05** | AES-GCM Nonce Tekrarı (Nonce Reuse Catastrophe) | Her şifrelemede 96-bit kriptografik rastgele CSPRNG nonce üretilir. Aynı anahtar altında asla sabit veya sayaç tabanlı deterministik nonce kullanılmaz. | Doğum günü paradoksu (birthday bound): Tek anahtar altında $2^{32}$ şifrelemeye kadar çakışma ihtimali ihmal edilebilir düzeydedir ($< 2^{-32}$). Kök anahtar rotasyonu bu riski yönetir. |
| **T-06** | Kilitli kasada bellek veya arayüzden veri sızması | Kasa kilitlendiğinde tüm hassas UI bileşenleri, modeller, arama dizinleri ve DTO'lar sıfırlanır. Anahtar buffer'ları `ZeroMemory` ile temizlenir. Devam eden decrypt/reveal operasyonları iptal edilir. | .NET Garbage Collector tarafından taşınan managed string kopyaları ve işletim sistemi pagefile/crash dump dosyaları %100 silinme garantisi veremez. |
| **T-07** | Windows Pano geçmişi ve bulut senkronizasyonu üzerinden sızıntı | `ExcludeClipboardContentFromMonitorProcessing` ve bulut dışlama bayrakları kullanılır; 20 saniye sonra zaman aşımı ve sahiplik kontrolüyle temizlenir. | Kötü amaçlı, Windows API standartlarını hiçe sayan agresif klavye/pano dinleyicileri (hook/logger) engellenemez. |
| **T-08** | Yanlış pencereye veya taklit uygulamaya parola doldurma (Context Spoofing) | Yalnızca kullanıcı onayıyla doldurma (otomatik doldurma/Enter yok). HWND, PID, dosya yolu ve focus son anda yeniden doğrulanır. | Hedef uygulamanın kendisi ele geçirilmişse, meşru olarak aldığı parolayı kötüye kullanabilir. |
| **T-09** | Aynı kullanıcı yetkisinde çalışan zararlı yazılım (Same-User Malware) | Oturum süreleri sınırlanır (5 dk hareketsizlikte otomatik kilit); hassas açık metin formu diske yazılmaz; UI izolasyonu korunur. | Aynı kullanıcı hesabı altındaki zararlı süreçler bellek okuma/enjeksiyon yapabilir; OS düzeyinde tam tecrit ancak ayrı kullanıcı/sandbox ile mümkündür. |

---

## 5. Değişmez Güvenlik Sözleşmesi (Non-Negotiables)

1. **Plaintext Asla Diske Yazılmaz:** Master password, çözülmüş hesap alanları ve simetrik anahtarlar hiçbir zaman diske yazılmaz, SQLite unencrypted sütununda tutulmaz, loglanmaz veya geçici dosyalara bırakılmaz.
2. **Kriptografik Anahtar Ayrımı (Key Separation):** KEK yalnızca Kök Anahtarı sarmak için kullanılır; doğrudan kayıt şifrelemez. Kayıtlar `RecordKey`, manifest ise `ManifestKey` ile şifrelenir. Anahtarlar birbirinin yerine kullanılamaz.
3. **Nonce Benzersizliği:** Her AEAD işlemi CSPRNG ile üretilmiş yeni bir 96-bit nonce kullanır. Yeniden şifreleme, güncelleme ve geri yükleme durumlarında da yeni nonce zorunludur.
4. **Katı Fail-Closed İlkesi:** Doğrulama, auth tag veya KDF parametre sınır hatası alındığında hiçbir veri sunulmaz. Hata mesajı saldırgana bilgi sızdırmaz ("Kasa açılamadı: kimlik doğrulama başarısız veya veri bozuk").
5. **Log Whitelist Kuralı:** Loglar yalnızca güvenli durum kodları ve süreleri içerebilir. İstisna detaylarında veya log mesajlarında parola, kullanıcı adı veya anahtar dökümü kesinlikle yasaktır.
6. **Kullanıcı Tetiklemeli Auto-fill:** Kullanıcı açıkça komut vermeden hiçbir forma otomatik veri basılmaz, otomatik `Enter` veya form gönderimi yapılmaz.
