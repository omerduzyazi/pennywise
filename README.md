# PennyWise — Kişisel Finans ve Bütçe Takip Sistemi

**Mimari ve Tasarım Kararları İçin Akademik Dokümantasyon**
*Oluşturulma Tarihi: 2026-06-04*

---

## 1. Mimari Tasarım Kararları

### 1.1 Katmanlı Mimari ve Sorumlulukların Ayrılığı
PennyWise sistemi, dört bağımsız derlemeden (assembly) oluşan katmanlı (N-Tier) bir mimariyi benimser: `PennyWise.API`, `PennyWise.Domain`, `PennyWise.Infrastructure`, ve `PennyWise.Tests`. Bu ayrıştırma, SOLID prensiplerine, özellikle Tek Sorumluluk Prensibi (SRP) ve Bağımlılığı Tersine Çevirme Prensibine (DIP) dayanmaktadır. Domain katmanı, sıfır harici bağımlılıkla tüm iş varlıklarını ve numaralandırmaları (enum) kapsar; böylece temel iş mantığının veri kalıcılığı (persistence), web framework kuralları veya üçüncü taraf kütüphane uygulamalarından bağımsız kalması sağlanır. Infrastructure katmanı, PostgreSQL bağlantısı için Npgsql sağlayıcısı ile yapılandırılmış Entity Framework Core 8.0 aracılığıyla veri erişim sorumluluğunu üstlenir. API katmanı, bağımlılık enjeksiyonunu ve HTTP işlem hattı yapılandırmasını yöneten kompozisyon kökü (composition root) olarak işlev görür. Bu katmanlaşma, bağımlılık akışının kesinlikle tek yönlü olduğu - dış katmanların iç katmanlara bağımlı olduğu, asla tersi olmadığı - Clean Architecture paradigmasını yansıtır.

### 1.2 Teknoloji Yığını Seçimi
Arka uç (backend) çalışma zamanı olarak .NET 8.0'ın seçilmesi; uzun vadeli destek (LTS) sınıflandırmasına, üstün performans kriterlerine ve minimal API'ler ile bağımlılık enjeksiyonuna (DI) sunduğu doğal desteğe dayanmaktadır. SQL Server yerine PostgreSQL 16'nın seçilmesinin nedeni; açık kaynaklı lisans modeli, yarı yapılandırılmış veriler için sunduğu JSONB desteği ve üretim ortamlarında kanıtlanmış ölçeklenebilirliğidir. Ön uçta (frontend), framework'e özgü derleme araçlarının getirdiği yükü ortadan kaldırmak için Vanilla HTML, CSS ve JavaScript kullanılmıştır. Bu sayede, DOM ve oluşturma yaşam döngüsü üzerinde tam kontrol sürdürülürken dağıtım karmaşıklığı azaltılır. Nginx 1.25, olay odaklı mimarisi ve ihmal edilebilir bellek alanı kaplaması nedeniyle ters vekil sunucu (reverse proxy) ve statik dosya sunucusu olarak seçilmiştir.

---

## 2. Konteynerleştirme Stratejisi

### 2.1 Önce-Docker (Docker-First) Geliştirme Paradigması
Proje, veritabanı, API ve ön uç gibi tüm hizmetlerin Docker konteynerleri içine hapsedildiği ve Docker Compose v3.9 aracılığıyla yönetildiği bir Docker-first geliştirme felsefesini zorunlu kılar. Bu yaklaşım, geliştirme, test ve üretim aşamalarında ortam eşitliğini garanti eder. API için çok aşamalı (multi-stage) Dockerfile, derleme için SDK imajını ve yürütme için ASP.NET çalışma zamanı (runtime) imajını kullanarak, tek aşamalı derlemelere kıyasla üretim imajı boyutunda yaklaşık %70'lik bir küçülme sağlar.

