# SportsBooking design refresh — previous visual version

Historical record of the first refresh. The current design and verification are described in `18_Design_Revision.md`; `Design_Preview.html` now shows that newer version.

The visual direction takes inspiration from Pitchbooking's sports photography, large headings, open spacing and rounded navigation. SportsBooking uses an original forest-green/lime palette, its existing ball mark, different composition and original Bulgarian copy. No Pitchbooking logos, photographs, page text or source code were copied.

## Changes

- `Views/Shared/_Layout.cshtml`: responsive top navigation replaces the sidebar, with the same role-dependent destinations. The account menu retains profile, payments, users, roles and POST logout. Theme switching, antiforgery fields and view script sections remain supported. A keyboard skip link is included.
- `Views/Home/Index.cshtml`: photo hero, GET search wired to the existing Fields Search filter, actual database counts, retained signed-in recent bookings and staff reports link, and introductory sections.
- `wwwroot/css/design.css`: new component styling layered over existing CSS; light/dark themes, keyboard focus, reduced motion and mobile breakpoints. Forms, filters, tables, uploads, pagination, reports and demo-account cards inherit the theme.
- `wwwroot/js/site.js`: active navigation selector and mobile menu with ARIA state, Escape/outside-click dismissal. Existing booking validation and demo-account fill behavior remain.
- `wwwroot/img/brand-mark.svg`: the existing original mark is recolored.
- `wwwroot/img/courts-hero.jpg`: bundled sports photograph, so the application does not need an external image request. Source: https://images.unsplash.com/photo-1554068865-24cecd4e34b8 (Unsplash).

## Preserved behavior

No C# business code, services, database contexts, models, SQL scripts, permissions, tests or existing forms were changed in this refresh. Assignments 7, 8 and 9 remain included. The data schema and ER diagrams are unchanged. All interface text remains Bulgarian. No new payment or availability capabilities are implied by the redesign.

## Validation

- .NET 9 Debug build: successful, 0 errors. The existing ImageSharp license warning remains; Release publishing was not verified.
- Existing xUnit suite: 38 passed, 0 failed, 0 skipped.
- An isolated temporary TestServer rendered the actual compiled Razor views for guest home, Fields, Login, Register, staff Reports and admin home: all returned HTTP 200. Generated search action and admin payments/roles links were checked. This host used sample view data and is not included in the application.
- Interactive browser screenshot/viewport checks could not run in this environment. Mobile and dark-mode rules were implemented, but their visual appearance and interactions should be checked in a normal browser. No full real-SQL Server end-to-end test was performed here.
- Build/test outputs and TRX report: `docs/verification/redesign-*`.

## Offline preview

Open `docs/Design_Preview.html` in your browser to view the actual rendered guest homepage with embedded styles, photograph and logo. Counts are sample data. Theme switching and mobile-menu toggling work; navigation, searching and booking are disabled in this offline preview. The running MVC application uses the real routes and database.

## Run

Use the existing `README.md` and `docs/SQL_SERVER_SETUP_BG.md`. No schema upgrade is needed for this design change. Restore, build and run the existing solution as before.

The earlier Figma link in `03_Design_Brief.md` describes the previous prototype; this refresh has not modified that external Figma file.
