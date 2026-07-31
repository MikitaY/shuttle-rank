# shuttle-rank

Rating system for the Minsk Badminton League. Scrapes tournament results from
[tournamentsoftware.com](https://www.tournamentsoftware.com), computes Elo,
points and inferred skill levels, and publishes them to a Vue web app.

**Live:** deployed to GitHub Pages on every push to `develop`.

## How it works

```
tournamentsoftware ──scrape──▶ data/league.json ──rate──▶ web/public/data/ratings.json ──▶ Vue app
```

1. Discover the organizer's tournaments (plus one-off events named in `config.json`).
2. Parse every match — across **all days** of multi-day tournaments — resolve
   playoff placeholders from group standings, and normalize player names via the
   alias map.
3. Compute ratings and write `ratings.json`, which the frontend renders.

## Layout

| Path | What |
|------|------|
| `src/SR.Core` | Domain models + `RatingEngine` (Elo, points, level inference). Pure, no I/O. |
| `src/SR.Scraper` | Console app: scrape → `league.json` → `ratings.json`. |
| `tests/SR.Core.Tests` | xUnit tests for the rating engine. |
| `web/` | Vue 3 + Vite frontend that reads `ratings.json`. |
| `config.json` | Organizer, tournaments, aliases, Elo/points/level parameters. |

## Running

Regenerate ratings (.NET 10 SDK):

```bash
dotnet run --project src/SR.Scraper                # uses the on-disk HTML cache
dotnet run --project src/SR.Scraper -- --refresh   # re-fetch every page
dotnet test                                        # run the engine tests
```

Frontend (Node 22):

```bash
cd web
npm ci
npm run dev      # local dev server
npm run build    # production build to web/dist
```

## Rating methodology

- **Elo** — updated per scope (`overall`, discipline, level, and discipline+level,
  e.g. `singles_D`). Initial 1500, K-factor 32, scale 400. Walkovers excluded.
- **Points** — participation per tournament, plus a win bonus scaled by round
  (group → final) and by the draw's level multiplier (E…A/M).
- **Level** — inferred from the levels a player actually competed in, adjusted up
  or down by their winrate.

All parameters live in `config.json`.

### Two rating styles

The frontend has a switcher between them; the backend computes both.

- **Per category** (`elo`, `points`) — a separate table per discipline, optionally
  narrowed to one level.
- **Cross-category** (`unified`) — one rating per player over *every* match. The seed
  is the midpoint of the category the player debuted in (E 1100, D 1300, C 1500,
  B 1700, A 1900; Masters seeds as B), so beating a stronger category pays off from the
  first match. K-factor falls with experience (40 / 32 / 24), doubles and mixed count
  0.75 as much as singles. Categories are 200-point chess-style bands: promotion at the
  next floor but only after 10 rated matches, demotion only 50 points below your own
  floor, and `D → C` once the next floor is within 60 points. Masters is not a rung —
  it's an age bracket. Pairs get no category of their own; the frontend's calculator
  averages two players' ratings, which is exactly the pair strength the engine uses.