### 2.2 Hizmet Orkestrasyonu ve Ağ İletişimi
`docker-compose.yml`, açık bağımlılık zincirlerine sahip üç hizmet tanımlar. PostgreSQL konteyneri bir sağlık kontrolü (health check) sunar ve API konteynerinin başlatılması, veritabanının sağlıklı bir duruma ulaşmasına (`condition: service_healthy`) bağlıdır. Tüm konteynerler kullanıcı tanımlı bir köprü ağı (`pennywise-network`) üzerinden iletişim kurarak DNS tabanlı hizmet keşfi (service discovery) sağlar. Nginx konteyneri, ön uçtan gelen API isteklerini (`/api/*`) arka uç konteynerine yönlendirerek (reverse-proxy), ön ucun tek kökenli (single-origin) dağıtım modelini korurken çapraz köken (cross-origin) kısıtlamaları olmadan API ile etkileşime girmesini sağlar.

---

## 3. CI/CD Pipeline Mimarisi

### 3.1 GitHub Actions ile Sürekli Entegrasyon (CI)
CI/CD işlem hattı GitHub Actions aracılığıyla uygulanır ve birbirini izleyen iki işten (job) oluşur: `build-and-test` ve `docker-build`. İlk iş, NuGet bağımlılıklarını geri yükler, çözümü (solution) Release konfigürasyonunda derler ve kod kapsamı (code coverage) toplamasıyla birlikte xUnit test paketini çalıştırır. Birinci işin başarılı bir şekilde tamamlanmasına bağlı olan ikinci iş, Docker imajlarını oluşturarak ve docker-compose yapılandırmasını doğrulayarak Docker imajlarını onaylar. Bu geçit (gating) mekanizması, fail-fast (hızlı hata ver) prensibine bağlı kalarak test paketinden geçemeyen bir kod tabanından hiçbir Docker yapısının üretilmemesini sağlar.

### 3.2 Test Stratejisi
Test projesi, genişletilebilirlik modeli ve paralel test çalıştırma yetenekleri nedeniyle seçilen xUnit'i test framework'ü olarak kullanır. Entegrasyon testleri, süreç içi (in-process) bir test sunucusu oluşturmak için `WebApplicationFactory`'den yararlanır ve PostgreSQL DbContext'i bir EF Core InMemory sağlayıcısıyla değiştirir. Bu değiştirme, canlı bir veritabanı örneğine ihtiyaç duymadan CI ortamlarında testin yürütülmesini sağlar. FluentAssertions, testlerin sürdürülebilirliğini artıran akıcı (fluent), okunabilir bir doğrulama (assertion) sözdizimi sağlar.

---

## 4. Kimlik Doğrulama ve Güvenlik

### 4.1 Kimlik Doğrulama Stratejisi: JWT vs Oturum (Session) Tabanlı
PennyWise sistemi, geleneksel sunucu taraflı oturum (session) yönetimi yerine JSON Web Token (JWT) tabanlı kimlik doğrulama kullanır. Birincisi, JWT token'ları kendi içlerinde bağımsızdır (self-contained); RESTful mimari tasarımının durumsallık (stateless) kısıtlamasıyla hizalanarak yatay ölçeklenebilirliği mümkün kılar. İkincisi, konteynerize edilmiş bir dağıtım bağlamında, durumsuz kimlik doğrulama "sticky session" (yapışkan oturum) anti-örüntüsünü (anti-pattern) ortadan kaldırır. Üçüncüsü, JWT token'ları `Microsoft.AspNetCore.Authentication.JwtBearer` ara katman yazılımı (middleware) tarafından doğal olarak desteklenir. Token, 512 bitlik simetrik bir anahtara sahip HMAC-SHA256 kullanılarak imzalanır.

### 4.2 Parola Hashleme: BCrypt Stratejisi
Kullanıcı parolaları, `BCrypt.Net-Next` NuGet paketi ile uygulanan BCrypt uyarlanabilir hashleme algoritması kullanılarak hashlenir. BCrypt, yapılandırılabilir iş faktörleri sayesinde kaba kuvvet (brute-force) saldırılarına karşı doğal direncinden dolayı seçilmiştir. Uyarlanabilir maliyet faktörü, parola doğrulamasının hesaplama maliyetinin donanım geliştikçe ölçeklenmesini sağlar. Bu yaklaşım, OWASP'ın Parola Depolama Kopya Kağıdı (Password Storage Cheat Sheet) önerilerine uyar.

