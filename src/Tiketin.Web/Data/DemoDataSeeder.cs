using Microsoft.EntityFrameworkCore;
using Tiketin.Web.Domain;

namespace Tiketin.Web.Data;

/// <summary>
/// Development-only demo content: ~40 tickets spread over the last 60 days with a
/// realistic status mix (mostly closed, some SLA breaches, some ratings) and 8
/// Indonesian KB articles. Idempotent: skipped when tickets already exist.
/// </summary>
public static class DemoDataSeeder
{
    // Fixed seed so demo data is reproducible across machines.
    private const int RandomSeed = 20260705;

    private record TicketSpec(string Title, string Description, string Category, TicketPriority Priority);

    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (await db.Tickets.AnyAsync(ct))
        {
            return;
        }

        var random = new Random(RandomSeed);
        var now = DateTimeOffset.UtcNow;

        var categories = await db.Categories.ToDictionaryAsync(c => c.Name, ct);
        var users = await db.Users.ToListAsync(ct);
        var roleByUser = await (
            from ur in db.UserRoles
            join r in db.Roles on ur.RoleId equals r.Id
            select new { ur.UserId, r.Name }).ToListAsync(ct);

        var technicians = users
            .Where(u => roleByUser.Any(x => x.UserId == u.Id && x.Name == "Technician"))
            .ToList();
        var employees = users
            .Where(u => roleByUser.Any(x => x.UserId == u.Id && x.Name == "Employee"))
            .ToList();
        var admin = users.Single(u => roleByUser.Any(x => x.UserId == u.Id && x.Name == "Admin"));

        if (technicians.Count == 0 || employees.Count == 0)
        {
            return; // user seed did not run; nothing sensible to do
        }

        var monthCounters = new Dictionary<string, int>();
        var tickets = new List<Ticket>();
        var events = new List<TicketEvent>();

