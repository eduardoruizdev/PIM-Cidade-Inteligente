// Alternativa para ambientes Windows onde o cliente TLS do dotnet está indisponível.
// Baixa pacotes oficiais por HTTPS para um feed local; não desativa certificados.
import fs from 'node:fs/promises';
import path from 'node:path';
const feed = path.resolve('.nuget/feed');
await fs.mkdir(feed, { recursive: true });
const seen = new Set();
function score(framework) {
  if (!framework) return 1;
  const f = framework.toLowerCase().replaceAll(' ', '');
  const modern = f.match(/^(?:net|\.netcoreapp)([5-8])\.([0-9])/);
  if (modern) return 100 + Number(modern[1]) * 10 + Number(modern[2]);
  if (f.includes('netstandard2.1')) return 92;
  if (f.includes('netstandard2.0')) return 90;
  if (f.includes('netstandard1.')) return 70;
  if (f.includes('netcoreapp3.1')) return 95;
  return -1;
}
async function download(name, version) {
  name = name.toLowerCase();
  version = version.toLowerCase();
  const key = `${name}/${version}`;
  if (seen.has(key)) return;
  seen.add(key);
  const base = `https://api.nuget.org/v3-flatcontainer/${key}/${name}`;
  const response = await fetch(`${base}.nuspec`);
  if (!response.ok) throw new Error(`${key}: nuspec HTTP ${response.status}`);
  const xml = await response.text();
  const file = path.join(feed, `${name}.${version}.nupkg`);
  try {
    await fs.access(file);
  } catch {
    const pkg = await fetch(`${base}.${version}.nupkg`);
    if (!pkg.ok) throw new Error(`${key}: pacote HTTP ${pkg.status}`);
    await fs.writeFile(file, new Uint8Array(await pkg.arrayBuffer()));
  }
  console.log(`${name} ${version}`);
  const dependencies = xml.match(/<dependencies>([\s\S]*?)<\/dependencies>/)?.[1] || '';
  const groups = [...dependencies.matchAll(/<group\b([^>]*?)(?:\/>|>([\s\S]*?)<\/group>)/g)]
    .map((g) => ({
      score: score(g[1].match(/targetFramework="([^"]+)"/)?.[1]),
      content: g[2] || '',
    }))
    .filter((g) => g.score >= 0)
    .sort((a, b) => b.score - a.score);
  const selected = groups.length ? groups[0].content : dependencies;
  for (const entry of selected.matchAll(/<dependency\s+([^>]+)\/?\s*>/g)) {
    const id = entry[1].match(/\bid="([^"]+)"/)?.[1];
    const v = entry[1]
      .match(/\bversion="([^"]+)"/)?.[1]
      .match(/\d+\.\d+(?:\.\d+)?(?:-[\w.-]+)?/)?.[0];
    if (id && v) await download(id, v);
  }
}
const roots = process.argv.slice(2);
if (roots.length) {
  for (const root of roots) {
    const i = root.lastIndexOf('@');
    await download(root.slice(0, i), root.slice(i + 1));
  }
} else {
  await download('Microsoft.EntityFrameworkCore.SqlServer', '8.0.28');
  await download('Swashbuckle.AspNetCore', '6.9.0');
  await download('Microsoft.NETCore.App.Ref', '8.0.28');
  await download('Microsoft.AspNetCore.App.Ref', '8.0.28');
}
console.log(`Feed local preparado: ${seen.size} pacotes.`);