---

## 5. Veri Erişimi ve Domain Modeli

### 5.1 Repository Pattern Gerekçesi
Domain katmanında tanımlanan jenerik `IRepository<T>` arayüzü, tüm veri erişim işlemlerini soyutlar. Somut (concrete) uygulama olan `Repository<T>`, Infrastructure katmanında yer alır ve Entity Framework Core'un `DbSet<T>` sınıfından yararlanır. Bu dolaylı yönlendirme birden çok amaca hizmet eder:
- **Test Edilebilirlik:** Denetleyiciler (Controllers) taklit (mock) uygulamalar kullanılarak test edilebilir.
- **Sorumlulukların Ayrılığı:** Domain katmanı altyapı bağımlılıklarından tamamen arınmış kalır.
- **Açık/Kapalı Prensibi (OCP):** Yeni varlık türleri, mevcut kodu değiştirmeden jenerik kayıt işlemiyle tam CRUD yeteneklerini otomatik olarak kazanır.

### 5.2 Varlık Hiyerarşisi
Tüm domain varlıkları, Guid tabanlı bir birincil anahtar (`Id`) ve denetim zaman damgaları (`CreatedAt`, `UpdatedAt`) sağlayan `BaseEntity` sınıfından miras alır. Birincil anahtarlar olarak otomatik artan tam sayılar yerine GUID'lerin kullanılması, bunların dağıtık sistemlere olan uygunluğu ve veritabanı gidiş-dönüşlerine gerek kalmadan istemci tarafında üretilebilme yeteneklerinden kaynaklanmaktadır.

### 5.3 Veritabanı Geçiş (Migration) Stratejisi
İlk veritabanı geçişi; beş temel tablonun (Users, Transactions, Budgets, Portfolios, Holdings) tümünü, bunlarla ilişkili yabancı anahtar (foreign key) kısıtlamalarını ve performans açısından kritik dizinleri (özellikle kimlik doğrulama sırasında O(1) arama hızını sağlamak için `Users.Email` üzerinde benzersiz bir dizin) içeren tam ilişkisel şemayı oluşturur. Geçiş (migration), uygulama başlangıcında `PennyWiseDbContext.Database.Migrate()` aracılığıyla otomatik olarak yürütülür.

---

## 6. API Tasarımı ve Veri Sahipliği

### 6.1 CRUD Tasarım Örüntüleri ve RESTful Kuralları
Transactions, Budgets ve Portfolios işlemleri RESTful mimari kurallarına sıkı sıkıya bağlıdır. Her bir denetleyici (controller) belirli bir kaynak koleksiyonunu hedefler ve standart HTTP metotlarını (GET, POST, PUT, DELETE) kullanır. C# 9.0 kayıt (record) türleri kullanılarak uygulanan Veri Transfer Nesneleri (DTO'lar), istemci ile API arasında net bir sözleşme kurarak dahili domain varlıklarının hiçbir zaman doğrudan açığa çıkmamasını sağlar. Bu ayrıştırma, aşırı veri gönderme (over-posting) zafiyetlerini önler.

### 6.2 Veri Sahipliği ve Çoklu Kiracı (Multi-Tenant) Sorgu Kapsamı
PennyWise birden fazla kullanıcıya eşzamanlı olarak hizmet verdiğinden, veri yalıtımı kritik bir güvenlik gereksinimidir. Veritabanıyla etkileşime giren her API isteği, depo (repository) düzeyinde "kiracı kapsamı" (tenant scoping) zorunluluğu uygular. Geçerli kullanıcının kimliği doğrudan doğrulanmış JWT taleplerinden (claims) çıkarılır ve her veritabanı sorgusuna zorunlu bir koşul olarak eklenir (örneğin, `_repo.FindAsync(t => t.UserId == userId)`). Bu tasarım, doğrulanmış bir kullanıcının yalnızca kendisine ait kayıtları alabilmesini, değiştirebilmesini veya silebilmesini garanti ederek Yatay Yetki Yükseltmesini (Horizontal Privilege Escalation) önler.