        var specs = TicketSpecs();
        for (var i = 0; i < specs.Length; i++)
        {
            var spec = specs[i];
            var category = categories[spec.Category];
            var reporter = employees[random.Next(employees.Count)];

            // Spread creation over the last 60 days, biased toward recent weeks.
            // The last few specs stay minutes old (inside every response SLA) so
            // the queue always shows live countdowns next to the breached ones.
            var isFresh = i >= specs.Length - 3;
            var createdAt = isFresh
                ? now.AddMinutes(-random.Next(10, 70))
                : now.AddDays(-Math.Pow(random.NextDouble(), 2.2) * 60).AddMinutes(-random.Next(0, 540));
            var ageDays = (now - createdAt).TotalDays;

            var yearMonth = createdAt.UtcDateTime.ToString("yyyyMM");
            monthCounters[yearMonth] = monthCounters.GetValueOrDefault(yearMonth) + 1;

            var ticket = new Ticket
            {
                Id = Guid.NewGuid(),
                TicketNumber = $"TKT-{yearMonth}-{monthCounters[yearMonth]:D4}",
                Title = spec.Title,
                Description = spec.Description,
                CategoryId = category.Id,
                Priority = spec.Priority,
                Status = TicketStatus.Open,
                ReporterId = reporter.Id,
                CreatedAt = createdAt
            };

            events.Add(NewEvent(ticket, reporter.Id, TicketEventType.Created, null, ticket.TicketNumber, createdAt));

            // Older tickets are overwhelmingly finished; recent ones mixed.
            var roll = random.NextDouble();
            var target = ageDays switch
            {
                > 14 => roll < 0.82 ? TicketStatus.Closed : TicketStatus.Resolved,
                > 5 => roll < 0.45 ? TicketStatus.Closed
                     : roll < 0.70 ? TicketStatus.Resolved
                     : roll < 0.85 ? TicketStatus.InProgress
                     : TicketStatus.Open,
                _ => roll < 0.20 ? TicketStatus.Resolved
                     : roll < 0.55 ? TicketStatus.InProgress
                     : TicketStatus.Open
            };

            if (target != TicketStatus.Open)
            {
                var technician = technicians[random.Next(technicians.Count)];
                var assignedAt = createdAt.AddMinutes(random.Next(5, 180));
                ticket.AssigneeId = technician.Id;
                events.Add(NewEvent(ticket, admin.Id, TicketEventType.Assigned, null, technician.FullName, assignedAt));

                // ~25% of handled tickets breach the response SLA.
                var responseFactor = random.NextDouble() < 0.25
                    ? 1.1 + random.NextDouble()
                    : 0.15 + random.NextDouble() * 0.7;
                ticket.FirstResponseAt = createdAt.AddMinutes(category.SlaResponseMinutes * responseFactor);

                var inProgressAt = ticket.FirstResponseAt.Value;
                events.Add(NewEvent(ticket, technician.Id, TicketEventType.StatusChanged,
                    nameof(TicketStatus.Open), nameof(TicketStatus.InProgress), inProgressAt));
                ticket.Status = TicketStatus.InProgress;

                if (target is TicketStatus.Resolved or TicketStatus.Closed)
                {
                    // ~20% breach the resolution SLA.
                    var resolutionFactor = random.NextDouble() < 0.20
                        ? 1.1 + random.NextDouble() * 1.5
                        : 0.2 + random.NextDouble() * 0.7;
                    var resolvedAt = createdAt.AddMinutes(category.SlaResolutionMinutes * resolutionFactor);
                    if (resolvedAt <= inProgressAt)
                    {
                        resolvedAt = inProgressAt.AddMinutes(random.Next(30, 240));
                    }

                    ticket.Status = TicketStatus.Resolved;
                    ticket.ResolvedAt = resolvedAt;
                    events.Add(NewEvent(ticket, technician.Id, TicketEventType.StatusChanged,
                        nameof(TicketStatus.InProgress), nameof(TicketStatus.Resolved), resolvedAt));

                    if (random.NextDouble() < 0.6)
                    {
                        ticket.SatisfactionRating = (short)(random.NextDouble() < 0.75
                            ? random.Next(4, 6)
                            : random.Next(2, 4));
                        ticket.SatisfactionNote = ticket.SatisfactionRating >= 4
                            ? RatingNotes[random.Next(RatingNotes.Length)]
                            : null;
                    }

                    if (target == TicketStatus.Closed)
                    {
                        var closedAt = resolvedAt.AddDays(7);
                        ticket.Status = TicketStatus.Closed;
                        ticket.ClosedAt = closedAt > now ? now : closedAt;
                        events.Add(NewEvent(ticket, null, TicketEventType.StatusChanged,
                            nameof(TicketStatus.Resolved), nameof(TicketStatus.Closed), ticket.ClosedAt.Value));
                    }
                }

                ticket.UpdatedAt = events.Where(e => e.TicketId == ticket.Id).Max(e => e.CreatedAt);
            }

            tickets.Add(ticket);
        }

        db.Tickets.AddRange(tickets);
        db.TicketEvents.AddRange(events);
        await db.SaveChangesAsync(ct);

        // Runtime numbers continue after the seeded ones for the current month.
        var currentMonth = now.UtcDateTime.ToString("yyyyMM");
        if (monthCounters.TryGetValue(currentMonth, out var used))
        {
            var sequence = $"ticket_seq_{currentMonth}";
            // EF1002: identifier comes from a date format string, not user input.
#pragma warning disable EF1002
            await db.Database.ExecuteSqlRawAsync($"CREATE SEQUENCE IF NOT EXISTS {sequence}", ct);
            await db.Database.ExecuteSqlRawAsync($"SELECT setval('{sequence}', {used})", ct);
#pragma warning restore EF1002
        }

