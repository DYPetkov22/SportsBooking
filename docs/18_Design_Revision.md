# Design revision — 11 November 2026

## Direction

A quieter sports-club identity replaces the previous forest/lime marketing layout. Warm paper, charcoal text and a clay-red accent come from the existing tennis photograph. Georgia gives the homepage a distinct heading; the application keeps clear system sans-serif text. The photograph stays unobscured next to the search rather than behind oversized promotional text.

- Search is the first useful homepage action, with an ordinary label and a link to the complete catalogue.
- Catalogue counts are a compact directory with thin dividers, rather than decorative cards.
- Instructions describe the actual reservation workflow, including the occupied-slot validation.
- Signed-in users still see recent bookings, today's active count and the new-reservation action. Staff still have their reports shortcut.
- Navigation, forms, filters, tables, reports, pagination, uploads and sign-in demos use smaller corners, restrained color and consistent spacing.
- Mobile layouts stack the homepage, expose the existing menu toggle and retain scrollable tables. Dark theme and keyboard focus remain supported.
- The theme button's accessible name now updates when switching modes.

## Changed application files

| File | Change |
|---|---|
| `SportsBooking.Web/Views/Home/Index.cshtml` | Split photo/search composition, compact directory, retained signed-in activity and staff reports, clearer booking instructions. |
| `SportsBooking.Web/Views/Shared/_Layout.cshtml` | Theme-color metadata and simpler footer wording. Same routes, role checks, POST logout and script section. |
| `SportsBooking.Web/wwwroot/css/design.css` | Replaced visual theme, shared component overrides and responsive/dark styles. |
| `SportsBooking.Web/wwwroot/img/brand-mark.svg` | Original court-line mark matching the clay accent. |
| `SportsBooking.Web/wwwroot/js/theme.js` | Accessible label follows the current theme. Existing stored preference behavior retained. |

Documentation: updated README, design brief and portable `Design_Preview.html`; marked `17_Design_Refresh.md` as the earlier design; this note and current verification outputs added.

## Structure and diagrams

No C# models, controllers, services, contexts, SQL scripts, view models, routes, permissions or tests changed. The SVG ER diagram (`ER_Diagram.svg`) and Mermaid schema (`04_Background_Schema.md`) still describe the same six business tables and relationships. Existing user flows (`02_App_Flow.md`) remain valid: homepage search submits GET to the existing Fields/Index Search filter; registration, booking, payments and administration use the same destinations. A changed homepage composition does not change the database or application architecture, so no diagram changes are required.

The old external Figma prototype has not been edited and should not be treated as the current visual specification.

## Verification

- .NET 9 Debug solution build: passed, 0 errors. Existing ImageSharp license warning remains.
- Existing xUnit suite: 38 passed, 0 failed, 0 skipped.
- Compiled Razor pages rendered through an isolated TestServer with sample view data: guest home, Fields, Login, Register, staff Reports and admin home all returned HTTP 200. Search action and staff navigation links verified. This temporary harness is not a production dependency.
- Application source comparison against the input ZIP confirms the business code, SQL scripts, tests and ER diagrams are unchanged.
- Desktop/mobile CSS and offline asset references were checked. Interactive visual browser verification could not be completed: this environment could not open the local preview in its browser. No real SQL Server end-to-end run was performed.
- Current build, test, TRX and Razor rendering results are in `docs/verification/design-revision-*`.

## Preview and running

Open `docs/Design_Preview.html` to review the new guest homepage without starting SQL Server. It embeds its styles, photograph and mark, uses sample counts and supports the theme/menu controls. App navigation and form submissions are intentionally inactive in that portable preview; run the MVC application to use those features.

Run and database setup are unchanged; see README and `SQL_SERVER_SETUP_BG.md`. No schema migration is required. Assignments 7, 8 and 9 are preserved.

The bundled photograph remains the original Unsplash image credited in `17_Design_Refresh.md`. No external image or font requests were added.
