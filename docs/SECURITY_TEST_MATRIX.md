# Güvenlik Test Matrisi — SECURITY_TEST_MATRIX (v1.0)

* **Sürüm:** 1.0 (Milestone M1.1)
* **Tarih:** 2026-10-10
* **Durum:** Onaylandı / Aktif
* **Hedef Projeler:** `PasswordManager.Security.Tests`, `PasswordManager.Infrastructure.Tests`, `PasswordManager.Windows.Tests`
* **İlgili Dokümanlar:** `THREAT_MODEL.md`, `VAULT_FORMAT.md`, `ADR-002`, `ADR-003`

---

## 1. Genel Bakış

Bu doküman, `PasswordManager` kriptografik çekirdeğinin, depolama katmanının ve işletim sistemi entegrasyonlarının doğrulanması için zorunlu güvenlik test vakalarını tanımlar. Bir özelliğin yalnızca başarıyla derlenmesi veya tek bir pozitif (happy path) testten geçmesi tamamlandı sayılması için yeterli değildir; tüm negatif, sınır aşımı ve kurcalama (tampering) testlerinin geçmesi şarttır.

---

## 2. Test Kategorileri ve Güvenlik Kabul Kriterleri

### 2.1. Kategori A: Standart Kriptografik Vektör Testleri (RFC Vectors)
*Amaç: Kütüphane uygulamalarının standartlara tam uyumlu olduğunun bağımsız kanıtlanması.*

| Test ID | Test Senaryosu | Beklenen Sonuç | Doğrulama Yöntemi |
| --- | --- | --- | --- |
| **SEC-A01** | Argon2id v1.3 Resmi RFC 9106 Test Vektörleri | Bilinen parola, salt, $m, t, p$ parametreleriyle üretilen çıktı resmi RFC 9106 baytlarıyla birebir eşleşir. | Birim Testi (`Argon2idVectorTests`) |
| **SEC-A02** | HKDF-SHA-256 RFC 5869 Test Vektörleri | RFC 5869'daki IKM, salt ve info değerleriyle `HKDF-Expand` çıktısı tam doğrulanır. | Birim Testi (`HkdfVectorTests`) |
| **SEC-A03** | AES-256-GCM NIST SP 800-38D Vektörleri | NIST onaylı anahtar, IV, plaintext ve AAD girdileriyle üretilen ciphertext ve auth tag tam eşleşir. | Birim Testi (`AesGcmVectorTests`) |

---

### 2.2. Kategori B: Kriptografik Anahtar Ayrımı (Key Separation)
*Amaç: Farklı amaçlar için türetilen anahtarların asla birbirinin yerine kullanılamayacağının kanıtlanması.*

| Test ID | Test Senaryosu | Beklenen Sonuç | Doğrulama Yöntemi |
| --- | --- | --- | --- |
| **SEC-B01** | `RecordKey != ManifestKey != RootKey != KEK` | Aynı kök anahtardan türetilen `RecordKey` ve `ManifestKey` kesinlikle birbirinden farklı 256-bit dizilimler üretir. | Birim Testi (`KeySeparationTests`) |
| **SEC-B02** | Farklı Kasa Kimliği (`VaultId`) ile Türetim | Aynı `RootKey` kullanılsa bile farklı `VaultId` verildiğinde üretilen alt anahtarlar tamamen farklıdır. | Birim Testi (`KeySeparationTests`) |
| **SEC-B03** | Manifest Zarfını `RecordKey` ile Açma Denemesi | `RecordKey` ile şifrelenen bir zarf `ManifestKey` ile açılamaz; auth tag hatası fırlatılır. | Birim Testi (`KeySeparationTests`) |

---

### 2.3. Kategori C: Kurcalama, Bütünlük ve AAD Testleri (Tampering & Substitution)
*Amaç: Veritabanına veya zarflara yapılan en ufak müdahalenin anında tespit edilip engellenmesi.*

| Test ID | Test Senaryosu | Beklenen Sonuç | Doğrulama Yöntemi |
| --- | --- | --- | --- |
| **SEC-C01** | Ciphertext Üzerinde 1-Bit Değişimi (Bit Flip) | Şifreli veride tek bir bit değiştirildiğinde AES-GCM tag doğrulaması çöker; düz metin sızdırılmaz. | Birim Testi (`EnvelopeTamperTests`) |
| **SEC-C02** | Auth Tag Değiştirme | Tag alanında 1 bayt değiştirildiğinde şifre çözme işlemi fail-closed ile reddedilir. | Birim Testi (`EnvelopeTamperTests`) |
| **SEC-C03** | Kayıt Kimliği (`RecordId`) Değiştirme (Substitution) | Geçerli bir kaydın ciphertext'i veritabanında başka bir `RecordId` altına taşındığında AAD uyuşmazlığı nedeniyle çözülmez. | Entegrasyon Testi (`RecordSubstitutionTests`) |
| **SEC-C04** | Çapraz Kasa Kayıt Enjeksiyonu (Cross-Vault Swap) | Kasa A'daki geçerli bir kayıt Kasa B'ye kopyalandığında `VaultId` AAD uyuşmazlığı nedeniyle açılamaz. | Entegrasyon Testi (`CrossVaultTamperTests`) |
| **SEC-C05** | Başlık (Header) Parametre Kurcalaması | `VaultHeader` içindeki `MemoryKiB`, `Iterations` veya `Salt` değiştirildiğinde `WrappedRootKey` AAD doğrulaması patlar ve kök anahtar çözülmez. | Birim Testi (`HeaderTamperTests`) |
| **SEC-C06** | Magic Byte / Format Sürümü Hatası | Magic baytları `"PWMV"` olmayan veya sürümü geçersiz olan başlıklar KDF çalıştırılmadan anında reddedilir. | Birim Testi (`HeaderValidationTests`) |