        await SeedKbArticlesAsync(db, categories, technicians, random, now, ct);
    }

    private static TicketEvent NewEvent(
        Ticket ticket, Guid? actorId, TicketEventType type, string? oldValue, string? newValue, DateTimeOffset at)
        => new()
        {
            TicketId = ticket.Id,
            ActorId = actorId,
            EventType = type,
            OldValue = oldValue,
            NewValue = newValue,
            CreatedAt = at
        };

    private static readonly string[] RatingNotes =
    [
        "Cepat ditangani, terima kasih.",
        "Penjelasannya mudah diikuti.",
        "Masalah selesai dalam sekali kunjungan.",
        "Respon ramah dan solusinya berhasil."
    ];

    private static TicketSpec[] TicketSpecs() =>
    [
        new("Laptop tidak menyala setelah update", "Laptop Lenovo saya mati total setelah update Windows semalam. Lampu indikator berkedip oranye. Sudah coba tahan tombol power 30 detik tapi tetap tidak menyala.", "Hardware", TicketPriority.High),
        new("Keyboard beberapa tombol tidak berfungsi", "Tombol A, S, dan spasi di laptop saya sering tidak merespons. Harus ditekan keras baru muncul. Mengganggu saat mengetik laporan.", "Hardware", TicketPriority.Medium),
        new("Monitor kedua tidak terdeteksi", "Monitor eksternal di meja saya tidak terdeteksi sejak kemarin. Kabel HDMI sudah dicoba ganti tapi tetap no signal.", "Hardware", TicketPriority.Medium),
        new("Mouse wireless sering putus koneksi", "Mouse wireless saya sering disconnect beberapa detik lalu nyambung lagi. Baterai baru diganti minggu lalu.", "Hardware", TicketPriority.Low),
        new("Laptop sangat panas dan kipas berisik", "Laptop terasa sangat panas di bagian kiri dan kipas berbunyi keras terus-menerus walau hanya buka Excel.", "Hardware", TicketPriority.Medium),
        new("SSD hampir penuh, minta penambahan kapasitas", "Drive C tinggal 8 GB dari 256 GB. Saya butuh ruang untuk data proyek. Mohon opsi upgrade atau pembersihan.", "Hardware", TicketPriority.Low),
        new("Headset tidak terdeteksi di laptop", "Headset USB untuk meeting tidak terdeteksi sama sekali di Device Manager. Di laptop rekan saya headset ini normal.", "Hardware", TicketPriority.Medium),
        new("Printer lantai 2 tidak merespons", "Printer HP LaserJet di lantai 2 statusnya offline dari semua komputer. Sudah dicoba restart printer tapi masih sama.", "Printer", TicketPriority.High),
        new("Hasil cetak bergaris hitam", "Semua dokumen yang dicetak dari printer Finance muncul garis hitam vertikal di sisi kanan.", "Printer", TicketPriority.Medium),
        new("Tidak bisa print dari aplikasi SAP", "Print dari SAP selalu gagal dengan pesan spooler error, padahal print dari Word normal.", "Printer", TicketPriority.Medium),
        new("Printer minta ganti toner", "Printer di ruang Marketing menampilkan peringatan toner low sejak dua hari lalu dan hasil cetak mulai pudar.", "Printer", TicketPriority.Low),
        new("Kertas selalu macet di tray 2", "Setiap mencetak lebih dari 5 halaman dari tray 2, kertas macet di bagian belakang printer.", "Printer", TicketPriority.Medium),
        new("Scanner tidak mengirim hasil ke email", "Fitur scan-to-email di mesin fotokopi lantai 1 tidak mengirim apa pun ke email saya sejak kemarin.", "Printer", TicketPriority.Medium),
        new("WiFi kantor putus-putus di ruang rapat", "Koneksi WiFi PATRA-OFFICE di ruang rapat besar sering putus saat video call. Di area lain normal.", "Jaringan", TicketPriority.High),
        new("Tidak bisa akses shared folder departemen", "Folder \\\\fileserver\\finance tidak bisa diakses, muncul pesan access denied. Kemarin masih bisa.", "Jaringan", TicketPriority.High),
        new("VPN tidak bisa connect dari rumah", "Sejak semalam VPN selalu gagal di tahap authenticating. Internet rumah saya normal. Besok saya harus kirim laporan dari luar kantor.", "Jaringan", TicketPriority.Critical),
        new("Internet sangat lambat di lantai 3", "Sejak pagi browsing dan download di seluruh lantai 3 sangat lambat, di bawah 1 Mbps.", "Jaringan", TicketPriority.High),
        new("Kabel LAN di meja baru tidak aktif", "Saya pindah ke meja baru di area Operations, tapi port LAN di meja tidak ada koneksi sama sekali.", "Jaringan", TicketPriority.Medium),
        new("Tidak bisa akses aplikasi internal dari jaringan tamu", "Vendor kami perlu mengakses portal e-procurement dari WiFi tamu tapi selalu timeout.", "Jaringan", TicketPriority.Low),
        new("Telepon IP tidak ada nada sambung", "Telepon IP di meja saya mati total, layar menyala tapi tidak ada dial tone.", "Jaringan", TicketPriority.Medium),
        new("Excel crash saat buka file besar", "Excel selalu not responding saat membuka file rekonsiliasi 80 MB. Sudah coba di safe mode tetap crash.", "Aplikasi", TicketPriority.High),
        new("Outlook tidak bisa kirim email dengan lampiran", "Email dengan lampiran di atas 5 MB selalu tersangkut di outbox. Email tanpa lampiran normal.", "Aplikasi", TicketPriority.High),
        new("Aplikasi absensi error saat check-in", "Aplikasi absensi menampilkan error 500 setiap kali saya check-in pagi. Teman satu tim juga mengalami.", "Aplikasi", TicketPriority.Critical),
        new("Minta instalasi software AutoCAD", "Saya membutuhkan AutoCAD untuk review gambar teknis dari kontraktor. Mohon diinstalkan beserta lisensinya.", "Aplikasi", TicketPriority.Low),
        new("SAP logon error setelah ganti laptop", "Setelah laptop diganti, SAP GUI menampilkan pesan partner not reached saat login ke server produksi.", "Aplikasi", TicketPriority.High),
        new("Teams tidak bisa share screen", "Saat meeting, tombol share screen di Teams abu-abu dan tidak bisa diklik. Reinstall belum membantu.", "Aplikasi", TicketPriority.Medium),
        new("Antivirus memblokir aplikasi laporan", "Aplikasi pelaporan internal terdeteksi sebagai ancaman oleh antivirus dan otomatis terhapus.", "Aplikasi", TicketPriority.Medium),
        new("Update Windows menghapus driver audio", "Setelah update Windows, tidak ada suara sama sekali. Device Manager menunjukkan tanda seru di audio device.", "Aplikasi", TicketPriority.Medium),
        new("Lupa password akun domain", "Saya lupa password login Windows setelah kembali dari cuti dua minggu. Akun sekarang terkunci.", "Akun & Akses", TicketPriority.High),
        new("Akun email baru untuk karyawan baru", "Mohon dibuatkan akun email dan akses sistem untuk staf baru di HSSE, mulai bekerja Senin depan.", "Akun & Akses", TicketPriority.Medium),
        new("Minta akses ke folder proyek Balikpapan", "Saya ditunjuk sebagai admin dokumen proyek Balikpapan dan butuh akses read-write ke foldernya.", "Akun & Akses", TicketPriority.Medium),
        new("Akun SAP terkunci setelah salah password", "Akun SAP saya terkunci setelah tiga kali salah memasukkan password baru.", "Akun & Akses", TicketPriority.High),
        new("Nonaktifkan akses karyawan resign", "Mohon menonaktifkan seluruh akses sistem atas nama karyawan yang resign efektif hari ini. Detail saya kirim via email.", "Akun & Akses", TicketPriority.Critical),
        new("OTP tidak masuk ke HP", "Kode OTP untuk login portal HR tidak pernah masuk ke nomor saya, padahal nomor sudah benar.", "Akun & Akses", TicketPriority.Medium),
        new("Proyektor ruang rapat tidak fokus", "Gambar dari proyektor ruang rapat lantai 2 buram di sisi kiri, sudah coba diatur ring fokus tapi tidak membaik.", "Lainnya", TicketPriority.Low),
        new("Minta pemasangan komputer di ruang arsip", "Ruang arsip baru membutuhkan satu unit komputer dengan akses ke sistem dokumen. Mohon dijadwalkan pemasangan.", "Lainnya", TicketPriority.Low),
        new("UPS berbunyi terus di ruang server kecil", "UPS di ruang server lantai 1 berbunyi beep panjang sejak pagi. Khawatir baterainya bermasalah.", "Lainnya", TicketPriority.High),
        new("Kartu akses pintu tidak berfungsi", "Kartu akses saya tidak bisa membuka pintu area kantor sejak tadi pagi, harus dibukakan resepsionis.", "Lainnya", TicketPriority.Medium),
        new("TV dashboard ruang monitoring mati", "TV yang menampilkan dashboard operasional di ruang monitoring tidak menyala walau lampu standby hidup.", "Lainnya", TicketPriority.Medium),
        new("Jam dinding sistem antrian tidak sinkron", "Jam pada layar sistem antrian pelayanan berbeda 15 menit dengan waktu sebenarnya.", "Lainnya", TicketPriority.Low)
    ];

    private static async Task SeedKbArticlesAsync(
        AppDbContext db,
        IReadOnlyDictionary<string, Category> categories,
        IReadOnlyList<AppUser> technicians,
        Random random,
        DateTimeOffset now,
        CancellationToken ct)
    {
        if (await db.KbArticles.AnyAsync(ct))
        {
            return;
        }

        (string Title, string Slug, string Category, string Body)[] articles =
        [
            ("Cara reset password akun domain sendiri", "cara-reset-password-akun-domain-sendiri", "Akun & Akses",
                """
                Password akun domain kedaluwarsa setiap 90 hari. Anda bisa menggantinya sendiri tanpa tiket selama akun belum terkunci.

                ## Langkah
                1. Dari komputer kantor, tekan `Ctrl + Alt + Del` lalu pilih **Change a password**.
                2. Masukkan password lama, lalu password baru dua kali.
                3. Password baru minimal 12 karakter dengan kombinasi huruf besar, huruf kecil, dan angka.
                4. Setelah berhasil, kunci layar dan login ulang supaya kredensial tersimpan.

                ## Kalau akun sudah terkunci
                Akun terkunci otomatis setelah 5 kali salah password dan terbuka sendiri setelah 15 menit. Kalau butuh lebih cepat, buat tiket kategori **Akun & Akses** dengan prioritas Tinggi.
                """),
            ("Printer offline: pemeriksaan sebelum membuat tiket", "printer-offline-pemeriksaan-sebelum-membuat-tiket", "Printer",
                """
                Sebagian besar kasus printer offline selesai dengan pemeriksaan singkat berikut.

                ## Periksa dulu
                1. Pastikan layar printer menyala. Kalau mati, cek kabel power dan saklar di sisi kanan.
                2. Lihat apakah ada kertas macet: buka penutup depan dan tray, keluarkan kertas yang tersangkut searah jalur kertas.
                3. Di komputer, buka **Settings > Devices > Printers & scanners**, pilih printernya, dan pastikan **Use Printer Offline** tidak tercentang.
                4. Matikan printer, tunggu 10 detik, nyalakan lagi. Tunggu sampai statusnya Ready.

                Kalau setelah semua langkah printer masih offline, buat tiket kategori **Printer** dan sebutkan lokasi printer serta pesan error yang tampil.
                """),
            ("Panduan koneksi VPN dari luar kantor", "panduan-koneksi-vpn-dari-luar-kantor", "Jaringan",
                """
                VPN dipakai untuk mengakses aplikasi internal dari luar jaringan kantor.

                ## Persiapan
                - Aplikasi **FortiClient** sudah terinstal (bawaan laptop dinas).
                - Akun domain aktif dan tidak terkunci.

                ## Langkah koneksi
                1. Buka FortiClient, pilih profil **PATRA-VPN**.
                2. Login dengan akun domain (tanpa awalan domain).
                3. Masukkan kode OTP yang dikirim ke HP Anda.
                4. Tunggu status **Connected**, lalu akses aplikasi internal seperti biasa.

                ## Masalah umum
                | Gejala | Penyebab umum | Solusi |
                | --- | --- | --- |
                | Berhenti di authenticating | Password kedaluwarsa | Ganti password dulu dari jaringan kantor |
                | OTP tidak masuk | Nomor HP lama | Buat tiket Akun & Akses untuk pembaruan nomor |
                | Connected tapi aplikasi timeout | Split tunnel belum aktif | Restart FortiClient, coba lagi |
                """),
            ("WiFi kantor: cara menghubungkan perangkat", "wifi-kantor-cara-menghubungkan-perangkat", "Jaringan",
                """
                Ada dua SSID di seluruh gedung.

                - **PATRA-OFFICE**: untuk perangkat dinas, login dengan akun domain.
                - **PATRA-GUEST**: untuk tamu dan perangkat pribadi, password dirotasi tiap bulan dan tersedia di resepsionis.

                ## Perangkat dinas tidak bisa connect
                1. Lupakan jaringan (Forget network) lalu sambungkan ulang.
                2. Pastikan memilih **PATRA-OFFICE**, bukan GUEST.
                3. Login memakai akun domain tanpa awalan domain.
                4. Kalau muncul "can't connect to this network", restart laptop dan coba lagi sebelum membuat tiket.
                """),
            ("Outlook penuh: membersihkan mailbox", "outlook-penuh-membersihkan-mailbox", "Aplikasi",
                """
                Kuota mailbox adalah 10 GB. Saat hampir penuh, email baru bisa tertolak.

                ## Cara cek pemakaian
                Buka Outlook, menu **File**: kapasitas tampil di bagian Mailbox Settings.

                ## Membersihkan
                1. Kosongkan folder **Deleted Items** dan **Junk Email**.
                2. Urutkan Inbox berdasarkan ukuran (Sort by Size) dan hapus email lampiran besar yang tidak diperlukan.
                3. Simpan lampiran penting ke folder departemen di file server, bukan di mailbox.
                4. Arsipkan email lama: **File > Tools > Clean Up Old Items** ke file PST di drive D.

                Butuh penambahan kuota untuk kebutuhan khusus? Ajukan tiket kategori **Aplikasi** dengan persetujuan atasan.
                """),
            ("Laptop lambat: perawatan mandiri rutin", "laptop-lambat-perawatan-mandiri-rutin", "Hardware",
                """
                Sebelum melaporkan laptop lambat, jalankan perawatan singkat ini. Biasanya cukup memulihkan performa.

                1. **Restart penuh** minimal dua hari sekali. Shutdown pada Windows modern tidak sepenuhnya me-reset sistem; pilih Restart.
                2. Kosongkan drive C minimal 15%: hapus file di Downloads dan kosongkan Recycle Bin.
                3. Cek aplikasi startup: `Ctrl + Shift + Esc` > tab **Startup apps**, nonaktifkan yang tidak perlu.
                4. Pastikan Windows Update selesai; update yang menggantung membuat sistem berat.
                5. Tutup tab browser yang tidak dipakai; setiap tab memakan memori.

                Kalau setelah itu masih lambat (misal booting lebih dari 3 menit), buat tiket kategori **Hardware** dan sebutkan sejak kapan terjadi.
                """),
            ("Meminta akses shared folder departemen", "meminta-akses-shared-folder-departemen", "Akun & Akses",
                """
                Akses folder di file server diatur per grup departemen dan butuh persetujuan pemilik folder.

                ## Prosedur
                1. Buat tiket kategori **Akun & Akses** dengan menyebutkan path folder lengkap, contoh `\\fileserver\finance\anggaran-2026`.
                2. Sebutkan jenis akses: **read-only** atau **read-write**.
                3. Sertakan nama atasan atau pemilik folder yang menyetujui.
                4. Tim IT memproses setelah persetujuan diterima, umumnya dalam 1 hari kerja.

                Akses bersifat personal. Jangan meminjam akun rekan kerja; audit akses dilakukan berkala.
                """),
            ("Mengenali email phishing dan cara melaporkannya", "mengenali-email-phishing-dan-cara-melaporkannya", "Lainnya",
                """
                Serangan phishing sering menyamar sebagai email internal. Kenali cirinya sebelum mengklik apa pun.

                ## Ciri umum
                - Alamat pengirim mirip tapi tidak persis, contoh `it-support@t1ketin.co` (angka 1 menggantikan huruf i).
                - Nada mendesak: "akun Anda akan diblokir dalam 24 jam".
                - Meminta password, OTP, atau data pribadi. **IT tidak pernah meminta password lewat email.**
                - Lampiran tidak terduga berekstensi `.zip`, `.html`, atau `.exe`.

                ## Kalau menerima email mencurigakan
                1. Jangan klik tautan atau buka lampiran.
                2. Laporkan lewat tombol **Report Phishing** di Outlook, atau forward ke helpdesk.
                3. Kalau terlanjur memasukkan password, segera ganti password dan buat tiket prioritas **Kritis**.
                """)
        ];

        var kbArticles = articles.Select(a => new KbArticle
        {
            Id = Guid.NewGuid(),
            Title = a.Title,
            Slug = a.Slug,
            BodyMarkdown = a.Body,
            CategoryId = categories[a.Category].Id,
            AuthorId = technicians[random.Next(technicians.Count)].Id,
            IsPublished = true,
            ViewCount = random.Next(7, 180),
            CreatedAt = now.AddDays(-random.Next(20, 90))
        });

        db.KbArticles.AddRange(kbArticles);
        await db.SaveChangesAsync(ct);
    }
}
