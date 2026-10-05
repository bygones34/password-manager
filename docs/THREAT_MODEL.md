# Threat Model — PasswordManager (v0)

Sürüm: 0.1 (Taslak / M0.1) • Tarih: 5 Ekim 2026 • Durum: Aktif

Bu belge, `PasswordManager` (kod adı) projesinin M0 aşamasındaki başlangıç tehdit modelini tanımlar. Proje geliştikçe ve yeni bileşenler (M1 Kasa Çekirdeği, M4 System Tray, M5 Auto-fill, M8 Browser Entegrasyonu) eklendikçe güncellenecektir.

---

## 1. Sistem Tanımı ve Kapsam

### 1.1. Ürün Hedefi
Windows 11 üzerinde çalışan, önce yerel (local-first), WinUI 3 tabanlı modern arayüze ve bildirim alanı (system tray) mini kasasına sahip, kullanıcı denetimli otomatik doldurma (auto-fill) sağlayan parola yöneticisi.

### 1.2. Hedef Platform ve Çalışma Ortamı
* **İşletim Sistemi:** Windows 11 (x64 mimarisi).
* **Kullanıcı Yetki Seviyesi:** Standart kullanıcı (UAC elevation veya UIAccess gerektirmez).
* **Ağ Durumu:** İlk sürüm tamamen internetsiz (offline) çalışır. Harici favicon/ikon indirme servisi veya telemetri bulunmaz; hesap alan adları ve parolalar internete iletilmez.

### 1.3. Kapsam Dışı Bırakılanlar (v0 / Başlangıç)
* Bulut senkronizasyonu (Cloud Sync) ve uzak sunucu mimarisi.
* Tarayıcı eklentileri (Chromium / Firefox) — M8 aşamasına kadar kapsam dışıdır.
* Windows Hello / Biyometrik entegrasyon — M7 aşamasına kadar kapsam dışıdır.
* OS oturum açma (Windows Sign-in), UAC ve güvenli masaüstü (Secure Desktop) ekranlarında otomatik doldurma.
* Kredi kartı, TOTP, Passkey, güvenli dosya eki yönetimi.
* `SendInput` tabanlı kör klavye enjeksiyonu.

---

## 2. Korunan Varlıklar (Assets)

1. **Master Password (Ana Parola):** Kullanıcının kasayı açmak için girdiği tek birincil sır.
2. **Kriptografik Anahtarlar:**
   - KEK (Key Encryption Key - Argon2id ile türetilen anahtar sarmalama anahtarı).
   - Kasa Kök Anahtarı (CSPRNG ile üretilen 32 bayt rastgele anahtar).
   - Kayıt ve Manifest Anahtarları (HKDF-SHA-256 ile amaç etiketlerine göre türetilen anahtarlar).
3. **Kayıt Verileri (Credentials & Metadata):** Hesap başlıkları, kullanıcı adları, parolalar, URL/domain eşlemeleri, notlar, kategoriler, favoriler ve revizyon bilgileri.
4. **Kasa Manifesti ve Bütünlüğü:** Hangi kayıtların var olduğu, kategori tanımları, kasa yapılandırması.
5. **Açık Kasa Oturumu (Active Session):** Bellekteki açık oturum durumu, geçici çözülmüş kayıtlar, arama sonuçları.
6. **Yedek Dosyaları:** Dışa aktarılan veya otomatik oluşturulan şifreli kasa yedekleri.
7. **Hedef Doğrulama Bağlamı:** Auto-fill sırasında hedeflenen uygulamanın kimlik ve pencere doğruluğu.

---

## 3. Tehdit Aktörleri ve Güven Sınırları

### 3.1. Tehdit Aktörleri
* **Yerel Dosya Hırsızı (Passive At-Rest Attacker):** Kasa veritabanını (`.db`) veya yedek dosyasını diskten, USB'den veya çalıntı cihazdan kopyalayan saldırgan.
* **Kurcalayan Saldırgan (Tampering / Active File Attacker):** Veritabanı dosyasına doğrudan yazarak kayıtları silen, değiştiren, eski kayıtları araya sokan (substitution) veya dosya başlığını bozan aktör.
* **Meraklı / Omuz Başı İzleyicisi (Shoulder Surfer / UI Snooper):** Kullanıcı ekran başından ayrıldığında veya kasa kilitliyken ekrana bakan kişi.
* **Kötü Niyetli / Ele Geçirilmiş Uygulama (Context Spoofing / Malicious App):** Kendisini meşru bir uygulama (örn. Steam veya kurumsal uygulama) gibi göstererek auto-fill mekanizmasından parola çalmaya çalışan yerel yazılım.
* **Pano İzleyicisi (Clipboard Monitor):** Windows panosuna kopyalanan parolaları yakalamaya çalışan üçüncü taraf yazılımlar.
* **Aynı Kullanıcı Oturumundaki Kötü Amaçlı Yazılım (Same-User Malware):** Kullanıcıyla aynı yetkide çalışan ve bellek okuma veya pencere mesajları gönderebilen zararlı yazılım.

### 3.2. Güven Sınırları (Trust Boundaries)
* **Kasa Depolama Sınırı (Storage Boundary):** Veritabanı ve disk yalnızca şifreli zarfları (encrypted envelopes) saklar. Düz metin (plaintext) disk sınırını geçemez.
* **Oturum / Bellek Sınırı (Session & Process Boundary):** Uygulama kilitlendiğinde anahtar materyali ve çözülmüş tüm veriler bellekten temizlenir. UI katmanı güvenlik çekirdeğini atlayamaz.
* **Hedef Uygulama Sınırı (Autofill Target Boundary):** Parola yöneticisi ile hedef uygulama arasındaki sınır. Hedef pencere (HWND), süreç kimliği (PID, başlangıç zamanı, dosya yolu) ve kontrol özellikleri doğrulanmadan veri aktarılmaz.

