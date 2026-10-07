# Project Status

Last updated: 2026-10-07
Current milestone: M0 (Foundation ve teknik fizibilite) — TAMAMLANDI
Current task: M0.7 (Tamamlandı) / M1 Başlangıcı (Sırada)

---

## 1. Tamamlanan Görevler (M0 Özeti)
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
  - Release modunda derleme ve testler başarıyla çalıştırıldı.

---

## 2. Doğrulama ve Çalıştırılan Komutlar
* `dotnet build -c Release` → 13 projenin tamamı **0 hata ve 0 uyarı** ile başarıyla derlendi.
* `dotnet test -c Release --no-build` → 6 test projesindeki tüm testler başarıyla geçti (**21 test geçti, 0 hata, 0 atlanan**).
* `git diff --check` → Temiz.

---

## 3. Manuel Doğrulama Durumu
* WinUI 3 shell'i masaüstünde başarıyla açıldı ve kullanıcı tarafından incelendi.
* Durum: **M0 Miltaşı Çıkış Kriterleri Karşılandı (Milestone M0 Completed).**

---

## 4. Kararlar ve ADR'ler
* `docs/THREAT_MODEL.md` (v0) aktif.
* `docs/adr/ADR-001.md` (Stack ve Toolchain) aktif.
* `docs/STORAGE_AND_SINGLE_INSTANCE.md` (Depolama, ACL ve Tek Örnek Mimarisi) aktif.

---

## 5. Sıradaki Miltaşı ve Görev
* **Milestone M1 — Secure Vault Çekirdeği**
  - **M1.1:** Threat Model v1, VAULT_FORMAT v1 ve Key/KDF ADR'lerinin (ADR-002 Kripto Formatı ve Anahtar Ayrımı, ADR-003 Argon2id Parametreleri) hazırlanması.
