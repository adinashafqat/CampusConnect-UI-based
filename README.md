# CampusConnect

A university campus management portal built with ASP.NET Core Blazor Server (.NET 8). CampusConnect gives students a single interface to browse campus information, book rooms, discover events, and find study partners. Administrators get a separate elevated view with full control over bookings, events, student records, and activity logs.

---

## Table of Contents

- [Overview](#overview)
- [Technology Stack](#technology-stack)
- [Project Structure](#project-structure)
- [Architecture](#architecture)
- [Data Models](#data-models)
- [Application State](#application-state)
- [Pages and UI](#pages-and-ui)
- [Authentication and Roles](#authentication-and-roles)
- [Partner Matching Algorithm](#partner-matching-algorithm)
- [Getting Started](#getting-started)
- [Demo Credentials](#demo-credentials)

---

## Overview

CampusConnect is a single-solution Blazor Server application with no external database. All state lives in a singleton `AppStateService` that is seeded with sample data at startup. The UI is a dark-sidebar shell with a top bar, modal dialogs, toast notifications, and a responsive card-based layout.

The application supports two distinct roles: student and administrator. Students can view the dashboard, browse other students, book rooms, read events, and find study partners. Administrators gain additional capabilities: approving bookings, publishing and removing events, adding students, and reading the activity log.

---

## Technology Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 8, Blazor Server |
| Language | C# 12, Razor |
| UI rendering | Server-Side Blazor over SignalR |
| Styling | Custom CSS (wwwroot/css/app.css) |
| State management | Singleton service with event-based change notification |
| Persistence | In-memory only (no database) |
| Target runtime | .NET 8 |

---

## Project Structure

```
campus/
  App.razor                     Root router component
  _Imports.razor                Global Razor using directives
  Program.cs                    Application entry point and DI registration
  CampusConnect.csproj          Project file targeting net8.0

  Components/
    Layout/
      MainLayout.razor          App shell: sidebar, topbar, login modal, toast system

  Models/
    Models.cs                   All domain model classes

  Pages/
    Index.razor                 Dashboard (/)
    Students.razor              Student directory (/students)
    Bookings.razor              Room booking (/bookings)
    Events.razor                Events board (/events)
    Partners.razor              Study partner finder (/partners)
    ActivityLogs.razor          Admin-only audit log (/admin/logs)
    _Host.cshtml                Blazor Server host page

  Services/
    AppStateService.cs          Singleton holding all application state and business logic

  wwwroot/
    css/
      app.css                   All custom styles

  Properties/
    launchSettings.json         Development launch profiles
```

---

## Architecture

CampusConnect follows the pattern native to Blazor Server: the browser maintains a persistent SignalR connection to the server, and all rendering and logic execute server-side. There is no REST API, no client-side JavaScript framework, and no database layer.

```
Browser (HTML + CSS)
      |
      | SignalR (WebSocket)
      |
ASP.NET Core Blazor Server
      |
      +-- Router (App.razor)
      |       Routes URL to the matching @page component
      |
      +-- MainLayout.razor
      |       Renders the persistent sidebar, top bar, login modal, and toast list
      |       Subscribes to AppStateService.OnChange to re-render on state mutations
      |
      +-- Page Components (Index, Students, Bookings, Events, Partners, ActivityLogs)
      |       Each injects AppStateService and subscribes to OnChange
      |       Render their own topbar and page body
      |       Delegate all mutations to AppStateService methods
      |
      +-- AppStateService (Singleton)
              Holds all Lists, the current SessionUser, and the undo stack
              Exposes methods for login, register, CRUD operations, and partner matching
              Fires OnChange event after every mutation so all subscribed components re-render
```

Because `AppStateService` is registered as a singleton, its data is shared across all connected browser sessions for the lifetime of the server process. This is intentional for a demo application; in production the service would be replaced by a database-backed scoped service.

---

## Data Models

All models are defined in `Models/Models.cs`.

**Location** represents a physical campus space with an id, name, type string, and seating capacity. Six locations are seeded: Main Gate, Library, Computer Lab, Cafeteria, CS Department, and Auditorium.

**Student** holds the profile of a registered student. Fields include a numeric student ID, name, email, department, year of study, a free-time slot string, a list of subjects, and a list of interests. The computed `Initial` property returns the first letter of the name for use in avatar elements.

**Booking** records a room reservation. It stores a string booking ID in the format `BK-NNNN`, references to student ID and name, a location ID, date, time slot, duration in hours, a priority integer from 1 to 5, and a status string that begins as `Pending` and may be set to `Approved` by an admin.

**CampusEvent** describes a campus event with a numeric event ID, title, description, date, time, location name string, and organizer name.

**LogEntry** captures an audit event with a timestamp, the email of the acting user (or `guest`), an action keyword, and a detail string.

**UserAction** is a lightweight struct stored on the undo stack. It holds a type string and a human-readable description.

**PartnerMatch** is a view model returned by the matching algorithm. It pairs a Student with an integer score.

**SessionUser** represents the currently logged-in user within the server session. It holds a name, email, an `IsAdmin` flag, and a computed `Initial`.

---

## Application State

`AppStateService` is the single source of truth. It is constructed once per application lifetime and exposes the following state through public properties.

```
Students      List<Student>
Bookings      List<Booking>      sorted by descending priority after each insert
Events        List<CampusEvent>  sorted by date after each insert
Locations     List<Location>
Logs          List<LogEntry>
UndoStack     Stack<UserAction>
CurrentUser   SessionUser?
```

Computed properties derived from the above: `IsAdmin`, `IsLoggedIn`, `TotalStudents`, `TotalBookings`, `TotalEvents`, `TotalLocations`.

Every public mutation method ends by calling the private `Notify()` helper, which raises the `OnChange` event. Every page component subscribes to this event in `OnInitialized` and unsubscribes in `Dispose`, triggering `StateHasChanged` on each notification.

Key methods:

- `Login(email, password)` looks up the email in the Students list. Any password is accepted in the demo. Sets `CurrentUser`, writes a log entry, and notifies.
- `AdminLogin(password)` checks for the hardcoded value `admin123`. Sets `CurrentUser` with `IsAdmin = true`.
- `Register(...)` validates uniqueness of ID and email, creates a Student, sets `CurrentUser`, and logs the action.
- `Logout()` clears `CurrentUser` and logs out.
- `AddBooking(...)` creates a `Booking`, appends it, re-sorts the list by priority descending, pushes an entry onto the undo stack, and logs.
- `ApproveBooking(bookingId)` mutates the Status field to `Approved`.
- `RemoveBooking(bookingId)` removes the booking from the list.
- `AddEvent(...)` creates a `CampusEvent`, appends it, re-sorts by date, and logs.
- `RemoveEvent(eventId)` removes the event from the list.
- `AddStudent(...)` validates uniqueness then appends a new Student.
- `GetBookingsForCurrentUser()` returns all bookings if admin, or only those matching the current user's name if student.
- `FindPartners()` runs the scoring algorithm described below.

---

## Pages and UI

### MainLayout

The persistent shell rendered for every route. It contains:

- A fixed left sidebar with the application logo, navigation links, and a user pill at the bottom. The admin navigation section (Activity Logs) is conditionally rendered based on `State.IsAdmin`.
- A login modal with three tabs: Sign In, Register, and Admin. The modal is toggled by clicking the Sign In button in the sidebar. Clicking the backdrop closes it.
- A toast container positioned at the bottom right. Toasts appear for 3.2 seconds then remove themselves. Each toast carries a message, a sub-message, and a left-border color.

### Dashboard (/)

Displays four stat cards: total students, total bookings, total events, and total locations. Below the stats, two side-by-side cards show the three nearest upcoming events and the four most recent bookings. A full-width card at the bottom renders all campus locations as a tiled grid showing name, capacity, and type badge.

### Students (/students)

A searchable, filterable table of all registered students. The search field filters in real time across name, email, and department using case-insensitive string contains. Each row shows an avatar initial circle, student ID, name, email, a department badge, year, free-time slot, and subject tags. Admins see an Add Student button in the topbar that opens a modal form for registering a new student with ID, name, email, department, year, and free-time slot.

### Room Bookings (/bookings)

Students who are not logged in see an informational alert. Logged-in students see a booking form at the top. The form collects location (dropdown of locations with capacity greater than zero), date (date picker), time slot (fixed options), duration in hours, and priority level 1 through 5. On submission, `AppStateService.AddBooking` is called. The booking list below the form shows the current user's bookings only. Admins see all bookings and an Approve button on each pending row. Bookings are always displayed in descending priority order because the service re-sorts the list on every insert.

### Events (/events)

A chronological list of campus events rendered as event cards, each with a date box showing abbreviated month and day, the event title, time, location, organizer, and description. Admins see a Remove button on each card and an Add Event button in the topbar that expands an inline form for publishing a new event. The form collects title, organizer, date, time, a location dropdown, and a description textarea.

### Find Partners (/partners)

Requires login. Logged-in students see their own profile card at the top (department, year, free time, subjects), followed by a scoring legend card that explains the point breakdown, followed by the ranked list of matches returned by `AppStateService.FindPartners`. Each match card shows the candidate's rank, avatar initial, name, department badge, year, free time, subjects, a horizontal score bar, and a numeric score with a label (Excellent, Good, or Fair) based on thresholds.

### Activity Logs (/admin/logs)

Accessible only to admins. Non-admins are redirected to `/` on initialization. Displays a table of all `LogEntry` records in reverse chronological order, showing timestamp, user email, action badge, and detail string. The action badge color is mapped per action type: login and register are green, logout and booking are blue, admin-login is purple, and add-event is amber.

---

## Authentication and Roles

Authentication is entirely in-memory and intended for demonstration purposes only. There are no passwords stored or verified for student login; any string is accepted as the password. The only validation is that the supplied email must match a student record.

Admin access is granted by entering the hardcoded password `admin123` on the Admin tab of the login modal.

The `SessionUser` object on `AppStateService` drives all role checks. Components read `State.IsLoggedIn` and `State.IsAdmin` to conditionally render UI elements and guard actions. The ActivityLogs page performs a programmatic redirect in `OnInitialized` if the current user is not an admin.

---

## Partner Matching Algorithm

`AppStateService.FindPartners` is called from the Partners page. It identifies the student record for the currently logged-in user, then iterates over all other students and computes a compatibility score using the following additive rules.

| Criterion | Points |
|---|---|
| Same department | 30 |
| Same year of study | 20 |
| Matching free-time slot | 25 |
| Each shared subject | 10 |
| Each shared interest | 5 |

Students who score zero are excluded. The remaining candidates are sorted descending by score and the top five are returned. The result is a `List<PartnerMatch>` which the Partners page renders as ranked cards with a visual score bar capped at 100 percent width.

---

## Getting Started

**Prerequisites**

- .NET 8 SDK

**Run the application**

```bash
git clone <repository-url>
cd campus
dotnet run
```

The application starts on `https://localhost:7001` (or the port configured in `Properties/launchSettings.json`). Open the URL in a browser. No database setup or environment variables are required.

**Build for release**

```bash
dotnet publish -c Release -o ./publish
```

---

## Demo Credentials

**Student login**

Use any of the seeded student email addresses with any password string.

| Name | Email | Department | Year |
|---|---|---|---|
| Ahmed Khan | ahmed@air.edu | CS | 3 |
| Ali Ahmed | ali@air.edu | CS | 2 |
| Amna Tariq | amna@air.edu | EE | 3 |
| Sara Khan | sara@air.edu | Math | 1 |

**Admin login**

Open the Admin tab in the login modal and enter the password `admin123`.

---

## Notes

- All data is reset when the server process restarts because there is no persistent storage.
- The undo stack (`UndoStack`) is populated on booking creation but no undo UI is currently wired to consume it; the infrastructure is in place for future implementation.
- Date fields are stored as strings in `yyyy-MM-dd` format and parsed to `DateTime` only for display purposes.
- The `AppStateService` singleton shares state across all browser sessions connected to the same server instance. This is acceptable for a demonstration but would require a session-scoped or database-backed approach in a multi-user production deployment.
