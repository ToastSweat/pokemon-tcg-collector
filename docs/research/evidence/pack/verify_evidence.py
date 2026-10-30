"""Verify local evidence excerpts and report citations. No network or simulation."""
from pathlib import Path
import re,json
from datetime import datetime
E=Path(__file__).resolve().parent
report=E.parents[1]/'Pack_Composition_Findings.md'
assert str(report).startswith('F:\\[WORKSPACE]\\[HERMES]\\[CHATGPT]\\PokemonThrowbackTCG\\')
quotes={
'pokemaster.txt':[
'Each booster pack contained 11 trading cards; one rare, three uncommon, five commons, and two energy cards.',
'The rare had a 1 in 3 chance of being a holographic rare.'],
'efour-sequence.txt':['We are all familiar with the base set unlimited having 5 commons, 2 energies, the rare, and then the 3 uncommons from front to back.'],
'efour-order.txt':['Been a long time since I opened but it thought it was energy, uncommons and commons, rare at 7 then remaining energy/commons'],
'efour-rarity.txt':[
'The ultimate output will be a table for each set which assigns to each card a “best-estimate” rarity.',
'Machamp which was not obtainable from booster packs',
'Base 2000 rares: 121 card sheet; since the Unlimited and Base 2000 commons and uncommons are both the same, this is assumed to apply to the rares as well',
'a few cards on the sheet are obscured by objects placed on the sheet',
'the sample size available is not nearly large enough for these older sets to say anything for certain.',
'however I am going to be using a sequence length 216 packs',
'Reorganizing the sheets may have been in order to avoid getting duplicate cards in one pack',
'sheet size / (pack insertion rate * magnitude)'],
'rulebook-v2-ocr.txt':['In addition, some rare cards are printed using holographic foil.',"you can't have more than 4 of any one card other than basic Energy cards in your deck"],
'uncut-sheets.txt':[
'Unlimited Base Uncommon - 8th Run: Company: Yaquinto, Date: 30-7-99 , Form 5, Job # 1739',
'Unlimited Common - 11th Run: Company: PBM Graphics, Date: 10/1/99 , Form 4, Job # P06057270 10 140, PBM # 81369'],
'efour-wrapper.txt':['All packs will be copyright dated either 1999, or 1999-2000.'],
'bulbapedia-base.txt':['Unlimited (©1999–2000) – The fourth print run, identical to the Unlimited print except for the updated copyright line, which reads ©1999–2000 instead of ©1999 .'],
'collation-methods.txt':['This website is not affiliated with Wizards of the Coast LLC in any way.']}
norm=lambda x:re.sub(r'\s+',' ',x).strip()
doc=report.read_text(encoding='utf-8')
results=[]
for f,qs in quotes.items():
    source=norm((E/f).read_text(encoding='utf-8'))
    for q in qs:results.append({'source_file':f,'quote':q,'in_source':norm(q) in source,'in_report':norm(q) in norm(doc)})
labels=set(re.findall(r'\[([A-Z][A-Z0-9; -]+)\]',doc))
individual=sorted(set(x.strip() for group in labels for x in group.split(';')))
missing_labels=[x for x in individual if not re.search(r'^### .*\b'+re.escape(x)+r'\b',doc,re.M)]
math={'pack_total':5+2+1+3,'holo_positions':5*8+10*7,'rare_positions':9*8+7*7,'common_uncommon_positions':25*4+7*3,'energy_positions':35+34+17+17+9+9,'twelve_over_thirtysix':12/36}
summary={'checked_at':datetime.now().astimezone().isoformat(),'report':str(report),'quotes':results,'missing_source_labels':missing_labels,'arithmetic_only_not_simulation':math,'bundled_media_files':[str(p) for p in E.rglob('*') if p.suffix.lower() in {'.jpg','.jpeg','.png','.gif','.webp','.mp3','.wav','.mp4','.pdf'}]}
summary['passed']=all(x['in_source'] and x['in_report'] for x in results) and not missing_labels and not summary['bundled_media_files']
(E/'verification.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'passed':summary['passed'],'excerpts_checked':len(results),'failures':[x for x in results if not(x['in_source'] and x['in_report'])],'missing_source_labels':missing_labels,'media_files':summary['bundled_media_files'],'arithmetic':math},ensure_ascii=False,indent=2))
raise SystemExit(0 if summary['passed'] else 1)
