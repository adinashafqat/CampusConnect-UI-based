using CampusConnect.Models;

namespace CampusConnect.Services
{
    public class AppStateService
    {
       
        public List<Student> Students { get; private set; } = new();
        public List<Booking> Bookings { get; private set; } = new();
        public List<CampusEvent> Events { get; private set; } = new();
        public List<Location> Locations { get; private set; } = new();
        public List<LogEntry> Logs { get; private set; } = new();
        public Stack<UserAction> UndoStack { get; private set; } = new();

        public SessionUser? CurrentUser { get; private set; }
        public bool IsAdmin => CurrentUser?.IsAdmin ?? false;
        public bool IsLoggedIn => CurrentUser != null;

        private int _nextBookingId = 1001;
        private int _nextEventId = 1;

        public event Action? OnChange;
        private void Notify() => OnChange?.Invoke();

        public AppStateService()
        {
            SeedLocations();
            SeedStudents();
            SeedEvents();
            SeedBookings();
        }

        private void SeedLocations()
        {
            Locations.AddRange(new[]
            {
                new Location { Id = 1, Name = "Main Gate",    Type = "entrance",  Capacity = 0   },
                new Location { Id = 2, Name = "Library",      Type = "library",   Capacity = 200 },
                new Location { Id = 3, Name = "Computer Lab", Type = "lab",       Capacity = 50  },
                new Location { Id = 4, Name = "Cafeteria",    Type = "cafeteria", Capacity = 150 },
                new Location { Id = 5, Name = "CS Department",Type = "dept",      Capacity = 100 },
                new Location { Id = 6, Name = "Auditorium",   Type = "hall",      Capacity = 500 },
            });
        }

        private void SeedStudents()
        {
            Students.AddRange(new[]
            {
                new Student { Id=1001, Name="Ahmed Khan",  Email="ahmed@air.edu",  Department="CS",      Year=3, FreeTime="Mon 2-4pm",  Subjects=new(){"Data Structures","Algorithms","DB"},         Interests=new(){"coding","study","group"} },
                new Student { Id=1002, Name="Ali Ahmed",      Email="ali@air.edu",    Department="CS",      Year=2, FreeTime="Tue 3-5pm",  Subjects=new(){"Data Structures","Algorithms","OOP"},        Interests=new(){"coding","gaming","study"} },
                new Student { Id=1003, Name="Amna Tariq",    Email="amna@air.edu",  Department="EE",      Year=3, FreeTime="Mon 2-4pm",  Subjects=new(){"Circuits","Signals","DB"},                  Interests=new(){"study","music"} },
                new Student { Id=1004, Name="Sara Khan",      Email="sara@air.edu",  Department="Math",    Year=1, FreeTime="Wed 1-3pm",  Subjects=new(){"Calculus","Linear Algebra","Stats"},        Interests=new(){"group","sports"} }
            });
        }

        private void SeedEvents()
        {
            Events.AddRange(new[]
            {
                new CampusEvent { EventId=1, Title="Tech Career Fair",      Description="Meet top tech recruiters",     Date="2025-05-10", Time="10:00 AM", LocationName="Auditorium",   Organizer="Career Office" },
                new CampusEvent { EventId=2, Title="Hackathon 2026",        Description="48h coding challenge",         Date="2025-05-18", Time="9:00 AM",  LocationName="Computer Lab", Organizer="CS Society"    },
                new CampusEvent { EventId=3, Title="Study Skills Workshop", Description="Boost your academic skills",   Date="2025-04-30", Time="2:00 PM",  LocationName="Library",      Organizer="Academic Dept" },
            });
            _nextEventId = Events.Max(e => e.EventId) + 1;
        }

        private void SeedBookings()
        {
            Bookings.AddRange(new[]
            {
                new Booking { BookingId="BK-1001", StudentId=1001, StudentName="Ali Ahmed", LocationId=3, Date="2025-04-25", Time="2:00 PM", Hours=2, Priority=4, Status="Pending" },
                new Booking { BookingId="BK-1002", StudentId=1002, StudentName="Amna Tariq",     LocationId=2, Date="2025-04-26", Time="10:00 AM",Hours=1, Priority=2, Status="Pending" },
            });
        }
        public bool Login(string email, string password)
        {
            var student = Students.FirstOrDefault(s => s.Email == email);
            if (student != null)
            {
                CurrentUser = new SessionUser { Name = student.Name, Email = student.Email, IsAdmin = false };
                AddLog("login", email);
                Notify();
                return true;
            }
            return false;
        }

        public bool AdminLogin(string password)
        {
            if (password == "admin123")
            {
                CurrentUser = new SessionUser { Name = "Administrator", Email = "admin@uni.edu", IsAdmin = true };
                AddLog("admin-login", "admin");
                Notify();
                return true;
            }
            return false;
        }