### 6.3 Sayfalama ve Filtreleme Stratejisi
İşlem hacmi arttıkça ölçeklenebilir performans sağlamak için, `GET /api/transactions` uç noktası sunucu taraflı sayfalama ve filtreleme uygular. API, tüm veri setini döndürmek yerine `page` ve `pageSize` sorgu parametrelerini kabul eder ve isteği verimli SQL `OFFSET` ve `FETCH NEXT` cümleciklerine çevirmek için LINQ'in `Skip()` ve `Take()` operatörlerini kullanır. Dinamik filtreleme yetenekleri doğrudan `IQueryable` arayüzüne uygulanarak veritabanı I/O işlemleri en aza indirilir.

### 6.4 Bütçe Takip Mantığı ve Veri Birleştirme (Aggregation)
`GET /api/budgets/status` uç noktası, varlıklar arası (cross-entity) veri birleştirme için bir örnek teşkil eder. API, kalan bakiyeyi hesaplamak için ön ucun (frontend) tüm bütçeleri ve tüm işlemleri indirmesini gerektirmek yerine, bu birleştirmeyi sunucu tarafında gerçekleştirir. Belirli bir ay ve yıl için sistem, o kullanıcıya ait tüm bütçeleri ve tüm harcama (expense) işlemlerini getirir. Ardından harcamaları kategoriye göre gruplandırır, toplamı hesaplar ve istemciye önceden hesaplanmış, görüntülenmeye hazır metrikler döndürür.

---

## 7. Gelişmiş Analitik: Portföy Performansı

### 7.1 Analitik için Servis Katmanı Örüntüsü
Proje, `IPortfolioAnalyticsService` aracılığıyla Servis Katmanı (Service Layer) örüntüsünü kullanır. Denetleyiciler (controllers) içindeki Repository örüntüsü tarafından yeterli bir şekilde ele alınan standart CRUD işlemlerinin aksine, karmaşık finansal hesaplamalar (TWR gibi) saf domain iş mantığını temsil eder. Bu mantığı özel bir servise (`PortfolioAnalyticsService`) çıkararak ve Bağımlılık Enjeksiyonu (DI) yoluyla kaydederek, Tek Sorumluluk Prensibi'ne (SRP) uymuş oluyoruz. Denetleyici yalnızca HTTP istek/yanıt yönetimi ve yetkilendirmeden sorumlu olurken, matematiksel hesaplamaları servis katmanına devreder.

### 7.2 Zaman Ağırlıklı Getiri (TWR) Hesaplama Modeli
Zaman Ağırlıklı Getiri (TWR) metriğinin amacı, nakit giriş ve çıkışlarının bozucu etkilerini ortadan kaldırarak bir portföyün bileşik büyüme oranını değerlendirmektir.

PennyWise'ın ilk aşaması için basitleştirilmiş bir yaklaşım modeli uygulanmıştır. Sistem, bireysel varlıkları (her bir nakit akışı için zaman damgalı defter girişleri yerine) ortalama bir `PurchasePrice` (Alış Fiyatı) ve `CurrentPrice` (Güncel Fiyat) ile takip ettiğinden, TWR hesaplaması şu şekilde basitleştirilmiştir:
`TWR = (Sum(CurrentValue) / Sum(CostBasis)) - 1`

Bu formül, elde tutma süresi (holding period) boyunca elde edilen mutlak yüzde getirisini temsil eder. Uygulama, nakit defteri muhasebesi ve günlük piyasa değerlemelerini (mark-to-market) içerecek şekilde gelecekteki adımlarda olgunlaştıkça, `IPortfolioAnalyticsService`, API denetleyicilerinde veya ön uçta (frontend) herhangi bir değişiklik gerektirmeden Bağımlılık Enjeksiyonu (DI) aracılığıyla daha sofistike bir uygulamayla (örneğin, modifiye edilmiş Dietz metodu) değiştirilebilir.
