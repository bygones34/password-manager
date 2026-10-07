# Project Status

Last updated: 2026-10-06
Current milestone: M0 (Foundation ve teknik fizibilite)
Current task: M0.4 (Tamamlandı) / M0.5 (Sırada)

---

## 1. Tamamlanan Görevler
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
  - `SyntheticVaultItem` modeli ve `SyntheticVaultService` (sentetik, test-only kasa verileri) oluşturuldu.
  - Modern MVVM mimarisine uygun `MainViewModel` (C# 13/14 partial properties, filtreleme, arama, parola maskeleme/gösterim, favori yönetimi ve dinamik tema geçişi) geliştirildi.
  - `MainWindow.xaml` & `MainWindow.xaml.cs` hazırlanarak sol tarafta `NavigationView` (All Items, Favorites, Logins, Servers, Secure Notes), üstte arama/tema çubuğu, ortada liste görünümü ve sağda zengin detay paneli (Master-Detail) kuruldu.
  - Tüm çözüm **0 hata ve 0 uyarı** ile derlendi; 6 test projesi sorunsuz geçti.

---

## 2. Doğrulama ve Çalıştırılan Komutlar
* `dotnet build` → 13 projenin tamamı **0 hata ve 0 uyarı** ile başarıyla derlendi (2.6 sn).
* `dotnet test` → 6 test projesindeki tüm testler başarıyla geçti (**7 test geçti, 0 hata**).
* `git diff --check` → Temiz.

---

## 3. Manuel Doğrulama Durumu
* XAML ikili derlemesi (`App.xbf`, `MainWindow.xbf`, `Tokens.xbf`) ve PRI dosya üretimi (`PasswordManager.Desktop.pri`) doğrulandı.
* Durum: Tamamlandı (M0.4 WinUI shell ve MVVM arayüz temeli).

---

## 4. Kararlar ve ADR'ler
* `docs/THREAT_MODEL.md` (v0) aktif.
* `docs/adr/ADR-001.md` (Stack ve Toolchain) aktif.

---

## 5. Bilinen Riskler ve Engeller
* M0.5'te System Tray mini penceresi, çoklu monitör/DPI konumlandırması ve foreground HWND yakalama yetenekleri native Win32 interop ile test edilecek.

---

## 6. Sıradaki Görev
* **M0.5:** Native spike: system tray ikonu + mini pencere, monitör/DPI konumu, foreground HWND yakalama ve Windows session olaylarına (lock/unlock) abonelik prototipi.
