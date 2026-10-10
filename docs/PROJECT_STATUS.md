# Project Status

Last updated: 2026-10-10
Current milestone: M1 (Secure Vault Çekirdeği) — DEVAM EDİYOR
Current task: M1.1 (Tamamlandı) / M1.2 (Sırada)

---

## 1. Tamamlanan Görevler

### Milestone M0 — Foundation ve Teknik Fizibilite (TAMAMLANDI)
* **M0.1 — Ürün Kapsamı ve Başlangıç Tehdit Modeli:**
  - Proje kod adı (`PasswordManager`), Windows 11 (x64) mimari hedefi ve ürün sınırları belirlendi.
  - `docs/THREAT_MODEL.md` (v0), `README.md` hazırlandı ve yerel git reposu başlatıldı.
* **M0.2 — Araç Zinciri Doğrulaması ve Mimari Karar Kaydı:**
  - Visual Studio Community 2026 Insiders, .NET 10 SDK (`10.0.401`), Windows Kits 10 SDK (`10.0.26100.0`) doğrulandı.
  - Windows App SDK 1.6 XAML derlemesi doğrulandı ve CLI derlemesi için `AppxMSBuildToolsPath` çözüldü.
  - `docs/adr/ADR-001.md` kabul edildi.
* **M0.3 — Çözüm, Katman Projeleri, CPM ve Standartlar:**
  - `global.json`, `.editorconfig`, `.gitignore`, `Directory.Build.props` ve `Directory.Packages.props` kuruldu.
  - 7 çekirdek kaynak katman projesi ve 6 test projesi oluşturulup `PasswordManager.slnx` çözümüne bağlandı.
  - Bağımlılık kuralları uygulandı ve duman testleri geçirildi.
