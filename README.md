# HumayQasimli — Portfolio + Vlog + İstifadəçi Sistemi + Admin Panel

ASP.NET Core 10 MVC, C#, Entity Framework Core, SQL Server, ASP.NET Core Identity əsasında
qurulmuş tam funksional platforma.

---

## ⚠️ Bu ZIP-i açmazdan əvvəl oxuyun

Bu layihə bir sandbox mühitində, **`dotnet` SDK və NuGet.org girişi olmadan** yazılmışdır.
Yəni kodun tərtib (compile) olunması Visual Studio-da / sizin maşınızda **ilk dəfə sizin
tərəfinizdən** yoxlanılmalıdır. Kodun məntiqi tam işlək olması üçün diqqətlə yazılıb, lakin
mən `dotnet build` icra edərək 0 xəta təsdiqi verə bilmədim. Aşağıdakı "Tez-tez rast gəlinən
xətalar" bölməsi buna görə əlavə edilib.

---

## 1. Tələb olunan proqramlar

- Visual Studio 2026 (və ya daha yeni) — "ASP.NET and web development" workload ilə
- .NET 10 SDK
- SQL Server (LocalDB, Express və ya tam versiya) — SQL Server Management Studio (SSMS) tövsiyə olunur
- (İstəyə bağlı) Gmail/SMTP hesabı — email OTP göndərmək üçün
- (İstəyə bağlı) Google Cloud Console layihəsi — Google login üçün

## 2. Layihəni açmaq

1. ZIP-i istədiyiniz qovluğa çıxarın — içindən `HumayQasimli` adlı bir qovluq çıxacaq.
2. Visual Studio-nu açın → **File → Open → Project/Solution** → çıxan qovluğun içindəki
   **`HumayQasimli.csproj`** faylını seçin. (Və ya Visual Studio-nun başlanğıc ekranında
   "Open a local folder" seçib birbaşa `HumayQasimli` qovluğunu göstərin.)
3. Visual Studio paketləri avtomatik "restore" edəcək (NuGet). İnternet bağlantınız olduğundan
   əmin olun.
4. Restore bitdikdən sonra Solution Explorer-də layihə görünəcək, F5 ilə işə sala bilərsiniz.

> Bu ZIP-də ayrıca `.sln` faylı yoxdur — Visual Studio `.csproj`-u birbaşa açanda öz-özünə
> müvəqqəti bir solution konteksti yaradır, bu tamam normaldır və layihənin işləməsinə
> mane olmur.

### Vlog-larda video necə əlavə olunur (Embed vs Xarici keçid)

Admin Panel → Vlogs → Yeni/Redaktə səhifəsində **"Video növü"** seçimi var:

- **Embed player** — YouTube/Vimeo kimi embed dəstəkləyən platformalardan olan videolar
  saytın öz içində birbaşa oynayır (iframe). Link `embed/` formatında olmalıdır
  (məs: `https://www.youtube.com/embed/VIDEO_ID`).
- **Xarici keçid** — Instagram, LinkedIn, TikTok, Facebook kimi platformalardan olan,
  embed edilə bilməyən videolar üçün seçin. Adi paylaşım linkini yapışdırmaq kifayətdir
  (məs: `https://www.instagram.com/reel/...`). Sayt bu halda videonu iframe-də göstərmək
  əvəzinə "Instagram-da izləyin" kimi bir keçid kartı göstərir və istifadəçi linkə
  kliklədikdə həmin platforma yeni pəncərədə açılır.

Beləliklə, 4-5 vloqu tam embed (YouTube/Vimeo) olaraq, qalanlarını isə sadəcə xarici keçid
kimi əlavə edə bilərsiniz — hər ikisi eyni Vlog formu üzərindən idarə olunur.

## 3. User Secrets ilə həssas məlumatların qurulması

**Heç bir parol və ya açar kodda yazılmayıb.** Bütün həssas dəyərləri Visual Studio-da
User Secrets ilə əlavə edin:

Solution Explorer-də layihəyə sağ klikləyin → **Manage User Secrets** → açılan
`secrets.json` faylına aşağıdakıları yazın (öz dəyərlərinizlə):

```json
{
  "ConnectionStrings:DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=HumayQasimliDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True",

  "SeedAdmin:Email": "admin@example.com",
  "SeedAdmin:Password": "Admin123!Strong",
  "SeedAdmin:UserName": "admin",

  "EmailSettings:SmtpHost": "smtp.gmail.com",
  "EmailSettings:SmtpPort": "587",
  "EmailSettings:SenderEmail": "sizin.email@gmail.com",
  "EmailSettings:Username": "sizin.email@gmail.com",
  "EmailSettings:Password": "gmail-app-password",
  "EmailSettings:EnableSsl": "true",

  "Authentication:Google:ClientId": "xxxxx.apps.googleusercontent.com",
  "Authentication:Google:ClientSecret": "xxxxx"
}
```