---

### 2.4. Kategori D: Nonce Güvenliği ve Çakışma Önleme (Nonce Uniqueness)
*Amaç: AEAD nonce tekrarı felaketinin engellenmesi.*

| Test ID | Test Senaryosu | Beklenen Sonuç | Doğrulama Yöntemi |
| --- | --- | --- | --- |
| **SEC-D01** | Aynı Kaydın Tekrar Kaydedilmesi | Aynı plaintext ardışık 1,000 kez kaydedildiğinde 1,000 farklı 96-bit nonce ve 1,000 farklı ciphertext üretilir. | Birim Testi (`NonceRandomnessTests`) |
| **SEC-D02** | CSPRNG Entropi Kontrolü | Üretilen nonce'ların bayt dağılımı rastgeledir; sabit IV veya deterministik sayaç kullanılmadığı kanıtlanır. | Birim Testi (`NonceRandomnessTests`) |

---

### 2.5. Kategori E: KDF Sınır Doğrulama ve DoS Savunması (Bounded Anti-DoS)
*Amaç: Kötü niyetli hazırlanmış dosya başlıklarıyla RAM veya CPU tükenmesinin engellenmesi.*

| Test ID | Test Senaryosu | Beklenen Sonuç | Doğrulama Yöntemi |
| --- | --- | --- | --- |
| **SEC-E01** | Aşırı Bellek Maliyeti ($> 512 \text{ MiB}$) | Dosyada bellek değeri örneğin 2 GB (2097152 KiB) olarak verildiğinde KDF başlatılmadan sınır hatası verilir. | Birim Testi (`KdfBoundingTests`) |
| **SEC-E02** | Yetersiz Bellek Maliyeti ($< 16 \text{ MiB}$) | Dosyada güvenlik tabanının altında bir bellek değeri (örn. 1 MB) verildiğinde anında reddedilir. | Birim Testi (`KdfBoundingTests`) |
| **SEC-E03** | Aşırı İterasyon ($> 10$) veya Paralellik ($> 16$) | İterasyon veya paralellik üst sınırı aşıldığında CPU DoS engellenir, fail-closed döner. | Birim Testi (`KdfBoundingTests`) |
| **SEC-E04** | Geçersiz Salt Uzunluğu ($< 16$ veya $> 64$ Bayt) | Salt uzunluğu standart dışı olan başlıklar işlenmez. | Birim Testi (`KdfBoundingTests`) |

---

### 2.6. Kategori F: Bellek Hijyeni ve Yaşam Döngüsü (Memory Hygiene & Cancellation)
*Amaç: Hassas anahtar ve parola materyalinin bellekte asgari süre kalması.*

| Test ID | Test Senaryosu | Beklenen Sonuç | Doğrulama Yöntemi |
| --- | --- | --- | --- |
| **SEC-F01** | Deterministik Anahtar Temizliği (`ZeroMemory`) | `VaultSession` veya anahtar nesnesi `Dispose` edildiğinde anahtar bayt dizilerinin içi sıfırlanır (`0x00`). | Birim Testi (`MemoryZeroingTests`) |
| **SEC-F02** | Kilitlenme Anında Bekleyen İsteklerin İptali | Decrypt veya reveal işlemi devam ederken `Lock()` tetiklendiğinde operasyon `OperationCanceledException` ile durur; UI'ya veri iletilmez. | Eşzamanlılık Testi (`LockRaceTests`) |

---

### 2.7. Kategori G: Sentetik Sır ve Dosya Canary Taraması (Canary Search)
*Amaç: Veritabanı, WAL, geçici dosya veya loglara hiçbir düz metin parolanın sızmadığının kanıtlanması.*

| Test ID | Test Senaryosu | Beklenen Sonuç | Doğrulama Yöntemi |
| --- | --- | --- | --- |
| **SEC-G01** | SQLite `.db` ve `-wal` Dosyalarında Canary Taraması | Kasa oluşturulup kayıtlar eklendikten sonra fiziksel veritabanı dosyasının baytlarında sentetik master password veya hesap parolaları aranır; hiçbir plaintext bulunamaz. | Entegrasyon Testi (`DatabaseCanaryScannerTests`) |
| **SEC-G02** | Log Dosyalarında Plaintext Sızıntı Taraması | Tüm CRUD ve açma/kilitleme operasyonları boyunca üretilen loglarda parola veya kullanıcı adı geçmediği doğrulanır. | Entegrasyon Testi (`LogSanitizationTests`) |

---

## 3. Matris Yürütme ve Kabul Eşiği
* Tüm **Kategori A, B, C, D, E, F, G** testleri CI ve yerel ortamda `%100` başarıyla geçmelidir.
* Hiçbir test atlanamaz (`Skip`/`Ignore` yasaktır).
* Güvenlik testlerinde yalnızca sentetik (açıkça test amaçlı) veriler kullanılır; asla gerçek parola veya gerçek kullanıcı hesabı kullanılmaz.
