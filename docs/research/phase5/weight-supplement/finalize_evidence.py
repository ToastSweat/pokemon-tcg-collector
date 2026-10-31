"""Research-only citation evidence and human-readable per-card transcript rendering."""
from pathlib import Path
from datetime import datetime, timezone
import hashlib
import json
import re
from retrieve_evidence import ledger
from build_supplement import build, ROOT, HERE

if __name__=='__main__':
    result = build()
    (HERE/'sheet-weight-supplement.json').write_text(json.dumps(result,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
    transcription = json.loads((HERE/'manual-transcription.json').read_text(encoding='utf-8'))
    text = (HERE/'article-context.text.txt').read_text(encoding='utf-8')
    quotes = [
        'The objective is to provide the best possible estimates for card rarities for all English sets that were released with randomized booster packs.',
        'The next couple of rows show how many cards (n) appeared how many times (m, “magnitude”) on the sheets.',
        'It is calculated as 1 / (pack insertion rate * magnitude / sheet size), which simplifies to sheet size / (pack insertion rate * magnitude).',
        'however I am going to be using a sequence length 216 packs (1 case of 6 booster boxes with 36 packs each) for all (or at least most) sets from generation 1 to generation 5',
        'those where the Ivy Pikachu was either removed after cutting or replaced with a blank that was removed. These would be 1st Edition as well as unlimited cards.',
        'those where the Ivy Pikachu was not removed, whether by mistake or not. Those are only 1st Edition cards.',
        'those where the blank has been replaced by an Exeggutor. Those would be unlimited cards. It is not clear if this variant was only distributed in Asia.',
        'The sheet is labelled “Pokémon Jungle EN Asia Form”; I don’t know if that meens these cards were intended for sale in Asia.',
        'I am assuming the uncommons sheet now has no blank anymore. The rare and holo sheets may be the same as for the other variants, or they may be different.',
        'Three of the four English sheets (holo, rares, uncommons) are known,  as is a German commons sheet.',
        'Verification with box opening videos shows the English sheet to be the same as the German one.',
    ]
    evidence=[]
    for q in quotes:
        assert q in text, f'not literal article text: {q}'
        offset = text.index(q)
        evidence.append({'citationId':1,'quote':q,'textFile':'article-context.text.txt','line':text[:offset].count('\n')+1,'charOffset':offset})
        ledger('quote','1','--text',q,'--from',str(HERE/'article-context.text.txt'))
    for table in transcription['tables']:
        lines=[table['titleVerbatim'],f"seq {table['sequenceLength']}",f"nseq {table['nseq']} np {table['np']} set {table['setSize']}"]
        for p in table['pools']:
            symbol=p['symbol']
            lines += [f"{symbol} sequence insertions {table['sequenceInsertions'][symbol]}; per-pack printed {table['perPackInsertionsPrinted'][symbol]}"]
            lines += [f"{symbol} n {g['n']} m {g['m']}" for g in p['groups']]
            lines += [f"{symbol} distinct total {p['distinctCardTotal']} emitted position total {p['emittedPositionTotal']}"]
            lines += [f"{symbol} n {g['n']} p {g['p']}" for g in p['pRows']]
            lines += p['exceptionLinesVerbatim']
        if table['footerVerbatim']:
            lines.append(table['footerVerbatim'])
        filename=table['tableId']+'.visual-transcript.txt'
        content='\n'.join(lines)+'\n'
        (HERE/filename).write_text(content,encoding='utf-8')
        q='\n'.join(lines)
        ledger('quote',str(table['citationId']),'--text',q,'--from',str(HERE/filename))
        evidence.append({'citationId':table['citationId'],'imageSourceId':table['imageSourceId'],'quote':q,'textFile':filename,'origin':'MANUAL VISUAL TRANSCRIPTION, not fetched page text or automated OCR; pixels in the hashed PNG are the primary evidence','transcriptSha256':hashlib.sha256((HERE/filename).read_bytes()).hexdigest()})
    (HERE/'literal-evidence-quotes.json').write_text(json.dumps(evidence,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
    old = json.loads((ROOT/'docs/research/phase5/jungle/rarity-guide.retrieval.json').read_text(encoding='utf-8'))
    provenance={'supplementObservedAtUtc':datetime.now(timezone.utc).isoformat(),'articleOriginalRetrieval':old,'articleNotRefetched':True,'originalRawHtmlSha256':hashlib.sha256((ROOT/'docs/research/phase5/jungle/rarity-guide.raw.html').read_bytes()).hexdigest(),'contextInterpretation':'Article list items 1/2/3 immediately precede the three supplied Jungle charts in that order; Fossil chart occurs in Fossil post body, not Jungle. Article source was read as text/HTML only; no linked sheet/card photos followed.','assessments':[
        {'tableId':'jungle-1-removed','scope':'Author says Ivy removed after cutting or blank removed; First Edition and Unlimited; emitted uncommon total120, no promo outcome.'},
        {'tableId':'jungle-2-ivy-error','scope':'Author says Ivy not removed; First Edition only; U1 promo outside existing standard Unlimited catalog. H/R/C exception assignments explicitly assumed unchanged.'},
        {'tableId':'jungle-3-exeggutor','scope':'Author says blank replaced with Exeggutor; Unlimited; Asia exclusivity unknown. H/R/C assignments explicitly assumed unchanged.'},
        {'tableId':'fossil-1','scope':'Heading First Edition & Unlimited; author reports H/R/U English sheets and German commons checked by videos. Neither sheets nor videos independently inspected here.'},
        {'scope':'Belgian fourth Jungle variant','assessment':'Discussed in text, not among the four authorized PNGs. Its numerical table was NOT fetched/viewed/transcribed. No attribution of these vectors to that variant.'},
    ],'notAuthenticated':['Factory sheet counts','Image authenticity/chain of custody','Which physical batch the generic candidate metadata depicts','Mixture prevalence between Jungle variants','Exact factory holo/pack odds','Blank/promo discard/error incidence','Sealed-box correlations, sheet traversal, physical pack orientation']}
    (HERE/'article-provenance-and-variants.json').write_text(json.dumps(provenance,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
    tables={t['tableId']:t for t in result['tables']}
    md=['# Complete per-card multiplicities — research transcription','', 'UNAPPROVED secondary author reconstruction, not factory authentication or rules. Names/numbers/source IDs below exactly match the frozen candidate catalogs; multiplicities are expanded from named exceptions plus the matching rarity complement. This is not a fresh card-art inspection.','', '## Jungle','', 'J1 = Ivy/blank removed; J2 = First Edition error (not Unlimited); J3 = Exeggutor-filled Unlimited. H/R/C exception-name assignments in J2/J3 inherit J1 under the printed “other sheets assumed unchanged” assumption.[1][2][3]', 'J3 is not confirmed Asia-exclusive; do not mix it with J1 or the uninspected Belgian variant.[1][4]','', '| # | Catalog display name | Source metadata ID | Pool | J1 | J2 (ordinary members only) | J3 |','|---|---|---|---|---:|---:|---:|']
    j2={c['collectorNumber']:c for c in tables['jungle-2-ivy-error']['cards']}
    j3={c['collectorNumber']:c for c in tables['jungle-3-exeggutor']['cards']}
    for c in tables['jungle-1-removed']['cards']:
        n=c['collectorNumber']
        md.append(f"| {n} | {c['displayName']} | `{c['sourceId']}` | {c['pool']} | {c['multiplicity']} | {j2[n]['multiplicity']} | {j3[n]['multiplicity']} |")
    md += ['', 'The table above expands the three Jungle PNGs, with the sole declared spelling alias `Nidoran f` → catalog display `Nidoran♀` / source metadata `Nidoran ♀`.[2][3][4]', '', 'J2 additionally has `U1 Pikachu (Promo)` with multiplicity 1, outside the 64-member candidate, with no asserted Jungle collector number. It is not common Pikachu #60.[3]', '', '## Fossil','', '| # | Catalog display name | Source metadata ID | Pool | Multiplicity |','|---|---|---|---|---:|']
    for c in tables['fossil-1']['cards']:
        md.append(f"| {c['collectorNumber']} | {c['displayName']} | `{c['sourceId']}` | {c['pool']} | {c['multiplicity']} |")
    md += ['', 'Fossil multiplicities expand only the supplied Fossil numeric chart, whose context belongs to the Fossil post.[1][5]', '', 'Exact candidate IDs, source names, normalized fractions, source-image IDs, independent histograms/sums and comparisons to uniform weights are in `sheet-weight-supplement.json`. That artifact is research-only.']
    (HERE/'per-card-transcription.md').write_text('\n'.join(md)+'\n',encoding='utf-8')
    print('Attached literal article/pixel-transcript quotes; rendered complete per-card table.')
