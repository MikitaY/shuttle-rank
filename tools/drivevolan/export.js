// Export of individual-tournament results from drivevolan.by.
//
// The site renders everything server-side and only for a signed-in user, so this runs in the
// browser console of any drivevolan.by tab. Every individual tournament is saved as <id>.json —
// drop the files into data/drivevolan/tournaments/. Names stay exactly as the site shows them;
// mapping to our players happens in the scraper via data/drivevolan/players.csv.
// Team tournaments (/team-tournaments/…) are skipped on purpose.
(async () => {
  const load = async (url) =>
    new DOMParser().parseFromString(await (await fetch(url, { credentials: 'include' })).text(), 'text/html');

  const list = await load('/tournament/list');
  const ids = [...new Set([...list.querySelectorAll('main a[href*="/tournament/"]')]
    .map((a) => +new URL(a.href).pathname.split('/')[2])
    .filter(Boolean))].sort((a, b) => a - b);

  // A side is either one name (singles) or a column of names (doubles); the winning side
  // carries the bold class / winner marker.
  const side = (td) => {
    const leaves = [...td.querySelectorAll('div')].filter((d) => !d.querySelector('div'));
    const names = (leaves.length ? leaves.map((d) => d.innerText) : [td.innerText])
      .map((s) => s.trim().replace(/\s+/g, ' '))
      .filter(Boolean);
    return { names, won: !!td.querySelector('.fw-bolder, .winner-indicator') };
  };

  const save = (name, data) => {
    const a = document.createElement('a');
    a.href = URL.createObjectURL(new Blob([JSON.stringify(data, null, 2) + '\n'], { type: 'application/json' }));
    a.download = name;
    a.click();
  };

  const summary = [];
  for (const id of ids) {
    const doc = await load(`/tournament/${id}`);
    const [, d, m, y] = (doc.querySelector('#details')?.innerText ?? '').match(/(\d\d)\.(\d\d)\.(\d{4})/) ?? [];
    const players = [...doc.querySelectorAll('#players a[href^="/player/"]')]
      .map((a) => ({ id: +a.getAttribute('href').split('/')[2], name: a.innerText.trim().replace(/\s+/g, ' ') }))
      .sort((a, b) => a.id - b.id);
    const matches = [...doc.querySelectorAll('#matches .d-none.d-md-block tbody tr')].map((tr) => {
      const c = tr.cells;
      const [category, ...round] = c[2].innerText.trim().split(/\s+/);
      const a = side(c[3]);
      const b = side(c[5]);
      return {
        category,
        round: round.join(' ') || null,
        court: c[0].innerText.trim(),
        time: c[1].innerText.trim().replace(/^—$/, '') || null,
        side_a: a.names,
        side_b: b.names,
        winner: a.won ? 'a' : b.won ? 'b' : null,
        score: c[4].innerText.trim().replace(/\s+/g, ' '),
        status: c[6].innerText.trim(),
      };
    });
    save(`${id}.json`, {
      id,
      url: `https://drivevolan.by/tournament/${id}`,
      name: doc.querySelector('main h1, main h2, main h3')?.innerText.trim(),
      date: y ? `${y}-${m}-${d}` : null,
      exported: new Date().toISOString().slice(0, 10),
      players,
      matches,
    });
    summary.push(`${id}: ${matches.length} matches`);
  }
  return summary;
})();
