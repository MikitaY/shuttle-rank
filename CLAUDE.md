# shuttle-rank

Rating system for the Minsk Badminton League. See [README.md](README.md) for the layout,
commands and rating methodology.

## Language

**Everything written for developers is in English**, no exceptions:

- code comments, XML doc comments, identifiers, test names;
- commit messages, branch names, pull request titles and descriptions;
- developer documentation (`README.md`, this file, code review notes).

**Everything the user reads stays in Russian** — that's the product language:

- UI copy in `web/src` (labels, captions, table headers, methodology notes);
- the standalone methodology page `web/public/rating-guide.html`;
- player names and tournament names from the source data.

So an English comment may quote Russian UI text — `// the "Пары" tab …` is correct.
Do not translate user-facing strings into English, and do not leave Russian in comments.
