# 🎓 CampusConnect — Blazor WebApp

A full university portal converted from C++ to C# / Blazor Server.  
**Navy blue & white** professional aesthetic.

---

## 📁 Project Structure

```
CampusConnect/
│
├── Models/
│   └── Models.cs               ← C# classes (converted from C++ structs)
│       • Location     ← struct place
│       • Student      ← struct pupil
│       • Booking      ← struct book
│       • CampusEvent  ← struct event
│       • LogEntry     ← struct logentry
│       • UserAction   ← struct act
│       • PartnerMatch ← struct match
│       • SessionUser  ← (new for auth state)
│
├── Services/
│   └── AppStateService.cs      ← All data + logic (replaces C++ globals)
│       • Students List         ← replaces shash (string hash map)
│       • Bookings List         ← replaces pqueue (priority queue)
│       • Events List           ← replaces evtree (BST)
│       • Locations List        ← replaces graph nodes
│       • FindPartners()        ← mirrors C++ findpart() scoring
│       • AddBooking()          ← mirrors bookroom()
│       • AddEvent()            ← mirrors viewevents() → add
│       • Login/Register()      ← mirrors login() / reg()
│
├── Components/Layout/
│   └── MainLayout.razor        ← Sidebar + topbar + login modal + toasts
│
├── Pages/
│   ├── Index.razor             ← Dashboard (stats, events preview, locations)
│   ├── Students.razor          ← Student directory with search & add
│   ├── Bookings.razor          ← Room booking form + table + admin approve
│   ├── Events.razor            ← Events list + admin add/remove
│   ├── Partners.razor          ← Partner finder with scoring breakdown
│   └── ActivityLogs.razor      ← Admin-only log viewer
│
├── wwwroot/css/
│   └── app.css                 ← Full navy blue & white design system
│
├── App.razor
├── _Imports.razor
├── Program.cs
└── CampusConnect.csproj
```

---

## 🚀 How to Run

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8) installed

### Steps

```bash
# 1. Navigate to the project folder
cd CampusConnect

# 2. Restore packages (auto-downloads Blazor libs)
dotnet restore

# 3. Run the app
dotnet run

# 4. Open in browser
# https://localhost:5001  (or http://localhost:5000)
```

---

## 🔑 Demo Credentials

| Role      | Email (any seeded)     | Password    |
|-----------|------------------------|-------------|
| Student   | alice@uni.edu          | any text    |
| Student   | bob@uni.edu            | any text    |
| Admin     | —                      | `admin123`  |

> **Note:** The demo uses in-memory data. Refreshing the page resets data.  
> To add persistence, inject a database (SQLite with EF Core is simplest).

---

## 🔄 C++ → C# Conversion Map

| C++ Concept              | C# / Blazor Equivalent                    |
|--------------------------|-------------------------------------------|
| `struct place`           | `class Location`                          |
| `struct pupil`           | `class Student`                           |
| `struct book`            | `class Booking`                           |
| `struct event`           | `class CampusEvent`                       |
| `struct logentry`        | `class LogEntry`                          |
| `shash` (hash table)     | `List<Student>` + LINQ `.FirstOrDefault()`|
| `pqueue` (priority queue)| `List<Booking>` sorted by `.Priority`     |
| `evtree` (BST)           | `List<CampusEvent>` sorted by `.Date`     |
| `graph` (adjacency list) | `List<Location>` (simplified)             |
| Global variables         | `AppStateService` (singleton DI service)  |
| `findpart()` scoring     | `FindPartners()` in `AppStateService`     |
| `cout` / `cin`           | Razor `.razor` pages with `@bind`         |
| `clearscr()` / menus     | Blazor `NavLink` routing + layout         |
| `wait()` (pause)         | `async/await` + UI feedback               |
| `saveall()` / files      | (Extend with EF Core for persistence)     |
| `undostack`              | `Stack<UserAction>` in service            |

---

## 🎨 UI Design

- **Font:** Sora (headings) + DM Sans (body)
- **Colors:** Deep navy `#0f2d6e`, bright blue `#2563eb`, white `#ffffff`
- **Components:** Cards, badges, tables, modals, toasts, form groups
- All defined as CSS custom properties in `wwwroot/css/app.css`

---

## ➕ Adding a Database (Optional)

```bash
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Design
```

Then create a `CampusDbContext`, replace `List<T>` in `AppStateService` with `DbSet<T>`,  
and call `dotnet ef migrations add Init && dotnet ef database update`.