* **M0.4 — WinUI Shell ve Modern Arayüz İskeleti:**
  - `Styles/Tokens.xaml` ile semantik renkler (Light/Dark temaları için `ThemeDictionaries`), kart kenarlıkları, tipografi ve boşluk token'ları oluşturuldu ve `App.xaml`'a bağlandı.
  - `CommunityToolkit.Mvvm` (8.4.2) merkezi paket yönetimine ve Desktop projesine entegre edildi.
  - `SyntheticVaultItem` modeli ve `SyntheticVaultService` oluşturuldu.
  - `MainViewModel` (C# 13/14 partial properties, filtreleme, arama, parola maskeleme/gösterim, favori yönetimi ve tema geçişi) geliştirildi.
  - `MainWindow.xaml` & `MainWindow.xaml.cs` hazırlanarak sol tarafta `NavigationView`, üstte arama/tema çubuğu, ortada liste görünümü ve sağda zengin detay paneli kuruldu.
  - `WindowsAppSDKSelfContained=true` ve `win-x64` yapılandırılarak bağımsız çalışma sağlandı.
* **M0.5 — Native Spike (Tray, Monitor/DPI, Foreground HWND, Session Lock):**
  - `PasswordManager.Platform.Windows` projesinde Win32 P/Invoke API'leri (`NativeMethods.cs`) tanımlandı.
  - Foreground HWND ve süreç tespiti (`ForegroundTargetDetector.cs`) uygulandı.
  - Flyout pozisyonlama ve sınır dışına taşmama (clamp) algoritması (`TrayPositionCalculator.cs`) geliştirildi.
  - Win32 WTS tabanlı oturum kilit olayları dinleyicisi (`SessionLockWatcher.cs`) ve bildirim alanı ikon yönetimi (`TrayIconManager.cs`) yazıldı.
* **M0.6 — SQLite Dosya Yolu/ACL ve Tek Instance Davranışı:**
  - `Application` katmanında `IStoragePathProvider` ve `ISingleInstanceManager` portları tanımlandı.
  - `WindowsStoragePathResolver` geliştirildi: Win32 paketsiz (`%LOCALAPPDATA%\PasswordManager\vault.db`) ile paketli (MSIX) ortam ayrımı yapıldı; Windows NTFS ACL kuralları (`DirectorySecurity`) uygulanarak yalnızca geçerli kullanıcı ve sistem yöneticilerine tam denetim verildi, diğer yerel kullanıcıların erişimi kısıtlandı.
  - `SingleInstanceAppLock` geliştirildi: Kullanıcı oturumuna özgü adlandırılmış Mutex ve `EventWaitHandle` sinyal mekanizması ile veritabanı kilit yarışları engellendi; ikinci örneğin aktif ana örneği öne getirmesi sağlandı.
  - Mimari tasarım ve güvenlik detayları `docs/STORAGE_AND_SINGLE_INSTANCE.md` dokümanında belgelendi.
* **M0.7 — Windows CI Build/Test Temeli ve Cross-Platform Doğrulama:**
  - GitHub Actions iş akışı (`.github/workflows/ci.yml`) oluşturuldu; Windows ortamında tam çözüm derleme/test adımları ve Ubuntu üzerinde platform bağımsız çekirdek projeler (`Domain`, `Application`, `Security`, `Infrastructure`) için test matrisi yapılandırıldı.
  - CI runner ortamı (`windows-latest`, VS 2022 Enterprise) için `Directory.Build.props` ve iş akışına `AppxMSBuildToolsPath` dinamik tespit adımı eklendi; WinUI 3 CLI derleme uyumluluğu sağlandı.
  - Release modunda derleme ve testler başarıyla çalıştırıldı; GitHub Actions CI yeşile döndü.

### Milestone M1 — Secure Vault Çekirdeği (DEVAM EDİYOR)
* **M1.1 — Tehdit Modeli v1, Kasa Formatı v1, Kriptografik Mimari ve ADR'ler:**
  - `docs/THREAT_MODEL.md` v1.0 sürümüne güncellendi: Çift kademeli anahtar hiyerarşisi (KEK, RootKey, RecordKey, ManifestKey), AES-256-GCM AEAD, AAD bağlam bütünlüğü, CSPRNG 96-bit nonce güvenliği, KDF DoS ve hostile import tehditleri, bellek hijyeni (`ZeroMemory`) ve fail-closed ilkeleri tanımlandı.
  - `docs/VAULT_FORMAT.md` v1.0 şartnamesi oluşturuldu: SQLite şeması ile kripto formatı ayrıldı; 132 baytlık ikili başlık (`VaultHeader`: Magic `"PWMV"`, sürüm 1, GUID `VaultId`, Argon2id parametreleri, Salt, Wrapped Root Key), kanonik JSON şemaları (`VaultRecord`, `VaultManifest`), AES-256-GCM AAD yapıları, master password değişiminde $O(1)$ re-wrap protokolü ve kök anahtar rotasyon prosedürleri kesinleştirildi.
  - `docs/adr/ADR-002.md` kabul edildi: Kriptografik Kasa Formatı, Anahtar Hiyerarşisi ve AEAD Nonce Politikası.
  - `docs/adr/ADR-003.md` kabul edildi: Argon2id KDF Parametreleri, Paket Seçimi (`Konscious.Security.Cryptography.Argon2`) ve Bounded Anti-DoS Doğrulama Politikası (Bellek: 16-512 MiB, İterasyon: 1-10, Paralellik: 1-16, Salt: 16-64 bayt).
  - `docs/SECURITY_TEST_MATRIX.md` oluşturuldu: RFC 9106/5869/NIST vektörleri, key separation, tampering/substitution, nonce uniqueness, bounded DoS, bellek sıfırlama ve SQLite/log canary tarama gereksinimleri belgelendi.
  - `DEVELOPMENT_ROADMAP.md` güncellendi (`M1.1` tamamlandı olarak işaretlendi).

---

## 2. Doğrulama ve Çalıştırılan Komutlar
* `git diff --check` → Temiz.
* Kod ve dokümantasyon bütünlüğü doğrulandı; proje derleme ve test ortamı hazır durumda.
* Mevcut test matrisi: 21 birim testi (%100 başarılı).

---

## 3. Manuel Doğrulama Durumu
* Mimari dokümanlar ve şartnameler karşılıklı tutarlılık ve AGENTS.md değişmez güvenlik kuralları açısından denetlendi.
* Durum: **M1.1 Alt Görev Kabul Kriterleri Karşılandı.**

---

## 4. Kararlar ve ADR'ler
* `docs/THREAT_MODEL.md` (v1.0) aktif.
* `docs/VAULT_FORMAT.md` (v1.0) aktif.
* `docs/SECURITY_TEST_MATRIX.md` (v1.0) aktif.
* `docs/adr/ADR-001.md` (Stack ve Toolchain) aktif.
* `docs/adr/ADR-002.md` (Kasa Formatı, Anahtar Hiyerarşisi ve Nonce Politikası) aktif.
* `docs/adr/ADR-003.md` (Argon2id Parametreleri, Paket Seçimi ve Sınır Doğrulama) aktif.
* `docs/STORAGE_AND_SINGLE_INSTANCE.md` (Depolama, ACL ve Tek Örnek Mimarisi) aktif.

---

## 5. Sıradaki Miltaşı ve Görev
* **Milestone M1 — Secure Vault Çekirdeği**
  - **M1.2:** Argon2id paket seçimi (`Konscious.Security.Cryptography.Argon2`), RFC 9106 resmi test vektörleri, masaüstü benchmark'ı ve güvenilmez parametreler için sınır doğrulama (bounded validation) implementasyonu.