---

## 4. Tehdit Analizi, Beklenen Korumalar ve Sınırlar

| Tehdit ID | Tehdit Açıklaması | Tasarım Koruması | Kabul Edilen Sınır / Kalan Risk |
| --- | --- | --- | --- |
| **T-01** | Çalınan veritabanı veya yedek dosyasının offline çözülmesi | Kasa kök anahtarı bağımsız üretilir ve Argon2id KEK ile AES-256-GCM kullanılarak sarılır. Kayıtlar AEAD ile şifrelenir. | Çok zayıf master password seçilirse offline sözlük/kaba kuvvet saldırısıyla kırılabilir. Kullanıcıya parola güçlüğü rehberliği verilir. |
| **T-02** | Kayıt veya başlık (header) modifikasyonu | AES-256-GCM kimlik doğrulama etiketi (auth tag) ve AAD (VaultId, RecordId, sürüm) doğrulaması. Başarısızlık durumunda fail-closed. | Saldırgan dosyayı bozarak kullanılmaz hale getirebilir (hizmet dışı bırakma / availability saldırısı). |
| **T-03** | Kayıtların yer değiştirilmesi veya eski kaydın geri konulması (Replay / Substitution) | Her kayıt için RecordId ve VaultId'ye bağlı AAD kontrolü; kasa manifesti ile kayıt kümesi bütünlüğü. | Yerel v1'de saldırgan tüm veritabanı dosyasını geçerli eski bir snapshot ile değiştirirse (rollback) yerel düzeyde bunu tespit etmek garanti değildir. |
| **T-04** | Kilitli kasadan veya arka plan pencerelerinden veri sızması | Kasa kilitliyken başlık, kullanıcı adı, alan adı, son kullanılanlar ve kayıt sayısı sıfırlanır. Arama sonuçları ve form taslakları silinir. | İşletim sistemi veya grafik sürücüsünün ekran kartı belleğindeki geçici dokuları temizlediği garanti edilemez. |
| **T-05** | Yanlış uygulamaya parola doldurma (Phishing / Context Spoofing) | Yalnızca kullanıcı eylemiyle tetikleme; HWND, PID, dosya yolu ve denetim deseni doğrulaması; doldurma anında son focus yeniden doğrulaması. | Hedef uygulamanın kendisi ele geçirilmişse (compromised), kendisine yazılan parolayı okuyabilir. Otomatik Enter/submit yapılmaz. |
| **T-06** | Pano geçmişi ve bulut senkronizasyonu üzerinden parola sızıntısı | Windows `ExcludeClipboardContentFromMonitorProcessing` ve bulut dışlama bayrakları kullanılır; 20 saniye sonra zaman aşımı ve sahiplik kontrolüyle temizlenir. | Windows API bayraklarına uymayan agresif üçüncü taraf clipboard hook/logger yazılımlarına karşı tam koruma garantisi verilemez. |
| **T-07** | Bellek analizi veya bellek dökümü (Crash/Memory Dump) | Anahtar buffer'ları `ZeroMemory` ile temizlenir; hassas nesneler deterministic dispose edilir. | Managed string kopyaları, .NET Garbage Collector hareketleri ve OS sayfalama (pagefile/hibernation) dosyaları tamamen kontrol edilemez. |
| **T-08** | Düşmanca hazırlanmış import / yedek dosyası ile DoS saldırısı | KDF parametreleri (bellek, iterasyon, kanal) ve dosya boyutları için katı üst sınırlar konulur. Doğrulama bitmeden aktif kasa değiştirilmez. | Bozuk dosya içeriği içeri aktarılamaz, kullanıcıya güvenli genel hata dönülür. |
| **T-09** | Aynı kullanıcı yetkisinde çalışan zararlı yazılım (Malware) | Oturum süreleri sınırlandırılır (otomatik kilitleme); arka planda açık kasa belleği minimize edilir. | Aynı kullanıcı hesabı altındaki bir süreç uygulamaya bellek enjeksiyonu yapabilir veya API'leri kanca atabilir; OS düzeyinde tam tecrit yalnızca ayrı oturum/hesap ile mümkündür. |

---

## 5. Değişmez Güvenlik İlkeleri (Non-Negotiables)

1. **Master password asla diske yazılmaz**, loglanmaz, telemetriye dahil edilmez ve görünür metin olarak saklanmaz.
2. **AES-GCM için her şifreleme işleminde yeni bir CSPRNG nonce** (96-bit) üretilir. Aynı anahtar/nonce çifti asla yeniden kullanılmaz.
3. **Fail-Closed İlkesi:** Herhangi bir auth tag, AAD veya header doğrulama hatasında veri asla çözülmüş olarak sunulmaz; ortak ve güvenli bir hata mesajı üretilir.
4. **Log Güvenliği:** Yalnızca güvenli durum kodları ve olaylar loglanır; DTO dökümü, entity serializasyonu veya hassas istisna detayları loglara girmez.
5. **Kullanıcı İradesi Dışında Doldurma Yok:** Auto-fill hiçbir koşulda kullanıcı tetiklemesi olmadan başlatılmaz; otomatik submit/enter işlemi uygulanmaz.

---

## 6. Sürüm ve Güncelleme Planı
* **v0 (M0.1 - Mevcut):** Başlangıç ürün hedefleri, sınırları ve temel tehdit haritası.
* **v1 (M1.1 Hedefi):** Şifreleme formatı (VAULT_FORMAT.md), Argon2id parametre sınırları, anahtar sarma (key wrap) ve SQLite şema detayları ile güncellenecektir.