> Əgər `EmailSettings` doldurulmayıbsa, tətbiq xəta vermir — OTP kodu real göndərilmir,
> əvəzinə Visual Studio-nun **Output / Debug** pəncərəsində log kimi görünür (inkişaf zamanı
> test üçün rahatdır).
>
> Əgər `Authentication:Google` doldurulmayıbsa, Google login düyməsi görünsə də, backend-də
> Google sxemi qeydiyyatdan keçmədiyi üçün klik etdikdə xəta verəcək. Google login-i istifadə
> etməzdən əvvəl mütləq doldurun (bax: bölmə 6).
>
> Əgər `SeedAdmin:Email` / `SeedAdmin:Password` doldurulmayıbsa, default admin hesabı
> yaradılmır — tətbiq işə düşür, amma admin panelə girmək üçün əvvəlcə adi qeydiyyatdan
> keçib sonra `AspNetUserRoles` cədvəlinə əl ilə "Admin" rolu əlavə etməli olacaqsınız.

## 4. Verilənlər bazasının yaradılması (Migration)

Mən bu mühitdə `dotnet-ef` alətini işlədə bilmədiyim üçün **Migrations qovluğu ZIP-də yoxdur** —
bunu ilk dəfə sizin maşınınızda yaratmaq lazımdır (bu, EF Core-un normal iş axınıdır):

**Package Manager Console-da** (Visual Studio → Tools → NuGet Package Manager → Package Manager Console):

```powershell
Add-Migration InitialCreate
Update-Database
```

və ya **terminalda**:

```bash
dotnet tool install --global dotnet-ef   # əgər quraşdırılmayıbsa
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Tətbiq həmçinin `Program.cs` içində başlanğıcda `db.Database.MigrateAsync()` çağırır, yəni
migration yaradıldıqdan sonra növbəti işə salınmalarda avtomatik tətbiq olunacaq.

## 5. İşə salmaq

Visual Studio-da **F5** (və ya `dotnet run`). Tətbiq ilk açılışda:

- Bütün cədvəlləri yaradır (migration tətbiq olunur)
- "Admin" və "User" rollarını yaradır
- (əgər `SeedAdmin` doldurulubsa) default admin hesabını yaradır
- Default kateqoriyalar və sayt tənzimləmələri əlavə edir

Admin panelə giriş: `/Admin/Dashboard` (yalnız Admin rolu ilə daxil olan istifadəçilər üçün).

## 6. Google Login qurulması

1. https://console.cloud.google.com → yeni layihə yaradın (və ya mövcud birini seçin)
2. **APIs & Services → Credentials → Create Credentials → OAuth client ID**
3. Application type: **Web application**
4. Authorized redirect URI-yə əlavə edin:
   `https://localhost:{PORT}/signin-google` (PORT-u `launchSettings.json`-dan görə bilərsiniz,
   məsələn `https://localhost:7100/signin-google`)
5. Client ID və Client Secret-i User Secrets-ə yazın (bax: bölmə 3).

## 7. Email OTP (SMTP) qurulması — Gmail nümunəsi

1. Gmail hesabınızda 2 addımlı doğrulamanı aktivləşdirin.
2. https://myaccount.google.com/apppasswords ünvanından **App Password** yaradın.
3. Bu 16 simvollu parolu `EmailSettings:Password` olaraq User Secrets-ə yazın (adi Gmail
   parolunuzu YOX).
4. `EmailSettings:SenderEmail` və `EmailSettings:Username` eyni Gmail ünvanı olmalıdır.

Başqa SMTP provayder istifadə etmək istəsəniz (SendGrid, Mailtrap, öz domen poçtunuz və s.),
sadəcə `SmtpHost`/`SmtpPort`/`Username`/`Password` dəyərlərini uyğun olaraq dəyişin.

## 8. Fayl yükləmə qovluqları

`wwwroot/uploads/` altında `vlogs`, `portfolio`, `profile`, `certificates`, `trainings`,
`media` qovluqları avtomatik yaradılır (ilk yükləmədə). Bu qovluqlar `.gitignore`-a əlavə
edilməlidir ki, real fayllar versiya nəzarətinə düşməsin (sadəcə `.gitkeep` saxlayın).

## 9. Layihə strukturu