        public bool Register(int id, string name, string email, string dept, int year, string freeTime)
        {
            if (Students.Any(s => s.Email == email || s.Id == id)) return false;

            var student = new Student
            {
                Id = id, Name = name, Email = email,
                Department = dept, Year = year, FreeTime = freeTime,
                Subjects = new() { "Data Structures", "Algorithms", "DB" },
                Interests = new() { "coding", "study", "group" }
            };
            Students.Add(student);
            CurrentUser = new SessionUser { Name = name, Email = email, IsAdmin = false };
            AddLog("register", email);
            Notify();
            return true;
        }

        public void Logout()
        {
            AddLog("logout", CurrentUser?.Email ?? "");
            CurrentUser = null;
            Notify();
        }

        public Booking AddBooking(int locationId, string date, string time, int hours, int priority)
        {
            if (CurrentUser == null) throw new InvalidOperationException("Not logged in");

            var student = Students.FirstOrDefault(s => s.Email == CurrentUser.Email);

            var booking = new Booking
            {
                BookingId = "BK-" + _nextBookingId++,
                StudentId = student?.Id ?? 0,
                StudentName = CurrentUser.Name,
                LocationId = locationId,
                Date = date,
                Time = time,
                Hours = hours,
                Priority = priority,
                Status = "Pending"
            };

            Bookings.Add(booking);
            Bookings = Bookings.OrderByDescending(b => b.Priority).ToList();

            UndoStack.Push(new UserAction { Type = "book", Description = $"Room at location {locationId}" });
            AddLog("booking", booking.BookingId);
            Notify();
            return booking;
        }

        public void ApproveBooking(string bookingId)
        {
            var b = Bookings.FirstOrDefault(x => x.BookingId == bookingId);
            if (b != null) { b.Status = "Approved"; Notify(); }
        }

        public void RemoveBooking(string bookingId)
        {
            Bookings.RemoveAll(b => b.BookingId == bookingId);
            Notify();
        }

        public List<Booking> GetBookingsForCurrentUser()
        {
            if (IsAdmin) return Bookings;
            return Bookings.Where(b => b.StudentName == CurrentUser?.Name).ToList();
        }

        public void AddEvent(string title, string desc, string date, string time, string locationName, string organizer)
        {
            var ev = new CampusEvent
            {
                EventId = _nextEventId++,
                Title = title, Description = desc,
                Date = date, Time = time,
                LocationName = locationName,
                Organizer = string.IsNullOrWhiteSpace(organizer) ? "Admin" : organizer
            };
            Events.Add(ev);
            Events = Events.OrderBy(e => e.Date).ToList();
            AddLog("add-event", title);
            Notify();
        }

        public void RemoveEvent(int eventId)
        {
            Events.RemoveAll(e => e.EventId == eventId);
            Notify();
        }

        public bool AddStudent(int id, string name, string email, string dept, int year, string freeTime)
        {
            if (Students.Any(s => s.Email == email || s.Id == id)) return false;
            Students.Add(new Student
            {
                Id = id, Name = name, Email = email,
                Department = dept, Year = year, FreeTime = freeTime,
                Subjects = new() { "Data Structures", "Algorithms", "DB" },
                Interests = new() { "coding", "study", "group" }
            });
            Notify();
            return true;
        }

        public List<PartnerMatch> FindPartners()
        {
            if (CurrentUser == null) return new();

            var me = Students.FirstOrDefault(s => s.Email == CurrentUser.Email);
            if (me == null) return new();

            var matches = new List<PartnerMatch>();

            foreach (var s in Students)
            {
                if (s.Id == me.Id) continue;

                int score = 0;
                if (s.Department == me.Department) score += 30;
                if (s.Year == me.Year)             score += 20;
                if (s.FreeTime == me.FreeTime)     score += 25;

                foreach (var sub in me.Subjects)
                    if (s.Subjects.Contains(sub)) score += 10;

                foreach (var like in me.Interests)
                    if (s.Interests.Contains(like)) score += 5;

                if (score > 0)
                    matches.Add(new PartnerMatch { Student = s, Score = score });
            }

            return matches.OrderByDescending(m => m.Score).Take(5).ToList();
        }

        private void AddLog(string action, string detail)
        {
            Logs.Add(new LogEntry
            {
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                UserId = CurrentUser?.Email ?? "guest",
                Action = action,
                Detail = detail
            });
        }

        public int TotalStudents  => Students.Count;
        public int TotalBookings  => Bookings.Count;
        public int TotalEvents    => Events.Count;
        public int TotalLocations => Locations.Count;

        public Location? GetLocation(int id) => Locations.FirstOrDefault(l => l.Id == id);
    }
}
