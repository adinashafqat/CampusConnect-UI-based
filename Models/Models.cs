namespace CampusConnect.Models
{
    public class Location
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public int Capacity { get; set; }
    }

    public class Student
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Department { get; set; } = "";
        public int Year { get; set; } = 1;
        public string FreeTime { get; set; } = "";
        public List<string> Subjects { get; set; } = new();
        public List<string> Interests { get; set; } = new();

        public string Initial => Name.Length > 0 ? Name[0].ToString().ToUpper() : "?";
    }

    public class Booking
    {
        public string BookingId { get; set; } = "";
        public int StudentId { get; set; }
        public string StudentName { get; set; } = "";
        public int LocationId { get; set; }
        public string Date { get; set; } = "";
        public string Time { get; set; } = "";
        public int Hours { get; set; } = 1;
        public int Priority { get; set; } = 3;  
        public string Status { get; set; } = "Pending";
    }

    public class CampusEvent
    {
        public int EventId { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Date { get; set; } = "";
        public string Time { get; set; } = "";
        public string LocationName { get; set; } = "";
        public string Organizer { get; set; } = "";
    }

    public class LogEntry
    {
        public string Timestamp { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        public string UserId { get; set; } = "";
        public string Action { get; set; } = "";
        public string Detail { get; set; } = "";
    }
    public class UserAction
    {
        public string Type { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public class PartnerMatch
    {
        public Student Student { get; set; } = new();
        public int Score { get; set; }
    }

    public class SessionUser
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public bool IsAdmin { get; set; }
        public string Initial => Name.Length > 0 ? Name[0].ToString().ToUpper() : "?";
    }
}