```
HumayQasimli/
├── Areas/Admin/            → Admin Panel (yalnız "Admin" rolu)
│   ├── Controllers/
│   └── Views/
├── Controllers/             → İctimai sayt + istifadəçi controller-ləri
├── Data/
│   ├── ApplicationDbContext.cs
│   └── DbSeeder.cs
├── Models/
│   ├── Entities/            → EF Core entity-ləri
│   └── ViewModels/
├── Services/                 → Email, OTP, fayl yükləmə, bildiriş servisləri
├── Views/                    → Razor səhifələri
├── wwwroot/
└── Program.cs
```

## 10. Tez-tez rast gəlinən xətalar və həlli

| Xəta | Səbəb / Həll |
|---|---|
| `Unable to connect to any of the specified MySQL/SQL Server hosts` | `ConnectionStrings:DefaultConnection` yanlışdır və ya SQL Server servisi işləmir. LocalDB üçün `SqlLocalDB.exe` işlədiyindən əmin olun. |
| `No migrations found` / verilənlər bazası yoxdur | Bölmə 4-də göstərilən `Add-Migration` + `Update-Database` addımlarını icra edin. |
| Google login klikləndikdə 500 xəta | `Authentication:Google:ClientId/ClientSecret` User Secrets-də boşdur — Program.cs Google sxemini yalnız bu dəyərlər varsa qeydiyyatdan keçirir. |
| Email göndərilmir, amma xəta da yoxdur | `EmailSettings` konfiqurasiyası boşdur — bu qəsdən belədir (development rahatlığı üçün), OTP kodu Output/Debug loguna yazılır. |
| Paketlər tapılmır / restore xətası | Visual Studio-da sağ klik → **Restore NuGet Packages**, və ya `dotnet restore`. İnternet bağlantısını yoxlayın. |
| `CS0246` tipində "tapılmadı" xətaları | Adətən restore tamamlanmayıb — restore bitənə qədər gözləyin, sonra **Build → Rebuild Solution**. |
| Admin panelə giriş "Access Denied" verir | Hesabınızda "Admin" rolu yoxdur. `SeedAdmin` dəyərlərini doldurub yenidən işə salın, və ya `AspNetUsers`/`AspNetRoles`/`AspNetUserRoles` cədvəllərinə SSMS ilə əl ilə əlavə edin. |
| Bootstrap/JS görünmür | Layihə Bootstrap 5 və Bootstrap Icons-u CDN-dən yükləyir (`cdn.jsdelivr.net`) — internet bağlantısı lazımdır. Offline işləmək istəsəniz, bu faylları `wwwroot/lib/`-ə endirib `_Layout.cshtml` / `_AdminLayout.cshtml`-də linkləri dəyişin. |

## 11. Vacib qeydlər

- **Bütün parollar / API açarları User Secrets və ya environment variable ilə verilməlidir.**
  `appsettings.json`-da bu sahələr qəsdən boş saxlanılıb.
- Production-a keçərkən `appsettings.json`-dakı `ConnectionStrings` və digər sahələri
  **environment variables** və ya Azure Key Vault kimi bir sirr idarəetmə sistemi ilə əvəz edin.
- Fayl yükləmə: uzantı + MIME + magic-number yoxlaması, təsadüfi fayl adı (GUID), ölçü limiti
  (`FileUploadSettings:MaxFileSizeBytes`) tətbiq olunur.
- Şifrələr ASP.NET Core Identity tərəfindən hash olunur (PBKDF2), heç vaxt açıq mətn
  saxlanılmır.
- Account lockout: 5 uğursuz cəhddən sonra hesab 15 dəqiqəliyə bloklanır.
- Anti-forgery token bütün POST formalarında (`[ValidateAntiForgeryToken]` /
  `[AutoValidateAntiforgeryToken]`) tətbiq olunur.

## 12. Nə üçün Migrations qovluğu yoxdur?

EF Core migration faylları .NET-in reflection əsaslı `dotnet-ef` aləti ilə real DbContext-i
işə salaraq generasiya olunur. Bu mühitdə `dotnet` SDK quraşdırılmadığı üçün bu addımı sizin
adınıza saxta/əl ilə yazmaq əvəzinə (bu, modellə uyğun olmayan və daha çox baş ağrısı yaradan
migration-lara səbəb ola bilərdi), bölmə 4-dəki standart EF Core iş axınına etibar etməyi seçdim.
Bu, tam adi VS layihələrində belə edilir.

---

Suallarınız olarsa və ya build zamanı konkret xəta mesajı alsansız, mənə mesajı olduğu kimi
göndərin — birlikdə həll edə bilərik.
