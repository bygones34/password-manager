# PasswordManager (Windows 11)

Windows 11 için modern, yerel öncelikli (local-first) ve güvenli masaüstü parola yöneticisi.

> **Durum:** Geliştirme aşamasında (Milestone M0 — Foundation ve Teknik Fizibilite)  
> **Kod Adı:** `PasswordManager`  
> **Hedef Platform:** Windows 11 (x64)

---

## Proje Hedefi

Windows 11 üzerinde Fluent Design standartlarına uygun bir ana arayüz, sistem bildirim alanında (System Tray) çalışan hızlı erişim mini kasası ve kullanıcı onaylı güvenli otomatik doldurma (Auto-fill) sağlayan bir parola yönetim çözümüdür.

### Temel Özellikler (v1 Kapsamı)
- **Yerel Öncelikli (Local-First):** Tüm veriler yerel SQLite veritabanında şifreli olarak saklanır. İlk sürüm tamamen çevrimdışı (offline) çalışır.
- **Kriptografik Güvenlik:** AES-256-GCM authenticated encryption, Argon2id anahtar türetimi (KDF), HKDF anahtar ayrımı ve her şifrelemede CSPRNG nonce kullanımı.
- **Modern Kullanıcı Deneyimi:** WinUI 3 + Windows App SDK tabanlı ana pencere, açık/koyu tema, erişilebilirlik ve klavye dostu gezinme.
- **Sistem Bildirim Alanı (System Tray):** Görev çubuğundan tek tıklamayla hızlı arama, kopyalama ve kasa durum kontrolü.
- **Doğrulanmış Auto-fill:** Hedef pencere (HWND), süreç (PID/path) ve denetim kimliğini doğrulayan, kullanıcı tarafından başlatılan güvenli doldurma (otomatik Enter/submit yoktur).
- **Veri Dayanıklılığı:** Atomik yedekleme (consistent snapshot) ve taşınabilir güvenli geri yükleme.

---

## Mimari ve Katmanlar

Çözüm temiz mimari (Clean Architecture) ve port-adapter prensiplerine göre yapılandırılmıştır:

```text
src/
  PasswordManager.Domain/              # Çekirdek iş kuralları ve modeller (bağımsız)
  PasswordManager.Application/         # Kullanım senaryoları ve port tanımları
  PasswordManager.Security/            # Kriptografi, Argon2id, AES-GCM port uygulamaları
  PasswordManager.Infrastructure/      # SQLite depolama, EF Core, dosya işlemleri
  PasswordManager.Platform.Windows/    # Windows OS servisleri (tray, session, clipboard)
  PasswordManager.Autofill.Windows/    # UI Automation hedef denetimi ve doldurma adaptörleri
  PasswordManager.Desktop/             # WinUI 3 composition root ve arayüz
```

---

## Geliştirme ve Çalışma Prensipleri

1. **Katı Güvenlik:** Master password diske yazılmaz veya loglanmaz. Kasa kilitliyken arayüzde hiçbir hesap metadata'sı gösterilmez.
2. **Küçük ve Doğrulanabilir Adımlar:** Geliştirmeler [DEVELOPMENT_ROADMAP.md](DEVELOPMENT_ROADMAP.md) üzerindeki miltaşları ve kabul kriterleri izlenerek yürütülür.
3. **Çalışma Kuralları:** Tüm süreç [AGENTS.md](AGENTS.md) kurallarına ve güvenlik sözleşmesine tabidir.
4. **Tehdit Modeli:** Başlangıç güvenlik sınırları ve tehdit analizi [docs/THREAT_MODEL.md](docs/THREAT_MODEL.md) içinde belgelenmiştir.

---

## Lisans ve Notlar
Bu repo henüz kamuya açık dağıtım veya gerçek parola kullanımı için hazır değildir. Testler sentetik verilerle yürütülmektedir.
